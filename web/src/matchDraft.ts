/** Borrador del partido de entrenamiento que se está registrando: se guarda en el navegador para no perderlo a medias. */
export interface MatchDraft { slots: string[]; homeScore: string; awayScore: string }

const key = (trainingId: number) => `frontenis.draft.match.${trainingId}`
export const EMPTY_SLOTS = ['', '', '', '']

export const isDraftEmpty = (d: MatchDraft) => d.slots.every(s => s === '') && d.homeScore === '' && d.awayScore === ''

export function loadDraft(trainingId: number): MatchDraft {
  try {
    const d = JSON.parse(localStorage.getItem(key(trainingId)) ?? 'null') as MatchDraft | null
    if (d && Array.isArray(d.slots) && d.slots.length === 4 && typeof d.homeScore === 'string' && typeof d.awayScore === 'string') return d
  } catch { /* sin almacenamiento o dato corrupto */ }
  return { slots: EMPTY_SLOTS, homeScore: '', awayScore: '' }
}

/** Guarda el borrador; si está vacío, lo borra. */
export function saveDraft(trainingId: number, d: MatchDraft) {
  try {
    if (isDraftEmpty(d)) localStorage.removeItem(key(trainingId))
    else localStorage.setItem(key(trainingId), JSON.stringify(d))
  } catch { /* sin almacenamiento */ }
}
