import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import { useT, traduire } from '../lib/i18n'
import { libelle } from '../lib/libelles'
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
  listerCommunaute, importerCommunaute, signalerCommunaute,
  mesPublicationsBiblio, publierBiblio, retirerPublicationBiblio,
  ApiError,
} from '../lib/api'
import type {
  MesureBiblio, SourceRisqueBiblio, PartiePrenanteBiblio, ValeurMetierBiblio,
  BienSupportBiblio, EvenementRedouteBiblio, ModeOperatoireBiblio, EntreeCommunaute,
} from '../lib/api'
import { PHASES_ACTION_ELEMENTAIRE } from '../lib/api'

var CATEGORIES_SR = ['Etatique', 'CrimeOrganise', 'Terroriste', 'ActivisteIdeologique', 'OfficineSpecialisee', 'Amateur', 'Vengeur', 'MalveillantPathologique', 'Autre']
var CATEGORIES_OV = ['EspionnageEtatiqueOuIndustriel', 'PrePositionnementStrategique', 'InfluenceDestabilisation', 'EntraveAuFonctionnement', 'SabotageDestruction', 'Lucratif', 'DefiAmusement', 'Autre']
var CATEGORIES_PP = ['Client', 'Partenaire', 'Prestataire', 'Autre']
var TYPES_BS = ['SystemeInformation', 'Reseau', 'RessourcesHumaines', 'Local']


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

/** Onglet -> slug de type utilise par les routes /bibliotheque/communaute/{type}. */
var SLUG_COMMUNAUTE: { [key in Onglet]: string } = {
  'mesures': 'mesure',
  'sources': 'source-risque',
  'parties-prenantes': 'partie-prenante',
  'valeurs-metier': 'valeur-metier',
  'biens-support': 'bien-support',
  'evenements-redoutes': 'evenement-redoute',
  'modes-operatoires': 'mode-operatoire',
}

export default function Bibliotheque() {
  var _t = useT()
  var [onglet, setOnglet] = useState<Onglet>('mesures')
  var [portee, setPortee] = useState<'perso' | 'communaute'>('perso')

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('biblio.eyebrow')}
        titre={_t('biblio.titre')}
        description={traduire('bib.desc')}
      />

      <div className="mb-4 inline-flex rounded-sm border border-paper-line p-0.5">
        {([['perso', ''], ['communaute', '']] as [typeof portee, string][]).map(function (o) {
          var actif = portee === o[0]
          return (
            <button key={o[0]} onClick={function () { setPortee(o[0]) }} className={'px-3 py-1 text-xs font-medium transition ' + (actif ? 'bg-signature text-white' : 'text-steel hover:text-ink')}>{traduire(o[0] === 'perso' ? 'bib.maBiblio' : 'bib.communaute')}</button>
          )
        })}
      </div>

      <div className="mb-8 flex flex-wrap gap-2 border-b border-paper-line">
        {ONGLETS.map(function (o) {
          var actif = onglet === o[0]
          return (
            <button
              key={o[0]}
              onClick={function () { setOnglet(o[0]) }}
              className={'-mb-px border-b-2 px-3 py-2 text-xs font-medium transition ' + (actif ? 'border-signature text-signature' : 'border-transparent text-steel hover:text-ink')}
            >
              {traduire('bib.onglet.' + o[0])}
            </button>
          )
        })}
      </div>

      {portee === 'communaute' ? (
        <VueCommunaute key={onglet} slug={SLUG_COMMUNAUTE[onglet]} onglet={onglet} />
      ) : (
        <>
          {onglet === 'mesures' && <OngletMesures />}
          {onglet === 'sources' && <OngletSources />}
          {onglet === 'parties-prenantes' && <OngletPartiesPrenantes />}
          {onglet === 'valeurs-metier' && <OngletValeursMetier />}
          {onglet === 'biens-support' && <OngletBiensSupport />}
          {onglet === 'evenements-redoutes' && <OngletEvenementsRedoutes />}
          {onglet === 'modes-operatoires' && <OngletModesOperatoires />}
        </>
      )}
    </div>
  )
}

