interface Props {
  label: string
  value: string
  onChange: (value: string) => void
  id?: string
}

/** Marcador numérico con flechas para subirlo o bajarlo de uno en uno (nunca por debajo de 0). */
export default function ScoreInput({ label, value, onChange, id }: Props) {
  const current = /^\d+$/.test(value.trim()) ? Number(value) : 0
  const step = (delta: number) => onChange(String(Math.max(0, current + delta)))
  return (
    <label className="score-input">
      {label}
      <span className="score-stepper">
        <button type="button" className="secondary" aria-label={`Bajar ${label.toLowerCase()}`} onClick={e => { e.preventDefault(); step(-1) }}>▼</button>
        <input id={id} inputMode="numeric" value={value} onChange={e => onChange(e.target.value)} />
        <button type="button" className="secondary" aria-label={`Subir ${label.toLowerCase()}`} onClick={e => { e.preventDefault(); step(1) }}>▲</button>
      </span>
    </label>
  )
}
