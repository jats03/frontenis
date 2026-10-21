import type { AuditActor } from './api/types'

export const ACTOR_LABEL: Record<AuditActor, string> = { Admin: 'Administrador', Player: 'Jugador', Visitor: 'Visitante' }

const RULES: [RegExp, string][] = [
  [/^POST \/api\/auth\/login/, 'Inicio de sesión'],
  [/^POST \/api\/auth\/change-password/, 'Cambio de contraseña'],
  [/^POST \/api\/slots\/(\d+)\/attend$/, 'Se apunta a la franja $1'],
  [/^DELETE \/api\/slots\/(\d+)\/attend\/(\d+)/, 'Se quita de la franja $1 (jugador $2)'],
  [/^POST \/api\/slots\/(\d+)\/attendees/, 'Admin apunta a alguien a la franja $1'],
  [/^DELETE \/api\/slots\/(\d+)\/attendees/, 'Admin quita a alguien de la franja $1'],
  [/^POST \/api\/slots\/(\d+)\/close/, 'Cierra las inscripciones de la franja $1'],
  [/^POST \/api\/slots\/close-week/, 'Cierra las inscripciones de una semana'],
  [/^POST \/api\/slots\/open-week/, 'Abre una semana de entrenos'],
  [/^POST \/api\/slots\/(\d+)\/open/, 'Abre la franja $1'],
  [/^POST \/api\/slots/, 'Crea una franja'],
  [/^PUT \/api\/slots\/(\d+)/, 'Edita la franja $1'],
  [/^DELETE \/api\/slots\/(\d+)/, 'Elimina la franja $1'],
  [/^POST \/api\/device\/link/, 'Vincula su dispositivo («Soy yo»)'],
  [/^DELETE \/api\/device\/link/, 'Desvincula su dispositivo'],
  [/^DELETE \/api\/device-links\/(\d+)/, 'Libera el dispositivo del jugador $1'],
  [/^POST \/api\/trainings\/(\d+)\/matches/, 'Registra un partido en el entreno $1'],
  [/^DELETE \/api\/trainings\/(\d+)\/matches\/(\d+)/, 'Borra el partido $2 del entreno $1'],
  [/^POST \/api\/trainings/, 'Crea un entreno'],
  [/^DELETE \/api\/trainings\/(\d+)/, 'Elimina el entreno $1'],
  [/^PUT \/api\/trainings\/(\d+)\/name/, 'Renombra el entreno $1'],
  [/^POST \/api\/players\/pairing-preview/, 'Previsualiza un sorteo de parejas'],
  [/^POST \/api\/players/, 'Crea un jugador'],
  [/^PUT \/api\/players\/(\d+)/, 'Edita al jugador $1'],
  [/^DELETE \/api\/players\/(\d+)/, 'Elimina al jugador $1'],
  [/^PUT \/api\/matches\/(\d+)\/submission/, 'Envía el resultado del partido $1 (pendiente de aprobar)'],
  [/^POST \/api\/match-submissions\/(\d+)\/approve/, 'Aprueba el resultado enviado $1'],
  [/^DELETE \/api\/match-submissions\/(\d+)/, 'Rechaza el resultado enviado $1'],
  [/^DELETE \/api\/match-rejections\/(\d+)/, 'Elimina el resultado rechazado $1'],
  [/^POST \/api\/match-rejections\/(\d+)\/restore/, 'Recupera el resultado rechazado $1'],
  [/^DELETE \/api\/matches\/(\d+)$/, 'Borra el partido $1 de su jornada'],
  [/^PUT \/api\/matches\/(\d+)\/swap/, 'Intercambia de jornada el partido $1 con otro'],
  [/^PUT \/api\/matches\/(\d+)\/round/, 'Mueve el partido $1 de jornada'],
  [/^PUT \/api\/matches\/(\d+)/, 'Guarda el resultado del partido $1'],
  [/^DELETE \/api\/matches\/(\d+)/, 'Borra el resultado del partido $1'],
  [/^POST \/api\/competitions\/(\d+)\/rotating\/players/, 'Añade un jugador a la liga rotatoria $1'],
  [/^PUT \/api\/competitions\/(\d+)\/groups\/(\d+)\/rounds\/swap/, 'Intercambia dos jornadas del grupo $2 (competición $1)'],
  [/^DELETE \/api\/competitions\/(\d+)\/groups\/(\d+)\/rounds\/(\d+)/, 'Quita la jornada $3 del grupo $2 (competición $1)'],
  [/^POST \/api\/competitions\/(\d+)\/groups\/(\d+)\/matches/, 'Añade un partido al grupo $2 (competición $1)'],
  [/^POST \/api\/competitions\/(\d+)\/draw/, 'Hace el sorteo de la competición $1'],
  [/^POST \/api\/competitions/, 'Crea una competición o inscribe parejas'],
  [/^DELETE \/api\/competitions\/(\d+)/, 'Elimina la competición $1'],
  [/^(POST|PUT|DELETE) \/api\/player-delegates/, 'Cambia apadrinados'],
  [/^(POST|PUT|DELETE) \/api\/slot-templates/, 'Cambia la plantilla semanal'],
]

/** Descripción en castellano de una acción registrada; si no se reconoce, devuelve método y ruta tal cual. */
export function describeAction(method: string, path: string): string {
  const key = `${method} ${path.split('?')[0]}`
  for (const [re, text] of RULES) {
    const m = re.exec(key)
    if (m) return text.replace(/\$(\d)/g, (_, i: string) => m[Number(i)] ?? '')
  }
  return key
}

/** Clase de color según el resultado HTTP. */
export const statusClass = (status: number) => (status >= 500 ? 'bad' : status >= 400 ? 'warn' : 'ok')
