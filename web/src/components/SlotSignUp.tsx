import type { DeviceMe, Slot } from '../api/types'
import { formatDate, formatTime } from '../dates'
import { UsersIcon } from './Icons'

interface Props {
  slot: Slot
  me: DeviceMe
  /** Apunta (o quita, si ya estaba apuntado) al jugador. */
  onToggle: (playerId: number, attending: boolean) => void
  /** Apunta a todos los que faltan (o quita a todos, si ya estaban todos). Solo con apadrinados. */
  onToggleAll?: (playerIds: number[], attending: boolean) => void
  /** Hay una operación en curso sobre esta franja. */
  busy?: boolean
}

/**
 * Botones para apuntarse y quitarse (a uno mismo y a los apadrinados) en una franja abierta. Con apadrinados, un botón
 * grande arriba apunta a todos de una vez (o quita a todos si ya estaban).
 */
export default function SlotSignUp({ slot, me, onToggle, onToggleAll, busy }: Props) {
  if (slot.status !== 'Open') return null
  const attending = new Set(slot.attendees.map(a => a.playerId))
  const where = `entreno del ${formatDate(slot.date)}, ${formatTime(slot.start)}–${formatTime(slot.end)}`
  const people = [{ ...me.owner, self: true }, ...me.delegates.map(d => ({ ...d, self: false }))]
  const missing = people.filter(p => !attending.has(p.id))
  const all = people.length > 1 && onToggleAll
  const allIn = missing.length === 0
  const allLabel = allIn ? 'Quitar a todos' : `Apuntar a todos (${missing.length})`
  return (
    <div className={`slot-actions row${all ? ' multi' : ''}`}>
      {all && (
        <button type="button" className={`slot-all${allIn ? ' secondary' : ''}`} disabled={busy}
          aria-label={`${allLabel}: ${people.map(p => p.name).join(', ')} (${where})`}
          onClick={() => onToggleAll(allIn ? people.map(p => p.id) : missing.map(p => p.id), allIn)}>
          <UsersIcon />{allLabel}
        </button>
      )}
      {people.map(p => {
        const yes = attending.has(p.id)
        const label = p.self ? (yes ? 'Quitarme' : 'Apuntarme') : `${yes ? 'Quitar a' : 'Apuntar a'} ${p.name}`
        return (
          <button key={p.id} type="button" className={yes || all ? 'secondary' : undefined} disabled={busy}
            aria-pressed={yes} aria-label={`${label} (${where})`} onClick={() => onToggle(p.id, yes)}>{label}</button>
        )
      })}
    </div>
  )
}
