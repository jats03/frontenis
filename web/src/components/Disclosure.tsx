import { useState, type ReactNode } from 'react'

/** Bloque plegable: solo el estado inicial depende de `defaultOpen`; después manda quien lo abre o cierra. */
export default function Disclosure({ summary, defaultOpen, className, children }:
  { summary: ReactNode; defaultOpen: boolean; className?: string; children: ReactNode }) {
  const [initial] = useState(defaultOpen)
  return (
    <details className={className} open={initial}>
      <summary>{summary}</summary>
      {children}
    </details>
  )
}
