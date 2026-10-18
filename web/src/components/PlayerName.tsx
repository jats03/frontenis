import type { ReactNode } from 'react'
import PlayerAvatar from './PlayerAvatar'

interface Props {
  /** Jugador (o jugadores, si el nombre es el de una pareja) cuyo muñeco se muestra a la izquierda. */
  playerId: number | number[]
  /** El nombre tal y como se mostraba hasta ahora. */
  children: ReactNode
  size?: number
}

/** Nombre de jugador precedido de su logo de perfil. El muñeco es decorativo: el nombre sigue siendo texto normal. */
export default function PlayerName({ playerId, children, size = 22 }: Props) {
  const ids = Array.isArray(playerId) ? playerId : [playerId]
  return (
    <span className="pname">
      <span className="pname-faces">
        {ids.map(id => <PlayerAvatar key={id} playerId={id} size={size} />)}
      </span>
      <span>{children}</span>
    </span>
  )
}
