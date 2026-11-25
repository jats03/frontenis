interface Props {
  label: string
  value: string
  onChange: (value: string) => void
  id?: string
}

/** Marcador numérico que se escribe a mano. */
export default function ScoreInput({ label, value, onChange, id }: Props) {
  return (
    <label className="score-input">
      {label}
      <input id={id} inputMode="numeric" value={value} onChange={e => onChange(e.target.value)} />
    </label>
  )
}
