import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addDaysIso, formatClock, formatDateTimeMadrid, formatOpening, formatTime, mondayOfIso, parseDate, parseUtc, toApiTime,
  todayIso, trainingTitle, weekTitle,
} from './dates'

describe('dates', () => {
  beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date('2026-10-21T10:00:00')) })
  afterEach(() => vi.useRealTimers())

  it('todayIso da la fecha local con ceros', () => {
    vi.setSystemTime(new Date('2026-03-05T08:00:00'))
    expect(todayIso()).toBe('2026-03-05')
  })

  it('parseDate interpreta una fecha sin hora como local', () => {
    const d = parseDate('2026-10-13')
    expect([d.getFullYear(), d.getMonth(), d.getDate(), d.getHours()]).toEqual([2026, 9, 13, 0])
  })

  it('parseUtc añade la Z solo cuando falta zona', () => {
    expect(parseUtc('2026-10-21T10:00:00').toISOString()).toBe('2026-10-21T10:00:00.000Z')
    expect(parseUtc('2026-10-21T10:00:00Z').toISOString()).toBe('2026-10-21T10:00:00.000Z')
    expect(parseUtc('2026-10-21T12:00:00+02:00').toISOString()).toBe('2026-10-21T10:00:00.000Z')
  })

  it('muestra la hora de Madrid a partir de un instante UTC', () => {
    expect(formatClock('2026-07-01T17:42:00')).toBe('19:42') // verano: UTC+2
    expect(formatClock('2026-01-01T17:42:00')).toBe('18:42') // invierno: UTC+1
    expect(formatDateTimeMadrid('2026-10-21T10:00:00')).toContain('12:00')
  })

  it('trainingTitle pone el día de la semana en mayúscula', () => {
    expect(trainingTitle('2026-10-22')).toBe('Jueves, 22 de octubre')
  })

  it('addDaysIso cruza meses y años', () => {
    expect(addDaysIso('2026-10-30', 3)).toBe('2026-11-02')
    expect(addDaysIso('2026-12-31', 1)).toBe('2027-01-01')
    expect(addDaysIso('2026-03-01', -1)).toBe('2026-02-28')
  })

  it('mondayOfIso devuelve el lunes de la semana (lunes a domingo)', () => {
    expect(mondayOfIso('2026-10-21')).toBe('2026-10-19') // miércoles
    expect(mondayOfIso('2026-10-19')).toBe('2026-10-19') // lunes
    expect(mondayOfIso('2026-10-25')).toBe('2026-10-19') // domingo
  })

  it('weekTitle distingue esta semana, la próxima y otras', () => {
    expect(weekTitle('2026-10-19')).toBe('Esta semana')
    expect(weekTitle('2026-10-26')).toBe('Próxima semana')
    expect(weekTitle('2026-11-02')).toBe('Semana del 2 al 8 de noviembre')
    expect(weekTitle('2026-11-30')).toBe('Semana del 30 de noviembre al 6 de diciembre')
  })

  it('formatOpening junta día y hora local', () => {
    expect(formatOpening('2026-10-16T19:00:00')).toBe('viernes, 16 de octubre, 19:00')
  })

  it('formatTime y toApiTime convierten entre 18:00 y 18:00:00', () => {
    expect(formatTime('18:00:00')).toBe('18:00')
    expect(toApiTime('18:00')).toBe('18:00:00')
    expect(toApiTime('18:00:00')).toBe('18:00:00')
  })
})
