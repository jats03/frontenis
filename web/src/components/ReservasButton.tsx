import { Link } from 'react-router-dom'
import { CalendarIcon } from './Icons'

/** Botón grande hacia las reservas de entrenos; va el primero de las pantallas de entrenamientos. */
export default function ReservasButton() {
  return (
    <Link className="button reservas-cta" to="/reservas">
      <CalendarIcon />
      <span>Reservas de entrenos: apúntate</span>
    </Link>
  )
}
