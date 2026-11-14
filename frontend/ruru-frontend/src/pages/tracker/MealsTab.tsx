import { useMemo, useState } from 'react'
import { BeadString } from '../../components'
import { FOODS, TARGETS, findFood, kcalOf, macrosFor, sumMacros, fmt, uid } from '../../data'
import type { Meal, MealItem, MacroTotals } from '../../models/Nutrition'
import { Picker } from './Picker'
import type { TabProps } from './shared'

const initialMeals: Meal[] = [
  {
    key: uid(),
    name: 'Breakfast',
    items: [
      { key: uid(), foodId: 'oats', g: 60 },
      { key: uid(), foodId: 'amasi', g: 250 },
      { key: uid(), foodId: 'banana', g: 120 },
    ],
  },
  {
    key: uid(),
    name: 'Lunch',
    items: [
      { key: uid(), foodId: 'pap', g: 300 },
      { key: uid(), foodId: 'chakalaka', g: 100 },
      { key: uid(), foodId: 'chicken', g: 150 },
    ],
  },
]

interface MealItemWithMacros extends MealItem {
  m: MacroTotals
}

interface MealWithMacros extends Omit<Meal, 'items'> {
  items: MealItemWithMacros[]
  total: MacroTotals
}

/** The Food tab: logs meals against the day's calorie and macro targets. Owns its own state. */
export function MealsTab({ active, notify }: TabProps) {
  const [meals, setMeals] = useState<Meal[]>(initialMeals)
  const [pickingFor, setPickingFor] = useState<string | null>(null)

  const withMacros = useMemo<MealWithMacros[]>(
    () =>
      meals.map((meal) => {
        const items = meal.items.map((it) => ({ ...it, m: macrosFor(it.foodId, it.g) }))
        return { ...meal, items, total: sumMacros(items.map((i) => i.m)) }
      }),
    [meals],
  )
  const day = sumMacros(withMacros.map((m) => m.total))
  const left = TARGETS.kcal - day.kcal

  const updateMeal = (key: string, fn: (m: Meal) => Meal) => setMeals((ms) => ms.map((m) => (m.key === key ? fn(m) : m)))

  function removeItem(meal: MealWithMacros, item: MealItem) {
    const prev = meals
    updateMeal(meal.key, (m) => ({ ...m, items: m.items.filter((i) => i.key !== item.key) }))
    notify(`Removed ${findFood(item.foodId).name} from ${meal.name}.`, () => setMeals(prev))
  }
  function removeMeal(meal: MealWithMacros) {
    const prev = meals
    setMeals((ms) => ms.filter((m) => m.key !== meal.key))
    notify(`Removed ${meal.name || 'meal'}.`, () => setMeals(prev))
  }

  return (
    <section className="panel meals-panel" aria-labelledby="meals-h" hidden={!active}>
      <div className="panel-head">
        <h2 id="meals-h" className="panel-title">Food</h2>
        <p className="kcal-left">
          <span className="kcal-num">{fmt(Math.abs(left))}</span>
          <span>kcal {left >= 0 ? 'left today' : 'over today'}</span>
        </p>
        <div className="strands">
          <BeadString label="Calories" value={day.kcal} target={TARGETS.kcal} unit="kcal" tone="k" />
          <BeadString label="Protein" value={day.p} target={TARGETS.p} tone="p" />
          <BeadString label="Carbs" value={day.c} target={TARGETS.c} tone="c" />
          <BeadString label="Fat" value={day.f} target={TARGETS.f} tone="f" />
        </div>
      </div>

      {withMacros.length === 0 && <p className="empty">No meals yet. Add one and start with what you ate last.</p>}

      {withMacros.map((meal) => (
        <article className="block" key={meal.key}>
          <header className="block-head">
            <div className="meal-name">
              <label className="sr-only" htmlFor={`mn-${meal.key}`}>Meal name</label>
              <input
                id={`mn-${meal.key}`}
                className="title-input sm"
                value={meal.name}
                autoComplete="off"
                onChange={(e) => updateMeal(meal.key, (m) => ({ ...m, name: e.target.value }))}
              />
              <p className="muted macro-line">
                <span className="m p">P {fmt(meal.total.p)} g</span>
                <span className="m c">C {fmt(meal.total.c)} g</span>
                <span className="m f">F {fmt(meal.total.f)} g</span>
              </p>
            </div>
            <p className="meal-kcal"><span className="kcal-num">{fmt(meal.total.kcal)}</span> kcal</p>
          </header>

          {meal.items.length > 0 ? (
            <ul className="food-list">
              {meal.items.map((it) => {
                const food = findFood(it.foodId)
                return (
                  <li key={it.key}>
                    <span className="food-name">{food.name}</span>
                    <span className="grams">
                      <input
                        type="number"
                        inputMode="decimal"
                        min="0"
                        step="5"
                        value={it.g}
                        aria-label={`${food.name}, grams`}
                        onChange={(e) =>
                          updateMeal(meal.key, (m) => ({ ...m, items: m.items.map((x) => (x.key === it.key ? { ...x, g: e.target.value } : x)) }))
                        }
                      />
                      <span aria-hidden="true">g</span>
                    </span>
                    <span className="food-kcal">{fmt(it.m.kcal)} kcal</span>
                    <button type="button" className="icon-btn" onClick={() => removeItem(meal, it)} aria-label={`Remove ${food.name}`}>
                      <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
                    </button>
                  </li>
                )
              })}
            </ul>
          ) : (
            <p className="empty">Nothing in this meal yet.</p>
          )}

          {pickingFor === meal.key ? (
            <Picker
              label="Search foods"
              options={FOODS}
              describe={(o) => `${fmt(kcalOf(o))} kcal / 100 g`}
              onClose={() => setPickingFor(null)}
              onPick={(id) => {
                updateMeal(meal.key, (m) => ({ ...m, items: [...m.items, { key: uid(), foodId: id, g: 100 }] }))
                setPickingFor(null)
              }}
            />
          ) : (
            <div className="row-actions">
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setPickingFor(meal.key)}>Add food</button>
              <button type="button" className="btn btn-ghost btn-sm danger" onClick={() => removeMeal(meal)}>Remove meal</button>
            </div>
          )}
        </article>
      ))}

      <button
        type="button"
        className="btn btn-outline btn-block"
        onClick={() => {
          const key = uid()
          const names = ['Breakfast', 'Lunch', 'Dinner', 'Snack']
          const name = names[meals.length] ?? `Meal ${meals.length + 1}`
          setMeals((ms) => [...ms, { key, name, items: [] }])
          setPickingFor(key)
        }}
      >
        Add meal
      </button>
    </section>
  )
}
