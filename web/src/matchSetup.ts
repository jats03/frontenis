/**
 * Preparación de un partido de competición que se está jugando: color de cada pareja, quién saca y a cuántos tantos se juega
 * cada set. Se guarda en este dispositivo (como el marcador) y se borra al enviar el resultado; no viaja a la API.
 */
export type Side = 'h' | 'a'
export type SideColor = 'red' | 'blue'

export interface MatchSetup {
  /** Color de la pareja local; la visitante lleva el contrario. `null` = sin elegir. */
  homeColor: SideColor | null
  /** Quién saca al empezar (set 1). */
  serveStart: Side | null
  /** Quién saca en el desempate (se sortea aparte). */
  serveTie: Side | null
  /** Tantos de cada set: set 1, set 2 y desempate. */
  goals: [number, number, number]
}

export const DEFAULT_GOALS: [number, number, number] = [15, 15, 10]
export const MIN_GOAL = 1
export const MAX_GOAL = 99

export const emptySetup = (): MatchSetup => ({ homeColor: null, serveStart: null, serveTie: null, goals: [...DEFAULT_GOALS] })

const key = (matchId: number) => `frontenis.matchsetup.${matchId}`

const isSide = (v: unknown): v is Side => v === 'h' || v === 'a'
const isColor = (v: unknown): v is SideColor => v === 'red' || v === 'blue'
const isGoal = (v: unknown): v is number => Number.isInteger(v) && (v as number) >= MIN_GOAL && (v as number) <= MAX_GOAL

export function loadSetup(matchId: number): MatchSetup {
  try {
    const d = JSON.parse(localStorage.getItem(key(matchId)) ?? 'null') as Partial<MatchSetup> | null
    if (!d) return emptySetup()
    const g = Array.isArray(d.goals) && d.goals.length === 3 && d.goals.every(isGoal) ? (d.goals as [number, number, number]) : [...DEFAULT_GOALS] as [number, number, number]
    return { homeColor: isColor(d.homeColor) ? d.homeColor : null, serveStart: isSide(d.serveStart) ? d.serveStart : null,
      serveTie: isSide(d.serveTie) ? d.serveTie : null, goals: g }
  } catch { return emptySetup() }
}

export function saveSetup(matchId: number, s: MatchSetup) {
  try { localStorage.setItem(key(matchId), JSON.stringify(s)) } catch { /* sin almacenamiento */ }
}

export function clearSetup(matchId: number) {
  try { localStorage.removeItem(key(matchId)) } catch { /* sin almacenamiento */ }
}

export const otherSide = (s: Side): Side => (s === 'h' ? 'a' : 'h')

/** Color de cada pareja a partir del de la local. */
export const colorOf = (s: MatchSetup, side: Side): SideColor | null =>
  s.homeColor === null ? null : side === 'h' ? s.homeColor : (s.homeColor === 'red' ? 'blue' : 'red')

export const colorName = (c: SideColor) => (c === 'red' ? 'rojo' : 'azul')

/** Quién saca en un set (0 = primero, 1 = segundo, 2 = desempate): el sorteado, el otro, y el del desempate sorteado aparte. */
export function serverOf(s: MatchSetup, setIndex: number): Side | null {
  if (setIndex === 2) return s.serveTie
  if (!s.serveStart) return null
  return setIndex === 0 ? s.serveStart : otherSide(s.serveStart)
}

/** Quién ha ganado el set por llegar al tanteo (sin ventaja de dos puntos), o `null` si aún no lo ha hecho nadie. */
export function setWinner(home: number, away: number, goal: number): Side | null {
  if (home >= goal && home > away) return 'h'
  if (away >= goal && away > home) return 'a'
  return null
}
