import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, Player } from '../api/types'
import PlayerPicker from './PlayerPicker'

interface Props {
  detail: CompetitionDetail
  players: Player[]
  onChanged: () => Promise<void>
  onPlayersChanged: () => Promise<void>
}

/** Liga rotatoria en borrador: se eligen los jugadores y se sortean los grupos y las jornadas. */
export default function RotatingDrawPanel({ detail, players, onChanged, onPlayersChanged }: Props) {
  const { summary } = detail
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [swapping, setSwapping] = useState<Set<number>>(new Set())

  const needed = summary.pairCount
  const chosen = players.filter(p => selected.has(p.id))
  const forwards = chosen.filter(p => p.position === 'Delantero').length
  const backs = chosen.length - forwards

  async function draw() {
    setBusy(true); setError(null)
    try { await api.competitions.drawRotating(summary.id, [...selected]); notify('Grupos y jornadas creados'); await onChanged() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  async function swap(p: Player) {
    const next = p.position === 'Delantero' ? 'Zaguero' : 'Delantero'
    setSwapping(prev => new Set(prev).add(p.id)); setError(null)
    try { await api.players.update(p.id, p.name, next, p.groupNumber ?? null); await onPlayersChanged() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setSwapping(prev => { const n = new Set(prev); n.delete(p.id); return n }) }
  }

  return (
    <section className="card">
      <h3>Jugadores de la liga</h3>
      <p className="muted">
        Elige al menos {needed} jugadores ({summary.groupCount} grupo(s) de {summary.pairsPerGroup}); si hay más,
        los sobrantes van al último grupo.
        Se reparten al azar entre los grupos y en cada jornada se forman parejas nuevas
        (da igual el puesto) hasta que todos hayan jugado con todos{summary.rounds > 0 ? `, con ${summary.rounds} jornadas` : ''}.
      </p>
      <PlayerPicker players={players} selected={selected} onChange={setSelected}
        onSwap={p => void swap(p)} swappingIds={swapping} />
      <p className={chosen.length >= needed ? 'muted' : 'error'}>
        Seleccionados: {chosen.length} (mínimo {needed}) · {forwards} delanteros · {backs} zagueros
      </p>
      {error && <p className="error">{error}</p>}
      <button disabled={busy || chosen.length < needed} onClick={() => void draw()}>
        Sortear grupos y jornadas
      </button>
    </section>
  )
}
