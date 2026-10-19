import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { MatchDto, PairDto } from '../api/types'

interface Props {
  match: MatchDto
  /** Partidos de la fase de grupos del mismo grupo (incluido este). */
  groupMatches: MatchDto[]
  pairs: PairDto[]
  onChanged: () => Promise<void> | void
}

const NEW_ROUND = 'new'

/** Jugadores de un partido (los de sus dos parejas). */
function playersOf(m: MatchDto, pairs: PairDto[]): number[] {
  return [m.homeEntryId, m.awayEntryId].flatMap(id => {
    const p = pairs.find(x => x.entryId === id)
    return p ? [p.player1.id, p.player2.id] : []
  })
}

/**
 * Herramientas de un partido pendiente de una liga rotatoria: moverlo a otra jornada de su grupo (o a una nueva)
 * o intercambiarlo con otro partido pendiente, lo que permite cruzar partidos aunque moverlos solos chocaría.
 */
export default function MatchRoundMover({ match, groupMatches, pairs, onChanged }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const rounds = [...new Set(groupMatches.map(m => m.round))].sort((a, b) => a - b)
  const last = Math.max(...rounds)

  // Solo se ofrecen los intercambios que no dejan a nadie jugando dos veces en la misma jornada.
  const swaps = groupMatches.filter(other => {
    if (other.id === match.id || other.isPlayed || other.round === match.round) return false
    const clash = (arriving: MatchDto, round: number) => {
      const busyIds = new Set(groupMatches.filter(m => m.id !== match.id && m.id !== other.id && m.round === round)
        .flatMap(m => playersOf(m, pairs)))
      return playersOf(arriving, pairs).some(id => busyIds.has(id))
    }
    return !clash(match, other.round) && !clash(other, match.round)
  }).sort((a, b) => a.round - b.round || a.order - b.order)

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
