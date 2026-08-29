import { useLectureSeule } from '../../lib/lectureSeule'

// Paire Modifier/Suppr., dupliquee 26 fois a la main dans AtelierPage.tsx.
// Reste volontairement du texte (pas des boutons pleins) : ce sont des
// actions secondaires sur une ligne deja identifiee visuellement, pas des
// appels a l'action.
export default function RowActions(props: { onModifier?: () => void; onSupprimer: () => void; labelModifier?: string; labelSupprimer?: string }) {
  if (useLectureSeule()) return null
  return (
    <div className="flex shrink-0 items-center gap-3">
      {props.onModifier && (
        <button onClick={props.onModifier} className="text-[11px] text-steel-light hover:text-signature">
          {props.labelModifier || 'Modifier'}
        </button>
      )}
      <button onClick={props.onSupprimer} className="text-[11px] text-steel-light hover:text-risk-critical">
        {props.labelSupprimer || 'Suppr.'}
      </button>
    </div>
  )
}
