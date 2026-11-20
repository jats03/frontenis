import { useId, useMemo, useState } from 'react'
import type { Player } from '../api/types'

interface Props {
  /** Id del campo de texto (para llevar el foco a él). */
  id: string
  label: string
  players: Player[]
  /** Jugador elegido (su id) o vacío. */
  value: string
  /** Jugadores que no se ofrecen (los ya elegidos en otros campos). */
  excludeIds?: Set<string>
  onChange: (value: string) => void
  /** Se llama tras elegir a alguien de la lista (para pasar al siguiente campo). */
  onChosen?: () => void
}

const norm = (s: string) => s.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '')

/** Nombre con la parte escrita subrayada. */
function Highlighted({ name, query }: { name: string; query: string }) {
  const q = norm(query.trim())
  const at = q ? norm(name).indexOf(q) : -1
  if (at < 0) return <>{name}</>
  return <>{name.slice(0, at)}<mark>{name.slice(at, at + q.length)}</mark>{name.slice(at + q.length)}</>
}

/**
 * Elegir un jugador escribiendo: salen las coincidencias (sin distinguir mayúsculas ni tildes, primero las que empiezan por
 * lo escrito) y se elige tocando o con flechas y Enter.
 */
export default function PlayerSearch({ id, label, players, value, excludeIds, onChange, onChosen }: Props) {
  const listId = useId()
  const selected = players.find(p => String(p.id) === value)
  const [typing, setTyping] = useState<string | null>(null) // lo que se está escribiendo; null = se muestra el elegido
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)

  const query = typing ?? ''
  const matches = useMemo(() => {
    const q = norm(query.trim())
    return players
      .filter(p => !excludeIds?.has(String(p.id)) && (!q || norm(p.name).includes(q)))
      .sort((a, b) => {
        const pa = q && norm(a.name).startsWith(q) ? 0 : 1, pb = q && norm(b.name).startsWith(q) ? 0 : 1
        return pa - pb || a.name.localeCompare(b.name, 'es')
      })
  }, [players, excludeIds, query])
  const current = Math.min(active, Math.max(0, matches.length - 1))

  function choose(p: Player) {
    onChange(String(p.id))
    setTyping(null); setOpen(false); setActive(0)
    onChosen?.()
  }

  function clear() {
    onChange(''); setTyping(''); setOpen(true); setActive(0)
    document.getElementById(id)?.focus()
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'ArrowDown') { e.preventDefault(); setOpen(true); setActive(Math.min(current + 1, matches.length - 1)) }
    else if (e.key === 'ArrowUp') { e.preventDefault(); setActive(Math.max(current - 1, 0)) }
    else if (e.key === 'Enter' && open && matches[current]) { e.preventDefault(); choose(matches[current]) }
    else if (e.key === 'Escape') { setOpen(false); setTyping(null) }
  }

  return (
    <div className={`ps${selected ? ' ps-ok' : ''}`}
      onBlur={e => { if (!e.currentTarget.contains(e.relatedTarget)) { setOpen(false); setTyping(null) } }}>
      <label htmlFor={id}>{label}</label>
      <div className="ps-box">
        <input id={id} role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list" autoComplete="off"
          aria-activedescendant={open && matches[current] ? `${listId}-${matches[current].id}` : undefined}
          placeholder="Escribe un nombre" value={typing ?? selected?.name ?? ''}
          onFocus={e => { setOpen(true); e.currentTarget.select() }}
          onChange={e => { setTyping(e.target.value); if (value) onChange(''); setOpen(true); setActive(0) }}
          onKeyDown={onKeyDown} />
        {(selected || typing) && <button type="button" className="ps-x" aria-label={`Quitar ${label}`} onMouseDown={e => e.preventDefault()} onClick={clear}>×</button>}
      </div>
      {open && (
        <ul id={listId} role="listbox" className="ps-list">
          {matches.length === 0 && <li className="ps-none" role="presentation">Nadie con ese nombre.</li>}
          {matches.map((p, k) => (
            <li key={p.id} id={`${listId}-${p.id}`} role="option" aria-selected={k === current} className={k === current ? 'on' : ''}
              onMouseDown={e => e.preventDefault()} onClick={() => choose(p)}>
              <Highlighted name={p.name} query={query} />
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
