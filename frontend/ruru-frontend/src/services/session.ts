// The signed-in user's API token. Kept in localStorage so a refresh doesn't sign them out.
const KEY = 'ruru-token'

export function getToken(): string | null {
  try {
    return localStorage.getItem(KEY)
  } catch {
    return null
  }
}

export function setToken(token: string): void {
  try {
    localStorage.setItem(KEY, token)
  } catch {
    /* storage unavailable: the user stays signed in until the page is closed */
  }
}

export function clearToken(): void {
  try {
    localStorage.removeItem(KEY)
  } catch {
    /* storage unavailable */
  }
}
