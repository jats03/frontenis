import type { Player } from '../api/types'

interface Props {
  players: Player[]
  selected: Set<number>
  onChange: (next: Set<number>) => void
  disabledIds?: Set<number>
  /** Si se indica, cada jugador muestra un botón para pasarlo de delantero a zaguero (y al revés). */
  onSwap?: (player: Player) => void
  /** Jugadores cuyo cambio de posición está en curso (se bloquea su botón). */
  swappingIds?: Set<number>
}

/** Casilla "seleccionar todos" con estado intermedio cuando solo hay una parte marcada. */
function SelectAll({ label, total, checked, onToggle }: { label: string; total: number; checked: number; onToggle: () => void }) {
  const all = total > 0 && checked === total
  return (
    <label className="check select-all">
      <input type="checkbox" checked={all} disabled={total === 0} onChange={onToggle}
        ref={el => { if (el) el.indeterminate = checked > 0 && !all }} />
      {label} <span className="muted">({checked}/{total})</span>
    </label>
  )
}

/** Lista seleccionable de jugadores agrupada por posición, con selección total y por grupo. */
export default function PlayerPicker({ players, selected, onChange, disabledIds, onSwap, swappingIds }: Props) {
  const enabled = (list: Player[]) => list.filter(p => !disabledIds?.has(p.id))

  function toggle(id: number) {
    const next = new Set(selected)
    if (next.has(id)) next.delete(id); else next.add(id)
    onChange(next)
  }

  /** Marca todos los jugadores de la lista; si ya lo estaban todos, los desmarca. */
  function toggleMany(list: Player[]) {
    const ids = enabled(list).map(p => p.id)
    const allSelected = ids.length > 0 && ids.every(id => selected.has(id))
    const next = new Set(selected)
    ids.forEach(id => (allSelected ? next.delete(id) : next.add(id)))
    onChange(next)
  }

  const count = (list: Player[]) => enabled(list).filter(p => selected.has(p.id)).length

  const group = (title: string, list: Player[]) => (
    <div className="picker-col">
      <SelectAll label={title} total={enabled(list).length} checked={count(list)} onToggle={() => toggleMany(list)} />
      {list.map(p => {
        const other = p.position === 'Delantero' ? 'Zaguero' : 'Delantero'
        return (
          <div key={p.id} className="picker-row">
            <label className="check">
              <input type="checkbox" checked={selected.has(p.id)} disabled={disabledIds?.has(p.id)}
                onChange={() => toggle(p.id)} />
              {p.name}{p.groupNumber != null && <span className="muted"> · G{p.groupNumber}</span>}
            </label>
            {onSwap && (
              <button type="button" className="secondary swap" disabled={swappingIds?.has(p.id)}
                title={`Pasar a ${other.toLowerCase()}`} aria-label={`Pasar a ${p.name} a ${other.toLowerCase()}`}
                onClick={() => onSwap(p)}>
                ⇄ {other}
              </button>
            )}
          </div>
        )
      })}
      {list.length === 0 && <p className="muted">Sin jugadores</p>}
    </div>
  )

  // Grupos de jugadores (1, 2, 3…) presentes en la lista, para seleccionarlos de una vez.
  const groupNumbers = [...new Set(players.map(p => p.groupNumber).filter((g): g is number => g != null))].sort((a, b) => a - b)
  const ungrouped = players.filter(p => p.groupNumber == null)

  return (
    <>
      <div className="picker-all">
        <SelectAll label="Seleccionar todos" total={enabled(players).length} checked={count(players)}
          onToggle={() => toggleMany(players)} />
      </div>
      {groupNumbers.length > 0 && (
        <div className="picker-all picker-groups">
          {groupNumbers.map(g => {
            const list = players.filter(p => p.groupNumber === g)
            return <SelectAll key={g} label={`Jugadores grupo ${g}`} total={enabled(list).length}
              checked={count(list)} onToggle={() => toggleMany(list)} />
          })}
          {ungrouped.length > 0 && (
            <SelectAll label="Sin grupo" total={enabled(ungrouped).length} checked={count(ungrouped)}
              onToggle={() => toggleMany(ungrouped)} />
          )}
        </div>
      )}
      <div className="picker">
        {group('Delanteros', players.filter(p => p.position === 'Delantero'))}
        {group('Zagueros', players.filter(p => p.position === 'Zaguero'))}
      </div>
    </>
  )
}
