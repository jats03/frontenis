import { useEffect, useRef } from 'react'
import { Link, Route, Routes, useLocation } from 'react-router-dom'
import ApiStatus from './components/ApiStatus'
import AppNav from './components/AppNav'
import LoadingSpinner from './components/LoadingSpinner'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import MorePage from './pages/MorePage'
import PlayerDetailPage from './pages/PlayerDetailPage'
import PlayerHistoryPage from './pages/PlayerHistoryPage'
import PlayersPage from './pages/PlayersPage'
import AccountPage from './pages/AccountPage'
import TrainingDetailPage from './pages/TrainingDetailPage'
import TrainingsPage from './pages/TrainingsPage'
import WhatsappPage from './pages/WhatsappPage'
import TimerPage from './pages/TimerPage'

export default function App() {
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
      <AppNav />
      <ApiStatus />
      <LoadingSpinner />
      <main id="contenido" className="content" tabIndex={-1} ref={main}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/competiciones" element={<CompetitionsPage />} />
          <Route path="/mas" element={<MorePage />} />
          <Route path="/competiciones/nueva" element={<CompetitionNewPage />} />
          <Route path="/competiciones/:id" element={<CompetitionDetailPage />} />
          <Route path="/jugadores" element={<PlayersPage />} />
          <Route path="/jugadores/historico" element={<PlayerHistoryPage />} />
          <Route path="/jugadores/:id" element={<PlayerDetailPage />} />
          <Route path="/entrenamientos" element={<TrainingsPage />} />
          <Route path="/entrenamientos/:id" element={<TrainingDetailPage />} />
          <Route path="/cronometro" element={<TimerPage />} />
          <Route path="/plantillas" element={<WhatsappPage />} />
          <Route path="/cuenta" element={<AccountPage />} />
          <Route path="/acceso" element={<LoginPage />} />
          <Route path="*" element={<><h1>Página no encontrada</h1><p><Link to="/">Volver al inicio</Link></p></>} />
        </Routes>
      </main>
    </div>
  )
}
