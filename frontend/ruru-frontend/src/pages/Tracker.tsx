import { useCallback, useEffect, useMemo, useRef, useState, type Dispatch, type SetStateAction } from 'react'
import { Logo, ThemeToggle, BeadString } from '../components'
import { Walkthrough, type WalkthroughStep } from '../walkthrough'
import { clearToken } from '../services/session'
import { EXERCISES, FOODS, TARGETS, findExercise, findFood, kcalOf, macrosFor, sumMacros, fmt, uid } from '../data'
import type { Meal, MealItem, MacroTotals } from '../models/Nutrition'
import type { Workout, WorkoutExercise, WorkoutSet } from '../models/Workout'

export type TrackerTab = 'workout' | 'meals'
type Notify = (msg: string, undo?: () => void) => void
type Setter<T> = Dispatch<SetStateAction<T>>

interface Toast {
  id: string
  msg: string
  undo?: () => void
}

const today = new Intl.DateTimeFormat('en-ZA', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date())

const initialWorkout: Workout = {
  name: 'Push day',
  exercises: [
    {
      key: uid(),
      exId: 'bench',
      sets: [
        { key: uid(), kg: 60, reps: 8, done: true },
        { key: uid(), kg: 70, reps: 6, done: false },
        { key: uid(), kg: 70, reps: 6, done: false },
      ],
    },
    { key: uid(), exId: 'ohp', sets: [{ key: uid(), kg: 40, reps: 8, done: false }] },
  ],
}

const initialMeals: Meal[] = [
  {
    key: uid(),
    name: 'Breakfast',
    items: [
      { key: uid(), foodId: 'oats', g: 60 },
      { key: uid(), foodId: 'amasi', g: 250 },
      { key: uid(), foodId: 'banana', g: 120 },
    ],
  },
  {
    key: uid(),
    name: 'Lunch',
    items: [
      { key: uid(), foodId: 'pap', g: 300 },
      { key: uid(), foodId: 'chakalaka', g: 100 },
      { key: uid(), foodId: 'chicken', g: 150 },
    ],
  },
]

interface PickerOption {
  id: string
  name: string
}

interface PickerProps<T extends PickerOption> {
  label: string
  options: T[]
  onPick: (id: string) => void
  onClose: () => void
  describe: (option: T) => string
}

