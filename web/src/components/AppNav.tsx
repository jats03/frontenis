import { useEffect, useState, type ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { greetingFor } from '../avatar'
import { startPresence } from '../timerStore'
import { useDevice } from '../useDevice'
import PlayerAvatar from './PlayerAvatar'
import PlayerCard, { askMotionPermission } from './PlayerCard'
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
  const identified = isAdmin || me !== null // sin identificar solo se ve «Inicio»
  const [showCard, setShowCard] = useState(false)
  useEffect(() => startPresence(), []) // marca la app como «vista» para el cronómetro (regla de los 15 minutos)
  const fullName = me?.owner.name.trim().replace(/\s+/g, ' ')
  return (
    <>
    <header className="topbar">
      <NavLink to="/" className="brand" aria-label="Club Frontenis Ciudad de Jaén, inicio">
        <img src="/logo.png" alt="" className="logo" />
      </NavLink>
      {fullName && (
        <span className="greeting" title={`Eres ${me!.owner.name}`}>
          <span className="greeting-text">
            <span className="greeting-hello">{greetingFor(new Date())}<span className="greeting-comma">,</span></span>
            <strong className="greeting-name">{fullName}</strong>
          </span>
          <button type="button" className="greeting-avatar-btn" aria-label="Ver mi carta de jugador" onClick={() => { askMotionPermission(); setShowCard(true) }}>
            <PlayerAvatar playerId={me!.owner.id} size={40} className="greeting-avatar" />
          </button>
        </span>
      )}
    </header>
    {showCard && me && <PlayerCard playerId={me.owner.id} onClose={() => setShowCard(false)} />}
    <nav aria-label="Principal" className="main-nav">
      {items.filter(i => identified || i.to === '/').map(i => (
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
