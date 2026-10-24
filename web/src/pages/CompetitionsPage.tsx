import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { CompetitionSummary } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import EmptyState from '../components/EmptyState'
import PageHeader from '../components/PageHeader'
import { useAsync, useDocumentTitle } from '../hooks'
import { statusText, typeLabel } from '../labels'
import LoadingBlock from '../components/LoadingBlock'

function Card({ c }: { c: CompetitionSummary }) {
  return (
    <Link to={`/competiciones/${c.id}`} className="card link-card">
      <div className="row between">
        <strong>{c.name}</strong>
        <span className={`badge ${c.status}`}>{statusText(c.status, c.type)}</span>
      </div>
      <p className="muted">
        {typeLabel[c.type]} · {c.type === 'RotatingLeague' ? c.status === 'Draft' ? `mín. ${c.pairCount} jugadores` : `${c.pairCount} jugadores` : `${c.registeredPairs}/${c.pairCount} parejas`} · {c.groupCount} grupo(s)
      </p>
    </Link>
  )
}

export default function CompetitionsPage() {
  useDocumentTitle('Competiciones')
  const { isAdmin } = useAuth()
  const { data, error, loading } = useAsync(() => api.competitions.list(), [], '/api/competitions')

  const inProgress = (data ?? []).filter(c => c.status === 'GroupStage' || c.status === 'Playoffs')
  const signup = (data ?? []).filter(c => c.status === 'Draft')
  const finished = (data ?? []).filter(c => c.status === 'Finished')
  const section = (title: string, list: CompetitionSummary[]) => list.length > 0 && (
    <section aria-label={title}>
      <h2 className="section-title">{title} <span className="muted">({list.length})</span></h2>
      <div className="grid">{list.map(c => <Card key={c.id} c={c} />)}</div>
    </section>
  )

  return (
    <>
      <PageHeader title="Competiciones" subtitle="Ligas, torneos y ligas rotatorias"
        actions={isAdmin && <Link className="button" to="/competiciones/nueva">+ Nueva</Link>} />
      {loading && !data && <LoadingBlock />}
      {error && !data && <p className="error">{error}</p>}
      {data?.length === 0 && (
        <EmptyState title="Aún no hay competiciones"
          action={isAdmin ? <Link className="button" to="/competiciones/nueva">Crear la primera</Link> : undefined}>
          {isAdmin ? 'Crea una liga, un torneo o una liga rotatoria.' : 'Todavía no se ha creado ninguna.'}
        </EmptyState>
      )}
      {section('En curso', inProgress)}
      {section('En inscripción', signup)}
      {section('Finalizadas', finished)}
    </>
  )
}
