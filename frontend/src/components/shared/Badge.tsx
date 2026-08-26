export type CouleurBadge = 'signature' | 'risk-critical' | 'risk-high' | 'risk-moderate' | 'risk-low' | 'steel'

// Formule unique pour tout ce qui encode une severite/zone/classe en couleur
// dans l'app (bordure + fond a 10% d'opacite) -- remplace les 6+ fonctions
// couleurX qui rendaient juste du texte colore brut (gravite, vraisemblance,
// niveau de risque, zone de dangerosite, classe d'acceptation, pertinence).
// Chaines completes en dur (pas de concatenation) : Tailwind ne genere une
// classe que s'il la trouve telle quelle, en clair, dans le code source.
var STYLES: { [key in CouleurBadge]: string } = {
  'signature': 'border-signature/30 bg-signature/10 text-signature',
  'risk-critical': 'border-risk-critical/30 bg-risk-critical/10 text-risk-critical',
  'risk-high': 'border-risk-high/30 bg-risk-high/10 text-risk-high',
  'risk-moderate': 'border-risk-moderate/30 bg-risk-moderate/10 text-risk-moderate',
  'risk-low': 'border-risk-low/30 bg-risk-low/10 text-risk-low',
  'steel': 'border-paper-line bg-white text-steel',
}

export default function Badge(props: { couleur: CouleurBadge; children: React.ReactNode; taille?: 'sm' | 'md' }) {
  var taille = props.taille || 'sm'
  var classesTaille = taille === 'md' ? 'px-2.5 py-1 text-[11px]' : 'px-2 py-0.5 text-[10px]'
  return (
    <span className={classesTaille + ' inline-flex shrink-0 items-center whitespace-nowrap rounded-sm border font-mono font-medium tracking-wide ' + STYLES[props.couleur]}>
      {props.children}
    </span>
  )
}
