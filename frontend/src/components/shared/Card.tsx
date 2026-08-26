// variant="flat" : traitement courant (bordure simple), pour du contenu qui
// fait partie du flux normal de la page.
// variant="elevated" : ombre + rayon plus genereux, reserve aux elements qui
// doivent vraiment se detacher (carte de scenario de risque, mesure
// d'ecosysteme, panneau de creation) -- pas d'usage generalise, sinon
// l'elevation ne veut plus rien dire.
export default function Card(props: { variant?: 'flat' | 'elevated'; className?: string; children: React.ReactNode }) {
  var variant = props.variant || 'flat'
  var base = variant === 'elevated'
    ? 'rounded-md border border-paper-line bg-white shadow-card transition duration-200 ease-premium hover:shadow-card-hover'
    : 'border border-paper-line'
  return (
    <div className={base + (props.className ? ' ' + props.className : '')}>
      {props.children}
    </div>
  )
}
