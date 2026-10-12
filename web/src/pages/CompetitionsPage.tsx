import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { useAsync } from '../hooks'
import { statusLabel, typeLabel } from '../labels'

export default function CompetitionsPage() {
  const { isAdmin } = useAuth()
  const { data, error, loading } = useAsync(() => api.competitions.list(), [])

  return (
    <>
      <div className="row between">
        <h2>Competiciones</h2>
        {isAdmin && <Link className="button" to="/competiciones/nueva">+ Nueva</Link>}
      </div>
      {loading && <p className="muted">Cargando…</p>}
      {error && <p className="error">{error}</p>}
      {data?.length === 0 && <p className="muted">Aún no hay competiciones.</p>}
      <div className="grid">
        {data?.map(c => (
          <Link key={c.id} to={`/competiciones/${c.id}`} className="card link-card">
            <div className="row between">
              <strong>{c.name}</strong>
              <span className={`badge ${c.status}`}>{statusLabel[c.status]}</span>
            </div>
            <p className="muted">
              {typeLabel[c.type]} · {c.type === 'RotatingLeague' ? c.status === 'Draft' ? `mín. ${c.pairCount} jugadores` : `${c.pairCount} jugadores` : `${c.registeredPairs}/${c.pairCount} parejas`} · {c.groupCount} grupo(s)
            </p>
          </Link>
        ))}
      </div>
    </>
  )
}
