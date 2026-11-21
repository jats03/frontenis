import { formatTime, useStopwatch } from '../useStopwatch'

/**
 * La hora del cronómetro y su botón de iniciar/pausar, para la cabecera de la sección plegable: así se ve y se controla
 * aunque la sección esté plegada. Es el que da el aviso sonoro al acabar el tiempo (siempre está montado).
 */
export default function TimerHeader({ storageKey }: { storageKey: string }) {
  const { status, remaining, start, pause } = useStopwatch(storageKey, undefined, true)
  const low = status !== 'idle' && remaining <= 10_000
  return (
    <div className={`timer-head${low ? ' low' : ''}${status === 'finished' ? ' done' : ''}`}>
      <span className="timer-head-time" role="timer" aria-live="off">{status === 'finished' ? '¡Tiempo!' : formatTime(remaining)}</span>
      {status === 'running'
        ? <button type="button" onClick={pause}>Pausar</button>
        : <button type="button" onClick={start}>{status === 'paused' ? 'Reanudar' : 'Iniciar'}</button>}
    </div>
  )
}
