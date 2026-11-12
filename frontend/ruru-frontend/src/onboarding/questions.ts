import type { IOnboardingAnswers } from '../models/Onboarding'

/*
  The onboarding questionnaire, as data. Each section is one progress bar at the top
  of the screen; each question is one "story". Values match the API's enum names
  (src/models/Onboarding.ts). Required questions have no Skip button.
*/

export type SingleKey =
  | 'sex'
  | 'mainGoal'
  | 'pace'
  | 'activityLevel'
  | 'experience'
  | 'sleep'
  | 'trainingLocation'
  | 'workoutMinutes'
  | 'diet'
export type MultiKey = 'healthConsiderations' | 'barriers' | 'trainingDays' | 'focusAreas' | 'excludedFoods'
export type NumberKey = 'heightCm' | 'currentWeightKg' | 'goalWeightKg'

export interface Option {
  value: string | number
  label: string
  hint?: string
}

interface QuestionBase {
  title: string
  subtitle?: string
  required?: boolean
}

export interface SingleQuestion extends QuestionBase {
  kind: 'single'
  key: SingleKey
  options: Option[]
}

export interface MultiQuestion extends QuestionBase {
  kind: 'multi'
  key: MultiKey
  options: Option[]
  /** Label for an extra option that clears the others, e.g. "None of these". */
  noneLabel?: string
}

export interface NumberQuestion extends QuestionBase {
  kind: 'number'
  key: NumberKey
  unit: 'cm' | 'kg'
  min: number
  max: number
  step: number
}

export interface DateQuestion extends QuestionBase {
  kind: 'date'
  key: 'dateOfBirth'
}

export type Question = SingleQuestion | MultiQuestion | NumberQuestion | DateQuestion

export interface Section {
  id: string
  title: string
  questions: Question[]
}

export const MINIMUM_AGE = 18

export const SECTIONS: Section[] = [
  {
    id: 'about',
    title: 'About you',
    questions: [
      {
        kind: 'single',
        key: 'sex',
        title: 'What is your sex?',
        subtitle: 'Sex affects body metrics like your daily energy needs.',
        required: true,
        options: [
          { value: 'Male', label: 'Male' },
          { value: 'Female', label: 'Female' },
          { value: 'NonBinary', label: 'Non-binary' },
        ],
      },
      {
        kind: 'date',
        key: 'dateOfBirth',
        title: 'When is your birthday?',
        subtitle: `Age changes how many calories you burn. You need to be ${MINIMUM_AGE} or older to use ruru.`,
        required: true,
      },
      {
        kind: 'single',
        key: 'mainGoal',
        title: 'What’s your main goal?',
        required: true,
        options: [
          { value: 'LoseWeight', label: 'Lose weight', hint: 'Burn off extra body fat.' },
          { value: 'BuildMuscle', label: 'Build muscle', hint: 'Get stronger and add muscle.' },
          { value: 'KeepFit', label: 'Keep fit', hint: 'Stay healthy and active.' },
        ],
      },
    ],
  },
  {
    id: 'body',
    title: 'Body',
    questions: [
      { kind: 'number', key: 'heightCm', title: 'How tall are you?', unit: 'cm', min: 100, max: 250, step: 1, required: true },
      {
        kind: 'number',
        key: 'currentWeightKg',
        title: 'What do you weigh now?',
        unit: 'kg',
        min: 30,
        max: 300,
        step: 0.5,
        required: true,
      },
      {
        kind: 'number',
        key: 'goalWeightKg',
        title: 'What’s your goal weight?',
        unit: 'kg',
        min: 30,
        max: 300,
        step: 0.5,
        required: true,
      },
      {
        kind: 'single',
        key: 'pace',
        title: 'How fast do you want to get there?',
        subtitle: 'We’ll set your targets to match. Skip and we’ll go with moderate.',
        options: [
          { value: 'Slow', label: 'Slow and steady' },
          { value: 'Moderate', label: 'Moderate' },
          { value: 'Fast', label: 'As fast as is safe' },
        ],
      },
      {
        kind: 'multi',
        key: 'healthConsiderations',
        title: 'Anything we should work around?',
        subtitle: 'We’ll steer clear of exercises and foods that don’t suit you.',
        noneLabel: 'None of these',
        options: [
          { value: 'SensitiveBack', label: 'Sensitive back' },
          { value: 'SensitiveKnees', label: 'Sensitive knees' },
          { value: 'Type1Diabetes', label: 'Type 1 diabetes' },
          { value: 'Type2Diabetes', label: 'Type 2 diabetes' },
          { value: 'InjuryRecovery', label: 'Recovering from an injury' },
          { value: 'LimitedMobility', label: 'Limited mobility' },
        ],
      },
    ],
  },
  {
    id: 'lifestyle',
    title: 'Lifestyle',
    questions: [
      {
        kind: 'single',
        key: 'activityLevel',
        title: 'How active are you right now?',
        required: true,
        options: [
          { value: 'NotMuch', label: 'Not much' },
          { value: 'OneToTwo', label: '1–2 workouts a week' },
          { value: 'ThreeToFive', label: '3–5 workouts a week' },
          { value: 'SixPlus', label: '6 or more a week' },
        ],
      },
      {
        kind: 'single',
        key: 'experience',
        title: 'How much training experience do you have?',
        options: [
          { value: 'Beginner', label: 'Beginner', hint: 'New to it, or coming back after a long break.' },
          { value: 'Intermediate', label: 'Intermediate', hint: 'Training on and off for a while.' },
          { value: 'Advanced', label: 'Advanced', hint: 'Training consistently for years.' },
        ],
      },
      {
        kind: 'single',
        key: 'sleep',
        title: 'How much do you usually sleep?',
        options: [
          { value: 'UnderFive', label: 'Less than 5 hours' },
          { value: 'FiveToSix', label: '5–6 hours' },
          { value: 'SevenToEight', label: '7–8 hours' },
          { value: 'OverEight', label: 'More than 8 hours' },
        ],
      },
      {
        kind: 'multi',
        key: 'barriers',
        title: 'What’s held you back before?',
        subtitle: 'Pick any that sound familiar.',
        noneLabel: 'Nothing really',
        options: [
          { value: 'BusySchedule', label: 'A busy schedule' },
          { value: 'Consistency', label: 'Staying consistent' },
          { value: 'Snacking', label: 'Snacking and sugar' },
          { value: 'NotSureWhatToDo', label: 'Not knowing what to do' },
          { value: 'LackOfSupport', label: 'No one to keep me going' },
        ],
      },
    ],
  },
  {
    id: 'training',
    title: 'Training',
    questions: [
      {
        kind: 'single',
        key: 'trainingLocation',
        title: 'Where will you train?',
        options: [
          { value: 'Gym', label: 'At a gym' },
          { value: 'HomeWithEquipment', label: 'At home, with some equipment' },
          { value: 'Bodyweight', label: 'Anywhere, bodyweight only' },
        ],
      },
      {
        kind: 'multi',
        key: 'trainingDays',
        title: 'Which days can you train?',
        subtitle: 'We’ll plan your week around these.',
        options: [
          { value: 'Monday', label: 'Monday' },
          { value: 'Tuesday', label: 'Tuesday' },
          { value: 'Wednesday', label: 'Wednesday' },
          { value: 'Thursday', label: 'Thursday' },
          { value: 'Friday', label: 'Friday' },
          { value: 'Saturday', label: 'Saturday' },
          { value: 'Sunday', label: 'Sunday' },
        ],
      },
      {
        kind: 'single',
        key: 'workoutMinutes',
        title: 'How long should a workout be?',
        options: [
          { value: 15, label: '15 minutes', hint: 'Quick' },
          { value: 30, label: '30 minutes', hint: 'Steady' },
          { value: 45, label: '45 minutes', hint: 'Standard' },
          { value: 60, label: '60 minutes', hint: 'Serious' },
          { value: 90, label: '90 minutes or more', hint: 'All in' },
        ],
      },
      {
        kind: 'multi',
        key: 'focusAreas',
        title: 'What do you want to focus on?',
        subtitle: 'Leave it blank for a full-body plan.',
        options: [
          { value: 'Chest', label: 'Chest' },
          { value: 'Back', label: 'Back' },
          { value: 'Shoulders', label: 'Shoulders' },
          { value: 'Arms', label: 'Arms' },
          { value: 'Core', label: 'Core' },
          { value: 'Legs', label: 'Legs' },
        ],
      },
    ],
  },
  {
    id: 'nutrition',
    title: 'Nutrition',
    questions: [
      {
        kind: 'single',
        key: 'diet',
        title: 'How do you eat?',
        options: [
          { value: 'NoPreference', label: 'A bit of everything' },
          { value: 'Vegetarian', label: 'Vegetarian' },
          { value: 'Vegan', label: 'Vegan' },
          { value: 'Pescatarian', label: 'Pescatarian' },
          { value: 'Keto', label: 'Keto' },
          { value: 'Mediterranean', label: 'Mediterranean' },
        ],
      },
      {
        kind: 'multi',
        key: 'excludedFoods',
        title: 'Anything you don’t eat?',
        noneLabel: 'I eat everything',
        options: [
          { value: 'Dairy', label: 'Dairy' },
          { value: 'Gluten', label: 'Gluten' },
          { value: 'Eggs', label: 'Eggs' },
          { value: 'Fish', label: 'Fish' },
          { value: 'Nuts', label: 'Nuts' },
          { value: 'Pork', label: 'Pork' },
        ],
      },
    ],
  },
]

