import type { ClothingSize } from './api/types'

const KIDS = ['6', '8', '10', '12', '14', '16']
const ADULT = ['S', 'M', 'L', 'XL', 'XXL', '3XL', '4XL']
/** Tallas de adulto sin sexo guardadas antes de separar mujer y hombre: se siguen mostrando, pero ya no se ofrecen. */
const LEGACY = ['XS', 'S', 'M', 'L', 'XL', 'XXL', '3XL']

/** Las tallas de adulto llevan el sexo como sufijo (las mismas que valida el servidor): «SM» = S de mujer, «SH» = S de hombre. */
const withSex = (sizes: string[]) => sizes.flatMap(s => [`${s}M`, `${s}H`])

/** Tallas que se ofrecen: de niño (6…16) y de adulto de la S a la 4XL, de mujer y de hombre. */
export const CLOTHING_SIZES = [...KIDS, ...withSex(ADULT)]

/** Todas las tallas que pueden aparecer guardadas, en el orden en que se cuentan. */
const KNOWN_SIZES = [...KIDS, ...LEGACY, ...withSex(ADULT)]

/** «SM» → «S (mujer)», «4XLH» → «4XL (hombre)»; las de niño y las antiguas sin sexo se dejan como están. */
export function sizeLabel(size: string): string {
  if (KNOWN_SIZES.includes(size) && !withSex(ADULT).includes(size)) return size
  const sex = size.endsWith('M') ? 'mujer' : size.endsWith('H') ? 'hombre' : null
  return sex ? `${size.slice(0, -1)} (${sex})` : size
}

/** Falta alguna de las dos tallas (camiseta o pantalón). */
export const isIncomplete = (r: Pick<ClothingSize, 'topSize' | 'bottomSize'>) => !r.topSize || !r.bottomSize

/** Cuántos jugadores han indicado cada talla, en el orden de las tallas conocidas. */
export function sizeCounts(rows: ClothingSize[], pick: (r: ClothingSize) => string | null) {
  return KNOWN_SIZES
    .map(size => ({ size, count: rows.filter(r => pick(r) === size).length }))
    .filter(c => c.count > 0)
}
