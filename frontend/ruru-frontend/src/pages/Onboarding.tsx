import { useEffect, useId, useRef, useState, type FormEvent, type PointerEvent, type ReactNode } from 'react'
import { Logo, ThemeToggle } from '../components'
import { EMPTY_ANSWERS, type IOnboardingAnswers } from '../models/Onboarding'
import { ApiError } from '../services/http'
import { CompleteOnboarding, GetOnboardingStatus, GiveConsent, SaveOnboardingAnswers, SkipOnboarding } from '../services/Onboarding'
import { clearToken } from '../services/session'
import { bmi, bmiBand, healthyWeightRange, weeklyChangeKg, HEALTHY_BMI_MAX, HEALTHY_BMI_MIN } from '../onboarding/bmi'
import {
  CONSENT_POINTS,
  CONSENT_VERSION,
  CONTACT_EMAIL,
  INFORMATION_REGULATOR_URL,
  PRIVACY_POLICY_URL,
  RESPONSIBLE_PARTY,
} from '../onboarding/consent'
import {
  MINIMUM_AGE,
  SECTIONS,
  STEPS,
  answerError,
  isAnswered,
  type MultiKey,
  type MultiQuestion,
  type NumberQuestion,
  type Question,
  type SingleKey,
  type SingleQuestion,
} from '../onboarding/questions'

type Phase = 'loading' | 'consent' | 'questions' | 'summary' | 'failed'

const TOUR_ROUTE = '#/app?tour=1'
const SWIPE_PX = 60

