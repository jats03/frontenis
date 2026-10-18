import { useSearchParams } from 'react-router-dom'
import PlayerHistoryPage from './PlayerHistoryPage'
import PlayersPage from './PlayersPage'

/** Portada de Jugadores: el histórico por defecto; la lista y los sorteos se piden con `?vista=`. */
export default function PlayersRoute() {
  const vista = useSearchParams()[0].get('vista')
  return vista === 'lista' || vista === 'sorteos' ? <PlayersPage /> : <PlayerHistoryPage />
}
