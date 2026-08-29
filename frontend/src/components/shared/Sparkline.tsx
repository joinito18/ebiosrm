/** Mini-courbe SVG de l'historique d'un indicateur (pas d'axes, pas de lib). */
export default function Sparkline(props: {
  valeurs: number[]
  cible?: number | null
  couleur?: string
  largeur?: number
  hauteur?: number
}) {
  var w = props.largeur || 160
  var h = props.hauteur || 36
  var couleur = props.couleur || '#000091'

  if (props.valeurs.length === 0) return <span className="text-[10px] text-steel-light">aucune donnee</span>
  if (props.valeurs.length === 1) {
    return (
      <svg width={w} height={h} className="overflow-visible">
        <circle cx={w / 2} cy={h / 2} r={3} fill={couleur} />
      </svg>
    )
  }

  var toutes = props.cible != null ? props.valeurs.concat([props.cible]) : props.valeurs
  var min = Math.min.apply(null, toutes)
  var max = Math.max.apply(null, toutes)
  var etendue = max - min || 1
  var pad = 4

  function x(i: number) { return pad + (i / (props.valeurs.length - 1)) * (w - 2 * pad) }
  function y(v: number) { return h - pad - ((v - min) / etendue) * (h - 2 * pad) }

  var points = props.valeurs.map(function (v, i) { return x(i) + ',' + y(v) }).join(' ')

  return (
    <svg width={w} height={h} className="overflow-visible">
      {props.cible != null && (
        <line x1={pad} y1={y(props.cible)} x2={w - pad} y2={y(props.cible)} stroke="#DDDDDD" strokeWidth={1} strokeDasharray="3 3" />
      )}
      <polyline points={points} fill="none" stroke={couleur} strokeWidth={1.5} />
      <circle cx={x(props.valeurs.length - 1)} cy={y(props.valeurs[props.valeurs.length - 1])} r={2.5} fill={couleur} />
    </svg>
  )
}
