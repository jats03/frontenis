import { useState } from 'react'
import { confirmDelete } from '../alerts'
import type { Player, Slot } from '../api/types'

interface Props {
  slot: Slot
  /** Jugadores del club, para apuntar a alguien a mano. */
  players: Player[]
  onOpenNow: () => void
  onCancel: () => void
  onRestore: () => void
  onDelete: () => void
  onSignUp: (playerId: number) => void
}

/** Acciones del administrador sobre una franja: abrir ya, cancelar o restaurar, eliminar y apuntar a un jugador. */
export default function SlotAdminActions({ slot, players, onOpenNow, onCancel, onRestore, onDelete, onSignUp }: Props) {
  const [playerId, setPlayerId] = useState('')
  const taken = new Set(slot.attendees.map(a => a.playerId))
  const available = players.filter(p => !taken.has(p.id))
  const editable = slot.status === 'Pending' || slot.status === 'Open'

  async function remove() {
    if (await confirmDelete('¿Eliminar esta franja?', 'Se borrarán también los jugadores apuntados.')) onDelete()
  }

  return (
    <div className="slot-actions">
      {slot.status === 'Open' && available.length > 0 && (
        <div className="row">
          <label className="grow">Apuntar a un jugador
            <select value={playerId} onChange={e => setPlayerId(e.target.value)}>
              <option value="">Elegir…</option>
              {available.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
          <button type="button" disabled={!playerId}
            onClick={() => { onSignUp(Number(playerId)); setPlayerId('') }}>Apuntar</button>
        </div>
      )}
      <div className="row">
        {slot.status === 'Pending' && <button type="button" onClick={onOpenNow}>Abrir ya</button>}
        {editable && <button type="button" className="secondary" onClick={onCancel}>Cancelar franja</button>}
        {slot.status === 'Cancelled' && <button type="button" className="secondary" onClick={onRestore}>Restaurar</button>}
        <button type="button" className="secondary danger" onClick={() => void remove()}>Eliminar</button>
      </div>
    </div>
  )
}
