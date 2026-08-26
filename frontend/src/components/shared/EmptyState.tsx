import { Inbox } from 'lucide-react'

// Remplace les ~10 etats vides identiques ("<p>Aucun ... renseigne.</p>",
// juste du texte gris) par un traitement un peu plus considere -- reste
// sobre (pas d'illustration, pas de gros pictogramme centre) pour ne pas
// peser dans une page qui contient deja beaucoup de sections.
export default function EmptyState(props: { message: string; icon?: React.ReactNode; action?: React.ReactNode }) {
  return (
    <div className="flex items-center gap-2.5 border border-dashed border-paper-line px-4 py-3 text-xs text-steel">
      <span className="shrink-0 text-steel-light">{props.icon || <Inbox size={14} strokeWidth={1.75} />}</span>
      <span className="flex-1">{props.message}</span>
      {props.action}
    </div>
  )
}
