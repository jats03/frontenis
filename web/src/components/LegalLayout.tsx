import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useDocumentTitle } from '../hooks'
import { isPlaceholder, legal } from '../legal/config'

/** Pinta un dato del titular; si aún es un marcador lo resalta para que no pase desapercibido. */
export function Field({ value }: { value: string }) {
  return isPlaceholder(value) ? <mark className="todo">{value}</mark> : <strong>{value}</strong>
}

export default function LegalLayout({ title, children }: { title: string; children: ReactNode }) {
  useDocumentTitle(title)
  return (
    <article className="card legal">
      <h1>{title}</h1>
      <p className="muted">Última actualización: {legal.updatedAt}</p>
      {children}
      <p className="muted legal-nav">
        <Link to="/privacidad">Política de privacidad</Link> · <Link to="/terminos">Términos y condiciones</Link> ·{' '}
        <Link to="/cookies">Política de cookies</Link>
      </p>
    </article>
  )
}
