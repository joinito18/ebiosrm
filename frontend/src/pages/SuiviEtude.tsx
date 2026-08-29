import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { Trash2 } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import { useT } from '../lib/i18n'
import EmptyState from '../components/shared/EmptyState'
import Button from '../components/shared/Button'
import Sparkline from '../components/shared/Sparkline'
import { toastSucces, toastErreur } from '../lib/toast'
import {
  getEtude, chargerEvolutionEtude, chargerIndicateurs,
  creerIndicateur, supprimerIndicateur, ajouterPointIndicateur, supprimerPointIndicateur, ApiError,
} from '../lib/api'
import type { Etude, EvolutionEtude, IndicateurSuivi, IndicateurAuto, SensAmelioration, TendanceEvolution } from '../lib/api'

var FLECHE_TENDANCE: { [key in TendanceEvolution]: string } = {
  Amelioration: '↘ amélioration', Stable: '→ stable', Degradation: '↗ dégradation', Nouveau: '＋ nouveau',
}
var CLASSE_TENDANCE: { [key in TendanceEvolution]: string } = {
  Amelioration: 'text-risk-low', Stable: 'text-steel', Degradation: 'text-risk-critical', Nouveau: 'text-signature',
}

function enAlerte(i: IndicateurSuivi): boolean {
  if (i.seuilAlerte == null || i.points.length === 0) return false
  var v = i.points[i.points.length - 1].valeur
  return i.sens === 'Baisse' ? v > i.seuilAlerte : v < i.seuilAlerte
}

export default function SuiviEtude() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var _t = useT()
  var [etude, setEtude] = useState<Etude | null>(null)
  var [evolution, setEvolution] = useState<EvolutionEtude | null>(null)
  var [autos, setAutos] = useState<IndicateurAuto[]>([])
  var [manuels, setManuels] = useState<IndicateurSuivi[]>([])
  var [chargement, setChargement] = useState(true)

  function rechargerIndicateurs() {
    return chargerIndicateurs(etudeId).then(function (d) { setAutos(d.automatiques); setManuels(d.manuels) })
  }

  useEffect(function () {
    getEtude(etudeId).then(setEtude).catch(function () {})
    Promise.all([
      chargerEvolutionEtude(etudeId).then(setEvolution).catch(function () {}),
      rechargerIndicateurs().catch(function () {}),
    ]).finally(function () { setChargement(false) })
  }, [etudeId])

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('suivi.eyebrow')}
        titre={_t('suivi.titre')}
        description={etude ? etude.nom : 'Indicateurs de risque et évolution dans le temps.'}
      />
      <p className="mb-6 text-[11px] text-steel">
        <Link to={'/etudes/' + etudeId} className="text-signature hover:underline">retour à l'étude</Link>
      </p>

      {chargement && <p className="text-sm text-steel">Chargement...</p>}

      {!chargement && (
        <div className="space-y-12">
          <SectionEvolution evolution={evolution} />
          <SectionIndicateursAuto autos={autos} />
          <SectionIndicateursManuels
            etudeId={etudeId}
            indicateurs={manuels}
            onChange={function () { rechargerIndicateurs() }}
          />
        </div>
      )}
    </div>
  )
}

