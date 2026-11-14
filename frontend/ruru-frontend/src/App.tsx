import { useEffect, useState } from 'react'
import Landing from './pages/Landing'
import Auth from './pages/Auth'
import Tracker from './pages/Tracker'
import Onboarding from './pages/Onboarding'
import { getToken } from './services/session'

interface Route {
  path: string
  query: URLSearchParams
}

// Tiny hash router: #/ , #/login , #/signup , #/welcome?edit=sleep&return=… , #/app?tab=meals&tour=meals , #/app?plan=1
function parse(): Route {
  const raw = window.location.hash.replace(/^#/, '') || '/'
  const [path, qs = ''] = raw.split('?')
  return { path, query: new URLSearchParams(qs) }
}

const TITLES: Record<string, string> = {
  '/login': 'Log in',
  '/signup': 'Create account',
  '/welcome': 'Set up your plan',
  '/app': 'Today',
}

/** `tour=workout` or `tour=meals` runs the walkthrough starting on that tab (`tour=1` is the old form). */
function tourStart(value: string | null): 'workout' | 'meals' | null {
  if (value === 'meals') return 'meals'
  return value === 'workout' || value === '1' ? 'workout' : null
}

/** Only allow returning into the app itself, never to another site. */
function safeReturn(value: string | null): string {
  return value && value.startsWith('#/app') ? value : '#/app'
}

function Redirect({ to }: { to: string }) {
  useEffect(() => window.location.replace(to), [to])
  return null
}

export default function App() {
  const [route, setRoute] = useState<Route>(parse)

  useEffect(() => {
    const onChange = () => {
      setRoute(parse())
      window.scrollTo(0, 0)
    }
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  useEffect(() => {
    const title = TITLES[route.path]
    document.title = title ? `${title} | ruru` : 'ruru | Train and eat on one screen'
  }, [route.path])

  switch (route.path) {
    case '/login':
      return <Auth key="login" mode="login" />
    case '/signup':
      return <Auth key="signup" mode="signup" />
    case '/welcome':
      // Onboarding saves to the signed-in user's account, so it needs a token.
      return getToken() ? (
        <Onboarding edit={route.query.get('edit')} returnTo={safeReturn(route.query.get('return'))} />
      ) : (
        <Redirect to="#/login" />
      )
    case '/app':
      return (
        <Tracker
          tab={route.query.get('tab') === 'meals' ? 'meals' : 'workout'}
          tour={tourStart(route.query.get('tour'))}
          planOpen={route.query.get('plan') === '1'}
        />
      )
    default:
      return <Landing />
  }
}
