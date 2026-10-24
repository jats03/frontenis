import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, Player } from '../api/types'

interface Props {
  detail: CompetitionDetail
  players: Player[]
  onChanged: () => Promise<void> | void
}

/** Añade un jugador a cualquier grupo de una liga rotatoria en marcha. Solo entra en el grupo: no se crea ni se cambia ningún partido. */
export default function RotatingAddPlayer({ detail, players, onChanged }: Props) {
  const groups = [...detail.groups].sort((a, b) => a.id - b.id)
  const [playerId, setPlayerId] = useState('')
  const [groupId, setGroupId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const taken = new Set([
    ...detail.pairs.flatMap(p => [p.player1.id, p.player2.id]),
    ...groups.flatMap(g => (g.players ?? []).map(p => p.id)),
  ])
  const available = players.filter(p => !taken.has(p.id)).sort((a, b) => a.name.localeCompare(b.name, 'es'))
  const target = groupId || String(groups.at(-1)?.id ?? '')

  async function add() {
    if (!playerId || !target) return
    setBusy(true)
    setError(null)
    try {
      await api.competitions.addRotatingPlayer(detail.summary.id, Number(playerId), Number(target))
      notify('Jugador añadido al grupo')
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
        El jugador entra en el grupo que elijas, sin partidos: los partidos nuevos los añades tú en «Añadir partido».
      </p>
      {error && <p className="error" role="alert">{error}</p>}
      {available.length === 0
        ? <p className="muted">No quedan jugadores fuera de esta liga.</p>
        : (
          <div className="row">
            <select aria-label="Jugador a añadir" value={playerId} onChange={e => setPlayerId(e.target.value)} disabled={busy}>
              <option value="">Elige un jugador…</option>
              {available.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
            {groups.length > 1 && (
              <select aria-label="Grupo" value={target} onChange={e => setGroupId(e.target.value)} disabled={busy}>
                {groups.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
              </select>
            )}
            <button onClick={() => void add()} disabled={!playerId || busy}>Añadir</button>
          </div>
        )}
    </section>
  )
}
