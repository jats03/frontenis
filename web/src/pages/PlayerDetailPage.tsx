import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Relation } from '../api/types'
import StaleNotice from '../components/StaleNotice'
import { useAsync, useDocumentTitle } from '../hooks'

function RelationTable({ title, caption, rows }: { title: string; caption: string; rows: Relation[] }) {
  return (
    <section className="card">
      <h2>{title}</h2>
      {rows.length === 0 ? <p className="muted">Sin datos todavía.</p> : (
        <div className="table-wrap">
          <table className="standings">
            <caption className="sr-only">{caption}</caption>
            <thead>
              <tr>
                <th scope="col" className="left">Jugador</th><th scope="col" title="Partidos">PJ</th>
                <th scope="col" title="Victorias">G</th><th scope="col" title="Derrotas">P</th>
                <th scope="col" title="Porcentaje de victorias">% G</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(r => (
                <tr key={r.playerId}>
                  <td className="left"><Link to={`/jugadores/${r.playerId}`}>{r.name}</Link></td>
                  <td>{r.played}</td><td>{r.won}</td><td>{r.lost}</td><td>{r.winRate}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

export default function PlayerDetailPage() {
  const id = Number(useParams().id)
  const [competitionId, setCompetitionId] = useState<number | undefined>()
  const { data, error, loading, savedAt } = useAsync(() => api.players.playerHistory(id, competitionId), [id, competitionId])
  useDocumentTitle(data?.summary.name ?? 'Jugador')

  if (error && !data) return <p className="error">{error}</p>
  if (!data) return loading ? <p className="muted">Cargando…</p> : null

  const s = data.summary
  // «Con quién gana más»: compañeros con victorias, ordenados por victorias y luego porcentaje.
  const bestPartners = [...data.partners].filter(p => p.won > 0)
    .sort((a, b) => b.won - a.won || b.winRate - a.winRate || b.played - a.played)
  const frequent = [...data.partners].sort((a, b) => b.played - a.played || b.won - a.won)
  const toughRivals = [...data.rivals].filter(r => r.lost > 0).sort((a, b) => b.lost - a.lost || b.played - a.played)

  const stat = (label: string, value: string | number) => (
    <div className="stat"><strong>{value}</strong><span>{label}</span></div>
  )

  return (
    <>
      <div className="row between">
        <h1>{s.name}</h1>
        <Link className="button secondary" to="/jugadores/historico">Volver al histórico</Link>
      </div>
      <label className="filter">Competición
        <select value={competitionId ?? ''} onChange={e => setCompetitionId(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">Todas</option>
          {data.competitions.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </label>
      <StaleNotice savedAt={savedAt} error={error} />
      <section className="card stats">
        {stat('Partidos', s.played)}{stat('Victorias', s.won)}{stat('Derrotas', s.lost)}
        {stat('% victorias', s.played ? `${s.winRate}%` : '—')}
        {stat('Puntos clasif.', s.classificationPoints)}
        {stat('Sets', `${s.setsFor}-${s.setsAgainst}`)}
        {stat('Tantos', `${s.tantosFor}-${s.tantosAgainst}`)}
        {stat('Competiciones', s.competitions)}
      </section>
      <div className="grid">
        <RelationTable title="Compañeros más frecuentes" caption="Compañeros ordenados por partidos jugados juntos" rows={frequent} />
        <RelationTable title="Con quién ha ganado más" caption="Compañeros ordenados por victorias conjuntas" rows={bestPartners} />
        <RelationTable title="Rivales más habituales" caption="Rivales ordenados por partidos disputados"
          rows={[...data.rivals].sort((a, b) => b.played - a.played || b.won - a.won)} />
        <RelationTable title="Rivales que más le han ganado" caption="Rivales ordenados por derrotas contra ellos" rows={toughRivals} />
      </div>
      <section className="card">
        <h2>Últimos partidos</h2>
        {data.recent.length === 0 ? <p className="muted">Sin partidos jugados.</p> : (
          <ul className="recent">
            {data.recent.map(m => (
              <li key={m.matchId}>
                <span className={m.won ? 'res win' : 'res loss'}>{m.won ? 'Victoria' : 'Derrota'}</span>{' '}
                <Link to={`/competiciones/${m.competitionId}`}>{m.competitionName}</Link>: con {m.partner} vs {m.rivals}{' '}
                <strong>({m.sets.map(x => `${x.home}-${x.away}`).join(', ')})</strong>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  )
}
