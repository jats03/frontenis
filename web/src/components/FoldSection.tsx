import { useId, type ReactNode } from 'react'

interface Props {
  title: string
  open: boolean
  onToggle: () => void
  /** Resumen o controles que se ven en la cabecera aunque la sección esté plegada (puede llevar botones). */
  summary?: ReactNode
  className?: string
  children: ReactNode
}

/** Sección plegable: la cabecera siempre se ve; el cuerpo solo cuando está desplegada. */
export default function FoldSection({ title, open, onToggle, summary, className = '', children }: Props) {
  const bodyId = useId()
  return (
    <section className={`card fold ${open ? 'open' : 'closed'} ${className}`}>
      <div className="fold-head">
        <button type="button" className="fold-toggle" aria-expanded={open} aria-controls={bodyId} onClick={onToggle}>
          <span className="fold-chevron" aria-hidden="true">{open ? '▾' : '▸'}</span>
          <h2>{title}</h2>
        </button>
        {summary && <div className="fold-summary">{summary}</div>}
      </div>
      {open && <div id={bodyId} className="fold-body">{children}</div>}
    </section>
  )
}