function texteEntree(onglet: Onglet, e: Record<string, unknown>): { titre: string; sous: string } {
  var s = function (k: string) { return (e[k] as string) || '' }
  if (onglet === 'mesures') return { titre: (s('code') ? s('code') + ' -- ' : '') + s('titre'), sous: [s('referentiel'), s('categorie')].filter(Boolean).join(' -- ') }
  if (onglet === 'sources') return { titre: s('descriptionSourceRisque') + ' -> ' + s('descriptionObjectifVise'), sous: s('theme') }
  if (onglet === 'parties-prenantes') return { titre: s('nom'), sous: [s('descriptionCategorie') || s('categorie'), s('rolesEtAttentes')].filter(Boolean).join(' -- ') }
  if (onglet === 'valeurs-metier') return { titre: s('intitule'), sous: [s('natureOuFinalite'), s('entiteProprietaireTypique')].filter(Boolean).join(' -- ') }
  if (onglet === 'biens-support') return { titre: s('intitule'), sous: [libelle('typeBienSupport', s('type')), s('entiteProprietaireTypique')].filter(Boolean).join(' -- ') }
  if (onglet === 'evenements-redoutes') return { titre: s('intitule'), sous: [(e['graviteIndicative'] ? 'G' + e['graviteIndicative'] : ''), s('impactsTypes')].filter(Boolean).join(' -- ') }
  var actions = (e['actions'] as unknown[]) || []
  return { titre: s('nom'), sous: [actions.length + ' ' + traduire('bib.mo.actionsN'), s('description')].filter(Boolean).join(' -- ') }
}

