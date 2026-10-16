import { whatsappShareUrl } from '../reminders'

interface Props {
  /** Texto del recordatorio, ya construido. */
  message: string
  label: string
  /** Texto accesible más descriptivo que la etiqueta visible. */
  ariaLabel?: string
}

/** Abre WhatsApp con el recordatorio escrito para que la persona lo mande al grupo que quiera. */
export default function ShareReminderButton({ message, label, ariaLabel }: Props) {
  return (
    <a className="button secondary share-reminder" href={whatsappShareUrl(message)}
      target="_blank" rel="noopener noreferrer" aria-label={ariaLabel ?? label}>
      {label}
    </a>
  )
}
