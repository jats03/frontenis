import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { DeviceMe } from '../api/types'
import { addDaysIso, mondayOfIso, nowTime, todayIso } from '../dates'
import { useAsync } from '../hooks'
import { isIncomplete } from '../clothing'
import { signUpWeeks } from '../signUpNotices'

/** Nombres con «y»: «Ana», «Ana y Luis», «Ana, Luis y Eva». */
const joinNames = (names: string[]) => (names.length < 2 ? names.join('') : `${names.slice(0, -1).join(', ')} y ${names[names.length - 1]}`)

/**
 * Para quien tiene identificado su dispositivo: avisos de lo que falta (tallas de ropa y apuntarse a algún entreno,
 * también de sus apadrinados): uno para esta semana y otro para la próxima si ya se han abierto sus entrenos.
 */
export default function HomeNotices({ me }: { me: DeviceMe }) {
  // Esta semana y la próxima, de lunes a domingo.
  const week = useMemo(() => { const from = mondayOfIso(todayIso()); return { from, to: addDaysIso(from, 13) } }, [])
  const sizes = useAsync(() => api.device.sizes(), [me.owner.id, me.delegates.length])
  const slots = useAsync(() => api.slots.list(week.from, week.to), [week.from, week.to])

  const rows = sizes.data ?? []
  const missingSizes = rows.filter(isIncomplete)

  const people = [me.owner, ...me.delegates]
  const weeks = slots.data ? signUpWeeks(slots.data, people, todayIso(), nowTime()) : []

  const notices: { key: string; text: string; to?: string; cta?: string }[] = []
  if (missingSizes.length > 0) {
    notices.push({
      key: 'tallas',
      text: missingSizes.some(r => r.playerId === me.owner.id) && missingSizes.length === 1
        ? 'Aún no has indicado tu talla de ropa.'
        : `Falta la talla de ropa de ${joinNames(missingSizes.map(r => r.name))}.`,
      to: '/tallas', cta: 'Indicar tallas',
    })
  }
  for (const w of weeks) {
    const you = w.missing.some(p => p.id === me.owner.id)
    const others = w.missing.filter(p => p.id !== me.owner.id).map(p => p.name)
    const alone = you && others.length === 0
    const who = joinNames([...(you ? ['tú'] : []), ...others])
    notices.push(w.key === 'current'
      ? { key: 'entreno', text: alone ? 'Aún no te has apuntado a ningún entreno de esta semana.' : `Sin ningún entreno apuntado esta semana: ${who}.` }
      : { key: 'entreno-proxima', text: alone
          ? 'Ya están abiertos los entrenos de la próxima semana y aún no te has apuntado a ninguno.'
          : `Ya están abiertos los entrenos de la próxima semana. Sin ninguno apuntado: ${who}.` })
  }

  if (notices.length === 0) return null
  return (
    <section aria-label="Avisos" className="notices">
      {notices.map(n => (
        <div key={n.key} className="notice" role="status">
          <svg className="notice-icon" viewBox="0 0 24 24" width="22" height="22" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z" /><path d="M12 9v4" /><path d="M12 17h.01" />
          </svg>
          <span className="notice-text">{n.text}</span>
          {n.to && <Link className="button small" to={n.to}>{n.cta}</Link>}
        </div>
      ))}
    </section>
  )
}
