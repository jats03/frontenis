import type { DeviceMe, Slot } from '../api/types'
import { formatDate, formatTime } from '../dates'

interface Props {
  slot: Slot
  me: DeviceMe
  /** Apunta (o quita, si ya estaba apuntado) al jugador. */
  onToggle: (playerId: number, attending: boolean) => void
}

/** Botones para apuntarse y quitarse (a uno mismo y a los apadrinados) en una franja abierta. */
export default function SlotSignUp({ slot, me, onToggle }: Props) {
  if (slot.status !== 'Open') return null
  const attending = new Set(slot.attendees.map(a => a.playerId))
  const where = `entreno del ${formatDate(slot.date)}, ${formatTime(slot.start)}–${formatTime(slot.end)}`
  const people = [{ ...me.owner, self: true }, ...me.delegates.map(d => ({ ...d, self: false }))]
  return (
    <div className="slot-actions row">
      {people.map(p => {
        const yes = attending.has(p.id)
        const label = p.self ? (yes ? 'Quitarme' : 'Apuntarme') : `${yes ? 'Quitar a' : 'Apuntar a'} ${p.name}`
        return (
          <button key={p.id} type="button" className={yes ? 'secondary' : undefined}
            aria-pressed={yes} aria-label={`${label} (${where})`} onClick={() => onToggle(p.id, yes)}>{label}</button>
        )
      })}
    </div>
  )
}
