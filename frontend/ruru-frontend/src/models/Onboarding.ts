// Mirrors backend/ruru-api/src/DTOs/Onboarding and src/model/Profile/ProfileOptions.cs.
// Enum values travel as their C# names.

export type Sex = 'Male' | 'Female' | 'NonBinary'
export type MainGoal = 'LoseWeight' | 'BuildMuscle' | 'KeepFit'
export type GoalPace = 'Slow' | 'Moderate' | 'Fast'
export type HealthConsideration =
  | 'SensitiveBack'
  | 'SensitiveKnees'
  | 'Type1Diabetes'
  | 'Type2Diabetes'
  | 'InjuryRecovery'
  | 'LimitedMobility'
export type ActivityLevel = 'NotMuch' | 'OneToTwo' | 'ThreeToFive' | 'SixPlus'
export type ExperienceLevel = 'Beginner' | 'Intermediate' | 'Advanced'
export type SleepDuration = 'UnderFive' | 'FiveToSix' | 'SevenToEight' | 'OverEight'
export type GoalBarrier = 'BusySchedule' | 'Consistency' | 'Snacking' | 'NotSureWhatToDo' | 'LackOfSupport'
export type TrainingLocation = 'Gym' | 'HomeWithEquipment' | 'Bodyweight'
export type DayOfWeek = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'
export type FocusArea = 'Chest' | 'Back' | 'Shoulders' | 'Arms' | 'Core' | 'Legs'
export type DietType = 'NoPreference' | 'Vegetarian' | 'Vegan' | 'Pescatarian' | 'Keto' | 'Mediterranean'
export type ExcludedFood = 'Dairy' | 'Gluten' | 'Eggs' | 'Fish' | 'Nuts' | 'Pork'

export interface IOnboardingAnswers {
  sex: Sex | null
  /** ISO date, yyyy-mm-dd */
  dateOfBirth: string | null
  mainGoal: MainGoal | null
  height: number | null
  currentWeight: number | null
  goalWeight: number | null
  pace: GoalPace | null
  healthConsiderations: HealthConsideration[]
  activityLevel: ActivityLevel | null
  experience: ExperienceLevel | null
  sleep: SleepDuration | null
  barriers: GoalBarrier[]
  trainingLocation: TrainingLocation | null
  trainingDays: DayOfWeek[]
  workoutMinutes: number | null
  focusAreas: FocusArea[]
  diet: DietType | null
  excludedFoods: ExcludedFood[]
}

export interface IOnboardingStatus {
  hasConsent: boolean
  consentVersion: string
  isComplete: boolean
  answers: IOnboardingAnswers
}

export interface IGiveConsentRequestBody {
  version: string
}

export const EMPTY_ANSWERS: IOnboardingAnswers = {
  sex: null,
  dateOfBirth: null,
  mainGoal: null,
  height: null,
  currentWeight: null,
  goalWeight: null,
  pace: null,
  healthConsiderations: [],
  activityLevel: null,
  experience: null,
  sleep: null,
  barriers: [],
  trainingLocation: null,
  trainingDays: [],
  workoutMinutes: null,
  focusAreas: [],
  diet: null,
  excludedFoods: [],
}
