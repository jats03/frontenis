import type { MatchDto } from '../api/types'
import { roundName } from '../labels'
import MatchCard from './MatchCard'

interface Props {
  matches: MatchDto[]
  canEdit: boolean
  onChanged: (m: MatchDto) => void
}

export default function Bracket({ matches, canEdit, onChanged }: Props) {
  const playoff = matches.filter(m => m.phase === 'Playoff')
  if (playoff.length === 0) return <p className="muted">Todavía no se ha generado la fase final.</p>

  const rounds = [...new Set(playoff.map(m => m.round))].sort((a, b) => a - b)
  const total = rounds[rounds.length - 1]

  return (
    <div className="bracket">
      {rounds.map(r => (
        <div className="bracket-round" key={r}>
          <h4>{roundName(r, total)}</h4>
          {playoff.filter(m => m.round === r).sort((a, b) => a.order - b.order).map(m => (
            <MatchCard key={m.id} match={m} canEdit={canEdit} onChanged={onChanged} />
          ))}
        </div>
      ))}
    </div>
  )
}
