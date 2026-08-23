import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import InlineForm from '../components/shared/InlineForm'
import GrilleMatrice from '../components/shared/GrilleMatrice'
import OverrideJugementExpert from '../components/shared/OverrideJugementExpert'
import { MATRICE_VRAISEMBLANCE, MATRICE_PERTINENCE, MATRICE_RISQUE, calculerNiveauDangerosite, determinerZoneDangerosite } from '../lib/calculsEbios'
import {
  getEtude, listValeursMetier, listBiensSupport, listEvenementsRedoutes, getSocleSecurite,
  demarrerAtelier1, validerAtelier1, rouvrirAtelier1, rapportAtelier1Url,
  demarrerAtelier2, validerAtelier2, rouvrirAtelier2, rapportAtelier2Url,
  demarrerAtelier3, validerAtelier3, rouvrirAtelier3, rapportAtelier3Url,
  demarrerAtelier4, validerAtelier4, rouvrirAtelier4, rapportAtelier4Url,
  createValeurMetier, updateValeurMetier, deleteValeurMetier,
  createBienSupport, updateBienSupport, deleteBienSupport,
  createEvenementRedoute, updateEvenementRedoute, deleteEvenementRedoute,
  createSocleSecurite, addReferentiel, updateReferentiel, deleteReferentiel,
  listCouplesSrOv, createCoupleSrOv, updateCoupleSrOv, deleteCoupleSrOv, definirPertinenceRetenue, reinitialiserPertinence,
  listPartiesPrenantes, createPartiePrenante, updatePartiePrenante, deletePartiePrenante, evaluerDangerosite,
  evaluerDangerositeResiduelle, ajouterMesureEcosysteme, supprimerMesureEcosysteme,
  definirDangerositeRetenue, reinitialiserDangerosite, definirDangerositeResidueleRetenue, reinitialiserDangerositeResiduelle,
  listScenariosStrategiques, createScenarioStrategique, updateScenarioStrategique, deleteScenarioStrategique,
  listCheminsAttaque, createCheminAttaque, updateCheminAttaque, deleteCheminAttaque,
  createEvenementIntermediaire, updateEvenementIntermediaire, deleteEvenementIntermediaire,
  listScenariosOperationnels, createScenarioOperationnel, deleteScenarioOperationnel,
  ajouterModeOperatoire, modifierModeOperatoire, supprimerModeOperatoire,
  definirVraisemblanceRetenue, reinitialiserVraisemblance,
  demarrerAtelier5, validerAtelier5, rouvrirAtelier5, rapportAtelier5Url, rapportSyntheseUrl,
  listScenariosDeRisque, creerScenarioDeRisque, supprimerScenarioDeRisque,
  definirNiveauRisqueInitialRetenue, reinitialiserNiveauRisqueInitial,
  evaluerRisqueResiduel, definirNiveauRisqueResiduelRetenue, reinitialiserNiveauRisqueResiduel,
  accepterRisqueResiduel, retirerAcceptation,
  getPlanTraitementRisque, creerPlanTraitementRisque,
  ajouterMesureTraitementRisque, modifierMesureTraitementRisque, supprimerMesureTraitementRisque,
  ApiError,
} from '../lib/api'
import type {
  Etude, ValeurMetier, BienSupport, EvenementRedoute, SocleSecurite, CoupleSourceRisqueObjectifVise, PartiePrenante,
  ScenarioStrategique, CheminAttaque, ScenarioOperationnel, ModeOperatoire, ModeOperatoireInput, ActionElementaireInput,
  ScenarioDeRisque, PlanTraitementRisque, MesureTraitementRisque, MesureTraitementRisqueInput,
} from '../lib/api'
import { PHASES_ACTION_ELEMENTAIRE } from '../lib/api'
import { CATALOGUE_ISO_27001, THEMES_ISO } from '../lib/iso27001'
import type { ControleIso } from '../lib/iso27001'

var NOMS_ATELIERS: { [key: number]: string } = {
  1: 'Cadrage',
  2: 'Sources de risque',
  3: 'Scenarios strategiques',
  4: 'Scenarios operationnels',
  5: 'Traitement du risque',
}

var TYPES_BIEN_SUPPORT = ['SystemeInformation', 'Reseau', 'RessourcesHumaines', 'Local']
var ETATS_CONFORMITE = ['Conforme', 'NonConforme', 'NonApplicable']

export default function AtelierPage() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var numero = Number(params.numero)

  var [etude, setEtude] = useState<Etude | null>(null)
  var [valeurs, setValeurs] = useState<ValeurMetier[]>([])
  var [biens, setBiens] = useState<BienSupport[]>([])
  var [evenements, setEvenements] = useState<EvenementRedoute[]>([])
  var [socle, setSocle] = useState<SocleSecurite | null>(null)
  var [couples, setCouples] = useState<CoupleSourceRisqueObjectifVise[]>([])
  var [parties, setParties] = useState<PartiePrenante[]>([])
  var [scenarios, setScenarios] = useState<ScenarioStrategique[]>([])
  var [cheminsAttaque, setCheminsAttaque] = useState<CheminAttaque[]>([])
  var [scenariosOperationnels, setScenariosOperationnels] = useState<ScenarioOperationnel[]>([])
  var [scenariosDeRisque, setScenariosDeRisque] = useState<ScenarioDeRisque[]>([])
  var [planTraitementRisque, setPlanTraitementRisque] = useState<PlanTraitementRisque | null>(null)
  var [chargement, setChargement] = useState(true)
  var [action, setAction] = useState('')
  var [messageErreur, setMessageErreur] = useState('')

  function charger() {
    setChargement(true)
    var numeroActuel = numero
    getEtude(etudeId).then(function (e) {
      setEtude(e)
      if (numeroActuel === 1) {
        return Promise.all([
          listValeursMetier(etudeId), listBiensSupport(etudeId),
          listEvenementsRedoutes(etudeId), getSocleSecurite(etudeId),
        ]).then(function (r) {
          setValeurs(r[0] || []); setBiens(r[1] || [])
          setEvenements(r[2] || []); setSocle(r[3])
        })
      }
      if (numeroActuel === 2) {
        return Promise.all([
          listCouplesSrOv(etudeId), listPartiesPrenantes(etudeId),
        ]).then(function (r) {
          setCouples(r[0] || []); setParties(r[1] || [])
        })
      }
      if (numeroActuel === 3) {
        return Promise.all([
          listPartiesPrenantes(etudeId), listCouplesSrOv(etudeId), listScenariosStrategiques(etudeId),
          listEvenementsRedoutes(etudeId), listValeursMetier(etudeId), listCheminsAttaque(etudeId),
        ]).then(function (r) {
          setParties(r[0] || []); setCouples(r[1] || []); setScenarios(r[2] || [])
          setEvenements(r[3] || []); setValeurs(r[4] || []); setCheminsAttaque(r[5] || [])
        })
      }
      if (numeroActuel === 4) {
        return Promise.all([
          listScenariosStrategiques(etudeId), listCouplesSrOv(etudeId), listCheminsAttaque(etudeId), listScenariosOperationnels(etudeId),
          listBiensSupport(etudeId),
        ]).then(function (r) {
          setScenarios(r[0] || []); setCouples(r[1] || []); setCheminsAttaque(r[2] || []); setScenariosOperationnels(r[3] || [])
          setBiens(r[4] || [])
        })
      }
      if (numeroActuel === 5) {
        return Promise.all([
          listScenariosStrategiques(etudeId), listCouplesSrOv(etudeId), listCheminsAttaque(etudeId), listScenariosOperationnels(etudeId),
          listScenariosDeRisque(etudeId), getPlanTraitementRisque(etudeId),
        ]).then(function (r) {
          setScenarios(r[0] || []); setCouples(r[1] || []); setCheminsAttaque(r[2] || []); setScenariosOperationnels(r[3] || [])
          setScenariosDeRisque(r[4] || []); setPlanTraitementRisque(r[5])
        })
      }
    }).finally(function () { setChargement(false) })
  }

  useEffect(function () { charger() }, [etudeId, numero])

  function handleDemarrer() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier1(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleValider() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier1(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleRouvrir() {
    if (!window.confirm('Rouvrir l atelier 1 ? Le rapport PDF deja genere restera consultable comme version figee, mais ne reflete plus l etat courant tant que l atelier n est pas revalide.')) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier1(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier2() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier2() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier2() {
    if (!window.confirm('Rouvrir l atelier 2 ?')) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier3() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier3() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier3() {
    if (!window.confirm('Rouvrir l atelier 3 ?')) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier4() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier4() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier4() {
    if (!window.confirm('Rouvrir l atelier 4 ?')) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier5() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier5(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier5() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier5(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier5() {
    if (!window.confirm('Rouvrir l atelier 5 ?')) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier5(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : 'Erreur.')
    }).finally(function () { setAction('') })
  }

  if (chargement) {
    return <div className="px-10 py-14 text-sm text-steel">Chargement...</div>
  }

  if (!etude) {
    return <div className="px-10 py-14 text-sm text-risk-critical">Etude introuvable.</div>
  }

  var nom = NOMS_ATELIERS[numero] || 'Atelier'
  var estAtelier1 = numero === 1
  var estAtelier2 = numero === 2
  var estAtelier3 = numero === 3
  var estAtelier4 = numero === 4
  var estAtelier5 = numero === 5
  var estVerrouille = !estAtelier1 && !estAtelier2 && !estAtelier3 && !estAtelier4 && !estAtelier5
  var lienRapport = rapportAtelier1Url(etudeId)
  var lienRapportAtelier2 = rapportAtelier2Url(etudeId)
  var lienRapportAtelier3 = rapportAtelier3Url(etudeId)
  var lienRapportAtelier4 = rapportAtelier4Url(etudeId)
  var lienRapportAtelier5 = rapportAtelier5Url(etudeId)
  var lienRapportSynthese = rapportSyntheseUrl(etudeId)
  var lienRetour = '/etudes/' + etudeId

  var boutonAction = null
  if (estAtelier1 && etude.statut === 'Brouillon') {
    boutonAction = <button onClick={handleDemarrer} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'demarrage' ? 'Demarrage...' : 'Demarrer l atelier'}</button>
  } else if (estAtelier1 && etude.statut === 'EnCours') {
    boutonAction = <button onClick={handleValider} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'validation' ? 'Validation...' : 'Valider l atelier'}</button>
  } else if (estAtelier1 && etude.statut === 'Validee') {
    boutonAction = (
      <>
        <button onClick={handleRouvrir} disabled={action !== ''} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-risk-high hover:text-risk-high disabled:opacity-50">{action === 'reouverture' ? 'Reouverture...' : 'Rouvrir l atelier'}</button>
        <a href={lienRapport} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger le rapport PDF</a>
      </>
    )
  }

  var boutonActionAtelier2 = null
  if (etude.statutAtelier2 === 'Brouillon') {
    boutonActionAtelier2 = <button onClick={handleDemarrerAtelier2} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'demarrage' ? 'Demarrage...' : 'Demarrer l atelier'}</button>
  } else if (etude.statutAtelier2 === 'EnCours') {
    boutonActionAtelier2 = <button onClick={handleValiderAtelier2} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'validation' ? 'Validation...' : 'Valider l atelier'}</button>
  } else if (etude.statutAtelier2 === 'Validee') {
    boutonActionAtelier2 = (
      <>
        <button onClick={handleRouvrirAtelier2} disabled={action !== ''} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-risk-high hover:text-risk-high disabled:opacity-50">{action === 'reouverture' ? 'Reouverture...' : 'Rouvrir l atelier'}</button>
        <a href={lienRapportAtelier2} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger le rapport PDF</a>
      </>
    )
  }

  var boutonActionAtelier3 = null
  if (etude.statutAtelier3 === 'Brouillon') {
    boutonActionAtelier3 = <button onClick={handleDemarrerAtelier3} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'demarrage' ? 'Demarrage...' : 'Demarrer l atelier'}</button>
  } else if (etude.statutAtelier3 === 'EnCours') {
    boutonActionAtelier3 = <button onClick={handleValiderAtelier3} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'validation' ? 'Validation...' : 'Valider l atelier'}</button>
  } else if (etude.statutAtelier3 === 'Validee') {
    boutonActionAtelier3 = (
      <>
        <button onClick={handleRouvrirAtelier3} disabled={action !== ''} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-risk-high hover:text-risk-high disabled:opacity-50">{action === 'reouverture' ? 'Reouverture...' : 'Rouvrir l atelier'}</button>
        <a href={lienRapportAtelier3} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger le rapport PDF</a>
      </>
    )
  }

  var boutonActionAtelier4 = null
  if (etude.statutAtelier4 === 'Brouillon') {
    boutonActionAtelier4 = <button onClick={handleDemarrerAtelier4} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'demarrage' ? 'Demarrage...' : 'Demarrer l atelier'}</button>
  } else if (etude.statutAtelier4 === 'EnCours') {
    boutonActionAtelier4 = <button onClick={handleValiderAtelier4} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'validation' ? 'Validation...' : 'Valider l atelier'}</button>
  } else if (etude.statutAtelier4 === 'Validee') {
    boutonActionAtelier4 = (
      <>
        <button onClick={handleRouvrirAtelier4} disabled={action !== ''} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-risk-high hover:text-risk-high disabled:opacity-50">{action === 'reouverture' ? 'Reouverture...' : 'Rouvrir l atelier'}</button>
        <a href={lienRapportAtelier4} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger le rapport PDF</a>
      </>
    )
  }

  var boutonActionAtelier5 = null
  if (etude.statutAtelier5 === 'Brouillon') {
    boutonActionAtelier5 = <button onClick={handleDemarrerAtelier5} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'demarrage' ? 'Demarrage...' : 'Demarrer l atelier'}</button>
  } else if (etude.statutAtelier5 === 'EnCours') {
    boutonActionAtelier5 = <button onClick={handleValiderAtelier5} disabled={action !== ''} className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">{action === 'validation' ? 'Validation...' : 'Valider l atelier'}</button>
  } else if (etude.statutAtelier5 === 'Validee') {
    boutonActionAtelier5 = (
      <>
        <button onClick={handleRouvrirAtelier5} disabled={action !== ''} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-risk-high hover:text-risk-high disabled:opacity-50">{action === 'reouverture' ? 'Reouverture...' : 'Rouvrir l atelier'}</button>
        <a href={lienRapportAtelier5} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger le rapport PDF</a>
        <a href={lienRapportSynthese} className="rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition hover:border-signature hover:text-signature">Telecharger la synthese globale</a>
      </>
    )
  }

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader eyebrow={'ATELIER ' + (numero < 10 ? '0' + numero : numero) + ' / 05 -- ' + etude.nom} titre={nom} />

      {messageErreur && <div className="mb-6 border border-risk-critical/30 bg-risk-critical/5 px-5 py-3 text-xs text-risk-critical">{messageErreur}</div>}

      {estVerrouille && (
        <div className="mb-10 border border-paper-line bg-paper-dim px-5 py-4">
          <p className="text-xs text-steel">Cet atelier n est pas encore implemente cote backend (Slice non commence). Aucune donnee reelle a afficher pour l instant.</p>
        </div>
      )}

      {estAtelier1 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <div className="font-mono text-[11px] text-steel">STATUT ACTUEL : <span className="font-medium text-ink">{etude.statut}</span></div>
            <div className="flex gap-2">{boutonAction}</div>
          </div>

          <ValeursMetierSection etudeId={etudeId} valeurs={valeurs} onChange={charger} />
          <BiensSupportSection etudeId={etudeId} valeurs={valeurs} biens={biens} onChange={charger} />
          <EvenementsRedoutesSection etudeId={etudeId} valeurs={valeurs} evenements={evenements} onChange={charger} />
          <SocleSection etudeId={etudeId} socle={socle} onChange={charger} />
        </div>
      )}

      {estAtelier2 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <div className="font-mono text-[11px] text-steel">STATUT ATELIER 2 : <span className="font-medium text-ink">{etude.statutAtelier2}</span></div>
            <div className="flex gap-2">{boutonActionAtelier2}</div>
          </div>

          <PartiesPrenantesSection etudeId={etudeId} parties={parties} onChange={charger} />
          <CouplesSrOvSection etudeId={etudeId} couples={couples} onChange={charger} />
        </div>
      )}

      {estAtelier3 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <div className="font-mono text-[11px] text-steel">STATUT ATELIER 3 : <span className="font-medium text-ink">{etude.statutAtelier3}</span></div>
            <div className="flex gap-2">{boutonActionAtelier3}</div>
          </div>
          <EvaluationDangerositeSection etudeId={etudeId} parties={parties} onChange={charger} />
          <MesuresEcosystemeSection etudeId={etudeId} parties={parties} onChange={charger} />
          <ScenariosStrategiquesSection etudeId={etudeId} couples={couples} scenarios={scenarios} evenements={evenements} valeurs={valeurs} onChange={charger} />
          <CheminsAttaqueSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} parties={parties} onChange={charger} />
        </div>
      )}

      {estAtelier4 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <div className="font-mono text-[11px] text-steel">STATUT ATELIER 4 : <span className="font-medium text-ink">{etude.statutAtelier4}</span></div>
            <div className="flex gap-2">{boutonActionAtelier4}</div>
          </div>
          <ScenariosOperationnelsSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} scenariosOperationnels={scenariosOperationnels} biens={biens} onChange={charger} />
        </div>
      )}

      {estAtelier5 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <div className="font-mono text-[11px] text-steel">STATUT ATELIER 5 : <span className="font-medium text-ink">{etude.statutAtelier5}</span></div>
            <div className="flex gap-2">{boutonActionAtelier5}</div>
          </div>
          <ScenariosDeRisqueSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} scenariosOperationnels={scenariosOperationnels} scenariosDeRisque={scenariosDeRisque} onChange={charger} />
          <PlanTraitementRisqueSection etudeId={etudeId} plan={planTraitementRisque} scenariosDeRisque={scenariosDeRisque} onChange={charger} />
        </div>
      )}

      <div className="mt-14 border-t border-paper-line pt-6">
        <Link to={lienRetour} className="font-mono text-[11px] text-steel hover:text-signature">Retour au dossier de l etude</Link>
      </div>
    </div>
  )
}

