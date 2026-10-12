import { useMemo, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionType } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export default function CompetitionNewPage() {
  const { isAdmin } = useAuth()
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [type, setType] = useState<CompetitionType>('League')
  const [pairCount, setPairCount] = useState(8)
  const [groupCount, setGroupCount] = useState(2)
  const [pairsPerGroup, setPairsPerGroup] = useState(4)
  const [pointsPerWin, setPointsPerWin] = useState(3)
  const [pointsPerLoss, setPointsPerLoss] = useState(0)
  const [doubleRound, setDoubleRound] = useState(false)
  const [qualifiers, setQualifiers] = useState(2)
  const [rounds, setRounds] = useState(0) // 0 = automático (la mitad de jugadores por grupo)
  const [error, setError] = useState<string | null>(null)

  // Mantiene coherentes los tres parámetros: parejas = grupos × parejas por grupo.
  function changePairCount(v: number) { setPairCount(v); if (groupCount > 0 && v % groupCount === 0) setPairsPerGroup(v / groupCount) }
  function changeGroups(v: number) { setGroupCount(v); if (v > 0 && pairCount % v === 0) setPairsPerGroup(pairCount / v) }
  function changePerGroup(v: number) { setPairsPerGroup(v); if (v > 0) setPairCount(v * groupCount) }

  const hint = useMemo(() => {
    if (groupCount * pairsPerGroup !== pairCount)
      return `Con ${groupCount} grupo(s) los grupos tendrán tamaño desigual (se reparten lo más equilibrado posible).`
    return `${groupCount} grupo(s) × ${pairsPerGroup} parejas = ${pairCount} parejas.`
  }, [groupCount, pairsPerGroup, pairCount])

  if (!isAdmin) return <p className="error">Necesitas iniciar sesión como administrador.</p>

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const rotating = type === 'RotatingLeague'
      const created = await api.competitions.create({
        name, type, pairCount: rotating ? groupCount * pairsPerGroup : pairCount, groupCount,
        rounds: rotating ? rounds : 0,
        pairsPerGroup: rotating || groupCount * pairsPerGroup === pairCount ? pairsPerGroup : 0,
        pointsPerWin, pointsPerLoss, doubleRound: rotating ? false : doubleRound,
        qualifiersPerGroup: type === 'Tournament' ? qualifiers : 0,
      })
      notify(`«${created.name}» creada`)
      navigate(`/competiciones/${created.id}`)
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
  }

  const num = (v: number, set: (n: number) => void, min = 0) => (
    <input type="number" min={min} value={Number.isNaN(v) ? '' : v} onChange={e => set(e.target.valueAsNumber)} />
  )

  return (
    <form className="card form" onSubmit={submit}>
      <h2>Nueva competición</h2>
      <label>Nombre<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Tipo
        <select value={type} onChange={e => setType(e.target.value as CompetitionType)}>
          <option value="League">Liga (solo clasificación)</option>
          <option value="Tournament">Torneo (grupos + fase final)</option>
          <option value="RotatingLeague">Liga rotatoria (parejas distintas cada jornada)</option>
        </select>
      </label>
      {type === 'RotatingLeague' ? (
        <>
          <div className="cols">
            <label>Nº de grupos{num(groupCount, setGroupCount, 1)}</label>
            <label>Jugadores por grupo (mínimo 4)
              <input type="number" min={4} value={Number.isNaN(pairsPerGroup) ? '' : pairsPerGroup}
                onChange={e => setPairsPerGroup(e.target.valueAsNumber)} />
            </label>
            <label>Jornadas (0 = automático)
              <input type="number" min={0} value={Number.isNaN(rounds) ? '' : rounds}
                onChange={e => setRounds(e.target.valueAsNumber)} />
            </label>
          </div>
          <p className="muted">
            {groupCount} grupo(s) × {pairsPerGroup} jugadores = {groupCount * pairsPerGroup} jugadores como mínimo; si hay más, los sobrantes van al último grupo (si un grupo no es múltiplo de 4, en cada jornada descansan por turnos los que sobran).
            En cada jornada las parejas se sortean de nuevo (un delantero y un zaguero) y la clasificación es individual.
            Al terminar, el último de cada grupo baja y el primero sube.
          </p>
        </>
      ) : (
        <>
          <div className="cols">
            <label>Nº de parejas{num(pairCount, changePairCount, 2)}</label>
            <label>Nº de grupos{num(groupCount, changeGroups, 1)}</label>
            <label>Parejas por grupo{num(pairsPerGroup, changePerGroup, 2)}</label>
          </div>
          <p className="muted">{hint}</p>
        </>
      )}
      <div className="cols">
        <label>Puntos por victoria{num(pointsPerWin, setPointsPerWin, 1)}</label>
        <label>Puntos por derrota{num(pointsPerLoss, setPointsPerLoss, 0)}</label>
        {type === 'Tournament' && <label>Clasificados por grupo{num(qualifiers, setQualifiers, 1)}</label>}
      </div>
      {type !== 'RotatingLeague' && <label className="check">
        <input type="checkbox" checked={doubleRound} onChange={e => setDoubleRound(e.target.checked)} />
        Ida y vuelta
      </label>}
      {error && <p className="error">{error}</p>}
      <button type="submit">Crear</button>
    </form>
  )
}
