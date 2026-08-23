import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { AtelierChainExpanded } from '../components/methodology/AtelierChain'
import type { AtelierNode } from '../components/methodology/AtelierChain'
import { getEtude, listValeursMetier, listEvenementsRedoutes } from '../lib/api'
import type { Etude } from '../lib/api'

export default function Dashboard() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var [etude, setEtude] = useState<Etude | null>(null)
  var [nbValeursMetier, setNbValeursMetier] = useState(0)
  var [nbEvenementsRedoutes, setNbEvenementsRedoutes] = useState(0)
  var [chargement, setChargement] = useState(true)

  useEffect(function () {
    setChargement(true)
    Promise.all([
      getEtude(etudeId),
      listValeursMetier(etudeId),
      listEvenementsRedoutes(etudeId),
    ]).then(function (results) {
      setEtude(results[0])
      setNbValeursMetier(results[1] ? results[1].length : 0)
      setNbEvenementsRedoutes(results[2] ? results[2].length : 0)
    }).finally(function () { setChargement(false) })
  }, [etudeId])

  if (chargement) {
    return <div className="px-10 py-14 text-sm text-steel">Chargement de l etude...</div>
  }

  if (!etude) {
    return <div className="px-10 py-14 text-sm text-risk-critical">Etude introuvable.</div>
  }

  var statutAtelier1: 'done' | 'current' | 'todo' = 'todo'
  if (etude.statut === 'Validee') statutAtelier1 = 'done'
  else if (etude.statut === 'EnCours') statutAtelier1 = 'current'

  var statutAtelier2: 'done' | 'current' | 'todo' = 'todo'
  if (etude.statutAtelier2 === 'Validee') statutAtelier2 = 'done'
  else if (etude.statutAtelier2 === 'EnCours') statutAtelier2 = 'current'

  var statutAtelier3: 'done' | 'current' | 'todo' = 'todo'
  if (etude.statutAtelier3 === 'Validee') statutAtelier3 = 'done'
  else if (etude.statutAtelier3 === 'EnCours') statutAtelier3 = 'current'

  var statutAtelier4: 'done' | 'current' | 'todo' = 'todo'
  if (etude.statutAtelier4 === 'Validee') statutAtelier4 = 'done'
  else if (etude.statutAtelier4 === 'EnCours') statutAtelier4 = 'current'

  var ateliers: AtelierNode[] = [
    { numero: 1, nom: 'Cadrage', objectif: 'Perimetre, valeurs metier, socle de securite', statut: statutAtelier1, progression: statutAtelier1 === 'done' ? 100 : statutAtelier1 === 'current' ? 50 : 0 },
    { numero: 2, nom: 'Sources de risque', objectif: 'Couples source de risque / objectif vise', statut: statutAtelier2, progression: statutAtelier2 === 'done' ? 100 : statutAtelier2 === 'current' ? 50 : 0 },
    { numero: 3, nom: 'Scenarios strategiques', objectif: 'Cartographie ecosysteme et dangerosite', statut: statutAtelier3, progression: statutAtelier3 === 'done' ? 100 : statutAtelier3 === 'current' ? 50 : 0 },
    { numero: 4, nom: 'Scenarios operationnels', objectif: 'Modes operatoires et vraisemblance', statut: statutAtelier4, progression: statutAtelier4 === 'done' ? 100 : statutAtelier4 === 'current' ? 50 : 0 },
    { numero: 5, nom: 'Traitement du risque', objectif: 'Strategie, PACS, risques residuels', statut: 'todo', progression: 0 },
  ]

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <div className="mb-8 border-b border-paper-line pb-8">
        <div className="mb-3 font-mono text-[11px] tracking-wide text-steel">
          REF. {etude.id.slice(0, 8).toUpperCase()} - {etude.versionReferentielId}
        </div>
        <h1 className="font-display text-[34px] leading-tight text-ink">
          {etude.nom}
        </h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-steel">
          {etude.perimetre}
        </p>
      </div>

      <div className="mb-12 flex divide-x divide-paper-line border-y border-paper-line">
        <div className="flex-1 px-6 py-5 first:pl-0">
          <div className="font-display text-[28px] leading-none text-ink">{etude.statut}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">STATUT</div>
        </div>
        <div className="flex-1 px-6 py-5">
          <div className="font-display text-[28px] leading-none text-ink">{nbValeursMetier}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">VALEURS METIER</div>
        </div>
        <div className="flex-1 px-6 py-5 last:pr-0">
          <div className="font-display text-[28px] leading-none text-ink">{nbEvenementsRedoutes}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">EVENEMENTS REDOUTES</div>
        </div>
      </div>

      <section>
        <h2 className="mb-6 font-mono text-[11px] tracking-wide text-steel-light">
          PARCOURS METHODOLOGIQUE
        </h2>
        <AtelierChainExpanded ateliers={ateliers} etudeId={etudeId} />
      </section>

      <section className="mt-12 border border-paper-line px-5 py-6">
        <p className="text-xs text-steel">
          La matrice des risques et le journal d activite apparaitront ici une fois les
          Ateliers 2 a 5 (sources de risque, scenarios, traitement) implementes. Aucune
          donnee fictive n est affichee tant que le backend correspondant n existe pas.
        </p>
      </section>
    </div>
  )
}
