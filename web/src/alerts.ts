import Swal from 'sweetalert2'

const toast = Swal.mixin({
  toast: true, position: 'top-end', showConfirmButton: false, timer: 2800, timerProgressBar: true,
})

// Mientras se muestra algún «Cargando…» (las raquetas) no sale ningún aviso: se guarda y se enseña al terminar.
let loadersShown = 0
let waiting: (() => void) | null = null

/** Lo llaman los avisos de carga al aparecer y al desaparecer. */
export function loaderShown() { loadersShown++ }
export function loaderHidden() {
  loadersShown = Math.max(0, loadersShown - 1)
  if (loadersShown === 0 && waiting) { const show = waiting; waiting = null; show() }
}

/** Aviso breve tras crear, generar o eliminar algo. Si hay una carga en pantalla, espera a que termine. */
export function notify(title: string, icon: 'success' | 'info' = 'success') {
  const show = () => { void toast.fire({ icon, title }) }
  if (loadersShown > 0) waiting = show // solo se conserva el último aviso pendiente
  else show()
}

/** Pide un texto (p. ej. un nuevo nombre). Devuelve null si se cancela. */
export async function promptText(
  title: string, value: string, label: string, validate?: (value: string) => string | null, maxLength = 150,
): Promise<string | null> {
  const r = await Swal.fire({
    title, input: 'text', inputValue: value, inputLabel: label, showCancelButton: true,
    confirmButtonText: 'Guardar', cancelButtonText: 'Cancelar', confirmButtonColor: '#0f766e',
    inputAttributes: { maxlength: String(maxLength) },
    inputValidator: v => (v.trim() ? (validate?.(v) ?? null) : 'El nombre es obligatorio.'),
  })
  return r.isConfirmed ? String(r.value).trim() : null
}

/** Confirmación genérica de una acción (no destructiva). Devuelve true si el usuario acepta. */
export async function confirmAction(title: string, text: string, confirmText = 'Aceptar', cancelText = 'Cancelar'): Promise<boolean> {
  const r = await Swal.fire({
    icon: 'question', title, text, showCancelButton: true,
    confirmButtonText: confirmText, cancelButtonText: cancelText, confirmButtonColor: '#0f766e',
  })
  return r.isConfirmed
}

/** Confirmación previa a cualquier eliminación. Devuelve true si el usuario acepta. */
export async function confirmDelete(title: string, text = 'Esta acción no se puede deshacer.'): Promise<boolean> {
  const r = await Swal.fire({
    icon: 'warning', title, text, showCancelButton: true, focusCancel: true,
    confirmButtonText: 'Sí, eliminar', cancelButtonText: 'Cancelar', confirmButtonColor: '#b91c1c',
  })
  return r.isConfirmed
}
