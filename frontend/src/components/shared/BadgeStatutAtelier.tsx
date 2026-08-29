import Badge from './Badge'
import type { CouleurBadge } from './Badge'
import { libelle } from '../../lib/libelles'

export var COULEUR_STATUT_ATELIER: { [key: string]: CouleurBadge } = { Brouillon: 'steel', EnCours: 'signature', Validee: 'risk-low' }

export default function BadgeStatutAtelier(props: { statut: string }) {
  return (
    <Badge couleur={COULEUR_STATUT_ATELIER[props.statut] || 'steel'}>
      {libelle('statutAtelier', props.statut)}
    </Badge>
  )
}