/*
  Onboarding: consent notice, then one question per screen in the style of stories
  (a bar per section along the top), then a summary of where the plan is heading.
  Answers are kept in state rather than a form action because they drive live
  feedback (BMI, age checks) and persist across steps; each finished section is
  saved to the API so the user can leave and pick up where they left off.
*/
export default function Onboarding() {
  const [phase, setPhase] = useState<Phase>('loading')
  const [answers, setAnswers] = useState<IOnboardingAnswers>(EMPTY_ANSWERS)
  // Multi-choice questions answered with "None of these" (stored as an empty list).
  const [noneChosen, setNoneChosen] = useState<ReadonlySet<string>>(new Set())
  const [step, setStep] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    GetOnboardingStatus()
      .then((status) => {
        if (status.isComplete) {
          window.location.replace('#/app')
          return
        }
        const saved = { ...EMPTY_ANSWERS, ...status.answers }
        const firstOpen = STEPS.findIndex((s) => !isAnswered(s.question, saved, new Set()))
        setAnswers(saved)
        setStep(firstOpen === -1 ? STEPS.length - 1 : firstOpen)
        setPhase(status.hasConsent ? 'questions' : 'consent')
      })
      .catch((err: unknown) => {
        if (err instanceof ApiError && err.status === 401) return signOut()
        setPhase('failed')
      })
  }, [])

  /** Runs an API call with a busy state and an inline error message. */
  async function run(task: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await task()
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) return signOut()
      setError(err instanceof Error ? err.message : 'Something went wrong. Try again.')
    } finally {
      setBusy(false)
    }
  }

  const agree = () =>
    run(async () => {
      await GiveConsent(CONSENT_VERSION)
      setPhase('questions')
    })

  const notNow = () =>
    run(async () => {
      await SkipOnboarding()
      window.location.hash = TOUR_ROUTE
    })

  /** Moves forward one question, saving at the end of each section and finishing at the end. */
  const advance = () =>
    run(async () => {
      const current = STEPS[step]
      const next = STEPS[step + 1]
      const endOfSection = !next || next.sectionIndex !== current.sectionIndex
      if (endOfSection) await SaveOnboardingAnswers(answers)
      if (!next) {
        await CompleteOnboarding()
        setPhase('summary')
        return
      }
      // Keeping fit usually means staying put, so start the goal at today's weight.
      if (next.question.key === 'goalWeightKg' && answers.goalWeightKg === null && answers.mainGoal === 'KeepFit') {
        setAnswers((a) => ({ ...a, goalWeightKg: a.currentWeightKg }))
      }
      setStep(step + 1)
    })

  const back = () => {
    setError(null)
    setStep((s) => Math.max(0, s - 1))
  }

  if (phase === 'loading') return <Shell><p className="muted onboard-status">Loading…</p></Shell>
  if (phase === 'failed') {
    return (
      <Shell>
        <div className="onboard-status">
          <p>We couldn’t load your setup. Check your connection and try again.</p>
          <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>Try again</button>
        </div>
      </Shell>
    )
  }
  if (phase === 'consent') return <ConsentScreen busy={busy} error={error} onAgree={agree} onNotNow={notNow} />
  if (phase === 'summary') return <Summary answers={answers} />

  const { question, section, sectionIndex, indexInSection } = STEPS[step]
  const problem = answerError(question, answers)
  const answered = isAnswered(question, answers, noneChosen)
  const canContinue = answered && !problem && !busy
  const isLast = step === STEPS.length - 1

  const setSingle = (key: SingleKey, value: string | number) =>
    setAnswers((a) => ({ ...a, [key]: value }) as IOnboardingAnswers)

  const toggleMulti = (q: MultiQuestion, value: string) => {
    setAnswers((a) => {
      const picked = new Set<string | number>(a[q.key])
      if (picked.has(value)) picked.delete(value)
      else picked.add(value)
      // Keep the order the options are listed in (matters for training days).
      return { ...a, [q.key]: q.options.map((o) => o.value).filter((v) => picked.has(v)) } as IOnboardingAnswers
    })
    setNoneChosen((s) => withoutKey(s, q.key))
  }

  const chooseNone = (key: MultiKey) => {
    setAnswers((a) => ({ ...a, [key]: [] }))
    setNoneChosen((s) => new Set(s).add(key))
  }

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    if (canContinue) void advance()
  }

  return (
    <Shell>
      <StoryHeader
        sectionIndex={sectionIndex}
        indexInSection={indexInSection}
        step={step}
        title={section.title}
        onBack={step > 0 ? back : undefined}
      />

      <Swipe onNext={canContinue ? () => void advance() : undefined} onBack={step > 0 ? back : undefined}>
        <form className="story" onSubmit={submit} noValidate aria-labelledby="story-q">
          {/* Remounting per question resets local input state and replays the slide-in. */}
          <div className="story-body" key={step}>
            <h1 id="story-q" className="story-q">{question.title}</h1>
            {question.subtitle && <p className="story-sub">{question.subtitle}</p>}

            {question.kind === 'single' && (
              <SingleChoice question={question} value={answers[question.key]} onPick={(v) => setSingle(question.key, v)} />
            )}
            {question.kind === 'multi' && (
              <MultiChoice
                question={question}
                values={answers[question.key]}
                noneChosen={noneChosen.has(question.key)}
                onToggle={(v) => toggleMulti(question, v)}
                onNone={() => chooseNone(question.key)}
              />
            )}
            {question.kind === 'number' && (
              <NumberField
                question={question}
                value={answers[question.key]}
                invalid={!!problem}
                onChange={(v) => setAnswers((a) => ({ ...a, [question.key]: v }))}
              />
            )}
            {question.kind === 'date' && (
              <DateField value={answers.dateOfBirth} invalid={!!problem} onChange={(v) => setAnswers((a) => ({ ...a, dateOfBirth: v }))} />
            )}

            {problem ? (
              <p className="field-error" id="story-err" role="alert">{problem}</p>
            ) : (
              <Feedback question={question} answers={answers} />
            )}
            {error && <p className="field-error" role="alert">{error}</p>}
          </div>

          <div className="story-actions">
            <button className="btn btn-primary btn-block" type="submit" disabled={!canContinue}>
              {busy ? 'Saving…' : isLast ? 'Finish' : 'Next'}
            </button>
            {!question.required && (
              <button type="button" className="btn btn-ghost btn-block" disabled={busy} onClick={() => void advance()}>
                Skip
              </button>
            )}
          </div>
        </form>
      </Swipe>
    </Shell>
  )
}

function signOut() {
  clearToken()
  window.location.replace('#/login')
}

function withoutKey(set: ReadonlySet<string>, key: string): ReadonlySet<string> {
  if (!set.has(key)) return set
  const next = new Set(set)
  next.delete(key)
  return next
}

function Shell({ children }: { children: ReactNode }) {
  return (
    <div className="page onboard">
      <header className="topbar onboard-top">
        <Logo href="#/welcome" />
        <ThemeToggle />
      </header>
      <main className="onboard-main">{children}</main>
    </div>
  )
}

interface StoryHeaderProps {
  sectionIndex: number
  indexInSection: number
  step: number
  title: string
  onBack?: () => void
}

