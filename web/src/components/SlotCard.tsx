import { useState, type ReactNode } from 'react'
import type { Slot } from '../api/types'
import { formatDate, formatOpening, formatTime } from '../dates'
import { slotStatusLabel } from '../slots'
import { WarningIcon } from './Icons'

interface Props {
  slot: Slot
  /** Si se indica, cada apuntado muestra un botón para quitarlo (administrador). */
  onRemoveAttendee?: (playerId: number) => void
  /** Acciones de la franja (administrador). */
  children?: ReactNode
}

/**
 * Tarjeta de una franja de entrenamiento. El aviso de «pocos jugadores» es visual: banda y triángulo de color
 * ámbar con un pequeño pulso; el texto solo aparece al tocar el triángulo (y siempre está para lectores de pantalla).
 */
export default function SlotCard({ slot, onRemoveAttendee, children }: Props) {
  const [hint, setHint] = useState(false)
  const status = slot.status.toLowerCase()
  const warning = 'Pocos jugadores para el entreno'
  return (
    <article className={`card slot-card status-${status}${slot.fewPlayers ? ' few' : ''}`}
      aria-label={`Entreno del ${formatDate(slot.date)}, ${formatTime(slot.start)} a ${formatTime(slot.end)}`}>
      <div className="slot-head">
        <span className="slot-time">{formatTime(slot.start)}–{formatTime(slot.end)}</span>
        <span className={`badge slot-${status}`}>{slotStatusLabel[slot.status]}</span>
        {slot.fewPlayers && (
          <button type="button" className="slot-warn" aria-label={warning} aria-expanded={hint}
            onClick={() => setHint(h => !h)}>
            <WarningIcon />
          </button>
        )}
      </div>
      {slot.fewPlayers && hint && <p className="slot-hint" role="status">{warning}</p>}
      {slot.status === 'Pending' && <p className="muted">Se abre el {formatOpening(slot.opensAt)}</p>}

      {slot.attendees.length > 0 ? (
        <>
          <ul className="chips" aria-label={`Apuntados (${slot.attendeeCount})`}>
            {slot.attendees.map(a => (
              <li key={a.playerId} className="chip">
                {a.name}
                {onRemoveAttendee && (
                  <button type="button" aria-label={`Quitar a ${a.name}`} onClick={() => onRemoveAttendee(a.playerId)}>✕</button>
                )}
              </li>
            ))}
          </ul>
          <p className="muted">{slot.attendeeCount} apuntado(s)</p>
        </>
      ) : (
        slot.status !== 'Cancelled' && <p className="muted">Nadie apuntado todavía</p>
      )}
      {children}
    </article>
  )
}
