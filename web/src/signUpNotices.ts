import type { PlayerRef, Slot } from './api/types'
import { addDaysIso, mondayOfIso } from './dates'

export interface SignUpWeek {
  /** `current`: esta semana; `next`: la próxima (solo si ya se han abierto sus entrenos). */
  key: 'current' | 'next'
  /** Personas (titular y apadrinados) que aún no están apuntadas a ningún entreno de esa semana. */
  missing: PlayerRef[]
}

/** «2026-10-19» y «18:00:00» → ya ha empezado (o es de un día anterior) respecto a `now` (hora local). */
const hasStarted = (s: Slot, todayIso: string, nowTime: string) => s.date < todayIso || (s.date === todayIso && s.start <= nowTime)

/**
 * Quién tiene que apuntarse todavía, por semana:
 * - Esta semana: solo cuentan los entrenos abiertos que aún no han empezado (los que ya han empezado o terminado no
 *   cuentan, ni para avisar ni para dar a alguien por apuntado). Sin entrenos así, no hay aviso.
 * - La próxima semana: solo si ya se ha abierto alguno de sus entrenos; cuenta cualquier entreno no cancelado.
 */
export function signUpWeeks(slots: readonly Slot[], people: readonly PlayerRef[], todayIso: string, nowTime: string): SignUpWeek[] {
  const thisMonday = mondayOfIso(todayIso)
  const nextMonday = addDaysIso(thisMonday, 7)
  const inWeek = (s: Slot, monday: string) => s.date >= monday && s.date <= addDaysIso(monday, 6)
  const missingFrom = (group: readonly Slot[]) => people.filter(p => !group.some(s => s.attendees.some(a => a.playerId === p.id)))

  const result: SignUpWeek[] = []

  const upcoming = slots.filter(s => inWeek(s, thisMonday) && s.status === 'Open' && !hasStarted(s, todayIso, nowTime))
  if (upcoming.length > 0) result.push({ key: 'current', missing: missingFrom(upcoming) })

  const next = slots.filter(s => inWeek(s, nextMonday) && s.status !== 'Cancelled')
  if (next.some(s => s.status === 'Open')) result.push({ key: 'next', missing: missingFrom(next) })

  return result.filter(w => w.missing.length > 0)
}
