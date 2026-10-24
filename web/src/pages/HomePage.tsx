import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import DeviceLinkPanel from '../components/DeviceLinkPanel'
import EmptyState from '../components/EmptyState'
import LoadingBlock from '../components/LoadingBlock'
import PageHeader from '../components/PageHeader'
import ReservasButton from '../components/ReservasButton'
import { useAsync, useDocumentTitle } from '../hooks'
import { statusText, typeLabel } from '../labels'
import { formatDate } from '../dates'
import { useDevice } from '../useDevice'

/** Portada: lo que está pasando ahora (competiciones en curso y último entrenamiento) y accesos rápidos. */
export default function HomePage() {
  useDocumentTitle('Inicio')
  const { isAdmin } = useAuth()
  const device = useDevice()
  // Sin identificar (y sin ser administrador) solo se ve esta portada, con el selector «¿Quién eres?» bajo el título.
  // No se pide nada más a la API: ni competiciones ni entrenos.
  if (!isAdmin && !device.me) {
    // Mientras se comprueba quién eres (al recargar), un aviso de carga en vez de una portada vacía.
    if (device.me === undefined) return <LoadingBlock />
    return (
      <>
        <PageHeader title="Club Frontenis Ciudad de Jaén" subtitle="Ligas, torneos y entrenamientos" />
        <DeviceLinkPanel device={device} />
      </>
    )
  }
  return <HomeContent />
}

function HomeContent() {
  useDocumentTitle('Inicio')
  const { isAdmin } = useAuth()
  const comps = useAsync(() => api.competitions.list(), [], '/api/competitions')
  const trainings = useAsync(() => api.trainings.list(), [], '/api/trainings')

  const active = (comps.data ?? []).filter(c => c.status !== 'Finished' && c.status !== 'Draft')
  const drafts = (comps.data ?? []).filter(c => c.status === 'Draft')
  const lastTraining = trainings.data?.[0]

  return (
    <>
      <PageHeader title="Club Frontenis Ciudad de Jaén" subtitle="Ligas, torneos y entrenamientos" />

      <ReservasButton label="Reservas Entrenamientos" />

      <section aria-labelledby="h-entreno">
        <h2 id="h-entreno" className="section-title">Entrenamiento</h2>
        {lastTraining ? (
          <Link to={`/entrenamientos/${lastTraining.id}`} className="card link-card highlight">
            <strong>{lastTraining.name}</strong>
            <span className="muted">{formatDate(lastTraining.date)} · {lastTraining.matchCount} partido(s)</span>
            <span className="cta">Registrar un partido o ver la clasificación →</span>
          </Link>
        ) : (
          <EmptyState title="Aún no hay entrenamientos"
            action={isAdmin ? <Link className="button" to="/entrenamientos">Crear el primero</Link> : undefined}>
            {isAdmin ? 'Crea una sesión para empezar a anotar partidos.' : 'Cuando se cree una sesión podrás registrar partidos aquí.'}
          </EmptyState>
        )}
      </section>

      <section aria-labelledby="h-curso">
        <div className="section-head">
          <h2 id="h-curso" className="section-title">Competiciones en curso</h2>
          <Link to="/competiciones">Ver todas</Link>
        </div>
        {active.length === 0 ? (
          <EmptyState title="No hay competiciones en curso"
            action={isAdmin ? <Link className="button" to="/competiciones/nueva">Nueva competición</Link> : undefined}>
            {drafts.length > 0 ? `${drafts.length} en inscripción, pendientes de sorteo.` : 'Cuando haya una activa, la verás aquí.'}
          </EmptyState>
        ) : (
          <div className="grid">
            {active.map(c => (
              <Link key={c.id} to={`/competiciones/${c.id}`} className="card link-card">
                <div className="row between">
                  <strong>{c.name}</strong>
                  <span className={`badge ${c.status}`}>{statusText(c.status, c.type)}</span>
                </div>
                <span className="muted">{typeLabel[c.type]} · {c.groupCount} grupo(s)</span>
              </Link>
            ))}
          </div>
        )}
      </section>

      <section aria-labelledby="h-acceso">
        <h2 id="h-acceso" className="section-title">Accesos rápidos</h2>
        <div className="quick-links">
          <Link className="card link-card" to="/jugadores/historico"><strong>Histórico</strong><span className="muted">Victorias, puntos y compañeros</span></Link>
          <Link className="card link-card" to="/jugadores?vista=sorteos"><strong>Sorteos</strong><span className="muted">Pareja o partido al azar</span></Link>
          <Link className="card link-card" to="/cronometro"><strong>Cronómetro</strong><span className="muted">Cuenta atrás</span></Link>
        </div>
      </section>
    </>
  )
}
