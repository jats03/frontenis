import { useState } from 'react'
import { api } from '../api/client'
import type { MatchDto } from '../api/types'

interface Props {
  match: MatchDto
  canEdit: boolean
  onChanged: (m: MatchDto) => void
}

export default function MatchCard({ match, canEdit, onChanged }: Props) {
  const [home, setHome] = useState(match.homeScore?.toString() ?? '')
  const [away, setAway] = useState(match.awayScore?.toString() ?? '')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const ready = match.homeEntryId !== null && match.awayEntryId !== null

  async function run(action: () => Promise<MatchDto>) {
    setBusy(true)
    setError(null)
    try { onChanged(await action()) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  function save() {
    const h = Number(home), a = Number(away)
    if (home === '' || away === '' || !Number.isInteger(h) || !Number.isInteger(a)) {
      setError('Introduce los dos marcadores.')
      return
    }
    void run(() => api.matches.setResult(match.id, h, a))
  }

  const side = (name: string | null, entryId: number | null) => (
    <span className={`team ${match.isPlayed && match.winnerEntryId === entryId ? 'winner' : ''}`}>
      {name ?? 'Por definir'}
    </span>
  )

  return (
    <article className={`match ${match.isPlayed ? 'played' : ''}`}>
      <div className="teams">
        {side(match.homeName, match.homeEntryId)}
        {side(match.awayName, match.awayEntryId)}
      </div>
      {canEdit && ready ? (
        <div className="score-edit">
          <input inputMode="numeric" aria-label="Puntos local" value={home} onChange={e => setHome(e.target.value)} />
          <span>-</span>
          <input inputMode="numeric" aria-label="Puntos visitante" value={away} onChange={e => setAway(e.target.value)} />
          <button disabled={busy} onClick={save}>Guardar</button>
          {match.isPlayed && (
            <button className="secondary" disabled={busy}
              onClick={() => run(() => api.matches.clearResult(match.id)).then(() => { setHome(''); setAway('') })}>
              Borrar
            </button>
          )}
        </div>
      ) : (
        <div className="score">{match.isPlayed ? `${match.homeScore} - ${match.awayScore}` : '—'}</div>
      )}
      {error && <p className="error">{error}</p>}
    </article>
  )
}
