import { notify } from '../alerts'
import { api } from '../api/client'

interface Props {
  competitionId: number
  groupId: number
  round: number
  /** Otras jornadas del grupo con las que se puede intercambiar (tengan o no partidos jugados). */
  others: number[]
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

/** Intercambia una jornada entera con otra del mismo grupo. */
export default function SwapRoundSelect({ competitionId, groupId, round, others, onChanged, onError }: Props) {
  async function swap(value: string) {
    if (!value) return
    try {
      await api.competitions.swapRounds(competitionId, groupId, round, Number(value))
      notify(`Jornadas ${round} y ${value} intercambiadas`)
      await onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : 'Error')
    }
  }

  return (
    <label className="muted round-swap">
      Intercambiar con{' '}
      <select value="" onChange={e => void swap(e.target.value)} aria-label={`Intercambiar la jornada ${round} con otra`}>
        <option value="">jornada…</option>
        {others.map(r => <option key={r} value={r}>Jornada {r}</option>)}
      </select>
    </label>
  )
}
