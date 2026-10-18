import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { PairingPreview, Player, Position } from '../api/types'
import Disclosure from '../components/Disclosure'
import PageHeader from '../components/PageHeader'
import PlayerPicker from '../components/PlayerPicker'
import PlayersTabs from '../components/PlayersTabs'
import { useAuth } from '../auth/AuthContext'
import { useAsync, useDocumentTitle } from '../hooks'
import LoadingBlock from '../components/LoadingBlock'
import PlayerName from '../components/PlayerName'

export default function PlayersPage() {
  useDocumentTitle('Jugadores')
  const { isAdmin } = useAuth()
  const vista = useSearchParams()[0].get('vista') === 'sorteos' ? 'sorteos' : 'lista'
  const { data: players, error, loading, reload } = useAsync(() => api.players.list(), [], '/api/players')
  const [name, setName] = useState('')
  const [position, setPosition] = useState<Position>('Delantero')
  const [groupInput, setGroupInput] = useState('')
  const [assignGroup, setAssignGroup] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [preview, setPreview] = useState<PairingPreview | null>(null)
  const [randomPair, setRandomPair] = useState<[Player, Player] | null>(null)
  const [randomMatch, setRandomMatch] = useState<[[Player, Player], [Player, Player]] | null>(null)
  const [swapping, setSwapping] = useState<Set<number>>(new Set())
  // Cambios de posición locales (visitantes): solo valen para el sorteo en pantalla, no se guardan.
  const [localPos, setLocalPos] = useState<Map<number, Position>>(new Map())
  const view: Player[] = (players ?? []).map(p => (localPos.has(p.id) ? { ...p, position: localPos.get(p.id)! } : p))

  const pick = <T,>(list: T[]) => list[Math.floor(Math.random() * list.length)]
  const chosen = view.filter(p => selected.has(p.id))
  const chosenForwards = chosen.filter(p => p.position === 'Delantero')
  const chosenBacks = chosen.filter(p => p.position === 'Zaguero')
  const canDraw = chosenForwards.length > 0 && chosenBacks.length > 0
  const canDrawMatch = chosenForwards.length >= 2 && chosenBacks.length >= 2
  /** Baraja una copia de la lista (Fisher-Yates). */
  const shuffle = <T,>(list: T[]) => {
    const a = [...list]
    for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]] }
    return a
  }
  /** Dos parejas distintas (delantero + zaguero cada una), sin repetir jugadores. */
  function drawRandomMatch() {
    if (!canDrawMatch) return
    const [f1, f2] = shuffle(chosenForwards)
    const [b1, b2] = shuffle(chosenBacks)
    setRandomMatch([[f1, b1], [f2, b2]])
  }
  function drawRandomPair() {
    if (!canDraw) return
    setRandomPair([pick(chosenForwards), pick(chosenBacks)])
  }

  async function guard(action: () => Promise<unknown>) {
    setMessage(null)
    try { await action() } catch (e) { setMessage(e instanceof Error ? e.message : 'Error') }
  }

  const add = (e: FormEvent) => {
    e.preventDefault()
    void guard(async () => { await api.players.create(name, position, groupInput === '' ? null : Number(groupInput)); notify(`Jugador «${name.trim()}» creado`); setName(''); await reload() })
  }

  async function deleteSelected() {
    const n = selected.size
    if (!(await confirmDelete(`¿Eliminar ${n} jugador(es)?`))) return
    await Promise.all([...selected].map(remove))
    notify(`${n} jugador(es) eliminado(s)`)
  }

  /** Asigna (o quita, con null) el grupo a todos los jugadores marcados. */
  async function applyGroup(group: number | null) {
    if (group !== null && (!Number.isInteger(group) || group < 1)) { setMessage('Indica un número de grupo válido (1, 2, 3…).'); return }
    const targets = view.filter(p => selected.has(p.id))
    await guard(async () => {
      await Promise.all(targets.map(p => api.players.update(p.id, p.name, p.position, group)))
      notify(group === null ? `${targets.length} jugador(es) sin grupo` : `${targets.length} jugador(es) asignados al grupo ${group}`)
      await reload()
    })
  }

  const remove = (id: number) => guard(async () => {
    await api.players.remove(id)
    setSelected(prev => { const n = new Set(prev); n.delete(id); return n })
    await reload()
  })

  function swapPosition(p: Player) {
    const next: Position = p.position === 'Delantero' ? 'Zaguero' : 'Delantero'
    setRandomPair(null)
    setRandomMatch(null)
    if (!isAdmin) {
      setLocalPos(prev => new Map(prev).set(p.id, next))
      return
    }
    // Administrador: se guarda en la base de datos y afecta a todas las pantallas.
    setSwapping(prev => new Set(prev).add(p.id))
    void guard(async () => { await api.players.update(p.id, p.name, next, p.groupNumber ?? null); await reload() })
      .finally(() => setSwapping(prev => { const n = new Set(prev); n.delete(p.id); return n }))
  }

  const makePreview = () => guard(async () => setPreview(await api.players.pairingPreview([...selected])))

  const picker = (
    <PlayerPicker players={view} selected={selected} onChange={setSelected}
      onSwap={swapPosition} swappingIds={swapping} />
  )

  return (
    <>
      <PageHeader title="Jugadores" subtitle={`${players?.length ?? 0} jugadores`} />
      <PlayersTabs current={vista} />
      {loading && !players && <LoadingBlock />}
      {((error && !players) || message) && <p className="error">{(error && !players ? error : null) ?? message}</p>}

      {vista === 'lista' && (
        <>
          {isAdmin && (
            <details className="card add-player">
              <summary>+ Añadir jugador</summary>
              <form className="form row" onSubmit={add}>
                <input placeholder="Nombre" aria-label="Nombre" value={name} onChange={e => setName(e.target.value)} required />
                <select aria-label="Posición" value={position} onChange={e => setPosition(e.target.value as Position)}>
                  <option value="Delantero">Delantero</option>
                  <option value="Zaguero">Zaguero</option>
                </select>
                <input type="number" min={1} max={99} placeholder="Grupo (opcional)" aria-label="Grupo de jugadores"
                  value={groupInput} onChange={e => setGroupInput(e.target.value)} />
                <button type="submit">Añadir</button>
              </form>
            </details>
          )}

          <section className="card" aria-label="Lista de jugadores">
            <p className="muted">
              Marca jugadores para sortear o gestionar. Con «⇄» cambias un jugador de delantero a zaguero y al revés
              {isAdmin ? ' (se guarda para todos).' : ' (solo en pantalla, no se guarda).'}
            </p>
            {picker}
          </section>

          {isAdmin && selected.size > 0 && (
            <section className="card selection-bar" aria-label="Acciones sobre los jugadores marcados">
              <strong>{selected.size} marcado(s)</strong>
              <div className="row">
                <input type="number" min={1} max={99} placeholder="Nº de grupo" aria-label="Número de grupo"
                  value={assignGroup} onChange={e => setAssignGroup(e.target.value)} />
                <button className="secondary" disabled={assignGroup === ''}
                  onClick={() => void applyGroup(Number(assignGroup))}>Asignar grupo</button>
                <button className="secondary" onClick={() => void applyGroup(null)}>Quitar grupo</button>
                <button className="secondary danger" onClick={() => void deleteSelected()}>Borrar</button>
              </div>
            </section>
          )}
        </>
      )}

      {vista === 'sorteos' && (
        <>
          <Disclosure className="card present" defaultOpen={selected.size === 0} summary={`Jugadores que participan (${selected.size})`}>
            {picker}
          </Disclosure>

          <div className="draw-grid">
            <section className="card">
              <h2>Pareja al azar</h2>
              <p className="muted">Un delantero y un zaguero ({chosenForwards.length} delanteros, {chosenBacks.length} zagueros).</p>
              {randomPair && (
                <p className="random-pair">
                  <strong><PlayerName playerId={randomPair[0].id}>{randomPair[0].name}</PlayerName></strong> <span className="muted">(Delantero)</span> +{' '}
                  <strong><PlayerName playerId={randomPair[1].id}>{randomPair[1].name}</PlayerName></strong> <span className="muted">(Zaguero)</span>
                </p>
              )}
              <div className="row">
                <button onClick={drawRandomPair} disabled={!canDraw}>
                  {randomPair ? 'Sortear otra pareja' : 'Obtener pareja al azar'}
                </button>
              </div>
              {!canDraw && <p className="muted">Marca al menos un delantero y un zaguero.</p>}
            </section>

            <section className="card">
              <h2>Partido al azar</h2>
              <p className="muted">Dos parejas con delantero y zaguero ({chosenForwards.length} delanteros, {chosenBacks.length} zagueros).</p>
              {randomMatch && (
                <div className="random-match">
                  {randomMatch.map((pair, i) => (
                    <p key={i} className="random-pair">
                      <strong><PlayerName playerId={pair[0].id}>{pair[0].name}</PlayerName></strong> <span className="muted">(Delantero)</span> +{' '}
                      <strong><PlayerName playerId={pair[1].id}>{pair[1].name}</PlayerName></strong> <span className="muted">(Zaguero)</span>
                    </p>
                  ))}
                </div>
              )}
              <div className="row">
                <button onClick={drawRandomMatch} disabled={!canDrawMatch}>
                  {randomMatch ? 'Sortear otro partido' : 'Obtener partido al azar'}
                </button>
              </div>
              {!canDrawMatch && <p className="muted">Marca al menos dos delanteros y dos zagueros.</p>}
            </section>
          </div>

          {isAdmin && (
            <section className="card">
              <h2>Emparejar a todos</h2>
              <p className="muted">Prueba cómo quedarían las parejas con todos los marcados. Es solo una vista previa: las parejas se crean al inscribirlas en una competición.</p>
              <div className="row">
                <button className="secondary" disabled={selected.size < 2} onClick={() => void makePreview()}>
                  Emparejar al azar ({selected.size})
                </button>
              </div>
              {preview && (
                <>
                  <ul className="plain">
                    {preview.pairs.map((p, i) => (
                      <li key={i}><PlayerName playerId={p.player1.id}>{p.player1.name}</PlayerName> <span className="muted">({p.player1.position})</span> + <PlayerName playerId={p.player2.id}>{p.player2.name}</PlayerName>{' '}
                        <span className="muted">({p.player2.position})</span></li>
                    ))}
                  </ul>
                  {preview.unpaired && <p className="muted">Sin pareja: <PlayerName playerId={preview.unpaired.id}>{preview.unpaired.name}</PlayerName></p>}
                </>
              )}
            </section>
          )}
        </>
      )}
    </>
  )
}
