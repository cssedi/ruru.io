import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Logo, ThemeToggle } from '../components'
import { Walkthrough, type WalkthroughStep } from '../walkthrough'
import { clearToken } from '../services/session'
import { uid } from '../data'
import { MealsTab } from './tracker/MealsTab'
import { WorkoutTab } from './tracker/WorkoutTab'
import type { Notify } from './tracker/shared'

export type TrackerTab = 'workout' | 'meals'

interface Toast {
  id: string
  msg: string
  undo?: () => void
}

const today = new Intl.DateTimeFormat('en-ZA', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date())

const WORKOUT_TOUR: WalkthroughStep[] = [
  {
    target: '.workout-panel .panel-head',
    tab: 'workout',
    title: 'Log your workout',
    body: 'Name your session, add lifts, then tick off each set as you finish it. Volume and sets done add up here.',
  },
]

const MEALS_TOUR: WalkthroughStep[] = [
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

const TABS_STEP: WalkthroughStep = {
  target: '.tabs',
  title: 'Two tabs, one day',
  body: 'Workout and Food each have their own tab. Switch any time; nothing you’ve typed is lost.',
}

interface TrackerProps {
  tab: TrackerTab
  /** When set, the one-time walkthrough runs, starting with this tab (the user's onboarding choice). */
  tour?: TrackerTab | null
}

/* The signed-in home: a top bar, the two tabs, toasts and the walkthrough. Each tab is its own component. */
export default function Tracker({ tab, tour = null }: TrackerProps) {
  const [toast, setToast] = useState<Toast | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const notify = useCallback<Notify>((msg, undo) => {
    clearTimeout(timer.current)
    setToast({ msg, undo, id: uid() })
    timer.current = setTimeout(() => setToast(null), 6000)
  }, [])
  useEffect(() => () => clearTimeout(timer.current), [])

  const tourSteps = useMemo(
    () => (tour === 'meals' ? [...MEALS_TOUR, ...WORKOUT_TOUR, TABS_STEP] : [...WORKOUT_TOUR, ...MEALS_TOUR, TABS_STEP]),
    [tour],
  )
  // The walkthrough changes the tab through the URL, like the tab links do.
  const showTab = useCallback(
    (next: TrackerTab) => {
      if (next !== tab) window.location.hash = `#/app?tab=${next}&tour=${tour}`
    },
    [tab, tour],
  )
  const endTour = useCallback(() => window.location.replace(`#/app?tab=${tour ?? tab}`), [tab, tour])

  return (
    <div className="page tracker">
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

      <main className="tracker-main">
        <WorkoutTab active={tab === 'workout'} notify={notify} />
        <MealsTab active={tab === 'meals'} notify={notify} />
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

      {tour && <Walkthrough steps={tourSteps} activeTab={tab} onTab={showTab} onDone={endTour} />}
    </div>
  )
}
