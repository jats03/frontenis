import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { CalendarIcon, HomeIcon, MoreIcon, TrophyIcon, UsersIcon } from './Icons'

const items: { to: string; label: string; short?: string; icon: ReactNode; end?: boolean }[] = [
  { to: '/', label: 'Inicio', icon: <HomeIcon />, end: true },
  { to: '/competiciones', label: 'Competiciones', icon: <TrophyIcon /> },
  { to: '/entrenamientos', label: 'Entrenamientos', short: 'Entrenos', icon: <CalendarIcon /> },
  { to: '/jugadores', label: 'Jugadores', icon: <UsersIcon /> },
  { to: '/mas', label: 'Más', icon: <MoreIcon /> },
]

/** Navegación única: barra inferior en móvil/tablet y barra lateral en ordenador. Sin cabecera con texto. */
export default function AppNav() {
  const { isAdmin, logout, username } = useAuth()
  return (
    <nav aria-label="Principal" className="main-nav">
      <img src="/logo.png" alt="Club Frontenis Ciudad de Jaén" className="nav-logo" />
      {items.map(i => (
        <NavLink key={i.to} to={i.to} end={i.end}>
          {i.icon}
          <span className="lbl-long">{i.label}</span>
          <span className="lbl-short">{i.short ?? i.label}</span>
        </NavLink>
      ))}
      {isAdmin && <button className="link nav-logout" onClick={logout}>Salir ({username})</button>}
    </nav>
  )
}
