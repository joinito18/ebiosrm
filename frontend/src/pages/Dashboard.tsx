import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { AtelierChainExpanded } from '../components/methodology/AtelierChain'
import type { AtelierNode } from '../components/methodology/AtelierChain'
import BadgeStatutAtelier from '../components/shared/BadgeStatutAtelier'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import Card from '../components/shared/Card'
import RiskMatrix from '../components/dashboard/RiskMatrix'
import {
  getEtude, listValeursMetier, listEvenementsRedoutes, listCouplesSrOv, listScenariosDeRisque,
} from '../lib/api'
import type { Etude, ScenarioDeRisque } from '../lib/api'

export default function Dashboard() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var [etude, setEtude] = useState<Etude | null>(null)
  var [nbValeursMetier, setNbValeursMetier] = useState(0)
  var [nbEvenementsRedoutes, setNbEvenementsRedoutes] = useState(0)
  var [nbCouplesRetenus, setNbCouplesRetenus] = useState(0)
  var [scenariosDeRisque, setScenariosDeRisque] = useState<ScenarioDeRisque[]>([])
  var [chargement, setChargement] = useState(true)

  useEffect(function () {
    setChargement(true)
    Promise.all([
      getEtude(etudeId),
      listValeursMetier(etudeId),
      listEvenementsRedoutes(etudeId),
      listCouplesSrOv(etudeId),
      listScenariosDeRisque(etudeId),
    ]).then(function (results) {
      setEtude(results[0])
      setNbValeursMetier(results[1] ? results[1].length : 0)
      setNbEvenementsRedoutes(results[2] ? results[2].length : 0)
      var couples = results[3] || []
      setNbCouplesRetenus(couples.filter(function (c) { return c.pertinence === 'TresPertinent' || c.pertinence === 'PlutotPertinent' }).length)
      setScenariosDeRisque(results[4] || [])
    }).finally(function () { setChargement(false) })
  }, [etudeId])

  if (chargement) {
    return <div className="px-6 py-10 text-sm lg:px-10 lg:py-14 text-steel">Chargement de l etude...</div>
  }

  if (!etude) {
    return <div className="px-6 py-10 text-sm lg:px-10 lg:py-14 text-risk-critical">Etude introuvable.</div>
  }

  function statutDe(s: string): 'done' | 'current' | 'todo' {
    if (s === 'Validee') return 'done'
    if (s === 'EnCours') return 'current'
    return 'todo'
  }

  var statutAtelier1 = statutDe(etude.statut)
  var statutAtelier2 = statutDe(etude.statutAtelier2)
  var statutAtelier3 = statutDe(etude.statutAtelier3)
  var statutAtelier4 = statutDe(etude.statutAtelier4)
  var statutAtelier5 = statutDe(etude.statutAtelier5)

  function progressionDe(s: 'done' | 'current' | 'todo'): number {
    return s === 'done' ? 100 : s === 'current' ? 50 : 0
  }

  var ateliers: AtelierNode[] = [
    { numero: 1, nom: 'Cadrage', objectif: 'Perimetre, valeurs metier, socle de securite', statut: statutAtelier1, progression: progressionDe(statutAtelier1) },
    { numero: 2, nom: 'Sources de risque', objectif: 'Couples source de risque / objectif vise', statut: statutAtelier2, progression: progressionDe(statutAtelier2) },
    { numero: 3, nom: 'Scenarios strategiques', objectif: 'Cartographie ecosysteme et dangerosite', statut: statutAtelier3, progression: progressionDe(statutAtelier3) },
    { numero: 4, nom: 'Scenarios operationnels', objectif: 'Modes operatoires et vraisemblance', statut: statutAtelier4, progression: progressionDe(statutAtelier4) },
    { numero: 5, nom: 'Traitement du risque', objectif: 'Plan de traitement et risques residuels', statut: statutAtelier5, progression: progressionDe(statutAtelier5) },
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

      <div className="mb-12 grid grid-cols-1 gap-px border-y border-paper-line bg-paper-line sm:grid-cols-3 lg:grid-cols-5">
        <div className="bg-paper px-6 py-5">
          <div className="mt-1"><BadgeStatutAtelier statut={etude.statut} /></div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">STATUT ATELIER 1</div>
        </div>
        <div className="bg-paper px-6 py-5">
          <div className="font-display text-[28px] leading-none text-ink">{nbValeursMetier}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">VALEURS METIER</div>
        </div>
        <div className="bg-paper px-6 py-5">
          <div className="font-display text-[28px] leading-none text-ink">{nbEvenementsRedoutes}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">EVENEMENTS REDOUTES</div>
        </div>
        <div className="bg-paper px-6 py-5">
          <div className="font-display text-[28px] leading-none text-ink">{nbCouplesRetenus}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">COUPLES SR/OV RETENUS</div>
        </div>
        <div className="bg-paper px-6 py-5">
          <div className="font-display text-[28px] leading-none text-ink">{scenariosDeRisque.length}</div>
          <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">SCENARIOS DE RISQUE</div>
        </div>
      </div>

      <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
        <section>
          <h2 className="mb-6 font-mono text-[11px] tracking-wide text-steel-light">
            PARCOURS METHODOLOGIQUE
          </h2>
          <AtelierChainExpanded ateliers={ateliers} etudeId={etudeId} />

          {statutAtelier5 === 'done' && (
            <Card variant="elevated" className="mt-8 px-5 py-6">
              <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-xs text-steel">Les 5 ateliers sont valides. La synthese globale consolide les points cles pour presentation a la Direction.</p>
                <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/synthese'} nomFichier={'synthese-' + etudeId + '.pdf'} className="shrink-0 rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white transition duration-200 ease-premium hover:bg-signature/90">Telecharger la synthese</BoutonTelechargerRapport>
              </div>
            </Card>
          )}
        </section>

        {scenariosDeRisque.length > 0 && (
          <section>
            <h2 className="mb-6 font-mono text-[11px] tracking-wide text-steel-light">
              CARTOGRAPHIE DES RISQUES
            </h2>
            <Card variant="elevated" className="p-5">
              <RiskMatrix scenarios={scenariosDeRisque} />
            </Card>
          </section>
        )}
      </div>
    </div>
  )
}
