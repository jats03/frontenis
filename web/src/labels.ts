import type { CompetitionStatus, CompetitionType, Position } from './api/types'

export const statusLabel: Record<CompetitionStatus, string> = {
  Draft: 'Inscripción',
  GroupStage: 'Fase de grupos',
  Playoffs: 'Fase final',
  Finished: 'Finalizada',
}
/** Texto del estado; en la liga rotatoria no hay «fase de grupos»: se dice «En curso». */
export const statusText = (status: CompetitionStatus, type: CompetitionType) =>
  type === 'RotatingLeague' && status === 'GroupStage' ? 'En curso' : statusLabel[status]

export const typeLabel: Record<CompetitionType, string> = {
  League: 'Liga', Tournament: 'Torneo', RotatingLeague: 'Liga rotatoria',
}
export const positionLabel: Record<Position, string> = { Delantero: 'Delantero', Zaguero: 'Zaguero' }

export function roundName(round: number, totalRounds: number): string {
  const fromEnd = totalRounds - round
  if (fromEnd === 0) return 'Final'
  if (fromEnd === 1) return 'Semifinales'
  if (fromEnd === 2) return 'Cuartos de final'
  return `Ronda ${round}`
}
