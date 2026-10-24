import { describe, expect, it } from 'vitest'
import { ACTOR_LABEL, describeAction, statusClass } from './auditView'

describe('describeAction', () => {
  it.each([
    ['POST', '/api/auth/login', 'Inicio de sesión'],
    ['POST', '/api/slots/12/attend', 'Se apunta a la franja 12'],
    ['DELETE', '/api/slots/12/attend/5', 'Se quita de la franja 12 (jugador 5)'],
    ['POST', '/api/slots/12/close', 'Cierra las inscripciones de la franja 12'],
    ['POST', '/api/trainings/3/matches', 'Registra un partido en el entreno 3'],
    ['DELETE', '/api/trainings/3/matches/9', 'Borra el partido 9 del entreno 3'],
    ['DELETE', '/api/competitions/4/groups/2/players/7', 'Quita al jugador 7 del grupo 2 (competición 4)'],
  ])('%s %s', (method, path, expected) => {
    expect(describeAction(method, path)).toBe(expected)
  })

  it('ignora la query string', () => {
    expect(describeAction('DELETE', '/api/players/8?force=true')).toBe('Elimina al jugador 8')
  })

  it('la regla más específica gana a la general', () => {
    expect(describeAction('POST', '/api/players/pairing-preview')).toBe('Previsualiza un sorteo de parejas')
    expect(describeAction('POST', '/api/players')).toBe('Crea un jugador')
    expect(describeAction('DELETE', '/api/matches/5')).toBe('Borra el partido 5 de su jornada')
    expect(describeAction('DELETE', '/api/matches/5/result')).toBe('Borra el resultado del partido 5')
  })

  it('si no se reconoce devuelve método y ruta tal cual', () => {
    expect(describeAction('GET', '/api/raro?x=1')).toBe('GET /api/raro')
  })
})

describe('statusClass', () => {
  it('clasifica por código HTTP', () => {
    expect(statusClass(200)).toBe('ok')
    expect(statusClass(399)).toBe('ok')
    expect(statusClass(400)).toBe('warn')
    expect(statusClass(409)).toBe('warn')
    expect(statusClass(500)).toBe('bad')
  })
})

describe('ACTOR_LABEL', () => {
  it('traduce los tres actores', () => {
    expect(Object.values(ACTOR_LABEL)).toEqual(['Administrador', 'Jugador', 'Visitante'])
  })
})