function ValeursMetierSection(props: { etudeId: string; valeurs: ValeurMetier[]; onChange: () => void }) {
  var [description, setDescription] = useState('')
  var [entite, setEntite] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)
  var [idEnEdition, setIdEnEdition] = useState('')
  var [descEdit, setDescEdit] = useState('')
  var [entiteEdit, setEntiteEdit] = useState('')

  function soumettre(fermer: () => void) {
    if (!description.trim() || !entite.trim()) {
      setErreur('Description et entite proprietaire obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createValeurMetier(props.etudeId, description, entite)
      .then(function () {
        setDescription('')
        setEntite('')
        fermer()
        props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function ouvrirEdition(v: ValeurMetier) {
    setIdEnEdition(v.id)
    setDescEdit(v.description)
    setEntiteEdit(v.entiteProprietaire)
  }

  function sauvegarderEdition(id: string) {
    if (!descEdit.trim() || !entiteEdit.trim()) return
    updateValeurMetier(props.etudeId, id, descEdit, entiteEdit)
      .then(function () { setIdEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(id: string) {
    if (!window.confirm('Supprimer cette valeur metier ?')) return
    deleteValeurMetier(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">VALEURS METIER ({props.valeurs.length})</h2>
      {props.valeurs.length === 0 ? (
        <p className="text-xs text-steel">Aucune valeur metier renseignee.</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.valeurs.map(function (v) {
            if (idEnEdition === v.id) {
              return (
                <div key={v.id} className="flex items-center gap-2 py-2">
                  <input type="text" value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <input type="text" value={entiteEdit} onChange={function (e) { setEntiteEdit(e.target.value) }} className="w-40 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEdition(v.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                </div>
              )
            }
            return (
              <div key={v.id} className="flex items-center justify-between py-3">
                <span className="text-sm text-ink">{v.description}</span>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-[11px] text-steel-light">{v.entiteProprietaire}</span>
                  <button onClick={function () { ouvrirEdition(v) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                  <button onClick={function () { supprimer(v.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label="Ajouter une valeur metier">
        {function (fermer) {
          return (
            <div>
              <input type="text" placeholder="Description" value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="Entite proprietaire" value={entite} onChange={function (e) { setEntite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}

function BiensSupportSection(props: { etudeId: string; valeurs: ValeurMetier[]; biens: BienSupport[]; onChange: () => void }) {
  var [valeurMetierId, setValeurMetierId] = useState('')
  var [description, setDescription] = useState('')
  var [type, setType] = useState(TYPES_BIEN_SUPPORT[0])
  var [entite, setEntite] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(fermer: () => void) {
    if (!valeurMetierId || !description.trim() || !entite.trim()) {
      setErreur('Valeur metier, description et entite proprietaire obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createBienSupport(props.etudeId, valeurMetierId, description, type, entite)
      .then(function () {
        setDescription('')
        setEntite('')
        fermer()
        props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  var [idEnEdition, setIdEnEdition] = useState('')
  var [descEdit, setDescEdit] = useState('')
  var [typeEdit, setTypeEdit] = useState(TYPES_BIEN_SUPPORT[0])
  var [entiteEdit, setEntiteEdit] = useState('')

  function ouvrirEdition(b: BienSupport) {
    setIdEnEdition(b.id)
    setDescEdit(b.description)
    setTypeEdit(b.type)
    setEntiteEdit(b.entiteProprietaire)
  }

  function sauvegarderEdition(id: string) {
    if (!descEdit.trim() || !entiteEdit.trim()) return
    updateBienSupport(props.etudeId, id, descEdit, typeEdit, entiteEdit)
      .then(function () { setIdEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(id: string) {
    if (!window.confirm('Supprimer ce bien support ?')) return
    deleteBienSupport(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">BIENS SUPPORT ({props.biens.length})</h2>
      {props.biens.length === 0 ? (
        <p className="text-xs text-steel">Aucun bien support renseigne.</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.biens.map(function (b) {
            if (idEnEdition === b.id) {
              return (
                <div key={b.id} className="flex items-center gap-2 py-2">
                  <input type="text" value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <select value={typeEdit} onChange={function (e) { setTypeEdit(e.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    {TYPES_BIEN_SUPPORT.map(function (t) { return <option key={t} value={t}>{t}</option> })}
                  </select>
                  <input type="text" value={entiteEdit} onChange={function (e) { setEntiteEdit(e.target.value) }} className="w-32 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEdition(b.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                </div>
              )
            }
            return (
              <div key={b.id} className="flex items-center justify-between py-3">
                <span className="text-sm text-ink">{b.description}</span>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-[11px] text-steel-light">{b.type} - {b.entiteProprietaire}</span>
                  <button onClick={function () { ouvrirEdition(b) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                  <button onClick={function () { supprimer(b.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label="Ajouter un bien support">
        {function (fermer) {
          return (
            <div>
              <select value={valeurMetierId} onChange={function (e) { setValeurMetierId(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="">Valeur metier associee</option>
                {props.valeurs.map(function (v) { return <option key={v.id} value={v.id}>{v.description}</option> })}
              </select>
              <input type="text" placeholder="Description" value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={type} onChange={function (e) { setType(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {TYPES_BIEN_SUPPORT.map(function (t) { return <option key={t} value={t}>{t}</option> })}
              </select>
              <input type="text" placeholder="Entite proprietaire" value={entite} onChange={function (e) { setEntite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}

function EvenementsRedoutesSection(props: { etudeId: string; valeurs: ValeurMetier[]; evenements: EvenementRedoute[]; onChange: () => void }) {
  var [valeurMetierId, setValeurMetierId] = useState('')
  var [description, setDescription] = useState('')
  var [gravite, setGravite] = useState('1')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(fermer: () => void) {
    if (!valeurMetierId || !description.trim()) {
      setErreur('Valeur metier et description obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createEvenementRedoute(props.etudeId, valeurMetierId, description, Number(gravite))
      .then(function () {
        setDescription('')
        fermer()
        props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  var [idEnEdition, setIdEnEdition] = useState('')
  var [descEdit, setDescEdit] = useState('')
  var [graviteEdit, setGraviteEdit] = useState('1')

  function ouvrirEdition(e: EvenementRedoute) {
    setIdEnEdition(e.id)
    setDescEdit(e.description)
    setGraviteEdit(String(e.gravite))
  }

  function sauvegarderEdition(id: string) {
    if (!descEdit.trim()) return
    updateEvenementRedoute(props.etudeId, id, descEdit, Number(graviteEdit))
      .then(function () { setIdEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(id: string) {
    if (!window.confirm('Supprimer cet evenement redoute ?')) return
    deleteEvenementRedoute(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">EVENEMENTS REDOUTES ({props.evenements.length})</h2>
      {props.evenements.length === 0 ? (
        <p className="text-xs text-steel">Aucun evenement redoute renseigne.</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.evenements.map(function (e) {
            if (idEnEdition === e.id) {
              return (
                <div key={e.id} className="flex items-center gap-2 py-2">
                  <input type="text" value={descEdit} onChange={function (ev) { setDescEdit(ev.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <select value={graviteEdit} onChange={function (ev) { setGraviteEdit(ev.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    <option value="1">Gravite 1</option><option value="2">Gravite 2</option><option value="3">Gravite 3</option><option value="4">Gravite 4</option>
                  </select>
                  <button onClick={function () { sauvegarderEdition(e.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                </div>
              )
            }
            return (
              <div key={e.id} className="flex items-start justify-between gap-6 py-3">
                <span className="text-sm text-ink">{e.description}</span>
                <div className="flex shrink-0 items-center gap-3">
                  <span className="font-mono text-[11px] font-medium text-risk-high">GRAVITE {e.gravite}</span>
                  <button onClick={function () { ouvrirEdition(e) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                  <button onClick={function () { supprimer(e.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label="Ajouter un evenement redoute">
        {function (fermer) {
          return (
            <div>
              <select value={valeurMetierId} onChange={function (e) { setValeurMetierId(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="">Valeur metier associee</option>
                {props.valeurs.map(function (v) { return <option key={v.id} value={v.id}>{v.description}</option> })}
              </select>
              <input type="text" placeholder="Description" value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={gravite} onChange={function (e) { setGravite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="1">Gravite 1</option>
                <option value="2">Gravite 2</option>
                <option value="3">Gravite 3</option>
                <option value="4">Gravite 4</option>
              </select>
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}

function SocleSection(props: { etudeId: string; socle: SocleSecurite | null; onChange: () => void }) {
  var [mode, setMode] = useState('iso')
  var [controleCode, setControleCode] = useState('')
  var [nomLibre, setNomLibre] = useState('')
  var [etat, setEtat] = useState(ETATS_CONFORMITE[0])
  var [etatActuel, setEtatActuel] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)
  var [idRefEnEdition, setIdRefEnEdition] = useState('')
  var [nomRefEdit, setNomRefEdit] = useState('')
  var [etatRefEdit, setEtatRefEdit] = useState(ETATS_CONFORMITE[0])
  var [etatActuelRefEdit, setEtatActuelRefEdit] = useState('')
  var [themeRefEdit, setThemeRefEdit] = useState<string | undefined>(undefined)
  var [codeRefEdit, setCodeRefEdit] = useState<string | undefined>(undefined)

  function ouvrirEditionRef(r: any) {
    setIdRefEnEdition(r.id)
    setNomRefEdit(r.nom)
    setEtatRefEdit(r.etat)
    setEtatActuelRefEdit(r.etatActuel || '')
    setThemeRefEdit(r.theme || undefined)
    setCodeRefEdit(r.codeControle || undefined)
  }

  function sauvegarderEditionRef(id: string) {
    if (!nomRefEdit.trim()) return
    updateReferentiel(props.etudeId, id, nomRefEdit, etatRefEdit, themeRefEdit, codeRefEdit, etatActuelRefEdit || undefined)
      .then(function () { setIdRefEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimerRef(id: string) {
    if (!window.confirm('Supprimer ce referentiel ?')) return
    deleteReferentiel(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function creerSocle() {
    setEnCours(true)
    setErreur('')
    createSocleSecurite(props.etudeId)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function ajouterReferentiel(fermer: () => void) {
    var controle: ControleIso | undefined
    if (mode === 'iso') {
      controle = CATALOGUE_ISO_27001.filter(function (c) { return c.code === controleCode })[0]
      if (!controle) {
        setErreur('Selectionnez un controle ISO 27001.')
        return
      }
    } else if (!nomLibre.trim()) {
      setErreur('Le nom du referentiel est obligatoire.')
      return
    }

    setEnCours(true)
    setErreur('')
    var nomEnvoye = controle ? controle.nom : nomLibre
    var themeEnvoye = controle ? controle.theme : undefined
    var codeEnvoye = controle ? controle.code : undefined

    addReferentiel(props.etudeId, nomEnvoye, etat, themeEnvoye, codeEnvoye, etatActuel || undefined)
      .then(function () {
        setControleCode('')
        setNomLibre('')
        setEtatActuel('')
        fermer()
        props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function referentielsParTheme(): { theme: string; items: any[] }[] {
    if (!props.socle) return []
    var groupes: { [key: string]: any[] } = {}
    var sansTheme: any[] = []
    props.socle.referentiels.forEach(function (r) {
      if (r.theme) {
        if (!groupes[r.theme]) groupes[r.theme] = []
        groupes[r.theme].push(r)
      } else {
        sansTheme.push(r)
      }
    })
    function trierParCode(items: any[]): any[] {
      return items.slice().sort(function (a, b) {
        var codeA = a.codeControle || ''
        var codeB = b.codeControle || ''
        var partsA = codeA.split('.').map(Number)
        var partsB = codeB.split('.').map(Number)
        for (var i = 0; i < Math.max(partsA.length, partsB.length); i++) {
          var vA = partsA[i] || 0
          var vB = partsB[i] || 0
          if (vA !== vB) return vA - vB
        }
        return 0
      })
    }
    var resultat = THEMES_ISO.filter(function (t) { return groupes[t] }).map(function (t) {
      return { theme: t, items: trierParCode(groupes[t]) }
    })
    if (sansTheme.length > 0) {
      resultat.push({ theme: 'Autres referentiels', items: sansTheme })
    }
    return resultat
  }

  var groupes = referentielsParTheme()

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">SOCLE DE SECURITE (ISO/IEC 27001:2022, Annexe A)</h2>

      {!props.socle ? (
        <div>
          <p className="mb-3 text-xs text-steel">Aucun socle de securite cree pour cette etude.</p>
          {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
          <button onClick={creerSocle} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Creation...' : 'Creer le socle de securite'}</button>
        </div>
      ) : (
        <div>
          {groupes.length === 0 ? (
            <p className="text-xs text-steel">Aucun controle renseigne.</p>
          ) : (
            <div className="space-y-6">
              {groupes.map(function (groupe) {
                return (
                  <div key={groupe.theme}>
                    <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{groupe.theme.toUpperCase()} ({groupe.items.length})</div>
                    <div className="divide-y divide-paper-line border-y border-paper-line">
                      {groupe.items.map(function (r: any) {
                        var couleur = r.etat === 'Conforme' ? 'text-risk-low' : r.etat === 'NonApplicable' ? 'text-steel-light' : 'text-risk-high'
                        if (idRefEnEdition === r.id) {
                          return (
                            <div key={r.id} className="space-y-1.5 py-2.5">
                              <input type="text" value={nomRefEdit} onChange={function (ev) { setNomRefEdit(ev.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                              <div className="flex items-center gap-2">
                                <select value={etatRefEdit} onChange={function (ev) { setEtatRefEdit(ev.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                                  {ETATS_CONFORMITE.map(function (e) { return <option key={e} value={e}>{e}</option> })}
                                </select>
                                <input type="text" value={etatActuelRefEdit} onChange={function (ev) { setEtatActuelRefEdit(ev.target.value) }} placeholder="Etat actuel observe" className="flex-1 border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                                <button onClick={function () { sauvegarderEditionRef(r.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                                <button onClick={function () { setIdRefEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                              </div>
                            </div>
                          )
                        }
                        return (
                          <div key={r.id} className="py-2.5">
                            <div className="flex items-center justify-between gap-6">
                              <span className="text-sm text-ink">
                                {r.codeControle && <span className="mr-2 font-mono text-[11px] text-steel-light">{r.codeControle}</span>}
                                {r.nom}
                              </span>
                              <div className="flex shrink-0 items-center gap-3">
                                <span className={'font-mono text-[11px] font-medium ' + couleur}>{r.etat.toUpperCase()}</span>
                                <button onClick={function () { ouvrirEditionRef(r) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                                <button onClick={function () { supprimerRef(r.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                              </div>
                            </div>
                            {r.etatActuel && (
                              <div className="mt-1 text-xs text-steel">{r.etatActuel}</div>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          <InlineForm label="Ajouter un controle">
            {function (fermer) {
              return (
                <div>
                  <div className="mb-3 flex gap-4">
                    <label className="flex items-center gap-1.5 text-xs text-ink">
                      <input type="radio" checked={mode === 'iso'} onChange={function () { setMode('iso') }} />
                      Controle ISO 27001
                    </label>
                    <label className="flex items-center gap-1.5 text-xs text-ink">
                      <input type="radio" checked={mode === 'libre'} onChange={function () { setMode('libre') }} />
                      Autre referentiel
                    </label>
                  </div>

                  {mode === 'iso' ? (
                    <select value={controleCode} onChange={function (e) { setControleCode(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                      <option value="">Choisir un controle</option>
                      {THEMES_ISO.map(function (theme) {
                        return (
                          <optgroup key={theme} label={theme}>
                            {CATALOGUE_ISO_27001.filter(function (c) { return c.theme === theme }).map(function (c) {
                              return <option key={c.code} value={c.code}>{c.code} -- {c.nom}</option>
                            })}
                          </optgroup>
                        )
                      })}
                    </select>
                  ) : (
                    <input type="text" placeholder="Nom du referentiel (ex: PSSI, RGPD)" value={nomLibre} onChange={function (e) { setNomLibre(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
                  )}

                  <select value={etat} onChange={function (e) { setEtat(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                    {ETATS_CONFORMITE.map(function (e) { return <option key={e} value={e}>{e}</option> })}
                  </select>

                  <textarea placeholder="Etat actuel observe (ex: Supports amovibles non chiffres)" value={etatActuel} onChange={function (e) { setEtatActuel(e.target.value) }} rows={2} className="mb-3 w-full resize-none border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />

                  {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
                  <button onClick={function () { ajouterReferentiel(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
                </div>
              )
            }}
          </InlineForm>
        </div>
      )}
    </section>
  )
}

var CATEGORIES_SR = ['Etatique', 'CrimeOrganise', 'Terroriste', 'ActivisteIdeologique', 'OfficineSpecialisee', 'Amateur', 'Vengeur', 'MalveillantPathologique', 'Autre']
var CATEGORIES_OV = ['EspionnageEtatiqueOuIndustriel', 'PrePositionnementStrategique', 'InfluenceDestabilisation', 'EntraveAuFonctionnement', 'SabotageDestruction', 'Lucratif', 'DefiAmusement', 'Autre']
var THEMES_SR_OV = ['Organisationnel', 'Personnes', 'Physique', 'Technologique']

var CATEGORIES_PP = ['Client', 'Partenaire', 'Prestataire', 'Autre']

function libelleCategoriePP(p: { categorie: string; descriptionCategorie?: string | null }) {
  return p.categorie === 'Autre' && p.descriptionCategorie ? p.descriptionCategorie : p.categorie
}

function PartiesPrenantesSection(props: { etudeId: string; parties: PartiePrenante[]; onChange: () => void }) {
  var [nom, setNom] = useState('')
  var [roles, setRoles] = useState('')
  var [representant, setRepresentant] = useState('')
  var [categorie, setCategorie] = useState(CATEGORIES_PP[0])
  var [descCategorie, setDescCategorie] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)
  var [idEdit, setIdEdit] = useState('')
  var [nomEdit, setNomEdit] = useState('')
  var [rolesEdit, setRolesEdit] = useState('')
  var [repEdit, setRepEdit] = useState('')
  var [categorieEdit, setCategorieEdit] = useState(CATEGORIES_PP[0])
  var [descCategorieEdit, setDescCategorieEdit] = useState('')

  function soumettre(fermer: () => void) {
    if (!nom.trim() || !roles.trim() || !representant.trim() || (categorie === 'Autre' && !descCategorie.trim())) {
      setErreur('Tous les champs sont obligatoires (dont la precision de categorie si "Autre").')
      return
    }
    setEnCours(true)
    setErreur('')
    createPartiePrenante(props.etudeId, nom, roles, representant, categorie, descCategorie)
      .then(function () { setNom(''); setRoles(''); setRepresentant(''); setDescCategorie(''); fermer(); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function ouvrirEdition(p: PartiePrenante) {
    setIdEdit(p.id); setNomEdit(p.nom); setRolesEdit(p.rolesEtAttentes); setRepEdit(p.representant)
    setCategorieEdit(p.categorie); setDescCategorieEdit(p.descriptionCategorie || '')
  }

  function sauvegarder(id: string) {
    if (!nomEdit.trim() || !rolesEdit.trim() || !repEdit.trim() || (categorieEdit === 'Autre' && !descCategorieEdit.trim())) return
    updatePartiePrenante(props.etudeId, id, nomEdit, rolesEdit, repEdit, categorieEdit, descCategorieEdit)
      .then(function () { setIdEdit(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(id: string) {
    if (!window.confirm('Supprimer cette partie prenante ?')) return
    deletePartiePrenante(props.etudeId, id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">PARTIES PRENANTES IMPORTANTES ({props.parties.length})</h2>
      {props.parties.length === 0 ? (
        <p className="text-xs text-steel">Aucune partie prenante renseignee.</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.parties.map(function (p) {
            if (idEdit === p.id) {
              return (
                <div key={p.id} className="space-y-1.5 py-2.5">
                  <input type="text" value={nomEdit} onChange={function (e) { setNomEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <input type="text" value={rolesEdit} onChange={function (e) { setRolesEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                  <div className="flex items-center gap-2">
                    <input type="text" value={repEdit} onChange={function (e) { setRepEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                    <select value={categorieEdit} onChange={function (e) { setCategorieEdit(e.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                      {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{c}</option> })}
                    </select>
                  </div>
                  {categorieEdit === 'Autre' && (
                    <input type="text" placeholder="Precisez la categorie" value={descCategorieEdit} onChange={function (e) { setDescCategorieEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                  )}
                  <div className="flex items-center gap-2">
                    <button onClick={function () { sauvegarder(p.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                    <button onClick={function () { setIdEdit('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                  </div>
                </div>
              )
            }
            return (
              <div key={p.id} className="py-2.5">
                <div className="flex items-center justify-between gap-6">
                  <span className="text-sm text-ink">{p.nom}</span>
                  <div className="flex shrink-0 items-center gap-3">
                    <span className="font-mono text-[10px] tracking-wide text-steel-light">{libelleCategoriePP(p).toUpperCase()}</span>
                    <span className="font-mono text-[11px] text-steel-light">{p.representant}</span>
                    <button onClick={function () { ouvrirEdition(p) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                    <button onClick={function () { supprimer(p.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                  </div>
                </div>
                <div className="mt-1 text-xs text-steel">{p.rolesEtAttentes}</div>
              </div>
            )
          })}
        </div>
      )}
      <InlineForm label="Ajouter une partie prenante">
        {function (fermer) {
          return (
            <div>
              <input type="text" placeholder="Nom" value={nom} onChange={function (e) { setNom(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="Roles et attentes" value={roles} onChange={function (e) { setRoles(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="Representant" value={representant} onChange={function (e) { setRepresentant(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={categorie} onChange={function (e) { setCategorie(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{c}</option> })}
              </select>
              {categorie === 'Autre' && (
                <input type="text" placeholder="Precisez la categorie" value={descCategorie} onChange={function (e) { setDescCategorie(e.target.value) }} className="mb-2 w-full border-b border-signature bg-transparent py-1.5 text-sm text-ink focus:outline-none" />
              )}
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}

var MOTIVATION_LABELS: { [key: string]: string } = {
  '1': '1 -- Tres peu motive (interet limite, attaque opportuniste)',
  '2': '2 -- Significatif (gain limite, abandonne facilement)',
  '3': '3 -- Motive (objectif clair, investit temps et ressources)',
  '4': '4 -- Fortement motive (cible prioritaire, volonte durable)',
}
var RESSOURCES_LABELS: { [key: string]: string } = {
  '1': '1 -- Limitees (outils gratuits, attaques simples)',
  '2': '2 -- Moderees (outils specialises, petite equipe)',
  '3': '3 -- Importantes (attaques complexes et prolongees)',
  '4': '4 -- Illimitees (experts, operations de longue duree)',
}

function couleurPertinence(p: string) {
  if (p === 'TresPertinent') return 'text-risk-critical'
  if (p === 'PlutotPertinent') return 'text-risk-high'
  if (p === 'MoyennementPertinent') return 'text-steel'
  return 'text-steel-light'
}

var OPTIONS_PERTINENCE = [
  { value: 'PeuPertinent', label: 'Peu pertinent' },
  { value: 'MoyennementPertinent', label: 'Moyennement pertinent' },
  { value: 'PlutotPertinent', label: 'Plutot pertinent' },
  { value: 'TresPertinent', label: 'Tres pertinent' },
]

function CoupleRow(props: { etudeId: string; couple: CoupleSourceRisqueObjectifVise; onChange: () => void }) {
  var c = props.couple
  var [edition, setEdition] = useState(false)
  var [sourceRisque, setSourceRisque] = useState(c.sourceRisque)
  var [descSr, setDescSr] = useState(c.descriptionSourceRisque)
  var [objectifVise, setObjectifVise] = useState(c.objectifVise)
  var [descOv, setDescOv] = useState(c.descriptionObjectifVise)
  var [contexte, setContexte] = useState(c.contexteVulnerabilite)
  var [theme, setTheme] = useState(c.theme)
  var [motivation, setMotivation] = useState(String(c.motivation))
  var [ressources, setRessources] = useState(String(c.ressources))
  var [erreur, setErreur] = useState('')

  function sauvegarder() {
    if (!descSr.trim() || !descOv.trim() || !contexte.trim()) {
      setErreur('Description SR, OV et contexte obligatoires.')
      return
    }
    updateCoupleSrOv(props.etudeId, c.id, sourceRisque, descSr, objectifVise, descOv, contexte, theme, Number(motivation), Number(ressources))
      .then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer() {
    if (!window.confirm('Supprimer ce couple SR/OV ?')) return
    deleteCoupleSrOv(props.etudeId, c.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  if (edition) {
    return (
      <div className="border-l-2 border-signature space-y-1.5 py-2.5 pl-3">
        <select value={sourceRisque} onChange={function (e) { setSourceRisque(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {CATEGORIES_SR.map(function (cc) { return <option key={cc} value={cc}>{cc}</option> })}
        </select>
        <input type="text" value={descSr} onChange={function (e) { setDescSr(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <select value={objectifVise} onChange={function (e) { setObjectifVise(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {CATEGORIES_OV.map(function (cc) { return <option key={cc} value={cc}>{cc}</option> })}
        </select>
        <input type="text" value={descOv} onChange={function (e) { setDescOv(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <textarea value={contexte} onChange={function (e) { setContexte(e.target.value) }} rows={2} className="w-full resize-none border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <select value={theme} onChange={function (e) { setTheme(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {THEMES_SR_OV.map(function (t) { return <option key={t} value={t}>{t}</option> })}
        </select>
        <div className="flex gap-3">
          <select value={motivation} onChange={function (e) { setMotivation(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{MOTIVATION_LABELS[v]}</option> })}
          </select>
          <select value={ressources} onChange={function (e) { setRessources(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{RESSOURCES_LABELS[v]}</option> })}
          </select>
        </div>
        <GrilleMatrice matrice={MATRICE_PERTINENCE} ligneLabels={['1 -- Tres peu motive', '2 -- Significatif', '3 -- Motive', '4 -- Fortement motive']} colonneLabels={['1 -- Limitees', '2 -- Moderees', '3 -- Importantes', '4 -- Illimitees']} ligneTitre="Motivation" colonneTitre="Ressources" ligneSelectionnee={Number(motivation) - 1} colonneSelectionnee={Number(ressources) - 1} couleurCellule={couleurPertinence} />
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
        </div>
      </div>
    )
  }

  return (
    <div className="py-2.5">
      <div className="flex items-center justify-between gap-6">
        <span className="text-sm text-ink">{c.sourceRisque === 'Autre' ? c.descriptionSourceRisque : c.sourceRisque} -- {c.objectifVise === 'Autre' ? c.descriptionObjectifVise : c.objectifVise}</span>
        <div className="flex shrink-0 items-center gap-3">
          <span className={'font-mono text-[11px] font-medium ' + couleurPertinence(c.pertinence)}>{c.pertinence}</span>
          <button onClick={function () { setEdition(true) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
          <button onClick={supprimer} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
        </div>
      </div>
      <div className="mt-1 text-xs text-steel">{c.contexteVulnerabilite}</div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">Motivation {c.motivation} -- Ressources {c.ressources}</div>
      <div className="mt-1.5">
        <OverrideJugementExpert
          valeurCalculee={c.pertinenceCalculee}
          valeurRetenue={c.pertinenceRetenue}
          justification={c.justificationPertinence}
          options={OPTIONS_PERTINENCE}
          onDefinir={function (v, j) { return definirPertinenceRetenue(props.etudeId, c.id, v, j).then(props.onChange) }}
          onReinitialiser={function () { return reinitialiserPertinence(props.etudeId, c.id).then(props.onChange) }}
        />
      </div>
    </div>
  )
}

function CouplesSrOvSection(props: { etudeId: string; couples: CoupleSourceRisqueObjectifVise[]; onChange: () => void }) {
  var [sourceRisque, setSourceRisque] = useState(CATEGORIES_SR[0])
  var [descSr, setDescSr] = useState('')
  var [objectifVise, setObjectifVise] = useState(CATEGORIES_OV[0])
  var [descOv, setDescOv] = useState('')
  var [contexte, setContexte] = useState('')
  var [theme, setTheme] = useState(THEMES_SR_OV[0])
  var [motivation, setMotivation] = useState('2')
  var [ressources, setRessources] = useState('2')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(fermer: () => void) {
    if (!descSr.trim() || !descOv.trim() || !contexte.trim()) {
      setErreur('Description SR, OV et contexte obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createCoupleSrOv(props.etudeId, sourceRisque, descSr, objectifVise, descOv, contexte, theme, Number(motivation), Number(ressources))
      .then(function () { setDescSr(''); setDescOv(''); setContexte(''); fermer(); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  var groupesTheme = THEMES_SR_OV.map(function (t) {
    return { theme: t, items: props.couples.filter(function (c) { return c.theme === t }) }
  })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">COUPLES SOURCE DE RISQUE / OBJECTIF VISE ({props.couples.length})</h2>

      {props.couples.length === 0 ? (
        <p className="text-xs text-steel">Aucun couple SR/OV renseigne.</p>
      ) : (
        <div className="space-y-6">
          {groupesTheme.map(function (g) {
            if (g.items.length === 0) {
              return (
                <div key={g.theme}>
                  <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{g.theme.toUpperCase()} (0)</div>
                  <p className="text-xs text-steel">Aucun couple renseigne pour ce theme.</p>
                </div>
              )
            }
            return (
              <div key={g.theme}>
                <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{g.theme.toUpperCase()} ({g.items.length})</div>
                <div className="divide-y divide-paper-line border-y border-paper-line">
                  {g.items.map(function (c) {
                    return <CoupleRow key={c.id} etudeId={props.etudeId} couple={c} onChange={props.onChange} />
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label="Ajouter un couple SR/OV">
        {function (fermer) {
          return (
            <div>
              <select value={sourceRisque} onChange={function (e) { setSourceRisque(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_SR.map(function (c) { return <option key={c} value={c}>{c}</option> })}
              </select>
              <input type="text" placeholder={sourceRisque === 'Autre' ? 'Precisez la categorie de source de risque' : 'Description de la source de risque'} value={descSr} onChange={function (e) { setDescSr(e.target.value) }} className={'mb-2 w-full bg-transparent py-1.5 text-sm text-ink focus:outline-none ' + (sourceRisque === 'Autre' ? 'border-b border-signature' : 'border-b border-paper-line focus:border-signature')} />
              <select value={objectifVise} onChange={function (e) { setObjectifVise(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_OV.map(function (c) { return <option key={c} value={c}>{c}</option> })}
              </select>
              <input type="text" placeholder={objectifVise === 'Autre' ? 'Precisez la categorie d objectif vise' : 'Description de l objectif vise'} value={descOv} onChange={function (e) { setDescOv(e.target.value) }} className={'mb-2 w-full bg-transparent py-1.5 text-sm text-ink focus:outline-none ' + (objectifVise === 'Autre' ? 'border-b border-signature' : 'border-b border-paper-line focus:border-signature')} />
              <textarea placeholder="Contexte / vulnerabilite associee" value={contexte} onChange={function (e) { setContexte(e.target.value) }} rows={2} className="mb-2 w-full resize-none border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={theme} onChange={function (e) { setTheme(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {THEMES_SR_OV.map(function (t) { return <option key={t} value={t}>{t}</option> })}
              </select>
              <div className="mb-3 flex gap-3">
                <select value={motivation} onChange={function (e) { setMotivation(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                  {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{MOTIVATION_LABELS[v]}</option> })}
                </select>
                <select value={ressources} onChange={function (e) { setRessources(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                  {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{RESSOURCES_LABELS[v]}</option> })}
                </select>
              </div>
              <GrilleMatrice matrice={MATRICE_PERTINENCE} ligneLabels={['1 -- Tres peu motive', '2 -- Significatif', '3 -- Motive', '4 -- Fortement motive']} colonneLabels={['1 -- Limitees', '2 -- Moderees', '3 -- Importantes', '4 -- Illimitees']} ligneTitre="Motivation" colonneTitre="Ressources" ligneSelectionnee={Number(motivation) - 1} colonneSelectionnee={Number(ressources) - 1} couleurCellule={couleurPertinence} />
              {erreur && <p className="mb-2 mt-2 text-xs text-risk-critical">{erreur}</p>}
              <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="mt-2 rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}

var ECHELLE_DANGEROSITE: { [key: string]: string[] } = {
  dependance: [
    '1 -- Relation non necessaire aux fonctions strategiques',
    '2 -- Relation utile aux fonctions strategiques',
    '3 -- Relation indispensable mais non exclusive',
    '4 -- Relation indispensable et unique (pas de substitution possible a court terme)',
  ],
  penetration: [
    '1 -- Pas d acces, ou acces utilisateur a des terminaux (poste, ordiphone)',
    '2 -- Acces administrateur a des terminaux, ou acces physique aux sites',
    '3 -- Acces administrateur a des serveurs metier (fichiers, bases, web, applicatifs)',
    '4 -- Acces administrateur a des equipements d infrastructure, ou acces physique aux salles serveurs',
  ],
  maturiteCyber: [
    '1 -- Regles d hygiene appliquees ponctuellement, non formalisees, reaction incertaine',
    '2 -- Regles d hygiene prises en compte, sans politique globale, mode reactif',
    '3 -- Politique globale appliquee en mode reactif, recherche de centralisation',
    '4 -- Politique de management du risque integree, dimension proactive',
  ],
  confiance: [
    '1 -- Intentions de la partie prenante non evaluables',
    '2 -- Intentions considerees comme neutres',
    '3 -- Intentions connues et probablement positives',
    '4 -- Intentions parfaitement connues et pleinement compatibles',
  ],
}

function libelleZone(zone: string) {
  if (zone === 'Danger') return 'Zone de danger'
  if (zone === 'Controle') return 'Zone de controle'
  return 'Zone de veille'
}

function couleurZone(zone: string) {
  if (zone === 'Danger') return 'text-risk-critical'
  if (zone === 'Controle') return 'text-risk-high'
  return 'text-risk-low'
}

function ChampEchelleDangerosite(props: { label: string; critere: string; valeur: string; onChange: (v: string) => void }) {
  return (
    <div>
      <label className="mb-1 block font-mono text-[9px] text-steel-light">{props.label}</label>
      <select value={props.valeur} onChange={function (e) { props.onChange(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
        <option value="1">1</option><option value="2">2</option><option value="3">3</option><option value="4">4</option>
      </select>
      <p className="mt-1 text-[10px] leading-snug text-steel">{ECHELLE_DANGEROSITE[props.critere][Number(props.valeur) - 1]}</p>
    </div>
  )
}

function EvaluationDangerositeSection(props: { etudeId: string; parties: PartiePrenante[]; onChange: () => void }) {
  var [idEnEdition, setIdEnEdition] = useState('')
  var [dependance, setDependance] = useState('2')
  var [penetration, setPenetration] = useState('2')
  var [maturiteCyber, setMaturiteCyber] = useState('2')
  var [confiance, setConfiance] = useState('2')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function ouvrirEvaluation(p: PartiePrenante) {
    setIdEnEdition(p.id)
    setDependance(String(p.dependance || 2))
    setPenetration(String(p.penetration || 2))
    setMaturiteCyber(String(p.maturiteCyber || 2))
    setConfiance(String(p.confiance || 2))
    setErreur('')
  }

  function soumettre(id: string) {
    setEnCours(true)
    setErreur('')
    evaluerDangerosite(props.etudeId, id, Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))
      .then(function () { setIdEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  var critiques = props.parties.filter(function (p) { return p.zone === 'Danger' || p.zone === 'Controle' })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">EVALUATION DE LA DANGEROSITE PAR PARTIE PRENANTE ({props.parties.length})</h2>
      <p className="mb-4 text-xs text-steel">Niveau de dangerosite = (Dependance x Penetration) / (Maturite cyber x Confiance). Les parties prenantes en zone de controle ou de danger sont dites <span className="font-medium text-ink">critiques</span> : elles definissent le perimetre reel de l analyse et doivent etre prises en compte dans les scenarios strategiques.</p>
      {props.parties.length === 0 ? (
        <p className="text-xs text-steel">Aucune partie prenante renseignee (a ajouter depuis l Atelier 2).</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.parties.map(function (p) {
            if (idEnEdition === p.id) {
              return (
                <div key={p.id} className="space-y-2 py-3">
                  <div className="text-sm text-ink">{p.nom}</div>
                  <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                    <ChampEchelleDangerosite label="DEPENDANCE" critere="dependance" valeur={dependance} onChange={setDependance} />
                    <ChampEchelleDangerosite label="PENETRATION" critere="penetration" valeur={penetration} onChange={setPenetration} />
                    <ChampEchelleDangerosite label="MATURITE CYBER" critere="maturiteCyber" valeur={maturiteCyber} onChange={setMaturiteCyber} />
                    <ChampEchelleDangerosite label="CONFIANCE" critere="confiance" valeur={confiance} onChange={setConfiance} />
                  </div>
                  <div className="font-mono text-[11px] text-steel-light">
                    Apercu : <span className={'font-medium ' + couleurZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))}>
                      {libelleZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))} ({calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))})
                    </span>
                  </div>
                  {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
                  <div className="flex gap-3">
                    <button onClick={function () { soumettre(p.id) }} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Enregistrement...' : 'OK'}</button>
                    <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                  </div>
                </div>
              )
            }
            return (
              <div key={p.id} className="py-2.5">
                <div className="flex items-center justify-between gap-6">
                  <div>
                    <div className="text-sm text-ink">{p.nom}</div>
                    <div className="mt-0.5 font-mono text-[10px] tracking-wide text-steel-light">{p.categorie === 'Autre' ? p.descriptionCategorie : p.categorie} -- {p.representant}</div>
                  </div>
                  <div className="flex shrink-0 items-center gap-3">
                    {p.niveauDangerosite != null && p.zone ? (
                      <span className={'font-mono text-[11px] font-medium ' + couleurZone(p.zone)}>{libelleZone(p.zone)} ({p.niveauDangerosite})</span>
                    ) : (
                      <span className="font-mono text-[11px] text-steel-light">Non evaluee</span>
                    )}
                    <button onClick={function () { ouvrirEvaluation(p) }} className="text-[11px] text-steel-light hover:text-signature">{p.niveauDangerosite != null ? 'Reevaluer' : 'Evaluer'}</button>
                  </div>
                </div>
                {p.niveauDangerositeCalcule != null && (
                  <div className="mt-1.5">
                    <OverrideJugementExpert
                      valeurCalculee={String(p.niveauDangerositeCalcule)}
                      valeurRetenue={p.niveauDangerositeRetenu != null ? String(p.niveauDangerositeRetenu) : null}
                      justification={p.justificationDangerosite}
                      options={[]}
                      onDefinir={function (v, j) { return definirDangerositeRetenue(props.etudeId, p.id, Number(v), j).then(props.onChange) }}
                      onReinitialiser={function () { return reinitialiserDangerosite(props.etudeId, p.id).then(props.onChange) }}
                      valeurLibre
                    />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
      {critiques.length > 0 && (
        <p className="mt-4 text-xs text-steel">
          <span className="font-medium text-ink">{critiques.length} partie(s) prenante(s) critique(s)</span> (zone de controle ou de danger) : {critiques.map(function (p) { return p.nom }).join(', ')}.
        </p>
      )}
    </section>
  )
}

function MesuresEcosystemeSection(props: { etudeId: string; parties: PartiePrenante[]; onChange: () => void }) {
  var critiques = props.parties.filter(function (p) { return p.zone === 'Danger' || p.zone === 'Controle' })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">MESURES DE SECURITE SUR L ECOSYSTEME</h2>
      <p className="mb-4 text-xs text-steel">Pour chaque partie prenante critique, proposez des mesures de reduction du risque (reduire la dangerosite induite, ou agir sur le deroulement des scenarios strategiques), puis reevaluez la dangerosite residuelle apres application des mesures.</p>
      {critiques.length === 0 ? (
        <p className="text-xs text-steel">Aucune partie prenante critique (zone de controle ou de danger) -- rien a traiter ici pour l instant.</p>
      ) : (
        <div className="space-y-6">
          {critiques.map(function (p) {
            return <MesuresPartiePrenante key={p.id} etudeId={props.etudeId} partie={p} onChange={props.onChange} />
          })}
        </div>
      )}
    </section>
  )
}

function MesuresPartiePrenante(props: { etudeId: string; partie: PartiePrenante; onChange: () => void }) {
  var p = props.partie
  var [ajoutMesure, setAjoutMesure] = useState(false)
  var [descMesure, setDescMesure] = useState('')
  var [reevaluation, setReevaluation] = useState(false)
  var [dependance, setDependance] = useState(String(p.dependanceResiduelle || p.dependance || 2))
  var [penetration, setPenetration] = useState(String(p.penetrationResiduelle || p.penetration || 2))
  var [maturiteCyber, setMaturiteCyber] = useState(String(p.maturiteCyberResiduelle || p.maturiteCyber || 2))
  var [confiance, setConfiance] = useState(String(p.confianceResiduelle || p.confiance || 2))
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function creerMesure() {
    if (!descMesure.trim()) {
      setErreur('La description de la mesure est obligatoire.')
      return
    }
    setEnCours(true)
    setErreur('')
    ajouterMesureEcosysteme(props.etudeId, p.id, descMesure)
      .then(function () { setDescMesure(''); setAjoutMesure(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function supprimerMesure(mesureId: string) {
    if (!window.confirm('Supprimer cette mesure ?')) return
    supprimerMesureEcosysteme(props.etudeId, p.id, mesureId).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function soumettreReevaluation() {
    setEnCours(true)
    setErreur('')
    evaluerDangerositeResiduelle(props.etudeId, p.id, Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))
      .then(function () { setReevaluation(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  return (
    <div className="border border-paper-line p-4">
      <div className="mb-1 flex items-center justify-between">
        <span className="text-sm font-medium text-ink">{p.nom}</span>
        <span className={'font-mono text-[11px] font-medium ' + couleurZone(p.zone || '')}>{libelleZone(p.zone || '')}</span>
      </div>
      <div className="mb-3 font-mono text-[10px] tracking-wide text-steel-light">{p.categorie === 'Autre' ? p.descriptionCategorie : p.categorie} -- {p.representant}</div>

      <div className="mb-3 flex items-center gap-4 border-y border-paper-line py-2">
        <div>
          <div className="font-mono text-[9px] text-steel-light">DANGEROSITE INITIALE</div>
          <div className={'font-mono text-sm font-medium ' + couleurZone(p.zone || '')}>{p.niveauDangerosite} -- {libelleZone(p.zone || '')}</div>
        </div>
        <div className="text-steel-light">&#8594;</div>
        <div>
          <div className="font-mono text-[9px] text-steel-light">DANGEROSITE RESIDUELLE</div>
          {p.niveauDangerositeResiduel != null && p.zoneResiduelle ? (
            <div className={'font-mono text-sm font-medium ' + couleurZone(p.zoneResiduelle)}>{p.niveauDangerositeResiduel} -- {libelleZone(p.zoneResiduelle)}</div>
          ) : (
            <div className="font-mono text-sm text-steel-light">Non reevaluee</div>
          )}
        </div>
      </div>

      {p.niveauDangerositeCalcule != null && (
        <div className="mb-3">
          <OverrideJugementExpert
            valeurCalculee={String(p.niveauDangerositeCalcule)}
            valeurRetenue={p.niveauDangerositeRetenu != null ? String(p.niveauDangerositeRetenu) : null}
            justification={p.justificationDangerosite}
            options={[]}
            onDefinir={function (v, j) { return definirDangerositeRetenue(props.etudeId, p.id, Number(v), j).then(props.onChange) }}
            onReinitialiser={function () { return reinitialiserDangerosite(props.etudeId, p.id).then(props.onChange) }}
            valeurLibre
          />
        </div>
      )}
      {p.niveauDangerositeResiduelCalcule != null && (
        <div className="mb-3">
          <OverrideJugementExpert
            valeurCalculee={String(p.niveauDangerositeResiduelCalcule)}
            valeurRetenue={p.niveauDangerositeResiduelRetenu != null ? String(p.niveauDangerositeResiduelRetenu) : null}
            justification={p.justificationDangerositeResiduelle}
            options={[]}
            onDefinir={function (v, j) { return definirDangerositeResidueleRetenue(props.etudeId, p.id, Number(v), j).then(props.onChange) }}
            onReinitialiser={function () { return reinitialiserDangerositeResiduelle(props.etudeId, p.id).then(props.onChange) }}
            valeurLibre
          />
        </div>
      )}

      <h3 className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">MESURES PROPOSEES ({p.mesures.length})</h3>
      {p.mesures.length === 0 ? (
        <p className="mb-2 text-xs text-steel">Aucune mesure proposee.</p>
      ) : (
        <ul className="mb-2 space-y-1.5">
          {p.mesures.map(function (m) {
            return (
              <li key={m.id} className="flex items-start justify-between gap-4">
                <span className="text-xs text-steel">{m.description}</span>
                <button onClick={function () { supprimerMesure(m.id) }} className="shrink-0 text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
              </li>
            )
          })}
        </ul>
      )}

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      {ajoutMesure ? (
        <div className="mb-3 space-y-1.5">
          <textarea placeholder="Description de la mesure (ex: reduire la dependance a ce sous-traitant)" value={descMesure} onChange={function (e) { setDescMesure(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={creerMesure} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            <button onClick={function () { setAjoutMesure(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
          </div>
        </div>
      ) : (
        <button onClick={function () { setAjoutMesure(true) }} className="mb-3 font-mono text-[10px] font-medium text-signature hover:underline">+ Ajouter une mesure</button>
      )}

      {reevaluation ? (
        <div className="space-y-2 border-t border-paper-line pt-3">
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <ChampEchelleDangerosite label="DEPENDANCE" critere="dependance" valeur={dependance} onChange={setDependance} />
            <ChampEchelleDangerosite label="PENETRATION" critere="penetration" valeur={penetration} onChange={setPenetration} />
            <ChampEchelleDangerosite label="MATURITE CYBER" critere="maturiteCyber" valeur={maturiteCyber} onChange={setMaturiteCyber} />
            <ChampEchelleDangerosite label="CONFIANCE" critere="confiance" valeur={confiance} onChange={setConfiance} />
          </div>
          <div className="font-mono text-[11px] text-steel-light">
            Apercu : <span className={'font-medium ' + couleurZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))}>
              {libelleZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))} ({calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))})
            </span>
          </div>
          <div className="flex gap-3">
            <button onClick={soumettreReevaluation} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Enregistrement...' : 'OK'}</button>
            <button onClick={function () { setReevaluation(false) }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
          </div>
        </div>
      ) : (
        <button onClick={function () { setReevaluation(true) }} className="font-mono text-[10px] font-medium text-signature hover:underline">{p.niveauDangerositeResiduel != null ? 'Reevaluer la dangerosite residuelle' : 'Evaluer la dangerosite residuelle'}</button>
      )}
    </div>
  )
}

function libelleCouple(c: CoupleSourceRisqueObjectifVise) {
  var sr = c.sourceRisque === 'Autre' ? c.descriptionSourceRisque : c.sourceRisque
  var ov = c.objectifVise === 'Autre' ? c.descriptionObjectifVise : c.objectifVise
  return sr + ' -- ' + ov
}

function couleurGravite(gravite: number) {
  if (gravite >= 4) return 'text-risk-critical'
  if (gravite >= 3) return 'text-risk-high'
  if (gravite >= 2) return 'text-risk-moderate'
  return 'text-risk-low'
}

function ScenariosStrategiquesSection(props: {
  etudeId: string
  couples: CoupleSourceRisqueObjectifVise[]
  scenarios: ScenarioStrategique[]
  evenements: EvenementRedoute[]
  valeurs: ValeurMetier[]
  onChange: () => void
}) {
  var [coupleEnCreation, setCoupleEnCreation] = useState('')
  var [description, setDescription] = useState('')
  var [evenementRedouteId, setEvenementRedouteId] = useState('')
  var [idEnEdition, setIdEnEdition] = useState('')
  var [descEdit, setDescEdit] = useState('')
  var [erEdit, setErEdit] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function libelleValeurMetier(id: string) {
    var vm = props.valeurs.filter(function (v) { return v.id === id })[0]
    return vm ? vm.description : '?'
  }

  function libelleEr(erId: string) {
    var er = props.evenements.filter(function (e) { return e.id === erId })[0]
    if (!er) return 'Evenement redoute introuvable'
    return libelleValeurMetier(er.valeurMetierId) + ' -- ' + er.description + ' (gravite ' + er.gravite + ')'
  }

  var couplesRetenus = props.couples.filter(function (c) {
    return c.pertinence === 'TresPertinent' || c.pertinence === 'PlutotPertinent'
  })
  var idsAvecScenario: { [key: string]: boolean } = {}
  props.scenarios.forEach(function (s) { idsAvecScenario[s.coupleSourceRisqueObjectifViseId] = true })
  var couplesSansScenario = couplesRetenus.filter(function (c) { return !idsAvecScenario[c.id] })

  function ouvrirCreation(coupleId: string) {
    setCoupleEnCreation(coupleId)
    setDescription('')
    setEvenementRedouteId(props.evenements.length > 0 ? props.evenements[0].id : '')
    setErreur('')
  }

  function creer(coupleId: string) {
    if (!description.trim() || !evenementRedouteId) {
      setErreur('La description et l evenement redoute cible sont obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createScenarioStrategique(props.etudeId, coupleId, evenementRedouteId, description)
      .then(function () { setCoupleEnCreation(''); setDescription(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function ouvrirEdition(s: ScenarioStrategique) {
    setIdEnEdition(s.id)
    setDescEdit(s.description)
    setErEdit(s.evenementRedouteId)
  }

  function sauvegarder(id: string) {
    if (!descEdit.trim() || !erEdit) return
    updateScenarioStrategique(props.etudeId, id, erEdit, descEdit)
      .then(function () { setIdEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(id: string) {
    if (!window.confirm('Supprimer ce scenario strategique ? Les chemins d attaque associes seront egalement supprimes.')) return
    deleteScenarioStrategique(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">SCENARIOS STRATEGIQUES ({props.scenarios.length})</h2>
      <p className="mb-4 text-xs text-steel">1 couple SR/OV retenu =&gt; 1 scenario stratégique. Chaque scenario cible un evenement redoute (Atelier 1) dont il herite la gravite -- identique pour le scenario et tous ses chemins d attaque.</p>

      {props.scenarios.length === 0 ? (
        <p className="text-xs text-steel">Aucun scenario strategique cree.</p>
      ) : (
        <div className="mb-6 divide-y divide-paper-line border-y border-paper-line">
          {props.scenarios.map(function (s) {
            var couple = props.couples.filter(function (c) { return c.id === s.coupleSourceRisqueObjectifViseId })[0]
            var er = props.evenements.filter(function (e) { return e.id === s.evenementRedouteId })[0]
            if (idEnEdition === s.id) {
              return (
                <div key={s.id} className="space-y-1.5 py-2.5">
                  {couple && <div className="font-mono text-[11px] text-steel-light">{libelleCouple(couple)}</div>}
                  <select value={erEdit} onChange={function (e) { setErEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    {props.evenements.map(function (e) { return <option key={e.id} value={e.id}>{libelleEr(e.id)}</option> })}
                  </select>
                  <textarea value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <div className="flex gap-2">
                    <button onClick={function () { sauvegarder(s.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                    <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                  </div>
                </div>
              )
            }
            return (
              <div key={s.id} className="py-2.5">
                <div className="flex items-center justify-between gap-6">
                  {couple ? (
                    <span className="text-sm text-ink">{libelleCouple(couple)}</span>
                  ) : (
                    <span className="text-sm text-steel-light">Couple introuvable</span>
                  )}
                  <div className="flex shrink-0 items-center gap-3">
                    {er && <span className={'font-mono text-[11px] font-medium ' + couleurGravite(er.gravite)}>Gravite {er.gravite}</span>}
                    <button onClick={function () { ouvrirEdition(s) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
                    <button onClick={function () { supprimer(s.id) }} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
                  </div>
                </div>
                <div className="mt-1 text-xs text-steel">{s.description}</div>
                <div className="mt-1 font-mono text-[10px] text-steel-light">Cible : {er ? libelleEr(er.id) : 'evenement redoute introuvable'}</div>
              </div>
            )
          })}
        </div>
      )}

      <h3 className="mb-3 font-mono text-[10px] tracking-wide text-steel-light">COUPLES RETENUS EN ATTENTE DE SCENARIO ({couplesSansScenario.length})</h3>
      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
      {couplesSansScenario.length === 0 ? (
        <p className="text-xs text-steel">Aucun couple retenu (Atelier 2) en attente -- soit tous ont deja un scenario, soit aucun couple n est encore retenu.</p>
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {couplesSansScenario.map(function (c) {
            if (coupleEnCreation === c.id) {
              return (
                <div key={c.id} className="space-y-1.5 py-2.5">
                  <div className="font-mono text-[11px] text-steel-light">{libelleCouple(c)}</div>
                  {props.evenements.length === 0 ? (
                    <p className="text-xs text-risk-critical">Aucun evenement redoute disponible (Atelier 1) -- impossible de creer un scenario.</p>
                  ) : (
                    <select value={evenementRedouteId} onChange={function (e) { setEvenementRedouteId(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                      {props.evenements.map(function (e) { return <option key={e.id} value={e.id}>{libelleEr(e.id)}</option> })}
                    </select>
                  )}
                  <textarea placeholder="Description du scenario (de la source de risque vers l objectif vise)" value={description} onChange={function (e) { setDescription(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <div className="flex gap-2">
                    <button onClick={function () { creer(c.id) }} disabled={enCours || props.evenements.length === 0} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Creation...' : 'Creer le scenario'}</button>
                    <button onClick={function () { setCoupleEnCreation(''); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
                  </div>
                </div>
              )
            }
            return (
              <div key={c.id} className="flex items-center justify-between gap-6 py-2.5">
                <span className="text-sm text-ink">{libelleCouple(c)}</span>
                <button onClick={function () { ouvrirCreation(c.id) }} className="text-[11px] font-medium text-signature hover:underline">Creer un scenario</button>
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}

function CheminsAttaqueSection(props: {
  etudeId: string
  scenarios: ScenarioStrategique[]
  couples: CoupleSourceRisqueObjectifVise[]
  chemins: CheminAttaque[]
  parties: PartiePrenante[]
  onChange: () => void
}) {
  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">CHEMINS D ATTAQUE ({props.chemins.length})</h2>
      <p className="mb-4 text-xs text-steel">1 scenario stratégique =&gt; plusieurs chemins d attaque. Un chemin d attaque est un sequencement d actions/effets que la source de risque devra probablement generer pour atteindre son objectif -- il peut etre direct (0 partie prenante traversee) ou passer par une ou plusieurs parties prenantes de l ecosysteme, chaque franchissement generant un evenement intermediaire.</p>
      {props.scenarios.length === 0 ? (
        <p className="text-xs text-steel">Aucun scenario stratégique -- creez-en un ci-dessus avant d ajouter des chemins d attaque.</p>
      ) : (
        <div className="space-y-6">
          {props.scenarios.map(function (scenario) {
            var couple = props.couples.filter(function (c) { return c.id === scenario.coupleSourceRisqueObjectifViseId })[0]
            var chemins = props.chemins.filter(function (c) { return c.scenarioStrategiqueId === scenario.id })
            return (
              <CheminsParScenario
                key={scenario.id}
                etudeId={props.etudeId}
                scenarioId={scenario.id}
                libelleScenario={couple ? libelleCouple(couple) : scenario.description}
                chemins={chemins}
                parties={props.parties}
                onChange={props.onChange}
              />
            )
          })}
        </div>
      )}
    </section>
  )
}

function CheminsParScenario(props: {
  etudeId: string
  scenarioId: string
  libelleScenario: string
  chemins: CheminAttaque[]
  parties: PartiePrenante[]
  onChange: () => void
}) {
  var [enCreation, setEnCreation] = useState(false)
  var [description, setDescription] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function creer() {
    if (!description.trim()) {
      setErreur('La description est obligatoire.')
      return
    }
    setEnCours(true)
    setErreur('')
    createCheminAttaque(props.etudeId, props.scenarioId, description)
      .then(function () { setDescription(''); setEnCreation(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  return (
    <div className="border border-paper-line p-4">
      <div className="mb-3 flex items-center justify-between">
        <span className="text-sm font-medium text-ink">{props.libelleScenario}</span>
        <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.chemins.length} CHEMIN{props.chemins.length > 1 ? 'S' : ''}</span>
      </div>

      {props.chemins.length === 0 ? (
        <p className="mb-3 text-xs text-steel">Aucun chemin d attaque pour ce scenario.</p>
      ) : (
        <div className="mb-3 space-y-4">
          {props.chemins.map(function (chemin) {
            return <CheminRow key={chemin.id} etudeId={props.etudeId} chemin={chemin} parties={props.parties} onChange={props.onChange} />
          })}
        </div>
      )}

      {enCreation ? (
        <div className="space-y-1.5">
          <textarea placeholder="Description du chemin (ex: canal d exfiltration direct, ou via tel prestataire)" value={description} onChange={function (e) { setDescription(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
          <div className="flex gap-3">
            <button onClick={creer} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Creation...' : 'Creer le chemin'}</button>
            <button onClick={function () { setEnCreation(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
          </div>
        </div>
      ) : (
        <button onClick={function () { setEnCreation(true) }} className="flex items-center gap-1.5 font-mono text-[11px] font-medium text-signature hover:underline">+ Ajouter un chemin d attaque</button>
      )}
    </div>
  )
}

function CheminRow(props: { etudeId: string; chemin: CheminAttaque; parties: PartiePrenante[]; onChange: () => void }) {
  var [ajoutEi, setAjoutEi] = useState(false)
  var [ppId, setPpId] = useState(props.parties.length > 0 ? props.parties[0].id : '')
  var [descEi, setDescEi] = useState('')
  var [editionChemin, setEditionChemin] = useState(false)
  var [descCheminEdit, setDescCheminEdit] = useState(props.chemin.description)
  var [eiEnEdition, setEiEnEdition] = useState('')
  var [descEiEdit, setDescEiEdit] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function supprimerChemin() {
    if (!window.confirm('Supprimer ce chemin d attaque et ses evenements intermediaires ?')) return
    deleteCheminAttaque(props.etudeId, props.chemin.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function sauvegarderChemin() {
    if (!descCheminEdit.trim()) return
    updateCheminAttaque(props.etudeId, props.chemin.id, descCheminEdit)
      .then(function () { setEditionChemin(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function ajouterEi() {
    if (!descEi.trim() || !ppId) {
      setErreur('La partie prenante et la description sont obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    createEvenementIntermediaire(props.etudeId, props.chemin.id, ppId, descEi)
      .then(function () { setDescEi(''); setAjoutEi(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function ouvrirEditionEi(eiId: string, descriptionActuelle: string) {
    setEiEnEdition(eiId)
    setDescEiEdit(descriptionActuelle)
  }

  function sauvegarderEi(eiId: string) {
    if (!descEiEdit.trim()) return
    updateEvenementIntermediaire(props.etudeId, props.chemin.id, eiId, descEiEdit)
      .then(function () { setEiEnEdition(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimerEi(eiId: string) {
    if (!window.confirm('Supprimer cet evenement intermediaire ?')) return
    deleteEvenementIntermediaire(props.etudeId, props.chemin.id, eiId).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function libellePartie(id: string) {
    var pp = props.parties.filter(function (p) { return p.id === id })[0]
    return pp ? pp.nom : 'Partie prenante introuvable'
  }

  return (
    <div className="border-l-2 border-paper-line pl-3">
      {editionChemin ? (
        <div className="space-y-1.5">
          <textarea value={descCheminEdit} onChange={function (e) { setDescCheminEdit(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={sauvegarderChemin} className="text-xs font-medium text-signature hover:underline">OK</button>
            <button onClick={function () { setEditionChemin(false) }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
          </div>
        </div>
      ) : (
        <div className="flex items-center justify-between gap-4">
          <span className="text-sm text-ink">{props.chemin.description}</span>
          <div className="flex shrink-0 items-center gap-3">
            <button onClick={function () { setDescCheminEdit(props.chemin.description); setEditionChemin(true) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
            <button onClick={supprimerChemin} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
          </div>
        </div>
      )}

      {props.chemin.evenementsIntermediaires.length === 0 ? (
        <p className="mt-1 text-xs italic text-steel">Chemin direct -- aucune partie prenante traversee.</p>
      ) : (
        <ol className="mt-2 space-y-1">
          {props.chemin.evenementsIntermediaires.map(function (ei, i) {
            if (eiEnEdition === ei.id) {
              return (
                <li key={ei.id} className="flex items-center gap-2">
                  <span className="font-mono text-[11px] text-steel-light">{i + 1}. {libellePartie(ei.partiePrenanteId)} --</span>
                  <input type="text" value={descEiEdit} onChange={function (e) { setDescEiEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-0.5 text-xs text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEi(ei.id) }} className="shrink-0 text-[11px] font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setEiEnEdition('') }} className="shrink-0 text-[11px] text-steel-light hover:text-ink">Annuler</button>
                </li>
              )
            }
            return (
              <li key={ei.id} className="flex items-center justify-between gap-4 font-mono text-[11px] text-steel">
                <span>{i + 1}. <span className="font-medium text-ink">{libellePartie(ei.partiePrenanteId)}</span> -- {ei.description}</span>
                <div className="flex shrink-0 items-center gap-2">
                  <button onClick={function () { ouvrirEditionEi(ei.id, ei.description) }} className="text-steel-light hover:text-signature">Modifier</button>
                  <button onClick={function () { supprimerEi(ei.id) }} className="text-steel-light hover:text-risk-critical">Suppr.</button>
                </div>
              </li>
            )
          })}
        </ol>
      )}

      {erreur && <p className="mt-1 text-xs text-risk-critical">{erreur}</p>}

      {ajoutEi ? (
        <div className="mt-2 space-y-1.5">
          <select value={ppId} onChange={function (e) { setPpId(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
            {props.parties.map(function (p) { return <option key={p.id} value={p.id}>{p.nom}</option> })}
          </select>
          <input type="text" placeholder="Description de l evenement intermediaire (ex: compromission de l acces distant)" value={descEi} onChange={function (e) { setDescEi(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={ajouterEi} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Ajout...' : 'Ajouter'}</button>
            <button onClick={function () { setAjoutEi(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
          </div>
        </div>
      ) : (
        <button onClick={function () { setAjoutEi(true) }} disabled={props.parties.length === 0} className="mt-2 font-mono text-[10px] font-medium text-signature hover:underline disabled:opacity-40">+ Ajouter une partie prenante traversee</button>
      )}
    </div>
  )
}

function couleurVraisemblance(v: string) {
  if (v === 'V4') return 'text-risk-critical'
  if (v === 'V3') return 'text-risk-high'
  if (v === 'V2') return 'text-risk-moderate'
  return 'text-risk-low'
}

var PROBABILITE_LABELS: { [key: string]: string } = {
  '1': '1 -- Faible (< 10% de reussite)',
  '2': '2 -- Significative (> 10%)',
  '3': '3 -- Tres elevee (> 40%)',
  '4': '4 -- Quasi-certaine (> 90%)',
}
var DIFFICULTE_LABELS: { [key: string]: string } = {
  '1': '1 -- Faible',
  '2': '2 -- Moderee',
  '3': '3 -- Elevee',
  '4': '4 -- Tres elevee',
}

function ScenariosOperationnelsSection(props: {
  etudeId: string
  scenarios: ScenarioStrategique[]
  couples: CoupleSourceRisqueObjectifVise[]
  chemins: CheminAttaque[]
  scenariosOperationnels: ScenarioOperationnel[]
  biens: BienSupport[]
  onChange: () => void
}) {
  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">SCENARIOS OPERATIONNELS ({props.scenariosOperationnels.length})</h2>
      <p className="mb-4 text-xs text-steel">1 chemin d attaque stratégique =&gt; 1 scenario operationnel decrivant eventuellement plusieurs modes operatoires techniques (sequence CONNAITRE / RENTRER / TROUVER / EXPLOITER). La vraisemblance globale du scenario est la plus vraisemblable de ses modes operatoires.</p>
      {props.scenarios.length === 0 ? (
        <p className="text-xs text-steel">Aucun scenario stratégique -- rien a traiter ici pour l instant.</p>
      ) : (
        <div className="space-y-8">
          {props.scenarios.map(function (scenario) {
            var couple = props.couples.filter(function (c) { return c.id === scenario.coupleSourceRisqueObjectifViseId })[0]
            var chemins = props.chemins.filter(function (c) { return c.scenarioStrategiqueId === scenario.id })
            if (chemins.length === 0) return null
            return (
              <div key={scenario.id}>
                <h3 className="mb-3 text-sm font-medium text-ink">{couple ? libelleCouple(couple) : scenario.description}</h3>
                <div className="space-y-4">
                  {chemins.map(function (chemin) {
                    var scenarioOp = props.scenariosOperationnels.filter(function (s) { return s.cheminAttaqueId === chemin.id })[0]
                    return <OperationnelParChemin key={chemin.id} etudeId={props.etudeId} chemin={chemin} scenarioOperationnel={scenarioOp} biens={props.biens} onChange={props.onChange} />
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}

function OperationnelParChemin(props: { etudeId: string; chemin: CheminAttaque; scenarioOperationnel?: ScenarioOperationnel; biens: BienSupport[]; onChange: () => void }) {
  var [enCours, setEnCours] = useState(false)
  var [erreur, setErreur] = useState('')

  function creer() {
    setEnCours(true)
    setErreur('')
    createScenarioOperationnel(props.etudeId, props.chemin.id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function supprimer() {
    if (!props.scenarioOperationnel) return
    if (!window.confirm('Supprimer ce scenario operationnel et ses modes operatoires ?')) return
    deleteScenarioOperationnel(props.etudeId, props.scenarioOperationnel.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div className="border border-paper-line p-4">
      <div className="mb-2 flex items-center justify-between gap-4">
        <span className="text-sm text-ink">{props.chemin.description}</span>
        {props.scenarioOperationnel && props.scenarioOperationnel.vraisemblanceGlobale && (
          <span className={'shrink-0 font-mono text-[11px] font-medium ' + couleurVraisemblance(props.scenarioOperationnel.vraisemblanceGlobale)}>Vraisemblance {props.scenarioOperationnel.vraisemblanceGlobale}</span>
        )}
      </div>

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      {!props.scenarioOperationnel ? (
        <button onClick={creer} disabled={enCours} className="font-mono text-[11px] font-medium text-signature hover:underline">{enCours ? 'Creation...' : '+ Creer le scenario operationnel'}</button>
      ) : (
        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.scenarioOperationnel.modesOperatoires.length} MODE(S) OPERATOIRE(S)</span>
            <button onClick={supprimer} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr. le scenario</button>
          </div>
          <div className="space-y-3">
            {props.scenarioOperationnel.modesOperatoires.map(function (mode) {
              return <ModeOperatoireRow key={mode.id} etudeId={props.etudeId} scenarioOperationnelId={props.scenarioOperationnel!.id} mode={mode} biens={props.biens} onChange={props.onChange} />
            })}
          </div>
          <AjoutModeOperatoire etudeId={props.etudeId} scenarioOperationnelId={props.scenarioOperationnel.id} biens={props.biens} onChange={props.onChange} />
        </div>
      )}
    </div>
  )
}

var LIBELLE_PHASE: { [key: string]: string } = { Connaitre: 'CONNAITRE', Rentrer: 'RENTRER', Trouver: 'TROUVER', Exploiter: 'EXPLOITER' }

function libelleBienSupport(biens: BienSupport[], bienSupportId: string) {
  var bien = biens.filter(function (b) { return b.id === bienSupportId })[0]
  return bien ? bien.description : '(bien support introuvable)'
}

// Une ligne par action elementaire : phase, description, bien support cible.
// Au moins une action est obligatoire (methodologie EBIOS RM), donc la
// derniere ligne restante ne peut pas etre supprimee.
function ActionElementaireListEditor(props: { actions: ActionElementaireInput[]; biens: BienSupport[]; onChange: (actions: ActionElementaireInput[]) => void }) {
  function modifier(index: number, champ: keyof ActionElementaireInput, valeur: string) {
    var copie = props.actions.slice()
    copie[index] = { ...copie[index], [champ]: valeur }
    props.onChange(copie)
  }

  function ajouterLigne() {
    var bienParDefaut = props.biens.length > 0 ? props.biens[0].id : ''
    props.onChange(props.actions.concat([{ description: '', phase: 'Connaitre', bienSupportId: bienParDefaut }]))
  }

  function supprimerLigne(index: number) {
    if (props.actions.length <= 1) return
    props.onChange(props.actions.filter(function (_, i) { return i !== index }))
  }

  return (
    <div className="space-y-1.5">
      <span className="font-mono text-[10px] tracking-wide text-steel-light">ACTIONS ELEMENTAIRES (cible un bien support precis)</span>
      {props.actions.map(function (a, index) {
        return (
          <div key={index} className="grid grid-cols-[100px_1fr_1fr_auto] items-center gap-2">
            <select value={a.phase} onChange={function (e) { modifier(index, 'phase', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {PHASES_ACTION_ELEMENTAIRE.map(function (p) { return <option key={p} value={p}>{LIBELLE_PHASE[p]}</option> })}
            </select>
            <input type="text" placeholder="Description de l action" value={a.description} onChange={function (e) { modifier(index, 'description', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <select value={a.bienSupportId} onChange={function (e) { modifier(index, 'bienSupportId', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {props.biens.length === 0 && <option value="">Aucun bien support</option>}
              {props.biens.map(function (b) { return <option key={b.id} value={b.id}>{b.description}</option> })}
            </select>
            <button type="button" onClick={function () { supprimerLigne(index) }} disabled={props.actions.length <= 1} className="text-[11px] text-steel-light hover:text-risk-critical disabled:opacity-30">×</button>
          </div>
        )
      })}
      <button type="button" onClick={ajouterLigne} className="font-mono text-[10px] font-medium text-signature hover:underline">+ Action elementaire</button>
    </div>
  )
}

function ModeOperatoireRow(props: { etudeId: string; scenarioOperationnelId: string; mode: ModeOperatoire; biens: BienSupport[]; onChange: () => void }) {
  var m = props.mode
  var [edition, setEdition] = useState(false)
  var [description, setDescription] = useState(m.description)
  var [actions, setActions] = useState<ActionElementaireInput[]>(m.actionsElementaires.map(function (a) {
    return { description: a.description, phase: a.phase, bienSupportId: a.bienSupportId }
  }))
  var [probabiliteSucces, setProbabiliteSucces] = useState(String(m.probabiliteSucces))
  var [difficulteTechnique, setDifficulteTechnique] = useState(String(m.difficulteTechnique))
  var [erreur, setErreur] = useState('')

  function sauvegarder() {
    if (!description.trim()) return
    if (actions.length === 0) { setErreur('Au moins une action elementaire est requise.'); return }
    var input: ModeOperatoireInput = {
      description: description, actions: actions,
      probabiliteSucces: Number(probabiliteSucces), difficulteTechnique: Number(difficulteTechnique),
    }
    modifierModeOperatoire(props.etudeId, props.scenarioOperationnelId, m.id, input)
      .then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer() {
    if (!window.confirm('Supprimer ce mode operatoire ?')) return
    supprimerModeOperatoire(props.etudeId, props.scenarioOperationnelId, m.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  if (edition) {
    return (
      <div className="border-l-2 border-signature space-y-1.5 pl-3">
        <input type="text" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
        <ActionElementaireListEditor actions={actions} biens={props.biens} onChange={setActions} />
        <div className="grid grid-cols-2 gap-2">
          <select value={probabiliteSucces} onChange={function (e) { setProbabiliteSucces(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{PROBABILITE_LABELS[v]}</option> })}
          </select>
          <select value={difficulteTechnique} onChange={function (e) { setDifficulteTechnique(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{DIFFICULTE_LABELS[v]}</option> })}
          </select>
        </div>
        <GrilleMatrice matrice={MATRICE_VRAISEMBLANCE} ligneLabels={['1 -- Faible', '2 -- Significative', '3 -- Tres elevee', '4 -- Quasi-certaine']} colonneLabels={['1 -- Faible', '2 -- Moderee', '3 -- Elevee', '4 -- Tres elevee']} ligneTitre="Probabilite" colonneTitre="Difficulte" ligneSelectionnee={Number(probabiliteSucces) - 1} colonneSelectionnee={Number(difficulteTechnique) - 1} couleurCellule={couleurVraisemblance} />
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
        </div>
      </div>
    )
  }

  var actionsParPhase = PHASES_ACTION_ELEMENTAIRE.map(function (phase) {
    return { phase: phase, actions: m.actionsElementaires.filter(function (a) { return a.phase === phase }) }
  })

  return (
    <div className="border-l-2 border-paper-line pl-3">
      <div className="flex items-center justify-between gap-4">
        <span className="text-sm text-ink">{m.description}</span>
        <div className="flex shrink-0 items-center gap-3">
          <span className={'font-mono text-[11px] font-medium ' + couleurVraisemblance(m.vraisemblance)}>{m.vraisemblance}</span>
          <button onClick={function () { setEdition(true) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
          <button onClick={supprimer} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
        </div>
      </div>
      <div className="mt-1 grid grid-cols-2 gap-x-4 gap-y-1 font-mono text-[10px] text-steel-light lg:grid-cols-4">
        {actionsParPhase.map(function (g) {
          return (
            <div key={g.phase}>
              <span className="text-steel">{LIBELLE_PHASE[g.phase]}</span>
              {g.actions.length === 0
                ? ' --'
                : g.actions.map(function (a, i) { return <div key={i}>{a.description} → {libelleBienSupport(props.biens, a.bienSupportId)}</div> })}
            </div>
          )
        })}
      </div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">Probabilite {m.probabiliteSucces} -- Difficulte {m.difficulteTechnique}</div>
      <div className="mt-1.5">
        <OverrideJugementExpert
          valeurCalculee={m.vraisemblanceCalculee}
          valeurRetenue={m.vraisemblanceRetenue}
          justification={m.justificationVraisemblance}
          options={[{ value: 'V1', label: 'V1' }, { value: 'V2', label: 'V2' }, { value: 'V3', label: 'V3' }, { value: 'V4', label: 'V4' }]}
          onDefinir={function (v, j) { return definirVraisemblanceRetenue(props.etudeId, props.scenarioOperationnelId, m.id, v, j).then(props.onChange) }}
          onReinitialiser={function () { return reinitialiserVraisemblance(props.etudeId, props.scenarioOperationnelId, m.id).then(props.onChange) }}
        />
      </div>
    </div>
  )
}

function AjoutModeOperatoire(props: { etudeId: string; scenarioOperationnelId: string; biens: BienSupport[]; onChange: () => void }) {
  var [ouvert, setOuvert] = useState(false)
  var [description, setDescription] = useState('')
  var [actions, setActions] = useState<ActionElementaireInput[]>([])
  var [probabiliteSucces, setProbabiliteSucces] = useState('2')
  var [difficulteTechnique, setDifficulteTechnique] = useState('2')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function ouvrir() {
    setOuvert(true)
    if (actions.length === 0) {
      setActions([{ description: '', phase: 'Connaitre', bienSupportId: props.biens.length > 0 ? props.biens[0].id : '' }])
    }
  }

  function creer() {
    if (!description.trim()) {
      setErreur('La description est obligatoire.')
      return
    }
    if (actions.length === 0 || actions.some(function (a) { return !a.description.trim() || !a.bienSupportId })) {
      setErreur('Chaque action elementaire doit avoir une description et un bien support cible.')
      return
    }
    setEnCours(true)
    setErreur('')
    var input: ModeOperatoireInput = {
      description: description, actions: actions,
      probabiliteSucces: Number(probabiliteSucces), difficulteTechnique: Number(difficulteTechnique),
    }
    ajouterModeOperatoire(props.etudeId, props.scenarioOperationnelId, input)
      .then(function () {
        setDescription(''); setActions([])
        setOuvert(false); props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  if (!ouvert) {
    return <button onClick={ouvrir} className="mt-3 font-mono text-[10px] font-medium text-signature hover:underline">+ Ajouter un mode operatoire</button>
  }

  return (
    <div className="mt-3 space-y-1.5 border-l-2 border-signature pl-3">
      <input type="text" placeholder="Description du mode operatoire" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
      <ActionElementaireListEditor actions={actions} biens={props.biens} onChange={setActions} />
      <div className="grid grid-cols-2 gap-2">
        <select value={probabiliteSucces} onChange={function (e) { setProbabiliteSucces(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
          {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{PROBABILITE_LABELS[v]}</option> })}
        </select>
        <select value={difficulteTechnique} onChange={function (e) { setDifficulteTechnique(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
          {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{DIFFICULTE_LABELS[v]}</option> })}
        </select>
      </div>
      <GrilleMatrice matrice={MATRICE_VRAISEMBLANCE} ligneLabels={['1 -- Faible', '2 -- Significative', '3 -- Tres elevee', '4 -- Quasi-certaine']} colonneLabels={['1 -- Faible', '2 -- Moderee', '3 -- Elevee', '4 -- Tres elevee']} ligneTitre="Probabilite" colonneTitre="Difficulte" ligneSelectionnee={Number(probabiliteSucces) - 1} colonneSelectionnee={Number(difficulteTechnique) - 1} couleurCellule={couleurVraisemblance} />
      {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
      <div className="flex gap-3">
        <button onClick={creer} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Ajout...' : 'Ajouter'}</button>
        <button onClick={function () { setOuvert(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
      </div>
    </div>
  )
}

// ============================= ATELIER 5 -- Traitement du risque =============================

function couleurNiveauRisque(niveau?: string | null) {
  if (niveau === 'Eleve') return 'text-risk-critical'
  if (niveau === 'Moyen') return 'text-risk-high'
  return 'text-risk-low'
}

var LIBELLE_CLASSE_ACCEPTATION: { [key: string]: string } = {
  AcceptableEnLEtat: 'Acceptable en l etat',
  TolerableSousControle: 'Tolerable sous controle',
  Inacceptable: 'Inacceptable',
}

var OPTIONS_NIVEAU_RISQUE = [
  { value: 'Faible', label: 'Faible' },
  { value: 'Moyen', label: 'Moyen' },
  { value: 'Eleve', label: 'Eleve' },
]

var OPTIONS_VRAISEMBLANCE = [
  { value: 'V1', label: 'V1' }, { value: 'V2', label: 'V2' }, { value: 'V3', label: 'V3' }, { value: 'V4', label: 'V4' },
]

function ScenariosDeRisqueSection(props: {
  etudeId: string
  scenarios: ScenarioStrategique[]
  couples: CoupleSourceRisqueObjectifVise[]
  chemins: CheminAttaque[]
  scenariosOperationnels: ScenarioOperationnel[]
  scenariosDeRisque: ScenarioDeRisque[]
  onChange: () => void
}) {
  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">SCENARIOS DE RISQUE ({props.scenariosDeRisque.length})</h2>
      <p className="mb-4 text-xs text-steel">1 scenario de risque = 1 chemin d attaque + son scenario operationnel. Le niveau initial est integralement derive (Gravite x Vraisemblance), le residuel exige une nouvelle evaluation apres application du plan de traitement.</p>
      {props.scenarios.length === 0 ? (
        <p className="text-xs text-steel">Aucun scenario stratégique -- rien a traiter ici pour l instant.</p>
      ) : (
        <div className="space-y-8">
          {props.scenarios.map(function (scenario) {
            var couple = props.couples.filter(function (c) { return c.id === scenario.coupleSourceRisqueObjectifViseId })[0]
            var chemins = props.chemins.filter(function (c) { return c.scenarioStrategiqueId === scenario.id })
            if (chemins.length === 0) return null
            return (
              <div key={scenario.id}>
                <h3 className="mb-3 text-sm font-medium text-ink">{couple ? libelleCouple(couple) : scenario.description}</h3>
                <div className="space-y-4">
                  {chemins.map(function (chemin) {
                    var scenarioOp = props.scenariosOperationnels.filter(function (s) { return s.cheminAttaqueId === chemin.id })[0]
                    var scenarioDeRisque = props.scenariosDeRisque.filter(function (s) { return s.cheminAttaqueId === chemin.id })[0]
                    return <RisqueParChemin key={chemin.id} etudeId={props.etudeId} chemin={chemin} scenarioOperationnel={scenarioOp} scenarioDeRisque={scenarioDeRisque} onChange={props.onChange} />
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}

function RisqueParChemin(props: { etudeId: string; chemin: CheminAttaque; scenarioOperationnel?: ScenarioOperationnel; scenarioDeRisque?: ScenarioDeRisque; onChange: () => void }) {
  var [enCours, setEnCours] = useState(false)
  var [erreur, setErreur] = useState('')

  if (!props.scenarioOperationnel) return null

  function creer() {
    setEnCours(true)
    setErreur('')
    creerScenarioDeRisque(props.etudeId, props.chemin.id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  if (!props.scenarioDeRisque) {
    return (
      <div className="border border-paper-line p-4">
        <div className="mb-2 text-sm text-ink">{props.chemin.description}</div>
        {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
        <button onClick={creer} disabled={enCours} className="font-mono text-[11px] font-medium text-signature hover:underline">{enCours ? 'Materialisation...' : '+ Materialiser le scenario de risque'}</button>
      </div>
    )
  }

  return <ScenarioDeRisqueCard etudeId={props.etudeId} description={props.chemin.description} scenario={props.scenarioDeRisque} onChange={props.onChange} />
}

function ScenarioDeRisqueCard(props: { etudeId: string; description: string; scenario: ScenarioDeRisque; onChange: () => void }) {
  var s = props.scenario
  var [erreur, setErreur] = useState('')
  var [graviteResiduelle, setGraviteResiduelle] = useState(String(s.graviteResiduelle || s.gravite))
  var [vraisemblanceResiduelle, setVraisemblanceResiduelle] = useState(s.vraisemblanceResiduelle || 'V1')
  var indexVraisemblanceResiduelle = OPTIONS_VRAISEMBLANCE.map(function (o) { return o.value }).indexOf(vraisemblanceResiduelle)

  function supprimer() {
    if (!window.confirm('Supprimer ce scenario de risque ?')) return
    supprimerScenarioDeRisque(props.etudeId, s.id).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function evaluerResiduel() {
    evaluerRisqueResiduel(props.etudeId, s.id, Number(graviteResiduelle), vraisemblanceResiduelle)
      .then(props.onChange)
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div className="border border-paper-line p-4">
      <div className="mb-2 flex items-center justify-between gap-4">
        <div>
          <div className="text-sm text-ink">{props.description}</div>
          <div className="font-mono text-[10px] text-steel-light">{s.libelleCouple}</div>
        </div>
        <button onClick={supprimer} className="shrink-0 text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
      </div>

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      <div className="grid gap-6 border-t border-paper-line pt-3 md:grid-cols-2">
        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <span className="font-mono text-[10px] tracking-wide text-steel-light">NIVEAU INITIAL (derive : gravite {s.gravite} x vraisemblance {s.vraisemblanceInitiale || '?'})</span>
            <span className={'font-mono text-xs font-medium ' + couleurNiveauRisque(s.niveauRisqueInitial)}>{s.niveauRisqueInitial || '--'}</span>
          </div>
          <OverrideJugementExpert
            valeurCalculee={s.niveauRisqueInitialCalcule || ''}
            valeurRetenue={s.niveauRisqueInitialRetenu}
            justification={s.justificationNiveauRisqueInitial}
            options={OPTIONS_NIVEAU_RISQUE}
            onDefinir={function (v, j) { return definirNiveauRisqueInitialRetenue(props.etudeId, s.id, v, j).then(props.onChange) }}
            onReinitialiser={function () { return reinitialiserNiveauRisqueInitial(props.etudeId, s.id).then(props.onChange) }}
          />
        </div>

        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <span className="font-mono text-[10px] tracking-wide text-steel-light">RISQUE RESIDUEL (apres plan de traitement)</span>
            {s.niveauRisqueResiduel && <span className={'font-mono text-xs font-medium ' + couleurNiveauRisque(s.niveauRisqueResiduel)}>{s.niveauRisqueResiduel}</span>}
          </div>
          <div className="grid grid-cols-2 gap-2">
            <select value={graviteResiduelle} onChange={function (e) { setGraviteResiduelle(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>Gravite {v}</option> })}
            </select>
            <select value={vraisemblanceResiduelle} onChange={function (e) { setVraisemblanceResiduelle(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {OPTIONS_VRAISEMBLANCE.map(function (o) { return <option key={o.value} value={o.value}>{o.label}</option> })}
            </select>
          </div>
          <div className="mt-1.5">
            <GrilleMatrice matrice={MATRICE_RISQUE} ligneLabels={['1', '2', '3', '4']} colonneLabels={['V1', 'V2', 'V3', 'V4']} ligneTitre="Gravite" colonneTitre="Vraisemblance" ligneSelectionnee={Number(graviteResiduelle) - 1} colonneSelectionnee={indexVraisemblanceResiduelle} couleurCellule={couleurNiveauRisque} />
          </div>
          <button onClick={evaluerResiduel} className="mt-1.5 font-mono text-[10px] font-medium text-signature hover:underline">Evaluer le risque residuel</button>

          {s.niveauRisqueResiduel && (
            <>
              <div className="mt-1.5">
                <OverrideJugementExpert
                  valeurCalculee={s.niveauRisqueResiduelCalcule || ''}
                  valeurRetenue={s.niveauRisqueResiduelRetenu}
                  justification={s.justificationNiveauRisqueResiduel}
                  options={OPTIONS_NIVEAU_RISQUE}
                  onDefinir={function (v, j) { return definirNiveauRisqueResiduelRetenue(props.etudeId, s.id, v, j).then(props.onChange) }}
                  onReinitialiser={function () { return reinitialiserNiveauRisqueResiduel(props.etudeId, s.id).then(props.onChange) }}
                />
              </div>
              <div className="mt-1 font-mono text-[10px] text-steel-light">Classe d acceptation : {LIBELLE_CLASSE_ACCEPTATION[s.classeAcceptationResiduelle || ''] || '--'}</div>
            </>
          )}
        </div>
      </div>

      {s.niveauRisqueResiduel && <AcceptationFormelleSection etudeId={props.etudeId} scenario={s} onChange={props.onChange} />}
    </div>
  )
}

function AcceptationFormelleSection(props: { etudeId: string; scenario: ScenarioDeRisque; onChange: () => void }) {
  var s = props.scenario
  var [proprietaire, setProprietaire] = useState('')
  var [validateur, setValidateur] = useState('')
  var [sponsor, setSponsor] = useState('')
  var [justification, setJustification] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  var risqueEleve = s.niveauRisqueResiduel === 'Eleve'

  function accepter() {
    if (!proprietaire.trim() || !validateur.trim()) {
      setErreur('Le proprietaire du risque et le validateur securite sont obligatoires.')
      return
    }
    if (risqueEleve && (!sponsor.trim() || !justification.trim())) {
      setErreur('Un risque residuel eleve exige un sponsor executif et une justification ecrite.')
      return
    }
    setEnCours(true)
    setErreur('')
    accepterRisqueResiduel(props.etudeId, s.id, proprietaire, validateur, sponsor || undefined, justification || undefined)
      .then(props.onChange)
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  function retirer() {
    if (!window.confirm('Retirer l acceptation formelle de ce risque residuel ?')) return
    retirerAcceptation(props.etudeId, s.id).then(props.onChange)
  }

  return (
    <div className="mt-4 border-t border-paper-line pt-3">
      <span className="font-mono text-[10px] tracking-wide text-steel-light">ACCEPTATION FORMELLE DU RISQUE RESIDUEL PAR LA DIRECTION</span>
      {s.accepteParDirection ? (
        <div className="mt-1.5 space-y-1 text-xs text-ink">
          <div>Proprietaire du risque : <span className="font-medium">{s.nomProprietaireRisque}</span></div>
          <div>Validateur securite : <span className="font-medium">{s.nomValidateurSecurite}</span></div>
          {s.nomSponsorExecutif && <div>Sponsor executif : <span className="font-medium">{s.nomSponsorExecutif}</span></div>}
          {s.justificationAcceptation && <div className="italic text-steel">{s.justificationAcceptation}</div>}
          {s.dateAcceptationUtc && <div className="font-mono text-[10px] text-steel-light">Accepte le {new Date(s.dateAcceptationUtc).toLocaleDateString('fr-FR')}</div>}
          <button onClick={retirer} className="text-[11px] text-steel-light hover:text-risk-critical">Retirer l acceptation</button>
        </div>
      ) : (
        <div className="mt-1.5 space-y-1.5">
          <div className="grid grid-cols-2 gap-2">
            <input type="text" placeholder="Proprietaire du risque" value={proprietaire} onChange={function (e) { setProprietaire(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <input type="text" placeholder="Validateur securite" value={validateur} onChange={function (e) { setValidateur(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
          </div>
          {risqueEleve && (
            <>
              <input type="text" placeholder="Sponsor executif (obligatoire, risque eleve)" value={sponsor} onChange={function (e) { setSponsor(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <textarea placeholder="Justification (obligatoire, risque eleve)" value={justification} onChange={function (e) { setJustification(e.target.value) }} rows={2} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            </>
          )}
          {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
          <button onClick={accepter} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Enregistrement...' : 'Accepter formellement'}</button>
        </div>
      )}
    </div>
  )
}

var AXES_MESURE = ['Gouvernance', 'Protection', 'Defense', 'Resilience']
var LIBELLE_COUT_COMPLEXITE: { [key: string]: string } = { Plus: '+', PlusPlus: '++', PlusPlusPlus: '+++' }
var LIBELLE_STATUT_MESURE: { [key: string]: string } = { ALancer: 'A lancer', EnCours: 'En cours', Termine: 'Termine' }

function PlanTraitementRisqueSection(props: { etudeId: string; plan: PlanTraitementRisque | null; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void }) {
  var [enCours, setEnCours] = useState(false)
  var [erreur, setErreur] = useState('')

  function creerPlan() {
    setEnCours(true)
    setErreur('')
    creerPlanTraitementRisque(props.etudeId).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') }).finally(function () { setEnCours(false) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">PLAN DE TRAITEMENT DU RISQUE {props.plan ? '(' + props.plan.mesures.length + ' MESURE(S))' : ''}</h2>
      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
      {!props.plan ? (
        <button onClick={creerPlan} disabled={enCours} className="font-mono text-[11px] font-medium text-signature hover:underline">{enCours ? 'Creation...' : '+ Creer le plan de traitement du risque'}</button>
      ) : (
        <div className="space-y-6">
          {AXES_MESURE.map(function (axe) {
            var mesuresAxe = props.plan!.mesures.filter(function (m) { return m.axe === axe })
            return (
              <div key={axe}>
                <h3 className="mb-2 font-mono text-[10px] tracking-wide text-steel">{axe.toUpperCase()}</h3>
                {mesuresAxe.length === 0 ? (
                  <p className="text-xs text-steel">Aucune mesure.</p>
                ) : (
                  <div className="space-y-2">
                    {mesuresAxe.map(function (m) {
                      return <MesureTraitementRisqueRow key={m.id} etudeId={props.etudeId} mesure={m} scenariosDeRisque={props.scenariosDeRisque} onChange={props.onChange} />
                    })}
                  </div>
                )}
              </div>
            )
          })}
          <AjoutMesureTraitementRisque etudeId={props.etudeId} scenariosDeRisque={props.scenariosDeRisque} onChange={props.onChange} />
        </div>
      )}
    </section>
  )
}

function libellesScenarios(scenariosDeRisque: ScenarioDeRisque[], ids: string[]) {
  return ids.map(function (id) {
    var s = scenariosDeRisque.filter(function (sc) { return sc.id === id })[0]
    return s ? s.libelleCouple + ' -- ' + s.libelleChemin : '(scenario supprime)'
  })
}

function SelectionScenariosDeRisque(props: { scenariosDeRisque: ScenarioDeRisque[]; selection: string[]; onChange: (ids: string[]) => void }) {
  function basculer(id: string) {
    if (props.selection.indexOf(id) >= 0) {
      props.onChange(props.selection.filter(function (s) { return s !== id }))
    } else {
      props.onChange(props.selection.concat([id]))
    }
  }

  if (props.scenariosDeRisque.length === 0) {
    return <p className="text-xs text-steel">Aucun scenario de risque materialise pour l instant.</p>
  }

  return (
    <div className="space-y-1 border border-paper-line p-2">
      {props.scenariosDeRisque.map(function (s) {
        return (
          <label key={s.id} className="flex items-center gap-2 text-xs text-ink">
            <input type="checkbox" checked={props.selection.indexOf(s.id) >= 0} onChange={function () { basculer(s.id) }} />
            {s.libelleCouple} -- {s.libelleChemin}
          </label>
        )
      })}
    </div>
  )
}

function MesureTraitementRisqueRow(props: { etudeId: string; mesure: MesureTraitementRisque; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void }) {
  var m = props.mesure
  var [edition, setEdition] = useState(false)
  var [description, setDescription] = useState(m.description)
  var [axe, setAxe] = useState(m.axe)
  var [scenariosIds, setScenariosIds] = useState<string[]>(m.scenariosDeRisqueIds)
  var [responsable, setResponsable] = useState(m.responsable)
  var [freins, setFreins] = useState(m.freinsEtDifficultes || '')
  var [coutComplexite, setCoutComplexite] = useState(m.coutComplexite)
  var [echeance, setEcheance] = useState(m.echeance || '')
  var [statut, setStatut] = useState(m.statut)
  var [erreur, setErreur] = useState('')

  function sauvegarder() {
    if (!description.trim() || !responsable.trim() || scenariosIds.length === 0) {
      setErreur('Description, responsable et au moins un scenario de risque sont obligatoires.')
      return
    }
    var input: MesureTraitementRisqueInput = {
      description: description, axe: axe, scenariosDeRisqueIds: scenariosIds, responsable: responsable,
      freinsEtDifficultes: freins || null, coutComplexite: coutComplexite, echeance: echeance || null, statut: statut,
    }
    modifierMesureTraitementRisque(props.etudeId, m.id, input).then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer() {
    if (!window.confirm('Supprimer cette mesure ?')) return
    supprimerMesureTraitementRisque(props.etudeId, m.id).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  if (edition) {
    return (
      <div className="space-y-1.5 border-l-2 border-signature pl-3">
        <input type="text" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
        <div className="grid grid-cols-3 gap-2">
          <select value={axe} onChange={function (e) { setAxe(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {AXES_MESURE.map(function (a) { return <option key={a} value={a}>{a}</option> })}
          </select>
          <select value={coutComplexite} onChange={function (e) { setCoutComplexite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {Object.keys(LIBELLE_COUT_COMPLEXITE).map(function (c) { return <option key={c} value={c}>{LIBELLE_COUT_COMPLEXITE[c]}</option> })}
          </select>
          <select value={statut} onChange={function (e) { setStatut(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {Object.keys(LIBELLE_STATUT_MESURE).map(function (s) { return <option key={s} value={s}>{LIBELLE_STATUT_MESURE[s]}</option> })}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <input type="text" placeholder="Responsable" value={responsable} onChange={function (e) { setResponsable(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
          <input type="text" placeholder="Echeance (ex. 6 mois)" value={echeance} onChange={function (e) { setEcheance(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
        </div>
        <input type="text" placeholder="Freins et difficultes" value={freins} onChange={function (e) { setFreins(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
        <SelectionScenariosDeRisque scenariosDeRisque={props.scenariosDeRisque} selection={scenariosIds} onChange={setScenariosIds} />
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
        </div>
      </div>
    )
  }

  return (
    <div className="border-l-2 border-paper-line pl-3">
      <div className="flex items-center justify-between gap-4">
        <span className="text-sm text-ink">{m.description}</span>
        <div className="flex shrink-0 items-center gap-3">
          <span className="font-mono text-[11px] text-steel-light">{LIBELLE_STATUT_MESURE[m.statut]}</span>
          <button onClick={function () { setEdition(true) }} className="text-[11px] text-steel-light hover:text-signature">Modifier</button>
          <button onClick={supprimer} className="text-[11px] text-steel-light hover:text-risk-critical">Suppr.</button>
        </div>
      </div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">Responsable {m.responsable} -- Cout {LIBELLE_COUT_COMPLEXITE[m.coutComplexite]} -- Echeance {m.echeance || '--'}</div>
      {m.freinsEtDifficultes && <div className="mt-0.5 text-[11px] italic text-steel">{m.freinsEtDifficultes}</div>}
      <div className="mt-0.5 text-[11px] text-steel-light">Scenarios : {libellesScenarios(props.scenariosDeRisque, m.scenariosDeRisqueIds).join('; ')}</div>
    </div>
  )
}

function AjoutMesureTraitementRisque(props: { etudeId: string; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void }) {
  var [description, setDescription] = useState('')
  var [axe, setAxe] = useState('Gouvernance')
  var [scenariosIds, setScenariosIds] = useState<string[]>([])
  var [responsable, setResponsable] = useState('')
  var [freins, setFreins] = useState('')
  var [coutComplexite, setCoutComplexite] = useState('Plus')
  var [echeance, setEcheance] = useState('')
  var [statut, setStatut] = useState('ALancer')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(fermer: () => void) {
    if (!description.trim() || !responsable.trim() || scenariosIds.length === 0) {
      setErreur('Description, responsable et au moins un scenario de risque sont obligatoires.')
      return
    }
    setEnCours(true)
    setErreur('')
    var input: MesureTraitementRisqueInput = {
      description: description, axe: axe, scenariosDeRisqueIds: scenariosIds, responsable: responsable,
      freinsEtDifficultes: freins || null, coutComplexite: coutComplexite, echeance: echeance || null, statut: statut,
    }
    ajouterMesureTraitementRisque(props.etudeId, input)
      .then(function () {
        setDescription(''); setScenariosIds([]); setResponsable(''); setFreins(''); setEcheance('')
        fermer(); props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur.') })
      .finally(function () { setEnCours(false) })
  }

  return (
    <InlineForm label="Ajouter une mesure de traitement">
      {function (fermer) {
        return (
          <div className="space-y-1.5">
            <input type="text" placeholder="Description de la mesure" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
            <div className="grid grid-cols-3 gap-2">
              <select value={axe} onChange={function (e) { setAxe(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {AXES_MESURE.map(function (a) { return <option key={a} value={a}>{a}</option> })}
              </select>
              <select value={coutComplexite} onChange={function (e) { setCoutComplexite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {Object.keys(LIBELLE_COUT_COMPLEXITE).map(function (c) { return <option key={c} value={c}>{LIBELLE_COUT_COMPLEXITE[c]}</option> })}
              </select>
              <select value={statut} onChange={function (e) { setStatut(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {Object.keys(LIBELLE_STATUT_MESURE).map(function (s) { return <option key={s} value={s}>{LIBELLE_STATUT_MESURE[s]}</option> })}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <input type="text" placeholder="Responsable" value={responsable} onChange={function (e) { setResponsable(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="Echeance (ex. 6 mois)" value={echeance} onChange={function (e) { setEcheance(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            </div>
            <input type="text" placeholder="Freins et difficultes (optionnel)" value={freins} onChange={function (e) { setFreins(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <SelectionScenariosDeRisque scenariosDeRisque={props.scenariosDeRisque} selection={scenariosIds} onChange={setScenariosIds} />
            {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
            <button onClick={function () { soumettre(fermer) }} disabled={enCours} className="rounded-sm bg-signature px-3 py-1.5 text-xs font-medium text-white hover:bg-signature/90 disabled:opacity-50">{enCours ? 'Ajout...' : 'Ajouter'}</button>
          </div>
        )
      }}
    </InlineForm>
  )
}
