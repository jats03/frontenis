import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'

interface Props {
  competitionId: number
  groupId: number
  round: number
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

/** Quita una jornada entera de un grupo (solo si no tiene partidos jugados). */
export default function RemoveRoundButton({ competitionId, groupId, round, onChanged, onError }: Props) {
  async function remove() {
    if (!(await confirmDelete(`¿Quitar la jornada ${round}?`,
      'Se borrarán sus partidos pendientes y las jornadas siguientes se renumerarán.'))) return
    try {
      await api.competitions.removeRound(competitionId, groupId, round)
      notify('Jornada eliminada')
      await onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : 'Error')
    }
  }

  return <button type="button" className="danger small" onClick={() => void remove()}>Quitar jornada {round}</button>
}
