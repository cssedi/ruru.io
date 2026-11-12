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

// Tiny hash router: #/ , #/login , #/signup , #/welcome , #/app?tab=meals&tour=1
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
      return getToken() ? <Onboarding /> : <Redirect to="#/login" />
    case '/app':
      return (
        <Tracker
          tab={route.query.get('tab') === 'meals' ? 'meals' : 'workout'}
          tour={route.query.get('tour') === '1'}
        />
      )
    default:
      return <Landing />
  }
}
