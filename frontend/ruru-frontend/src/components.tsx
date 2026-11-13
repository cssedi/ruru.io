import { useEffect, useState, type ReactElement } from 'react'
import { fmt } from './data'

export type Theme = 'light' | 'dark'
export type Tone = 'k' | 'p' | 'c' | 'f'

/* Logo: a stepped "r" in the outlined, flat-colour geometry of Ndebele
   wall painting, with a single bead in the corner. */
export function LogoMark({ size = 32 }: { size?: number }) {
  return (
    <svg className="logo-mark" width={size} height={size} viewBox="0 0 32 32" aria-hidden="true">
      <rect x="1.5" y="1.5" width="29" height="29" rx="7" fill="var(--mark-bg)" stroke="var(--mark-line)" strokeWidth="2" />
      <path d="M8.5 25V7.5h8v4h7v5.5h-7V25z" fill="var(--yellow)" stroke="var(--mark-line)" strokeWidth="2" strokeLinejoin="round" />
      <circle cx="23" cy="23" r="2.6" fill="var(--red)" stroke="var(--mark-line)" strokeWidth="1.6" />
    </svg>
  )
}

export function Logo({ href = '#/' }: { href?: string }) {
  return (
    <a className="logo" href={href} aria-label="ruru home">
      <LogoMark />
      <span className="wordmark" translate="no">ruru</span>
    </a>
  )
}

function readTheme(): Theme {
  try {
    const saved = localStorage.getItem('ruru-theme')
    if (saved === 'light' || saved === 'dark') return saved
  } catch {
    /* storage unavailable */
  }
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(readTheme)

  useEffect(() => {
    const root = document.documentElement
    root.dataset.theme = theme
    root.style.colorScheme = theme
    const bg = getComputedStyle(root).getPropertyValue('--bg').trim()
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', bg)
    try {
      localStorage.setItem('ruru-theme', theme)
    } catch {
      /* storage unavailable */
    }
  }, [theme])

  const next: Theme = theme === 'dark' ? 'light' : 'dark'
  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={() => setTheme(next)}
      aria-label={`Switch to ${next} mode`}
      title={`Switch to ${next} mode`}
    >
      <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true">
        {theme === 'dark' ? (
          <g fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <circle cx="12" cy="12" r="4.5" />
            <path d="M12 2v2.5M12 19.5V22M2 12h2.5M19.5 12H22M4.9 4.9l1.8 1.8M17.3 17.3l1.8 1.8M4.9 19.1l1.8-1.8M17.3 6.7l1.8-1.8" />
          </g>
        ) : (
          <path d="M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 10.5z" fill="none" stroke="currentColor" strokeWidth="2" strokeLinejoin="round" />
        )}
      </svg>
    </button>
  )
}

/* A strand of beads as a progress meter. 20 beads, grouped in fives by a
   small white spacer bead, so each group is a quarter of the target. */
interface BeadStringProps {
  label: string
  value: number
  target: number
  unit?: string
  tone: Tone
  beads?: number
}

export function BeadString({ label, value, target, unit = 'g', tone, beads = 20 }: BeadStringProps) {
  const ratio = target ? value / target : 0
  const filled = Math.min(beads, Math.round(ratio * beads))
  const over = value > target
  const items: ReactElement[] = []
  for (let i = 0; i < beads; i++) {
    if (i > 0 && i % 5 === 0) items.push(<span key={`s${i}`} className="bead spacer" />)
    items.push(<span key={i} className={i < filled ? 'bead on' : 'bead'} />)
  }
  return (
    <div className={`strand tone-${tone}`}>
      <div className="strand-head">
        <span className="strand-label">{label}</span>
        <span className="strand-value">
          <b>{fmt(value)}</b> / {fmt(target)}
          {unit && ` ${unit}`}
          {over && <span className="over">, {fmt(value - target)} over</span>}
        </span>
      </div>
      <div
        className="beads"
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={target}
        aria-valuenow={Math.round(value)}
        aria-valuetext={`${fmt(value)} of ${fmt(target)} ${unit}`}
      >
        {items}
      </div>
    </div>
  )
}

/* Thin stepped band in Ndebele-style colour blocks, used as a border. */
export function SteppedBand() {
  return <div className="stepped-band" aria-hidden="true" />
}
