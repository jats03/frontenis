import { NavLink, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import ApiStatus from './components/ApiStatus'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import LoginPage from './pages/LoginPage'
import PlayersPage from './pages/PlayersPage'
import TimerPage from './pages/TimerPage'

export default function App() {
  const { isAdmin, username, logout } = useAuth()
  return (
    <div className="app">
      <header className="topbar">
        <span className="brand"><img src="/logo.png" alt="Frontenis Club Ciudad de Jaén" className="logo" /> Frontenis</span>
        <nav>
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
      <main className="content">
        <Routes>
          <Route path="/" element={<CompetitionsPage />} />
          <Route path="/competiciones/nueva" element={<CompetitionNewPage />} />
          <Route path="/competiciones/:id" element={<CompetitionDetailPage />} />
          <Route path="/jugadores" element={<PlayersPage />} />
          <Route path="/cronometro" element={<TimerPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="*" element={<p>Página no encontrada.</p>} />
        </Routes>
      </main>
    </div>
  )
}
