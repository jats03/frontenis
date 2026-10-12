import { useEffect, useRef, useState } from 'react'

type Status = 'idle' | 'running' | 'paused' | 'finished'

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

export default function TimerPage() {
  const [minutes, setMinutes] = useState(10)
  const [status, setStatus] = useState<Status>('idle')
  const [remaining, setRemaining] = useState(10 * 60_000)
  const endAt = useRef(0)

  useEffect(() => {
    if (status !== 'running') return
    const id = setInterval(() => {
      const left = endAt.current - Date.now()
      if (left <= 0) { setRemaining(0); setStatus('finished'); beep() }
      else setRemaining(left)
    }, 200)
    return () => clearInterval(id)
  }, [status])

  const total = Math.max(1, Math.round(minutes)) * 60_000

  const start = () => {
    const base = status === 'idle' || status === 'finished' ? total : remaining
    endAt.current = Date.now() + base
    setRemaining(base)
    setStatus('running')
  }
  const pause = () => { setRemaining(Math.max(0, endAt.current - Date.now())); setStatus('paused') }
  const reset = () => { setStatus('idle'); setRemaining(total) }
  const changeMinutes = (value: number) => {
    const m = Math.min(999, Math.max(1, Math.round(value) || 1))
    setMinutes(m)
    if (status === 'idle' || status === 'finished') { setRemaining(m * 60_000); setStatus('idle') }
  }

  const low = status !== 'idle' && remaining <= 10_000

  return (
    <>
      <h2>Cronómetro</h2>
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
      </section>
    </>
  )
}
