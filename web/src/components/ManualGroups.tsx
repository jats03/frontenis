import { useMemo, useState } from 'react'

export interface ManualItem { id: number; label: string }

interface Props {
  items: ManualItem[]
  groupCount: number
  /** Mínimo de elementos por grupo. */
  minPerGroup: number
  noun: string // "pareja(s)" | "jugador(es)"
  busy?: boolean
  submitLabel: string
  onSubmit: (groups: number[][]) => void
}

const groupName = (i: number) => (i < 26 ? `Grupo ${String.fromCharCode(65 + i)}` : `Grupo ${i + 1}`)

/** Asignación manual de cada elemento a un grupo (A, B, C…). */
export default function ManualGroups({ items, groupCount, minPerGroup, noun, busy, submitLabel, onSubmit }: Props) {
  const [assign, setAssign] = useState<Record<number, number>>({})

  const counts = useMemo(() => {
    const c = Array<number>(groupCount).fill(0)
    items.forEach(it => { const g = assign[it.id]; if (g !== undefined && g < groupCount) c[g]++ })
    return c
  }, [assign, items, groupCount])

  const unassigned = items.filter(it => assign[it.id] === undefined || assign[it.id] >= groupCount).length
  const small = counts.findIndex(c => c < minPerGroup)
  const problem = unassigned > 0
    ? `Faltan ${unassigned} ${noun} por asignar.`
    : small >= 0 ? `${groupName(small)} necesita al menos ${minPerGroup} ${noun}.` : null

  function submit() {
    const groups = Array.from({ length: groupCount }, (_, g) => items.filter(it => assign[it.id] === g).map(it => it.id))
    onSubmit(groups)
  }

  return (
    <div className="manual-groups">
      <p className="muted">Elige el grupo de cada uno. El grupo A es el de más nivel.</p>
      <div className="manual-list">
        {items.map(it => (
          <label key={it.id} className="manual-row">
            <span>{it.label}</span>
            <select aria-label={`Grupo de ${it.label}`} value={assign[it.id] ?? ''}
              onChange={e => setAssign(prev => {
                const next = { ...prev }
                if (e.target.value === '') delete next[it.id]; else next[it.id] = Number(e.target.value)
                return next
              })}>
              <option value="">— Grupo —</option>
              {Array.from({ length: groupCount }, (_, g) => <option key={g} value={g}>{groupName(g)}</option>)}
            </select>
          </label>
        ))}
      </div>
      <p className="muted">{counts.map((c, g) => `${groupName(g)}: ${c}`).join(' · ')}</p>
      {problem && <p className="error">{problem}</p>}
      <button disabled={busy || problem !== null} onClick={submit}>{submitLabel}</button>
    </div>
  )
}
