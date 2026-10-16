/** Iconos de la barra de navegación (trazo simple, heredan el color del texto). */
const base = { width: 24, height: 24, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: 2,
  strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const, 'aria-hidden': true, focusable: false }

export const HomeIcon = () => <svg {...base}><path d="M3 11l9-8 9 8" /><path d="M5 10v10h14V10" /></svg>
export const TrophyIcon = () => <svg {...base}><path d="M8 4h8v5a4 4 0 0 1-8 0V4z" /><path d="M8 6H4v1a4 4 0 0 0 4 4M16 6h4v1a4 4 0 0 1-4 4" /><path d="M12 13v4M8 20h8M10 17h4" /></svg>
export const CalendarIcon = () => <svg {...base}><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M3 10h18M8 3v4M16 3v4" /></svg>
export const UsersIcon = () => <svg {...base}><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20a6.5 6.5 0 0 1 13 0" /><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 14.5a6.5 6.5 0 0 1 3.5 5.5" /></svg>
export const MoreIcon = () => <svg {...base}><circle cx="5" cy="12" r="1.2" /><circle cx="12" cy="12" r="1.2" /><circle cx="19" cy="12" r="1.2" /></svg>
export const WarningIcon = () => <svg {...base}><path d="M12 3.5 2.5 20h19L12 3.5z" /><path d="M12 10v4.5M12 17.5v.01" /></svg>
