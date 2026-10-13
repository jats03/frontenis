import { useEffect, useRef, useState } from 'react'

export interface MenuItem {
  label: string
  onSelect: () => void
  /** Acción destructiva: se muestra en rojo y al final del menú. */
  danger?: boolean
  /** Si es true, la opción no aparece. */
  hidden?: boolean
}

/** Menú «⋯» accesible (teclado: flechas, Esc, Tab) para agrupar acciones secundarias de una pantalla. */
export default function ActionMenu({ items, label = 'Más acciones' }: { items: MenuItem[]; label?: string }) {
  const visible = items.filter(i => !i.hidden)
  const ordered = [...visible.filter(i => !i.danger), ...visible.filter(i => i.danger)]
  const [open, setOpen] = useState(false)
  const root = useRef<HTMLDivElement>(null)
  const trigger = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent | TouchEvent) => {
      if (root.current && !root.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('touchstart', onDown)
    root.current?.querySelector<HTMLElement>('[role=menuitem]')?.focus()
    return () => { document.removeEventListener('mousedown', onDown); document.removeEventListener('touchstart', onDown) }
  }, [open])

  if (ordered.length === 0) return null

  function onKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Escape') { setOpen(false); trigger.current?.focus(); return }
    if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return
    e.preventDefault()
    const els = [...(root.current?.querySelectorAll<HTMLElement>('[role=menuitem]') ?? [])]
    const i = els.indexOf(document.activeElement as HTMLElement)
    els[(i + (e.key === 'ArrowDown' ? 1 : -1) + els.length) % els.length]?.focus()
  }

  return (
    <div className="action-menu" ref={root} onKeyDown={onKeyDown}>
      <button ref={trigger} type="button" className="secondary menu-trigger" aria-haspopup="menu" aria-expanded={open}
        aria-label={label} onClick={() => setOpen(o => !o)}>
        <span aria-hidden="true">⋯</span>
      </button>
      {open && (
        <ul className="menu-list" role="menu" aria-label={label}>
          {ordered.map(i => (
            <li key={i.label} role="none">
              <button type="button" role="menuitem" className={i.danger ? 'danger' : ''}
                onClick={() => { setOpen(false); trigger.current?.focus(); i.onSelect() }}>
                {i.label}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
