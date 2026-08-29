import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import { useT } from '../lib/i18n'
import Button from '../components/shared/Button'
import EmptyState from '../components/shared/EmptyState'
import { toastSucces, toastErreur } from '../lib/toast'
import {
  listerMesuresBiblio, ajouterMesureBiblio, supprimerMesureBiblio,
  listerSourcesRisqueBiblio, ajouterSourceRisqueBiblio, supprimerSourceRisqueBiblio,
  listerPartiesPrenantesBiblio, ajouterPartiePrenanteBiblio, supprimerPartiePrenanteBiblio,
  listerValeursMetierBiblio, ajouterValeurMetierBiblio, supprimerValeurMetierBiblio,
  listerBiensSupportBiblio, ajouterBienSupportBiblio, supprimerBienSupportBiblio,
  listerEvenementsRedoutesBiblio, ajouterEvenementRedouteBiblio, supprimerEvenementRedouteBiblio,
  listerModesOperatoiresBiblio, ajouterModeOperatoireBiblio, supprimerModeOperatoireBiblio,
  ApiError,
} from '../lib/api'
import type {
  MesureBiblio, SourceRisqueBiblio, PartiePrenanteBiblio, ValeurMetierBiblio,
  BienSupportBiblio, EvenementRedouteBiblio, ModeOperatoireBiblio,
} from '../lib/api'
import { PHASES_ACTION_ELEMENTAIRE } from '../lib/api'

var LIBELLE_REFERENTIEL: { [key: string]: string } = { Libre: 'Libre', Iso27002: 'ISO 27002', HygieneAnssi: 'Hygiene ANSSI' }
var CATEGORIES_SR = ['Etatique', 'CrimeOrganise', 'Terroriste', 'ActivisteIdeologique', 'OfficineSpecialisee', 'Amateur', 'Vengeur', 'MalveillantPathologique', 'Autre']
var CATEGORIES_OV = ['EspionnageEtatiqueOuIndustriel', 'PrePositionnementStrategique', 'InfluenceDestabilisation', 'EntraveAuFonctionnement', 'SabotageDestruction', 'Lucratif', 'DefiAmusement', 'Autre']
var CATEGORIES_PP = ['Client', 'Partenaire', 'Prestataire', 'Autre']
var TYPES_BS = ['SystemeInformation', 'Reseau', 'RessourcesHumaines', 'Local']
var LIBELLE_TYPE_BS: { [key: string]: string } = {
  SystemeInformation: 'Systeme d information', Reseau: 'Reseau', RessourcesHumaines: 'Ressources humaines', Local: 'Local',
}

var LIBELLE_PHASE_AE: { [key: string]: string } = { Connaitre: 'CONNAITRE', Rentrer: 'RENTRER', Trouver: 'TROUVER', Exploiter: 'EXPLOITER' }

type Onglet = 'mesures' | 'sources' | 'parties-prenantes' | 'valeurs-metier' | 'biens-support' | 'evenements-redoutes' | 'modes-operatoires'

var ONGLETS: [Onglet, string][] = [
  ['mesures', 'Mesures'],
  ['sources', 'Sources de risque'],
  ['parties-prenantes', 'Parties prenantes'],
  ['valeurs-metier', 'Valeurs metier'],
  ['biens-support', 'Biens support'],
  ['evenements-redoutes', 'Evenements redoutes'],
  ['modes-operatoires', 'Modes operatoires'],
]

export default function Bibliotheque() {
  var _t = useT()
  var [onglet, setOnglet] = useState<Onglet>('mesures')

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('biblio.eyebrow')}
        titre={_t('biblio.titre')}
        description="Catalogues fournis (ISO 27002, hygiene ANSSI, exemples EBIOS RM) et vos propres elements, a reutiliser d'une etude a l'autre."
      />

      <div className="mb-8 flex flex-wrap gap-2 border-b border-paper-line">
        {ONGLETS.map(function (o) {
          var actif = onglet === o[0]
          return (
            <button
              key={o[0]}
              onClick={function () { setOnglet(o[0]) }}
              className={'-mb-px border-b-2 px-3 py-2 text-xs font-medium transition ' + (actif ? 'border-signature text-signature' : 'border-transparent text-steel hover:text-ink')}
            >
              {o[1]}
            </button>
          )
        })}
      </div>

      {onglet === 'mesures' && <OngletMesures />}
      {onglet === 'sources' && <OngletSources />}
      {onglet === 'parties-prenantes' && <OngletPartiesPrenantes />}
      {onglet === 'valeurs-metier' && <OngletValeursMetier />}
      {onglet === 'biens-support' && <OngletBiensSupport />}
      {onglet === 'evenements-redoutes' && <OngletEvenementsRedoutes />}
      {onglet === 'modes-operatoires' && <OngletModesOperatoires />}
    </div>
  )
}

