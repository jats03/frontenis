import type { ClothingSize } from './api/types'

/** Tallas admitidas (las mismas que valida el servidor): de niño (6…16) y de adulto (XS…3XL). */
export const CLOTHING_SIZES = ['6', '8', '10', '12', '14', '16', 'XS', 'S', 'M', 'L', 'XL', 'XXL', '3XL'] as const

/** Falta alguna de las dos tallas (camiseta o pantalón). */
export const isIncomplete = (r: Pick<ClothingSize, 'topSize' | 'bottomSize'>) => !r.topSize || !r.bottomSize

/** Cuántos jugadores han indicado cada talla, en el orden de <see cref="CLOTHING_SIZES"/>. */
export function sizeCounts(rows: ClothingSize[], pick: (r: ClothingSize) => string | null) {
  return CLOTHING_SIZES
    .map(size => ({ size, count: rows.filter(r => pick(r) === size).length }))
    .filter(c => c.count > 0)
}
