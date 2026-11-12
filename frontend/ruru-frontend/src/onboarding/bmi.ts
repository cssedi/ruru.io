// Body mass index helpers for the weight questions. BMI is a rough guide, so the
// copy shown to users stays gentle and never blocks them from continuing.

export type BmiBand = 'under' | 'healthy' | 'over'

export const HEALTHY_BMI_MIN = 18.5
export const HEALTHY_BMI_MAX = 24.9

export function bmi(weightKg: number, heightCm: number): number {
  const m = heightCm / 100
  return weightKg / (m * m)
}

export function bmiBand(value: number): BmiBand {
  if (value < HEALTHY_BMI_MIN) return 'under'
  if (value > HEALTHY_BMI_MAX) return 'over'
  return 'healthy'
}

/** Weight range (kg) that gives a healthy BMI at this height. */
export function healthyWeightRange(heightCm: number): [number, number] {
  const m = heightCm / 100
  return [HEALTHY_BMI_MIN * m * m, HEALTHY_BMI_MAX * m * m]
}

/** Rough weekly change (kg) by pace. Losing can go faster than gaining lean mass. */
export function weeklyChangeKg(direction: 'lose' | 'gain', pace: 'Slow' | 'Moderate' | 'Fast'): number {
  const lose = { Slow: 0.25, Moderate: 0.5, Fast: 0.75 }
  const gain = { Slow: 0.1, Moderate: 0.25, Fast: 0.35 }
  return (direction === 'lose' ? lose : gain)[pace]
}
