import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { greetingFor } from '../avatar'
import { useDevice } from '../useDevice'
import PlayerAvatar from './PlayerAvatar'
import { CalendarIcon, HomeIcon, MoreIcon, TrophyIcon, UsersIcon } from './Icons'

const items: { to: string; label: string; short?: string; icon: ReactNode; end?: boolean }[] = [
  { to: '/', label: 'Inicio', icon: <HomeIcon />, end: true },
  { to: '/competiciones', label: 'Competiciones', icon: <TrophyIcon /> },
  { to: '/entrenamientos', label: 'Entrenamientos', short: 'Entrenos', icon: <CalendarIcon /> },
  { to: '/jugadores', label: 'Jugadores', icon: <UsersIcon /> },
  { to: '/mas', label: 'Más', icon: <MoreIcon /> },
]

/** Navegación única: barra inferior en móvil/tablet y barra lateral en ordenador. Cabecera con el logo y, si el dispositivo está vinculado, un saludo. */
export default function AppNav() {
  const { isAdmin, logout, username } = useAuth()
  const { me } = useDevice()
  const firstName = me?.owner.name.trim().split(/\s+/)[0]
  return (
    <>
    <header className="topbar">
      <NavLink to="/" className="brand" aria-label="Club Frontenis Ciudad de Jaén, inicio">
        <img src="/logo.png" alt="" className="logo" />
      </NavLink>
      {firstName && (
        <span className="greeting" title={`Eres ${me!.owner.name}`}>
          <span className="greeting-text">
            <span className="greeting-hello">{greetingFor(new Date())}</span>
            <strong className="greeting-name">{firstName}</strong>
          </span>
          <PlayerAvatar playerId={me!.owner.id} size={40} className="greeting-avatar" />
        </span>
      )}
    </header>
    <nav aria-label="Principal" className="main-nav">
      {items.map(i => (
        <NavLink key={i.to} to={i.to} end={i.end}>
          {i.icon}
          <span className="lbl-long">{i.label}</span>
          <span className="lbl-short">{i.short ?? i.label}</span>
        </NavLink>
      ))}
      {isAdmin && <button className="link nav-logout" onClick={logout}>Salir ({username})</button>}
    </nav>
    </>
  )
}