/** One bar per section, filled like stories: done, in progress, or not started. */
function StoryHeader({ sectionIndex, indexInSection, step, title, onBack }: StoryHeaderProps) {
  return (
    <div className="story-head">
      <button type="button" className="story-back" onClick={onBack} disabled={!onBack} aria-label="Previous question">
        <svg width="22" height="22" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M15 5l-7 7 7 7" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      <div className="story-progress">
        <p className="story-section">{title}</p>
        <div
          className="story-bars"
          role="progressbar"
          aria-label="Setup progress"
          aria-valuemin={1}
          aria-valuemax={STEPS.length}
          aria-valuenow={step + 1}
          aria-valuetext={`Question ${step + 1} of ${STEPS.length}, ${title}`}
        >
          {SECTIONS.map((s, i) => {
            const fill = i < sectionIndex ? 1 : i > sectionIndex ? 0 : (indexInSection + 1) / s.questions.length
            return (
              <span key={s.id} className="story-bar">
                <span style={{ transform: `scaleX(${fill})` }} />
              </span>
            )
          })}
        </div>
      </div>
      <span className="story-count" aria-hidden="true">{step + 1}/{STEPS.length}</span>
    </div>
  )
}

/** Horizontal swipes on touch screens move between questions, like stories. */
function Swipe({ children, onNext, onBack }: { children: ReactNode; onNext?: () => void; onBack?: () => void }) {
  const start = useRef<{ x: number; y: number } | null>(null)
  const down = (e: PointerEvent) => {
    start.current = e.pointerType === 'touch' ? { x: e.clientX, y: e.clientY } : null
  }
  const up = (e: PointerEvent) => {
    if (!start.current) return
    const dx = e.clientX - start.current.x
    const dy = e.clientY - start.current.y
    start.current = null
    if (Math.abs(dx) < SWIPE_PX || Math.abs(dy) > Math.abs(dx)) return
    if (dx < 0) onNext?.()
    else onBack?.()
  }
  return (
    <div className="swipe" onPointerDown={down} onPointerUp={up} onPointerCancel={() => (start.current = null)}>
      {children}
    </div>
  )
}

function SingleChoice({ question, value, onPick }: { question: SingleQuestion; value: string | number | null; onPick: (v: string | number) => void }) {
  const name = useId()
  return (
    <div className="choices" role="radiogroup" aria-labelledby="story-q">
      {question.options.map((o) => (
        <label key={o.value} className="choice">
          <input type="radio" name={name} checked={value === o.value} onChange={() => onPick(o.value)} />
          <span className="choice-text">
            <span className="choice-label">{o.label}</span>
            {o.hint && <span className="choice-hint">{o.hint}</span>}
          </span>
          <span className="choice-mark round" aria-hidden="true" />
        </label>
      ))}
    </div>
  )
}

interface MultiChoiceProps {
  question: MultiQuestion
  values: readonly (string | number)[]
  noneChosen: boolean
  onToggle: (v: string) => void
  onNone: () => void
}

function MultiChoice({ question, values, noneChosen, onToggle, onNone }: MultiChoiceProps) {
  return (
    <div className="choices" role="group" aria-labelledby="story-q">
      {question.options.map((o) => (
        <label key={o.value} className="choice">
          <input type="checkbox" checked={values.includes(o.value)} onChange={() => onToggle(String(o.value))} />
          <span className="choice-text">
            <span className="choice-label">{o.label}</span>
          </span>
          <span className="choice-mark" aria-hidden="true" />
        </label>
      ))}
      {question.noneLabel && (
        <label className="choice">
          <input type="checkbox" checked={noneChosen} onChange={onNone} />
          <span className="choice-text">
            <span className="choice-label">{question.noneLabel}</span>
          </span>
          <span className="choice-mark" aria-hidden="true" />
        </label>
      )}
    </div>
  )
}

interface NumberFieldProps {
  question: NumberQuestion
  value: number | null
  invalid: boolean
  onChange: (v: number | null) => void
}

