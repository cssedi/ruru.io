import { useEffect, useState } from 'react'
import type { IGeneratedPlan, IPlanDay, IPlanReadiness, IReadinessItem, ITrainingPlan } from '../../models/TrainingPlan'
import { ApiError } from '../../services/http'
import {
  DiscardPlan,
  GeneratePlan,
  GetCurrentPlan,
  GetPlanReadiness,
  GiveAiConsent,
  SavePlan,
  WithdrawAiConsent,
} from '../../services/TrainingPlan'
import { AI_CONSENT_POINTS, AI_CONSENT_VERSION } from '../../plans/aiConsent'
import type { Notify } from './shared'

type View = 'loading' | 'idle' | 'readiness' | 'consent' | 'generating' | 'draft'

/** Where a gap link sends the user back to: this panel, open on the readiness check. */
const RETURN_TO = '#/app?tab=workout&plan=1'

interface AiPlanProps {
  /** Opens straight on the readiness check, e.g. after answering a missing question. */
  open: boolean
  notify: Notify
  /** Loads one plan day into the workout log. */
  onStart: (day: IPlanDay) => void
}

/*
  The optional AI training plan, inside the Workout tab.
  Before generating, it shows what ruru already knows and what's missing, with a link to the
  onboarding question for each gap. It never asks the user anything itself. A generated plan is a
  draft until the user saves it; discarding deletes it.
*/
export function AiPlan({ open, notify, onStart }: AiPlanProps) {
  const [view, setView] = useState<View>('loading')
  const [saved, setSaved] = useState<ITrainingPlan | null>(null)
  const [draft, setDraft] = useState<ITrainingPlan | null>(null)
  const [readiness, setReadiness] = useState<IPlanReadiness | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([GetCurrentPlan(), open ? GetPlanReadiness() : Promise.resolve(null)])
      .then(([current, ready]) => {
        if (cancelled) return
        setSaved(current)
        setReadiness(ready)
        setView(open ? 'readiness' : 'idle')
      })
      .catch(() => !cancelled && setView('idle'))
    return () => {
      cancelled = true
    }
  }, [open])

  async function run(task: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await task()
    } catch (err) {
      setError(err instanceof ApiError || err instanceof Error ? err.message : 'Something went wrong. Try again.')
    } finally {
      setBusy(false)
    }
  }

  const openReadiness = () =>
    run(async () => {
      setReadiness(await GetPlanReadiness())
      setView('readiness')
    })

  async function generateDraft() {
    setView('generating')
    try {
      setDraft(await GeneratePlan())
      setView('draft')
    } catch (err) {
      setView('readiness')
      throw err
    }
  }

  const generate = () => run(generateDraft)

  const createPlan = () => (readiness?.hasAiConsent ? generate() : setView('consent'))

  const agree = () =>
    run(async () => {
      setReadiness(await GiveAiConsent(AI_CONSENT_VERSION))
      await generateDraft()
    })

  const save = () =>
    run(async () => {
      if (!draft) return
      setSaved(await SavePlan(draft.id))
      setDraft(null)
      setView('idle')
      notify('Plan saved. Start any session from it below.')
    })

  const discard = () =>
    run(async () => {
      if (!draft) return
      await DiscardPlan(draft.id)
      setDraft(null)
      setView('idle')
      notify('Plan discarded.')
    })

  const stopAi = () =>
    run(async () => {
      setReadiness(await WithdrawAiConsent())
      notify('ruru won’t send your details to the AI again unless you agree.')
    })

  const close = () => {
    setError(null)
    setView('idle')
    if (open) window.location.replace('#/app?tab=workout')
  }

  if (view === 'loading') return null

  return (
    <section className="ai-plan" aria-labelledby="ai-plan-h">
      {view === 'idle' && (
        <>
          {saved ? (
            <>
              <PlanView plan={saved.plan} headingId="ai-plan-h" onStart={onStart} />
              <div className="ai-plan-actions">
                <button type="button" className="btn btn-outline btn-sm" onClick={openReadiness} disabled={busy}>New plan with AI</button>
                <button type="button" className="link small" onClick={stopAi} disabled={busy}>Stop using AI</button>
              </div>
            </>
          ) : (
            <div className="ai-cta">
              <div>
                <h2 id="ai-plan-h">Get a training plan made for you</h2>
                <p className="muted">Optional. ruru uses your setup answers to plan your weeks with AI.</p>
              </div>
              <button type="button" className="btn btn-primary btn-sm" onClick={openReadiness} disabled={busy}>Plan with AI</button>
            </div>
          )}
        </>
      )}

      {view === 'readiness' && readiness && (
        <Readiness readiness={readiness} busy={busy} onCreate={createPlan} onCancel={close} />
      )}

      {view === 'consent' && (
        <div className="ai-consent">
          <h2 id="ai-plan-h">Before we create your plan</h2>
          <p className="muted">To write your plan, ruru sends some of your details to an AI service. Please read this first.</p>
          <dl className="consent-points">
            {AI_CONSENT_POINTS.map((p) => (
              <div key={p.heading}>
                <dt>{p.heading}</dt>
                <dd>{p.body}</dd>
              </div>
            ))}
          </dl>
          <div className="ai-plan-actions">
            <button type="button" className="btn btn-primary" onClick={agree} disabled={busy}>I agree, create my plan</button>
            <button type="button" className="btn btn-ghost" onClick={() => setView('readiness')} disabled={busy}>Cancel</button>
          </div>
        </div>
      )}

      {view === 'generating' && (
        <div className="ai-generating" role="status">
          <span className="spinner" aria-hidden="true" />
          <div>
            <h2 id="ai-plan-h">Writing your plan…</h2>
            <p className="muted">This usually takes under a minute. Keep this page open.</p>
          </div>
        </div>
      )}

      {view === 'draft' && draft && (
        <>
          <p className="draft-flag">Draft: save it to keep it, or discard it.</p>
          <PlanView plan={draft.plan} headingId="ai-plan-h" />
          <div className="ai-plan-actions">
            <button type="button" className="btn btn-primary" onClick={save} disabled={busy}>
              {saved ? 'Save and replace my current plan' : 'Save plan'}
            </button>
            <button type="button" className="btn btn-outline" onClick={discard} disabled={busy}>Discard</button>
          </div>
        </>
      )}

      {error && <p className="field-error" role="alert">{error}</p>}
    </section>
  )
}

