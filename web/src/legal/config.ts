/**
 * DATOS DEL TITULAR DE LA WEB — RELLENAR ANTES DE PUBLICAR.
 * Aparecen en el aviso legal, la política de privacidad y los términos (LSSI-CE art. 10 y RGPD art. 13).
 * Mientras un campo contenga «[COMPLETAR…]» se muestra resaltado en las páginas legales.
 */
export const legal = {
  /** Nombre o razón social (persona física, club o asociación). */
  ownerName: '[COMPLETAR: nombre o razón social del titular]',
  /** NIF / CIF. */
  ownerTaxId: '[COMPLETAR: NIF/CIF]',
  /** Domicilio postal completo. */
  ownerAddress: '[COMPLETAR: domicilio postal]',
  /** Correo para ejercer derechos y contactar. */
  contactEmail: '[COMPLETAR: correo electrónico de contacto]',
  /** Nombre del sitio. */
  siteName: 'Frontenis Ligas y Torneos',
  /** Dirección de la web (sin barra final). */
  siteUrl: '[COMPLETAR: https://tu-dominio]',
  /** Fecha de última actualización de los textos legales. */
  updatedAt: '13 de octubre de 2026',
}

export const isPlaceholder = (v: string) => v.includes('[COMPLETAR')
