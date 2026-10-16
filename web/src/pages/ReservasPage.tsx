import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { Player, Slot } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import EmptyState from '../components/EmptyState'
import PageHeader from '../components/PageHeader'
import SlotAdminActions from '../components/SlotAdminActions'
import SlotCard from '../components/SlotCard'
import { addDaysIso, formatDate, mondayOfIso, todayIso } from '../dates'
import { useAsync, useDocumentTitle } from '../hooks'

/** Franjas de entrenamiento de esta semana y la siguiente. El administrador puede gestionarlas desde aquí. */
export default function ReservasPage() {
  useDocumentTitle('Reservas de entrenos')
  const { isAdmin } = useAuth()
  const from = useMemo(() => mondayOfIso(todayIso()), [])
  const to = addDaysIso(from, 13)
  const slots = useAsync(() => api.slots.list(from, to), [from, to])
  // Solo el administrador necesita la lista de jugadores (para apuntar a mano): al resto no se le pide.
  const players = useAsync(() => (isAdmin ? api.players.list() : Promise.resolve([] as Player[])), [isAdmin])
  const [error, setError] = useState<string | null>(null)

  const byDay = useMemo(() => {
    const days = new Map<string, Slot[]>()
    for (const s of slots.data ?? []) days.set(s.date, [...(days.get(s.date) ?? []), s])
    return [...days.entries()]
  }, [slots.data])

  /** Ejecuta una acción del administrador y sustituye la franja devuelta; si falla, muestra el motivo. */
  async function run(action: () => Promise<Slot>, done?: string) {
    setError(null)
    try {
      const updated = await action()
      slots.setData(list => (list ?? []).map(s => (s.id === updated.id ? updated : s)))
      if (done) notify(done)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

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
      <PageHeader title="Reservas de entrenos" subtitle="Franjas de esta semana y de la siguiente"
        actions={isAdmin && <Link className="button" to="/reservas/gestion">Gestionar</Link>} />
      {error && <p className="error" role="alert">{error}</p>}
      {slots.error && !slots.data && <p className="error">{slots.error}</p>}
      {slots.loading && !slots.data && <p className="muted">Cargando…</p>}

      {slots.data?.length === 0 && (
        <EmptyState title="Aún no hay franjas publicadas"
          action={isAdmin ? <Link className="button" to="/reservas/gestion">Crear la plantilla y generar franjas</Link> : undefined}>
          {isAdmin ? 'Define la plantilla semanal y genera las franjas de las próximas semanas.' : 'Cuando el club publique los entrenos los verás aquí.'}
        </EmptyState>
      )}

      {byDay.map(([date, daySlots]) => (
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
            </SlotCard>
          ))}
        </section>
      ))}
    </>
  )
}
