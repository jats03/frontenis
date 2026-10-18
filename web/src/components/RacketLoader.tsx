/** Una raqueta de frontenis con el mango abajo, dibujada con su centro en x = 22 (pivote en la empuñadura). */
function Racket({ className }: { className: string }) {
  return (
    <g className={`racket ${className}`}>
      {/* mango y garganta */}
      <rect x="20.4" y="52" width="3.2" height="18" rx="1.6" fill="currentColor" opacity=".8" />
      <rect x="20.4" y="62" width="3.2" height="7" rx="1.2" fill="#f2a900" />
      {/* cordaje */}
      <g stroke="currentColor" strokeWidth=".9" opacity=".55">
        <line x1="16" y1="26.3" x2="16" y2="49.7" /><line x1="20" y1="24.2" x2="20" y2="51.8" />
        <line x1="24" y1="24.2" x2="24" y2="51.8" /><line x1="28" y1="26.3" x2="28" y2="49.7" />
        <line x1="12.5" y1="31" x2="31.5" y2="31" /><line x1="11" y1="38" x2="33" y2="38" /><line x1="12.5" y1="45" x2="31.5" y2="45" />
      </g>
      {/* marco */}
      <ellipse cx="22" cy="38" rx="11.5" ry="14.5" fill="none" stroke="currentColor" strokeWidth="3" />
    </g>
  )
}

/**
 * Animación de carga: dos raquetas se pasan una pelota. La pelota sube y baja en arco mientras viaja de una
 * raqueta a la otra, que golpean justo al llegar. Es decorativa (el aviso accesible lo da el componente que la usa).
 */
export default function RacketLoader() {
  return (
    <svg className="rally" style={{ color: 'var(--loader-ink)' }} viewBox="0 0 160 84" width="160" height="84" aria-hidden="true" focusable="false">
      <ellipse className="rally-shadow" cx="80" cy="78" rx="6" ry="1.6" fill="#000" />
      <Racket className="racket-left" />
      <g transform="translate(160 0) scale(-1 1)"><Racket className="racket-right" /></g>
      <g className="ball-x">
        <g className="ball-y">
          <circle cx="0" cy="0" r="5" fill="#f2a900" stroke="#b97c00" strokeWidth="1" />
          <path d="M-3 -2.2c1.6 1 4 1 5.8-.2" stroke="#fff" strokeWidth=".9" fill="none" opacity=".75" strokeLinecap="round" />
        </g>
      </g>
    </svg>
  )
}
