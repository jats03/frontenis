import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import EmptyState from '../components/EmptyState'
import PageHeader from '../components/PageHeader'
import ReservasButton from '../components/ReservasButton'
import { formatDate, todayIso, trainingTitle } from '../dates'
import { useAsync, useDocumentTitle } from '../hooks'
import LoadingBlock from '../components/LoadingBlock'

export default function TrainingsPage() {
  useDocumentTitle('Entrenamientos')
  const { isAdmin } = useAuth()
  const navigate = useNavigate()
  const { data, error, loading } = useAsync(() => api.trainings.list(), [], '/api/trainings')
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [date, setDate] = useState(todayIso())
  const [win, setWin] = useState(3)
  const [loss, setLoss] = useState(0)
  const [formError, setFormError] = useState<string | null>(null)

  async function create(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    try {
      const t = await api.trainings.create(name.trim() || trainingTitle(date), date, win, loss)
      notify('Entrenamiento creado')
      navigate(`/entrenamientos/${t.id}`)
    } catch (err) { setFormError(err instanceof Error ? err.message : 'Error') }
  }

  return (
    <>
      <ReservasButton />
      <PageHeader title="Entrenamientos" subtitle="Partidos sueltos «a X tantos» con clasificación por parejas e individual"
        actions={isAdmin && !creating && <button onClick={() => setCreating(true)}>+ Nueva sesión</button>} />

      {isAdmin && creating && (
        <form className="card form" onSubmit={create}>
          <h2>Nueva sesión</h2>
          <div className="cols">
            <label>Nombre (opcional)<input value={name} maxLength={150} onChange={e => setName(e.target.value)} placeholder="Martes, 13 de octubre" /></label>
            <label>Fecha<input type="date" value={date} onChange={e => setDate(e.target.value)} required /></label>
          </div>
          <div className="cols">
            <label>Puntos por victoria<input type="number" min={1} value={Number.isNaN(win) ? '' : win} onChange={e => setWin(e.target.valueAsNumber)} /></label>
            <label>Puntos por derrota<input type="number" min={0} value={Number.isNaN(loss) ? '' : loss} onChange={e => setLoss(e.target.valueAsNumber)} /></label>
          </div>
          {formError && <p className="error">{formError}</p>}
          <div className="row">
            <button type="submit">Crear sesión</button>
            <button type="button" className="secondary" onClick={() => setCreating(false)}>Cancelar</button>
          </div>
        </form>
      )}

      {loading && !data && <LoadingBlock />}
      {error && !data && <p className="error">{error}</p>}
      {data?.length === 0 && (
        <EmptyState title="Aún no hay sesiones de entrenamiento"
          action={isAdmin && !creating ? <button onClick={() => setCreating(true)}>Crear la primera sesión</button> : undefined}>
          {isAdmin ? 'Crea una sesión por día y anota los partidos que se jueguen.' : 'Cuando se cree una sesión podrás registrar partidos.'}
        </EmptyState>
      )}
      <div className="grid">
        {data?.map(t => (
          <Link key={t.id} to={`/entrenamientos/${t.id}`} className="card link-card">
            <strong>{t.name}</strong>
            <p className="muted">{formatDate(t.date)} · {t.matchCount} partido(s)</p>
          </Link>
        ))}
      </div>
    </>
  )
}
