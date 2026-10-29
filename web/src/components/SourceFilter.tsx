import { useSearchParams } from 'react-router-dom'
import { BALLS, DEFAULT_BALL, type BallType } from '../balls'
import BallPicker from './BallPicker'
import { api } from '../api/client'
import type { HistoryFilter } from '../api/types'
import { useAsync } from '../hooks'

const parse = (v: string | null): number[] =>
  (v ?? '').split(',').map(Number).filter(n => Number.isInteger(n) && n > 0)

/** Bola de la URL (?b=Slow): una sola; si no hay ninguna válida, la preolímpica (con un enlace antiguo de varias, la primera). */
const parseBall = (v: string | null): BallType => {
  const wanted = (v ?? '').split(',')
  return BALLS.find(b => wanted.includes(b.key))?.key ?? DEFAULT_BALL
}

/** El filtro vive en la URL (?c=1,2&t=3&b=Slow) para poder compartirlo y mantenerlo al navegar entre páginas del histórico. */
export function useHistoryFilter(): [HistoryFilter, (f: HistoryFilter) => void, string] {
  const [params, setParams] = useSearchParams()
  const filter: HistoryFilter = { competitionIds: parse(params.get('c')), trainingIds: parse(params.get('t')), balls: [parseBall(params.get('b'))] }
  const set = (f: HistoryFilter) => {
    const next = new URLSearchParams()
    if (f.competitionIds.length) next.set('c', f.competitionIds.join(','))
    if (f.trainingIds.length) next.set('t', f.trainingIds.join(','))
    const ball = f.balls?.[0] ?? DEFAULT_BALL
    if (ball !== DEFAULT_BALL) next.set('b', ball) // la bola por defecto no hace falta ponerla en la URL
    setParams(next, { replace: true })
  }
  return [filter, set, params.toString() ? `?${params.toString()}` : '']
}

interface GroupProps {
  legend: string
  items: { id: number; name: string }[]
  selected: number[]
  onChange: (ids: number[]) => void
}

function Group({ legend, items, selected, onChange }: GroupProps) {
  const all = items.length > 0 && selected.length === items.length
  const toggle = (id: number) =>
    onChange(selected.includes(id) ? selected.filter(x => x !== id) : [...selected, id])
  return (
    <fieldset className="source-group">
      <legend>{legend} <span className="muted">({selected.length}/{items.length})</span></legend>
      {items.length === 0 ? <p className="muted">No hay ninguno.</p> : (
        <>
          <label className="check select-all">
            <input type="checkbox" checked={all}
              ref={el => { if (el) el.indeterminate = selected.length > 0 && !all }}
              onChange={() => onChange(all ? [] : items.map(i => i.id))} />
            Seleccionar todos
          </label>
          <div className="source-list">
            {items.map(i => (
              <label className="check" key={i.id}>
                <input type="checkbox" checked={selected.includes(i.id)} onChange={() => toggle(i.id)} />
                {i.name}
              </label>
            ))}
          </div>
        </>
      )}
    </fieldset>
  )
}

/** Elige de qué competiciones y/o entrenamientos sale el histórico (ninguno marcado = todos). */
export default function SourceFilter({ value, onChange }: { value: HistoryFilter; onChange: (f: HistoryFilter) => void }) {
  const comps = useAsync(() => api.competitions.list(), [], '/api/competitions')
  const trainings = useAsync(() => api.trainings.list(), [], '/api/trainings')
  const count = value.competitionIds.length + value.trainingIds.length
  return (
    <>
    {/* Siempre hay una bola (y solo una) marcada: no se puede dejar sin elegir ni elegir varias. */}
    <div className="card ball-filter">
      <BallPicker value={value.balls?.[0] ?? DEFAULT_BALL} onChange={ball => onChange({ ...value, balls: [ball] })} />
      <p className="muted">Solo cuentan los partidos (de entrenamiento y de competición) jugados con esta bola. Siempre hay una, y solo una, marcada.</p>
    </div>
    <details className="card source-filter" open={count > 0}>
      <summary>
        Filtrar partidos: {count === 0 ? 'todos (competiciones y entrenamientos)'
          : `${value.competitionIds.length} competición(es) y ${value.trainingIds.length} entrenamiento(s)`}
      </summary>
      <p className="muted">Marca una, varias o todas. Sin nada marcado se incluye todo.</p>
      <div className="source-groups">
        <Group legend="Competiciones" items={comps.data ?? []} selected={value.competitionIds}
          onChange={ids => onChange({ ...value, competitionIds: ids })} />
        <Group legend="Entrenamientos" items={(trainings.data ?? []).map(t => ({ id: t.id, name: t.name }))}
          selected={value.trainingIds} onChange={ids => onChange({ ...value, trainingIds: ids })} />
      </div>
      {count > 0 && <button className="secondary" onClick={() => onChange({ ...value, competitionIds: [], trainingIds: [] })}>Quitar filtros</button>}
    </details>
    </>
  )
}
