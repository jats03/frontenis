import { TIMER_KEY } from '../timerStore'
import { formatTime, useStopwatch } from '../useStopwatch'

interface Props {
  /** Clave de almacenamiento: cada cronómetro (el general, o el de cada partido) guarda su estado aparte. */
  storageKey?: string
  defaultMinutes?: number
  /** Versión compacta (sin texto de ayuda), para ponerla junto a un marcador. */
  compact?: boolean
}

/** Cronómetro con cuenta atrás que sigue contando aunque se salga de la pantalla o de la app. */
export default function StopwatchPanel({ storageKey = TIMER_KEY, defaultMinutes = 10, compact = false }: Props) {
  const { minutes, status, remaining, total, start, pause, reset, changeMinutes } = useStopwatch(storageKey, defaultMinutes, true)
  const low = status !== 'idle' && remaining <= 10_000
  const locked = status === 'running' || status === 'paused'

  return (
    <section className={`card timer ${compact ? 'timer-compact' : ''}`}>
      <div className={`timer-display ${low ? 'low' : ''} ${status === 'finished' ? 'done' : ''}`}
        role="timer" aria-live="off">
        {formatTime(remaining)}
      </div>
      {status === 'finished' && <p className="center"><strong>¡Tiempo!</strong></p>}

      <div className="row center">
        <label>
          Minutos
          <input type="number" min={1} max={999} value={minutes} disabled={locked}
            onChange={e => changeMinutes(Number(e.target.value))} />
        </label>
      </div>
      <div className="row center presets">
        {[5, 10, 15, 20].map(m => (
          <button key={m} className="secondary" disabled={locked} onClick={() => changeMinutes(m)}>{m} min</button>
        ))}
      </div>

      <div className="row center">
        {status === 'running'
          ? <button onClick={pause}>Pausar</button>
          : <button onClick={start}>{status === 'paused' ? 'Reanudar' : 'Iniciar'}</button>}
        <button className="secondary" onClick={reset} disabled={status === 'idle' && remaining === total}>Reiniciar</button>
      </div>
      {!compact && (
        <p className="muted center">Sigue contando aunque salgas de la pantalla o de la app. Si pasan más de 15 minutos sin abrirla, se detiene solo.</p>
      )}
    </section>
  )
}
