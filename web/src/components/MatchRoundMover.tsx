import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'

interface Props {
  matchId: number
  round: number
  /** Jornadas existentes en el grupo del partido. */
  rounds: number[]
  onChanged: () => Promise<void> | void
}

const NEW_ROUND = 'new'

/** Selector para mover un partido pendiente a otra jornada de su grupo (o a una nueva al final). */
export default function MatchRoundMover({ matchId, round, rounds, onChanged }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const last = Math.max(...rounds)

  async function move(value: string) {
    if (!value) return
    setBusy(true)
    setError(null)
    try {
      await api.matches.moveToRound(matchId, value === NEW_ROUND ? last + 1 : Number(value))
      notify('Partido movido')
      await onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="round-mover">
      <label className="muted">
        Mover a{' '}
        <select value="" disabled={busy} onChange={e => void move(e.target.value)} aria-label={`Mover el partido desde la jornada ${round}`}>
          <option value="">jornada…</option>
          {rounds.filter(r => r !== round).map(r => <option key={r} value={r}>Jornada {r}</option>)}
          <option value={NEW_ROUND}>Nueva jornada ({last + 1})</option>
        </select>
      </label>
      {error && <span className="error" role="alert"> {error}</span>}
    </div>
  )
}
