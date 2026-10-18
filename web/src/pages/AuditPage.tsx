import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { AuditActor, AuditQuery } from '../api/types'
import { ACTOR_LABEL, describeAction, statusClass } from '../auditView'
import { useAuth } from '../auth/AuthContext'
import LoadingBlock from '../components/LoadingBlock'
import PageHeader from '../components/PageHeader'
import { useAsync, useDocumentTitle } from '../hooks'

const PAGE_SIZE = 50
const EMPTY: AuditQuery = { actor: '', text: '', method: '', from: '', to: '', page: 1 }
const when = (iso: string) => new Date(iso).toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'medium' })

/** Registro de actividad: quién hizo qué (acciones que modifican datos), solo para el administrador. */
export default function AuditPage() {
  useDocumentTitle('Registro de actividad')
  const { isAdmin } = useAuth()
  const [draft, setDraft] = useState<AuditQuery>(EMPTY)
  const [query, setQuery] = useState<AuditQuery>(EMPTY)
  const result = useAsync(
    () => (isAdmin ? api.audit.search(query, PAGE_SIZE) : Promise.resolve(null)),
    [isAdmin, query])

  if (!isAdmin) return <><h1>Registro de actividad</h1><p className="error">Necesitas iniciar sesión como administrador.</p></>

  const apply = (e: FormEvent) => { e.preventDefault(); setQuery({ ...draft, page: 1 }) }
  const clear = () => { setDraft(EMPTY); setQuery(EMPTY) }
  const goTo = (page: number) => setQuery(q => ({ ...q, page }))
  const set = <K extends keyof AuditQuery>(k: K, v: AuditQuery[K]) => setDraft(d => ({ ...d, [k]: v }))

  const data = result.data
  const pages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1

  return (
    <>
      <PageHeader title="Registro de actividad"
        subtitle="Acciones que modifican datos, de los últimos 30 días. Las consultas y pantallas abiertas están en el log del servidor." />
      <form className="card form audit-filters" onSubmit={apply} aria-label="Filtros del registro">
        <label>Quién
          <select value={draft.actor} onChange={e => set('actor', e.target.value as AuditActor | '')}>
            <option value="">Todos</option>
            {(Object.keys(ACTOR_LABEL) as AuditActor[]).map(a => <option key={a} value={a}>{ACTOR_LABEL[a]}</option>)}
          </select>
        </label>
        <label>Buscar (nombre o ruta)
          <input value={draft.text} maxLength={100} onChange={e => set('text', e.target.value)} placeholder="Ana, /slots, login…" />
        </label>
        <label>Tipo
          <select value={draft.method} onChange={e => set('method', e.target.value)}>
            <option value="">Todos</option>
            <option value="POST">Crear (POST)</option>
            <option value="PUT">Cambiar (PUT)</option>
            <option value="DELETE">Borrar (DELETE)</option>
          </select>
        </label>
        <label>Desde<input type="date" value={draft.from} onChange={e => set('from', e.target.value)} /></label>
        <label>Hasta<input type="date" value={draft.to} onChange={e => set('to', e.target.value)} /></label>
        <div className="row">
          <button type="submit">Filtrar</button>
          <button type="button" className="secondary" onClick={clear}>Quitar filtros</button>
        </div>
      </form>

      {result.error && <p className="error" role="alert">{result.error}</p>}
      {!data && !result.error && <LoadingBlock />}
      {data && (
        <>
          <p className="muted" role="status">
            {data.total === 0 ? 'No hay registros con estos filtros.' : `${data.total} registro${data.total === 1 ? '' : 's'} · página ${data.page} de ${pages}`}
          </p>
          {data.items.length > 0 && (
            <div className="table-wrap">
              <table className="standings audit-table">
                <caption className="sr-only">Acciones registradas, de la más reciente a la más antigua</caption>
                <thead>
                  <tr><th scope="col" className="left">Fecha</th><th scope="col" className="left">Quién</th>
                    <th scope="col" className="left">Acción</th><th scope="col">Resultado</th><th scope="col" className="opt">ms</th></tr>
                </thead>
                <tbody>
                  {data.items.map(a => (
                    <tr key={a.id}>
                      <td className="left nowrap">{when(a.atUtc)}</td>
                      <td className="left"><span className={`badge actor-${a.actor}`}>{ACTOR_LABEL[a.actor]}</span>{' '}
                        {a.actor !== 'Visitor' && <strong>{a.actorName}</strong>}</td>
                      <td className="left">{describeAction(a.method, a.path)}
                        <div className="muted audit-raw">{a.method} {a.path}</div></td>
                      <td><span className={`audit-status ${statusClass(a.status)}`}>{a.status}</span></td>
                      <td className="opt">{a.durationMs}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {pages > 1 && (
            <nav className="row" aria-label="Páginas del registro">
              <button className="secondary" disabled={data.page <= 1} onClick={() => goTo(data.page - 1)}>← Más recientes</button>
              <button className="secondary" disabled={data.page >= pages} onClick={() => goTo(data.page + 1)}>Más antiguos →</button>
            </nav>
          )}
        </>
      )}
      <p><Link to="/mas">← Volver a Más</Link></p>
    </>
  )
}
