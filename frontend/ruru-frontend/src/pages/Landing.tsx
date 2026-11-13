import { Logo, ThemeToggle, BeadString, SteppedBand } from '../components'
import { macrosFor, sumMacros, fmt, TARGETS } from '../data'

const lunch = [
  { id: 'pap', name: 'Pap', g: 300 },
  { id: 'chakalaka', name: 'Chakalaka', g: 100 },
  { id: 'chicken', name: 'Chicken breast', g: 150 },
].map((x) => ({ ...x, m: macrosFor(x.id, x.g) }))
const lunchTotal = sumMacros(lunch.map((x) => x.m))

const sets = [
  { kg: 60, reps: 8, done: true },
  { kg: 70, reps: 6, done: true },
  { kg: 75, reps: 5, done: false },
]

const localFoods: string[] = ['Pap', 'Morogo', 'Amasi', 'Chakalaka', 'Mogodu', 'Boerewors', 'Biltong', 'Sugar beans', 'Vetkoek']

export default function Landing() {
  return (
    <div className="page landing">
      <a className="skip" href="#main-hero" onClick={(e) => { e.preventDefault(); document.getElementById('main-hero')?.focus() }}>
        Skip to content
      </a>
      <header className="topbar">
        <Logo />
        <nav className="topbar-actions" aria-label="Account">
          <a className="link" href="#/login">Log in</a>
          <a className="btn btn-primary btn-sm" href="#/signup">Create account</a>
          <ThemeToggle />
        </nav>
      </header>

      <main>
        <section className="hero" id="main-hero" tabIndex={-1} aria-labelledby="hero-title">
          <div className="hero-copy">
            <h1 id="hero-title" className="hero-title">
              <span className="wide">Lift on one side.</span>
              <span className="narrow">Eat on the other.</span>
            </h1>
            <p className="lede">
              ruru keeps your training log and your food log side by side, so the
              plate always answers to the bar.
            </p>
            <div className="hero-ctas">
              <a className="btn btn-primary" href="#/signup">Create account</a>
              <a className="btn btn-ghost" href="#/app">Try the tracker</a>
            </div>
          </div>

          <div className="split-preview" aria-label="Preview of a workout and a meal">
            <div className="pane">
              <p className="pane-kicker">Push day</p>
              <h2 className="pane-title">Bench press</h2>
              <table className="sets mini">
                <thead>
                  <tr><th scope="col">Set</th><th scope="col">kg</th><th scope="col">Reps</th><th scope="col"><span className="sr-only">Done</span></th></tr>
                </thead>
                <tbody>
                  {sets.map((s, i) => (
                    <tr key={i} className={s.done ? 'done' : ''}>
                      <td>{i + 1}</td><td>{s.kg}</td><td>{s.reps}</td>
                      <td><span className={s.done ? 'tick on' : 'tick'} aria-label={s.done ? 'Done' : 'Not done'} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <p className="pane-foot">Volume <b>{fmt(sets.filter((s) => s.done).reduce((a, s) => a + s.kg * s.reps, 0))} kg</b></p>
            </div>
            <div className="pane">
              <p className="pane-kicker">Lunch</p>
              <h2 className="pane-title"><span className="kcal-num">{fmt(lunchTotal.kcal)}</span> kcal</h2>
              <ul className="food-mini">
                {lunch.map((x) => (
                  <li key={x.id}><span>{x.name}</span><span>{x.g} g</span></li>
                ))}
              </ul>
              <BeadString label="Protein" value={lunchTotal.p} target={TARGETS.p} tone="p" />
            </div>
          </div>
        </section>

        <section className="explain" aria-labelledby="explain-title">
          <h2 id="explain-title" className="section-title">Two logs, one day</h2>
          <div className="explain-cols">
            <div>
              <h3>Training</h3>
              <ul className="plain">
                <li>Log weight and reps set by set, tick them off as you go.</li>
                <li>See the total volume for the session while you train.</li>
                <li>Pick from common lifts instead of typing names.</li>
              </ul>
            </div>
            <div>
              <h3>Food</h3>
              <ul className="plain">
                <li>Enter grams. Calories and macros work themselves out.</li>
                <li>Every meal shows its own calorie total.</li>
                <li>Protein, carbs and fat are counted as strands of beads against your daily target.</li>
              </ul>
            </div>
          </div>
        </section>

        <section className="local" aria-labelledby="local-title">
          <h2 id="local-title" className="section-title">The food you actually eat is already in the list</h2>
          <ul className="chips">
            {localFoods.map((f, i) => (
              <li key={f} className={`chip tone-${(['p', 'c', 'f'] as const)[i % 3]}`}><span className="dot" aria-hidden="true" />{f}</li>
            ))}
          </ul>
        </section>

        <section className="closing">
          <h2 className="section-title">Start with today’s session</h2>
          <a className="btn btn-primary" href="#/signup">Create account</a>
        </section>
      </main>

      <footer className="footer">
        <SteppedBand />
        <div className="footer-inner">
          <Logo />
          <p>Made in Mzansi. © 2026 ruru</p>
        </div>
      </footer>
    </div>
  )
}
