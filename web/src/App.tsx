import { useEffect, useRef } from 'react'
import { logPageView } from './audit'
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import { useDevice } from './useDevice'
import ApiStatus from './components/ApiStatus'
import AppNav from './components/AppNav'
import LoadingBlock from './components/LoadingBlock'
import LoadingSpinner from './components/LoadingSpinner'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import MorePage from './pages/MorePage'
import PlayerDetailPage from './pages/PlayerDetailPage'
import PlayerHistoryPage from './pages/PlayerHistoryPage'
import ClothingSizesPage from './pages/ClothingSizesPage'
import PlayersRoute from './pages/PlayersRoute'
import ReservasGestionPage from './pages/ReservasGestionPage'
import ReservasPage from './pages/ReservasPage'
import AccountPage from './pages/AccountPage'
import TrainingDetailPage from './pages/TrainingDetailPage'
import TrainingsPage from './pages/TrainingsPage'
import AuditPage from './pages/AuditPage'
import WhatsappPage from './pages/WhatsappPage'
import TimerPage from './pages/TimerPage'
import MatchPlayPage from './pages/MatchPlayPage'

export default function App() {
  const { pathname } = useLocation()
  const main = useRef<HTMLElement>(null)
  const first = useRef(true)
  // Al cambiar de página, el foco pasa al contenido (accesibilidad para teclado y lectores de pantalla).
  useEffect(() => {
    if (first.current) { first.current = false; return }
    main.current?.focus()
  }, [pathname])
  // Cada pantalla que se abre queda anotada en el registro del servidor.
  useEffect(() => { logPageView(pathname) }, [pathname])
  const { isAdmin } = useAuth()
  const { me } = useDevice()
  // Sin identificar (y sin ser administrador) no se puede ver nada más que la portada.
  const identified = isAdmin || me !== null
  return (
    <div className="app">
      <a className="skip-link" href="#contenido" onClick={e => { e.preventDefault(); main.current?.focus() }}>Saltar al contenido</a>
      <AppNav />
      <ApiStatus />
      <LoadingSpinner />
      <main id="contenido" className="content" tabIndex={-1} ref={main}>
        {me === undefined && !isAdmin && pathname !== '/' ? (
          <LoadingBlock /> // aún no se sabe quién eres: no se abre ninguna pantalla con sus datos
        ) : !identified ? (
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/acceso" element={<LoginPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        ) : (
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/competiciones" element={<CompetitionsPage />} />
          <Route path="/mas" element={<MorePage />} />
          <Route path="/registro" element={<AuditPage />} />
          <Route path="/tallas" element={<ClothingSizesPage />} />
          <Route path="/competiciones/nueva" element={<CompetitionNewPage />} />
          <Route path="/competiciones/:id" element={<CompetitionDetailPage />} />
          <Route path="/competiciones/:id/partidos/:matchId" element={<MatchPlayPage />} />
          <Route path="/jugadores" element={<PlayersRoute />} />
          <Route path="/jugadores/historico" element={<PlayerHistoryPage />} />
          <Route path="/jugadores/:id" element={<PlayerDetailPage />} />
          <Route path="/entrenamientos" element={<TrainingsPage />} />
          <Route path="/entrenamientos/:id" element={<TrainingDetailPage />} />
          <Route path="/reservas" element={<ReservasPage />} />
          <Route path="/reservas/gestion" element={<ReservasGestionPage />} />
          <Route path="/cronometro" element={<TimerPage />} />
          <Route path="/plantillas" element={<WhatsappPage />} />
          <Route path="/cuenta" element={<AccountPage />} />
          <Route path="/acceso" element={<LoginPage />} />
          <Route path="*" element={<><h1>Página no encontrada</h1><p><Link to="/">Volver al inicio</Link></p></>} />
        </Routes>
        )}
      </main>
    </div>
  )
}
