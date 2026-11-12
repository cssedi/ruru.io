export type MuscleGroup = 'Legs' | 'Chest' | 'Back' | 'Shoulders' | 'Glutes' | 'Arms'

export interface Exercise {
  id: string
  name: string
  group: MuscleGroup
}

export interface WorkoutSet {
  key: string
  /** Kept as entered so the input can be cleared while typing. */
  kg: number | string
  reps: number | string
  done: boolean
}

export interface WorkoutExercise {
  key: string
  exId: string
  sets: WorkoutSet[]
}

export interface Workout {
  name: string
  exercises: WorkoutExercise[]
}
