import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { PlayerRef } from '../api/types'

interface Props {
  competitionId: number
  groupId: number
  /** Jornadas que ya tiene el grupo. */
  rounds: number[]
  /** Jugadores del grupo. */
  players: PlayerRef[]
  onChanged: () => Promise<void> | void
}

const NEW_ROUND = 'new'
/** Id que entiende la API como «Jugador reserva» (un comodín que no es ningún jugador de la app). */
const RESERVE = '0'

/**
 * Formulario para añadir un partido a cualquier jornada de un grupo (o a una nueva), sin más condiciones.
 * En un hueco se puede poner el «Jugador reserva» (cuando falta un jugador): juega, pero no puntúa ni cuenta en nada.
 */
export default function AddMatchForm({ competitionId, groupId, rounds, players, onChanged }: Props) {
  const last = rounds.length ? Math.max(...rounds) : 0
  const [open, setOpen] = useState(false)
  const [round, setRound] = useState(rounds.length ? String(rounds[0]) : NEW_ROUND)
  const [sel, setSel] = useState<string[]>(['', '', '', ''])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const sorted = [...players].sort((a, b) => a.name.localeCompare(b.name, 'es'))
  const chosen = sel.filter(Boolean)
  const ready = chosen.length === 4 && new Set(chosen).size === 4

  function show(newRound: boolean) {
    setRound(newRound || rounds.length === 0 ? NEW_ROUND : String(rounds[0]))
    setError(null)
    setOpen(true)
  }

  async function submit() {
    setBusy(true)
    setError(null)
    try {
      await api.competitions.addMatch(competitionId, groupId, {
        round: round === NEW_ROUND ? last + 1 : Number(round),
        home1Id: Number(sel[0]), home2Id: Number(sel[1]), away1Id: Number(sel[2]), away2Id: Number(sel[3]),
      })
      notify('Partido añadido')
      setSel(['', '', '', ''])
      setOpen(false)
      await onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error')
    } finally {
      setBusy(false)
    }
  }

  if (!open) {
    return (
      <div className="row">
        <button type="button" className="secondary small" onClick={() => show(false)}>Añadir partido</button>
        <button type="button" className="secondary small" onClick={() => show(true)}>Nueva jornada</button>
      </div>
    )
  }

  const labels = ['Pareja A · jugador 1', 'Pareja A · jugador 2', 'Pareja B · jugador 1', 'Pareja B · jugador 2']
  return (
    <div className="add-match">
      <h3>{round === NEW_ROUND ? `Nueva jornada (${last + 1})` : 'Añadir partido'}</h3>
      {error && <p className="error" role="alert">{error}</p>}
      <label>Jornada{' '}
        <select value={round} onChange={e => setRound(e.target.value)} disabled={busy}>
          {rounds.map(r => <option key={r} value={r}>Jornada {r}</option>)}
          <option value={NEW_ROUND}>Nueva jornada ({last + 1})</option>
        </select>
      </label>
      <div className="cols">
        {labels.map((label, i) => (
          <label key={label}>{label}
            <select value={sel[i]} disabled={busy} onChange={e => setSel(prev => prev.map((v, k) => (k === i ? e.target.value : v)))}>
              <option value="">Elige…</option>
              {(!sel.includes(RESERVE) || sel[i] === RESERVE) && <option value={RESERVE}>Jugador reserva</option>}
              {sorted.filter(p => !sel.includes(String(p.id)) || sel[i] === String(p.id))
                .map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
        ))}
      </div>
      <p className="muted">Si falta un jugador, elige «Jugador reserva»: juega el partido pero no puntúa ni cuenta en la clasificación.</p>
      <div className="row">
        <button type="button" disabled={!ready || busy} onClick={() => void submit()}>Añadir</button>
        <button type="button" className="secondary" disabled={busy} onClick={() => setOpen(false)}>Cancelar</button>
      </div>
    </div>
  )
}
