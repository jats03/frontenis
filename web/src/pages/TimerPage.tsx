import { useDocumentTitle } from '../hooks'
import { useCallback, useEffect, useRef, useState } from 'react'
import { loadTimer, saveTimer, TIMER_CHANGED, type TimerStatus } from '../timerStore'

type Status = TimerStatus

function format(ms: number) {
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

interface View { minutes: number; status: Status; remaining: number; endAt: number }

/** Estado del cronómetro a partir de lo guardado: uno en marcha sigue contando con la hora de fin guardada. */
function restore(): View {
  const t = loadTimer()
  if (!t) return { minutes: 10, status: 'idle', remaining: 10 * 60_000, endAt: 0 }
  if (t.status === 'running') {
    const left = t.endAt - Date.now()
    return left <= 0
      ? { minutes: t.minutes, status: 'finished', remaining: 0, endAt: t.endAt }
      : { minutes: t.minutes, status: 'running', remaining: left, endAt: t.endAt }
  }
  return { minutes: t.minutes, status: t.status, remaining: t.remaining, endAt: t.endAt }
}

export default function TimerPage() {
  useDocumentTitle('Cronómetro')
  const [initial] = useState(restore)
  const [minutes, setMinutes] = useState(initial.minutes)
  const [status, setStatus] = useState<Status>(initial.status)
  const [remaining, setRemaining] = useState(initial.remaining)
  const endAt = useRef(initial.endAt)

  const persist = useCallback((s: Status, m: number, left: number) => {
    saveTimer({ minutes: m, status: s, endAt: endAt.current, remaining: left })
  }, [])

  // Si por fuera (la regla de los 15 minutos) cambia el estado guardado, la pantalla lo recoge.
  useEffect(() => {
    const sync = () => { const v = restore(); setMinutes(v.minutes); setStatus(v.status); setRemaining(v.remaining); endAt.current = v.endAt }
    window.addEventListener(TIMER_CHANGED, sync)
    return () => window.removeEventListener(TIMER_CHANGED, sync)
  }, [])

  useEffect(() => {
    if (status !== 'running') return
    const id = setInterval(() => {
      const left = endAt.current - Date.now()
      if (left <= 0) { setRemaining(0); setStatus('finished'); persist('finished', minutes, 0); beep() }
      else setRemaining(left)
    }, 200)
    return () => clearInterval(id)
  }, [status, minutes, persist])

  const total = Math.max(1, Math.round(minutes)) * 60_000

  const start = () => {
    const base = status === 'idle' || status === 'finished' ? total : remaining
    endAt.current = Date.now() + base
    setRemaining(base)
    setStatus('running')
    persist('running', minutes, base)
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

  const low = status !== 'idle' && remaining <= 10_000

  return (
    <>
      <h1>Cronómetro</h1>
      <section className="card timer">
        <div className={`timer-display ${low ? 'low' : ''} ${status === 'finished' ? 'done' : ''}`}
          role="timer" aria-live="off">
          {format(remaining)}
        </div>
        {status === 'finished' && <p className="center"><strong>¡Tiempo!</strong></p>}

        <div className="row center">
          <label>
            Minutos
            <input type="number" min={1} max={999} value={minutes} disabled={status === 'running' || status === 'paused'}
              onChange={e => changeMinutes(Number(e.target.value))} />
          </label>
        </div>
        <div className="row center presets">
          {[5, 10, 15, 20].map(m => (
            <button key={m} className="secondary" disabled={status === 'running' || status === 'paused'}
              onClick={() => changeMinutes(m)}>{m} min</button>
          ))}
        </div>

        <div className="row center">
          {status === 'running'
            ? <button onClick={pause}>Pausar</button>
            : <button onClick={start}>{status === 'paused' ? 'Reanudar' : 'Iniciar'}</button>}
          <button className="secondary" onClick={reset} disabled={status === 'idle' && remaining === total}>Reiniciar</button>
        </div>
        <p className="muted center">Sigue contando aunque salgas de la pantalla o de la app. Si pasan más de 15 minutos sin abrirla, se detiene solo.</p>
      </section>
    </>
  )
}
