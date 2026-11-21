import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import PlayerHistoryPage from './PlayerHistoryPage'
import PlayersPage from './PlayersPage'

/** Portada de Jugadores: el histórico por defecto; la lista (solo administrador) y los sorteos se piden con `?vista=`. */
export default function PlayersRoute() {
  const { isAdmin } = useAuth()
  const vista = useSearchParams()[0].get('vista')
  return (vista === 'lista' && isAdmin) || vista === 'sorteos' ? <PlayersPage /> : <PlayerHistoryPage />
}
