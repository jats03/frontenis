import Swal from 'sweetalert2'

const toast = Swal.mixin({
  toast: true, position: 'top-end', showConfirmButton: false, timer: 2800, timerProgressBar: true,
})

/** Aviso breve tras crear, generar o eliminar algo. */
export function notify(title: string, icon: 'success' | 'info' = 'success') {
  void toast.fire({ icon, title })
}

/** Pide un texto (p. ej. un nuevo nombre). Devuelve null si se cancela. */
export async function promptText(title: string, value: string, label: string): Promise<string | null> {
  const r = await Swal.fire({
    title, input: 'text', inputValue: value, inputLabel: label, showCancelButton: true,
    confirmButtonText: 'Guardar', cancelButtonText: 'Cancelar', confirmButtonColor: '#0f766e',
    inputAttributes: { maxlength: '150' },
    inputValidator: v => (v.trim() ? null : 'El nombre es obligatorio.'),
  })
  return r.isConfirmed ? String(r.value).trim() : null
}

/** Confirmación previa a cualquier eliminación. Devuelve true si el usuario acepta. */
export async function confirmDelete(title: string, text = 'Esta acción no se puede deshacer.'): Promise<boolean> {
  const r = await Swal.fire({
    icon: 'warning', title, text, showCancelButton: true, focusCancel: true,
    confirmButtonText: 'Sí, eliminar', cancelButtonText: 'Cancelar', confirmButtonColor: '#b91c1c',
  })
  return r.isConfirmed
}
