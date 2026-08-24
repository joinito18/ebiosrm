var LIBELLE_STATUT_ATELIER: { [key: string]: string } = { Brouillon: 'Brouillon', EnCours: 'En cours', Validee: 'Validee' }

export default function BadgeStatutAtelier(props: { statut: string }) {
  var style = props.statut === 'Validee'
    ? 'border-risk-low/30 bg-risk-low/10 text-risk-low'
    : props.statut === 'EnCours'
      ? 'border-signature/30 bg-signature/10 text-signature'
      : 'border-paper-line bg-white text-steel'
  return <span className={'rounded-sm border px-2 py-0.5 font-mono text-[10px] font-medium tracking-wide ' + style}>{LIBELLE_STATUT_ATELIER[props.statut] || props.statut}</span>
}
