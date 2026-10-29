import { ballLabel, type BallType } from '../balls'

/** Etiqueta pequeña con la bola con la que se jugó un partido. */
export default function BallTag({ ball }: { ball: BallType }) {
  return <span className={`ball-tag ball-${ball}`}>{ballLabel(ball)}</span>
}
