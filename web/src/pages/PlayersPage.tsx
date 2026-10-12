import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { PairingPreview, Player, Position } from '../api/types'
import PlayerPicker from '../components/PlayerPicker'
import { useAuth } from '../auth/AuthContext'
import { useAsync } from '../hooks'
import { positionLabel } from '../labels'

export default function PlayersPage() {
  const { isAdmin } = useAuth()
  const { data: players, error, loading, reload } = useAsync(() => api.players.list(), [])
  const [name, setName] = useState('')
  const [position, setPosition] = useState<Position>('Delantero')
  const [message, setMessage] = useState<string | null>(null)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [preview, setPreview] = useState<PairingPreview | null>(null)
  const [randomPair, setRandomPair] = useState<[Player, Player] | null>(null)
  const [randomMsg, setRandomMsg] = useState<string | null>(null)

  const pick = <T,>(list: T[]) => list[Math.floor(Math.random() * list.length)]
  function drawRandomPair() {
    const all = players ?? []
    const pool = selected.size > 0 ? all.filter(p => selected.has(p.id)) : all
    const forwards = pool.filter(p => p.position === 'Delantero')
    const backs = pool.filter(p => p.position === 'Zaguero')
    if (forwards.length === 0 || backs.length === 0) {
      setRandomPair(null)
      setRandomMsg('Hace falta al menos un delantero y un zaguero' + (selected.size > 0 ? ' entre los seleccionados.' : '.'))
      return
    }
    setRandomMsg(null)
    setRandomPair([pick(forwards), pick(backs)])
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
        {isAdmin ? (
          <>
            <p className="muted">Marca jugadores para probar un emparejamiento aleatorio (zaguero + delantero).</p>
            <PlayerPicker players={players ?? []} selected={selected} onChange={setSelected} />
          </>
        ) : (
          <ul className="plain">
            {players?.map(p => <li key={p.id}>{p.name} <span className="muted">· {positionLabel[p.position]}</span></li>)}
          </ul>
        )}
      </section>

      <section className="card">
        <h3>Pareja al azar</h3>
        <p className="muted">
          Sortea un delantero y un zaguero{isAdmin ? ' (de los marcados, o de todos si no hay ninguno marcado)' : ''}.
        </p>
        <div className="row">
          <button onClick={drawRandomPair} disabled={(players?.length ?? 0) < 2}>
            {randomPair ? 'Sortear otra pareja' : 'Obtener pareja al azar'}
          </button>
        </div>
        {randomMsg && <p className="error">{randomMsg}</p>}
        {randomPair && (
          <p className="random-pair">
            <strong>{randomPair[0].name}</strong> <span className="muted">(Delantero)</span> +{' '}
            <strong>{randomPair[1].name}</strong> <span className="muted">(Zaguero)</span>
          </p>
        )}
      </section>

      {isAdmin && (
        <>
          <div className="row">
            <button disabled={selected.size < 2} onClick={() => void makePreview()}>
              Emparejar al azar ({selected.size})
            </button>
            <button className="secondary" disabled={selected.size === 0}
              onClick={() => { if (confirm('¿Borrar los jugadores seleccionados?')) void Promise.all([...selected].map(remove)) }}>
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
