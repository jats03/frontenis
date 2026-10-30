import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import PageHeader from '../components/PageHeader'
import { useDocumentTitle } from '../hooks'
import { useDevice } from '../useDevice'

export default function MorePage() {
  useDocumentTitle('Más')
  const { isAdmin, username, logout } = useAuth()
  const device = useDevice()
  return (
    <>
      <PageHeader title="Más" subtitle="Herramientas y cuenta" />
      <h2 className="section-title">Herramientas</h2>
      <div className="menu-cards">
        <Link className="card link-card" to="/reservas">
          <strong>Reservas de entrenos</strong>
          <span className="muted">Mira las franjas de la semana y quién se ha apuntado.</span>
        </Link>
        <Link className="card link-card" to="/cronometro">
          <strong>Cronómetro</strong>
          <span className="muted">Cuenta atrás para los partidos y los turnos.</span>
        </Link>
        {(isAdmin || device.me) && (
          <Link className="card link-card" to="/tallas">
            <strong>Tallas de ropa</strong>
            <span className="muted">{isAdmin ? 'Camiseta y pantalón de cada jugador, para hacer el pedido.' : 'Indica tu talla de camiseta y pantalón y la de tus apadrinados.'}</span>
          </Link>
        )}
        <Link className="card link-card" to="/plantillas">
          <strong>Plantillas de WhatsApp</strong>
          <span className="muted">Genera el cuadro semanal de entrenamientos listo para copiar y pegar.</span>
        </Link>
      </div>
      {isAdmin && (
        <>
          <h2 className="section-title">Cuenta de administrador</h2>
          <div className="menu-cards">
            <Link className="card link-card" to="/cuenta">
              <strong>Mi cuenta</strong>
              <span className="muted">Cambiar la contraseña ({username}).</span>
            </Link>
            <Link className="card link-card" to="/registro">
              <strong>Registro de actividad</strong>
              <span className="muted">Quién ha hecho qué: apuntarse, resultados, inicios de sesión…</span>
            </Link>
            <button className="card link-card as-button" onClick={logout}>
              <strong>Cerrar sesión</strong>
              <span className="muted">Salir del modo administrador en este dispositivo.</span>
            </button>
          </div>
        </>
      )}
    </>
  )
}
