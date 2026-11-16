"""Selects 500 seed exercises from megaGymDataset.csv.
Usage: python3 select_exercises.py <megaGymDataset.csv> <out.json>
Split (approved): free weights 190, bodyweight 140, machines and cables 110, portable kit 60.
"""
import csv, json, re, sys, collections

QUOTA = {'FreeWeights': 190, 'Bodyweight': 140, 'Machines': 110, 'Portable': 60}
# Share of each group per focus area (rest is filled by score).
FOCUS_SHARE = {
    'FreeWeights': {'Legs': .24, 'Back': .17, 'Chest': .15, 'Shoulders': .15, 'Arms': .19, 'Core': .10},
    'Machines':    {'Legs': .26, 'Back': .22, 'Chest': .15, 'Shoulders': .12, 'Arms': .15, 'Core': .10},
    'Bodyweight':  {'Legs': .27, 'Back': .12, 'Chest': .14, 'Shoulders': .07, 'Arms': .08, 'Core': .32},
    'Portable':    {'Legs': .22, 'Back': .15, 'Chest': .13, 'Shoulders': .15, 'Arms': .13, 'Core': .22},
}
FOCUS = {'Chest': 'Chest', 'Lats': 'Back', 'Middle Back': 'Back', 'Lower Back': 'Back', 'Traps': 'Back',
         'Shoulders': 'Shoulders', 'Neck': 'Shoulders', 'Biceps': 'Arms', 'Triceps': 'Arms', 'Forearms': 'Arms',
         'Abdominals': 'Core', 'Quadriceps': 'Legs', 'Hamstrings': 'Legs', 'Glutes': 'Legs', 'Calves': 'Legs',
         'Abductors': 'Legs', 'Adductors': 'Legs'}
TYPE = {'Strength': 'Strength', 'Stretching': 'Stretching', 'Plyometrics': 'Plyometrics', 'Powerlifting': 'Powerlifting',
        'Cardio': 'Cardio', 'Olympic Weightlifting': 'OlympicWeightlifting', 'Strongman': 'Strongman'}
LEVEL = {'Beginner': 'Beginner', 'Intermediate': 'Intermediate', 'Expert': 'Advanced'}

# Branded programme entries, partner drills and other non-standard names.
BRAND = re.compile(r'^(Holman|30|HM|FYR2?|MetaBurn|AM|UP|TBS|UNS|UN|Total Fitness|King Maker|Paul Carter|Partner|Taylor|KV|'
                   r'Dumbbell Fix|Tyler Holt|McGill|Muscle Up|Bear Camp|Kiss Me|BFA|Jefit|Lee Labrada|Kris Gethin|Linda)\b', re.I)
JUNK = re.compile(r'Gethin|partner|Variation|Complex\b|\bto sprint\b|juggle|Otis|Bosu|sledgehammer|tire|keg|atlas|yoke|log lift|'
                  r'chains?\b|\bPVC\b|circus|car driver|rickshaw|conan|axle', re.I)

