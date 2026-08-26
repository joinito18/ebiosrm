export type VarianteBouton = 'primary' | 'secondary' | 'ghost' | 'danger'
export type TailleBouton = 'sm' | 'md'

// Chaines completes en dur par variante/taille (pas de concatenation de
// classes dynamiques -- Tailwind ne genere que ce qu'il trouve tel quel dans
// le source). Remplace ~15 chaines de bouton retapees a la main dans
// AtelierPage.tsx (le meme bouton "rounded-sm bg-signature px-3 py-1.5 ...").
var VARIANTES: { [key in VarianteBouton]: string } = {
  primary: 'bg-signature text-white hover:bg-signature/90',
  secondary: 'border border-paper-line text-ink hover:border-signature hover:text-signature',
  ghost: 'text-signature hover:underline',
  danger: 'border border-paper-line text-ink hover:border-risk-critical hover:text-risk-critical',
}

var TAILLES: { [key in TailleBouton]: string } = {
  sm: 'px-3 py-1.5 text-xs',
  md: 'px-4 py-2 text-xs',
}

export default function Button(props: {
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void
  type?: 'button' | 'submit'
  variante?: VarianteBouton
  taille?: TailleBouton
  disabled?: boolean
  children: React.ReactNode
  className?: string
}) {
  var variante = props.variante || 'secondary'
  var taille = props.taille || 'sm'
  var basePadding = variante === 'ghost' ? 'text-[11px]' : TAILLES[taille] + ' rounded-sm'
  return (
    <button
      type={props.type || 'button'}
      onClick={props.onClick}
      disabled={props.disabled}
      className={
        'inline-flex items-center gap-1.5 font-medium transition duration-200 ease-premium disabled:opacity-50 ' +
        basePadding + ' ' + VARIANTES[variante] + (props.className ? ' ' + props.className : '')
      }
    >
      {props.children}
    </button>
  )
}
