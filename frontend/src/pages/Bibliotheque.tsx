import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import Button from '../components/shared/Button'
import EmptyState from '../components/shared/EmptyState'
import { toastSucces, toastErreur } from '../lib/toast'
import {
  listerMesuresBiblio, ajouterMesureBiblio, supprimerMesureBiblio,
  listerSourcesRisqueBiblio, ajouterSourceRisqueBiblio, supprimerSourceRisqueBiblio,
  ApiError,
} from '../lib/api'
import type { MesureBiblio, SourceRisqueBiblio } from '../lib/api'

var LIBELLE_REFERENTIEL: { [key: string]: string } = { Libre: 'Libre', Iso27002: 'ISO 27002', HygieneAnssi: 'Hygiene ANSSI' }
var CATEGORIES_SR = ['Etatique', 'CrimeOrganise', 'Terroriste', 'ActivisteIdeologique', 'OfficineSpecialisee', 'Amateur', 'Vengeur', 'MalveillantPathologique', 'Autre']
var CATEGORIES_OV = ['EspionnageEtatiqueOuIndustriel', 'PrePositionnementStrategique', 'InfluenceDestabilisation', 'EntraveAuFonctionnement', 'SabotageDestruction', 'Lucratif', 'DefiAmusement', 'Autre']

export default function Bibliotheque() {
  var [onglet, setOnglet] = useState<'mesures' | 'sources'>('mesures')

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="ELEMENTS REUTILISABLES"
        titre="Bibliotheque"
        description="Catalogues fournis (ISO 27002, hygiene ANSSI) et vos propres elements, a reutiliser d'une etude a l'autre."
      />

      <div className="mb-8 flex gap-2 border-b border-paper-line">
        {[['mesures', 'Mesures'], ['sources', 'Sources de risque']].map(function (o) {
          var actif = onglet === o[0]
          return (
            <button
              key={o[0]}
              onClick={function () { setOnglet(o[0] as 'mesures' | 'sources') }}
              className={'-mb-px border-b-2 px-3 py-2 text-xs font-medium transition ' + (actif ? 'border-signature text-signature' : 'border-transparent text-steel hover:text-ink')}
            >
              {o[1]}
            </button>
          )
        })}
      </div>

      {onglet === 'mesures' ? <OngletMesures /> : <OngletSources />}
    </div>
  )
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
        <input type="text" placeholder="Titre de la mesure" value={titre} onChange={function (e) { setTitre(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        <input type="text" placeholder="Description (optionnel)" value={description} onChange={function (e) { setDescription(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        <input type="text" placeholder="Categorie / axe (optionnel)" value={categorie} onChange={function (e) { setCategorie(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        <Button variante="primary" onClick={ajouter} disabled={!titre.trim()}>Ajouter</Button>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        <input type="text" placeholder="Rechercher..." value={q} onChange={function (e) { setQ(e.target.value) }} className="flex-1 border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        {filtres.map(function (f) {
          var actif = referentiel === f[0]
          return (
            <button key={f[0]} onClick={function () { setReferentiel(f[0]) }} className={'border px-2 py-1 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[1]}</button>
          )
        })}
      </div>

      {chargement ? (
        <p className="text-sm text-steel">Chargement...</p>
      ) : mesures.length === 0 ? (
        <EmptyState message="Aucune mesure." />
      ) : (
        <ul className="divide-y divide-paper-line border-y border-paper-line">
          {mesures.map(function (m) {
            return (
              <li key={m.id} className="flex items-start justify-between gap-4 py-2.5">
                <div>
                  <div className="text-sm text-ink">{m.code ? m.code + ' -- ' : ''}{m.titre}</div>
                  <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                    {LIBELLE_REFERENTIEL[m.referentiel] || m.referentiel}{m.categorie ? ' -- ' + m.categorie : ''}{m.systeme ? '' : ' -- ma bibliotheque'}
                  </div>
                  {m.description && <div className="mt-0.5 text-xs text-steel">{m.description}</div>}
                </div>
                {!m.systeme && (
                  <button onClick={function () { supprimer(m) }} aria-label="Retirer" className="shrink-0 text-steel-light transition hover:text-risk-critical">
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
        <input type="text" placeholder="Description de la source de risque" value={dsr} onChange={function (e) { setDsr(e.target.value) }} className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        <input type="text" placeholder="Description de l objectif vise" value={dov} onChange={function (e) { setDov(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
        <Button variante="primary" onClick={ajouter} disabled={!dsr.trim() || !dov.trim()}>Ajouter</Button>
      </div>

      <input type="text" placeholder="Rechercher..." value={q} onChange={function (e) { setQ(e.target.value) }} className="mb-3 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />

      {chargement ? (
        <p className="text-sm text-steel">Chargement...</p>
      ) : sources.length === 0 ? (
        <EmptyState message="Aucune source de risque." />
      ) : (
        <ul className="divide-y divide-paper-line border-y border-paper-line">
          {sources.map(function (s) {
            return (
              <li key={s.id} className="flex items-start justify-between gap-4 py-2.5">
                <div>
                  <div className="text-sm text-ink">{s.descriptionSourceRisque} &rarr; {s.descriptionObjectifVise}</div>
                  <div className="mt-0.5 font-mono text-[10px] text-steel-light">
                    {s.theme || '--'}{s.motivationTypique ? ' -- motivation ' + s.motivationTypique : ''}{s.ressourcesTypiques ? ' / ressources ' + s.ressourcesTypiques : ''}{s.systeme ? '' : ' -- ma bibliotheque'}
                  </div>
                </div>
                {!s.systeme && (
                  <button onClick={function () { supprimer(s) }} aria-label="Retirer" className="shrink-0 text-steel-light transition hover:text-risk-critical">
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
