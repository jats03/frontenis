import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { PairingPreview, Player, Position } from '../api/types'
import PlayerPicker from '../components/PlayerPicker'
import { useAuth } from '../auth/AuthContext'
import { useAsync } from '../hooks'

export default function PlayersPage() {
  const { isAdmin } = useAuth()
  const { data: players, error, loading, reload } = useAsync(() => api.players.list(), [])
  const [name, setName] = useState('')
  const [position, setPosition] = useState<Position>('Delantero')
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
    void guard(async () => { await api.players.create(name, position); setName(''); await reload() })
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
    void guard(async () => { await api.players.update(p.id, p.name, next); await reload() })
      .finally(() => setSwapping(prev => { const n = new Set(prev); n.delete(p.id); return n }))
  }

  const makePreview = () => guard(async () => setPreview(await api.players.pairingPreview([...selected])))

  return (
    <>
      <h2>Jugadores</h2>
      {loading && <p className="muted">Cargando…</p>}
      {(error || message) && <p className="error">{error ?? message}</p>}

      {isAdmin && (
        <form className="card form row" onSubmit={add}>
          <input placeholder="Nombre" value={name} onChange={e => setName(e.target.value)} required />
          <select value={position} onChange={e => setPosition(e.target.value as Position)}>
            <option value="Delantero">Delantero</option>
            <option value="Zaguero">Zaguero</option>
          </select>
          <button type="submit">Añadir</button>
        </form>
      )}

      <section className="card">
        <p className="muted">
          Marca los jugadores que participan en el sorteo (o usa «Seleccionar todos»).
          Con «⇄» puedes pasar un jugador de delantero a zaguero y al revés
          {isAdmin ? ' (se guarda para todos).' : ' (solo para este sorteo, no se guarda).'}
        </p>
        <PlayerPicker players={view} selected={selected} onChange={setSelected}
          onSwap={swapPosition} swappingIds={swapping} />
      </section>

      <section className="card">
        <h3>Pareja al azar</h3>
        <p className="muted">
          Sortea un delantero y un zaguero entre los jugadores marcados
          ({chosenForwards.length} delanteros, {chosenBacks.length} zagueros).
        </p>
        <div className="row">
          <button onClick={drawRandomPair} disabled={!canDraw}>
            {randomPair ? 'Sortear otra pareja' : 'Obtener pareja al azar'}
          </button>
        </div>
        {!canDraw && <p className="muted">Marca al menos un delantero y un zaguero.</p>}
        {randomPair && (
          <p className="random-pair">
            <strong>{randomPair[0].name}</strong> <span className="muted">(Delantero)</span> +{' '}
            <strong>{randomPair[1].name}</strong> <span className="muted">(Zaguero)</span>
          </p>
        )}
      </section>

      <section className="card">
        <h3>Partido al azar</h3>
        <p className="muted">
          Sortea dos parejas (cada una con un delantero y un zaguero) entre los jugadores marcados
          ({chosenForwards.length} delanteros, {chosenBacks.length} zagueros).
        </p>
        <div className="row">
          <button onClick={drawRandomMatch} disabled={!canDrawMatch}>
            {randomMatch ? 'Sortear otro partido' : 'Obtener partido al azar'}
          </button>
        </div>
        {!canDrawMatch && <p className="muted">Marca al menos dos delanteros y dos zagueros.</p>}
        {randomMatch && (
          <div className="random-match">
            {randomMatch.map((pair, i) => (
              <p key={i} className="random-pair">
                <strong>{pair[0].name}</strong> <span className="muted">(Delantero)</span> +{' '}
                <strong>{pair[1].name}</strong> <span className="muted">(Zaguero)</span>
              </p>
            ))}
          </div>
        )}
      </section>

      {isAdmin && (
        <>
          <div className="row">
            <button disabled={selected.size < 2} onClick={() => void makePreview()}>
              Emparejar al azar ({selected.size})
            </button>
            <button className="secondary" disabled={selected.size === 0}
              onClick={() => { if (confirm(`¿Borrar ${selected.size} jugador(es) seleccionado(s)? No se puede deshacer.`)) void Promise.all([...selected].map(remove)) }}>
              Borrar seleccionados
            </button>
          </div>
          {preview && (
            <section className="card">
              <h3>Vista previa</h3>
              <ul className="plain">
                {preview.pairs.map((p, i) => (
                  <li key={i}>{p.player1.name} <span className="muted">({p.player1.position})</span> + {p.player2.name}{' '}
                    <span className="muted">({p.player2.position})</span></li>
                ))}
              </ul>
              {preview.unpaired && <p className="muted">Sin pareja: {preview.unpaired.name}</p>}
              <p className="muted">Es solo una prueba: las parejas se crean al inscribirlas en una competición.</p>
            </section>
          )}
        </>
      )}
    </>
  )
}
