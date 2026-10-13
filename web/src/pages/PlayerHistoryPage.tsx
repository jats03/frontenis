import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { PlayerHistoryRow } from '../api/types'
import StaleNotice from '../components/StaleNotice'
import { useAsync, useDocumentTitle } from '../hooks'

type SortKey = 'classificationPoints' | 'won' | 'lost' | 'played' | 'winRate' | 'tantosDiff' | 'tantosFor' | 'name'

const columns: { key: SortKey; label: string; title: string; opt?: boolean }[] = [
  { key: 'played', label: 'PJ', title: 'Partidos jugados' },
  { key: 'won', label: 'G', title: 'Victorias' },
  { key: 'lost', label: 'P', title: 'Derrotas' },
  { key: 'winRate', label: '% G', title: 'Porcentaje de victorias' },
  { key: 'tantosFor', label: 'TF', title: 'Tantos a favor (todos los sets)', opt: true },
  { key: 'tantosDiff', label: 'Dif', title: 'Diferencia de tantos' },
  { key: 'classificationPoints', label: 'Pts', title: 'Puntos de clasificación' },
]

export default function PlayerHistoryPage() {
  useDocumentTitle('Histórico de jugadores')
  const [competitionId, setCompetitionId] = useState<number | undefined>()
  const [sort, setSort] = useState<SortKey>('classificationPoints')
  const [desc, setDesc] = useState(true)
  const comps = useAsync(() => api.competitions.list(), [], '/api/competitions')
  const { data, error, loading, savedAt } = useAsync(() => api.players.history(competitionId), [competitionId])

  const rows = useMemo(() => {
    const list = [...(data ?? [])]
    list.sort((a: PlayerHistoryRow, b: PlayerHistoryRow) => {
      const r = sort === 'name' ? a.name.localeCompare(b.name) : (a[sort] as number) - (b[sort] as number)
      return (desc ? -r : r) || a.name.localeCompare(b.name)
    })
    return list
  }, [data, sort, desc])

  function toggle(key: SortKey) {
    if (key === sort) setDesc(d => !d)
    else { setSort(key); setDesc(key !== 'name') }
  }
  const aria = (key: SortKey) => (key === sort ? (desc ? 'descending' : 'ascending') : 'none')

  return (
    <>
      <div className="row between">
        <h1>Histórico de jugadores</h1>
        <Link className="button secondary" to="/jugadores">Volver a jugadores</Link>
      </div>
      <label className="filter">Competición
        <select value={competitionId ?? ''} onChange={e => setCompetitionId(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">Todas</option>
          {comps.data?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </label>
      <StaleNotice savedAt={savedAt} error={error} />
      {error && !data && <p className="error">{error}</p>}
      {loading && !data && <p className="muted">Cargando…</p>}
      {data && (
        <section className="card">
          <div className="table-wrap">
            <table className="standings">
              <caption className="sr-only">Histórico de jugadores</caption>
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col" className="left" aria-sort={aria('name')}>
                    <button className="link-button" onClick={() => toggle('name')}>Jugador</button>
                  </th>
                  {columns.map(c => (
                    <th key={c.key} scope="col" className={c.opt ? 'opt' : undefined} aria-sort={aria(c.key)} title={c.title}>
                      <button className="link-button" onClick={() => toggle(c.key)}>{c.label}</button>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((r, i) => (
                  <tr key={r.playerId}>
                    <td>{i + 1}</td>
                    <td className="left"><Link to={`/jugadores/${r.playerId}`}>{r.name}</Link></td>
                    <td>{r.played}</td><td>{r.won}</td><td>{r.lost}</td><td>{r.played ? `${r.winRate}%` : '—'}</td>
                    <td className="opt">{r.tantosFor}</td>
                    <td>{r.tantosDiff > 0 ? `+${r.tantosDiff}` : r.tantosDiff}</td>
                    <td><strong>{r.classificationPoints}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="muted">
            Los puntos de clasificación solo cuentan partidos de liga o fase de grupos (cada competición con sus puntos
            por victoria y derrota); los tantos suman todos los sets, incluida la fase final.
          </p>
        </section>
      )}
    </>
  )
}
