import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, Player } from '../api/types'

interface Props {
  detail: CompetitionDetail
  players: Player[]
  onChanged: () => Promise<void> | void
}

/** Añade un jugador a una liga rotatoria en marcha: entra en el último grupo y se recalculan sus partidos pendientes. */
export default function RotatingAddPlayer({ detail, players, onChanged }: Props) {
  const [playerId, setPlayerId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const taken = new Set(detail.pairs.flatMap(p => [p.player1.id, p.player2.id]))
  const available = players.filter(p => !taken.has(p.id)).sort((a, b) => a.name.localeCompare(b.name, 'es'))
  const lastGroup = [...detail.groups].sort((a, b) => a.id - b.id).at(-1)

  const locked = lastGroup !== undefined &&
    detail.matches.some(m => m.groupId === lastGroup.id && m.isPlayed)

  async function add() {
    if (!playerId) return
    setBusy(true)
    setError(null)
    try {
      await api.competitions.addRotatingPlayer(detail.summary.id, Number(playerId))
      notify('Jugador añadido; partidos del grupo recalculados')
      setPlayerId('')
      await onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="card">
      <h2>Añadir jugador</h2>
      <p className="muted">
        Entra en {lastGroup ? lastGroup.name : 'el último grupo'} y se vuelven a calcular todos sus partidos.
        Solo es posible mientras en ese grupo no se haya jugado ninguno.
      </p>
      {error && <p className="error" role="alert">{error}</p>}
      {locked
        ? <p className="muted">Ya se ha jugado algún partido en {lastGroup?.name}: no se pueden añadir más jugadores.</p>
        : available.length === 0
        ? <p className="muted">No quedan jugadores fuera de esta liga.</p>
        : (
          <div className="row">
            <select aria-label="Jugador a añadir" value={playerId} onChange={e => setPlayerId(e.target.value)} disabled={busy}>
              <option value="">Elige un jugador…</option>
              {available.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
            <button onClick={() => void add()} disabled={!playerId || busy}>Añadir</button>
          </div>
        )}
    </section>
  )
}
