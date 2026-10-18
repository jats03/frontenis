import { useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { Player, SlotTemplate, WeekDay } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import DelegatesPanel from '../components/DelegatesPanel'
import DeviceLinksPanel from '../components/DeviceLinksPanel'
import PageHeader from '../components/PageHeader'
import SlotTemplateRow from '../components/SlotTemplateRow'
import { addDaysIso, mondayOfIso, toApiTime, todayIso } from '../dates'
import { useAsync, useDocumentTitle } from '../hooks'
import { WEEK_DAYS } from '../slots'
import LoadingBlock from '../components/LoadingBlock'

/** Administración de reservas: plantilla semanal, generación de franjas, apertura de semanas y franjas extra. */
export default function ReservasGestionPage() {
  useDocumentTitle('Gestionar reservas')
  const { isAdmin } = useAuth()
  const templates = useAsync(() => (isAdmin ? api.slotTemplates.list() : Promise.resolve([] as SlotTemplate[])), [isAdmin])
  const players = useAsync(() => (isAdmin ? api.players.list() : Promise.resolve([] as Player[])), [isAdmin])
  const nextMonday = useMemo(() => addDaysIso(mondayOfIso(todayIso()), 7), [])

  const [error, setError] = useState<string | null>(null)
  // Nueva plantilla
  const [day, setDay] = useState<WeekDay>('Monday')
  const [start, setStart] = useState('18:00')
  const [end, setEnd] = useState('20:00')
  // Generar franjas y abrir semana
  const [from, setFrom] = useState(nextMonday)
  const [weeks, setWeeks] = useState(4)
  const [weekStart, setWeekStart] = useState(nextMonday)
  // Franja extra
  const [extraDate, setExtraDate] = useState(todayIso())
  const [extraStart, setExtraStart] = useState('18:00')
  const [extraEnd, setExtraEnd] = useState('20:00')

  if (!isAdmin) return <><h1>Gestionar reservas</h1><p className="error">Necesitas iniciar sesión como administrador.</p></>

  /** Ejecuta una acción, avisa con el resultado y muestra el error si falla. */
  async function run(action: () => Promise<unknown>, done: string | (() => string), reload = false) {
    setError(null)
    try {
      await action()
      notify(typeof done === 'string' ? done : done())
      if (reload) await templates.reload()
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const addTemplate = (e: FormEvent) => {
    e.preventDefault()
    void run(() => api.slotTemplates.create({ day, start: toApiTime(start), end: toApiTime(end), active: true }),
      'Plantilla añadida', true)
  }

  const generate = (e: FormEvent) => {
    e.preventDefault()
    let created = 0
    void run(async () => { created = (await api.slots.generate(from, weeks)).created },
      () => (created === 0 ? 'No faltaba ninguna franja' : `${created} franja(s) creada(s)`))
  }

  const closeWeek = () => {
    let closed = 0
    void run(async () => { closed = (await api.slots.closeWeek(weekStart)).closed },
      () => (closed === 0 ? 'No había franjas abiertas antes de tiempo' : `${closed} franja(s) vuelta(s) a cerrar`))
  }

  const openWeek = (e: FormEvent) => {
    e.preventDefault()
    let opened = 0
    void run(async () => { opened = (await api.slots.openWeek(weekStart)).opened },
      () => (opened === 0 ? 'No había franjas por abrir' : `${opened} franja(s) abierta(s)`))
  }

  const addExtra = (e: FormEvent) => {
    e.preventDefault()
    void run(() => api.slots.create({ date: extraDate, start: toApiTime(extraStart), end: toApiTime(extraEnd) }),
      'Franja añadida')
  }

  async function removeTemplate(t: SlotTemplate) {
    if (await confirmDelete('¿Eliminar esta plantilla?', 'Las franjas ya generadas se conservan.'))
      void run(() => api.slotTemplates.remove(t.id), 'Plantilla eliminada', true)
  }

  return (
    <>
      <PageHeader title="Gestionar reservas" subtitle="Plantilla semanal, franjas y apertura"
        actions={<Link className="button secondary" to="/reservas">Ver reservas</Link>} />
      {error && <p className="error" role="alert">{error}</p>}

      <h2 className="section-title">Plantilla semanal</h2>
      <p className="muted">Lo que suele haber cada semana. Cambiarla no modifica las franjas ya generadas.</p>
      {templates.loading && !templates.data && <LoadingBlock />}
      {templates.error && <p className="error">{templates.error}</p>}
      <ul className="plain">
        {templates.data?.map(t => (
          <SlotTemplateRow key={`${t.id}-${t.day}-${t.start}-${t.end}-${t.active}`} template={t}
            onSave={changes => void run(() => api.slotTemplates.update(t.id, changes), 'Plantilla guardada', true)}
            onDelete={() => void removeTemplate(t)} />
        ))}
      </ul>
      <form className="card form" onSubmit={addTemplate}>
        <h3>Añadir al horario semanal</h3>
        <div className="cols">
          <label>Día
            <select value={day} onChange={e => setDay(e.target.value as WeekDay)}>
              {WEEK_DAYS.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
            </select>
          </label>
          <label>Desde<input type="time" value={start} onChange={e => setStart(e.target.value)} required /></label>
          <label>Hasta<input type="time" value={end} onChange={e => setEnd(e.target.value)} required /></label>
        </div>
        <div className="row"><button type="submit">Añadir</button></div>
      </form>

      <h2 className="section-title">Generar franjas</h2>
      <form className="card form" onSubmit={generate}>
        <p className="muted">Crea las franjas que falten a partir de la plantilla. Se pueden repetir sin miedo: no se duplica nada.
          Las inscripciones de cada semana se abren solas según la regla del club.</p>
        <div className="cols">
          <label>Desde<input type="date" value={from} onChange={e => setFrom(e.target.value)} required /></label>
          <label>Semanas (1-8)
            <input type="number" min={1} max={8} value={Number.isNaN(weeks) ? '' : weeks} onChange={e => setWeeks(e.target.valueAsNumber)} required />
          </label>
        </div>
        <div className="row"><button type="submit">Generar franjas</button></div>
      </form>

      <h2 className="section-title">Abrir o cerrar una semana</h2>
      <form className="card form" onSubmit={openWeek}>
        <p className="muted">Abre las inscripciones de todas las franjas de esa semana sin esperar a la apertura automática. Si te equivocas, «Cerrar semana» las deja como estaban (no toca las que ya se abren por su horario).</p>
        <label>Cualquier día de la semana<input type="date" value={weekStart} onChange={e => setWeekStart(e.target.value)} required /></label>
        <div className="row">
          <button type="submit">Abrir semana</button>
          <button type="button" className="secondary" onClick={closeWeek}>Cerrar semana</button>
        </div>
      </form>

      <h2 className="section-title">Añadir una franja extra</h2>
      <form className="card form" onSubmit={addExtra}>
        <p className="muted">Un día o un horario que no está en la plantilla.</p>
        <div className="cols">
          <label>Fecha<input type="date" value={extraDate} onChange={e => setExtraDate(e.target.value)} required /></label>
          <label>Desde<input type="time" value={extraStart} onChange={e => setExtraStart(e.target.value)} required /></label>
          <label>Hasta<input type="time" value={extraEnd} onChange={e => setExtraEnd(e.target.value)} required /></label>
        </div>
        <div className="row"><button type="submit">Añadir franja</button></div>
      </form>

      <DeviceLinksPanel onError={setError} />
      <DelegatesPanel players={players.data ?? []} onError={setError} />
    </>
  )
}
