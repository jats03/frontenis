import { Link } from 'react-router-dom'
import { CalendarIcon } from './Icons'

/** Botón grande hacia las reservas de entrenos; va el primero de la portada y de las pantallas de entrenamientos. */
export default function ReservasButton({ label = 'Reservas de entrenos: apúntate' }: { label?: string }) {
  return (
    <Link className="button reservas-cta" to="/reservas">
      <CalendarIcon />
      <span>{label}</span>
    </Link>
  )
}
