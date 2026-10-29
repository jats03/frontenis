/** Tipos de bola con los que se juega un partido de entrenamiento. */
export type BallType = 'Slow' | 'PreOlympic' | 'Olympic'

export const BALLS: { key: BallType; label: string }[] = [
  { key: 'Slow', label: 'Lenta' },
  { key: 'PreOlympic', label: 'Preolímpica' },
  { key: 'Olympic', label: 'Olímpica' },
]

/** Bola que se propone por defecto. */
export const DEFAULT_BALL: BallType = 'PreOlympic'

export const ballLabel = (b: BallType) => BALLS.find(x => x.key === b)?.label ?? b
