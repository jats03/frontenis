import { formatTime, useStopwatch } from '../useStopwatch'

interface Props {
  storageKey: string
  /** Se muestra cuando el cronómetro grande ya no se ve en pantalla. */
  visible: boolean
}

/**
 * Barra mínima del mismo cronómetro (hora y botón de iniciar/pausar) que se queda fija arriba al bajar por la pantalla.
 * Ocupa cero alto en el flujo de la página, así que aparecer y desaparecer no mueve nada.
 */
export default function StopwatchMini({ storageKey, visible }: Props) {
  const { status, remaining, start, pause } = useStopwatch(storageKey, 10, false) // el aviso sonoro lo da el panel grande
  const low = status !== 'idle' && remaining <= 10_000
  return (
    <div className="timer-mini-wrap">
      <div className={`timer-mini${visible ? ' on' : ''}${low ? ' low' : ''}`} aria-hidden={!visible}>
        <span className="timer-mini-time" role="timer" aria-live="off">{status === 'finished' ? '¡Tiempo!' : formatTime(remaining)}</span>
        {status === 'running'
          ? <button type="button" tabIndex={visible ? 0 : -1} onClick={pause}>Pausar</button>
          : <button type="button" tabIndex={visible ? 0 : -1} onClick={start}>{status === 'paused' ? 'Reanudar' : 'Iniciar'}</button>}
      </div>
    </div>
  )
}
