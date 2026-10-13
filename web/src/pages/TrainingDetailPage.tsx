import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { Player, TrainingDetail, TrainingRow, TrainingSuggestion } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import PlayerPicker from '../components/PlayerPicker'
import StaleNotice from '../components/StaleNotice'
import SubTabs from '../components/SubTabs'
import { useAsync, useDocumentTitle } from '../hooks'

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
  const { data, setData, error, loading, savedAt } = useAsync(() => api.trainings.get(id), [id], `/api/trainings/${id}`)
  const players = useAsync(() => api.players.list(), [], '/api/players')
  useDocumentTitle(data?.summary.name ?? 'Entrenamiento')

  const [tab, setTab] = useState<'pairs' | 'players'>('pairs')
  const [showMatches, setShowMatches] = useState(true)
  const [slots, setSlots] = useState<Slots>(EMPTY)
  const [target, setTarget] = useState(15)
  const [homeScore, setHomeScore] = useState('')
  const [awayScore, setAwayScore] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [present, setPresent] = useState<Set<number>>(new Set())
  const [presentTouched, setPresentTouched] = useState(false)
  const [suggestions, setSuggestions] = useState<TrainingSuggestion[] | null>(null)
  const [suggestError, setSuggestError] = useState<string | null>(null)

  const list: Player[] = players.data ?? []
  const nameOf = useMemo(() => new Map(list.map(p => [p.id, p.name])), [list])

  // Presentes por defecto: quienes ya han jugado en la sesión (hasta que se toque la selección).
  useEffect(() => {
    if (presentTouched || !data) return
    setPresent(new Set(data.matches.flatMap(m => [...m.homePlayerIds, ...m.awayPlayerIds])))
  }, [data, presentTouched])

  async function addMatch() {
    setFormError(null)
    const [h1, h2, a1, a2] = slots.map(Number)
    if (slots.some(s => s === '')) { setFormError('Elige los cuatro jugadores.'); return }
    if (homeScore === '' || awayScore === '') { setFormError('Introduce el marcador.'); return }
    try {
      const updated = await api.trainings.addMatch(id, {
        home1Id: h1, home2Id: h2, away1Id: a1, away2Id: a2,
        targetPoints: target, homeScore: Number(homeScore), awayScore: Number(awayScore),
      })
      commit(updated)
      notify('Partido añadido')
      setSlots(EMPTY); setHomeScore(''); setAwayScore('')
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
    document.getElementById('nuevo-partido')?.scrollIntoView({ behavior: 'smooth' })
  }

  if (error && !data) return <p className="error">{error}</p>
  if (!data) return loading ? <p className="muted">Cargando…</p> : null

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
      <div className="row between">
        <div>
          <h1>{s.name}</h1>
          <p className="muted">
            {new Date(s.date).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })} · {s.pointsPerWin} pts por victoria
            {s.pointsPerLoss > 0 && ` · ${s.pointsPerLoss} por derrota`}
          </p>
        </div>
        <div className="row">
          <Link className="button secondary" to="/entrenamientos">Todas las sesiones</Link>
          {isAdmin && <button className="secondary" onClick={() => void removeSession()}>Eliminar</button>}
        </div>
      </div>
      <StaleNotice savedAt={savedAt} error={error} />

      <div className="training-layout">
        <div>
          {isAdmin && (
            <section className="card form" id="nuevo-partido">
              <h2>Añadir partido</h2>
              <div className="cols">
                {slotSelect(0, 'Pareja A · jugador 1')}{slotSelect(1, 'Pareja A · jugador 2')}
              </div>
              <div className="cols">
                {slotSelect(2, 'Pareja B · jugador 1')}{slotSelect(3, 'Pareja B · jugador 2')}
              </div>
              <div className="cols">
                <label>Se juega a (tantos)
                  <input type="number" min={1} value={Number.isNaN(target) ? '' : target} onChange={e => setTarget(e.target.valueAsNumber)} />
                </label>
                <label>Marcador pareja A<input inputMode="numeric" value={homeScore} onChange={e => setHomeScore(e.target.value)} /></label>
                <label>Marcador pareja B<input inputMode="numeric" value={awayScore} onChange={e => setAwayScore(e.target.value)} /></label>
              </div>
              {formError && <p className="error">{formError}</p>}
              <button onClick={() => void addMatch()}>Añadir partido</button>
            </section>
          )}

          <section className="card">
            <div className="row between">
              <h2>Partidos jugados ({data.matches.length})</h2>
              <button className="secondary" aria-expanded={showMatches} onClick={() => setShowMatches(v => !v)}>
                {showMatches ? 'Ocultar partidos' : 'Mostrar partidos'}
              </button>
            </div>
            {showMatches && (data.matches.length === 0 ? <p className="muted">Todavía no se ha jugado ningún partido.</p> : (
              <ol className="training-matches">
                {[...data.matches].reverse().map(m => (
                  <li key={m.id} value={m.order}>
                    <span className={m.homeWon ? 'team winner' : 'team'}>{m.homeName}</span>
                    <strong className="score"> {m.homeScore} - {m.awayScore} </strong>
                    <span className={!m.homeWon ? 'team winner' : 'team'}>{m.awayName}</span>
                    <small className="muted"> (a {m.targetPoints})</small>
                    {isAdmin && <button className="secondary small" onClick={() => void removeMatch(m.id)}>Borrar</button>}
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
        <p className="muted">
          Marca quién está presente y calculo los partidos más recomendables. Criterio: primero que todos jueguen parecido
          (prioridad a quien ha jugado menos y descanso a quien jugó el último partido); después parejas lo más igualadas
          posible, según el nivel de cada jugador (porcentaje de victorias suavizado, con un pequeño ajuste por el margen
          de tantos); y se evita repetir compañero o el mismo cruce.
        </p>
        <PlayerPicker players={list} selected={present} onChange={n => { setPresent(n); setPresentTouched(true) }} />
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
                {isAdmin && <div><button className="secondary small" onClick={() => applySuggestion(sg)}>Usar este partido</button></div>}
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
