import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'

interface Props {
  matchId: number
  /** Si el partido ya tiene resultado, la confirmación lo avisa. */
  played: boolean
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

/** Borra un partido de una jornada de una liga rotatoria (con o sin resultado). */
export default function RemoveMatchButton({ matchId, played, onChanged, onError }: Props) {
  async function remove() {
    const detail = played
      ? 'El partido ya tiene resultado: se perderá y cambiará la clasificación.'
      : 'Se quitará de la jornada; si era el único de esa jornada, la jornada desaparece.'
    if (!(await confirmDelete('¿Borrar este partido?', detail))) return
    try {
      await api.matches.remove(matchId)
      notify('Partido borrado')
      await onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : 'Error')
    }
  }

  return <button type="button" className="danger small" onClick={() => void remove()}>Borrar partido</button>
}
