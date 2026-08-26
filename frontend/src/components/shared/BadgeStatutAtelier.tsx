import Badge from './Badge'
import type { CouleurBadge } from './Badge'

var LIBELLE_STATUT_ATELIER: { [key: string]: string } = { Brouillon: 'Brouillon', EnCours: 'En cours', Validee: 'Validee' }
export var COULEUR_STATUT_ATELIER: { [key: string]: CouleurBadge } = { Brouillon: 'steel', EnCours: 'signature', Validee: 'risk-low' }

export default function BadgeStatutAtelier(props: { statut: string }) {
  return (
    <Badge couleur={COULEUR_STATUT_ATELIER[props.statut] || 'steel'}>
      {LIBELLE_STATUT_ATELIER[props.statut] || props.statut}
    </Badge>
  )
}
