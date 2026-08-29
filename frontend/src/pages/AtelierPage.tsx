import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { LectureSeuleProvider, useLectureSeule } from '../lib/lectureSeule'
import PageHeader from '../components/shared/PageHeader'
import BadgeStatutAtelier from '../components/shared/BadgeStatutAtelier'
import InlineForm from '../components/shared/InlineForm'
import GrilleMatrice from '../components/shared/GrilleMatrice'
import OverrideJugementExpert from '../components/shared/OverrideJugementExpert'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import Button from '../components/shared/Button'
import Badge from '../components/shared/Badge'
import type { CouleurBadge } from '../components/shared/Badge'
import Card from '../components/shared/Card'
import EmptyState from '../components/shared/EmptyState'
import RowActions from '../components/shared/RowActions'
import SelecteurBibliotheque from '../components/shared/SelecteurBibliotheque'
import CartographieAtelier3 from '../components/shared/CartographieAtelier3'
import ChampTechniqueMitre from '../components/shared/ChampTechniqueMitre'
import SelecteurConformite from '../components/shared/SelecteurConformite'
import { toastSucces, toastErreur } from '../lib/toast'
import { MATRICE_VRAISEMBLANCE, MATRICE_PERTINENCE, MATRICE_RISQUE, calculerNiveauDangerosite, determinerZoneDangerosite } from '../lib/calculsEbios'
import {
  getEtude, listValeursMetier, listBiensSupport, listEvenementsRedoutes, getSocleSecurite,
  demarrerAtelier1, validerAtelier1, rouvrirAtelier1,
  demarrerAtelier2, validerAtelier2, rouvrirAtelier2,
  demarrerAtelier3, validerAtelier3, rouvrirAtelier3,
  demarrerAtelier4, validerAtelier4, rouvrirAtelier4,
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
  demarrerAtelier5, validerAtelier5, rouvrirAtelier5,
  listScenariosDeRisque, creerScenarioDeRisque, supprimerScenarioDeRisque,
  definirNiveauRisqueInitialRetenue, reinitialiserNiveauRisqueInitial,
  evaluerRisqueResiduel, definirNiveauRisqueResiduelRetenue, reinitialiserNiveauRisqueResiduel,
  accepterRisqueResiduel, retirerAcceptation,
  getPlanTraitementRisque, creerPlanTraitementRisque,
  ajouterMesureTraitementRisque, modifierMesureTraitementRisque, supprimerMesureTraitementRisque,
  listerSourcesRisqueBiblio, ajouterSourceRisqueBiblio, listerMesuresBiblio, ajouterMesureBiblio,
  listerPartiesPrenantesBiblio, listerValeursMetierBiblio, listerBiensSupportBiblio, listerEvenementsRedoutesBiblio,
  listerModesOperatoiresBiblio, suggererMesuresBiblio, suggererPartiesPrenantesBiblio, suggererModesOperatoiresBiblio,
  ApiError,
} from '../lib/api'
import type {
  Etude, ValeurMetier, BienSupport, EvenementRedoute, SocleSecurite, CoupleSourceRisqueObjectifVise, PartiePrenante,
  ScenarioStrategique, CheminAttaque, ScenarioOperationnel, ModeOperatoire, ModeOperatoireInput, ActionElementaireInput,
  ScenarioDeRisque, PlanTraitementRisque, MesureTraitementRisque, MesureTraitementRisqueInput,
  SourceRisqueBiblio, MesureBiblio,
  PartiePrenanteBiblio, ValeurMetierBiblio, BienSupportBiblio, EvenementRedouteBiblio, ModeOperatoireBiblio,
  PhaseActionElementaire,
} from '../lib/api'
import { PHASES_ACTION_ELEMENTAIRE } from '../lib/api'
import { traduire, langueCourante } from '../lib/i18n'
import { libelle, clesDe, optionsDe } from '../lib/libelles'
import { CATALOGUE_ISO_27001, THEMES_ISO } from '../lib/iso27001'
import type { ControleIso } from '../lib/iso27001'


var TYPES_BIEN_SUPPORT = clesDe('typeBienSupport')
var ETATS_CONFORMITE = clesDe('etatConformite')

// Convertit une classe de couleur texte brute (couleurZone, couleurPertinence,
// couleurGravite, couleurVraisemblance, couleurNiveauRisque -- toutes encore
// utilisees telles quelles par GrilleMatrice.couleurCellule, qui a besoin
// d'une classe Tailwind litterale) vers une cle Badge, pour les usages ou la
// meme valeur est affichee comme pastille plutot que comme texte colore brut.
var COULEUR_BADGE_DEPUIS_CLASSE: { [key: string]: CouleurBadge } = {
  'text-risk-critical': 'risk-critical',
  'text-risk-high': 'risk-high',
  'text-risk-moderate': 'risk-moderate',
  'text-risk-low': 'risk-low',
  'text-steel': 'steel',
  'text-steel-light': 'steel',
}
function badgeCouleur(classeTexte: string): CouleurBadge {
  return COULEUR_BADGE_DEPUIS_CLASSE[classeTexte] || 'steel'
}

/**
 * Bouton « Depuis la bibliotheque » + panneau de selection, a placer en tete
 * d'un formulaire d'ajout. Choisir une entree pre-remplit les champs du
 * formulaire (via onChoisir) ; l'analyste revoit puis valide normalement.
 */
function DepuisBiblio<T extends { id: string }>(props: {
  titre: string
  charger: (q: string) => Promise<T[]>
  rendre: (item: T) => React.ReactNode
  onChoisir: (item: T) => void
  filtres?: { valeur: string; libelle: string }[]
  filtreActif?: string
  onFiltre?: (v: string) => void
}) {
  var [ouvert, setOuvert] = useState(false)
  if (!ouvert) {
    return (
      <button type="button" onClick={function () { setOuvert(true) }} className="mb-2 font-mono text-[10px] text-signature hover:underline">
        {traduire('ap.depuisBiblio')}
      </button>
    )
  }
  return (
    <SelecteurBibliotheque<T>
      titre={props.titre}
      charger={props.charger}
      cle={function (i) { return i.id }}
      rendre={props.rendre}
      filtres={props.filtres}
      filtreActif={props.filtreActif}
      onFiltre={props.onFiltre}
      onChoisir={function (i) { props.onChoisir(i); setOuvert(false) }}
      onFermer={function () { setOuvert(false) }}
    />
  )
}

