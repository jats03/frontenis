import { NavLink, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import CompetitionNewPage from './pages/CompetitionNewPage'
import CompetitionsPage from './pages/CompetitionsPage'
import LoginPage from './pages/LoginPage'
import PlayersPage from './pages/PlayersPage'

export default function App() {
  const { isAdmin, username, logout } = useAuth()
  return (
    <div className="app">
      <header className="topbar">
        <span className="brand">🎾 Frontenis</span>
        <nav>
          <NavLink to="/" end>Competiciones</NavLink>
          <NavLink to="/jugadores">Jugadores</NavLink>
        </nav>
        {isAdmin ? (
          <button className="link" onClick={logout}>Salir ({username})</button>
        ) : (
          <NavLink to="/login">Admin</NavLink>
        )}
      </header>
      <main className="content">
        <Routes>
          <Route path="/" element={<CompetitionsPage />} />
          <Route path="/competiciones/nueva" element={<CompetitionNewPage />} />
          <Route path="/competiciones/:id" element={<CompetitionDetailPage />} />
          <Route path="/jugadores" element={<PlayersPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="*" element={<p>Página no encontrada.</p>} />
        </Routes>
      </main>
    </div>
  )
}
