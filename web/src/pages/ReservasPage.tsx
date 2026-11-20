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
import ShareImageButton from '../components/ShareImageButton'
import SlotSignUp from '../components/SlotSignUp'
import { addDaysIso, formatDate, mondayOfIso, parseDate, todayIso, weekTitle } from '../dates'
import { useAsync, useDocumentTitle } from '../hooks'
import { exportAttendanceImage } from '../imageExport'
import { isRemindable } from '../reminders'
import { useDevice } from '../useDevice'
import LoadingBlock from '../components/LoadingBlock'
import SubTabs from '../components/SubTabs'

const WEEKDAYS = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo']
/** Día de la semana de una fecha, con el lunes como 0. */
const weekdayOf = (iso: string) => (parseDate(iso).getDay() + 6) % 7

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
  const [announcement, setAnnouncement] = useState('')
  // El administrador gestiona desde aquí; el resto de personas se identifican con su dispositivo.
  const device = useDevice()

  // Franjas agrupadas por semana (lunes a domingo) y, dentro de cada una, por día. Los días que ya han pasado
  // se ocultan (si hoy es miércoles, el martes ya no aparece); una semana sin días por venir tampoco.
  // Días de la semana con entrenos por venir (para los botones de filtro) y el elegido; «Todos» es lo de siempre.
  const [daySel, setDaySel] = useState<string>('all')
  const weekdays = useMemo(() => {
    const today = todayIso()
    return [...new Set((slots.data ?? []).filter(s => s.date >= today).map(s => weekdayOf(s.date)))].sort((a, b) => a - b)
  }, [slots.data])
  const day = daySel !== 'all' && weekdays.includes(Number(daySel)) ? daySel : 'all'

  const weeks = useMemo(() => {
    const byWeek = new Map<string, Map<string, Slot[]>>()
    const today = todayIso()
    for (const s of slots.data ?? []) {
      if (s.date < today) continue
      if (day !== 'all' && weekdayOf(s.date) !== Number(day)) continue
      const monday = mondayOfIso(s.date)
      const days = byWeek.get(monday) ?? new Map<string, Slot[]>()
      days.set(s.date, [...(days.get(s.date) ?? []), s])
      byWeek.set(monday, days)
    }
    return [...byWeek.entries()].map(([monday, days]) => ({ monday, days: [...days.entries()] }))
  }, [slots.data, day])

  /** Ejecuta una acción del administrador y sustituye la franja devuelta; si falla, muestra el motivo. */
  async function run(action: () => Promise<Slot>, done?: string) {
    setError(null)
    try {
      const updated = await action()
      slots.setData(list => (list ?? []).map(s => (s.id === updated.id ? updated : s)))
      if (done) { notify(done); setAnnouncement(done) }
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const toggle = (slot: Slot, playerId: number, attending: boolean) =>
    run(() => (attending ? api.slots.unattend(slot.id, playerId) : api.slots.attend(slot.id, playerId)),
      attending ? 'Te has quitado' : '¡Apuntado!')

  // Apuntar (o quitar) de una vez a todos los de la persona: una petición por jugador, una tras otra.
  const [bulkBusy, setBulkBusy] = useState<number | null>(null)
  async function toggleAll(slot: Slot, playerIds: number[], attending: boolean) {
    setError(null); setBulkBusy(slot.id)
    let updated: Slot | null = null
    let failed = false
    try {
      for (const id of playerIds) updated = await (attending ? api.slots.unattend(slot.id, id) : api.slots.attend(slot.id, id))
    } catch (e) { failed = true; setError(e instanceof Error ? e.message : 'Error') }
    if (updated) { const done = updated; slots.setData(list => (list ?? []).map(s => (s.id === done.id ? done : s))) }
    if (!failed) { const msg = attending ? 'Quitados todos' : '¡Todos apuntados!'; notify(msg); setAnnouncement(msg) }
    setBulkBusy(null)
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
      <PageHeader title="Reservas de entrenos" subtitle="Franjas de las próximas semanas"
        actions={isAdmin && <Link className="button" to="/reservas/gestion">Gestionar</Link>} />
      {!isAdmin && device.me && device.me.delegates.length > 0 && (
        <p className="muted">Tienes permitido apuntar a los siguientes jugadores: {device.me.delegates.map(d => d.name).join(', ')}</p>
      )}
      {error && <p className="error" role="alert">{error}</p>}
      <p className="sr-only" role="status" aria-live="polite">{announcement}</p>
      {slots.error && !slots.data && <p className="error">{slots.error}</p>}
      {slots.loading && !slots.data && <LoadingBlock />}

      {slots.data?.length === 0 && (
        <EmptyState title="Aún no hay franjas publicadas"
          action={isAdmin ? <Link className="button" to="/reservas/gestion">Crear la plantilla y generar franjas</Link> : undefined}>
          {isAdmin ? 'Define la plantilla semanal y genera las franjas de las próximas semanas.' : 'Cuando el club publique los entrenos los verás aquí.'}
        </EmptyState>
      )}

      {slots.data && slots.data.length > 0 && weeks.length === 0 && (
        <EmptyState title="No quedan entrenos por venir">Cuando el club publique nuevas franjas las verás aquí.</EmptyState>
      )}

      {weekdays.length > 1 && (
        <SubTabs label="Día de la semana" value={day} onChange={setDaySel}
          options={[{ key: 'all', label: 'Todos' }, ...weekdays.map(w => ({ key: String(w), label: WEEKDAYS[w] }))]} />
      )}

      {weeks.map(({ monday, days }) => {
        const weekSlots = days.flatMap(([, list]) => list)
        return (
          <div key={monday} className="slot-week">
            <h2 className="week-title">{weekTitle(monday)}</h2>
            {weekSlots.some(isRemindable) && (
              <ShareImageButton what="la semana"
                onShare={() => exportAttendanceImage('Entrenamientos', weekSlots.filter(isRemindable), `entrenos_${monday}.png`)} />
            )}
            {days.map(([date, daySlots]) => (
              <section key={date} aria-labelledby={`dia-${date}`}>
                <h3 id={`dia-${date}`} className="section-title slot-day">{formatDate(date)}</h3>
                {daySlots.map(slot => (
                  <SlotCard key={slot.id} slot={slot}
                    onRemoveAttendee={isAdmin ? id => void run(() => api.slots.withdraw(slot.id, id)) : undefined}>
                    {isAdmin && (
                      <SlotAdminActions slot={slot} players={players.data ?? []}
                        onOpenNow={() => void run(() => api.slots.openNow(slot.id), 'Franja abierta')}
                        onCloseSignUps={() => void run(() => api.slots.closeSignUps(slot.id), 'Inscripciones cerradas')}
                        onCancel={() => void run(() => api.slots.cancel(slot.id), 'Franja cancelada')}
                        onRestore={() => void run(() => api.slots.restore(slot.id), 'Franja restaurada')}
                        onDelete={() => void remove(slot)}
                        onSignUp={id => void run(() => api.slots.signUp(slot.id, id))} />
                    )}
                    {!isAdmin && device.me && (
                      <SlotSignUp slot={slot} me={device.me} busy={bulkBusy === slot.id}
                        onToggle={(id, attending) => void toggle(slot, id, attending)}
                        onToggleAll={(ids, attending) => void toggleAll(slot, ids, attending)} />
                    )}
                    {isRemindable(slot) && (
                      <ShareImageButton what="este entreno"
                        ariaLabel={`Enviar por WhatsApp el entreno de ${formatDate(slot.date)}`}
                        onShare={() => exportAttendanceImage('Entrenamiento', [slot], `entreno_${slot.date}_${slot.start.slice(0, 2)}h.png`)} />
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