function SectionEvolution(props: { evolution: EvolutionEtude | null }) {
  var e = props.evolution
  return (
    <section>
      <h2 className="mb-3 font-mono text-[11px] tracking-wide text-steel-light">ÉVOLUTION DEPUIS LA DERNIÈRE REVUE (N / N-1)</h2>
      {!e ? (
        <EmptyState message="Aucune validation de l'Atelier 5 pour l'instant — pas de point de comparaison." />
      ) : !e.precedente ? (
        <p className="text-xs text-steel">
          Première validation ({e.courante.libelle || 'v' + e.courante.version}, {new Date(e.courante.dateUtc).toLocaleDateString('fr-FR')}).
          Une prochaine revalidation de l'Atelier 5 créera un point de comparaison.
        </p>
      ) : (
        <>
          <p className="mb-3 text-xs text-steel">
            {e.precedente.libelle || 'v' + e.precedente.version} ({new Date(e.precedente.dateUtc).toLocaleDateString('fr-FR')})
            {' → '}
            {e.courante.libelle || 'v' + e.courante.version} ({new Date(e.courante.dateUtc).toLocaleDateString('fr-FR')})
          </p>
          <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
            {[
              ['Mesures', e.mesures.total, e.mesures.total - e.mesures.totalPrecedent],
              ['Mesures terminées', e.mesures.terminees, e.mesures.terminees - e.mesures.termineesPrecedent],
              ['Mesures ajoutées / retirées', e.mesures.ajoutees + ' / ' + e.mesures.retirees, null],
            ].map(function (c) {
              var delta = c[2] as number | null
              return (
                <div key={c[0] as string} className="border border-paper-line p-3">
                  <div className="font-display text-xl text-ink">
                    {c[1]}
                    {delta != null && delta !== 0 && <span className={'ml-1 text-xs ' + (delta > 0 ? 'text-risk-low' : 'text-steel')}>({delta > 0 ? '+' : ''}{delta})</span>}
                  </div>
                  <div className="font-mono text-[10px] text-steel-light">{(c[0] as string).toUpperCase()}</div>
                </div>
              )
            })}
          </div>
          <ul className="divide-y divide-paper-line border-y border-paper-line">
            {e.scenarios.map(function (s, i) {
              return (
                <li key={i} className="flex items-center justify-between gap-4 py-2 text-xs">
                  <span className="text-ink">{s.libelle}</span>
                  <span className="shrink-0 font-mono text-[11px]">
                    <span className="text-steel-light">{s.niveauResiduelPrecedent || '—'}</span>
                    {' → '}
                    <span className="text-ink">{s.niveauResiduelCourant || '—'}</span>
                    <span className={'ml-2 ' + CLASSE_TENDANCE[s.tendance]}>{FLECHE_TENDANCE[s.tendance]}</span>
                  </span>
                </li>
              )
            })}
          </ul>
        </>
      )}
    </section>
  )
}

function SectionIndicateursAuto(props: { autos: IndicateurAuto[] }) {
  return (
    <section>
      <h2 className="mb-3 font-mono text-[11px] tracking-wide text-steel-light">INDICATEURS AUTOMATIQUES (état courant)</h2>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {props.autos.map(function (a) {
          var atteint = a.cible != null && (a.sens === 'Baisse' ? a.valeur <= a.cible : a.valeur >= a.cible)
          return (
            <div key={a.nom} className="border border-paper-line p-3">
              <div className="flex items-baseline justify-between">
                <span className={'font-display text-2xl ' + (atteint ? 'text-risk-low' : 'text-ink')}>{a.valeur}{a.unite}</span>
                {a.cible != null && <span className="font-mono text-[10px] text-steel-light">cible {a.cible}{a.unite}</span>}
              </div>
              <div className="mt-1 text-xs text-ink">{a.nom}</div>
              <div className="font-mono text-[10px] text-steel-light">{a.categorie}</div>
            </div>
          )
        })}
      </div>
    </section>
  )
}

