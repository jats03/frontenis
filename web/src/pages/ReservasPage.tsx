import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { Player, Slot } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import EmptyState from '../components/EmptyState'
import PageHeader from '../components/PageHeader'
import DeviceLinkPanel from '../components/DeviceLinkPanel'
import SlotAdminActions from '../components/SlotAdminActions'
import SlotCard from '../components/SlotCard'
import ShareReminderButton from '../components/ShareReminderButton'
import SlotSignUp from '../components/SlotSignUp'
import { addDaysIso, formatDate, mondayOfIso, todayIso, weekTitle } from '../dates'
import { useAsync, useDocumentTitle } from '../hooks'
import { isRemindable, reservasLink, slotReminder, weekReminder } from '../reminders'
import { useDevice } from '../useDevice'

/** Cuántas semanas (desde la actual) se muestran: todas las que el administrador haya generado. */
const WEEKS_SHOWN = 8

/** Franjas de entrenamiento de las próximas semanas. El administrador puede gestionarlas desde aquí. */
export default function ReservasPage() {
  useDocumentTitle('Reservas de entrenos')
  const { isAdmin } = useAuth()
  const from = useMemo(() => mondayOfIso(todayIso()), [])
  const to = addDaysIso(from, WEEKS_SHOWN * 7 - 1)
  const slots = useAsync(() => api.slots.list(from, to), [from, to])
  // Solo el administrador necesita la lista de jugadores (para apuntar a mano): al resto no se le pide.
  const players = useAsync(() => (isAdmin ? api.players.list() : Promise.resolve([] as Player[])), [isAdmin])
  const [error, setError] = useState<string | null>(null)
  // El administrador gestiona desde aquí; el resto de personas se identifican con su dispositivo.
  const device = useDevice(!isAdmin)

  // Franjas agrupadas por semana (lunes a domingo) y, dentro de cada una, por día.
  const weeks = useMemo(() => {
    const byWeek = new Map<string, Map<string, Slot[]>>()
    for (const s of slots.data ?? []) {
      const monday = mondayOfIso(s.date)
      const days = byWeek.get(monday) ?? new Map<string, Slot[]>()
      days.set(s.date, [...(days.get(s.date) ?? []), s])
      byWeek.set(monday, days)
    }
    return [...byWeek.entries()].map(([monday, days]) => ({ monday, days: [...days.entries()] }))
  }, [slots.data])
  const link = reservasLink()

  /** Ejecuta una acción del administrador y sustituye la franja devuelta; si falla, muestra el motivo. */
  async function run(action: () => Promise<Slot>, done?: string) {
    setError(null)
    try {
      const updated = await action()
      slots.setData(list => (list ?? []).map(s => (s.id === updated.id ? updated : s)))
      if (done) notify(done)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const toggle = (slot: Slot, playerId: number, attending: boolean) =>
    run(() => (attending ? api.slots.unattend(slot.id, playerId) : api.slots.attend(slot.id, playerId)),
      attending ? 'Te has quitado' : '¡Apuntado!')

  async function remove(slot: Slot) {
    setError(null)
    try {
      await api.slots.remove(slot.id)
      slots.setData(list => (list ?? []).filter(s => s.id !== slot.id))
      notify('Franja eliminada')
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  return (
    <>
      <PageHeader title="Reservas de entrenos" subtitle="Franjas de las próximas semanas"
        actions={isAdmin && <Link className="button" to="/reservas/gestion">Gestionar</Link>} />
      {!isAdmin && <DeviceLinkPanel device={device} />}
      {error && <p className="error" role="alert">{error}</p>}
      {slots.error && !slots.data && <p className="error">{slots.error}</p>}
      {slots.loading && !slots.data && <p className="muted">Cargando…</p>}

      {slots.data?.length === 0 && (
        <EmptyState title="Aún no hay franjas publicadas"
          action={isAdmin ? <Link className="button" to="/reservas/gestion">Crear la plantilla y generar franjas</Link> : undefined}>
          {isAdmin ? 'Define la plantilla semanal y genera las franjas de las próximas semanas.' : 'Cuando el club publique los entrenos los verás aquí.'}
        </EmptyState>
      )}

      {weeks.map(({ monday, days }) => {
        const weekSlots = days.flatMap(([, list]) => list)
        return (
          <div key={monday} className="slot-week">
            <h2 className="week-title">{weekTitle(monday)}</h2>
            {weekSlots.some(isRemindable) && (
              <ShareReminderButton label="Recordar la semana por WhatsApp"
                message={weekReminder(weekSlots, monday, link)} />
            )}
            {days.map(([date, daySlots]) => (
              <section key={date} aria-labelledby={`dia-${date}`}>
                <h2 id={`dia-${date}`} className="section-title slot-day">{formatDate(date)}</h2>
                {daySlots.map(slot => (
                  <SlotCard key={slot.id} slot={slot}
                    onRemoveAttendee={isAdmin ? id => void run(() => api.slots.withdraw(slot.id, id)) : undefined}>
                    {isAdmin && (
                      <SlotAdminActions slot={slot} players={players.data ?? []}
                        onOpenNow={() => void run(() => api.slots.openNow(slot.id), 'Franja abierta')}
                        onCancel={() => void run(() => api.slots.cancel(slot.id), 'Franja cancelada')}
                        onRestore={() => void run(() => api.slots.restore(slot.id), 'Franja restaurada')}
                        onDelete={() => void remove(slot)}
                        onSignUp={id => void run(() => api.slots.signUp(slot.id, id))} />
                    )}
                    {!isAdmin && device.me && (
                      <SlotSignUp slot={slot} me={device.me} onToggle={(id, attending) => void toggle(slot, id, attending)} />
                    )}
                    {isRemindable(slot) && (
                      <ShareReminderButton label="Avisar por WhatsApp"
                        ariaLabel={`Avisar por WhatsApp del entreno de ${formatDate(slot.date)}`}
                        message={slotReminder(slot, link)} />
                    )}
                  </SlotCard>
                ))}
              </section>
            ))}
          </div>
        )
      })}
    </>
  )
}
