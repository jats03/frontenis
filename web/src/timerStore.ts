/**
 * El cronómetro sigue corriendo aunque se salga de la pantalla o de la aplicación: su estado se guarda en el
 * navegador con la hora de fin (no con cuentas atrás en memoria). Si pasan más de 15 minutos sin abrir la app y
 * el cronómetro no había llegado a su fin, se detiene solo.
 */
export type TimerStatus = 'idle' | 'running' | 'paused' | 'finished'
export interface TimerSave { minutes: number; status: TimerStatus; endAt: number; remaining: number }

export const TIMER_KEY = 'frontenis.timer'
const SEEN_KEY = 'frontenis.lastSeen'
/** Sin abrir la app durante este tiempo, un cronómetro en marcha se detiene. */
export const AWAY_MS = 15 * 60_000
/** Aviso a la pantalla del cronómetro de que su estado guardado ha cambiado por fuera. */
export const TIMER_CHANGED = 'frontenis:timer-changed'

export function loadTimer(): TimerSave | null {
  try {
    const t = JSON.parse(localStorage.getItem(TIMER_KEY) ?? 'null') as TimerSave | null
    return t && typeof t.minutes === 'number' && typeof t.endAt === 'number' ? t : null
  } catch { return null }
}

export function saveTimer(t: TimerSave) {
  try { localStorage.setItem(TIMER_KEY, JSON.stringify(t)) } catch { /* sin almacenamiento */ }
}

const lastSeen = (): number => {
  try { return Number(localStorage.getItem(SEEN_KEY)) || 0 } catch { return 0 }
}
const touch = () => { try { localStorage.setItem(SEEN_KEY, String(Date.now())) } catch { /* sin almacenamiento */ } }

/** Si el cronómetro está en marcha y se lleva más de 15 minutos sin abrir la app, lo detiene (vuelve a cero). */
export function stopIfAway(now = Date.now()): boolean {
  const t = loadTimer()
  const seen = lastSeen()
  if (!t || t.status !== 'running' || seen === 0 || t.endAt <= now || now - seen <= AWAY_MS) return false
  saveTimer({ ...t, status: 'idle', remaining: t.minutes * 60_000 })
  return true
}

// Al abrir la app (antes de que nada marque «visto»), se comprueba si estuvo demasiado tiempo sin abrirse.
stopIfAway()

/** Marca la app como «vista» mientras está abierta y a la vista; al volver tras un rato fuera, aplica la regla de los 15 minutos. */
export function startPresence(): () => void {
  touch()
  const beat = setInterval(() => { if (document.visibilityState === 'visible') touch() }, 15_000)
  const onVisible = () => {
    if (document.visibilityState !== 'visible') { touch(); return }
    if (stopIfAway()) window.dispatchEvent(new Event(TIMER_CHANGED))
    touch()
  }
  document.addEventListener('visibilitychange', onVisible)
  return () => { clearInterval(beat); document.removeEventListener('visibilitychange', onVisible) }
}