/** Big number with − and + buttons. The text is kept as typed so "7" can become "72". */
function NumberField({ question, value, invalid, onChange }: NumberFieldProps) {
  const [text, setText] = useState(value === null ? '' : String(value))
  const id = useId()

  const type = (raw: string) => {
    setText(raw)
    const parsed = Number.parseFloat(raw.replace(',', '.'))
    onChange(Number.isFinite(parsed) ? parsed : null)
  }
  const nudge = (dir: 1 | -1) => {
    const base = value ?? (question.unit === 'cm' ? 170 : 70)
    const next = Math.min(question.max, Math.max(question.min, Math.round((base + dir * question.step) * 10) / 10))
    setText(String(next))
    onChange(next)
  }

  return (
    <div className="num-field">
      <button type="button" className="num-step" onClick={() => nudge(-1)} aria-label={`Decrease by ${question.step} ${question.unit}`}>−</button>
      <label className="num-wrap" htmlFor={id}>
        <span className="sr-only">{question.title}</span>
        <input
          id={id}
          className="num-input"
          inputMode="decimal"
          autoComplete="off"
          value={text}
          placeholder={question.unit === 'cm' ? '170' : '70'}
          onChange={(e) => type(e.target.value)}
          aria-invalid={invalid}
          aria-describedby={invalid ? 'story-err' : undefined}
          autoFocus
        />
        <span className="num-unit">{question.unit}</span>
      </label>
      <button type="button" className="num-step" onClick={() => nudge(1)} aria-label={`Increase by ${question.step} ${question.unit}`}>+</button>
    </div>
  )
}

function DateField({ value, invalid, onChange }: { value: string | null; invalid: boolean; onChange: (v: string | null) => void }) {
  // Latest date of birth that makes someone old enough today.
  const now = new Date()
  const latest = new Date(now.getFullYear() - MINIMUM_AGE, now.getMonth(), now.getDate())
  const max = toIsoDate(latest)
  return (
    <div className="field date-field">
      <label htmlFor="dob" className="sr-only">Date of birth</label>
      <input
        id="dob"
        type="date"
        value={value ?? ''}
        max={max}
        min="1900-01-01"
        onChange={(e) => onChange(e.target.value || null)}
        aria-invalid={invalid}
        aria-describedby={invalid ? 'story-err' : undefined}
      />
    </div>
  )
}

