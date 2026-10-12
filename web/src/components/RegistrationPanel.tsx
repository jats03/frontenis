import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { CompetitionDetail, Player } from '../api/types'
import PlayerPicker from './PlayerPicker'

interface Props {
  detail: CompetitionDetail
  players: Player[]
  onChanged: () => Promise<void>
}

/** Inscripción de parejas (aleatoria o manual) y sorteo, mientras la competición está en borrador. */
export default function RegistrationPanel({ detail, players, onChanged }: Props) {
  const { summary, pairs } = detail
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [manual1, setManual1] = useState('')
  const [manual2, setManual2] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const taken = useMemo(() => new Set(pairs.flatMap(p => [p.player1.id, p.player2.id])), [pairs])
  const available = players.filter(p => !taken.has(p.id))
  const missing = summary.pairCount - pairs.length

  async function run(action: () => Promise<unknown>) {
    setBusy(true); setError(null)
    try { await action(); await onChanged() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  return (
    <section className="card">
      <h3>Inscripción ({pairs.length}/{summary.pairCount} parejas)</h3>
      {error && <p className="error">{error}</p>}

      {missing > 0 && (
        <>
          <p className="muted">
            Selecciona jugadores y se emparejarán al azar (zaguero + delantero; los sobrantes de un mismo tipo se emparejan entre sí).
            Faltan {missing} pareja(s) = {missing * 2} jugadores.
          </p>
          <PlayerPicker players={available} selected={selected} onChange={setSelected} />
          <div className="row">
            <button disabled={busy || selected.size < 2}
              onClick={() => void run(async () => { await api.competitions.registerRandom(summary.id, [...selected]); setSelected(new Set()) })}>
              Emparejar al azar e inscribir ({selected.size})
            </button>
          </div>

          <h4>Pareja manual</h4>
          <div className="row">
            <select value={manual1} onChange={e => setManual1(e.target.value)}>
              <option value="">Jugador 1</option>
              {available.map(p => <option key={p.id} value={p.id}>{p.name} ({p.position})</option>)}
            </select>
            <select value={manual2} onChange={e => setManual2(e.target.value)}>
              <option value="">Jugador 2</option>
              {available.filter(p => String(p.id) !== manual1).map(p => <option key={p.id} value={p.id}>{p.name} ({p.position})</option>)}
            </select>
            <button disabled={busy || !manual1 || !manual2}
              onClick={() => void run(async () => { await api.competitions.registerManual(summary.id, Number(manual1), Number(manual2)); setManual1(''); setManual2('') })}>
              Añadir
            </button>
          </div>
        </>
      )}

      {missing === 0 && (
        <div className="row">
          <button disabled={busy} onClick={() => void run(() => api.competitions.draw(summary.id))}>
            🎲 Sortear grupos y generar calendario
          </button>
        </div>
      )}
    </section>
  )
}
