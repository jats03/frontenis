import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { CalendarIcon, HomeIcon, MoreIcon, TrophyIcon, UsersIcon } from './Icons'

const items: { to: string; label: string; icon: ReactNode; end?: boolean }[] = [
  { to: '/', label: 'Inicio', icon: <HomeIcon />, end: true },
  { to: '/competiciones', label: 'Competiciones', icon: <TrophyIcon /> },
  { to: '/entrenamientos', label: 'Entrenamientos', icon: <CalendarIcon /> },
  { to: '/jugadores', label: 'Jugadores', icon: <UsersIcon /> },
  { to: '/mas', label: 'Más', icon: <MoreIcon /> },
]

/** Barra superior (marca + navegación en ordenador) y barra inferior fija (móvil). */
export default function AppNav() {
  const { isAdmin, logout, username } = useAuth()
  return (
    <>
      <header className="topbar">
        <NavLink to="/" className="brand" aria-label="Frontenis Club Ciudad de Jaén, inicio">
          <img src="/logo.png" alt="" className="logo" />
          <span className="brand-name">Frontenis</span>
        </NavLink>
        <nav aria-label="Principal" className="nav-desktop">
          {items.map(i => <NavLink key={i.to} to={i.to} end={i.end}>{i.label}</NavLink>)}
        </nav>
        {isAdmin && <button className="link nav-desktop" onClick={logout}>Salir ({username})</button>}
      </header>
      <nav aria-label="Principal (móvil)" className="bottom-nav">
        {items.map(i => (
          <NavLink key={i.to} to={i.to} end={i.end}>
            {i.icon}<span>{i.label === 'Entrenamientos' ? 'Entrenos' : i.label}</span>
          </NavLink>
        ))}
      </nav>
    </>
  )
}
