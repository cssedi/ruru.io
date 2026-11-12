import { useActionState, useEffect, useRef, useState, type ReactNode } from 'react'
import { Logo, ThemeToggle, SteppedBand } from '../components'
import { RegisterAccount, SignIn } from '../services/Auth'

export type AuthMode = 'login' | 'signup'

type FieldName = 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword'
type FieldErrors = Partial<Record<FieldName, string>>

// Text fields echoed back after a failed submit so React's post-action form reset
// doesn't wipe them. Passwords are deliberately left out (they'd end up in the DOM).
type EchoedField = 'firstName' | 'lastName' | 'email' | 'goal'

interface AuthState {
  errors: FieldErrors
  formError: string | null
  values: Partial<Record<EchoedField, string>>
}

const INITIAL_STATE: AuthState = { errors: {}, formError: null, values: {} }
const GOALS = ['Build muscle', 'Lose fat', 'Maintain']

interface AuthCopy {
  greet: string
  line: string
  title: string
  submit: string
  busy: string
  swap: [question: string, linkText: string, href: string]
}

const COPY: Record<AuthMode, AuthCopy> = {
  login: {
    greet: 'Sawubona.',
    line: 'Your sets and meals are where you left them.',
    title: 'Log in',
    submit: 'Log in',
    busy: 'Logging in…',
    swap: ['New to ruru?', 'Create an account', '#/signup'],
  },
  signup: {
    greet: 'Welcome to ruru.',
    line: 'Set up takes a minute. Your first workout takes longer.',
    title: 'Create your account',
    submit: 'Create account',
    busy: 'Creating account…',
    swap: ['Already have an account?', 'Log in', '#/login'],
  },
}

const field = (data: FormData, name: FieldName | 'goal'): string => String(data.get(name) ?? '').trim()

function validate(mode: AuthMode, data: FormData): FieldErrors {
  const next: FieldErrors = {}
  if (mode === 'signup') {
    if (!field(data, 'firstName')) next.firstName = 'Enter your first name.'
    if (!field(data, 'lastName')) next.lastName = 'Enter your last name.'
  }
  if (!/^\S+@\S+\.\S+$/.test(field(data, 'email'))) next.email = 'Enter an email address like thandi@example.com.'
  if (field(data, 'password').length < 8) next.password = 'Use at least 8 characters.'
  if (mode === 'signup' && field(data, 'confirmPassword') !== field(data, 'password')) {
    next.confirmPassword = 'Passwords don’t match. Type the same password twice.'
  }
  return next
}

async function authAction(mode: AuthMode, _prev: AuthState, data: FormData): Promise<AuthState> {
  const values: AuthState['values'] = {
    firstName: field(data, 'firstName'),
    lastName: field(data, 'lastName'),
    email: field(data, 'email'),
    goal: field(data, 'goal'),
  }

  const errors = validate(mode, data)
  if (Object.keys(errors).length > 0) return { errors, formError: null, values }

  const email = field(data, 'email')
  const password = String(data.get('password') ?? '')

  try {
    if (mode === 'login') {
      const resp = await SignIn({ email, password })
      if (!resp.token) return { errors: {}, formError: resp.message || 'Email or password is incorrect.', values }
    } else {
      await RegisterAccount({
        firstName: field(data, 'firstName'),
        lastName: field(data, 'lastName'),
        email,
        password,
        confirmPassword: String(data.get('confirmPassword') ?? ''),
      })
    }
  } catch (err) {
    return { errors: {}, formError: err instanceof Error ? err.message : 'Something went wrong. Try again.', values }
  }

  window.location.hash = '#/app'
  return INITIAL_STATE
}