/** Every question in order, with where it sits, for stepping through. */
export const STEPS = SECTIONS.flatMap((section, sectionIndex) =>
  section.questions.map((question, indexInSection) => ({ section, sectionIndex, question, indexInSection })),
)

/** Age in whole years on today's date, or null for an invalid date. */
export function ageFrom(isoDate: string, today = new Date()): number | null {
  const dob = new Date(`${isoDate}T00:00:00`)
  if (Number.isNaN(dob.getTime())) return null
  let age = today.getFullYear() - dob.getFullYear()
  const beforeBirthday =
    today.getMonth() < dob.getMonth() || (today.getMonth() === dob.getMonth() && today.getDate() < dob.getDate())
  if (beforeBirthday) age--
  return age
}

/** Why the answer can't be accepted yet, or null when it's fine (empty is fine; see `isAnswered`). */
export function answerError(question: Question, answers: IOnboardingAnswers): string | null {
  if (question.kind === 'number') {
    const value = answers[question.key]
    if (value === null) return null
    if (value < question.min || value > question.max) {
      return `Enter a number between ${question.min} and ${question.max} ${question.unit}.`
    }
  }
  if (question.kind === 'date' && answers.dateOfBirth) {
    const age = ageFrom(answers.dateOfBirth)
    if (age === null || age > 120) return 'Check the date.'
    if (age < MINIMUM_AGE) return `You need to be ${MINIMUM_AGE} or older to use ruru.`
  }
  return null
}

export function isAnswered(question: Question, answers: IOnboardingAnswers, noneChosen: ReadonlySet<string>): boolean {
  switch (question.kind) {
    case 'multi':
      return answers[question.key].length > 0 || noneChosen.has(question.key)
    case 'number':
      return answers[question.key] !== null
    case 'date':
      return answers.dateOfBirth !== null && answers.dateOfBirth !== ''
    case 'single':
      return answers[question.key] !== null
  }
}
