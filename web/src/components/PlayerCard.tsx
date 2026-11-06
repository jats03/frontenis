import { useEffect, useRef, useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { HistoryFilter } from '../api/types'
import { setAvatarSeed, useAvatarSeed } from '../avatarStore'
import { AVATAR_CHOICES } from '../avatar'
import { cardFromHistory, positionCode } from '../cardStats'
import { useAsync } from '../hooks'
import { useDevice } from '../useDevice'
import LoadingBlock from './LoadingBlock'
import PlayerAvatar from './PlayerAvatar'

// La carta de nivel cuenta todos los partidos jugados, sea cual sea la bola.
const ALL: HistoryFilter = { competitionIds: [], trainingIds: [] }

const CHOICES = Array.from({ length: AVATAR_CHOICES }, (_, i) => i + 1)

interface Props { playerId: number; onClose: () => void }

/** Carta del jugador (estilo videojuego de fútbol) en una superposición: avatar grande, posición y estadísticas. Sin animaciones. */
export default function PlayerCard({ playerId, onClose }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const { data, error } = useAsync(() => api.players.playerHistory(playerId, ALL), [playerId])
  const { me } = useDevice()
  // El avatar lo cambia el titular del dispositivo o uno de sus apadrinados.
  const canChangeAvatar = !!me && (me.owner.id === playerId || me.delegates.some(d => d.id === playerId))
  const current = useAvatarSeed(playerId)
  const [picking, setPicking] = useState(false)
  const [busy, setBusy] = useState(false)
  const [avatarError, setAvatarError] = useState<string | null>(null)

  useEffect(() => {
    const d = ref.current
    if (d && !d.open) d.showModal()
  }, [])

  const summary = data?.summary
  const card = summary ? cardFromHistory(summary) : null
  const name = summary?.name ?? ''
  const [hint, setHint] = useState<string | null>(null)
  const diff = summary ? summary.tantosFor - summary.tantosAgainst : 0
  const counts: [string, string, string][] = summary ? [
    ['V', 'Victorias', String(summary.won)], ['D', 'Derrotas', String(summary.lost)], ['SET', 'Sets ganados', String(summary.setsFor)],
    ['DT', 'Diferencia de tantos (a favor menos en contra)', diff > 0 ? `+${diff}` : diff < 0 ? `−${-diff}` : '0'],
    ['PAR', 'Partidos jugados', String(summary.played)],
  ] : []
  const hinted = counts.find(([label]) => label === hint)

  /** Guarda el avatar elegido (vacío = el de siempre) y lo refleja al instante en toda la app. */
  async function chooseAvatar(seed: number | null) {
    setAvatarError(null)
    setBusy(true)
    try {
      const saved = await api.device.setAvatar(playerId, seed)
      setAvatarSeed(playerId, saved.avatarSeed)
      notify('Avatar cambiado')
      setPicking(false)
    } catch (e) { setAvatarError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  return (
    <dialog ref={ref} className="card-dialog" aria-label={name ? `Carta de ${name}` : 'Carta del jugador'}
      onClose={onClose} onClick={e => { if (e.target === e.currentTarget) ref.current?.close() /* solo si se pulsa fuera de la carta */ }}>
      {!summary || !card ? (
        error ? <p role="alert">{error}</p> : <LoadingBlock />
      ) : picking ? null /* al elegir avatar solo se ve la cuadrícula, para que quepa en pantallas pequeñas */ : (
        <div className="fut-wrap">
        <button type="button" className="fut-x" aria-label="Cerrar la carta" onClick={() => ref.current?.close()}>
          <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2.6" strokeLinecap="round"><path d="M6 6l12 12M18 6L6 18" /></svg>
        </button>
        <div className={`fut-card ${card.tier}`}>
          <div className="fut-top">
            {card.played > 0 && (
              <div className="fut-rating">
                <strong>{card.overall}</strong>
                <span title={summary.position}>{positionCode(summary.position)}</span>
              </div>
            )}
            <PlayerAvatar playerId={playerId} size={150} className="fut-avatar" />
          </div>
          <h2 className="fut-name">{summary.name}</h2>
          <p className="fut-position">{summary.position}</p>
          {card.played > 0 && <>
          <ul className="fut-stats" aria-label="Estadísticas">
            {counts.map(([label, title, value]) => (
              <li key={label}>
                {/* En el móvil no hay «pasar el ratón»: al tocar una sigla se explica debajo. */}
                <button type="button" className="fut-stat" aria-pressed={hint === label} title={title}
                  onClick={e => { e.stopPropagation(); setHint(h => (h === label ? null : label)) }}>
                  <strong>{value}</strong>
                  <span aria-hidden="true">{label}</span>
                  <span className="sr-only"> {title}</span>
                </button>
              </li>
            ))}
          </ul>
          <p className="fut-hint" aria-live="polite">
            {hinted ? <span><strong>{hinted[0]}</strong> · {hinted[1]}</span> : 'Toca una sigla para ver qué significa'}
          </p>
          </>}
        </div>
        </div>
      )}
      {summary && canChangeAvatar && (
        <div className="avatar-change" onClick={e => e.stopPropagation()}>
          {!picking
            ? <button type="button" className="secondary fut-close" onClick={() => setPicking(true)}>Cambiar avatar</button>
            : (
              <section className="card avatar-picker" aria-label="Elegir avatar">
                <h2 className="avatar-title">Avatar de {name}</h2>
                {avatarError && <p className="error" role="alert">{avatarError}</p>}
                <div className="avatar-grid" role="radiogroup" aria-label="Avatares">
                  {/* El de siempre del jugador: vuelve a él. Lleva «Actual» solo si es el que tiene ahora. */}
                  <button type="button" role="radio" aria-checked={current === null} aria-label={current === null ? 'Avatar original (actual)' : 'Avatar original'}
                    disabled={busy} className="avatar-option" onClick={() => void chooseAvatar(null)}>
                    <PlayerAvatar playerId={playerId} seed={playerId} size={48} />
                    {current === null && <span>Actual</span>}
                  </button>
                  {CHOICES.map(n => (
                    <button key={n} type="button" role="radio" aria-checked={current === n} aria-label={current === n ? `Avatar ${n} (actual)` : `Avatar ${n}`} disabled={busy}
                      className="avatar-option" onClick={() => void chooseAvatar(n)}>
                      <PlayerAvatar playerId={playerId} seed={n} size={48} />
                      {current === n && <span>Actual</span>}
                    </button>
                  ))}
                </div>
                <button type="button" className="secondary" onClick={() => setPicking(false)}>Cancelar</button>
              </section>
            )}
        </div>
      )}
    </dialog>
  )
}
