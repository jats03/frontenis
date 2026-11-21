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

/**
 * Sección plegable: la cabecera siempre se ve; el cuerpo solo cuando está desplegada. Se pliega y despliega pulsando en
 * cualquier punto de la cabecera (título, resumen o el hueco entre ambos), salvo en los botones que lleve dentro del resumen
 * (por ejemplo, el de iniciar el cronómetro), que hacen lo suyo.
 */
export default function FoldSection({ title, open, onToggle, summary, className = '', children }: Props) {
  const bodyId = useId()
  return (
    <section className={`card fold ${open ? 'open' : 'closed'} ${className}`}>
      <div className="fold-head" onClick={e => {
        const control = (e.target as HTMLElement).closest('button, a, input, select, textarea, [role="button"]')
        if (control && !control.classList.contains('fold-toggle')) return // un control del resumen: que haga lo suyo
        onToggle()
      }}>
        <button type="button" className="fold-toggle" aria-expanded={open} aria-controls={bodyId}>
          <span className="fold-chevron" aria-hidden="true">{open ? '▾' : '▸'}</span>
          <h2>{title}</h2>
        </button>
        {summary && <div className="fold-summary">{summary}</div>}
      </div>
      {open && <div id={bodyId} className="fold-body">{children}</div>}
    </section>
  )
}
