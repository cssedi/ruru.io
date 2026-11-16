// Mirrors backend/ruru-api/src/DTOs/TrainingPlans.

import type { DayOfWeek } from './Onboarding'

export interface IReadinessItem {
  /** The onboarding question key, e.g. "trainingDays". */
  key: string
  label: string
  /** The answer in plain words, or null when it's missing. */
  value: string | null
  /** Needed: no plan without it. Otherwise it makes the plan fit better. */
  needed: boolean
}

export interface IPlanReadiness {
  canGenerate: boolean
  hasAiConsent: boolean
  aiConsentVersion: string
  hasProfileConsent: boolean
  items: IReadinessItem[]
}

export interface IPlanPhase {
  name: string
  weeks: string
  focus: string
  intensity: string
}

export interface IPlanExercise {
  exerciseId: string
  name: string
  sets: number
  reps: string
  rest: string
  intensity: string
  notes: string
  easierOption: string
}

export interface IPlanDay {
  day: DayOfWeek
  name: string
  focus: string
  exercises: IPlanExercise[]
}

export interface IGeneratedPlan {
  title: string
  summary: string
  durationWeeks: number
  split: string
  phases: IPlanPhase[]
  days: IPlanDay[]
  warmUp: string[]
  coolDown: string[]
  progression: string
  safetyNotes: string[]
}

export type TrainingPlanStatus = 'Draft' | 'Saved' | 'Archived'

export interface ITrainingPlan {
  id: string
  status: TrainingPlanStatus
  createdAt: string
  plan: IGeneratedPlan
}
