import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { MatchDto } from '../api/types'

interface Props {
  match: MatchDto
  /** Partidos de la fase de grupos del mismo grupo (incluido este). */
  groupMatches: MatchDto[]
  onChanged: () => Promise<void> | void
}

const NEW_ROUND = 'new'

/**
 * Herramientas de un partido pendiente de una liga rotatoria: moverlo a otra jornada de su grupo (o a una nueva)
 * o intercambiarlo con otro partido pendiente. No se impide que un jugador repita en una misma jornada.
 */
export default function MatchRoundMover({ match, groupMatches, onChanged }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const rounds = [...new Set(groupMatches.map(m => m.round))].sort((a, b) => a - b)
  const last = Math.max(...rounds)

  // Se puede intercambiar con cualquier otro partido pendiente del grupo en otra jornada.
  const swaps = groupMatches
    .filter(other => other.id !== match.id && !other.isPlayed && other.round !== match.round)
    .sort((a, b) => a.round - b.round || a.order - b.order)

  async function run(action: () => Promise<unknown>, done: string) {
    setBusy(true)
    setError(null)
    try {
      await action()
      notify(done)
      await onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error')
    } finally {
      setBusy(false)
    }
  }

  const move = (value: string) => value && run(
    () => api.matches.moveToRound(match.id, value === NEW_ROUND ? last + 1 : Number(value)), 'Partido movido')
  const swap = (value: string) => value && run(() => api.matches.swapWith(match.id, Number(value)), 'Partidos intercambiados')

  return (
    <div className="round-mover">
      <label className="muted">
        Mover a{' '}
        <select value="" disabled={busy} onChange={e => void move(e.target.value)} aria-label={`Mover el partido desde la jornada ${match.round}`}>
          <option value="">jornada…</option>
          {rounds.filter(r => r !== match.round).map(r => <option key={r} value={r}>Jornada {r}</option>)}
          <option value={NEW_ROUND}>Nueva jornada ({last + 1})</option>
        </select>
      </label>
      {swaps.length > 0 && (
        <label className="muted">
          {' '}o intercambiar con{' '}
          <select value="" disabled={busy} onChange={e => void swap(e.target.value)} aria-label={`Intercambiar el partido de la jornada ${match.round} con otro`}>
            <option value="">otro partido…</option>
            {swaps.map(o => <option key={o.id} value={o.id}>J{o.round}: {o.homeName} vs {o.awayName}</option>)}
          </select>
        </label>
      )}
      {error && <span className="error" role="alert"> {error}</span>}
    </div>
  )
}