STAPLES = [  # widely used movements; matched against the cleaned name
    r'^(barbell )?(back |front |goblet |bodyweight |box |split |bulgarian split |sumo |hack |overhead )?squats?\b', r'\bbench press\b', r'\bdeadlift', r'\bpull-?ups?\b',
    r'\bchin-?ups?\b', r'\bpush-?ups?\b', r'\bdips?\b', r'\bplank\b', r'\blunges?\b', r'\blat pull-?down', r'\bleg press\b',
    r'\bleg extensions?\b', r'\b(leg|hamstring) curls?\b', r'\b(biceps?|hammer|preacher|concentration|ez-bar|barbell|dumbbell|cable) curls?\b',
    r'\b(bent[- ]over|seated cable|one-arm dumbbell|t-bar|inverted|upright|cable|dumbbell|barbell)? ?rows?\b',
    r'\b(overhead|shoulder|military|arnold|push) press\b', r'\blateral raises?\b', r'\bfront raises?\b', r'\b(rear delt|reverse) fl(y|ies|yes)\b',
    r'\bface pull', r'\bshrugs?\b', r'\bhip thrust', r'\bglute bridge', r'\bcalf raises?\b', r'\b(chest |cable |dumbbell |pec deck )?fl(y|ies|yes)\b',
    r'\bcrossover', r'\bpullover', r'\b(triceps?) (push-?down|extension|kickback)', r'\bskull ?crusher', r'\bcrunch(es)?\b', r'\bsit-?ups?\b',
    r'\bleg raises?\b', r'\bknee raises?\b', r'\brussian twist', r'\bmountain climber', r'\bburpee', r'\bjumping jack', r'\bbox jump',
    r'\bjump squat', r'\bstep-?ups?\b', r'\bgood morning', r'\bhyperextension', r'\bback extension', r'\bkettlebell swing', r'\bturkish get-?up',
    r'\bfarmer', r'\bclean\b', r'\bsnatch\b', r'\bthruster', r'\bwall sit', r'\bsuperman', r'\bbird dog', r'\bdead bug', r'\bbicycle',
    r'\bhip (abduction|adduction)', r'\bthigh (abductor|adductor)', r'\bchest press\b', r'\bpec deck', r'\bsmith machine', r'\bcable', r'\bwood ?chop',
    r'\bpallof', r'\bab (roller|wheel)', r'\bjump rope|rope jumping', r'\bslam\b', r'\bband pull-?apart', r'\bside plank', r'\bflutter kick',
    r'\bhollow', r'\bv-up', r'\bdonkey kick', r'\bfire hydrant', r'\bhigh knees', r'\bstretch\b',
]
STAPLES = [re.compile(p, re.I) for p in STAPLES]

def group_of(row):
    e, t = row['Equipment'].strip(), row['Title'].lower()
    # The name is more reliable than the dataset's equipment column for these.
    if re.search(r'smith machine|\bmachine\b|\bcable\b|leverage', t): return 'Machines'
    if re.search(r'dumbbell|barbell|kettlebell|ez-bar', t) and e not in ('Machine', 'Cable'): return 'FreeWeights'
    if re.search(r'\bband(s|ed)?\b', t) and e in ('Body Only', 'None', '', 'Other'): return 'Portable'
    if e in ('Machine', 'Cable'): return 'Machines'
    if e in ('Dumbbell', 'Barbell', 'E-Z Curl Bar', 'Kettlebells'): return 'FreeWeights'
    if e in ('Body Only', 'None', ''): return 'Bodyweight'
    if e in ('Bands', 'Medicine Ball', 'Exercise Ball', 'Foam Roll'): return 'Portable'
    # "Other": keep only what clearly belongs to a group.
    if re.search(r"\bdips?\b|captain's chair|parallel bars?|hanging", t): return 'Bodyweight'
    if re.search(r'landmine|\bplate\b|trap bar', t): return 'FreeWeights'
    if re.search(r'ab roller|ab wheel|suspended|battle rope|jump rope|rope jumping', t): return 'Portable'
    return None

GROUP_EQUIPMENT = {'Machines': ('Machine', 'Cable'), 'FreeWeights': ('Dumbbell', 'Barbell', 'E-Z Curl Bar', 'Kettlebells'),
                   'Bodyweight': ('Body Only',), 'Portable': ('Bands', 'Medicine Ball', 'Exercise Ball', 'Foam Roll')}

def equipment_of(row, group):
    """The dataset's equipment, unless the name moved the exercise to another group."""
    e, t = row['Equipment'].strip(), row['Title'].lower()
    if e in GROUP_EQUIPMENT[group]: return e
    for word, label in (('cable', 'Cable'), ('machine', 'Machine'), ('leverage', 'Machine'), ('dumbbell', 'Dumbbell'),
                        ('kettlebell', 'Kettlebells'), ('ez-bar', 'E-Z Curl Bar'), ('barbell', 'Barbell'), ('plate', 'Weight Plate'),
                        ('landmine', 'Barbell'), ('trap bar', 'Trap Bar'), ('band', 'Bands'), ('suspended', 'Suspension Trainer'),
                        ('rope', 'Rope'), ('ab roller', 'Ab Wheel'), ('ab wheel', 'Ab Wheel'), ('dip', 'Dip Bars'),
                        ('parallel bar', 'Dip Bars'), ("captain's chair", "Captain's Chair"), ('hanging', 'Pull-Up Bar')):
        if word in t: return label
    return 'Body Only' if group == 'Bodyweight' else e or 'Other'

