import { useEffect, useRef, useState } from 'react'

export interface WalkthroughStep {
  /** CSS selector for the element to spotlight. */
  target: string
  title: string
  body: string
  /** On narrow screens only one tracker tab shows at a time; switch to this one first. */
  tab?: 'workout' | 'meals'
}

interface WalkthroughProps {
  steps: WalkthroughStep[]
  /** Called with a step's tab so the page can show it before the step is measured. */
  onTab: (tab: 'workout' | 'meals') => void
  onDone: () => void
}

const PAD = 8

/*
  A short, one-time tour after onboarding: a spotlight ring around one part of the
  page and a card at the bottom explaining it. Escape or "Skip tour" ends it.
*/
export function Walkthrough({ steps, onTab, onDone }: WalkthroughProps) {
  const [index, setIndex] = useState(0)
  const [rect, setRect] = useState<DOMRect | null>(null)
  const nextRef = useRef<HTMLButtonElement>(null)
  const step = steps[index]
  const last = index === steps.length - 1

  useEffect(() => {
    if (step.tab) onTab(step.tab)
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
    let target: Element | null = null
    // A hidden element (e.g. the other tab on a phone) measures 0×0: dim the page instead.
    const measure = () => {
      const r = target?.getBoundingClientRect()
      setRect(r && r.width > 0 ? r : null)
    }
    // Wait a frame so a tab switch has rendered before looking for the element.
    const frame = requestAnimationFrame(() => {
      target = document.querySelector(step.target)
      target?.scrollIntoView({ block: 'center', behavior: reduceMotion ? 'auto' : 'smooth' })
      measure()
      nextRef.current?.focus()
    })
    window.addEventListener('resize', measure)
    window.addEventListener('scroll', measure, true)
    return () => {
      cancelAnimationFrame(frame)
      window.removeEventListener('resize', measure)
      window.removeEventListener('scroll', measure, true)
    }
  }, [step, onTab])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onDone()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onDone])

  return (
    <div className="tour" role="dialog" aria-modal="true" aria-labelledby="tour-title" aria-describedby="tour-body">
      {rect ? (
        <div
          className="tour-spot"
          style={{
            top: rect.top - PAD,
            left: rect.left - PAD,
            width: rect.width + PAD * 2,
            height: rect.height + PAD * 2,
          }}
        />
      ) : (
        <div className="tour-dim" />
      )}
      <div className="tour-card">
        <p className="tour-count">{index + 1} of {steps.length}</p>
        <h2 id="tour-title">{step.title}</h2>
        <p id="tour-body">{step.body}</p>
        <div className="tour-actions">
          <button type="button" className="link" onClick={onDone}>Skip tour</button>
          <button
            ref={nextRef}
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => (last ? onDone() : setIndex(index + 1))}
          >
            {last ? 'Start logging' : 'Next'}
          </button>
        </div>
      </div>
    </div>
  )
}
