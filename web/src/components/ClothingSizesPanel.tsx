import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { ClothingSize } from '../api/types'
import { CLOTHING_SIZES, isIncomplete } from '../clothing'

/**
 * Tallas de ropa del jugador de este dispositivo y de sus apadrinados: camiseta (parte de arriba) y pantalón
 * (parte de abajo). Cada cambio se guarda al elegirlo.
 */
export default function ClothingSizesPanel({ rows, onChange }: { rows: ClothingSize[]; onChange: (rows: ClothingSize[]) => void }) {
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)
  const missing = rows.filter(isIncomplete).length
  // Abierto al entrar; lo decide quien lo usa (no se cierra solo al completar la última talla).
  const [open, setOpen] = useState(true)

  async function save(row: ClothingSize, edit: Partial<Pick<ClothingSize, 'topSize' | 'bottomSize'>>) {
    const next = { topSize: row.topSize, bottomSize: row.bottomSize, ...edit }
    setError(null)
    setBusyId(row.playerId)
    try {
      const saved = await api.device.saveSizes(row.playerId, next)
      onChange(rows.map(r => (r.playerId === saved.playerId ? saved : r)))
      notify('Talla guardada')
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusyId(null) }
  }

  const select = (row: ClothingSize, field: 'topSize' | 'bottomSize', label: string) => (
    <label>{label}
      <select value={row[field] ?? ''} disabled={busyId === row.playerId}
        onChange={e => void save(row, { [field]: e.target.value || null })}>
        <option value="">Sin indicar</option>
        {CLOTHING_SIZES.map(s => <option key={s} value={s}>{s}</option>)}
      </select>
    </label>
  )

  return (
    <details className="card sizes-panel" open={open} onToggle={e => setOpen(e.currentTarget.open)}>
      <summary>
        <strong>Mis tallas</strong>
        <span className={missing > 0 ? 'size-state warn' : 'size-state ok'}>
          {missing > 0 ? (missing === 1 ? 'Falta 1 persona' : `Faltan ${missing} personas`) : 'Completas'}
        </span>
      </summary>
      <p className="muted">Indica la talla de camiseta y de pantalón.</p>
      {error && <p className="error" role="alert">{error}</p>}
      <ul className="plain sizes-list">
        {rows.map(r => (
          <li key={r.playerId} className="size-row">
            <strong>{r.name}</strong>
            <div className="cols">
              {select(r, 'topSize', 'Camiseta')}
              {select(r, 'bottomSize', 'Pantalón')}
            </div>
          </li>
        ))}
      </ul>
    </details>
  )
}
