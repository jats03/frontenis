import { useEffect, useRef } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import ApiStatus from './components/ApiStatus'
import LoadingSpinner from './components/LoadingSpinner'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import LoginPage from './pages/LoginPage'
import PlayersPage from './pages/PlayersPage'
import WhatsappPage from './pages/WhatsappPage'
import TimerPage from './pages/TimerPage'

export default function App() {
  const { isAdmin, username, logout } = useAuth()
  const { pathname } = useLocation()
  const main = useRef<HTMLElement>(null)
  const first = useRef(true)
  // Al cambiar de página, el foco pasa al contenido (accesibilidad para teclado y lectores de pantalla).
  useEffect(() => {
    if (first.current) { first.current = false; return }
    main.current?.focus()
  }, [pathname])
  return (
    <div className="app">
      <a className="skip-link" href="#contenido" onClick={e => { e.preventDefault(); main.current?.focus() }}>Saltar al contenido</a>
      <header className="topbar">
        <span className="brand"><img src="/logo.png" alt="Frontenis Club Ciudad de Jaén" className="logo" /></span>
        <nav aria-label="Principal">
          <NavLink to="/" end>Competiciones</NavLink>
          <NavLink to="/jugadores">Jugadores</NavLink>
          <NavLink to="/cronometro">Cronómetro</NavLink>
          <NavLink to="/plantillas">Plantillas</NavLink>
        </nav>
        {/* El acceso de administrador no se anuncia: se entra escribiendo la dirección /acceso */}
        {isAdmin && <button className="link" onClick={logout}>Salir ({username})</button>}
      </header>
      <ApiStatus />
      <LoadingSpinner />
      <main id="contenido" className="content" tabIndex={-1} ref={main}>
        <Routes>
          <Route path="/" element={<CompetitionsPage />} />
          <Route path="/competiciones/nueva" element={<CompetitionNewPage />} />
          <Route path="/competiciones/:id" element={<CompetitionDetailPage />} />
          <Route path="/jugadores" element={<PlayersPage />} />
          <Route path="/cronometro" element={<TimerPage />} />
          <Route path="/plantillas" element={<WhatsappPage />} />
          <Route path="/acceso" element={<LoginPage />} />
          <Route path="*" element={<><h1>Página no encontrada</h1><p><Link to="/">Volver al inicio</Link></p></>} />
        </Routes>
      </main>
    </div>
  )
}
