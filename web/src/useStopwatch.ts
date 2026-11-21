import { useCallback, useEffect, useRef, useState } from 'react'
import { loadTimer, saveTimer, TIMER_CHANGED, TIMER_KEY, type TimerStatus } from './timerStore'

export function formatTime(ms: number) {
  const total = Math.max(0, Math.ceil(ms / 1000))
  const m = Math.floor(total / 60)
  const s = total % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

function beep() {
  try {
    const ctx = new AudioContext()
    for (let i = 0; i < 3; i++) {
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()
      osc.connect(gain); gain.connect(ctx.destination)
      osc.frequency.value = 880
      const t = ctx.currentTime + i * 0.4
      gain.gain.setValueAtTime(0.3, t)
      gain.gain.exponentialRampToValueAtTime(0.001, t + 0.3)
      osc.start(t); osc.stop(t + 0.3)
    }
  } catch { /* sin audio disponible */ }
}

interface View { minutes: number; status: TimerStatus; remaining: number; endAt: number }

/** Estado del cronómetro a partir de lo guardado: uno en marcha sigue contando con la hora de fin guardada. */
function restore(key: string, defaultMinutes: number): View {
  const t = loadTimer(key)
  if (!t) return { minutes: defaultMinutes, status: 'idle', remaining: defaultMinutes * 60_000, endAt: 0 }
  if (t.status === 'running') {
    const left = t.endAt - Date.now()
    return left <= 0
      ? { minutes: t.minutes, status: 'finished', remaining: 0, endAt: t.endAt }
      : { minutes: t.minutes, status: 'running', remaining: left, endAt: t.endAt }
  }
  return { minutes: t.minutes, status: t.status, remaining: t.remaining, endAt: t.endAt }
}

/**
 * Cronómetro con cuenta atrás cuyo estado vive en el navegador (con la hora de fin), así que sigue contando aunque se salga
 * de la pantalla. Varias pantallas pueden usar el mismo (el panel grande y la barra fija): se avisan entre sí con
 * TIMER_CHANGED. Solo una debe pasar `sound` para que el aviso sonoro no suene dos veces.
 */
export function useStopwatch(storageKey: string = TIMER_KEY, defaultMinutes = 10, sound = true) {
  const [initial] = useState(() => restore(storageKey, defaultMinutes))
  const [minutes, setMinutes] = useState(initial.minutes)
  const [status, setStatus] = useState<TimerStatus>(initial.status)
  const [remaining, setRemaining] = useState(initial.remaining)
  const endAt = useRef(initial.endAt)

  const persist = useCallback((s: TimerStatus, m: number, left: number) => {
    saveTimer({ minutes: m, status: s, endAt: endAt.current, remaining: left }, storageKey)
    window.dispatchEvent(new Event(TIMER_CHANGED)) // las otras pantallas del mismo cronómetro lo recogen
  }, [storageKey])

  // Si por fuera (la regla de los 15 minutos, o la otra pantalla) cambia el estado guardado, esta lo recoge.
  useEffect(() => {
    const sync = () => {
      const v = restore(storageKey, defaultMinutes)
      setMinutes(v.minutes); setStatus(v.status); setRemaining(v.remaining); endAt.current = v.endAt
    }
    window.addEventListener(TIMER_CHANGED, sync)
    return () => window.removeEventListener(TIMER_CHANGED, sync)
  }, [storageKey, defaultMinutes])

  useEffect(() => {
    if (status !== 'running') return
    const id = setInterval(() => {
      const left = endAt.current - Date.now()
      if (left <= 0) {
        setRemaining(0); setStatus('finished')
        if (sound) { saveTimer({ minutes, status: 'finished', endAt: endAt.current, remaining: 0 }, storageKey); beep() }
      } else setRemaining(left)
    }, 200)
    return () => clearInterval(id)
  }, [status, minutes, sound, storageKey])

  const total = Math.max(1, Math.round(minutes)) * 60_000

  const start = () => {
    const base = status === 'idle' || status === 'finished' ? total : remaining
    endAt.current = Date.now() + base
    setRemaining(base); setStatus('running'); persist('running', minutes, base)
  }
  const pause = () => {
    const left = Math.max(0, endAt.current - Date.now())
    setRemaining(left); setStatus('paused'); persist('paused', minutes, left)
  }
  const reset = () => { setStatus('idle'); setRemaining(total); persist('idle', minutes, total) }
  const changeMinutes = (value: number) => {
    const m = Math.min(999, Math.max(1, Math.round(value) || 1))
    setMinutes(m)
    if (status === 'idle' || status === 'finished') { setRemaining(m * 60_000); setStatus('idle'); persist('idle', m, m * 60_000) }
  }

  return { minutes, status, remaining, total, start, pause, reset, changeMinutes }
}
