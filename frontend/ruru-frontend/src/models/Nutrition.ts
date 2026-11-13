export interface Macros {
  p: number
  c: number
  f: number
}

export interface MacroTotals extends Macros {
  kcal: number
}

export type MacroTargets = MacroTotals

/** A food in the database. Macros are grams per 100 g. */
export interface Food extends Macros {
  id: string
  name: string
}

export interface MealItem {
  key: string
  foodId: string
  /** Kept as entered so the input can be cleared while typing. */
  g: number | string
}

export interface Meal {
  key: string
  name: string
  items: MealItem[]
}
