// Mock data only. Nothing here talks to a server.
import type { Food, Macros, MacroTargets, MacroTotals } from './models/Nutrition'
import type { Exercise } from './models/Workout'

// Macros per 100 g (approximate, cooked where relevant)
export const FOODS: Food[] = [
  { id: 'pap', name: 'Pap (stiff, cooked)', p: 2.3, c: 22, f: 0.5 },
  { id: 'morogo', name: 'Morogo, cooked', p: 3, c: 4, f: 0.5 },
  { id: 'chakalaka', name: 'Chakalaka', p: 2, c: 9, f: 3 },
  { id: 'amasi', name: 'Amasi, full cream', p: 3.3, c: 4.5, f: 3.4 },
  { id: 'boerewors', name: 'Boerewors, grilled', p: 17, c: 2, f: 22 },
  { id: 'mogodu', name: 'Mogodu (tripe), cooked', p: 12, c: 0, f: 4 },
  { id: 'biltong', name: 'Biltong, beef', p: 55, c: 2, f: 4 },
  { id: 'chicken', name: 'Chicken breast, grilled', p: 31, c: 0, f: 3.6 },
  { id: 'eggs', name: 'Eggs, whole', p: 13, c: 1.1, f: 10 },
  { id: 'oats', name: 'Rolled oats, dry', p: 13, c: 66, f: 7 },
  { id: 'banana', name: 'Banana', p: 1.1, c: 23, f: 0.3 },
  { id: 'rice', name: 'White rice, cooked', p: 2.7, c: 28, f: 0.3 },
  { id: 'beans', name: 'Sugar beans, cooked', p: 8, c: 22, f: 0.5 },
  { id: 'vetkoek', name: 'Vetkoek', p: 7, c: 45, f: 15 },
  { id: 'bread', name: 'Brown bread', p: 9, c: 43, f: 3.5 },
  { id: 'pb', name: 'Peanut butter', p: 25, c: 20, f: 50 },
  { id: 'whey', name: 'Whey protein powder', p: 78, c: 8, f: 6 },
]

export const EXERCISES: Exercise[] = [
  { id: 'squat', name: 'Barbell back squat', group: 'Legs' },
  { id: 'bench', name: 'Bench press', group: 'Chest' },
  { id: 'deadlift', name: 'Deadlift', group: 'Back' },
  { id: 'ohp', name: 'Overhead press', group: 'Shoulders' },
  { id: 'row', name: 'Barbell row', group: 'Back' },
  { id: 'pullup', name: 'Pull-up', group: 'Back' },
  { id: 'rdl', name: 'Romanian deadlift', group: 'Legs' },
  { id: 'lunge', name: 'Dumbbell walking lunge', group: 'Legs' },
  { id: 'incline', name: 'Incline dumbbell press', group: 'Chest' },
  { id: 'pulldown', name: 'Lat pulldown', group: 'Back' },
  { id: 'thrust', name: 'Hip thrust', group: 'Glutes' },
  { id: 'curl', name: 'Dumbbell curl', group: 'Arms' },
]

export const TARGETS: MacroTargets = { kcal: 2400, p: 160, c: 260, f: 80 }

export const kcalOf = ({ p, c, f }: Macros): number => p * 4 + c * 4 + f * 9

export function findFood(id: string): Food {
  const food = FOODS.find((x) => x.id === id)
  if (!food) throw new Error(`Unknown food: ${id}`)
  return food
}

export function findExercise(id: string): Exercise {
  const ex = EXERCISES.find((x) => x.id === id)
  if (!ex) throw new Error(`Unknown exercise: ${id}`)
  return ex
}

export function macrosFor(foodId: string, grams: number | string): MacroTotals {
  const food = findFood(foodId)
  const k = (Number(grams) || 0) / 100
  const m: Macros = { p: food.p * k, c: food.c * k, f: food.f * k }
  return { ...m, kcal: kcalOf(m) }
}

export function sumMacros(list: MacroTotals[]): MacroTotals {
  return list.reduce<MacroTotals>(
    (a, m) => ({ p: a.p + m.p, c: a.c + m.c, f: a.f + m.f, kcal: a.kcal + m.kcal }),
    { p: 0, c: 0, f: 0, kcal: 0 },
  )
}

const nf = new Intl.NumberFormat('en-ZA', { maximumFractionDigits: 0 })
export const fmt = (n: number): string => nf.format(Math.round(n))

let seq = 100
export const uid = (): string => `id${seq++}`
