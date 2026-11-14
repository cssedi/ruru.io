import { useState } from 'react'
import { EXERCISES, findExercise, fmt, uid } from '../../data'
import type { Workout, WorkoutExercise, WorkoutSet } from '../../models/Workout'
import { Picker } from './Picker'
import type { TabProps } from './shared'

const initialWorkout: Workout = {
  name: 'Push day',
  exercises: [
    {
      key: uid(),
      exId: 'bench',
      sets: [
        { key: uid(), kg: 60, reps: 8, done: true },
        { key: uid(), kg: 70, reps: 6, done: false },
        { key: uid(), kg: 70, reps: 6, done: false },
      ],
    },
    { key: uid(), exId: 'ohp', sets: [{ key: uid(), kg: 40, reps: 8, done: false }] },
  ],
}

/** The Workout tab: logs one training session. Owns its own state. */
export function WorkoutTab({ active, notify }: TabProps) {
  const [workout, setWorkout] = useState<Workout>(initialWorkout)
  const [picking, setPicking] = useState(false)
  const done = workout.exercises.flatMap((e) => e.sets).filter((s) => s.done)
  const volume = done.reduce((a, s) => a + (Number(s.kg) || 0) * (Number(s.reps) || 0), 0)

  const updateEx = (key: string, fn: (e: WorkoutExercise) => WorkoutExercise) =>
    setWorkout((w) => ({ ...w, exercises: w.exercises.map((e) => (e.key === key ? fn(e) : e)) }))
  const updateSet = (exKey: string, setKey: string, patch: Partial<WorkoutSet>) =>
    updateEx(exKey, (e) => ({ ...e, sets: e.sets.map((s) => (s.key === setKey ? { ...s, ...patch } : s)) }))

  function removeExercise(ex: WorkoutExercise) {
    const prev = workout
    setWorkout((w) => ({ ...w, exercises: w.exercises.filter((e) => e.key !== ex.key) }))
    notify(`Removed ${findExercise(ex.exId).name}.`, () => setWorkout(prev))
  }

  return (
    <section className="panel workout-panel" aria-labelledby="workout-h" hidden={!active}>
      <div className="panel-head">
        <h2 id="workout-h" className="panel-title">Workout</h2>
        <label className="sr-only" htmlFor="wname">Workout name</label>
        <input
          id="wname"
          className="title-input"
          value={workout.name}
          onChange={(e) => setWorkout((w) => ({ ...w, name: e.target.value }))}
          autoComplete="off"
        />
        <dl className="stats">
          <div><dt>Volume</dt><dd>{fmt(volume)} kg</dd></div>
          <div><dt>Sets done</dt><dd>{done.length}</dd></div>
          <div><dt>Lifts</dt><dd>{workout.exercises.length}</dd></div>
        </dl>
      </div>

      {workout.exercises.length === 0 && !picking && (
        <p className="empty">No lifts yet. Add your first one to start logging sets.</p>
      )}

      {workout.exercises.map((ex) => {
        const meta = findExercise(ex.exId)
        return (
          <article className="block" key={ex.key}>
            <header className="block-head">
              <div>
                <h3>{meta.name}</h3>
                <p className="muted">{meta.group}</p>
              </div>
              <button type="button" className="icon-btn" onClick={() => removeExercise(ex)} aria-label={`Remove ${meta.name}`}>
                <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
              </button>
            </header>
            <table className="sets">
              <thead>
                <tr>
                  <th scope="col">Set</th>
                  <th scope="col">kg</th>
                  <th scope="col">Reps</th>
                  <th scope="col"><span className="sr-only">Done</span></th>
                </tr>
              </thead>
              <tbody>
                {ex.sets.map((s, i) => (
                  <tr key={s.key} className={s.done ? 'done' : ''}>
                    <td>{i + 1}</td>
                    <td>
                      <input type="number" inputMode="decimal" min="0" step="2.5" value={s.kg} aria-label={`Set ${i + 1} weight in kg`} onChange={(e) => updateSet(ex.key, s.key, { kg: e.target.value })} />
                    </td>
                    <td>
                      <input type="number" inputMode="numeric" min="0" value={s.reps} aria-label={`Set ${i + 1} reps`} onChange={(e) => updateSet(ex.key, s.key, { reps: e.target.value })} />
                    </td>
                    <td>
                      <label className="check">
                        <input type="checkbox" checked={s.done} onChange={(e) => updateSet(ex.key, s.key, { done: e.target.checked })} />
                        <span className="tick" aria-hidden="true" />
                        <span className="sr-only">Mark set {i + 1} done</span>
                      </label>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() =>
                updateEx(ex.key, (e) => {
                  const last: Pick<WorkoutSet, 'kg' | 'reps'> = e.sets[e.sets.length - 1] ?? { kg: 20, reps: 8 }
                  return { ...e, sets: [...e.sets, { key: uid(), kg: last.kg, reps: last.reps, done: false }] }
                })
              }
            >
              Add set
            </button>
          </article>
        )
      })}

      {picking ? (
        <Picker
          label="Search lifts"
          options={EXERCISES}
          describe={(o) => o.group}
          onClose={() => setPicking(false)}
          onPick={(id) => {
            setWorkout((w) => ({ ...w, exercises: [...w.exercises, { key: uid(), exId: id, sets: [{ key: uid(), kg: 20, reps: 8, done: false }] }] }))
            setPicking(false)
          }}
        />
      ) : (
        <button type="button" className="btn btn-outline btn-block" onClick={() => setPicking(true)}>Add lift</button>
      )}

      <button
        type="button"
        className="btn btn-primary btn-block"
        disabled={done.length === 0}
        onClick={() => notify(`${workout.name || 'Workout'} saved: ${done.length} sets, ${fmt(volume)} kg. Sharp sharp!`)}
      >
        Finish workout
      </button>
      {done.length === 0 && <p className="hint center">Tick off at least one set to finish.</p>}
    </section>
  )
}