function SectionIndicateursManuels(props: { etudeId: string; indicateurs: IndicateurSuivi[]; onChange: () => void }) {
  var [nom, setNom] = useState('')
  var [unite, setUnite] = useState('')
  var [cible, setCible] = useState('')
  var [seuil, setSeuil] = useState('')
  var [sens, setSens] = useState<SensAmelioration>('Baisse')

  function ajouter() {
    if (!nom.trim()) return
    creerIndicateur(props.etudeId, {
      nom: nom, unite: unite || undefined,
      cible: cible === '' ? null : Number(cible),
      seuilAlerte: seuil === '' ? null : Number(seuil),
      sens: sens,
    })
      .then(function () { toastSucces('Indicateur cree.'); setNom(''); setUnite(''); setCible(''); setSeuil(''); props.onChange() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  return (
    <section>
      <h2 className="mb-3 font-mono text-[11px] tracking-wide text-steel-light">INDICATEURS SUIVIS MANUELLEMENT</h2>

      <div className="mb-4 border border-paper-line p-4">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">NOUVEL INDICATEUR</div>
        <div className="grid gap-2 sm:grid-cols-2">
          <input type="text" placeholder="Nom (ex. incidents de securite / mois)" value={nom} onChange={function (e) { setNom(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
          <input type="text" placeholder="Unite (%, jours...)" value={unite} onChange={function (e) { setUnite(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
          <input type="number" placeholder="Cible (optionnel)" value={cible} onChange={function (e) { setCible(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
          <input type="number" placeholder="Seuil d alerte (optionnel)" value={seuil} onChange={function (e) { setSeuil(e.target.value) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none" />
          <select value={sens} onChange={function (e) { setSens(e.target.value as SensAmelioration) }} className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
            <option value="Baisse">Plus bas = mieux</option>
            <option value="Hausse">Plus haut = mieux</option>
          </select>
        </div>
        <div className="mt-3"><Button variante="primary" onClick={ajouter} disabled={!nom.trim()}>Creer l indicateur</Button></div>
      </div>

      {props.indicateurs.length === 0 ? (
        <EmptyState message="Aucun indicateur manuel. Ajoutez-en un pour suivre une valeur dans le temps." />
      ) : (
        <div className="space-y-3">
          {props.indicateurs.map(function (i) {
            return <LigneIndicateur key={i.id} etudeId={props.etudeId} indicateur={i} onChange={props.onChange} />
          })}
        </div>
      )}
    </section>
  )
}

function LigneIndicateur(props: { etudeId: string; indicateur: IndicateurSuivi; onChange: () => void }) {
  var i = props.indicateur
  var [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  var [valeur, setValeur] = useState('')
  var [ajoutOuvert, setAjoutOuvert] = useState(false)

  function ajouterPoint() {
    if (valeur === '') return
    ajouterPointIndicateur(props.etudeId, i.id, { date: date, valeur: Number(valeur) })
      .then(function () { toastSucces('Point ajoute.'); setValeur(''); setAjoutOuvert(false); props.onChange() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function retirer() {
    if (!window.confirm('Supprimer l indicateur "' + i.nom + '" et son historique ?')) return
    supprimerIndicateur(props.etudeId, i.id)
      .then(function () { toastSucces('Indicateur supprime.'); props.onChange() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  function retirerPoint(pointId: string) {
    supprimerPointIndicateur(props.etudeId, i.id, pointId)
      .then(function () { props.onChange() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Erreur.') })
  }

  var dernier = i.points.length > 0 ? i.points[i.points.length - 1] : null
  var alerte = enAlerte(i)

  return (
    <div className={'border p-3 ' + (alerte ? 'border-risk-critical/40 bg-risk-critical/5' : 'border-paper-line')}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="text-sm font-medium text-ink">{i.nom}</div>
          <div className="font-mono text-[10px] text-steel-light">
            {i.categorie ? i.categorie + ' — ' : ''}{i.sens === 'Baisse' ? 'plus bas = mieux' : 'plus haut = mieux'}
            {i.cible != null && ' — cible ' + i.cible + (i.unite || '')}
            {i.seuilAlerte != null && ' — alerte ' + i.seuilAlerte + (i.unite || '')}
          </div>
        </div>
        <div className="flex items-center gap-4">
          {dernier && (
            <span className={'font-display text-2xl ' + (alerte ? 'text-risk-critical' : 'text-ink')}>
              {dernier.valeur}{i.unite || ''}
            </span>
          )}
          <Sparkline valeurs={i.points.map(function (p) { return p.valeur })} cible={i.cible} couleur={alerte ? '#B34000' : '#000091'} />
          <button onClick={retirer} aria-label="Supprimer" className="text-steel-light transition hover:text-risk-critical"><Trash2 size={14} /></button>
        </div>
      </div>

      <div className="mt-2 flex flex-wrap items-center gap-2 text-[11px]">
        {i.points.map(function (p) {
          return (
            <span key={p.id} className="inline-flex items-center gap-1 border border-paper-line px-1.5 py-0.5 font-mono text-[10px] text-steel">
              {p.date} : {p.valeur}
              <button onClick={function () { retirerPoint(p.id) }} className="text-steel-light hover:text-risk-critical">×</button>
            </span>
          )
        })}
        {!ajoutOuvert ? (
          <button onClick={function () { setAjoutOuvert(true) }} className="font-mono text-[10px] text-signature hover:underline">+ point</button>
        ) : (
          <span className="inline-flex items-center gap-1">
            <input type="date" value={date} onChange={function (e) { setDate(e.target.value) }} className="border-b border-paper-line bg-transparent py-0.5 text-[11px] text-ink focus:border-signature focus:outline-none" />
            <input type="number" placeholder="valeur" value={valeur} onChange={function (e) { setValeur(e.target.value) }} className="w-20 border-b border-paper-line bg-transparent py-0.5 text-[11px] text-ink focus:border-signature focus:outline-none" />
            <button onClick={ajouterPoint} className="font-mono text-[10px] text-signature hover:underline">OK</button>
            <button onClick={function () { setAjoutOuvert(false) }} className="font-mono text-[10px] text-steel-light hover:text-ink">annuler</button>
          </span>
        )}
      </div>
    </div>
  )
}
