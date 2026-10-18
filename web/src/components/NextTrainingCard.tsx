import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { Slot } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { addDaysIso, formatDate, formatOpening, todayIso } from '../dates'
import { useAsync } from '../hooks'
import { useDevice } from '../useDevice'
import EmptyState from './EmptyState'
import SlotCard from './SlotCard'
import SlotSignUp from './SlotSignUp'

/** El primer entreno con las inscripciones abiertas; si no hay, el próximo por abrir. */
function nextSlot(slots: Slot[]): Slot | undefined {
  const open = slots.filter(s => s.status === 'Open')
  const pending = slots.filter(s => s.status === 'Pending')
  const byTime = (a: Slot, b: Slot) => a.date.localeCompare(b.date) || a.start.localeCompare(b.start)
  return [...open].sort(byTime)[0] ?? [...pending].sort(byTime)[0]
}

/**
 * Portada: lo primero que se ve. Muestra el próximo entreno abierto con los apuntados y, si el dispositivo
 * está identificado, los botones para apuntarse sin salir de la portada.
 */
export default function NextTrainingCard() {
  const { isAdmin } = useAuth()
  const { me } = useDevice()
  const from = useMemo(() => todayIso(), [])
  const slots = useAsync(() => api.slots.list(from, addDaysIso(from, 13)), [from])
  const [error, setError] = useState<string | null>(null)
  const [announcement, setAnnouncement] = useState('')
  const next = useMemo(() => nextSlot(slots.data ?? []), [slots.data])

  async function toggle(slot: Slot, playerId: number, attending: boolean) {
    setError(null)
    try {
      const updated = attending ? await api.slots.unattend(slot.id, playerId) : await api.slots.attend(slot.id, playerId)
      slots.setData(list => (list ?? []).map(s => (s.id === updated.id ? updated : s)))
      const msg = attending ? 'Te has quitado' : '¡Apuntado!'
      notify(msg)
      setAnnouncement(msg)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  return (
    <section aria-labelledby="h-reservas">
      <h2 id="h-reservas" className="section-title">Reservas de entrenos</h2>
      {error && <p className="error" role="alert">{error}</p>}
      <p className="sr-only" role="status" aria-live="polite">{announcement}</p>
      {slots.loading && !slots.data && <p className="muted">Cargando…</p>}
      {slots.error && !slots.data && <p className="error">{slots.error}</p>}
      {slots.data && (next ? (
        <>
          <p className="slot-when">
            <strong>{next.status === 'Open' ? 'Próximo entreno' : 'Se abre pronto'}:</strong>{' '}
            <span className="slot-day">{formatDate(next.date)}</span>
          </p>
          <SlotCard slot={next}>
            {!isAdmin && me && <SlotSignUp slot={next} me={me} onToggle={(id, attending) => void toggle(next, id, attending)} />}
            {!isAdmin && me === null && next.status === 'Open' && (
              <p className="muted">Dinos quién eres en Reservas y podrás apuntarte con un toque.</p>
            )}
          </SlotCard>
          {next.status === 'Pending' && <p className="muted">Las inscripciones abren el {formatOpening(next.opensAt)}.</p>}
        </>
      ) : (
        <EmptyState title="Ahora mismo no hay entrenos abiertos">Cuando el club los abra, los verás aquí.</EmptyState>
      ))}
      <Link className="button reservas-cta" to="/reservas">Ver todas las reservas</Link>
    </section>
  )
}