def clean(title):
    t = re.sub(r'\s+', ' ', title).strip().rstrip('-').strip()
    return t[0].upper() + t[1:]

def base_key(name):
    """Last two words, e.g. 'bench press', so near-identical variations can be capped."""
    words = re.sub(r'[^a-z ]', ' ', name.lower().replace('-', '')).split()
    words = [w.rstrip('s') for w in words if w not in ('with', 'bands', 'on', 'the', 'a', 'to', 'and', 'of')]
    return ' '.join(words[-2:])

def score(row, name):
    s = 0.0
    if any(p.search(name) for p in STAPLES): s += 10
    try: rating = float(row['Rating'] or 0)
    except ValueError: rating = 0
    s += rating
    if row['Desc'].strip(): s += 1
    if row['Level'] == 'Beginner': s += .5
    s -= .5 * max(0, len(name.split()) - 3)
    return s

def main(src, out):
    rows = list(csv.DictReader(open(src, encoding='utf-8', errors='replace')))
    seen, pool = set(), collections.defaultdict(list)
    for r in rows:
        name = clean(r['Title'])
        key = re.sub(r'[^a-z0-9]', '', name.lower()).rstrip('s')
        if key in seen or BRAND.search(name) or JUNK.search(name) or r['BodyPart'] not in FOCUS: continue
        g = group_of(r)
        if not g: continue
        seen.add(key)
        pool[g].append({'name': name, 'type': TYPE[r['Type']], 'bodyPart': r['BodyPart'], 'focus': FOCUS[r['BodyPart']],
                        'equipment': equipment_of(r, g), 'equipmentGroup': g, 'level': LEVEL[r['Level']],
                        '_score': score(r, name), '_base': base_key(name)})
    picked = []
    for g, quota in QUOTA.items():
        cands = sorted(pool[g], key=lambda c: (-c['_score'], c['name']))
        chosen, per_base = [], collections.Counter()
        cap = 5 if g == 'FreeWeights' else 4
        def take(pred, n):
            for c in cands:
                if n <= 0: break
                if c in chosen or not pred(c) or per_base[c['_base']] >= cap: continue
                chosen.append(c); per_base[c['_base']] += 1; n -= 1
        if g == 'Bodyweight':  # a base of mobility and conditioning work
            take(lambda c: c['type'] == 'Stretching', 20)
            take(lambda c: c['type'] in ('Plyometrics', 'Cardio'), 14)
        strength = lambda c: c['type'] not in ('Stretching',) or g != 'Bodyweight'
        remaining = quota - len(chosen)
        for focus, share in FOCUS_SHARE[g].items():
            take(lambda c: c['focus'] == focus and strength(c), round(remaining * share))
        take(lambda c: True, quota - len(chosen))
        assert len(chosen) == quota, (g, len(chosen))
        picked += chosen
    picked.sort(key=lambda c: (c['equipmentGroup'], c['focus'], c['name']))
    assert len({c['name'].lower() for c in picked}) == 500
    data = [{k: v for k, v in c.items() if not k.startswith('_') and k != 'focus'} for c in picked]
    json.dump(data, open(out, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    for g in QUOTA:
        gs = [c for c in picked if c['equipmentGroup'] == g]
        print(g, len(gs), dict(collections.Counter(c['focus'] for c in gs)), dict(collections.Counter(c['type'] for c in gs)),
              dict(collections.Counter(c['level'] for c in gs)))
    return picked

if __name__ == '__main__':
    p = main(sys.argv[1], sys.argv[2])
    import random; random.seed(3)
    for g in QUOTA:
        gs = [c['name'] for c in p if c['equipmentGroup'] == g]
        print('\n', g, '|'.join(random.sample(gs, 45)))