function toIsoDate(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

const kg = (n: number) => `${Math.round(n)} kg`

/** Gentle, informative notes under the body questions. BMI is a guide, so nothing here blocks. */
function Feedback({ question, answers }: { question: Question; answers: IOnboardingAnswers }) {
  const { heightCm, currentWeightKg, goalWeightKg, mainGoal } = answers
  const range = `${HEALTHY_BMI_MIN}–${HEALTHY_BMI_MAX}`

  if (question.key === 'heightCm') {
    return <Note tone="info" title="Why we ask">Your height and weight give your BMI, which we use to set safe daily targets.</Note>
  }

  if (question.key === 'currentWeightKg' && heightCm && currentWeightKg) {
    const value = bmi(currentWeightKg, heightCm)
    const band = bmiBand(value)
    const shown = value.toFixed(1)
    if (band === 'healthy') {
      return <Note tone="good" title={`Your BMI is ${shown}`}>That’s in the healthy range ({range}). We’ll use it to tailor your plan.</Note>
    }
    return (
      <Note tone="info" title={`Your BMI is ${shown}`}>
        That’s {band === 'under' ? 'below' : 'above'} the healthy range ({range}). We’ll use it to pace your plan safely.
      </Note>
    )
  }

  if (question.key === 'goalWeightKg' && heightCm && goalWeightKg) {
    const value = bmi(goalWeightKg, heightCm)
    const band = bmiBand(value)
    const [low, high] = healthyWeightRange(heightCm)
    const healthy = `${kg(low)}–${kg(high)}`
    if (mainGoal === 'LoseWeight' && currentWeightKg && goalWeightKg > currentWeightKg) {
      return <Note tone="warn" title="That’s more than you weigh now">Did you mean to choose “Build muscle” as your main goal? You can go back and change it.</Note>
    }
    if (band === 'under') {
      return (
        <Note tone="warn" title={`A goal BMI of ${value.toFixed(1)} is below the healthy range`}>
          For your height, a healthy weight is about {healthy}. Speak to a doctor before aiming lower.
        </Note>
      )
    }
    if (band === 'over') {
      return (
        <Note tone="info" title={`A goal BMI of ${value.toFixed(1)}`}>
          That’s a good first step. For your height, the healthy range is about {healthy}.
        </Note>
      )
    }
    return <Note tone="good" title={`A goal BMI of ${value.toFixed(1)}`}>That’s in the healthy range for your height.</Note>
  }

  return null
}

function Note({ tone, title, children }: { tone: 'good' | 'warn' | 'info'; title: string; children: ReactNode }) {
  return (
    <div className={`note ${tone}`} aria-live="polite">
      <p className="note-title">{title}</p>
      <p>{children}</p>
    </div>
  )
}

interface ConsentScreenProps {
  busy: boolean
  error: string | null
  onAgree: () => void
  onNotNow: () => void
}

function ConsentScreen({ busy, error, onAgree, onNotNow }: ConsentScreenProps) {
  return (
    <Shell>
      <section className="consent" aria-labelledby="consent-h">
        <p className="consent-kicker">Before we start</p>
        <h1 id="consent-h">Let’s personalise your plan</h1>
        <p className="consent-lead">
          To tailor your workouts and meals, we’ll ask about your body, health and habits. Some of this is health
          information, which South Africa’s Protection of Personal Information Act (POPIA) protects specially, so we need
          your permission first.
        </p>

        <dl className="consent-points">
          {CONSENT_POINTS.map((p) => (
            <div key={p.heading}>
              <dt>{p.heading}</dt>
              <dd>{p.body}</dd>
            </div>
          ))}
        </dl>

        <p className="consent-small">
          {RESPONSIBLE_PARTY} is responsible for your information. Questions or requests: {CONTACT_EMAIL}. Read our
          privacy policy at {PRIVACY_POLICY_URL}. You can contact the Information Regulator at{' '}
          <a className="link" href={INFORMATION_REGULATOR_URL} target="_blank" rel="noreferrer">inforegulator.org.za</a>.
        </p>

        {error && <p className="field-error" role="alert">{error}</p>}

        <div className="consent-actions">
          <button type="button" className="btn btn-primary btn-block" onClick={onAgree} disabled={busy}>
            {busy ? 'Saving…' : 'I agree'}
          </button>
          <button type="button" className="btn btn-outline btn-block" onClick={onNotNow} disabled={busy}>
            Not right now
          </button>
        </div>
      </section>
    </Shell>
  )
}

/** Where the plan is heading, from the user's own numbers only. */
function Summary({ answers }: { answers: IOnboardingAnswers }) {
  const now = answers.currentWeightKg ?? 0
  const goal = answers.goalWeightKg ?? now
  const diff = goal - now
  const steady = Math.abs(diff) < 0.5
  const pace = answers.pace ?? 'Moderate'
  const weeks = steady ? 0 : Math.ceil(Math.abs(diff) / weeklyChangeKg(diff < 0 ? 'lose' : 'gain', pace))
  const fmtKg = (n: number) => `${Number.isInteger(n) ? n : n.toFixed(1)} kg`

  return (
    <Shell>
      <section className="summary" aria-labelledby="summary-h">
        <p className="consent-kicker">You’re all set</p>
        <h1 id="summary-h">
          {steady ? `Let’s keep you strong around ${fmtKg(goal)}` : `Let’s get you to ${fmtKg(goal)}`}
        </h1>
        <p className="consent-lead">
          {steady
            ? 'Your plan focuses on staying consistent and building fitness.'
            : `At a ${pace === 'Slow' ? 'slow and steady' : pace === 'Fast' ? 'faster' : 'moderate'} pace, that’s roughly ${weeks} weeks.`}
        </p>

        <GoalChart now={now} goal={goal} steady={steady} />

        <p className="consent-small">
          This is an estimate from your answers, not a promise. Everyone changes at a different speed, so check with a
          doctor before making big changes.
        </p>

        <button type="button" className="btn btn-primary btn-block" onClick={() => (window.location.hash = TOUR_ROUTE)}>
          Show me around
        </button>
      </section>
    </Shell>
  )
}

function GoalChart({ now, goal, steady }: { now: number; goal: number; steady: boolean }) {
  // 320×160 drawing; the line eases from today's weight to the goal.
  const top = 28
  const bottom = 132
  const yNow = steady ? 80 : goal < now ? top : bottom
  const yGoal = steady ? 80 : goal < now ? bottom : top
  const path = `M16 ${yNow} C 130 ${yNow}, 190 ${yGoal}, 304 ${yGoal}`
  return (
    <figure className="goal-chart">
      <svg viewBox="0 0 320 160" role="img" aria-label={`From ${Math.round(now)} kilograms now to ${Math.round(goal)} kilograms at your goal`}>
        <line x1="16" x2="304" y1="148" y2="148" className="goal-axis" />
        <path d={`${path} L304 148 L16 148 Z`} className="goal-area" />
        <path d={path} className="goal-line" />
        <circle cx="16" cy={yNow} r="6" className="goal-dot now" />
        <circle cx="304" cy={yGoal} r="7" className="goal-dot" />
      </svg>
      <figcaption>
        <span><b>{Math.round(now)} kg</b> now</span>
        <span><b>{Math.round(goal)} kg</b> goal</span>
      </figcaption>
    </figure>
  )
}
