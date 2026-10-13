import { Link } from 'react-router-dom'

export type PlayersView = 'lista' | 'sorteos' | 'historico'

const tabs: { key: PlayersView; label: string; to: string }[] = [
  { key: 'lista', label: 'Lista', to: '/jugadores' },
  { key: 'sorteos', label: 'Sorteos', to: '/jugadores?vista=sorteos' },
  { key: 'historico', label: 'Histórico', to: '/jugadores/historico' },
]

/** Pestañas de la sección Jugadores (cada una es un enlace, así funcionan el botón atrás y compartir la dirección). */
export default function PlayersTabs({ current }: { current: PlayersView }) {
  return (
    <nav aria-label="Secciones de jugadores" className="tabs link-tabs">
      {tabs.map(t => (
        <Link key={t.key} to={t.to} className={current === t.key ? 'active' : ''}
          aria-current={current === t.key ? 'page' : undefined}>{t.label}</Link>
      ))}
    </nav>
  )
}