export default function Auth({ mode }: { mode: AuthMode }) {
  const c = COPY[mode]
  const [showPw, setShowPw] = useState(false)
  const [{ errors, formError, values }, formAction, busy] = useActionState(
    (prev: AuthState, data: FormData) => authAction(mode, prev, data),
    INITIAL_STATE,
  )

  // Move focus to the first invalid field after each failed submit.
  const formRef = useRef<HTMLFormElement>(null)
  useEffect(() => {
    const first = (Object.keys(errors) as FieldName[])[0]
    const el = first && formRef.current?.elements.namedItem(first)
    if (el instanceof HTMLElement) el.focus()
  }, [errors])

  const err = (k: FieldName): ReactNode =>
    errors[k] ? <p className="field-error" id={`${k}-err`}>{errors[k]}</p> : null
  const described = (k: FieldName): string | undefined => (errors[k] ? `${k}-err` : undefined)

  return (
    <div className="page auth">
      <aside className="auth-panel shweshwe">
        <Logo />
        <div className="auth-greet">
          <p className="greet">{c.greet}</p>
          <p>{c.line}</p>
        </div>
        <SteppedBand />
      </aside>

      <main className="auth-main">
        <div className="auth-top">
          <a className="link" href="#/">Back to home</a>
          <ThemeToggle />
        </div>
        <form ref={formRef} className="auth-form" action={formAction} noValidate>
          <h1>{c.title}</h1>

          {formError && <p className="field-error" role="alert">{formError}</p>}

          {mode === 'signup' && (
            <>
              <div className="field">
                <label htmlFor="firstName">First name</label>
                <input id="firstName" name="firstName" defaultValue={values.firstName} autoComplete="given-name" placeholder="Thandi…" aria-invalid={!!errors.firstName} aria-describedby={described('firstName')} />
                {err('firstName')}
              </div>
              <div className="field">
                <label htmlFor="lastName">Last name</label>
                <input id="lastName" name="lastName" defaultValue={values.lastName} autoComplete="family-name" placeholder="Mokoena…" aria-invalid={!!errors.lastName} aria-describedby={described('lastName')} />
                {err('lastName')}
              </div>
            </>
          )}

          <div className="field">
            <label htmlFor="email">Email</label>
            <input id="email" name="email" type="email" defaultValue={values.email} autoComplete="email" spellCheck={false} inputMode="email" placeholder="thandi@example.com" aria-invalid={!!errors.email} aria-describedby={described('email')} />
            {err('email')}
          </div>

          <div className="field">
            <div className="label-row">
              <label htmlFor="password">Password</label>
              {mode === 'login' && <a className="link small" href="#/login">Forgot password?</a>}
            </div>
            <div className="pw-wrap">
              <input id="password" name="password" type={showPw ? 'text' : 'password'} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} aria-invalid={!!errors.password} aria-describedby={described('password') ?? (mode === 'signup' ? 'pw-hint' : undefined)} />
              <button type="button" className="pw-toggle" onClick={() => setShowPw((v) => !v)} aria-pressed={showPw}>
                {showPw ? 'Hide' : 'Show'}
              </button>
            </div>
            {err('password') || (mode === 'signup' && <p className="hint" id="pw-hint">At least 8 characters.</p>)}
          </div>

          {mode === 'signup' && (
            <div className="field">
              <label htmlFor="confirmPassword">Confirm password</label>
              <input id="confirmPassword" name="confirmPassword" type={showPw ? 'text' : 'password'} autoComplete="new-password" aria-invalid={!!errors.confirmPassword} aria-describedby={described('confirmPassword')} />
              {err('confirmPassword')}
            </div>
          )}

          {mode === 'signup' && (
            <fieldset className="field goal">
              <legend>Main goal</legend>
              <div className="segmented">
                {GOALS.map((g) => (
                  <label key={g}>
                    <input type="radio" name="goal" value={g} defaultChecked={g === (values.goal || GOALS[0])} />
                    <span>{g}</span>
                  </label>
                ))}
              </div>
            </fieldset>
          )}

          <button className="btn btn-primary btn-block" type="submit" disabled={busy}>
            {busy ? c.busy : c.submit}
          </button>

          <p className="swap">
            {c.swap[0]} <a className="link" href={c.swap[2]}>{c.swap[1]}</a>
          </p>
        </form>
      </main>
    </div>
  )
}