/* Inline search list used for both lifts and foods. */
function Picker<T extends PickerOption>({ label, options, onPick, onClose, describe }: PickerProps<T>) {
  const [q, setQ] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)
  useEffect(() => inputRef.current?.focus(), [])
  const shown = options.filter((o) => o.name.toLowerCase().includes(q.trim().toLowerCase()))
  return (
    <div className="picker" onKeyDown={(e) => e.key === 'Escape' && onClose()}>
      <div className="picker-search">
        <label className="sr-only" htmlFor={`pick-${label}`}>{label}</label>
        <input
          id={`pick-${label}`}
          ref={inputRef}
          type="search"
          autoComplete="off"
          placeholder={`${label}…`}
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>Cancel</button>
      </div>
      {shown.length ? (
        <ul className="picker-list">
          {shown.map((o) => (
            <li key={o.id}>
              <button type="button" onClick={() => onPick(o.id)}>
                <span>{o.name}</span>
                <span className="muted">{describe(o)}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="empty">Nothing matches “{q}”. Try a shorter word.</p>
      )}
    </div>
  )
}

interface WorkoutPanelProps {
  workout: Workout
  setWorkout: Setter<Workout>
  notify: Notify
}

function WorkoutPanel({ workout, setWorkout, notify }: WorkoutPanelProps) {
  const [picking, setPicking] = useState(false)
  const done = workout.exercises.flatMap((e) => e.sets).filter((s) => s.done)
  const volume = done.reduce((a, s) => a + (Number(s.kg) || 0) * (Number(s.reps) || 0), 0)

  const updateEx = (key: string, fn: (e: WorkoutExercise) => WorkoutExercise) =>
    setWorkout((w) => ({ ...w, exercises: w.exercises.map((e) => (e.key === key ? fn(e) : e)) }))
  const updateSet = (exKey: string, setKey: string, patch: Partial<WorkoutSet>) =>
    updateEx(exKey, (e) => ({ ...e, sets: e.sets.map((s) => (s.key === setKey ? { ...s, ...patch } : s)) }))

  function removeExercise(ex: WorkoutExercise) {
    const prev = workout
    setWorkout((w) => ({ ...w, exercises: w.exercises.filter((e) => e.key !== ex.key) }))
    notify(`Removed ${findExercise(ex.exId).name}.`, () => setWorkout(prev))
  }

  return (
    <section className="panel workout-panel" aria-labelledby="workout-h">
      <div className="panel-head">
        <h2 id="workout-h" className="panel-title">Workout</h2>
        <label className="sr-only" htmlFor="wname">Workout name</label>
        <input
          id="wname"
          className="title-input"
          value={workout.name}
          onChange={(e) => setWorkout((w) => ({ ...w, name: e.target.value }))}
          autoComplete="off"
        />
        <dl className="stats">
          <div><dt>Volume</dt><dd>{fmt(volume)} kg</dd></div>
          <div><dt>Sets done</dt><dd>{done.length}</dd></div>
          <div><dt>Lifts</dt><dd>{workout.exercises.length}</dd></div>
        </dl>
      </div>

      {workout.exercises.length === 0 && !picking && (
        <p className="empty">No lifts yet. Add your first one to start logging sets.</p>
      )}

      {workout.exercises.map((ex) => {
        const meta = findExercise(ex.exId)
        return (
          <article className="block" key={ex.key}>
            <header className="block-head">
              <div>
                <h3>{meta.name}</h3>
                <p className="muted">{meta.group}</p>
              </div>
              <button type="button" className="icon-btn" onClick={() => removeExercise(ex)} aria-label={`Remove ${meta.name}`}>
                <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
              </button>
            </header>
            <table className="sets">
              <thead>
                <tr>
                  <th scope="col">Set</th>
                  <th scope="col">kg</th>
                  <th scope="col">Reps</th>
                  <th scope="col"><span className="sr-only">Done</span></th>
                </tr>
              </thead>
              <tbody>
                {ex.sets.map((s, i) => (
                  <tr key={s.key} className={s.done ? 'done' : ''}>
                    <td>{i + 1}</td>
                    <td>
                      <input type="number" inputMode="decimal" min="0" step="2.5" value={s.kg} aria-label={`Set ${i + 1} weight in kg`} onChange={(e) => updateSet(ex.key, s.key, { kg: e.target.value })} />
                    </td>
                    <td>
                      <input type="number" inputMode="numeric" min="0" value={s.reps} aria-label={`Set ${i + 1} reps`} onChange={(e) => updateSet(ex.key, s.key, { reps: e.target.value })} />
                    </td>
                    <td>
                      <label className="check">
                        <input type="checkbox" checked={s.done} onChange={(e) => updateSet(ex.key, s.key, { done: e.target.checked })} />
                        <span className="tick" aria-hidden="true" />
                        <span className="sr-only">Mark set {i + 1} done</span>
                      </label>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() =>
                updateEx(ex.key, (e) => {
                  const last: Pick<WorkoutSet, 'kg' | 'reps'> = e.sets[e.sets.length - 1] ?? { kg: 20, reps: 8 }
                  return { ...e, sets: [...e.sets, { key: uid(), kg: last.kg, reps: last.reps, done: false }] }
                })
              }
            >
              Add set
            </button>
          </article>
        )
      })}

      {picking ? (
        <Picker
          label="Search lifts"
          options={EXERCISES}
          describe={(o) => o.group}
          onClose={() => setPicking(false)}
          onPick={(id) => {
            setWorkout((w) => ({ ...w, exercises: [...w.exercises, { key: uid(), exId: id, sets: [{ key: uid(), kg: 20, reps: 8, done: false }] }] }))
            setPicking(false)
          }}
        />
      ) : (
        <button type="button" className="btn btn-outline btn-block" onClick={() => setPicking(true)}>Add lift</button>
      )}

      <button
        type="button"
        className="btn btn-primary btn-block"
        disabled={done.length === 0}
        onClick={() => notify(`${workout.name || 'Workout'} saved: ${done.length} sets, ${fmt(volume)} kg. Sharp sharp!`)}
      >
        Finish workout
      </button>
      {done.length === 0 && <p className="hint center">Tick off at least one set to finish.</p>}
    </section>
  )
}

interface MealsPanelProps {
  meals: Meal[]
  setMeals: Setter<Meal[]>
  notify: Notify
}

interface MealItemWithMacros extends MealItem {
  m: MacroTotals
}

interface MealWithMacros extends Omit<Meal, 'items'> {
  items: MealItemWithMacros[]
  total: MacroTotals
}

function MealsPanel({ meals, setMeals, notify }: MealsPanelProps) {
  const [pickingFor, setPickingFor] = useState<string | null>(null)

  const withMacros = useMemo<MealWithMacros[]>(
    () =>
      meals.map((meal) => {
        const items = meal.items.map((it) => ({ ...it, m: macrosFor(it.foodId, it.g) }))
        return { ...meal, items, total: sumMacros(items.map((i) => i.m)) }
      }),
    [meals],
  )
  const day = sumMacros(withMacros.map((m) => m.total))
  const left = TARGETS.kcal - day.kcal

  const updateMeal = (key: string, fn: (m: Meal) => Meal) => setMeals((ms) => ms.map((m) => (m.key === key ? fn(m) : m)))

  function removeItem(meal: MealWithMacros, item: MealItem) {
    const prev = meals
    updateMeal(meal.key, (m) => ({ ...m, items: m.items.filter((i) => i.key !== item.key) }))
    notify(`Removed ${findFood(item.foodId).name} from ${meal.name}.`, () => setMeals(prev))
  }
  function removeMeal(meal: MealWithMacros) {
    const prev = meals
    setMeals((ms) => ms.filter((m) => m.key !== meal.key))
    notify(`Removed ${meal.name || 'meal'}.`, () => setMeals(prev))
  }

  return (
    <section className="panel meals-panel" aria-labelledby="meals-h">
      <div className="panel-head">
        <h2 id="meals-h" className="panel-title">Food</h2>
        <p className="kcal-left">
          <span className="kcal-num">{fmt(Math.abs(left))}</span>
          <span>kcal {left >= 0 ? 'left today' : 'over today'}</span>
        </p>
        <div className="strands">
          <BeadString label="Calories" value={day.kcal} target={TARGETS.kcal} unit="kcal" tone="k" />
          <BeadString label="Protein" value={day.p} target={TARGETS.p} tone="p" />
          <BeadString label="Carbs" value={day.c} target={TARGETS.c} tone="c" />
          <BeadString label="Fat" value={day.f} target={TARGETS.f} tone="f" />
        </div>
      </div>

      {withMacros.length === 0 && <p className="empty">No meals yet. Add one and start with what you ate last.</p>}

      {withMacros.map((meal) => (
        <article className="block" key={meal.key}>
          <header className="block-head">
            <div className="meal-name">
              <label className="sr-only" htmlFor={`mn-${meal.key}`}>Meal name</label>
              <input
                id={`mn-${meal.key}`}
                className="title-input sm"
                value={meal.name}
                autoComplete="off"
                onChange={(e) => updateMeal(meal.key, (m) => ({ ...m, name: e.target.value }))}
              />
              <p className="muted macro-line">
                <span className="m p">P {fmt(meal.total.p)} g</span>
                <span className="m c">C {fmt(meal.total.c)} g</span>
                <span className="m f">F {fmt(meal.total.f)} g</span>
              </p>
            </div>
            <p className="meal-kcal"><span className="kcal-num">{fmt(meal.total.kcal)}</span> kcal</p>
          </header>

          {meal.items.length > 0 ? (
            <ul className="food-list">
              {meal.items.map((it) => {
                const food = findFood(it.foodId)
                return (
                  <li key={it.key}>
                    <span className="food-name">{food.name}</span>
                    <span className="grams">
                      <input
                        type="number"
                        inputMode="decimal"
                        min="0"
                        step="5"
                        value={it.g}
                        aria-label={`${food.name}, grams`}
                        onChange={(e) =>
                          updateMeal(meal.key, (m) => ({ ...m, items: m.items.map((x) => (x.key === it.key ? { ...x, g: e.target.value } : x)) }))
                        }
                      />
                      <span aria-hidden="true">g</span>
                    </span>
                    <span className="food-kcal">{fmt(it.m.kcal)} kcal</span>
                    <button type="button" className="icon-btn" onClick={() => removeItem(meal, it)} aria-label={`Remove ${food.name}`}>
                      <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
                    </button>
                  </li>
                )
              })}
            </ul>
          ) : (
            <p className="empty">Nothing in this meal yet.</p>
          )}

          {pickingFor === meal.key ? (
            <Picker
              label="Search foods"
              options={FOODS}
              describe={(o) => `${fmt(kcalOf(o))} kcal / 100 g`}
              onClose={() => setPickingFor(null)}
              onPick={(id) => {
                updateMeal(meal.key, (m) => ({ ...m, items: [...m.items, { key: uid(), foodId: id, g: 100 }] }))
                setPickingFor(null)
              }}
            />
          ) : (
            <div className="row-actions">
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setPickingFor(meal.key)}>Add food</button>
              <button type="button" className="btn btn-ghost btn-sm danger" onClick={() => removeMeal(meal)}>Remove meal</button>
            </div>
          )}
        </article>
      ))}

      <button
        type="button"
        className="btn btn-outline btn-block"
        onClick={() => {
          const key = uid()
          const names = ['Breakfast', 'Lunch', 'Dinner', 'Snack']
          const name = names[meals.length] ?? `Meal ${meals.length + 1}`
          setMeals((ms) => [...ms, { key, name, items: [] }])
          setPickingFor(key)
        }}
      >
        Add meal
      </button>
    </section>
  )
}

const TOUR_STEPS: WalkthroughStep[] = [
  {
    target: '.workout-panel .panel-head',
    tab: 'workout',
    title: 'Log your workout',
    body: 'Name your session, add lifts, then tick off each set as you finish it. Volume and sets done add up here.',
  },
  {
    target: '.meals-panel .kcal-left',
    tab: 'meals',
    title: 'Track what you eat',
    body: 'Add foods to each meal and see how many calories you have left today.',
  },
  {
    target: '.meals-panel .strands',
    tab: 'meals',
    title: 'Watch the beads fill',
    body: 'Each strand is one target: calories, protein, carbs and fat. Beads fill as you log food.',
  },
]

export default function Tracker({ tab, tour = false }: { tab: TrackerTab; tour?: boolean }) {
  const [workout, setWorkout] = useState<Workout>(initialWorkout)
  const [meals, setMeals] = useState<Meal[]>(initialMeals)
  const [toast, setToast] = useState<Toast | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const notify: Notify = (msg, undo) => {
    clearTimeout(timer.current)
    setToast({ msg, undo, id: uid() })
    timer.current = setTimeout(() => setToast(null), 6000)
  }
  useEffect(() => () => clearTimeout(timer.current), [])

  // The walkthrough reads and changes the tab through the URL, like the tab links do.
  const showTab = useCallback((next: TrackerTab) => {
    if (next !== tab) window.location.hash = `#/app?tab=${next}&tour=1`
  }, [tab])
  const endTour = useCallback(() => window.location.replace(`#/app?tab=${tab}`), [tab])

  return (
    <div className="page tracker" data-tab={tab}>
      <header className="topbar app-bar">
        <Logo href="#/app" />
        <p className="today">{today}</p>
        <nav className="topbar-actions" aria-label="Account">
          <a className="link" href="#/" onClick={clearToken}>Log out</a>
          <ThemeToggle />
        </nav>
      </header>

      <nav className="tabs" aria-label="Sections">
        <a href="#/app?tab=workout" aria-current={tab === 'workout' ? 'page' : undefined}>Workout</a>
        <a href="#/app?tab=meals" aria-current={tab === 'meals' ? 'page' : undefined}>Food</a>
      </nav>

      <main className="split">
        <WorkoutPanel workout={workout} setWorkout={setWorkout} notify={notify} />
        <div className="seam" aria-hidden="true" />
        <MealsPanel meals={meals} setMeals={setMeals} notify={notify} />
      </main>

      <div className="toast-region" aria-live="polite">
        {toast && (
          <div className="toast" key={toast.id}>
            <span>{toast.msg}</span>
            {toast.undo && (
              <button type="button" className="link" onClick={() => { toast.undo?.(); setToast(null) }}>Undo</button>
            )}
          </div>
        )}
      </div>

      {tour && <Walkthrough steps={TOUR_STEPS} onTab={showTab} onDone={endTour} />}
    </div>
  )
}