function VueCommunaute(props: { slug: string; onglet: Onglet }) {
  var [items, setItems] = useState<EntreeCommunaute[]>([])
  var [q, setQ] = useState('')
  var [chargement, setChargement] = useState(true)

  function charger() {
    setChargement(true)
    listerCommunaute(props.slug, q)
      .then(setItems)
      .catch(function () { toastErreur(traduire('bib.communauteIndispo')) })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () {
    var minuteur = setTimeout(charger, 200)
    return function () { clearTimeout(minuteur) }
  }, [q, props.slug])

  function importer(e: EntreeCommunaute) {
    importerCommunaute(props.slug, e.id)
      .then(function () { toastSucces(traduire('bib.com.importe')) })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function signaler(e: EntreeCommunaute) {
    var motif = window.prompt(traduire('bib.com.promptMotif')) || ''
    if (motif === null) return
    signalerCommunaute(props.slug, e.id, motif)
      .then(function () { toastSucces(traduire('bib.com.signalEnr')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <p className="mb-3 border border-paper-line bg-paper-dim px-3 py-2 text-[11px] text-steel">
        {traduire('bib.com.intro')}
      </p>
      <Champ valeur={q} onChange={setQ} placeholder={traduire('bib.rechercher')} className="mb-3" />
      {chargement ? (
        <p className="text-sm text-steel">{traduire('bib.chargement')}</p>
      ) : items.length === 0 ? (
        <EmptyState message={traduire('bib.com.vide')} />
      ) : (
        <ul className="divide-y divide-paper-line border-y border-paper-line">
          {items.map(function (e) {
            var t = texteEntree(props.onglet, e.entree)
            return (
              <li key={e.id} className="flex items-start justify-between gap-4 py-2.5">
                <div className="min-w-0">
                  <div className="text-sm text-ink">{t.titre}</div>
                  <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                    {[traduire('bib.com.par') + ' ' + (e.publieParMoi ? traduire('bib.com.vous') : e.proprietaire), t.sous, e.signalements > 0 ? e.signalements + ' ' + (e.signalements > 1 ? traduire('bib.com.signalementsN') : traduire('bib.com.signalementN')) : ''].filter(Boolean).join(' -- ')}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <button onClick={function () { importer(e) }} className="border border-paper-line px-2 py-0.5 font-mono text-[10px] text-steel transition hover:border-signature hover:text-signature">{traduire('bib.com.importer')}</button>
                  {!e.publieParMoi && (
                    <button onClick={function () { signaler(e) }} className="font-mono text-[10px] text-steel-light hover:text-risk-critical">{traduire('bib.com.signaler')}</button>
                  )}
                </div>
              </li>
            )
          })}
        </ul>
      )}
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

/**
 * Enveloppe commune : recherche + liste + etat vide + chargement.
 * `slug` (type communautaire) active un bouton Publier / Retirer du partage
 * sur les entrees personnelles.
 */
function Liste<T extends { id: string; systeme: boolean }>(props: {
  q: string; onQ: (v: string) => void
  chargement: boolean; items: T[]; vide: string
  rendre: (item: T) => React.ReactNode
  onSupprimer: (item: T) => void
  slug?: string
}) {
  var items = props.items || []
  var [publiees, setPubliees] = useState<{ [id: string]: boolean }>({})

  useEffect(function () {
    if (!props.slug) return
    mesPublicationsBiblio()
      .then(function (ids) {
        var m: { [id: string]: boolean } = {}
        ids.forEach(function (i) { m[i] = true })
        setPubliees(m)
      })
      .catch(function () {})
  }, [props.slug, props.chargement])

  function basculerPartage(item: T) {
    if (!props.slug) return
    var estPublie = !!publiees[item.id]
    var action = estPublie ? retirerPublicationBiblio(props.slug, item.id) : publierBiblio(props.slug, item.id)
    action
      .then(function () {
        setPubliees(function (p) { var c = { ...p }; if (estPublie) delete c[item.id]; else c[item.id] = true; return c })
        toastSucces(estPublie ? traduire('bib.retireDuPartage') : traduire('bib.publieCommunaute'))
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <Champ valeur={props.q} onChange={props.onQ} placeholder={traduire('bib.rechercher')} className="mb-3" />
      {props.chargement ? (
        <p className="text-sm text-steel">{traduire('bib.chargement')}</p>
      ) : items.length === 0 ? (
        <EmptyState message={props.vide} />
      ) : (
        <ul className="divide-y divide-paper-line border-y border-paper-line">
          {items.map(function (item) {
            return (
              <li key={item.id} className="flex items-start justify-between gap-4 py-2.5">
                <div className="min-w-0">{props.rendre(item)}</div>
                {!item.systeme && (
                  <div className="flex shrink-0 items-center gap-2">
                    {props.slug && (
                      <button onClick={function () { basculerPartage(item) }} className={'font-mono text-[10px] transition ' + (publiees[item.id] ? 'text-signature hover:text-steel' : 'text-steel-light hover:text-signature')}>
                        {publiees[item.id] ? traduire('bib.publieCheck') : traduire('bib.publier')}
                      </button>
                    )}
                    <button onClick={function () { props.onSupprimer(item) }} aria-label={traduire('commun.supprimer')} className="text-steel-light transition hover:text-risk-critical">
                      <Trash2 size={14} />
                    </button>
                  </div>
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.mes.ajoutee'))
        setTitre(''); setDescription(''); setCategorie('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(m: MesureBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + m.titre + '" ' + traduire('bib.retirerPost'))) return
    supprimerMesureBiblio(m.id)
      .then(function () { toastSucces(traduire('bib.mes.retiree')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  var filtres = [['', 'Tous'], ['Iso27002', 'ISO 27002'], ['HygieneAnssi', 'Hygiene ANSSI'], ['Libre', 'Ma bibliotheque']]

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.mes.formTitre')}</div>
        <Champ valeur={titre} onChange={setTitre} placeholder={traduire('bib.mes.titrePh')} className="mb-2" />
        <Champ valeur={description} onChange={setDescription} placeholder={traduire('bib.mes.descPh')} className="mb-2" />
        <Champ valeur={categorie} onChange={setCategorie} placeholder={traduire('bib.mes.catPh')} className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!titre.trim()}>{traduire('bib.ajouter')}</Button>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        {filtres.map(function (f) {
          var actif = referentiel === f[0]
          return (
            <button key={f[0]} onClick={function () { setReferentiel(f[0]) }} className={'border px-2 py-1 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[0] === '' ? traduire('commun.tous') : f[1]}</button>
          )
        })}
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={mesures} vide={traduire('bib.mes.vide')}
        onSupprimer={supprimer}
        slug="mesure"
        rendre={function (m) {
          return (
            <>
              <div className="text-sm text-ink">{m.code ? m.code + ' -- ' : ''}{m.titre}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(libelle('referentielMesure', m.referentiel), m.categorie, !m.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.src.ajoutee'))
        setDsr(''); setDov('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(s: SourceRisqueBiblio) {
    if (!window.confirm(traduire('bib.src.confirm'))) return
    supprimerSourceRisqueBiblio(s.id)
      .then(function () { toastSucces(traduire('bib.src.retiree')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.src.formTitre')}</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-2">
          <select value={sr} onChange={function (e) { setSr(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_SR.map(function (c) { return <option key={c} value={c}>{libelle('categorieSR', c)}</option> })}
          </select>
          <select value={ov} onChange={function (e) { setOv(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_OV.map(function (c) { return <option key={c} value={c}>{libelle('categorieOV', c)}</option> })}
          </select>
        </div>
        <Champ valeur={dsr} onChange={setDsr} placeholder={traduire('bib.src.dsrPh')} className="mb-2" />
        <Champ valeur={dov} onChange={setDov} placeholder={traduire('bib.src.dovPh')} className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!dsr.trim() || !dov.trim()}>{traduire('bib.ajouter')}</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={sources} vide={traduire('bib.src.vide')}
        onSupprimer={supprimer}
        slug="source-risque"
        rendre={function (s) {
          return (
            <>
              <div className="text-sm text-ink">{s.descriptionSourceRisque} &rarr; {s.descriptionObjectifVise}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(s.theme, s.motivationTypique && traduire('bib.src.motivation') + s.motivationTypique, s.ressourcesTypiques && traduire('bib.src.ressources') + s.ressourcesTypiques, !s.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.pp.ajoutee'))
        setNom(''); setRoles('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(p: PartiePrenanteBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + p.nom + '" ' + traduire('bib.retirerPost'))) return
    supprimerPartiePrenanteBiblio(p.id)
      .then(function () { toastSucces(traduire('bib.pp.retiree')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.pp.formTitre')}</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_180px]">
          <Champ valeur={nom} onChange={setNom} placeholder={traduire('bib.pp.nomPh')} />
          <select value={categorie} onChange={function (e) { setCategorie(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {CATEGORIES_PP.map(function (c) { return <option key={c} value={c}>{libelle('categoriePP', c)}</option> })}
          </select>
        </div>
        <Champ valeur={roles} onChange={setRoles} placeholder={traduire('bib.pp.rolesPh')} className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!nom.trim() || !roles.trim()}>{traduire('bib.ajouter')}</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide={traduire('bib.pp.vide')}
        onSupprimer={supprimer}
        slug="partie-prenante"
        rendre={function (p) {
          var niveaux = [p.dependanceTypique, p.penetrationTypique, p.maturiteCyberTypique, p.confianceTypique]
          var aNiveaux = niveaux.some(function (n) { return n != null })
          return (
            <>
              <div className="text-sm text-ink">{p.nom}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(p.descriptionCategorie || p.categorie, aNiveaux && traduire('bib.pp.niveauxMeta') + niveaux.map(function (n) { return n == null ? '-' : n }).join('/'), !p.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.vm.ajoutee'))
        setIntitule(''); setNature(''); setEntite('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(v: ValeurMetierBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + v.intitule + '" ' + traduire('bib.retirerPost'))) return
    supprimerValeurMetierBiblio(v.id)
      .then(function () { toastSucces(traduire('bib.vm.retiree')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.vm.formTitre')}</div>
        <Champ valeur={intitule} onChange={setIntitule} placeholder={traduire('bib.vm.intitulePh')} className="mb-2" />
        <div className="grid gap-2 sm:grid-cols-2">
          <Champ valeur={nature} onChange={setNature} placeholder={traduire('bib.vm.naturePh')} />
          <Champ valeur={entite} onChange={setEntite} placeholder={traduire('bib.vm.entitePh')} />
        </div>
        <div className="mt-3"><Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>{traduire('bib.ajouter')}</Button></div>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide={traduire('bib.vm.vide')}
        onSupprimer={supprimer}
        slug="valeur-metier"
        rendre={function (v) {
          return (
            <>
              <div className="text-sm text-ink">{v.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(v.natureOuFinalite, v.entiteProprietaireTypique, !v.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.bs.ajoute'))
        setIntitule(''); setEntite('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(b: BienSupportBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + b.intitule + '" ' + traduire('bib.retirerPost'))) return
    supprimerBienSupportBiblio(b.id)
      .then(function () { toastSucces(traduire('bib.bs.retire')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  var filtres = [['', 'Tous']].concat(TYPES_BS.map(function (t) { return [t, libelle('typeBienSupport', t)] }))

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.bs.formTitre')}</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_200px]">
          <Champ valeur={intitule} onChange={setIntitule} placeholder={traduire('bib.bs.intitulePh')} />
          <select value={typeForm} onChange={function (e) { setTypeForm(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {TYPES_BS.map(function (t) { return <option key={t} value={t}>{libelle('typeBienSupport', t)}</option> })}
          </select>
        </div>
        <Champ valeur={entite} onChange={setEntite} placeholder={traduire('bib.vm.entitePh')} className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>{traduire('bib.ajouter')}</Button>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        {filtres.map(function (f) {
          var actif = type === f[0]
          return (
            <button key={f[0]} onClick={function () { setType(f[0]) }} className={'border px-2 py-1 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[0] === '' ? traduire('commun.tous') : f[1]}</button>
          )
        })}
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide={traduire('bib.bs.vide')}
        onSupprimer={supprimer}
        slug="bien-support"
        rendre={function (b) {
          return (
            <>
              <div className="text-sm text-ink">{b.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(libelle('typeBienSupport', b.type), b.entiteProprietaireTypique, !b.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.er.ajoute'))
        setIntitule(''); setGravite(''); setImpacts('')
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(e: EvenementRedouteBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + e.intitule + '" ' + traduire('bib.retirerPost'))) return
    supprimerEvenementRedouteBiblio(e.id)
      .then(function () { toastSucces(traduire('bib.er.retire')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.er.formTitre')}</div>
        <div className="mb-2 grid gap-2 sm:grid-cols-[1fr_120px]">
          <Champ valeur={intitule} onChange={setIntitule} placeholder={traduire('bib.er.intitulePh')} />
          <select value={gravite} onChange={function (e) { setGravite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            <option value="">{traduire('bib.er.graviteQ')}</option>
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>G{n}</option> })}
          </select>
        </div>
        <Champ valeur={impacts} onChange={setImpacts} placeholder={traduire('bib.er.impactsPh')} className="mb-3" />
        <Button variante="primary" onClick={ajouter} disabled={!intitule.trim()}>{traduire('bib.ajouter')}</Button>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide={traduire('bib.er.vide')}
        onSupprimer={supprimer}
        slug="evenement-redoute"
        rendre={function (e) {
          return (
            <>
              <div className="text-sm text-ink">{e.intitule}</div>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(e.graviteIndicative && traduire('bib.er.graviteIndic') + e.graviteIndicative, e.impactsTypes, !e.systeme && traduire('bib.metaMaBiblio'))}
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
      .catch(function () { toastErreur(traduire('bib.indispo')) })
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
        toastSucces(traduire('bib.mo.ajoute'))
        setNom(''); setDescription('')
        setActions([{ description: '', phase: PHASES_ACTION_ELEMENTAIRE[0], cibleBienSupport: '', techniqueMitre: '' }])
        charger()
      })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  function supprimer(m: ModeOperatoireBiblio) {
    if (!window.confirm(traduire('bib.retirerPre') + '"' + m.nom + '" ' + traduire('bib.retirerPost'))) return
    supprimerModeOperatoireBiblio(m.id)
      .then(function () { toastSucces(traduire('bib.mo.retire')); charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : traduire('bib.err')) })
  }

  return (
    <div>
      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.mo.formTitre')}</div>
        <Champ valeur={nom} onChange={setNom} placeholder={traduire('bib.mo.nomPh')} className="mb-2" />
        <Champ valeur={description} onChange={setDescription} placeholder={traduire('bib.mo.descPh')} className="mb-2" />
        <div className="mb-2 grid gap-2 sm:grid-cols-2">
          <select value={proba} onChange={function (e) { setProba(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>{traduire('bib.mo.probaN')} {n}</option> })}
          </select>
          <select value={diff} onChange={function (e) { setDiff(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            {[1, 2, 3, 4].map(function (n) { return <option key={n} value={n}>{traduire('bib.mo.diffN')} {n}</option> })}
          </select>
        </div>
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('bib.mo.actionsTitre')}</div>
        {actions.map(function (a, i) {
          return (
            <div key={i} className="mb-1.5 grid grid-cols-[110px_1fr_1fr_110px_auto] items-center gap-1.5">
              <select value={a.phase} onChange={function (e) { majAction(i, 'phase', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                {PHASES_ACTION_ELEMENTAIRE.map(function (p) { return <option key={p} value={p}>{libelle('phase', p)}</option> })}
              </select>
              <input type="text" placeholder={traduire('bib.mo.actionDescPh')} value={a.description} onChange={function (e) { majAction(i, 'description', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder={traduire('bib.mo.ciblePh')} value={a.cibleBienSupport} onChange={function (e) { majAction(i, 'cibleBienSupport', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <input type="text" placeholder="MITRE" value={a.techniqueMitre} onChange={function (e) { majAction(i, 'techniqueMitre', e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
              <button type="button" onClick={function () { if (actions.length > 1) setActions(actions.filter(function (_, j) { return j !== i })) }} disabled={actions.length <= 1} className="text-[11px] text-steel-light hover:text-risk-critical disabled:opacity-30">×</button>
            </div>
          )
        })}
        <button type="button" onClick={function () { setActions(actions.concat([{ description: '', phase: PHASES_ACTION_ELEMENTAIRE[0], cibleBienSupport: '', techniqueMitre: '' }])) }} className="mb-3 font-mono text-[10px] text-signature hover:underline">{traduire('bib.mo.addAction')}</button>
        <div><Button variante="primary" onClick={ajouter} disabled={!nom.trim()}>{traduire('bib.ajouter')}</Button></div>
      </div>

      <Liste
        q={q} onQ={setQ} chargement={chargement} items={items} vide={traduire('bib.mo.vide')}
        onSupprimer={supprimer}
        slug="mode-operatoire"
        rendre={function (m) {
          var ouvert = deplie === m.id
          return (
            <>
              <button type="button" onClick={function () { setDeplie(ouvert ? '' : m.id) }} className="text-left text-sm text-ink hover:text-signature">{m.nom}</button>
              <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                {meta(m.actions.length + ' ' + (m.actions.length > 1 ? traduire('bib.mo.actionsN') : traduire('bib.mo.actionN')), m.probabiliteSuccesTypique && traduire('bib.mo.probaMeta') + m.probabiliteSuccesTypique, m.difficulteTechniqueTypique && traduire('bib.mo.diffMeta') + m.difficulteTechniqueTypique, !m.systeme && traduire('bib.metaMaBiblio'))}
              </div>
              {m.description && <div className="mt-0.5 text-xs text-steel">{m.description}</div>}
              {ouvert && (
                <ol className="mt-1.5 space-y-0.5 border-l border-paper-line pl-3">
                  {m.actions.map(function (a, i) {
                    return (
                      <li key={i} className="text-[11px] text-steel">
                        <span className="font-mono text-[9px] text-steel-light">{libelle('phase', a.phase)}</span>{' '}
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
