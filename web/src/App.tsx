import { useEffect, useRef } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import ApiStatus from './components/ApiStatus'
import LoadingSpinner from './components/LoadingSpinner'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import LoginPage from './pages/LoginPage'
import CookiesPage from './pages/CookiesPage'
import PrivacyPage from './pages/PrivacyPage'
import TermsPage from './pages/TermsPage'
import PlayersPage from './pages/PlayersPage'
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
        </nav>
        {isAdmin ? (
          <button className="link" onClick={logout}>Salir ({username})</button>
        ) : (
          <NavLink to="/login">Admin</NavLink>
        )}
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
          <Route path="/login" element={<LoginPage />} />
          <Route path="/privacidad" element={<PrivacyPage />} />
          <Route path="/terminos" element={<TermsPage />} />
          <Route path="/cookies" element={<CookiesPage />} />
          <Route path="*" element={<><h1>Página no encontrada</h1><p><Link to="/">Volver al inicio</Link></p></>} />
        </Routes>
      </main>
      <footer className="site-footer">
        <nav aria-label="Información legal">
          <Link to="/privacidad">Política de privacidad</Link>
          <Link to="/terminos">Términos y condiciones</Link>
          <Link to="/cookies">Política de cookies</Link>
        </nav>
      </footer>
    </div>
  )
}
