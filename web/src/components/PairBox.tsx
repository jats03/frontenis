import type { ReactNode } from 'react'

interface Props {
  side: 'A' | 'B'
  children: ReactNode
}

/** Bloque de una pareja (A o B) con sus jugadores y su marcador dentro, de color propio, para no mezclar parejas. */
export default function PairBox({ side, children }: Props) {
  return (
    <div className={`pair-box pair-${side}`} role="group" aria-label={`Pareja ${side}`}>
      <h3 className="pair-title">Pareja {side}</h3>
      {children}
    </div>
  )
}
