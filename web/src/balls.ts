/** Tipos de bola con los que se juega un partido de entrenamiento. */
export type TrainingBall = 'Slow' | 'PreOlympic' | 'Olympic'

export const BALLS: { key: TrainingBall; label: string }[] = [
  { key: 'Slow', label: 'Lenta' },
  { key: 'PreOlympic', label: 'Preolímpica' },
  { key: 'Olympic', label: 'Olímpica' },
]

/** Bola que se propone por defecto. */
export const DEFAULT_BALL: TrainingBall = 'PreOlympic'

export const ballLabel = (b: TrainingBall) => BALLS.find(x => x.key === b)?.label ?? b