interface ReadinessProps {
  readiness: IPlanReadiness
  busy: boolean
  onCreate: () => void
  onCancel: () => void
}

/** What ruru knows and what's missing. Each gap links to its onboarding question; nothing is asked here. */
function Readiness({ readiness, busy, onCreate, onCancel }: ReadinessProps) {
  const known = readiness.items.filter((i) => i.value !== null)
  const missing = readiness.items.filter((i) => i.value === null)
  const missingNeeded = missing.filter((i) => i.needed)

  return (
    <div className="readiness">
      <h2 id="ai-plan-h">Here’s what your plan will be based on</h2>
      <p className="muted">
        The more ruru knows, the better the plan fits. Anything missing can be answered from the links below.
      </p>

      {known.length > 0 && (
        <>
          <h3 className="readiness-h">What ruru knows</h3>
          <dl className="readiness-list">
            {known.map((i) => (
              <div key={i.key}>
                <dt>{i.label}</dt>
                <dd>
                  {i.value}
                  <a className="link small" href={questionLink(i)}>Change</a>
                </dd>
              </div>
            ))}
          </dl>
        </>
      )}

      {missing.length > 0 && (
        <>
          <h3 className="readiness-h">Not answered yet</h3>
          {!readiness.hasProfileConsent && (
            <p className="hint">You’ll be asked to agree to the health-information notice before your first answer is saved.</p>
          )}
          <ul className="readiness-gaps">
            {missing.map((i) => (
              <li key={i.key}>
                <span>
                  {i.label}
                  {i.needed && <span className="tag">Needed</span>}
                </span>
                <a className="link small" href={questionLink(i)}>Answer</a>
              </li>
            ))}
          </ul>
        </>
      )}

      {missingNeeded.length > 0 && (
        <p className="hint">Answer the questions marked “Needed” to create your plan.</p>
      )}

      <div className="ai-plan-actions">
        <button type="button" className="btn btn-primary" onClick={onCreate} disabled={busy || !readiness.canGenerate}>
          {missing.length > 0 && readiness.canGenerate ? 'Create my plan anyway' : 'Create my plan'}
        </button>
        <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>Not now</button>
      </div>
    </div>
  )
}

