import { Link } from 'react-router-dom'
import type { DeviceMe, Slot } from '../api/types'
import { formatTime, nowTime, parseDate, todayIso } from '../dates'
import { myUpcomingSlots } from '../signUpNotices'
import { CalendarIcon } from './Icons'

const joinNames = (names: string[]) => (names.length < 2 ? names.join('') : `${names.slice(0, -1).join(', ')} y ${names[names.length - 1]}`)

/** «Lun 23» a partir de una fecha. */
function dayLabel(iso: string) {
  const d = parseDate(iso)
  const w = d.toLocaleDateString('es-ES', { weekday: 'short' }).replace('.', '')
  return `${w.charAt(0).toUpperCase()}${w.slice(1)} ${d.getDate()}`
}

/**
 * Fila compacta, bajo el botón de reservas, con los próximos entrenos a los que está apuntada la persona (y sus apadrinados):
 * una etiqueta por entreno con el día y la hora. Sin ningún entreno apuntado no sale nada.
 */
export default function MyTrainings({ me, slots }: { me: DeviceMe; slots: Slot[] | null | undefined }) {
  if (!slots) return null
  const mine = myUpcomingSlots(slots, [me.owner, ...me.delegates], todayIso(), nowTime())
  if (mine.length === 0) return null
  return (
    <section className="my-slots" aria-label="Entrenos a los que estás apuntado">
      <span className="my-slots-label"><CalendarIcon />Apuntado a</span>
      <ul>
        {mine.map(({ slot, who }) => {
          const others = who.filter(p => p.id !== me.owner.id).map(p => p.name)
          const you = who.some(p => p.id === me.owner.id)
          const names = others.length > 0 ? joinNames([...(you ? ['tú'] : []), ...others]) : ''
          const text = `${dayLabel(slot.date)} · ${formatTime(slot.start)}`
          return (
            <li key={slot.id}>
              <Link className="my-chip" to="/reservas" aria-label={`${text}${names ? `, ${names}` : ''}. Ver las reservas`}>
                <strong>{dayLabel(slot.date)}</strong><span>{formatTime(slot.start)}</span>{names && <em>{names}</em>}
              </Link>
            </li>
          )
        })}
      </ul>
    </section>
  )
}
