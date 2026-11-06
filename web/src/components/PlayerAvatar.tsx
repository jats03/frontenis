import { AVATAR_BASE, avatarParts, type AvatarParts } from '../avatar'
import { useAvatarSeed } from '../avatarStore'

interface Props {
  playerId: number
  /** Fuerza un avatar concreto (para la cuadrícula de elección); si no, el que eligió el jugador o el de siempre. */
  seed?: number
  /** Lado en píxeles. */
  size?: number
  className?: string
}

/** Pelo por detrás de la cabeza (melena y rizos). */
function HairBack({ style, color }: { style: AvatarParts['hairStyle']; color: string }) {
  if (style === 'long') return <path d="M17 30c0-14 6-19 15-19s15 5 15 19v14c0 3-4 4-6 2l-1-12H24l-1 12c-2 2-6 1-6-2z" fill={color} />
  if (style === 'curly') return <g fill={color}><circle cx="32" cy="27" r="17" /><circle cx="19" cy="34" r="6" /><circle cx="45" cy="34" r="6" /></g>
  if (style === 'afro') return <circle cx="32" cy="26" r="21" fill={color} />
  if (style === 'ponytail') return <path d="M43 20c10 1 12 13 5 24-1-7-3-11-6-14z" fill={color} />
  return null
}

/** Pelo y complementos por delante de la cabeza. */
function HairFront({ style, color, shirt }: { style: AvatarParts['hairStyle']; color: string; shirt: string }) {
  switch (style) {
    case 'long':
      return <path d="M19 28c1-9 7-13 13-13s12 4 13 13c-4-5-9-7-13-7s-9 2-13 7z" fill={color} />
    case 'bun':
      return (
        <g fill={color}>
          <circle cx="32" cy="12" r="5.5" />
          <path d="M19 28c0-10 6-14 13-14s13 4 13 14c-4-5-8-6-13-6s-9 1-13 6z" />
        </g>
      )
    case 'curly':
      return <path d="M20 27c1-8 6-11 12-11s11 3 12 11c-4-3-8-4-12-4s-8 1-12 4z" fill={color} />
    case 'afro':
      return <path d="M19.500 27c1-8 6-11 12.500-11s11.500 3 12.500 11c-4-3-8-4-12.500-4s-8.500 1-12.500 4z" fill={color} />
    case 'bald':
      return null
    case 'mohawk':
      return <path d="M27.500 21c-1-6 1-11 4.500-11s5.500 5 4.500 11c-1.500-1-3-1.500-4.500-1.500s-3 .5-4.500 1.500z" fill={color} />
    case 'buzz':
      return <path d="M19 29c-1-11 6-15 13-15s14 4 13 15c-2-6-7-8-13-8s-11 2-13 8z" fill={color} opacity=".45" />
    case 'cap':
      return (
        <g>
          <path d="M18.5 27c0-10 6-15 13.5-15s13.5 5 13.5 15z" fill={shirt} />
          <path d="M31 26h17c0 3-3 4-8 4h-9z" fill={shirt} opacity=".85" />
          <path d="M18.5 27h27v2h-27z" fill="#00000022" />
        </g>
      )
    case 'headband':
      return (
        <g>
          <path d="M19 27c0-9 6-13 13-13s13 4 13 13c-4-4-8-5-13-5s-9 1-13 5z" fill={color} />
          <rect x="18.6" y="24.5" width="26.8" height="4.2" rx="2" fill="#fff" />
          <rect x="18.6" y="26.9" width="26.8" height="1.8" fill={shirt} opacity=".9" />
        </g>
      )
    default:
      return <path d="M19 29c-1-11 6-15 13-15s14 4 13 15c-2-6-7-8-13-8s-11 2-13 8z" fill={color} />
  }
}

/**
 * Muñeco del jugador: rasgos fijos (pelo, piel, camiseta) elegidos a partir de su identificador o del avatar que haya elegido.
 * Es decorativo, por eso va oculto para lectores de pantalla.
 */
export default function PlayerAvatar({ playerId, seed, size = 40, className }: Props) {
  const chosen = useAvatarSeed(playerId)
  const picked = seed ?? chosen ?? undefined // avatar elegido a mano; sin él, el de siempre (el del identificador)
  const p = avatarParts(picked ?? playerId, picked !== undefined && picked > AVATAR_BASE)
  const mouthColor = p.accessory === 'beard' ? '#fff3ea' : '#2b1d14' // sobre la barba la boca se ve en claro
  return (
    <svg className={className} width={size} height={size} viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <circle cx="32" cy="32" r="32" fill={p.background} />
      <HairBack style={p.hairStyle} color={p.hair} />
      {/* hombros y camiseta */}
      <path d="M8 64c0-11 9-17 24-17s24 6 24 17z" fill={p.shirt} />
      <path d="M26 47l6 6 6-6z" fill="#ffffff55" />
      {/* cuello y cabeza */}
      <rect x="28" y="38" width="8" height="10" rx="3" fill={p.skin} />
      <ellipse cx="32" cy="31" rx="13" ry="14" fill={p.skin} />
      <ellipse cx="19.2" cy="32" rx="2.4" ry="3.2" fill={p.skin} />
      <ellipse cx="44.8" cy="32" rx="2.4" ry="3.2" fill={p.skin} />
      <HairFront style={p.hairStyle} color={p.hair} shirt={p.shirt} />
      {p.accessory === 'beard' && <path d="M19.500 34c.5 12 6 17 12.500 17s12-5 12.500-17c-3 5-6.500 7.500-12.500 7.500S22.500 39 19.500 34z" fill={p.hair} />}
      {/* cara */}
      <circle cx="26.5" cy="33" r="1.7" fill="#2b1d14" />
      <circle cx="37.5" cy="33" r="1.7" fill="#2b1d14" />
      <circle cx="23.5" cy="37.5" r="2.4" fill="#ff7a7a" opacity=".28" />
      <circle cx="40.5" cy="37.5" r="2.4" fill="#ff7a7a" opacity=".28" />
      {p.mouth === 'smile' && <path d="M27.5 38.5c1.2 2.2 7.8 2.2 9 0" stroke={mouthColor} strokeWidth="1.6" strokeLinecap="round" fill="none" />}
      {p.mouth === 'grin' && <path d="M27 38c1 4.5 9 4.5 10 0z" fill={mouthColor} />}
      {p.mouth === 'neutral' && <path d="M28.500 39.500h7" stroke={mouthColor} strokeWidth="1.6" strokeLinecap="round" fill="none" />}
      {p.accessory === 'mustache' && <path d="M26 37.600c2-2.400 4-1.600 6-.4 2-1.200 4-2 6 .4-2 2.400-4 1.400-6 .6-2 .8-4 1.800-6-.6z" fill={p.hair} />}
      {p.accessory === 'earring' && <circle cx="19.200" cy="36.800" r="1.400" fill="#f2c200" />}
      {p.accessory === 'glasses' && (
        <g fill="none" stroke="#2b1d14" strokeWidth="1.300">
          <circle cx="26.500" cy="33" r="4.300" /><circle cx="37.500" cy="33" r="4.300" /><path d="M30.800 33h2.400" />
        </g>
      )}
      {p.accessory === 'sunglasses' && (
        <g fill="#1a1a1f">
          <rect x="21.500" y="29.800" width="10" height="6.200" rx="2.800" /><rect x="32.500" y="29.800" width="10" height="6.200" rx="2.800" /><rect x="31" y="31.500" width="2" height="1.300" />
        </g>
      )}
    </svg>
  )
}
