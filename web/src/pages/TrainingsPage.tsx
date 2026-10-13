import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import StaleNotice from '../components/StaleNotice'
import { useAsync, useDocumentTitle } from '../hooks'

// Fechas en hora local (toISOString usaría UTC y de madrugada daría el día anterior).
const pad = (n: number) => String(n).padStart(2, '0')
const today = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` }
const fmt = (iso: string) => new Date(iso.length === 10 ? `${iso}T00:00:00` : iso).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })

export default function TrainingsPage() {
  useDocumentTitle('Entrenamientos')
  const { isAdmin } = useAuth()
  const navigate = useNavigate()
  const { data, error, loading, savedAt } = useAsync(() => api.trainings.list(), [], '/api/trainings')
  const [name, setName] = useState('')
  const [date, setDate] = useState(today())
  const [win, setWin] = useState(3)
  const [loss, setLoss] = useState(0)
  const [formError, setFormError] = useState<string | null>(null)

  async function create(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    try {
      const t = await api.trainings.create(name.trim() || `Entrenamiento ${fmt(date)}`, date, win, loss)
      notify('Entrenamiento creado')
      navigate(`/entrenamientos/${t.id}`)
    } catch (err) { setFormError(err instanceof Error ? err.message : 'Error') }
  }

  return (
    <>
      <h1>Entrenamientos</h1>
      <p className="muted">Partidos sueltos «a X tantos» con las parejas que quieras, con clasificación por parejas e individual y sugerencias del siguiente partido.</p>
      {isAdmin && (
        <form className="card form" onSubmit={create}>
          <h2>Nueva sesión</h2>
          <div className="cols">
            <label>Nombre (opcional)<input value={name} maxLength={150} onChange={e => setName(e.target.value)} placeholder="Martes 13 de octubre" /></label>
            <label>Fecha<input type="date" value={date} onChange={e => setDate(e.target.value)} required /></label>
          </div>
          <div className="cols">
            <label>Puntos por victoria<input type="number" min={1} value={Number.isNaN(win) ? '' : win} onChange={e => setWin(e.target.valueAsNumber)} /></label>
            <label>Puntos por derrota<input type="number" min={0} value={Number.isNaN(loss) ? '' : loss} onChange={e => setLoss(e.target.valueAsNumber)} /></label>
          </div>
          {formError && <p className="error">{formError}</p>}
          <button type="submit">Crear sesión</button>
        </form>
      )}
      <StaleNotice savedAt={savedAt} error={error} />
      {loading && !data && <p className="muted">Cargando…</p>}
      {error && !data && <p className="error">{error}</p>}
      {data?.length === 0 && <p className="muted">Aún no hay sesiones de entrenamiento.</p>}
      <div className="grid">
        {data?.map(t => (
          <Link key={t.id} to={`/entrenamientos/${t.id}`} className="card link-card">
            <strong>{t.name}</strong>
            <p className="muted">{fmt(t.date)} · {t.matchCount} partido(s)</p>
          </Link>
        ))}
      </div>
    </>
  )
}
