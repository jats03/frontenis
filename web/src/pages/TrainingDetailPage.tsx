import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import Disclosure from '../components/Disclosure'
import ActionMenu from '../components/ActionMenu'
import PageHeader from '../components/PageHeader'
import { formatShortDate } from '../dates'
import { confirmDelete, notify, promptText } from '../alerts'
import { api } from '../api/client'
import type { Player, TrainingDetail, TrainingRow, TrainingSuggestion } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import PlayerPicker from '../components/PlayerPicker'
import SubTabs from '../components/SubTabs'
import { useAsync, useDocumentTitle } from '../hooks'
import LoadingBlock from '../components/LoadingBlock'

type Slots = [string, string, string, string]
const EMPTY: Slots = ['', '', '', '']

function Standings({ rows, label }: { rows: TrainingRow[]; label: string }) {
  if (rows.length === 0) return <p className="muted">Aún no hay partidos.</p>
  return (
    <div className="table-wrap">
      <table className="standings">
        <caption className="sr-only">Clasificación {label}</caption>
        <thead>
          <tr>
            <th scope="col">#</th><th scope="col" className="left">{label === 'por parejas' ? 'Pareja' : 'Jugador'}</th>
            <th scope="col" title="Jugados">PJ</th><th scope="col" title="Ganados">G</th><th scope="col" title="Perdidos">P</th>
            <th scope="col" className="opt" title="Tantos a favor">TF</th><th scope="col" className="opt" title="Tantos en contra">TC</th>
            <th scope="col" title="Diferencia">Dif</th><th scope="col" title="Puntos de clasificación">Pts</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(r => (
            <tr key={r.playerIds.join('-')}>
              <td>{r.rank}</td><td className="left">{r.name}</td>
              <td>{r.played}</td><td>{r.won}</td><td>{r.lost}</td>
              <td className="opt">{r.pointsFor}</td><td className="opt">{r.pointsAgainst}</td>
              <td>{r.pointsDiff > 0 ? `+${r.pointsDiff}` : r.pointsDiff}</td>
              <td><strong>{r.classificationPoints}</strong></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default function TrainingDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const { isAdmin } = useAuth()
  const { data, setData, error, loading } = useAsync(() => api.trainings.get(id), [id], `/api/trainings/${id}`)
  const players = useAsync(() => api.players.list(), [], '/api/players')
  useDocumentTitle(data?.summary.name ?? 'Entrenamiento')

  const [tab, setTab] = useState<'pairs' | 'players'>('pairs')
  const [showMatches, setShowMatches] = useState(true)
  const [formOpen, setFormOpen] = useState(false)
  const [slots, setSlots] = useState<Slots>(EMPTY)
  const [homeScore, setHomeScore] = useState('')
  const [awayScore, setAwayScore] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [presentChosen, setPresentChosen] = useState<Set<number> | null>(null) // null = los que ya han jugado
  const [suggestions, setSuggestions] = useState<TrainingSuggestion[] | null>(null)
  const [suggestError, setSuggestError] = useState<string | null>(null)

  const list: Player[] = useMemo(() => players.data ?? [], [players.data])
  const nameOf = useMemo(() => new Map(list.map(p => [p.id, p.name])), [list])

  // Presentes por defecto: quienes ya han jugado en la sesión (hasta que se toque la selección).
  const present = useMemo(
    () => presentChosen ?? new Set(data?.matches.flatMap(m => [...m.homePlayerIds, ...m.awayPlayerIds]) ?? []),
    [presentChosen, data])

  async function addMatch() {
    setFormError(null)
    const [h1, h2, a1, a2] = slots.map(Number)
    if (slots.some(s => s === '')) { setFormError('Elige los cuatro jugadores.'); return }
    if (homeScore === '' || awayScore === '') { setFormError('Introduce el marcador.'); return }
    try {
      const updated = await api.trainings.addMatch(id, {
        home1Id: h1, home2Id: h2, away1Id: a1, away2Id: a2,
        homeScore: Number(homeScore), awayScore: Number(awayScore),
      })
      commit(updated)
      notify('Partido añadido')
      setSlots(EMPTY); setHomeScore(''); setAwayScore('')
      setFormOpen(false)
      if (suggestions) void suggest()
    } catch (e) { setFormError(e instanceof Error ? e.message : 'Error') }
  }

  function commit(d: TrainingDetail) {
    setData(d)
  }

  async function removeMatch(matchId: number) {
    if (!(await confirmDelete('¿Borrar este partido?'))) return
    try { commit(await api.trainings.removeMatch(id, matchId)); notify('Partido eliminado'); if (suggestions) void suggest() }
    catch (e) { setFormError(e instanceof Error ? e.message : 'Error') }
  }

  async function rename() {
    if (!data) return
    const name = await promptText('Editar nombre', data.summary.name, 'Nombre del entrenamiento')
    if (name === null || name.trim() === data.summary.name) return
    try {
      const t = await api.trainings.rename(id, name)
      setData({ ...data, summary: { ...data.summary, name: t.name } })
      notify('Nombre actualizado')
    } catch (e) { setFormError(e instanceof Error ? e.message : 'Error') }
  }

  async function removeSession() {
    if (!(await confirmDelete('¿Eliminar este entrenamiento?', 'Se borrarán todos sus partidos.'))) return
    try { await api.trainings.remove(id); notify('Entrenamiento eliminado'); navigate('/entrenamientos') }
    catch (e) { setFormError(e instanceof Error ? e.message : 'Error') }
  }

  async function suggest() {
    setSuggestError(null)
    try { setSuggestions(await api.trainings.suggest(id, [...present], 5)) }
    catch (e) { setSuggestError(e instanceof Error ? e.message : 'Error'); setSuggestions(null) }
  }

  function applySuggestion(s: TrainingSuggestion) {
    setSlots([String(s.homePlayerIds[0]), String(s.homePlayerIds[1]), String(s.awayPlayerIds[0]), String(s.awayPlayerIds[1])])
    setHomeScore(''); setAwayScore('')
    setFormOpen(true)
    setTimeout(() => document.getElementById('nuevo-partido')?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 50)
  }

  if (error && !data) return <p className="error">{error}</p>
  if (!data) return loading ? <LoadingBlock /> : null

  const s = data.summary
  const slotSelect = (i: number, label: string) => (
    <label>{label}
      <select value={slots[i]} onChange={e => setSlots(prev => prev.map((v, k) => (k === i ? e.target.value : v)) as Slots)}>
        <option value="">—</option>
        {list.filter(p => !slots.some((v, k) => k !== i && v === String(p.id))).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
      </select>
    </label>
  )

  return (
    <>
      <PageHeader title={s.name}
        subtitle={<>{formatShortDate(s.date)} · {s.pointsPerWin} pts por victoria{s.pointsPerLoss > 0 && ` · ${s.pointsPerLoss} por derrota`}</>}
        actions={<>
          {!formOpen && <button onClick={() => setFormOpen(true)}>+ Registrar partido</button>}
          <ActionMenu items={[
            { label: 'Todas las sesiones', onSelect: () => navigate('/entrenamientos') },
            { label: 'Editar nombre', hidden: !isAdmin, onSelect: () => void rename() },
            { label: 'Eliminar sesión', danger: true, hidden: !isAdmin, onSelect: () => void removeSession() },
          ]} />
        </>} />
      {!formOpen && formError && <p className="error" role="alert">{formError}</p>}

      <div className="training-layout">
        <div>
          {formOpen && (
            <section className="card form" id="nuevo-partido" aria-label="Registrar partido">
              <h2>Registrar partido</h2>
              <p className="muted">Cualquier persona puede registrar un partido; solo el administrador puede borrarlos.</p>
              <div className="cols">
                {slotSelect(0, 'Pareja A · jugador 1')}{slotSelect(1, 'Pareja A · jugador 2')}
              </div>
              <div className="cols">
                {slotSelect(2, 'Pareja B · jugador 1')}{slotSelect(3, 'Pareja B · jugador 2')}
              </div>
              <div className="cols">
                <label>Marcador pareja A<input inputMode="numeric" value={homeScore} onChange={e => setHomeScore(e.target.value)} /></label>
                <label>Marcador pareja B<input inputMode="numeric" value={awayScore} onChange={e => setAwayScore(e.target.value)} /></label>
              </div>
              {formError && <p className="error">{formError}</p>}
              <div className="row">
                <button onClick={() => void addMatch()}>Guardar partido</button>
                <button className="secondary" onClick={() => { setFormOpen(false); setFormError(null) }}>Cancelar</button>
              </div>
            </section>
          )}

          <section className="card">
            <div className="row between">
              <h2>Partidos jugados ({data.matches.length})</h2>
              <button className="secondary small" aria-expanded={showMatches} onClick={() => setShowMatches(v => !v)}>
                {showMatches ? 'Ocultar partidos' : 'Mostrar partidos'}
              </button>
            </div>
            {showMatches && (data.matches.length === 0 ? <p className="muted">Todavía no se ha jugado ningún partido.</p> : (
              <ol className="training-matches">
                {[...data.matches].reverse().map(m => (
                  <li key={m.id} value={m.order}>
                    <div className="tm-teams">
                      <span className={m.homeWon ? 'team winner' : 'team'}>{m.homeName}</span>
                      <span className={!m.homeWon ? 'team winner' : 'team'}>{m.awayName}</span>
                    </div>
                    <div className="tm-score">
                      <strong>{m.homeScore} - {m.awayScore}</strong>
                    </div>
                    {isAdmin && <button className="secondary small tm-del" onClick={() => void removeMatch(m.id)}>Borrar</button>}
                  </li>
                ))}
              </ol>
            ))}
          </section>
        </div>

        <section className="card">
          <h2>Clasificación</h2>
          <SubTabs label="Tipo de clasificación" value={tab} onChange={k => setTab(k as 'pairs' | 'players')}
            options={[{ key: 'pairs', label: 'Por parejas' }, { key: 'players', label: 'Individual' }]} />
          {tab === 'pairs'
            ? <Standings rows={data.pairStandings} label="por parejas" />
            : <Standings rows={data.playerStandings} label="individual" />}
        </section>
      </div>

      <section className="card">
        <h2>Siguiente partido</h2>
        <details className="how">
          <summary>¿Cómo se elige?</summary>
          <p className="muted">
            Primero, que todos jueguen parecido (prioridad a quien ha jugado menos y descanso a quien jugó el último partido).
            Después, parejas lo más igualadas posible según el nivel de cada jugador (porcentaje de victorias suavizado, con un
            pequeño ajuste por el margen de tantos). Se evita repetir compañero o el mismo cruce.
          </p>
        </details>
        <Disclosure className="present" defaultOpen={present.size < 4} summary={`Jugadores presentes (${present.size})`}>
          <PlayerPicker players={list} selected={present} onChange={setPresentChosen} />
        </Disclosure>
        <div className="row">
          <button disabled={present.size < 4} onClick={() => void suggest()}>
            {suggestions ? 'Recalcular sugerencias' : 'Sugerir siguiente partido'}
          </button>
          {present.size < 4 && <span className="muted">Marca al menos 4 jugadores.</span>}
        </div>
        {suggestError && <p className="error">{suggestError}</p>}
        {suggestions && suggestions.length > 0 && (
          <ol className="suggestions">
            {suggestions.map((sg, i) => (
              <li key={i} className={i === 0 ? 'best' : ''}>
                <div>
                  {i === 0 && <strong>Recomendado: </strong>}
                  <span>{sg.homeName}</span> <span className="muted">vs</span> <span>{sg.awayName}</span>
                </div>
                <small className="muted">
                  Nivel {sg.homeLevel} vs {sg.awayLevel} · {sg.reason}
                </small>
                <div><button className="secondary small" onClick={() => applySuggestion(sg)}>Usar este partido</button></div>
              </li>
            ))}
          </ol>
        )}
        {suggestions && suggestions.length > 0 && nameOf.size > 0 && (
          <p className="muted">Sugerencias para: {[...present].map(pid => nameOf.get(pid)).filter(Boolean).join(', ')}.</p>
        )}
      </section>
    </>
  )
}
