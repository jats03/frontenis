import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, Group } from '../api/types'
import Disclosure from './Disclosure'

interface Props {
  detail: CompetitionDetail
  group: Group
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

/** Jugadores de un grupo de la liga rotatoria; se puede quitar a quien aún no ha jugado ningún partido (el grupo conserva al menos 4). */
export default function GroupPlayersAdmin({ detail, group, onChanged, onError }: Props) {
  const players = group.players ?? []
  const pairById = new Map(detail.pairs.map(p => [p.entryId, p]))
  const played = new Set<number>()
  for (const m of detail.matches) {
    if (m.groupId !== group.id || !m.isPlayed) continue
    for (const entryId of [m.homeEntryId, m.awayEntryId]) {
      const pair = entryId == null ? undefined : pairById.get(entryId)
      if (pair) { played.add(pair.player1.id); played.add(pair.player2.id) }
    }
  }
  const canRemove = players.length > 4

  async function remove(id: number, name: string) {
    if (!(await confirmDelete(`¿Quitar a ${name} del grupo?`, 'Se borrarán sus partidos pendientes y las jornadas que se queden vacías.'))) return
    try {
      await api.competitions.removePlayer(detail.summary.id, group.id, id)
      notify(`${name} ya no está en el grupo`)
      await onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : 'Error')
    }
  }

  return (
    <Disclosure summary={`Jugadores del grupo (${players.length})`} defaultOpen={false}>
      <ul className="plain">
        {players.map(p => (
          <li key={p.id} className="row between">
            <span>{p.name}{played.has(p.id) ? <span className="muted"> · ya ha jugado</span> : null}</span>
            {!played.has(p.id) && canRemove && (
              <button type="button" className="danger small" onClick={() => void remove(p.id, p.name)}>Quitar</button>
            )}
          </li>
        ))}
      </ul>
      {!canRemove && <p className="muted">Cada grupo necesita al menos 4 jugadores.</p>}
    </Disclosure>
  )
}