function questionLink(item: IReadinessItem): string {
  return `#/welcome?edit=${encodeURIComponent(item.key)}&return=${encodeURIComponent(RETURN_TO)}`
}

interface PlanViewProps {
  plan: IGeneratedPlan
  headingId: string
  onStart?: (day: IPlanDay) => void
}

function PlanView({ plan, headingId, onStart }: PlanViewProps) {
  return (
    <article className="plan">
      <header>
        <p className="plan-kicker">Your plan · {plan.durationWeeks} weeks · {plan.split}</p>
        <h2 id={headingId}>{plan.title}</h2>
        <p>{plan.summary}</p>
      </header>

      {plan.safetyNotes.length > 0 && (
        <div className="note warn">
          <p className="note-title">Take care</p>
          <ul>{plan.safetyNotes.map((n) => <li key={n}>{n}</li>)}</ul>
        </div>
      )}

      {plan.phases.length > 0 && (
        <ol className="plan-phases" aria-label="Phases">
          {plan.phases.map((p) => (
            <li key={p.name + p.weeks}>
              <b>{p.name}</b> <span className="muted">weeks {p.weeks}</span>
              <span>{p.focus}{p.intensity ? ` · ${p.intensity}` : ''}</span>
            </li>
          ))}
        </ol>
      )}

      {plan.days.map((day) => (
        <section className="plan-day" key={day.day} aria-label={`${day.day}: ${day.name}`}>
          <header className="plan-day-head">
            <div>
              <h3>{day.day} · {day.name}</h3>
              {day.focus && <p className="muted">{day.focus}</p>}
            </div>
            {onStart && (
              <button type="button" className="btn btn-outline btn-sm" onClick={() => onStart(day)}>Start session</button>
            )}
          </header>
          <ol className="plan-exercises">
            {day.exercises.map((e, i) => (
              <li key={e.exerciseId + i}>
                <div className="plan-ex-main">
                  <b>{e.name}</b>
                  <span>{e.sets} × {e.reps}{e.rest ? ` · rest ${e.rest}` : ''}{e.intensity ? ` · ${e.intensity}` : ''}</span>
                </div>
                {(e.notes || e.easierOption) && (
                  <details>
                    <summary>Form and easier option</summary>
                    {e.notes && <p>{e.notes}</p>}
                    {e.easierOption && <p><b>Easier:</b> {e.easierOption}</p>}
                  </details>
                )}
              </li>
            ))}
          </ol>
        </section>
      ))}

      {(plan.warmUp.length > 0 || plan.coolDown.length > 0 || plan.progression) && (
        <details className="plan-extra">
          <summary>Warm-up, cool-down and progression</summary>
          {plan.warmUp.length > 0 && (<><h4>Warm-up</h4><ul>{plan.warmUp.map((w) => <li key={w}>{w}</li>)}</ul></>)}
          {plan.coolDown.length > 0 && (<><h4>Cool-down</h4><ul>{plan.coolDown.map((c) => <li key={c}>{c}</li>)}</ul></>)}
          {plan.progression && (<><h4>Progression</h4><p>{plan.progression}</p></>)}
        </details>
      )}

      <p className="consent-small">
        Written by AI from your answers. It’s a starting point, not medical advice: stop if anything hurts, and check
        with a doctor or physiotherapist about any health condition.
      </p>
    </article>
  )
}
