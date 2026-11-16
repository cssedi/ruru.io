import { useEffect, useRef, useState } from 'react'

interface PickerOption {
  id: string
  name: string
}

interface PickerProps<T extends PickerOption> {
  label: string
  options: T[]
  onPick: (id: string) => void
  onClose: () => void
  describe: (option: T) => string
}

/* Inline search list used for both lifts and foods. */
export function Picker<T extends PickerOption>({ label, options, onPick, onClose, describe }: PickerProps<T>) {
  const [q, setQ] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)
  useEffect(() => inputRef.current?.focus(), [])
  const shown = options.filter((o) => o.name.toLowerCase().includes(q.trim().toLowerCase()))
  return (
    <div className="picker" onKeyDown={(e) => e.key === 'Escape' && onClose()}>
      <div className="picker-search">
        <label className="sr-only" htmlFor={`pick-${label}`}>{label}</label>
        <input
          id={`pick-${label}`}
          ref={inputRef}
          type="search"
          autoComplete="off"
          placeholder={`${label}…`}
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>Cancel</button>
      </div>
      {shown.length ? (
        <ul className="picker-list">
          {shown.map((o) => (
            <li key={o.id}>
              <button type="button" onClick={() => onPick(o.id)}>
                <span>{o.name}</span>
                <span className="muted">{describe(o)}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="empty">Nothing matches “{q}”. Try a shorter word.</p>
      )}
    </div>
  )
}