function metaBiblio(systeme: boolean, ...parts: (string | number | null | undefined | false)[]) {
  return [systeme ? 'catalogue' : 'ma bibliotheque'].concat(parts.filter(Boolean).map(String)).join(' -- ')
}

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
  // Incremente a chaque rechargement -> force le rafraichissement des schemas
  // SVG (cartographie A3) generes cote serveur.
  var [versionDonnees, setVersionDonnees] = useState(0)

  function charger() {
    setChargement(true)
    setVersionDonnees(function (v) { return v + 1 })
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
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleValider() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier1(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleRouvrir() {
    if (!window.confirm(traduire('ap.confirmRouvrir1'))) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier1(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier2() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier2() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier2() {
    if (!window.confirm(traduire('ap.confirmRouvrir2'))) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier2(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier3() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier3() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier3() {
    if (!window.confirm(traduire('ap.confirmRouvrir3'))) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier3(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier4() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier4() {
    setAction('validation')
    setMessageErreur('')
    validerAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier4() {
    if (!window.confirm(traduire('ap.confirmRouvrir4'))) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier4(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleDemarrerAtelier5() {
    setAction('demarrage')
    setMessageErreur('')
    demarrerAtelier5(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleValiderAtelier5() {
    var saisie = window.prompt(traduire('ap.promptCampagne'), '')
    if (saisie === null) return
    var libelle = saisie.trim() || undefined
    setAction('validation')
    setMessageErreur('')
    validerAtelier5(etudeId, libelle).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  function handleRouvrirAtelier5() {
    if (!window.confirm(traduire('ap.confirmRouvrir5'))) {
      return
    }
    setAction('reouverture')
    setMessageErreur('')
    rouvrirAtelier5(etudeId).then(function () { charger() }).catch(function (err) {
      setMessageErreur(err instanceof ApiError ? err.message : traduire('ap.err'))
    }).finally(function () { setAction('') })
  }

  if (chargement) {
    return <div className="px-6 py-10 text-sm lg:px-10 lg:py-14 text-steel">{traduire('ap.chargement')}</div>
  }

  if (!etude) {
    return <div className="px-6 py-10 text-sm lg:px-10 lg:py-14 text-risk-critical">{traduire('ap.introuvable')}</div>
  }

  var nom = traduire('atelier.' + numero + '.nom')
  var estAtelier1 = numero === 1
  var estAtelier2 = numero === 2
  var estAtelier3 = numero === 3
  var estAtelier4 = numero === 4
  var estAtelier5 = numero === 5
  var estVerrouille = !estAtelier1 && !estAtelier2 && !estAtelier3 && !estAtelier4 && !estAtelier5
  var lienRetour = '/etudes/' + etudeId

  var CLASSE_TELECHARGEMENT = 'inline-flex items-center gap-1.5 rounded-sm border border-paper-line px-4 py-2 text-xs font-medium text-ink transition duration-200 ease-premium hover:border-signature hover:text-signature'

  var boutonAction = null
  if (estAtelier1 && etude.statut === 'Brouillon') {
    boutonAction = <Button variante="primary" taille="md" onClick={handleDemarrer} disabled={action !== ''}>{action === 'demarrage' ? traduire('ap.demarrage') : traduire('ap.demarrer')}</Button>
  } else if (estAtelier1 && etude.statut === 'EnCours') {
    boutonAction = <Button variante="primary" taille="md" onClick={handleValider} disabled={action !== ''}>{action === 'validation' ? traduire('ap.validationpts') : traduire('ap.valider')}</Button>
  } else if (estAtelier1 && etude.statut === 'Validee') {
    boutonAction = (
      <>
        <Button variante="danger" taille="md" onClick={handleRouvrir} disabled={action !== ''}>{action === 'reouverture' ? traduire('ap.reouverture') : traduire('ap.rouvrir')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/atelier1'} nomFichier={'rapport-atelier1-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerPdf')}</BoutonTelechargerRapport>
      </>
    )
  }

  var boutonActionAtelier2 = null
  if (etude.statutAtelier2 === 'Brouillon') {
    boutonActionAtelier2 = <Button variante="primary" taille="md" onClick={handleDemarrerAtelier2} disabled={action !== ''}>{action === 'demarrage' ? traduire('ap.demarrage') : traduire('ap.demarrer')}</Button>
  } else if (etude.statutAtelier2 === 'EnCours') {
    boutonActionAtelier2 = <Button variante="primary" taille="md" onClick={handleValiderAtelier2} disabled={action !== ''}>{action === 'validation' ? traduire('ap.validationpts') : traduire('ap.valider')}</Button>
  } else if (etude.statutAtelier2 === 'Validee') {
    boutonActionAtelier2 = (
      <>
        <Button variante="danger" taille="md" onClick={handleRouvrirAtelier2} disabled={action !== ''}>{action === 'reouverture' ? traduire('ap.reouverture') : traduire('ap.rouvrir')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/atelier2'} nomFichier={'rapport-atelier2-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerPdf')}</BoutonTelechargerRapport>
      </>
    )
  }

  var boutonActionAtelier3 = null
  if (etude.statutAtelier3 === 'Brouillon') {
    boutonActionAtelier3 = <Button variante="primary" taille="md" onClick={handleDemarrerAtelier3} disabled={action !== ''}>{action === 'demarrage' ? traduire('ap.demarrage') : traduire('ap.demarrer')}</Button>
  } else if (etude.statutAtelier3 === 'EnCours') {
    boutonActionAtelier3 = <Button variante="primary" taille="md" onClick={handleValiderAtelier3} disabled={action !== ''}>{action === 'validation' ? traduire('ap.validationpts') : traduire('ap.valider')}</Button>
  } else if (etude.statutAtelier3 === 'Validee') {
    boutonActionAtelier3 = (
      <>
        <Button variante="danger" taille="md" onClick={handleRouvrirAtelier3} disabled={action !== ''}>{action === 'reouverture' ? traduire('ap.reouverture') : traduire('ap.rouvrir')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/atelier3'} nomFichier={'rapport-atelier3-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerPdf')}</BoutonTelechargerRapport>
      </>
    )
  }

  var boutonActionAtelier4 = null
  if (etude.statutAtelier4 === 'Brouillon') {
    boutonActionAtelier4 = <Button variante="primary" taille="md" onClick={handleDemarrerAtelier4} disabled={action !== ''}>{action === 'demarrage' ? traduire('ap.demarrage') : traduire('ap.demarrer')}</Button>
  } else if (etude.statutAtelier4 === 'EnCours') {
    boutonActionAtelier4 = <Button variante="primary" taille="md" onClick={handleValiderAtelier4} disabled={action !== ''}>{action === 'validation' ? traduire('ap.validationpts') : traduire('ap.valider')}</Button>
  } else if (etude.statutAtelier4 === 'Validee') {
    boutonActionAtelier4 = (
      <>
        <Button variante="danger" taille="md" onClick={handleRouvrirAtelier4} disabled={action !== ''}>{action === 'reouverture' ? traduire('ap.reouverture') : traduire('ap.rouvrir')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/atelier4'} nomFichier={'rapport-atelier4-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerPdf')}</BoutonTelechargerRapport>
      </>
    )
  }

  var boutonActionAtelier5 = null
  if (etude.statutAtelier5 === 'Brouillon') {
    boutonActionAtelier5 = <Button variante="primary" taille="md" onClick={handleDemarrerAtelier5} disabled={action !== ''}>{action === 'demarrage' ? traduire('ap.demarrage') : traduire('ap.demarrer')}</Button>
  } else if (etude.statutAtelier5 === 'EnCours') {
    boutonActionAtelier5 = (
      <>
        <Button variante="primary" taille="md" onClick={handleValiderAtelier5} disabled={action !== ''}>{action === 'validation' ? traduire('ap.validationpts') : traduire('ap.valider')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/cadre-de-suivi'} nomFichier={'cadre-de-suivi-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerCadre')}</BoutonTelechargerRapport>
      </>
    )
  } else if (etude.statutAtelier5 === 'Validee') {
    boutonActionAtelier5 = (
      <>
        <Button variante="danger" taille="md" onClick={handleRouvrirAtelier5} disabled={action !== ''}>{action === 'reouverture' ? traduire('ap.reouverture') : traduire('ap.rouvrir')}</Button>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/atelier5'} nomFichier={'rapport-atelier5-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerPdf')}</BoutonTelechargerRapport>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/synthese'} nomFichier={'synthese-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerSynthese')}</BoutonTelechargerRapport>
        <BoutonTelechargerRapport path={'/etudes/' + etudeId + '/rapports/cadre-de-suivi'} nomFichier={'cadre-de-suivi-' + etudeId + '.pdf'} className={CLASSE_TELECHARGEMENT}>{traduire('ap.telechargerCadre')}</BoutonTelechargerRapport>
      </>
    )
  }

  var lectureSeule = etude.monRole === 'Lecteur'

  return (
    <LectureSeuleProvider valeur={lectureSeule}>
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader eyebrow={traduire('ap.eyebrowAtelier') + ' ' + (numero < 10 ? '0' + numero : numero) + ' / 05 -- ' + etude.nom} titre={nom} />

      {lectureSeule && (
        <div className="mb-6 inline-block border border-paper-line bg-paper-dim px-3 py-1.5 text-[11px] text-steel">
          {traduire('ap.lectureSeule')}
        </div>
      )}

      {messageErreur && <div className="mb-6 border border-risk-critical/30 bg-risk-critical/5 px-5 py-3 text-xs text-risk-critical">{messageErreur}</div>}

      {estVerrouille && (
        <div className="mb-10 border border-paper-line bg-paper-dim px-5 py-4">
          <p className="text-xs text-steel">{traduire('ap.numeroInvalide')} &mdash; <Link to={'/etudes/' + etudeId} className="text-signature hover:underline">{traduire('ap.retourTdb')}</Link>.</p>
        </div>
      )}

      {estAtelier1 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <BadgeStatutAtelier statut={etude.statut} />
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
            <BadgeStatutAtelier statut={etude.statutAtelier2} />
            <div className="flex gap-2">{boutonActionAtelier2}</div>
          </div>

          <CouplesSrOvSection etudeId={etudeId} couples={couples} onChange={charger} />
        </div>
      )}

      {estAtelier3 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <BadgeStatutAtelier statut={etude.statutAtelier3} />
            <div className="flex gap-2">{boutonActionAtelier3}</div>
          </div>
          <PartiesPrenantesSection etudeId={etudeId} parties={parties} onChange={charger} />
          <EvaluationDangerositeSection etudeId={etudeId} parties={parties} onChange={charger} />
          <MesuresEcosystemeSection etudeId={etudeId} parties={parties} onChange={charger} />
          <ScenariosStrategiquesSection etudeId={etudeId} couples={couples} scenarios={scenarios} evenements={evenements} valeurs={valeurs} onChange={charger} />
          <CheminsAttaqueSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} parties={parties} onChange={charger} />
          <section>
            <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.cartoGraphique')}</h2>
            <CartographieAtelier3 etudeId={etudeId} rafraichir={versionDonnees} />
          </section>
        </div>
      )}

      {estAtelier4 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <BadgeStatutAtelier statut={etude.statutAtelier4} />
            <div className="flex gap-2">{boutonActionAtelier4}</div>
          </div>
          <ScenariosOperationnelsSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} scenariosOperationnels={scenariosOperationnels} biens={biens} onChange={charger} />
        </div>
      )}

      {estAtelier5 && (
        <div className="space-y-10">
          <div className="flex items-center justify-between border-b border-paper-line pb-6">
            <BadgeStatutAtelier statut={etude.statutAtelier5} />
            <div className="flex gap-2">{boutonActionAtelier5}</div>
          </div>
          <ScenariosDeRisqueSection etudeId={etudeId} scenarios={scenarios} couples={couples} chemins={cheminsAttaque} scenariosOperationnels={scenariosOperationnels} scenariosDeRisque={scenariosDeRisque} onChange={charger} />
          <PlanTraitementRisqueSection etudeId={etudeId} plan={planTraitementRisque} scenariosDeRisque={scenariosDeRisque} onChange={charger} />
        </div>
      )}

      <div className="mt-14 border-t border-paper-line pt-6">
        <Link to={lienRetour} className="font-mono text-[11px] text-steel hover:text-signature">{traduire('ap.retourDossier')}</Link>
      </div>
    </div>
    </LectureSeuleProvider>
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
      setErreur(traduire('ap.a1.vmErr'))
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer(id: string) {
    if (!window.confirm(traduire('ap.a1.vmConfirm'))) return
    deleteValeurMetier(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a1.vmTitre')} ({props.valeurs.length})</h2>
      {props.valeurs.length === 0 ? (
        <EmptyState message={traduire('ap.a1.vmVide')} />
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.valeurs.map(function (v) {
            if (idEnEdition === v.id) {
              return (
                <div key={v.id} className="flex items-center gap-2 border-l-2 border-signature py-2 pl-3">
                  <input type="text" value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <input type="text" value={entiteEdit} onChange={function (e) { setEntiteEdit(e.target.value) }} className="w-40 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEdition(v.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
                </div>
              )
            }
            return (
              <div key={v.id} className="flex items-center justify-between py-3">
                <span className="text-sm text-ink">{v.description}</span>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-[11px] text-steel-light">{v.entiteProprietaire}</span>
                  <RowActions onModifier={function () { ouvrirEdition(v) }} onSupprimer={function () { supprimer(v.id) }} />
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label={traduire('ap.a1.vmAdd')}>
        {function (fermer) {
          return (
            <div>
              <DepuisBiblio<ValeurMetierBiblio>
                titre={traduire('ap.a1.vmBiblio')}
                charger={function (q) { return listerValeursMetierBiblio(q) }}
                rendre={function (v) {
                  return (
                    <div>
                      <div className="text-sm text-ink">{v.intitule}</div>
                      <div className="text-[10px] text-steel-light">{metaBiblio(v.systeme, v.natureOuFinalite, v.entiteProprietaireTypique)}</div>
                    </div>
                  )
                }}
                onChoisir={function (v) { setDescription(v.intitule); if (v.entiteProprietaireTypique) setEntite(v.entiteProprietaireTypique) }}
              />
              <input type="text" placeholder={traduire('ap.commun.description')} value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder={traduire('ap.commun.entiteProp')} value={entite} onChange={function (e) { setEntite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
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
  var [filtreBiblioBs, setFiltreBiblioBs] = useState('')

  useEffect(function () {
    if (props.valeurs.length === 0) return
    if (!props.valeurs.some(function (v) { return v.id === valeurMetierId })) {
      setValeurMetierId(props.valeurs[0].id)
    }
  }, [props.valeurs])

  function soumettre(fermer: () => void) {
    if (!valeurMetierId || !description.trim() || !entite.trim()) {
      setErreur(traduire('ap.a1.bsErr'))
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer(id: string) {
    if (!window.confirm(traduire('ap.a1.bsConfirm'))) return
    deleteBienSupport(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a1.bsTitre')} ({props.biens.length})</h2>
      {props.biens.length === 0 ? (
        <EmptyState message={traduire('ap.a1.bsVide')} />
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.biens.map(function (b) {
            if (idEnEdition === b.id) {
              return (
                <div key={b.id} className="flex items-center gap-2 border-l-2 border-signature py-2 pl-3">
                  <input type="text" value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <select value={typeEdit} onChange={function (e) { setTypeEdit(e.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    {TYPES_BIEN_SUPPORT.map(function (t) { return <option key={t} value={t}>{libelle('typeBienSupport', t)}</option> })}
                  </select>
                  <input type="text" value={entiteEdit} onChange={function (e) { setEntiteEdit(e.target.value) }} className="w-32 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEdition(b.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
                </div>
              )
            }
            return (
              <div key={b.id} className="flex items-center justify-between py-3">
                <span className="text-sm text-ink">{b.description}</span>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-[11px] text-steel-light">{libelle('typeBienSupport', b.type) || b.type} - {b.entiteProprietaire}</span>
                  <RowActions onModifier={function () { ouvrirEdition(b) }} onSupprimer={function () { supprimer(b.id) }} />
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label={traduire('ap.a1.bsAdd')}>
        {function (fermer) {
          return (
            <div>
              <DepuisBiblio<BienSupportBiblio>
                titre={traduire('ap.a1.bsBiblio')}
                filtres={[{ valeur: '', libelle: traduire('ap.commun.tous') }].concat(TYPES_BIEN_SUPPORT.map(function (t) { return { valeur: t, libelle: libelle('typeBienSupport', t) } }))}
                filtreActif={filtreBiblioBs}
                onFiltre={setFiltreBiblioBs}
                charger={function (q) { return listerBiensSupportBiblio(filtreBiblioBs, q) }}
                rendre={function (b) {
                  return (
                    <div>
                      <div className="text-sm text-ink">{b.intitule}</div>
                      <div className="text-[10px] text-steel-light">{metaBiblio(b.systeme, libelle('typeBienSupport', b.type) || b.type, b.entiteProprietaireTypique)}</div>
                    </div>
                  )
                }}
                onChoisir={function (b) { setDescription(b.intitule); setType(b.type); if (b.entiteProprietaireTypique) setEntite(b.entiteProprietaireTypique) }}
              />
              <select value={valeurMetierId} onChange={function (e) { setValeurMetierId(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="">{traduire('ap.a1.vmAssociee')}</option>
                {props.valeurs.map(function (v) { return <option key={v.id} value={v.id}>{v.description}</option> })}
              </select>
              <input type="text" placeholder={traduire('ap.commun.description')} value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={type} onChange={function (e) { setType(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {TYPES_BIEN_SUPPORT.map(function (t) { return <option key={t} value={t}>{libelle('typeBienSupport', t)}</option> })}
              </select>
              <input type="text" placeholder={traduire('ap.commun.entiteProp')} value={entite} onChange={function (e) { setEntite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
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

  useEffect(function () {
    if (props.valeurs.length === 0) return
    if (!props.valeurs.some(function (v) { return v.id === valeurMetierId })) {
      setValeurMetierId(props.valeurs[0].id)
    }
  }, [props.valeurs])

  function soumettre(fermer: () => void) {
    if (!valeurMetierId || !description.trim()) {
      setErreur(traduire('ap.a1.erErr'))
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer(id: string) {
    if (!window.confirm(traduire('ap.a1.erConfirm'))) return
    deleteEvenementRedoute(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a1.erTitre')} ({props.evenements.length})</h2>
      {props.evenements.length === 0 ? (
        <EmptyState message={traduire('ap.a1.erVide')} />
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.evenements.map(function (e) {
            if (idEnEdition === e.id) {
              return (
                <div key={e.id} className="flex items-center gap-2 border-l-2 border-signature py-2 pl-3">
                  <input type="text" value={descEdit} onChange={function (ev) { setDescEdit(ev.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <select value={graviteEdit} onChange={function (ev) { setGraviteEdit(ev.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    <option value="1">{traduire('ap.a1.gravite1')}</option><option value="2">{traduire('ap.a1.gravite2')}</option><option value="3">{traduire('ap.a1.gravite3')}</option><option value="4">{traduire('ap.a1.gravite4')}</option>
                  </select>
                  <button onClick={function () { sauvegarderEdition(e.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
                </div>
              )
            }
            return (
              <div key={e.id} className="flex items-start justify-between gap-6 py-3">
                <span className="text-sm text-ink">{e.description}</span>
                <div className="flex shrink-0 items-center gap-3">
                  <Badge couleur="risk-high">{traduire('ap.a1.graviteBadge')} {e.gravite}</Badge>
                  <RowActions onModifier={function () { ouvrirEdition(e) }} onSupprimer={function () { supprimer(e.id) }} />
                </div>
              </div>
            )
          })}
        </div>
      )}

      <InlineForm label={traduire('ap.a1.erAdd')}>
        {function (fermer) {
          return (
            <div>
              <DepuisBiblio<EvenementRedouteBiblio>
                titre={traduire('ap.a1.erBiblio')}
                charger={function (q) { return listerEvenementsRedoutesBiblio(q) }}
                rendre={function (e) {
                  return (
                    <div>
                      <div className="text-sm text-ink">{e.intitule}</div>
                      <div className="text-[10px] text-steel-light">{metaBiblio(e.systeme, e.graviteIndicative && 'G' + e.graviteIndicative, e.impactsTypes)}</div>
                    </div>
                  )
                }}
                onChoisir={function (e) { setDescription(e.intitule); if (e.graviteIndicative) setGravite(String(e.graviteIndicative)) }}
              />
              <select value={valeurMetierId} onChange={function (e) { setValeurMetierId(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="">{traduire('ap.a1.vmAssociee')}</option>
                {props.valeurs.map(function (v) { return <option key={v.id} value={v.id}>{v.description}</option> })}
              </select>
              <input type="text" placeholder={traduire('ap.commun.description')} value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={gravite} onChange={function (e) { setGravite(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                <option value="1">{traduire('ap.a1.gravite1')}</option>
                <option value="2">{traduire('ap.a1.gravite2')}</option>
                <option value="3">{traduire('ap.a1.gravite3')}</option>
                <option value="4">{traduire('ap.a1.gravite4')}</option>
              </select>
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimerRef(id: string) {
    if (!window.confirm(traduire('ap.a1.refConfirm'))) return
    deleteReferentiel(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function creerSocle() {
    setEnCours(true)
    setErreur('')
    createSocleSecurite(props.etudeId)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  function ajouterReferentiel(fermer: () => void) {
    var controle: ControleIso | undefined
    if (mode === 'iso') {
      controle = CATALOGUE_ISO_27001.filter(function (c) { return c.code === controleCode })[0]
      if (!controle) {
        setErreur(traduire('ap.a1.refErrIso'))
        return
      }
    } else if (!nomLibre.trim()) {
      setErreur(traduire('ap.a1.refErrNom'))
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      resultat.push({ theme: traduire('ap.a1.autresRef'), items: sansTheme })
    }
    return resultat
  }

  var groupes = referentielsParTheme()

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a1.socleTitre')}</h2>

      {!props.socle ? (
        <div>
          <EmptyState message={traduire('ap.a1.socleVide')} />
          {erreur && <p className="mb-2 mt-3 text-xs text-risk-critical">{erreur}</p>}
          <div className="mt-3">
            <Button variante="primary" onClick={creerSocle} disabled={enCours}>{enCours ? traduire('ap.creation') : traduire('ap.a1.socleCreer')}</Button>
          </div>
        </div>
      ) : (
        <div>
          {groupes.length === 0 ? (
            <EmptyState message={traduire('ap.a1.ctrlVide')} />
          ) : (
            <div className="space-y-6">
              {groupes.map(function (groupe) {
                return (
                  <div key={groupe.theme}>
                    <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{groupe.theme.toUpperCase()} ({groupe.items.length})</div>
                    <div className="divide-y divide-paper-line border-y border-paper-line">
                      {groupe.items.map(function (r: any) {
                        var couleur: CouleurBadge = r.etat === 'Conforme' ? 'risk-low' : r.etat === 'NonApplicable' ? 'steel' : 'risk-high'
                        if (idRefEnEdition === r.id) {
                          return (
                            <div key={r.id} className="space-y-1.5 border-l-2 border-signature py-2.5 pl-3">
                              <input type="text" value={nomRefEdit} onChange={function (ev) { setNomRefEdit(ev.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                              <div className="flex items-center gap-2">
                                <select value={etatRefEdit} onChange={function (ev) { setEtatRefEdit(ev.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                                  {ETATS_CONFORMITE.map(function (e) { return <option key={e} value={e}>{libelle('etatConformite', e)}</option> })}
                                </select>
                                <input type="text" value={etatActuelRefEdit} onChange={function (ev) { setEtatActuelRefEdit(ev.target.value) }} placeholder={traduire('ap.a1.etatActuel')} className="flex-1 border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                                <button onClick={function () { sauvegarderEditionRef(r.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                                <button onClick={function () { setIdRefEnEdition('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
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
                                <Badge couleur={couleur}>{(libelle('etatConformite', r.etat) || r.etat).toUpperCase()}</Badge>
                                <RowActions onModifier={function () { ouvrirEditionRef(r) }} onSupprimer={function () { supprimerRef(r.id) }} />
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

          <InlineForm label={traduire('ap.a1.ctrlAdd')}>
            {function (fermer) {
              return (
                <div>
                  <div className="mb-3 flex gap-4">
                    <label className="flex items-center gap-1.5 text-xs text-ink">
                      <input type="radio" checked={mode === 'iso'} onChange={function () { setMode('iso') }} />
                      {traduire('ap.a1.ctrlIso')}
                    </label>
                    <label className="flex items-center gap-1.5 text-xs text-ink">
                      <input type="radio" checked={mode === 'libre'} onChange={function () { setMode('libre') }} />
                      {traduire('ap.a1.autreRefRadio')}
                    </label>
                  </div>

                  {mode === 'iso' ? (
                    <select value={controleCode} onChange={function (e) { setControleCode(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                      <option value="">{traduire('ap.a1.choisirCtrl')}</option>
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
                    <input type="text" placeholder={traduire('ap.a1.refNomPh')} value={nomLibre} onChange={function (e) { setNomLibre(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
                  )}

                  <select value={etat} onChange={function (e) { setEtat(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                    {ETATS_CONFORMITE.map(function (e) { return <option key={e} value={e}>{libelle('etatConformite', e)}</option> })}
                  </select>

                  <textarea placeholder={traduire('ap.a1.etatActuelPh')} value={etatActuel} onChange={function (e) { setEtatActuel(e.target.value) }} rows={2} className="mb-3 w-full resize-none border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />

                  {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
                  <Button variante="primary" onClick={function () { ajouterReferentiel(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
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
var THEMES_SR_OV = clesDe('theme')

var CATEGORIES_PP = clesDe('categoriePP')

function libelleCategoriePP(p: { categorie: string; descriptionCategorie?: string | null }) {
  return p.categorie === 'Autre' && p.descriptionCategorie ? p.descriptionCategorie : libelle('categoriePP', p.categorie)
}

function PartiesPrenantesSection(props: { etudeId: string; parties: PartiePrenante[]; onChange: () => void }) {
  var [nom, setNom] = useState('')
  var [roles, setRoles] = useState('')
  var [representant, setRepresentant] = useState('')
  var [categorie, setCategorie] = useState(CATEGORIES_PP[0])
  var [descCategorie, setDescCategorie] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)
  var [graine, setGraine] = useState(0)
  var [idEdit, setIdEdit] = useState('')
  var [nomEdit, setNomEdit] = useState('')
  var [rolesEdit, setRolesEdit] = useState('')
  var [repEdit, setRepEdit] = useState('')
  var [categorieEdit, setCategorieEdit] = useState(CATEGORIES_PP[0])
  var [descCategorieEdit, setDescCategorieEdit] = useState('')

  function soumettre(fermer: () => void) {
    if (!nom.trim() || !roles.trim() || !representant.trim() || (categorie === 'Autre' && !descCategorie.trim())) {
      setErreur(traduire('ap.a2.ppErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    createPartiePrenante(props.etudeId, nom, roles, representant, categorie, descCategorie)
      .then(function () { setNom(''); setRoles(''); setRepresentant(''); setDescCategorie(''); fermer(); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer(id: string) {
    if (!window.confirm(traduire('ap.a2.ppConfirm'))) return
    deletePartiePrenante(props.etudeId, id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a2.ppTitre')} ({props.parties.length})</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a2.ppIntro')}</p>
      {props.parties.length === 0 ? (
        <EmptyState message={traduire('ap.a2.ppVide')} />
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.parties.map(function (p) {
            if (idEdit === p.id) {
              return (
                <div key={p.id} className="space-y-1.5 border-l-2 border-signature py-2.5 pl-3">
                  <input type="text" value={nomEdit} onChange={function (e) { setNomEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <input type="text" value={rolesEdit} onChange={function (e) { setRolesEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                  <div className="flex items-center gap-2">
                    <input type="text" value={repEdit} onChange={function (e) { setRepEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                    <select value={categorieEdit} onChange={function (e) { setCategorieEdit(e.target.value) }} className="border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                      {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{libelle('categoriePP', c)}</option> })}
                    </select>
                  </div>
                  {categorieEdit === 'Autre' && (
                    <input type="text" placeholder={traduire('ap.a2.precisezCat')} value={descCategorieEdit} onChange={function (e) { setDescCategorieEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
                  )}
                  <div className="flex items-center gap-2">
                    <button onClick={function () { sauvegarder(p.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                    <button onClick={function () { setIdEdit('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
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
                    <RowActions onModifier={function () { ouvrirEdition(p) }} onSupprimer={function () { supprimer(p.id) }} />
                  </div>
                </div>
                <div className="mt-1 text-xs text-steel">{p.rolesEtAttentes}</div>
              </div>
            )
          })}
        </div>
      )}
      <PanneauSuggestions<PartiePrenanteBiblio>
        titre={traduire('ap.a2.ppSugg')}
        rafraichir={props.parties.length}
        charger={function () { return suggererPartiesPrenantesBiblio(props.etudeId) }}
        rendre={function (pp) { return { titre: pp.nom, sousTitre: pp.descriptionCategorie || pp.categorie } }}
        onUtiliser={function (pp) {
          setNom(pp.nom); setRoles(pp.rolesEtAttentes)
          if (pp.representant) setRepresentant(pp.representant)
          if (CATEGORIES_PP.indexOf(pp.categorie) >= 0) setCategorie(pp.categorie)
          if (pp.descriptionCategorie) setDescCategorie(pp.descriptionCategorie)
          setGraine(Date.now())
        }}
      />
      <InlineForm label={traduire('ap.a2.ppAdd')} signalOuvrir={graine || undefined}>
        {function (fermer) {
          return (
            <div>
              <DepuisBiblio<PartiePrenanteBiblio>
                titre={traduire('ap.a2.ppBiblio')}
                charger={function (q) { return listerPartiesPrenantesBiblio(q) }}
                rendre={function (pp) {
                  return (
                    <div>
                      <div className="text-sm text-ink">{pp.nom}</div>
                      <div className="text-[10px] text-steel-light">{metaBiblio(pp.systeme, pp.descriptionCategorie || pp.categorie)}</div>
                      <div className="text-[10px] text-steel">{pp.rolesEtAttentes}</div>
                    </div>
                  )
                }}
                onChoisir={function (pp) {
                  setNom(pp.nom); setRoles(pp.rolesEtAttentes)
                  if (pp.representant) setRepresentant(pp.representant)
                  if (['Client', 'Partenaire', 'Prestataire', 'Autre'].indexOf(pp.categorie) >= 0) setCategorie(pp.categorie)
                  if (pp.descriptionCategorie) setDescCategorie(pp.descriptionCategorie)
                }}
              />
              <input type="text" placeholder={traduire('ap.commun.nom')} value={nom} onChange={function (e) { setNom(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder={traduire('ap.a2.rolesPh')} value={roles} onChange={function (e) { setRoles(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder={traduire('ap.a2.representant')} value={representant} onChange={function (e) { setRepresentant(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={categorie} onChange={function (e) { setCategorie(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{libelle('categoriePP', c)}</option> })}
              </select>
              {categorie === 'Autre' && (
                <input type="text" placeholder={traduire('ap.a2.precisezCat')} value={descCategorie} onChange={function (e) { setDescCategorie(e.target.value) }} className="mb-2 w-full border-b border-signature bg-transparent py-1.5 text-sm text-ink focus:outline-none" />
              )}
              {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
              <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
            </div>
          )
        }}
      </InlineForm>
    </section>
  )
}


function couleurPertinence(p: string) {
  if (p === 'TresPertinent') return 'text-risk-critical'
  if (p === 'PlutotPertinent') return 'text-risk-high'
  if (p === 'MoyennementPertinent') return 'text-steel'
  return 'text-steel-light'
}

var OPTIONS_PERTINENCE = optionsDe('pertinence')

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
      setErreur(traduire('ap.a2.coupleErr'))
      return
    }
    updateCoupleSrOv(props.etudeId, c.id, sourceRisque, descSr, objectifVise, descOv, contexte, theme, Number(motivation), Number(ressources))
      .then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer() {
    if (!window.confirm(traduire('ap.a2.coupleConfirm'))) return
    deleteCoupleSrOv(props.etudeId, c.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function versBibliotheque() {
    ajouterSourceRisqueBiblio({
      sourceRisque: c.sourceRisque, descriptionSourceRisque: c.descriptionSourceRisque,
      objectifVise: c.objectifVise, descriptionObjectifVise: c.descriptionObjectifVise,
      theme: c.theme, motivationTypique: c.motivation, ressourcesTypiques: c.ressources,
    })
      .then(function () { toastSucces(traduire('ap.a2.srVersBiblio')) })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  if (edition) {
    return (
      <div className="border-l-2 border-signature space-y-1.5 py-2.5 pl-3">
        <select value={sourceRisque} onChange={function (e) { setSourceRisque(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {CATEGORIES_SR.map(function (cc) { return <option key={cc} value={cc}>{libelle('categorieSR', cc)}</option> })}
        </select>
        <input type="text" value={descSr} onChange={function (e) { setDescSr(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <select value={objectifVise} onChange={function (e) { setObjectifVise(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {CATEGORIES_OV.map(function (cc) { return <option key={cc} value={cc}>{libelle('categorieOV', cc)}</option> })}
        </select>
        <input type="text" value={descOv} onChange={function (e) { setDescOv(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <textarea value={contexte} onChange={function (e) { setContexte(e.target.value) }} rows={2} className="w-full resize-none border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
        <select value={theme} onChange={function (e) { setTheme(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none">
          {THEMES_SR_OV.map(function (t) { return <option key={t} value={t}>{libelle('theme', t)}</option> })}
        </select>
        <div className="flex gap-3">
          <select value={motivation} onChange={function (e) { setMotivation(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('motivation', v)}</option> })}
          </select>
          <select value={ressources} onChange={function (e) { setRessources(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('ressources', v)}</option> })}
          </select>
        </div>
        <GrilleMatrice matrice={MATRICE_PERTINENCE} ligneLabels={traduire('ap.mx.motiv').split('|')} colonneLabels={traduire('ap.mx.ress').split('|')} ligneTitre={traduire('ap.a2.motivation')} colonneTitre={traduire('ap.a2.ressources')} ligneSelectionnee={Number(motivation) - 1} colonneSelectionnee={Number(ressources) - 1} couleurCellule={couleurPertinence} />
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
        </div>
      </div>
    )
  }

  return (
    <div className="py-2.5">
      <div className="flex items-center justify-between gap-6">
        <span className="text-sm text-ink">{libelleCouple(c)}</span>
        <div className="flex shrink-0 items-center gap-3">
          <Badge couleur={badgeCouleur(couleurPertinence(c.pertinence))}>{libelle('pertinence', c.pertinence) || c.pertinence}</Badge>
          <button onClick={versBibliotheque} title={traduire('ap.ajouterMaBiblio')} className="text-[11px] text-steel-light hover:text-signature">{traduire('ap.versBiblioLien')}</button>
          <RowActions onModifier={function () { setEdition(true) }} onSupprimer={supprimer} />
        </div>
      </div>
      <div className="mt-1 text-xs text-steel">{c.contexteVulnerabilite}</div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">{traduire('ap.a2.motivationLbl')} {c.motivation} -- {traduire('ap.a2.ressourcesLbl')} {c.ressources}</div>
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

  var [selecteurBiblio, setSelecteurBiblio] = useState(false)

  function soumettre(fermer: () => void) {
    if (!descSr.trim() || !descOv.trim() || !contexte.trim()) {
      setErreur(traduire('ap.a2.coupleErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    createCoupleSrOv(props.etudeId, sourceRisque, descSr, objectifVise, descOv, contexte, theme, Number(motivation), Number(ressources))
      .then(function () { setDescSr(''); setDescOv(''); setContexte(''); fermer(); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  var groupesTheme = THEMES_SR_OV.map(function (t) {
    return { theme: t, items: props.couples.filter(function (c) { return c.theme === t }) }
  })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a2.couplesTitre')} ({props.couples.length})</h2>

      {props.couples.length === 0 ? (
        <EmptyState message={traduire('ap.a2.coupleVide')} />
      ) : (
        <div className="space-y-6">
          {groupesTheme.map(function (g) {
            if (g.items.length === 0) {
              return (
                <div key={g.theme}>
                  <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{g.theme.toUpperCase()} (0)</div>
                  <EmptyState message={traduire('ap.a2.coupleVideTheme')} />
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

      <InlineForm label={traduire('ap.a2.coupleAdd')}>
        {function (fermer) {
          return (
            <div>
              {!selecteurBiblio ? (
                <button type="button" onClick={function () { setSelecteurBiblio(true) }} className="mb-2 font-mono text-[10px] text-signature hover:underline">
                  {traduire('ap.depuisBiblio')}
                </button>
              ) : (
                <SelecteurBibliotheque<SourceRisqueBiblio>
                  titre={traduire('ap.a2.srBiblioTitre')}
                  charger={function (q) { return listerSourcesRisqueBiblio(q) }}
                  cle={function (s) { return s.id }}
                  rendre={function (s) {
                    return (
                      <>
                        <div className="font-medium">{s.descriptionSourceRisque} &rarr; {s.descriptionObjectifVise}</div>
                        <div className="text-[10px] text-steel-light">{s.theme || '--'}{s.systeme ? ' -- catalogue' : ' -- ma bibliotheque'}</div>
                      </>
                    )
                  }}
                  onChoisir={function (s) {
                    setSourceRisque(s.sourceRisque)
                    setDescSr(s.descriptionSourceRisque)
                    setObjectifVise(s.objectifVise)
                    setDescOv(s.descriptionObjectifVise)
                    if (s.theme && THEMES_SR_OV.indexOf(s.theme) >= 0) setTheme(s.theme)
                    if (s.motivationTypique) setMotivation(String(s.motivationTypique))
                    if (s.ressourcesTypiques) setRessources(String(s.ressourcesTypiques))
                    setSelecteurBiblio(false)
                  }}
                  onFermer={function () { setSelecteurBiblio(false) }}
                />
              )}
              <select value={sourceRisque} onChange={function (e) { setSourceRisque(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_SR.map(function (c) { return <option key={c} value={c}>{libelle('categorieSR', c)}</option> })}
              </select>
              <input type="text" placeholder={sourceRisque === 'Autre' ? traduire('ap.a2.precisezSr') : traduire('ap.a2.descSr')} value={descSr} onChange={function (e) { setDescSr(e.target.value) }} className={'mb-2 w-full bg-transparent py-1.5 text-sm text-ink focus:outline-none ' + (sourceRisque === 'Autre' ? 'border-b border-signature' : 'border-b border-paper-line focus:border-signature')} />
              <select value={objectifVise} onChange={function (e) { setObjectifVise(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {CATEGORIES_OV.map(function (c) { return <option key={c} value={c}>{libelle('categorieOV', c)}</option> })}
              </select>
              <input type="text" placeholder={objectifVise === 'Autre' ? traduire('ap.a2.precisezOv') : traduire('ap.a2.descOv')} value={descOv} onChange={function (e) { setDescOv(e.target.value) }} className={'mb-2 w-full bg-transparent py-1.5 text-sm text-ink focus:outline-none ' + (objectifVise === 'Autre' ? 'border-b border-signature' : 'border-b border-paper-line focus:border-signature')} />
              <textarea placeholder={traduire('ap.a2.contextePh')} value={contexte} onChange={function (e) { setContexte(e.target.value) }} rows={2} className="mb-2 w-full resize-none border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
              <select value={theme} onChange={function (e) { setTheme(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                {THEMES_SR_OV.map(function (t) { return <option key={t} value={t}>{libelle('theme', t)}</option> })}
              </select>
              <div className="mb-3 flex gap-3">
                <select value={motivation} onChange={function (e) { setMotivation(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                  {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('motivation', v)}</option> })}
                </select>
                <select value={ressources} onChange={function (e) { setRessources(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                  {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('ressources', v)}</option> })}
                </select>
              </div>
              <GrilleMatrice matrice={MATRICE_PERTINENCE} ligneLabels={traduire('ap.mx.motiv').split('|')} colonneLabels={traduire('ap.mx.ress').split('|')} ligneTitre={traduire('ap.a2.motivation')} colonneTitre={traduire('ap.a2.ressources')} ligneSelectionnee={Number(motivation) - 1} colonneSelectionnee={Number(ressources) - 1} couleurCellule={couleurPertinence} />
              {erreur && <p className="mb-2 mt-2 text-xs text-risk-critical">{erreur}</p>}
              <div className="mt-2">
                <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
              </div>
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
  return libelle('zoneDangerosite', zone || 'Veille')
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

  var critiques = props.parties.filter(function (p) { return p.zone === 'Danger' || p.zone === 'Controle' })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.cartoDgTitre')}</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a3.dgFormule')} <span className="font-medium text-ink">{traduire('ap.a3.critiqueMot')}</span>.</p>
      {props.parties.length === 0 ? (
        <div className="mb-8"><EmptyState message={traduire('ap.a3.ppVideDessus')} /></div>
      ) : (
        <div className="mb-8 overflow-x-auto">
          <table className="w-full border-collapse text-xs">
            <thead>
              <tr className="border-b border-paper-line text-left font-mono text-[9px] tracking-wide text-steel-light">
                <th className="py-1.5 pr-2 font-medium">{traduire('ap.a3.thPp')}</th>
                <th className="py-1.5 pr-2 font-medium">{traduire('ap.a3.thCat')}</th>
                <th className="py-1.5 pr-2 font-medium">{traduire('ap.a3.thRep')}</th>
                <th className="py-1.5 pr-2 text-center font-medium">{traduire('ap.dg.dependance')}</th>
                <th className="py-1.5 pr-2 text-center font-medium">{traduire('ap.dg.penetration')}</th>
                <th className="py-1.5 pr-2 text-center font-medium">{traduire('ap.dg.maturite')}</th>
                <th className="py-1.5 pr-2 text-center font-medium">{traduire('ap.dg.confiance')}</th>
                <th className="py-1.5 font-medium">{traduire('ap.a3.thNiveauZone')}</th>
              </tr>
            </thead>
            <tbody>
              {props.parties.map(function (p) {
                return (
                  <tr key={p.id} className="border-b border-paper-line/60">
                    <td className="py-1.5 pr-2 text-ink">{p.nom}</td>
                    <td className="py-1.5 pr-2 text-steel">{libelleCategoriePP(p)}</td>
                    <td className="py-1.5 pr-2 text-steel">{p.representant}</td>
                    <td className="py-1.5 pr-2 text-center font-mono text-steel">{p.dependance ?? '--'}</td>
                    <td className="py-1.5 pr-2 text-center font-mono text-steel">{p.penetration ?? '--'}</td>
                    <td className="py-1.5 pr-2 text-center font-mono text-steel">{p.maturiteCyber ?? '--'}</td>
                    <td className="py-1.5 pr-2 text-center font-mono text-steel">{p.confiance ?? '--'}</td>
                    <td className="py-1.5">
                      {p.niveauDangerosite != null && p.zone ? (
                        <Badge couleur={badgeCouleur(couleurZone(p.zone))}>{libelleZone(p.zone)} ({p.niveauDangerosite})</Badge>
                      ) : (
                        <span className="font-mono text-steel-light">{traduire('ap.a3.nonEvaluee')}</span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <h3 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.evalParPp')} ({props.parties.length})</h3>
      {props.parties.length === 0 ? (
        <EmptyState message={traduire('ap.a3.ppVideDessus')} />
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {props.parties.map(function (p) {
            return <LigneEvaluationDangerosite key={p.id} etudeId={props.etudeId} partie={p} onChange={props.onChange} />
          })}
        </div>
      )}
      {critiques.length > 0 && (
        <p className="mt-4 text-xs text-steel">
          <span className="font-medium text-ink">{critiques.length} {traduire('ap.a3.ppCritiquesN')}</span> {traduire('ap.a3.zoneCtrlDanger')} {critiques.map(function (p) { return p.nom }).join(', ')}.
        </p>
      )}
    </section>
  )
}

function LigneEvaluationDangerosite(props: { etudeId: string; partie: PartiePrenante; onChange: () => void }) {
  var p = props.partie
  var lectureSeule = useLectureSeule()
  var jamaisEvaluee = p.niveauDangerosite == null && !lectureSeule
  var [edition, setEdition] = useState(false)
  var [dependance, setDependance] = useState(String(p.dependance || 2))
  var [penetration, setPenetration] = useState(String(p.penetration || 2))
  var [maturiteCyber, setMaturiteCyber] = useState(String(p.maturiteCyber || 2))
  var [confiance, setConfiance] = useState(String(p.confiance || 2))
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function ouvrirEvaluation() {
    setDependance(String(p.dependance || 2))
    setPenetration(String(p.penetration || 2))
    setMaturiteCyber(String(p.maturiteCyber || 2))
    setConfiance(String(p.confiance || 2))
    setErreur('')
    setEdition(true)
  }

  function soumettre() {
    setEnCours(true)
    setErreur('')
    evaluerDangerosite(props.etudeId, p.id, Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))
      .then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  if (edition || jamaisEvaluee) {
    return (
      <div className="space-y-2 py-3">
        <div className="text-sm text-ink">{p.nom}</div>
        <div className="font-mono text-[9px] tracking-wide text-steel-light">{traduire('ap.a3.evaluerDg')}</div>
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <ChampEchelleDangerosite label={traduire('ap.dg.dependance')} critere="dependance" valeur={dependance} onChange={setDependance} />
          <ChampEchelleDangerosite label={traduire('ap.dg.penetration')} critere="penetration" valeur={penetration} onChange={setPenetration} />
          <ChampEchelleDangerosite label={traduire('ap.dg.maturite')} critere="maturiteCyber" valeur={maturiteCyber} onChange={setMaturiteCyber} />
          <ChampEchelleDangerosite label={traduire('ap.dg.confiance')} critere="confiance" valeur={confiance} onChange={setConfiance} />
        </div>
        <div className="flex items-center gap-2 font-mono text-[11px] text-steel-light">
          {traduire('ap.apercu')}
          <Badge couleur={badgeCouleur(couleurZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance)))))}>
            {libelleZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))} ({calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))})
          </Badge>
        </div>
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <Button variante="primary" onClick={soumettre} disabled={enCours}>{enCours ? traduire('ap.enregistrementpts') : traduire('ap.a3.enregistrerEval')}</Button>
          {!jamaisEvaluee && <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>}
        </div>
      </div>
    )
  }

  return (
    <div className="py-2.5">
      <div className="flex items-center justify-between gap-6">
        <div className="text-sm text-ink">{p.nom}</div>
        <div className="flex shrink-0 items-center gap-3">
          {p.niveauDangerosite == null ? (
            <span className="font-mono text-[11px] text-steel-light">{traduire('ap.a3.nonEvaluee')}</span>
          ) : (
            <Badge couleur={badgeCouleur(couleurZone(p.zone || ''))}>{libelleZone(p.zone || '')} ({p.niveauDangerosite})</Badge>
          )}
          {!lectureSeule && <button onClick={ouvrirEvaluation} className="text-[11px] text-steel-light hover:text-signature">{traduire('ap.a3.reevaluer')}</button>}
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
}

function MesuresEcosystemeSection(props: { etudeId: string; parties: PartiePrenante[]; onChange: () => void }) {
  var critiques = props.parties.filter(function (p) { return p.zone === 'Danger' || p.zone === 'Controle' })

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.mesuresEcoTitre')}</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a3.mesuresEcoIntro')}</p>
      {critiques.length === 0 ? (
        <EmptyState message={traduire('ap.a3.critiqueVide')} />
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
      setErreur(traduire('ap.a3.mesureErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    ajouterMesureEcosysteme(props.etudeId, p.id, descMesure)
      .then(function () { setDescMesure(''); setAjoutMesure(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  function supprimerMesure(mesureId: string) {
    if (!window.confirm(traduire('ap.a3.mesureConfirm'))) return
    supprimerMesureEcosysteme(props.etudeId, p.id, mesureId).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function soumettreReevaluation() {
    setEnCours(true)
    setErreur('')
    evaluerDangerositeResiduelle(props.etudeId, p.id, Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))
      .then(function () { setReevaluation(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  return (
    <Card variant="elevated" className="p-4">
      <div className="mb-3 flex items-center justify-between">
        <span className="text-sm font-medium text-ink">{p.nom}</span>
        <Badge couleur={badgeCouleur(couleurZone(p.zone || ''))}>{libelleZone(p.zone || '')}</Badge>
      </div>

      <div className="mb-3 flex items-center gap-4 border-y border-paper-line py-2">
        <div>
          <div className="font-mono text-[9px] text-steel-light">{traduire('ap.a3.dgInitiale')}</div>
          <div className="mt-1"><Badge couleur={badgeCouleur(couleurZone(p.zone || ''))}>{p.niveauDangerosite} -- {libelleZone(p.zone || '')}</Badge></div>
        </div>
        <div className="text-steel-light">&#8594;</div>
        <div>
          <div className="font-mono text-[9px] text-steel-light">{traduire('ap.a3.dgResiduelle')}</div>
          {p.niveauDangerositeResiduel != null && p.zoneResiduelle ? (
            <div className="mt-1"><Badge couleur={badgeCouleur(couleurZone(p.zoneResiduelle))}>{p.niveauDangerositeResiduel} -- {libelleZone(p.zoneResiduelle)}</Badge></div>
          ) : (
            <div className="font-mono text-sm text-steel-light">{traduire('ap.a3.nonReevaluee')}</div>
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

      <h3 className="mb-2 font-display text-sm text-ink">{traduire('ap.a3.mesuresProposees')} ({p.mesures.length})</h3>
      {p.mesures.length === 0 ? (
        <div className="mb-2"><EmptyState message={traduire('ap.a3.mesureVide')} /></div>
      ) : (
        <ul className="mb-2 space-y-1.5">
          {p.mesures.map(function (m) {
            return (
              <li key={m.id} className="flex items-start justify-between gap-4">
                <span className="text-xs text-steel">{m.description}</span>
                <RowActions onSupprimer={function () { supprimerMesure(m.id) }} />
              </li>
            )
          })}
        </ul>
      )}

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      {ajoutMesure && (
        <div className="mb-3 space-y-1.5">
          <textarea placeholder={traduire('ap.a3.mesureDescPh')} value={descMesure} onChange={function (e) { setDescMesure(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={creerMesure} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</button>
            <button onClick={function () { setAjoutMesure(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
          </div>
        </div>
      )}

      {!ajoutMesure && !reevaluation && (
        <div className="mb-3 flex flex-wrap gap-4">
          <Button variante="ghost" onClick={function () { setAjoutMesure(true) }}>{traduire('ap.a3.addMesure')}</Button>
          <Button variante="ghost" onClick={function () { setReevaluation(true) }}>{p.niveauDangerositeResiduel != null ? traduire('ap.a3.reevalResid') : traduire('ap.a3.evalResid')}</Button>
        </div>
      )}

      {reevaluation && (
        <div className="space-y-2 border-t border-paper-line pt-3">
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <ChampEchelleDangerosite label={traduire('ap.dg.dependance')} critere="dependance" valeur={dependance} onChange={setDependance} />
            <ChampEchelleDangerosite label={traduire('ap.dg.penetration')} critere="penetration" valeur={penetration} onChange={setPenetration} />
            <ChampEchelleDangerosite label={traduire('ap.dg.maturite')} critere="maturiteCyber" valeur={maturiteCyber} onChange={setMaturiteCyber} />
            <ChampEchelleDangerosite label={traduire('ap.dg.confiance')} critere="confiance" valeur={confiance} onChange={setConfiance} />
          </div>
          <div className="flex items-center gap-2 font-mono text-[11px] text-steel-light">
            {traduire('ap.apercu')}
            <Badge couleur={badgeCouleur(couleurZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance)))))}>
              {libelleZone(determinerZoneDangerosite(calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))))} ({calculerNiveauDangerosite(Number(dependance), Number(penetration), Number(maturiteCyber), Number(confiance))})
            </Badge>
          </div>
          <div className="flex gap-3">
            <button onClick={soumettreReevaluation} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.enregistrementpts') : 'OK'}</button>
            <button onClick={function () { setReevaluation(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
          </div>
        </div>
      )}
    </Card>
  )
}

function libelleCouple(c: CoupleSourceRisqueObjectifVise) {
  var sr = c.sourceRisque === 'Autre' ? c.descriptionSourceRisque : (libelle('categorieSR', c.sourceRisque) || c.sourceRisque)
  var ov = c.objectifVise === 'Autre' ? c.descriptionObjectifVise : (libelle('categorieOV', c.objectifVise) || c.objectifVise)
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
    if (!er) return traduire('ap.a3.erIntrouvable')
    return libelleValeurMetier(er.valeurMetierId) + ' -- ' + er.description + ' (gravite ' + er.gravite + ')'
  }

  var couplesRetenus = props.couples.filter(function (c) {
    return c.pertinence === 'TresPertinent' || c.pertinence === 'PlutotPertinent'
  })
  var idsAvecScenario: { [key: string]: boolean } = {}
  props.scenarios.forEach(function (s) { idsAvecScenario[s.coupleSourceRisqueObjectifViseId] = true })
  var couplesSansScenario = couplesRetenus.filter(function (c) { return !idsAvecScenario[c.id] })
  var couplesNonRetenus = props.couples.filter(function (c) {
    return c.pertinence !== 'TresPertinent' && c.pertinence !== 'PlutotPertinent'
  })

  function ouvrirCreation(coupleId: string) {
    setCoupleEnCreation(coupleId)
    setDescription('')
    setEvenementRedouteId(props.evenements.length > 0 ? props.evenements[0].id : '')
    setErreur('')
  }

  function creer(coupleId: string) {
    if (!description.trim() || !evenementRedouteId) {
      setErreur(traduire('ap.a3.scenErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    createScenarioStrategique(props.etudeId, coupleId, evenementRedouteId, description)
      .then(function () { setCoupleEnCreation(''); setDescription(''); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer(id: string) {
    if (!window.confirm(traduire('ap.a3.scenConfirm'))) return
    deleteScenarioStrategique(props.etudeId, id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.scenTitre')} ({props.scenarios.length})</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a3.scenIntro')}</p>

      {props.scenarios.length === 0 ? (
        <EmptyState message={traduire('ap.a3.scenVide')} />
      ) : (
        <div className="mb-6 divide-y divide-paper-line border-y border-paper-line">
          {props.scenarios.map(function (s) {
            var couple = props.couples.filter(function (c) { return c.id === s.coupleSourceRisqueObjectifViseId })[0]
            var er = props.evenements.filter(function (e) { return e.id === s.evenementRedouteId })[0]
            if (idEnEdition === s.id) {
              return (
                <div key={s.id} className="space-y-1.5 border-l-2 border-signature py-2.5 pl-3">
                  {couple && <div className="font-mono text-[11px] text-steel-light">{libelleCouple(couple)}</div>}
                  <select value={erEdit} onChange={function (e) { setErEdit(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                    {props.evenements.map(function (e) { return <option key={e.id} value={e.id}>{libelleEr(e.id)}</option> })}
                  </select>
                  <textarea value={descEdit} onChange={function (e) { setDescEdit(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <div className="flex gap-2">
                    <button onClick={function () { sauvegarder(s.id) }} className="text-xs font-medium text-signature hover:underline">OK</button>
                    <button onClick={function () { setIdEnEdition('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
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
                    <span className="text-sm text-steel-light">{traduire('ap.a3.coupleIntrouvable')}</span>
                  )}
                  <div className="flex shrink-0 items-center gap-3">
                    {er && <Badge couleur={badgeCouleur(couleurGravite(er.gravite))}>Gravite {er.gravite}</Badge>}
                    <RowActions onModifier={function () { ouvrirEdition(s) }} onSupprimer={function () { supprimer(s.id) }} />
                  </div>
                </div>
                <div className="mt-1 text-xs text-steel">{s.description}</div>
                <div className="mt-1 font-mono text-[10px] text-steel-light">{traduire('ap.a3.cible')} {er ? libelleEr(er.id) : traduire('ap.a3.erIntrouvableMin')}</div>
              </div>
            )
          })}
        </div>
      )}

      <h3 className="mb-3 font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a3.couplesEnAttente')} ({couplesSansScenario.length})</h3>
      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
      {couplesSansScenario.length === 0 ? (
        couplesNonRetenus.length > 0 ? (
          <div className="space-y-2">
            <p className="text-xs text-steel">{traduire('ap.a3.couplesNonRetenus')}</p>
            <ul className="space-y-1">
              {couplesNonRetenus.map(function (c) {
                return (
                  <li key={c.id} className="flex items-center justify-between gap-4 font-mono text-[11px] text-steel">
                    <span>{libelleCouple(c)}</span>
                    <Badge couleur={badgeCouleur(couleurPertinence(c.pertinence))}>{libelle('pertinence', c.pertinence) || c.pertinence}</Badge>
                  </li>
                )
              })}
            </ul>
          </div>
        ) : (
          <EmptyState message={traduire('ap.a3.couplesTousScenario')} />
        )
      ) : (
        <div className="divide-y divide-paper-line border-y border-paper-line">
          {couplesSansScenario.map(function (c) {
            if (coupleEnCreation === c.id) {
              return (
                <div key={c.id} className="space-y-1.5 border-l-2 border-signature py-2.5 pl-3">
                  <div className="font-mono text-[11px] text-steel-light">{libelleCouple(c)}</div>
                  {props.evenements.length === 0 ? (
                    <p className="text-xs text-risk-critical">{traduire('ap.a3.aucunErDispo')}</p>
                  ) : (
                    <select value={evenementRedouteId} onChange={function (e) { setEvenementRedouteId(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none">
                      {props.evenements.map(function (e) { return <option key={e.id} value={e.id}>{libelleEr(e.id)}</option> })}
                    </select>
                  )}
                  <textarea placeholder={traduire('ap.a3.scenDescPh')} value={description} onChange={function (e) { setDescription(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
                  <div className="flex gap-2">
                    <button onClick={function () { creer(c.id) }} disabled={enCours || props.evenements.length === 0} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.creation') : traduire('ap.a3.creerScenario')}</button>
                    <button onClick={function () { setCoupleEnCreation(''); setErreur('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
                  </div>
                </div>
              )
            }
            return (
              <div key={c.id} className="flex items-center justify-between gap-6 py-2.5">
                <span className="text-sm text-ink">{libelleCouple(c)}</span>
                <Button variante="ghost" onClick={function () { ouvrirCreation(c.id) }}>{traduire('ap.a3.creerUnScenario')}</Button>
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
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a3.cheminsTitre')} ({props.chemins.length})</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a3.cheminsIntro')}</p>
      {props.scenarios.length === 0 ? (
        <EmptyState message={traduire('ap.a3.cheminsScenVide')} />
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
      setErreur(traduire('ap.a3.cheminErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    createCheminAttaque(props.etudeId, props.scenarioId, description)
      .then(function () { setDescription(''); setEnCreation(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  return (
    <Card variant="elevated" className="p-4">
      <div className="mb-3 flex items-center justify-between">
        <span className="text-sm font-medium text-ink">{props.libelleScenario}</span>
        <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.chemins.length} {props.chemins.length > 1 ? traduire('ap.a3.cheminsMaj') : traduire('ap.a3.cheminMaj')}</span>
      </div>

      {props.chemins.length === 0 ? (
        <div className="mb-3"><EmptyState message={traduire('ap.a3.cheminVide')} /></div>
      ) : (
        <div className="mb-3 space-y-4">
          {props.chemins.map(function (chemin) {
            return <CheminRow key={chemin.id} etudeId={props.etudeId} chemin={chemin} parties={props.parties} onChange={props.onChange} />
          })}
        </div>
      )}

      {enCreation ? (
        <div className="space-y-1.5">
          <textarea placeholder={traduire('ap.a3.cheminDescPh')} value={description} onChange={function (e) { setDescription(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
          <div className="flex gap-3">
            <button onClick={creer} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.creation') : traduire('ap.a3.creerChemin')}</button>
            <button onClick={function () { setEnCreation(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
          </div>
        </div>
      ) : (
        <Button variante="ghost" onClick={function () { setEnCreation(true) }}>{traduire('ap.a3.addChemin')}</Button>
      )}
    </Card>
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

  useEffect(function () {
    if (props.parties.length === 0) return
    if (!props.parties.some(function (p) { return p.id === ppId })) {
      setPpId(props.parties[0].id)
    }
  }, [props.parties])

  function supprimerChemin() {
    if (!window.confirm(traduire('ap.a3.cheminConfirm'))) return
    deleteCheminAttaque(props.etudeId, props.chemin.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function sauvegarderChemin() {
    if (!descCheminEdit.trim()) return
    updateCheminAttaque(props.etudeId, props.chemin.id, descCheminEdit)
      .then(function () { setEditionChemin(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function ajouterEi() {
    if (!descEi.trim() || !ppId) {
      setErreur(traduire('ap.a3.eiErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    createEvenementIntermediaire(props.etudeId, props.chemin.id, ppId, descEi)
      .then(function () { setDescEi(''); setAjoutEi(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimerEi(eiId: string) {
    if (!window.confirm(traduire('ap.a3.eiConfirm'))) return
    deleteEvenementIntermediaire(props.etudeId, props.chemin.id, eiId).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function libellePartie(id: string) {
    var pp = props.parties.filter(function (p) { return p.id === id })[0]
    return pp ? pp.nom : traduire('ap.a3.ppIntrouvable')
  }

  return (
    <div className="border-l-2 border-paper-line pl-3">
      {editionChemin ? (
        <div className="space-y-1.5">
          <textarea value={descCheminEdit} onChange={function (e) { setDescCheminEdit(e.target.value) }} rows={2} className="w-full resize-none border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={sauvegarderChemin} className="text-xs font-medium text-signature hover:underline">OK</button>
            <button onClick={function () { setEditionChemin(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
          </div>
        </div>
      ) : (
        <div className="flex items-center justify-between gap-4">
          <span className="text-sm text-ink">{props.chemin.description}</span>
          <RowActions onModifier={function () { setDescCheminEdit(props.chemin.description); setEditionChemin(true) }} onSupprimer={supprimerChemin} />
        </div>
      )}

      {props.chemin.evenementsIntermediaires.length === 0 ? (
        <p className="mt-1 text-xs italic text-steel">{traduire('ap.a3.cheminDirect')}</p>
      ) : (
        <ol className="mt-2 space-y-1">
          {props.chemin.evenementsIntermediaires.map(function (ei, i) {
            if (eiEnEdition === ei.id) {
              return (
                <li key={ei.id} className="flex items-center gap-2">
                  <span className="font-mono text-[11px] text-steel-light">{i + 1}. {libellePartie(ei.partiePrenanteId)} --</span>
                  <input type="text" value={descEiEdit} onChange={function (e) { setDescEiEdit(e.target.value) }} className="flex-1 border-b border-signature bg-transparent py-0.5 text-xs text-ink focus:outline-none" />
                  <button onClick={function () { sauvegarderEi(ei.id) }} className="shrink-0 text-[11px] font-medium text-signature hover:underline">OK</button>
                  <button onClick={function () { setEiEnEdition('') }} className="shrink-0 text-[11px] text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
                </li>
              )
            }
            return (
              <li key={ei.id} className="flex items-center justify-between gap-4 font-mono text-[11px] text-steel">
                <span>{i + 1}. <span className="font-medium text-ink">{libellePartie(ei.partiePrenanteId)}</span> -- {ei.description}</span>
                <RowActions onModifier={function () { ouvrirEditionEi(ei.id, ei.description) }} onSupprimer={function () { supprimerEi(ei.id) }} />
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
          <input type="text" placeholder={traduire('ap.a3.eiDescPh')} value={descEi} onChange={function (e) { setDescEi(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-xs text-ink focus:outline-none" />
          <div className="flex gap-3">
            <button onClick={ajouterEi} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</button>
            <button onClick={function () { setAjoutEi(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
          </div>
        </div>
      ) : (
        <Button variante="ghost" onClick={function () { setAjoutEi(true) }} disabled={props.parties.length === 0}>{traduire('ap.a3.addPp')}</Button>
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
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a4.scenTitre')} ({props.scenariosOperationnels.length})</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a4.scenIntro')}</p>
      {props.scenarios.length === 0 ? (
        <EmptyState message={traduire('ap.a4.scenVide')} />
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
  var [graineModeOp, setGraineModeOp] = useState<{ mo: ModeOperatoireBiblio; n: number } | null>(null)

  function creer() {
    setEnCours(true)
    setErreur('')
    createScenarioOperationnel(props.etudeId, props.chemin.id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  function supprimer() {
    if (!props.scenarioOperationnel) return
    if (!window.confirm(traduire('ap.a4.scenConfirm'))) return
    deleteScenarioOperationnel(props.etudeId, props.scenarioOperationnel.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <Card variant="elevated" className="p-4">
      <div className="mb-2 flex items-center justify-between gap-4">
        <span className="text-sm text-ink">{props.chemin.description}</span>
        {props.scenarioOperationnel && props.scenarioOperationnel.vraisemblanceGlobale && (
          <Badge couleur={badgeCouleur(couleurVraisemblance(props.scenarioOperationnel.vraisemblanceGlobale))}>Vraisemblance {props.scenarioOperationnel.vraisemblanceGlobale}</Badge>
        )}
      </div>

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      {!props.scenarioOperationnel ? (
        <Button variante="ghost" onClick={creer} disabled={enCours}>{enCours ? traduire('ap.creation') : traduire('ap.a4.creerScenOp')}</Button>
      ) : (
        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.scenarioOperationnel.modesOperatoires.length} {traduire('ap.a4.modesMaj')}</span>
            <RowActions onSupprimer={supprimer} labelSupprimer={traduire('ap.a4.supprScen')} />
          </div>
          <div className="space-y-3">
            {props.scenarioOperationnel.modesOperatoires.map(function (mode) {
              return <ModeOperatoireRow key={mode.id} etudeId={props.etudeId} scenarioOperationnelId={props.scenarioOperationnel!.id} mode={mode} biens={props.biens} onChange={props.onChange} />
            })}
          </div>
          <div className="mt-3">
            <PanneauSuggestions<ModeOperatoireBiblio>
              titre={traduire('ap.a4.moSugg')}
              rafraichir={props.scenarioOperationnel.modesOperatoires.length}
              charger={function () { return suggererModesOperatoiresBiblio(props.etudeId) }}
              rendre={function (mo) { return { titre: mo.nom, sousTitre: mo.actions.length + ' actions' } }}
              onUtiliser={function (mo) { setGraineModeOp({ mo: mo, n: Date.now() }) }}
            />
          </div>
          <AjoutModeOperatoire etudeId={props.etudeId} scenarioOperationnelId={props.scenarioOperationnel.id} biens={props.biens} onChange={props.onChange} graine={graineModeOp} />
        </div>
      )}
    </Card>
  )
}


function libelleBienSupport(biens: BienSupport[], bienSupportId: string) {
  var bien = biens.filter(function (b) { return b.id === bienSupportId })[0]
  return bien ? bien.description : '(bien support introuvable)'
}

// Une ligne par action elementaire : phase, description, bien support cible.
// Au moins une action est obligatoire (methodologie EBIOS RM), donc la
// derniere ligne restante ne peut pas etre supprimee.
function ActionElementaireListEditor(props: { actions: ActionElementaireInput[]; biens: BienSupport[]; onChange: (actions: ActionElementaireInput[]) => void }) {
  function modifier(index: number, champ: keyof ActionElementaireInput, valeur: string | null) {
    var copie = props.actions.slice()
    copie[index] = { ...copie[index], [champ]: valeur }
    props.onChange(copie)
  }

  function ajouterLigne() {
    var bienParDefaut = props.biens.length > 0 ? props.biens[0].id : ''
    props.onChange(props.actions.concat([{ description: '', phase: 'Connaitre', bienSupportId: bienParDefaut, techniqueMitre: null }]))
  }

  function supprimerLigne(index: number) {
    if (props.actions.length <= 1) return
    props.onChange(props.actions.filter(function (_, i) { return i !== index }))
  }

  return (
    <div className="space-y-1.5">
      <span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a4.actionsTitre')}</span>
      {props.actions.map(function (a, index) {
        return (
          <div key={index} className="grid grid-cols-[90px_1.2fr_1fr_110px_auto] items-center gap-2">
            <select value={a.phase} onChange={function (e) { modifier(index, 'phase', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {PHASES_ACTION_ELEMENTAIRE.map(function (p) { return <option key={p} value={p}>{libelle('phase', p)}</option> })}
            </select>
            <input type="text" placeholder={traduire('ap.a4.actionDescPh')} value={a.description} onChange={function (e) { modifier(index, 'description', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <select value={a.bienSupportId} onChange={function (e) { modifier(index, 'bienSupportId', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
              {props.biens.length === 0 && <option value="">{traduire('ap.a4.aucunBs')}</option>}
              {props.biens.map(function (b) { return <option key={b.id} value={b.id}>{b.description}</option> })}
            </select>
            <ChampTechniqueMitre valeur={a.techniqueMitre} phase={a.phase} onChange={function (t) { modifier(index, 'techniqueMitre', t) }} />
            <button type="button" onClick={function () { supprimerLigne(index) }} disabled={props.actions.length <= 1} className="text-[11px] text-steel-light hover:text-risk-critical disabled:opacity-30">×</button>
          </div>
        )
      })}
      <Button variante="ghost" type="button" onClick={ajouterLigne}>{traduire('ap.a4.addAction')}</Button>
    </div>
  )
}

function ModeOperatoireRow(props: { etudeId: string; scenarioOperationnelId: string; mode: ModeOperatoire; biens: BienSupport[]; onChange: () => void }) {
  var m = props.mode
  var [edition, setEdition] = useState(false)
  var [description, setDescription] = useState(m.description)
  var [actions, setActions] = useState<ActionElementaireInput[]>(m.actionsElementaires.map(function (a) {
    return { description: a.description, phase: a.phase, bienSupportId: a.bienSupportId, techniqueMitre: a.techniqueMitre }
  }))
  var [probabiliteSucces, setProbabiliteSucces] = useState(String(m.probabiliteSucces))
  var [difficulteTechnique, setDifficulteTechnique] = useState(String(m.difficulteTechnique))
  var [erreur, setErreur] = useState('')

  function sauvegarder() {
    if (!description.trim()) return
    if (actions.length === 0) { setErreur(traduire('ap.a4.moErrAction')); return }
    var input: ModeOperatoireInput = {
      description: description, actions: actions,
      probabiliteSucces: Number(probabiliteSucces), difficulteTechnique: Number(difficulteTechnique),
    }
    modifierModeOperatoire(props.etudeId, props.scenarioOperationnelId, m.id, input)
      .then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer() {
    if (!window.confirm(traduire('ap.a4.moConfirm'))) return
    supprimerModeOperatoire(props.etudeId, props.scenarioOperationnelId, m.id).then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  if (edition) {
    return (
      <div className="border-l-2 border-signature space-y-1.5 pl-3">
        <input type="text" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
        <ActionElementaireListEditor actions={actions} biens={props.biens} onChange={setActions} />
        <p className="text-[11px] leading-snug text-steel">{traduire('ap.a4.probaNote')}</p>
        <div className="grid grid-cols-2 gap-2">
          <select value={probabiliteSucces} onChange={function (e) { setProbabiliteSucces(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('probabilite', v)}</option> })}
          </select>
          <select value={difficulteTechnique} onChange={function (e) { setDifficulteTechnique(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('difficulte', v)}</option> })}
          </select>
        </div>
        <GrilleMatrice matrice={MATRICE_VRAISEMBLANCE} ligneLabels={traduire('ap.mx.proba').split('|')} colonneLabels={traduire('ap.mx.diff').split('|')} ligneTitre={traduire('ap.dg.probabilite')} colonneTitre={traduire('ap.dg.difficulte')} ligneSelectionnee={Number(probabiliteSucces) - 1} colonneSelectionnee={Number(difficulteTechnique) - 1} couleurCellule={couleurVraisemblance} />
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
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
          <Badge couleur={badgeCouleur(couleurVraisemblance(m.vraisemblance))}>{m.vraisemblance}</Badge>
          <RowActions onModifier={function () { setEdition(true) }} onSupprimer={supprimer} />
        </div>
      </div>
      <div className="mt-1 grid grid-cols-2 gap-x-4 gap-y-1 font-mono text-[10px] text-steel-light lg:grid-cols-4">
        {actionsParPhase.map(function (g) {
          return (
            <div key={g.phase}>
              <span className="text-steel">{libelle('phase', g.phase)}</span>
              {g.actions.length === 0
                ? ' --'
                : g.actions.map(function (a, i) {
                    return (
                      <div key={i}>
                        {a.description} → {libelleBienSupport(props.biens, a.bienSupportId)}
                        {a.techniqueMitre && <span className="ml-1 text-signature">[{a.techniqueMitre}]</span>}
                      </div>
                    )
                  })}
            </div>
          )
        })}
      </div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">{traduire('ap.dg.probabiliteLbl')} {m.probabiliteSucces} -- {traduire('ap.dg.difficulteLbl')} {m.difficulteTechnique}</div>
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

function AjoutModeOperatoire(props: { etudeId: string; scenarioOperationnelId: string; biens: BienSupport[]; onChange: () => void; graine?: { mo: ModeOperatoireBiblio; n: number } | null }) {
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
      setActions([{ description: '', phase: 'Connaitre', bienSupportId: props.biens.length > 0 ? props.biens[0].id : '', techniqueMitre: null }])
    }
  }

  function appliquerMode(mo: ModeOperatoireBiblio) {
    setDescription(mo.nom + (mo.description ? ' — ' + mo.description : ''))
    if (mo.probabiliteSuccesTypique) setProbabiliteSucces(String(mo.probabiliteSuccesTypique))
    if (mo.difficulteTechniqueTypique) setDifficulteTechnique(String(mo.difficulteTechniqueTypique))
    var bienDefaut = props.biens.length > 0 ? props.biens[0].id : ''
    setActions(mo.actions.map(function (a) {
      return { description: a.cibleBienSupport ? a.description + ' (cible : ' + a.cibleBienSupport + ')' : a.description, phase: a.phase as PhaseActionElementaire, bienSupportId: bienDefaut, techniqueMitre: a.techniqueMitre }
    }))
  }

  useEffect(function () {
    if (props.graine) { setOuvert(true); appliquerMode(props.graine.mo) }
  }, [props.graine ? props.graine.n : 0])

  function creer() {
    if (!description.trim()) {
      setErreur(traduire('ap.a3.cheminErr'))
      return
    }
    if (actions.length === 0 || actions.some(function (a) { return !a.description.trim() || !a.bienSupportId })) {
      setErreur(traduire('ap.a4.moErrActions'))
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
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  if (!ouvert) {
    return <div className="mt-3"><Button variante="ghost" onClick={ouvrir}>{traduire('ap.a4.addMo')}</Button></div>
  }

  return (
    <div className="mt-3 space-y-1.5 border-l-2 border-signature pl-3">
      <DepuisBiblio<ModeOperatoireBiblio>
        titre={traduire('ap.a4.moBiblio')}
        charger={function (q) { return listerModesOperatoiresBiblio(q) }}
        rendre={function (mo) {
          return (
            <div>
              <div className="text-sm text-ink">{mo.nom}</div>
              <div className="text-[10px] text-steel-light">{metaBiblio(mo.systeme, mo.actions.length + ' actions')}</div>
              {mo.description && <div className="text-[10px] text-steel">{mo.description}</div>}
            </div>
          )
        }}
        onChoisir={appliquerMode}
      />
      <input type="text" placeholder={traduire('ap.a4.moDescPh')} value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none" />
      {actions.some(function (a) { return /\(cible : /.test(a.description) }) && (
        <p className="text-[11px] text-risk-high">{traduire('ap.a4.associerBs')}</p>
      )}
      <ActionElementaireListEditor actions={actions} biens={props.biens} onChange={setActions} />
      <p className="text-[11px] leading-snug text-steel">{traduire('ap.a4.probaNote')}</p>
      <div className="grid grid-cols-2 gap-2">
        <select value={probabiliteSucces} onChange={function (e) { setProbabiliteSucces(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
          {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('probabilite', v)}</option> })}
        </select>
        <select value={difficulteTechnique} onChange={function (e) { setDifficulteTechnique(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
          {['1', '2', '3', '4'].map(function (v) { return <option key={v} value={v}>{libelle('difficulte', v)}</option> })}
        </select>
      </div>
      <GrilleMatrice matrice={MATRICE_VRAISEMBLANCE} ligneLabels={traduire('ap.mx.proba').split('|')} colonneLabels={traduire('ap.mx.diff').split('|')} ligneTitre={traduire('ap.dg.probabilite')} colonneTitre={traduire('ap.dg.difficulte')} ligneSelectionnee={Number(probabiliteSucces) - 1} colonneSelectionnee={Number(difficulteTechnique) - 1} couleurCellule={couleurVraisemblance} />
      {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
      <div className="flex gap-3">
        <button onClick={creer} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</button>
        <button onClick={function () { setOuvert(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
      </div>
    </div>
  )
}

// ============================= ATELIER 5 -- Traitement du risque =============================

export function couleurNiveauRisque(niveau?: string | null) {
  if (niveau === 'Eleve') return 'text-risk-critical'
  if (niveau === 'Moyen') return 'text-risk-high'
  return 'text-risk-low'
}


var OPTIONS_NIVEAU_RISQUE = optionsDe('niveauRisque')

var OPTIONS_VRAISEMBLANCE = optionsDe('vraisemblance')

export function ScenariosDeRisqueSection(props: {
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
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">{traduire('ap.a5.scenTitre')} ({props.scenariosDeRisque.length})</h2>
      <p className="mb-4 text-xs text-steel">{traduire('ap.a5.scenIntro')}</p>
      {props.scenarios.length === 0 ? (
        <EmptyState message={traduire('ap.a4.scenVide')} />
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

export function RisqueParChemin(props: { etudeId: string; chemin: CheminAttaque; scenarioOperationnel?: ScenarioOperationnel; scenarioDeRisque?: ScenarioDeRisque; onChange: () => void }) {
  var [enCours, setEnCours] = useState(false)
  var [erreur, setErreur] = useState('')

  if (!props.scenarioOperationnel) return null

  function creer() {
    setEnCours(true)
    setErreur('')
    creerScenarioDeRisque(props.etudeId, props.chemin.id)
      .then(function () { props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  if (!props.scenarioDeRisque) {
    return (
      <Card variant="elevated" className="p-4">
        <div className="mb-2 text-sm text-ink">{props.chemin.description}</div>
        {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
        <Button variante="ghost" onClick={creer} disabled={enCours}>{enCours ? traduire('ap.a5.materialisation') : traduire('ap.a5.materialiser')}</Button>
      </Card>
    )
  }

  return <ScenarioDeRisqueCard etudeId={props.etudeId} description={props.chemin.description} scenario={props.scenarioDeRisque} onChange={props.onChange} />
}

export function ScenarioDeRisqueCard(props: { etudeId: string; description: string; scenario: ScenarioDeRisque; onChange: () => void }) {
  var s = props.scenario
  var [erreur, setErreur] = useState('')
  var [graviteResiduelle, setGraviteResiduelle] = useState(String(s.graviteResiduelle || s.gravite))
  var [vraisemblanceResiduelle, setVraisemblanceResiduelle] = useState(s.vraisemblanceResiduelle || 'V1')
  var indexVraisemblanceResiduelle = OPTIONS_VRAISEMBLANCE.map(function (o) { return o.value }).indexOf(vraisemblanceResiduelle)

  function supprimer() {
    if (!window.confirm(traduire('ap.a5.scenConfirm'))) return
    supprimerScenarioDeRisque(props.etudeId, s.id).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function evaluerResiduel() {
    evaluerRisqueResiduel(props.etudeId, s.id, Number(graviteResiduelle), vraisemblanceResiduelle)
      .then(props.onChange)
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  return (
    <Card variant="elevated" className="p-4">
      <div className="mb-2 flex items-center justify-between gap-4">
        <div>
          <div className="text-sm text-ink">{props.description}</div>
          <div className="font-mono text-[10px] text-steel-light">{s.libelleCouple}</div>
        </div>
        <RowActions onSupprimer={supprimer} />
      </div>

      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}

      <div className="grid gap-6 border-t border-paper-line pt-3 md:grid-cols-2">
        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a5.niveauInitPre')} {s.gravite} {traduire('ap.a5.xVrais')} {s.vraisemblanceInitiale || '?'})</span>
            {s.niveauRisqueInitial && <Badge couleur={badgeCouleur(couleurNiveauRisque(s.niveauRisqueInitial))}>{s.niveauRisqueInitial}</Badge>}
            {!s.niveauRisqueInitial && <span className="font-mono text-xs text-steel-light">--</span>}
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
            <span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a5.risqueResiduelTitre')}</span>
            {s.niveauRisqueResiduel && <Badge couleur={badgeCouleur(couleurNiveauRisque(s.niveauRisqueResiduel))}>{s.niveauRisqueResiduel}</Badge>}
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
            <GrilleMatrice matrice={MATRICE_RISQUE} ligneLabels={['1', '2', '3', '4']} colonneLabels={['V1', 'V2', 'V3', 'V4']} ligneTitre={traduire('ap.dg.gravite')} colonneTitre={traduire('ap.dg.vraisemblance')} ligneSelectionnee={Number(graviteResiduelle) - 1} colonneSelectionnee={indexVraisemblanceResiduelle} couleurCellule={couleurNiveauRisque} />
          </div>
          <Button variante="ghost" onClick={evaluerResiduel} className="mt-1.5">{traduire('ap.a5.evaluerResiduel')}</Button>

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
              <div className="mt-1 font-mono text-[10px] text-steel-light">{traduire('ap.a5.classeAcceptLbl')} {libelle('classeAcceptation', s.classeAcceptationResiduelle || '') || '--'}</div>
            </>
          )}
        </div>
      </div>

      {s.niveauRisqueResiduel && <AcceptationFormelleSection etudeId={props.etudeId} scenario={s} onChange={props.onChange} />}
    </Card>
  )
}

export function AcceptationFormelleSection(props: { etudeId: string; scenario: ScenarioDeRisque; onChange: () => void }) {
  var s = props.scenario
  var [proprietaire, setProprietaire] = useState('')
  var [validateur, setValidateur] = useState('')
  var [sponsor, setSponsor] = useState('')
  var [justification, setJustification] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  var risqueEleve = s.niveauRisqueResiduel === 'Eleve'
  var lectureSeule = useLectureSeule()

  function accepter() {
    if (!proprietaire.trim() || !validateur.trim()) {
      setErreur(traduire('ap.a5.accepterErr1'))
      return
    }
    if (risqueEleve && (!sponsor.trim() || !justification.trim())) {
      setErreur(traduire('ap.a5.accepterErr2'))
      return
    }
    setEnCours(true)
    setErreur('')
    accepterRisqueResiduel(props.etudeId, s.id, proprietaire, validateur, sponsor || undefined, justification || undefined)
      .then(props.onChange)
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  function retirer() {
    if (!window.confirm(traduire('ap.a5.retirerConfirm'))) return
    retirerAcceptation(props.etudeId, s.id).then(props.onChange)
  }

  return (
    <div className="mt-4 border-t border-paper-line pt-3">
      <span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a5.acceptationTitre')}</span>
      {s.accepteParDirection ? (
        <div className="mt-1.5 space-y-1 text-xs text-ink">
          <div>{traduire('ap.a5.proprioLbl')} <span className="font-medium">{s.nomProprietaireRisque}</span></div>
          <div>{traduire('ap.a5.validateurLbl')} <span className="font-medium">{s.nomValidateurSecurite}</span></div>
          {s.nomSponsorExecutif && <div>{traduire('ap.a5.sponsorLbl')} <span className="font-medium">{s.nomSponsorExecutif}</span></div>}
          {s.justificationAcceptation && <div className="italic text-steel">{s.justificationAcceptation}</div>}
          {s.dateAcceptationUtc && <div className="font-mono text-[10px] text-steel-light">{traduire('ap.a5.accepteLe')} {new Date(s.dateAcceptationUtc).toLocaleDateString(langueCourante() === 'en' ? 'en-GB' : 'fr-FR')}</div>}
          {!lectureSeule && <button onClick={retirer} className="text-[11px] text-steel-light hover:text-risk-critical">{traduire('ap.a5.retirerAcceptation')}</button>}
        </div>
      ) : lectureSeule ? (
        <div className="mt-1.5 font-mono text-[11px] text-steel-light">{traduire('ap.a5.pasAcceptee')}</div>
      ) : (
        <div className="mt-1.5 space-y-1.5">
          <div className="grid grid-cols-2 gap-2">
            <input type="text" placeholder={traduire('ap.a5.proprioPh')} value={proprietaire} onChange={function (e) { setProprietaire(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <input type="text" placeholder={traduire('ap.a5.validateurPh')} value={validateur} onChange={function (e) { setValidateur(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
          </div>
          {risqueEleve && (
            <>
              <input type="text" placeholder={traduire('ap.a5.sponsorPh')} value={sponsor} onChange={function (e) { setSponsor(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <textarea placeholder={traduire('ap.a5.justifPh')} value={justification} onChange={function (e) { setJustification(e.target.value) }} rows={2} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            </>
          )}
          {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
          <Button variante="primary" onClick={accepter} disabled={enCours}>{enCours ? traduire('ap.enregistrementpts') : traduire('ap.a5.accepterFormellement')}</Button>
        </div>
      )}
    </div>
  )
}

var AXES_MESURE = clesDe('axeMesure')
// Symbole officiel "+/++/+++" (seule forme trouvee dans la doc officielle,
// aucune legende associee) -- le mot entre parentheses est un choix
// d'interpretation du projet pour rester comprehensible hors contexte,
// pas une terminologie ANSSI.

/**
 * Panneau depliable de suggestions de bibliotheque, croisant le contenu de
 * l'etude avec les entrees candidates. Generique : mesures (A5), parties
 * prenantes (A3), modes operatoires (A4).
 */
function PanneauSuggestions<T extends { id: string }>(props: {
  titre: string
  charger: () => Promise<import('../lib/api').Suggestion<T>[]>
  rafraichir?: unknown
  rendre: (entree: T) => { titre: string; sousTitre?: string }
  onUtiliser: (entree: T) => void
}) {
  var [suggestions, setSuggestions] = useState<import('../lib/api').Suggestion<T>[]>([])
  var [ouvert, setOuvert] = useState(false)
  var [charge, setCharge] = useState(false)
  var lectureSeule = useLectureSeule()

  useEffect(function () {
    if (!ouvert || charge) return
    props.charger()
      .then(function (s) { setSuggestions(s); setCharge(true) })
      .catch(function () { setCharge(true) })
  }, [ouvert, props.rafraichir])

  if (lectureSeule) return null

  return (
    <div className="border border-paper-line bg-paper-dim p-3">
      <button type="button" onClick={function () { setOuvert(!ouvert); setCharge(false) }} className="font-mono text-[10px] tracking-wide text-signature hover:underline">
        {ouvert ? '−' : '+'} {props.titre.toUpperCase()}
      </button>
      {ouvert && (
        <div className="mt-2">
          {!charge ? (
            <p className="text-xs text-steel">{traduire('ap.a5.suggAnalyse')}</p>
          ) : suggestions.length === 0 ? (
            <p className="text-xs text-steel-light">{traduire('ap.a5.suggAucune')}</p>
          ) : (
            <ul className="divide-y divide-paper-line">
              {suggestions.map(function (s) {
                var r = props.rendre(s.entree)
                return (
                  <li key={s.entree.id} className="flex items-start justify-between gap-3 py-2">
                    <div className="min-w-0">
                      <div className="text-xs text-ink">{r.titre}</div>
                      <div className="mt-0.5 font-mono text-[9px] text-steel-light">
                        {r.sousTitre ? r.sousTitre + ' -- ' : ''}{traduire('ap.lieA')} {s.motsCles.join(', ')}
                      </div>
                    </div>
                    <button type="button" onClick={function () { props.onUtiliser(s.entree) }} className="shrink-0 border border-paper-line px-2 py-0.5 font-mono text-[10px] text-steel transition hover:border-signature hover:text-signature">
                      {traduire('ap.a5.suggPpUtiliser')}
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}

export function PlanTraitementRisqueSection(props: { etudeId: string; plan: PlanTraitementRisque | null; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void }) {
  var [enCours, setEnCours] = useState(false)
  var [erreur, setErreur] = useState('')
  var [graine, setGraine] = useState<{ titre: string; n: number } | null>(null)
  var lectureSeulePlan = useLectureSeule()

  function creerPlan() {
    setEnCours(true)
    setErreur('')
    creerPlanTraitementRisque(props.etudeId).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) }).finally(function () { setEnCours(false) })
  }

  return (
    <section>
      <h2 className="mb-4 font-mono text-[11px] tracking-wide text-steel-light">PLAN DE TRAITEMENT DU RISQUE {props.plan ? '(' + props.plan.mesures.length + ' MESURE(S))' : ''}</h2>
      {erreur && <p className="mb-2 text-xs text-risk-critical">{erreur}</p>}
      {!props.plan ? (
        <>
          <Button variante="ghost" onClick={creerPlan} disabled={enCours}>{enCours ? traduire('ap.creation') : traduire('ap.a5.planCreation')}</Button>
          {lectureSeulePlan && <p className="text-xs text-steel-light">{traduire('ap.a5.planVide')}</p>}
        </>
      ) : (
        <div className="space-y-6">
          {AXES_MESURE.map(function (axe) {
            var mesuresAxe = props.plan!.mesures.filter(function (m) { return m.axe === axe })
            return (
              <div key={axe}>
                <h3 className="mb-2 font-mono text-[10px] tracking-wide text-steel">{libelle('axeMesure', axe).toUpperCase()}</h3>
                {mesuresAxe.length === 0 ? (
                  <EmptyState message={traduire('ap.a5.mesureVide')} />
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
          <PanneauSuggestions<MesureBiblio>
            titre={traduire('ap.a5.suggMesures')}
            rafraichir={props.plan.mesures.length}
            charger={function () { return suggererMesuresBiblio(props.etudeId) }}
            rendre={function (m) {
              return {
                titre: (m.code ? m.code + ' -- ' : '') + m.titre,
                sousTitre: libelle('referentielMesure', m.referentiel) || m.referentiel,
              }
            }}
            onUtiliser={function (m) { setGraine({ titre: m.code ? m.titre + ' (' + m.code + ')' : m.titre, n: Date.now() }) }}
          />
          <AjoutMesureTraitementRisque etudeId={props.etudeId} scenariosDeRisque={props.scenariosDeRisque} onChange={props.onChange} graine={graine} />
        </div>
      )}
    </section>
  )
}

export function libellesScenarios(scenariosDeRisque: ScenarioDeRisque[], ids: string[]) {
  return ids.map(function (id) {
    var s = scenariosDeRisque.filter(function (sc) { return sc.id === id })[0]
    return s ? s.libelleCouple + ' -- ' + s.libelleChemin : '(scenario supprime)'
  })
}

export function SelectionScenariosDeRisque(props: { scenariosDeRisque: ScenarioDeRisque[]; selection: string[]; onChange: (ids: string[]) => void }) {
  function basculer(id: string) {
    if (props.selection.indexOf(id) >= 0) {
      props.onChange(props.selection.filter(function (s) { return s !== id }))
    } else {
      props.onChange(props.selection.concat([id]))
    }
  }

  if (props.scenariosDeRisque.length === 0) {
    return <EmptyState message={traduire('ap.a5.scenNonMateria')} />
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

export function MesureTraitementRisqueRow(props: { etudeId: string; mesure: MesureTraitementRisque; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void }) {
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
  var [codesConformite, setCodesConformite] = useState<string[]>(m.codesConformite || [])
  var [erreur, setErreur] = useState('')

  function sauvegarder() {
    if (!description.trim() || !responsable.trim() || scenariosIds.length === 0) {
      setErreur(traduire('ap.a5.mesureErr'))
      return
    }
    var input: MesureTraitementRisqueInput = {
      description: description, axe: axe, scenariosDeRisqueIds: scenariosIds, responsable: responsable,
      freinsEtDifficultes: freins || null, coutComplexite: coutComplexite, echeance: echeance || null, statut: statut,
      codesConformite: codesConformite,
    }
    modifierMesureTraitementRisque(props.etudeId, m.id, input).then(function () { setEdition(false); props.onChange() })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function supprimer() {
    if (!window.confirm(traduire('ap.a3.mesureConfirm'))) return
    supprimerMesureTraitementRisque(props.etudeId, m.id).then(props.onChange).catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  function versBibliotheque() {
    ajouterMesureBiblio({ titre: m.description, categorie: m.axe })
      .then(function () { toastSucces(traduire('ap.a5.mesureVersBiblio')) })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
  }

  if (edition) {
    return (
      <div className="space-y-1.5 border-l-2 border-signature pl-3">
        <input type="text" value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-signature bg-transparent py-1 text-sm text-ink focus:outline-none" />
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
          <select value={axe} onChange={function (e) { setAxe(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {AXES_MESURE.map(function (a) { return <option key={a} value={a}>{libelle('axeMesure', a)}</option> })}
          </select>
          <select value={coutComplexite} onChange={function (e) { setCoutComplexite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {clesDe('coutComplexite').map(function (c) { return <option key={c} value={c}>{libelle('coutComplexite', c)}</option> })}
          </select>
          <select value={statut} onChange={function (e) { setStatut(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {clesDe('statutMesure').map(function (s) { return <option key={s} value={s}>{libelle('statutMesure', s)}</option> })}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <input type="text" placeholder={traduire('ap.a5.responsablePh')} value={responsable} onChange={function (e) { setResponsable(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
          <input type="text" placeholder={traduire('ap.a5.echeancePh')} value={echeance} onChange={function (e) { setEcheance(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
        </div>
        <input type="text" placeholder={traduire('ap.a5.freinsPh')} value={freins} onChange={function (e) { setFreins(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
        <SelectionScenariosDeRisque scenariosDeRisque={props.scenariosDeRisque} selection={scenariosIds} onChange={setScenariosIds} />
        <div><span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a5.conformiteCouverte')}</span><div className="mt-1"><SelecteurConformite valeurs={codesConformite} onChange={setCodesConformite} /></div></div>
        {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
        <div className="flex gap-3">
          <button onClick={sauvegarder} className="text-xs font-medium text-signature hover:underline">OK</button>
          <button onClick={function () { setEdition(false) }} className="text-xs text-steel-light hover:text-ink">{traduire('ap.annuler')}</button>
        </div>
      </div>
    )
  }

  return (
    <div className="border-l-2 border-paper-line pl-3">
      <div className="flex items-center justify-between gap-4">
        <span className="text-sm text-ink">{m.description}</span>
        <div className="flex shrink-0 items-center gap-3">
          <span className="font-mono text-[11px] text-steel-light">{libelle('statutMesure', m.statut)}</span>
          <button onClick={versBibliotheque} title={traduire('ap.ajouterMaBiblio')} className="text-[11px] text-steel-light hover:text-signature">{traduire('ap.versBiblioLien')}</button>
          <RowActions onModifier={function () { setEdition(true) }} onSupprimer={supprimer} />
        </div>
      </div>
      <div className="mt-1 font-mono text-[10px] text-steel-light">{traduire('ap.a5.responsableLbl')} {m.responsable} -- {traduire('ap.a5.coutLbl')} {libelle('coutComplexite', m.coutComplexite)} -- {traduire('ap.a5.echeanceLbl')} {m.echeance || '--'}</div>
      {m.freinsEtDifficultes && <div className="mt-0.5 text-[11px] italic text-steel">{m.freinsEtDifficultes}</div>}
      <div className="mt-0.5 text-[11px] text-steel-light">{traduire('ap.a5.scenariosLbl')} {libellesScenarios(props.scenariosDeRisque, m.scenariosDeRisqueIds).join('; ')}</div>
      {m.codesConformite && m.codesConformite.length > 0 && (
        <div className="mt-0.5 flex flex-wrap gap-1">
          {m.codesConformite.map(function (c) { return <span key={c} className="border border-paper-line px-1 font-mono text-[10px] text-signature">{c}</span> })}
        </div>
      )}
    </div>
  )
}


export function AjoutMesureTraitementRisque(props: { etudeId: string; scenariosDeRisque: ScenarioDeRisque[]; onChange: () => void; graine?: { titre: string; n: number } | null }) {
  var [description, setDescription] = useState('')
  var [axe, setAxe] = useState('Gouvernance')

  useEffect(function () {
    if (props.graine) setDescription(props.graine.titre)
  }, [props.graine ? props.graine.n : 0])
  var [selecteurBiblio, setSelecteurBiblio] = useState(false)
  var [refBiblio, setRefBiblio] = useState('')
  var [scenariosIds, setScenariosIds] = useState<string[]>([])
  var [responsable, setResponsable] = useState('')
  var [freins, setFreins] = useState('')
  var [coutComplexite, setCoutComplexite] = useState('Plus')
  var [echeance, setEcheance] = useState('')
  var [statut, setStatut] = useState('ALancer')
  var [codesConformite, setCodesConformite] = useState<string[]>([])
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(fermer: () => void) {
    if (!description.trim() || !responsable.trim() || scenariosIds.length === 0) {
      setErreur(traduire('ap.a5.mesureErr'))
      return
    }
    setEnCours(true)
    setErreur('')
    var input: MesureTraitementRisqueInput = {
      description: description, axe: axe, scenariosDeRisqueIds: scenariosIds, responsable: responsable,
      freinsEtDifficultes: freins || null, coutComplexite: coutComplexite, echeance: echeance || null, statut: statut,
      codesConformite: codesConformite,
    }
    ajouterMesureTraitementRisque(props.etudeId, input)
      .then(function () {
        setDescription(''); setScenariosIds([]); setResponsable(''); setFreins(''); setEcheance(''); setCodesConformite([])
        fermer(); props.onChange()
      })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : traduire('ap.err')) })
      .finally(function () { setEnCours(false) })
  }

  return (
    <InlineForm label={traduire('ap.a5.mesureAdd')} signalOuvrir={props.graine ? props.graine.n : undefined}>
      {function (fermer) {
        return (
          <div className="space-y-1.5">
            {!selecteurBiblio ? (
              <button type="button" onClick={function () { setSelecteurBiblio(true) }} className="font-mono text-[10px] text-signature hover:underline">
                {traduire('ap.depuisBiblio')}
              </button>
            ) : (
              <SelecteurBibliotheque<MesureBiblio>
                titre={traduire('ap.a5.mesuresBiblioTitre')}
                filtres={[{ valeur: '', libelle: traduire('ap.commun.tous') }, { valeur: 'Iso27002', libelle: 'ISO 27002' }, { valeur: 'HygieneAnssi', libelle: 'Hygiene ANSSI' }, { valeur: 'Libre', libelle: traduire('ap.a5.maBibliotheque') }]}
                filtreActif={refBiblio}
                onFiltre={setRefBiblio}
                charger={function (q) { return listerMesuresBiblio(refBiblio, q) }}
                cle={function (m) { return m.id }}
                rendre={function (m) {
                  return (
                    <>
                      <div className="font-medium">{m.code ? m.code + ' -- ' : ''}{m.titre}</div>
                      <div className="text-[10px] text-steel-light">{libelle('referentielMesure', m.referentiel) || m.referentiel}{m.categorie ? ' -- ' + m.categorie : ''}</div>
                    </>
                  )
                }}
                onChoisir={function (m) {
                  setDescription(m.code ? m.titre + ' (' + m.code + ')' : m.titre)
                  setSelecteurBiblio(false)
                }}
                onFermer={function () { setSelecteurBiblio(false) }}
              />
            )}
            <input type="text" placeholder={traduire('ap.a5.mesureDescPh')} value={description} onChange={function (e) { setDescription(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
              <select value={axe} onChange={function (e) { setAxe(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {AXES_MESURE.map(function (a) { return <option key={a} value={a}>{libelle('axeMesure', a)}</option> })}
              </select>
              <select value={coutComplexite} onChange={function (e) { setCoutComplexite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {clesDe('coutComplexite').map(function (c) { return <option key={c} value={c}>{libelle('coutComplexite', c)}</option> })}
              </select>
              <select value={statut} onChange={function (e) { setStatut(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {clesDe('statutMesure').map(function (s) { return <option key={s} value={s}>{libelle('statutMesure', s)}</option> })}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <input type="text" placeholder={traduire('ap.a5.responsablePh')} value={responsable} onChange={function (e) { setResponsable(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder={traduire('ap.a5.echeancePh')} value={echeance} onChange={function (e) { setEcheance(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            </div>
            <input type="text" placeholder={traduire('ap.a5.freinsPhOpt')} value={freins} onChange={function (e) { setFreins(e.target.value) }} className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
            <SelectionScenariosDeRisque scenariosDeRisque={props.scenariosDeRisque} selection={scenariosIds} onChange={setScenariosIds} />
            <div><span className="font-mono text-[10px] tracking-wide text-steel-light">{traduire('ap.a5.conformiteCouverteNis2')}</span><div className="mt-1"><SelecteurConformite valeurs={codesConformite} onChange={setCodesConformite} /></div></div>
            {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
            <Button variante="primary" onClick={function () { soumettre(fermer) }} disabled={enCours}>{enCours ? traduire('ap.ajout') : traduire('ap.ajouter')}</Button>
          </div>
        )
      }}
    </InlineForm>
  )
}
