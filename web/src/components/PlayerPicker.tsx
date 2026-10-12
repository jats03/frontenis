import type { Player } from '../api/types'

interface Props {
  players: Player[]
  selected: Set<number>
  onChange: (next: Set<number>) => void
  disabledIds?: Set<number>
}

/** Lista seleccionable de jugadores agrupada por posición. */
export default function PlayerPicker({ players, selected, onChange, disabledIds }: Props) {
  function toggle(id: number) {
    const next = new Set(selected)
    if (next.has(id)) next.delete(id); else next.add(id)
    onChange(next)
  }
  const group = (title: string, list: Player[]) => (
    <div className="picker-col">
      <h4>{title} ({list.filter(p => selected.has(p.id)).length}/{list.length})</h4>
      {list.map(p => (
        <label key={p.id} className="check">
          <input type="checkbox" checked={selected.has(p.id)} disabled={disabledIds?.has(p.id)}
            onChange={() => toggle(p.id)} />
          {p.name}
        </label>
      ))}
      {list.length === 0 && <p className="muted">Sin jugadores</p>}
    </div>
  )
  return (
    <div className="picker">
      {group('Delanteros', players.filter(p => p.position === 'Delantero'))}
      {group('Zagueros', players.filter(p => p.position === 'Zaguero'))}
    </div>
  )
}
