import type { ReactNode } from 'react'

/** Estado vacío: explica qué falta y ofrece el siguiente paso. */
export default function EmptyState({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {children && <p className="muted">{children}</p>}
      {action}
    </div>
  )
}
