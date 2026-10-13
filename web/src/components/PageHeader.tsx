import type { ReactNode } from 'react'

interface Props {
  title: string
  /** Texto de apoyo bajo el título (estado, fecha, reglas…). */
  subtitle?: ReactNode
  /** Etiqueta de estado junto al título. */
  badge?: ReactNode
  /** Acciones: la principal (botón de color) y, si hay más, un ActionMenu. */
  actions?: ReactNode
}

/** Cabecera común de todas las pantallas: título a la izquierda, acción principal a la derecha. */
export default function PageHeader({ title, subtitle, badge, actions }: Props) {
  return (
    <header className="page-header">
      <div className="page-title">
        <h1>{title} {badge}</h1>
        {subtitle && <p className="muted">{subtitle}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </header>
  )
}
