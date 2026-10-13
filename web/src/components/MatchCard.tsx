import { useState } from 'react'
import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { MatchDto, SetScore } from '../api/types'

interface Props {
  match: MatchDto
  canEdit: boolean
  /** 1 o 2 sets por partido (con 2, el 3.º solo si hay empate a 1). */
  setsPerMatch?: number
  onChanged: (m: MatchDto) => void
}

type Cell = { h: string; a: string }

function initial(match: MatchDto): Cell[] {
  const cells: Cell[] = [{ h: '', a: '' }, { h: '', a: '' }, { h: '', a: '' }]
  match.sets?.forEach((s, i) => { if (i < 3) cells[i] = { h: String(s.home), a: String(s.away) } })
  if (!match.sets && match.homeScore !== null && match.awayScore !== null)
    cells[0] = { h: String(match.homeScore), a: String(match.awayScore) }
  return cells
}

const num = (v: string): number | null => (v.trim() !== '' && /^\d+$/.test(v.trim()) ? Number(v) : null)

export default function MatchCard({ match, canEdit, setsPerMatch = 1, onChanged }: Props) {
  const [cells, setCells] = useState<Cell[]>(() => initial(match))
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const ready = match.homeEntryId !== null && match.awayEntryId !== null
  const two = setsPerMatch === 2

  // El 3.er set solo aparece si los dos primeros están completos y cada pareja ganó uno.
  const s1 = [num(cells[0].h), num(cells[0].a)]
  const s2 = [num(cells[1].h), num(cells[1].a)]
  const bothDone = s1.every(v => v !== null) && s2.every(v => v !== null)
  const split = bothDone && (s1[0]! > s1[1]!) !== (s2[0]! > s2[1]!)
  const visible = two ? (split ? 3 : 2) : 1

  function setCell(i: number, side: 'h' | 'a', v: string) {
    setCells(prev => prev.map((c, k) => (k === i ? { ...c, [side]: v } : c)))
  }

  async function run(action: () => Promise<MatchDto>) {
    setBusy(true)
    setError(null)
    try { onChanged(await action()) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  async function clear() {
    if (!(await confirmDelete('¿Borrar el resultado de este partido?'))) return
    await run(async () => { const m = await api.matches.clearResult(match.id); notify('Resultado eliminado'); return m })
    setCells([{ h: '', a: '' }, { h: '', a: '' }, { h: '', a: '' }])
  }

  function save() {
    const sets: SetScore[] = []
    for (let i = 0; i < visible; i++) {
      const h = num(cells[i].h), a = num(cells[i].a)
      if (h === null || a === null) { setError(`Introduce los dos marcadores del set ${i + 1}.`); return }
      if (h === a) { setError(`El set ${i + 1} no puede acabar en empate.`); return }
      sets.push({ home: h, away: a })
    }
    void run(() => api.matches.setResult(match.id, sets))
  }

  const side = (name: string | null, entryId: number | null) => (
    <span className={`team ${match.isPlayed && match.winnerEntryId === entryId ? 'winner' : ''}`}>
      {name ?? 'Por definir'}
    </span>
  )

  const multi = (match.sets?.length ?? 0) > 1
  const readOnly = match.isPlayed
    ? (multi
      ? `${match.homeSets} - ${match.awaySets}`
      : `${match.homeScore} - ${match.awayScore}`)
    : '—'

  return (
    <article className={`match ${match.isPlayed ? 'played' : ''}`}>
      <div className="teams">
        {side(match.homeName, match.homeEntryId)}
        {side(match.awayName, match.awayEntryId)}
      </div>
      {canEdit && ready ? (
        <div className="sets-edit">
          {Array.from({ length: visible }, (_, i) => (
            <div className="score-edit" key={i}>
              {visible > 1 && <span className="set-label">{i === 2 ? 'Desempate' : `Set ${i + 1}`}</span>}
              <input inputMode="numeric" aria-label={`Puntos local, set ${i + 1}`} value={cells[i].h}
                onChange={e => setCell(i, 'h', e.target.value)} />
              <span>-</span>
              <input inputMode="numeric" aria-label={`Puntos visitante, set ${i + 1}`} value={cells[i].a}
                onChange={e => setCell(i, 'a', e.target.value)} />
            </div>
          ))}
          <div className="row">
            <button disabled={busy} onClick={save}>Guardar</button>
            {match.isPlayed && (
              <button className="secondary" disabled={busy} onClick={() => void clear()}>Borrar</button>
            )}
          </div>
        </div>
      ) : (
        <div className="score">
          {readOnly}
          {multi && <small className="muted sets-detail"> ({match.sets!.map(s => `${s.home}-${s.away}`).join(', ')})</small>}
        </div>
      )}
      {error && <p className="error">{error}</p>}
    </article>
  )
}
