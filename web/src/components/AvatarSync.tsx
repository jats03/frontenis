import { useEffect } from 'react'
import { api } from '../api/client'
import { loadAvatarSeeds } from '../avatarStore'
import { useAsync } from '../hooks'

/** Mantiene al día los avatares elegidos (la lista de jugadores ya los incluye y se sirve de la caché de lecturas). No pinta nada. */
export default function AvatarSync() {
  const players = useAsync(() => api.players.list(), [], '/api/players')
  useEffect(() => { if (players.data) loadAvatarSeeds(players.data) }, [players.data])
  return null
}