/** Champ texte inline, meme style partout. */
function Champ(props: { valeur: string; onChange: (v: string) => void; placeholder: string; className?: string }) {
  return (
    <input
      type="text"
      placeholder={props.placeholder}
      value={props.valeur}
      onChange={function (e) { props.onChange(e.target.value) }}
      className={'w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none ' + (props.className || '')}
    />
  )
}

/** Enveloppe commune : recherche + liste + etat vide + chargement. */
function Liste<T extends { id: string; systeme: boolean }>(props: {
  q: string; onQ: (v: string) => void
  chargement: boolean; items: T[]; vide: string
  rendre: (item: T) => React.ReactNode
  onSupprimer: (item: T) => void
}) {
  var items = props.items || []
  return (
    <div>
      <Champ valeur={props.q} onChange={props.onQ} placeholder="Rechercher..." className="mb-3" />
      {props.chargement ? (
        <p className="text-sm text-steel">Chargement...</p>
      ) : items.length === 0 ? (
        <EmptyState message={props.vide} />
      ) : (
        <ul className="divide-y divide-paper-line border-y border-paper-line">
          {items.map(function (item) {
            return (
              <li key={item.id} className="flex items-start justify-between gap-4 py-2.5">
                <div className="min-w-0">{props.rendre(item)}</div>
                {!item.systeme && (
                  <button onClick={function () { props.onSupprimer(item) }} aria-label="Retirer" className="shrink-0 text-steel-light transition hover:text-risk-critical">
                    <Trash2 size={14} />
                  </button>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

function meta(...parts: (string | number | null | undefined | false)[]) {
  return parts.filter(Boolean).join(' -- ')
}

function OngletMesures() {
  var [mesures, setMesures] = useState<MesureBiblio[]>([])
  var [q, setQ] = useState('')
  var [referentiel, setReferentiel] = useState('')
  var [chargement, setChargement] = useState(true)
  var [titre, setTitre] = useState('')
  var [description, setDescription] = useState('')
  var [categorie, setCategorie] = useState('')

  function charger() {
    setChargement(true)
    listerMesuresBiblio(referentiel, q)
      .then(setMesures)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q, referentiel])

  function ajouter() {
    if (!titre.trim()) return
    ajouterMesureBiblio({ titre: titre, description: description, categorie: categorie })
      .then(function () {
        toastSucces('Mesure ajoutee.')
        setTitre(''); setDescription(''); setCategorie('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(m: MesureBiblio) {
    if (!window.confirm('Retirer "' + m.titre + '" de votre bibliotheque ?')) return
    supprimerMesureBiblio(m.id)
      .then(function () { toastSucces('Mesure retiree.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  var filtres = [['', 'Tous'], ['Iso27002', 'ISO 27002'], ['HygieneAnssi', 'Hygiene ANSSI'], ['Libre', 'Ma bibliotheque']]

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UNE MESURE A MA BIBLIOTHEQUE</div>
        <Champ valeur={titre} onChange={setTitre} placeholder="Titre de la mesure" className="mb-2" />
        <Champ valeur={description} onChange={setDescription} placeholder="Description (optionnel)" className="mb-2" />
        <Champ valeur={categorie} onChange={setCategorie} placeholder="Categorie / axe (optionnel)" className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!titre.trim()}>Ajouter</Button>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        {filtres.map(function (f) {
          var actif = referentiel === f[0]
          return (
            <button key={f[0]} onClick={function () { setReferentiel(f[0]) }} className={'border px-2 py-1 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[1]}</button>
          )
        })}
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={mesures} vide="Aucune mesure."
        onSupprimer={supprimer}
        rendre={function (m) {
          return (
            <>
              <div className="text-sm text-ink">{m.code ? m.code + ' -- ' : ''}{m.titre}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(LIBELLE_REFERENTIEL[m.referentiel] || m.referentiel, m.categorie, !m.systeme && 'ma bibliotheque')}
              </div>
              {m.description && <div className="mt-0.5 text-xs text-steel">{m.description}</div>}
            </>
          )
        }}
      />
    </div>
  )
}

function OngletSources() {
  var [sources, setSources] = useState<SourceRisqueBiblio[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)
  var [sr, setSr] = useState(CATEGORIES_SR[0])
  var [dsr, setDsr] = useState('')
  var [ov, setOv] = useState(CATEGORIES_OV[0])
  var [dov, setDov] = useState('')

  function charger() {
    setChargement(true)
    listerSourcesRisqueBiblio(q)
      .then(setSources)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q])

  function ajouter() {
    if (!dsr.trim() || !dov.trim()) return
    ajouterSourceRisqueBiblio({ sourceRisque: sr, descriptionSourceRisque: dsr, objectifVise: ov, descriptionObjectifVise: dov })
      .then(function () {
        toastSucces('Source de risque ajoutee.')
        setDsr(''); setDov('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(s: SourceRisqueBiblio) {
    if (!window.confirm('Retirer cette source de risque de votre bibliotheque ?')) return
    supprimerSourceRisqueBiblio(s.id)
      .then(function () { toastSucces('Source retiree.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UN COUPLE SOURCE DE RISQUE / OBJECTIF VISE</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-2">
          <select value={sr} onChange={function (e) { setSr(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_SR.map(function (c) { return <option key={c} value={c}>{c}</option> })}
          </select>
          <select value={ov} onChange={function (e) { setOv(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_OV.map(function (c) { return <option key={c} value={c}>{c}</option> })}
          </select>
        </div>
        <Champ valeur={dsr} onChange={setDsr} placeholder="Description de la source de risque" className="mb-2" />
        <Champ valeur={dov} onChange={setDov} placeholder="Description de l objectif vise" className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!dsr.trim() || !dov.trim()}>Ajouter</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={sources} vide="Aucune source de risque."
        onSupprimer={supprimer}
        rendre={function (s) {
          return (
            <>
              <div className="text-sm text-ink">{s.descriptionSourceRisque} &rarr; {s.descriptionObjectifVise}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(s.theme, s.motivationTypique && 'motivation ' + s.motivationTypique, s.ressourcesTypiques && 'ressources ' + s.ressourcesTypiques, !s.systeme && 'ma bibliotheque')}
              </div>
            </>
          )
        }}
      />
    </div>
  )
}

function OngletPartiesPrenantes() {
  var [items, setItems] = useState<PartiePrenanteBiblio[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)
  var [nom, setNom] = useState('')
  var [categorie, setCategorie] = useState(CATEGORIES_PP[2])
  var [roles, setRoles] = useState('')

  function charger() {
    setChargement(true)
    listerPartiesPrenantesBiblio(q)
      .then(setItems)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q])

  function ajouter() {
    if (!nom.trim() || !roles.trim()) return
    ajouterPartiePrenanteBiblio({ nom: nom, categorie: categorie, rolesEtAttentes: roles })
      .then(function () {
        toastSucces('Partie prenante ajoutee.')
        setNom(''); setRoles('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(p: PartiePrenanteBiblio) {
    if (!window.confirm('Retirer "' + p.nom + '" de votre bibliotheque ?')) return
    supprimerPartiePrenanteBiblio(p.id)
      .then(function () { toastSucces('Partie prenante retiree.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UNE PARTIE PRENANTE A MA BIBLIOTHEQUE</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_180px]">
          <Champ valeur={nom} onChange={setNom} placeholder="Nom (ex. Infogereur)" />
          <select value={categorie} onChange={function (e) { setCategorie(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{c}</option> })}
          </select>
        </div>
        <Champ valeur={roles} onChange={setRoles} placeholder="Roles et attentes dans l ecosysteme" className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!nom.trim() || !roles.trim()}>Ajouter</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide="Aucune partie prenante."
        onSupprimer={supprimer}
        rendre={function (p) {
          var niveaux = [p.dependanceTypique, p.penetrationTypique, p.maturiteCyberTypique, p.confianceTypique]
          var aNiveaux = niveaux.some(function (n) { return n != null })
          return (
            <>
              <div className="text-sm text-ink">{p.nom}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(p.descriptionCategorie || p.categorie, aNiveaux && 'dep/pen/mat/conf ' + niveaux.map(function (n) { return n == null ? '-' : n }).join('/'), !p.systeme && 'ma bibliotheque')}
              </div>
              <div className="mt-0.5 text-xs text-steel">{p.rolesEtAttentes}</div>
            </>
          )
        }}
      />
    </div>
  )
}

function OngletValeursMetier() {
  var [items, setItems] = useState<ValeurMetierBiblio[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)
  var [intitule, setIntitule] = useState('')
  var [nature, setNature] = useState('')
  var [entite, setEntite] = useState('')

  function charger() {
    setChargement(true)
    listerValeursMetierBiblio(q)
      .then(setItems)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q])

  function ajouter() {
    if (!intitule.trim()) return
    ajouterValeurMetierBiblio({ intitule: intitule, natureOuFinalite: nature, entiteProprietaireTypique: entite })
      .then(function () {
        toastSucces('Valeur metier ajoutee.')
        setIntitule(''); setNature(''); setEntite('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(v: ValeurMetierBiblio) {
    if (!window.confirm('Retirer "' + v.intitule + '" de votre bibliotheque ?')) return
    supprimerValeurMetierBiblio(v.id)
      .then(function () { toastSucces('Valeur metier retiree.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UNE VALEUR METIER A MA BIBLIOTHEQUE</div>
        <Champ valeur={intitule} onChange={setIntitule} placeholder="Intitule (ex. Processus de paie)" className="mb-2" />
        <div className="grid gap-2 sm:grid-cols-2">
          <Champ valeur={nature} onChange={setNature} placeholder="Nature / finalite (Processus, Information...)" />
          <Champ valeur={entite} onChange={setEntite} placeholder="Entite proprietaire type (optionnel)" />
        </div>
        <div className="mt-3"><Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>Ajouter</Button></div>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide="Aucune valeur metier."
        onSupprimer={supprimer}
        rendre={function (v) {
          return (
            <>
              <div className="text-sm text-ink">{v.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(v.natureOuFinalite, v.entiteProprietaireTypique, !v.systeme && 'ma bibliotheque')}
              </div>
            </>
          )
        }}
      />
    </div>
  )
}

function OngletBiensSupport() {
  var [items, setItems] = useState<BienSupportBiblio[]>([])
  var [q, setQ] = useState('')
  var [type, setType] = useState('')
  var [chargement, setChargement] = useState(true)
  var [intitule, setIntitule] = useState('')
  var [typeForm, setTypeForm] = useState(TYPES_BS[0])
  var [entite, setEntite] = useState('')

  function charger() {
    setChargement(true)
    listerBiensSupportBiblio(type, q)
      .then(setItems)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q, type])

  function ajouter() {
    if (!intitule.trim()) return
    ajouterBienSupportBiblio({ intitule: intitule, type: typeForm, entiteProprietaireTypique: entite })
      .then(function () {
        toastSucces('Bien support ajoute.')
        setIntitule(''); setEntite('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(b: BienSupportBiblio) {
    if (!window.confirm('Retirer "' + b.intitule + '" de votre bibliotheque ?')) return
    supprimerBienSupportBiblio(b.id)
      .then(function () { toastSucces('Bien support retire.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  var filtres = [['', 'Tous']].concat(TYPES_BS.map(function (t) { return [t, LIBELLE_TYPE_BS[t]] }))

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UN BIEN SUPPORT A MA BIBLIOTHEQUE</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_200px]">
          <Champ valeur={intitule} onChange={setIntitule} placeholder="Intitule (ex. Active Directory)" />
          <select value={typeForm} onChange={function (e) { setTypeForm(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {TYPES_BS.map(function (t) { return <option key={t} value={t}>{LIBELLE_TYPE_BS[t]}</option> })}
          </select>
        </div>
        <Champ valeur={entite} onChange={setEntite} placeholder="Entite proprietaire type (optionnel)" className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>Ajouter</Button>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        {filtres.map(function (f) {
          var actif = type === f[0]
          return (
            <button key={f[0]} onClick={function () { setType(f[0]) }} className={'border px-2 py-1 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[1]}</button>
          )
        })}
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide="Aucun bien support."
        onSupprimer={supprimer}
        rendre={function (b) {
          return (
            <>
              <div className="text-sm text-ink">{b.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(LIBELLE_TYPE_BS[b.type] || b.type, b.entiteProprietaireTypique, !b.systeme && 'ma bibliotheque')}
              </div>
              {b.description && <div className="mt-0.5 text-xs text-steel">{b.description}</div>}
            </>
          )
        }}
      />
    </div>
  )
}

function OngletEvenementsRedoutes() {
  var [items, setItems] = useState<EvenementRedouteBiblio[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)
  var [intitule, setIntitule] = useState('')
  var [gravite, setGravite] = useState('')
  var [impacts, setImpacts] = useState('')

  function charger() {
    setChargement(true)
    listerEvenementsRedoutesBiblio(q)
      .then(setItems)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q])

  function ajouter() {
    if (!intitule.trim()) return
    var g = gravite ? parseInt(gravite, 10) : null
    ajouterEvenementRedouteBiblio({ intitule: intitule, graviteIndicative: g, impactsTypes: impacts })
      .then(function () {
        toastSucces('Evenement redoute ajoute.')
        setIntitule(''); setGravite(''); setImpacts('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(e: EvenementRedouteBiblio) {
    if (!window.confirm('Retirer "' + e.intitule + '" de votre bibliotheque ?')) return
    supprimerEvenementRedouteBiblio(e.id)
      .then(function () { toastSucces('Evenement redoute retire.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UN EVENEMENT REDOUTE A MA BIBLIOTHEQUE</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_120px]">
          <Champ valeur={intitule} onChange={setIntitule} placeholder="Intitule de l evenement redoute" />
          <select value={gravite} onChange={function (e) { setGravite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            <option value="">Gravite ?</option>
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>G{n}</option> })}
          </select>
        </div>
        <Champ valeur={impacts} onChange={setImpacts} placeholder="Types d impacts (ex. Financier, Juridique, Image)" className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>Ajouter</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide="Aucun evenement redoute."
        onSupprimer={supprimer}
        rendre={function (e) {
          return (
            <>
              <div className="text-sm text-ink">{e.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(e.graviteIndicative && 'gravite indicative G' + e.graviteIndicative, e.impactsTypes, !e.systeme && 'ma bibliotheque')}
              </div>
            </>
          )
        }}
      />
    </div>
  )
}

type LigneAction = { description: string; phase: string; cibleBienSupport: string; techniqueMitre: string }

function OngletModesOperatoires() {
  var [items, setItems] = useState<ModeOperatoireBiblio[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)
  var [nom, setNom] = useState('')
  var [description, setDescription] = useState('')
  var [proba, setProba] = useState('2')
  var [diff, setDiff] = useState('2')
  var [actions, setActions] = useState<LigneAction[]>([{ description: '', phase: PHASES_ACTION_ELEMENTAIRE[0], cibleBienSupport: '', techniqueMitre: '' }])
  var [deplie, setDeplie] = useState('')

  function charger() {
    setChargement(true)
    listerModesOperatoiresBiblio(q)
      .then(setItems)
      .catch(function () { toastErreur('Bibliotheque indisponible.') })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q])

  function majAction(i: number, champ: keyof LigneAction, v: string) {
    var copie = actions.slice()
    copie[i] = { ...copie[i], [champ]: v }
    setActions(copie)
  }

  function ajouter() {
    var lignes = actions.filter(function (a) { return a.description.trim() })
    if (!nom.trim() || lignes.length === 0) return
    ajouterModeOperatoireBiblio({
      nom: nom, description: description,
      probabiliteSuccesTypique: parseInt(proba, 10), difficulteTechniqueTypique: parseInt(diff, 10),
      actions: lignes.map(function (a) {
        return { description: a.description, phase: a.phase, cibleBienSupport: a.cibleBienSupport || null, techniqueMitre: a.techniqueMitre || null }
      }),
    })
      .then(function () {
        toastSucces('Mode operatoire ajoute.')
        setNom(''); setDescription('')
        setActions([{ description: '', phase: PHASES_ACTION_ELEMENTAIRE[0], cibleBienSupport: '', techniqueMitre: '' }])
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function supprimer(m: ModeOperatoireBiblio) {
    if (!window.confirm('Retirer "' + m.nom + '" de votre bibliotheque ?')) return
    supprimerModeOperatoireBiblio(m.id)
      .then(function () { toastSucces('Mode operatoire retire.'); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UN MODE OPERATOIRE A MA BIBLIOTHEQUE</div>
        <Champ valeur={nom} onChange={setNom} placeholder="Nom (ex. Rançongiciel par hameçonnage)" className="mb-2" />
        <Champ valeur={description} onChange={setDescription} placeholder="Description courte (optionnel)" className="mb-2" />
        <div className="mb-2 grid gap-2 sm:grid-cols-2">
          <select value={proba} onChange={function (e) { setProba(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>Probabilite de succes {n}</option> })}
          </select>
          <select value={diff} onChange={function (e) { setDiff(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>Difficulte technique {n}</option> })}
          </select>
        </div>
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">ACTIONS ELEMENTAIRES</div>
        {actions.map(function (a, i) {
          return (
            <div key={i} className="mb-1.5 grid grid-cols-[110px_1fr_1fr_110px_auto] items-center gap-1.5">
              <select value={a.phase} onChange={function (e) { majAction(i, 'phase', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {PHASES_ACTION_ELEMENTAIRE.map(function (p) { return <option key={p} value={p}>{LIBELLE_PHASE_AE[p]}</option> })}
              </select>
              <input type="text" placeholder="Description de l action" value={a.description} onChange={function (e) { majAction(i, 'description', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="Cible (libelle bien support)" value={a.cibleBienSupport} onChange={function (e) { majAction(i, 'cibleBienSupport', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="MITRE" value={a.techniqueMitre} onChange={function (e) { majAction(i, 'techniqueMitre', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <button type="button" onClick={function () { if (actions.length > 1) setActions(actions.filter(function (_, j) { return j !== i })) }} disabled={actions.length <= 1} className="text-[11px] text-steel-light hover:text-risk-critical disabled:opacity-30">×</button>
            </div>
          )
        })}
        <button type="button" onClick={function () { setActions(actions.concat([{ description: '', phase: PHASES_ACTION_ELEMENTAIRE[0], cibleBienSupport: '', techniqueMitre: '' }])) }} className="mb-3 font-mono text-[10px] text-signature hover:underline">+ Action elementaire</button>
        <div><Button variante="primary" onClick={ajouter} disabled={!nom.trim()}>Ajouter</Button></div>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide="Aucun mode operatoire."
        onSupprimer={supprimer}
        rendre={function (m) {
          var ouvert = deplie === m.id
          return (
            <>
              <button type="button" onClick={function () { setDeplie(ouvert ? '' : m.id) }} className="text-left text-sm text-ink hover:text-signature">{m.nom}</button>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(m.actions.length + ' action' + (m.actions.length > 1 ? 's' : ''), m.probabiliteSuccesTypique && 'proba ' + m.probabiliteSuccesTypique, m.difficulteTechniqueTypique && 'diff ' + m.difficulteTechniqueTypique, !m.systeme && 'ma bibliotheque')}
              </div>
              {m.description && <div className="mt-0.5 text-xs text-steel">{m.description}</div>}
              {ouvert && (
                <ol className="mt-1.5 space-y-0.5 border-l border-paper-line pl-3">
                  {m.actions.map(function (a, i) {
                    return (
                      <li key={i} className="text-[11px] text-steel">
                        <span className="font-mono text-[9px] text-steel-light">{LIBELLE_PHASE_AE[a.phase] || a.phase}</span>{' '}
                        {a.description}
                        {a.cibleBienSupport ? ' — ' + a.cibleBienSupport : ''}
                        {a.techniqueMitre ? ' [' + a.techniqueMitre + ']' : ''}
                      </li>
                    )
                  })}
                </ol>
              )}
            </>
          )
        }}
      />
    </div>
  )
}
