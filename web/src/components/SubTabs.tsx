export interface SubTabOption { key: string; label: string }

interface Props {
  options: SubTabOption[]
  value: string
  onChange: (key: string) => void
  label: string
}

/** Barra de pestañas secundarias (grupos, jornadas…). */
export default function SubTabs({ options, value, onChange, label }: Props) {
  return (
    <div className="tabs sub-tabs" role="tablist" aria-label={label}>
      {options.map(o => (
        <button key={o.key} role="tab" aria-selected={value === o.key} className={value === o.key ? 'active' : ''}
          onClick={() => onChange(o.key)}>{o.label}</button>
      ))}
    </div>
  )
}
