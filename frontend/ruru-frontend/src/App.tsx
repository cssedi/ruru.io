import { useEffect, useState } from 'react'
import Landing from './pages/Landing'
import Auth from './pages/Auth'
import Tracker from './pages/Tracker'

interface Route {
  path: string
  query: URLSearchParams
}

// Tiny hash router: #/ , #/login , #/signup , #/app?tab=meals
function parse(): Route {
  const raw = window.location.hash.replace(/^#/, '') || '/'
  const [path, qs = ''] = raw.split('?')
  return { path, query: new URLSearchParams(qs) }
}

const TITLES: Record<string, string> = { '/login': 'Log in', '/signup': 'Create account', '/app': 'Today' }

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
    case '/app':
      return <Tracker tab={route.query.get('tab') === 'meals' ? 'meals' : 'workout'} />
    default:
      return <Landing />
  }
}
