import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { PairDto, Player } from '../api/types'

interface Props {
  competitionId: number
  groupId: number
  /** Jornadas que ya tiene el grupo. */
  rounds: number[]
  /** Parejas de la liga; de ellas salen los jugadores del grupo. */
  pairs: PairDto[]
  onChanged: () => Promise<void> | void
}

const NEW_ROUND = 'new'

/** Formulario para añadir un partido a cualquier jornada de un grupo (o a una nueva), sin más condiciones. */
export default function AddMatchForm({ competitionId, groupId, rounds, pairs, onChanged }: Props) {
  const last = rounds.length ? Math.max(...rounds) : 0
  const [open, setOpen] = useState(false)
  const [round, setRound] = useState(String(rounds[0] ?? 1))
  const [sel, setSel] = useState<string[]>(['', '', '', ''])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const players: Player[] = [...new Map(pairs.filter(p => p.groupId === groupId)
    .flatMap(p => [p.player1, p.player2]).map(p => [p.id, p])).values()]
    .sort((a, b) => a.name.localeCompare(b.name, 'es'))
  const chosen = sel.filter(Boolean)
  const ready = chosen.length === 4 && new Set(chosen).size === 4

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

  if (!open) return <button type="button" className="secondary small" onClick={() => setOpen(true)}>Añadir partido</button>

  const labels = ['Pareja A · jugador 1', 'Pareja A · jugador 2', 'Pareja B · jugador 1', 'Pareja B · jugador 2']
  return (
    <div className="add-match">
      <h3>Añadir partido</h3>
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
              {players.filter(p => !sel.includes(String(p.id)) || sel[i] === String(p.id))
                .map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
        ))}
      </div>
      <div className="row">
        <button type="button" disabled={!ready || busy} onClick={() => void submit()}>Añadir</button>
        <button type="button" className="secondary" disabled={busy} onClick={() => setOpen(false)}>Cancelar</button>
      </div>
    </div>
  )
}
