import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import { useT } from '../lib/i18n'
import EmptyState from '../components/shared/EmptyState'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import { chargerConformiteEtude, getEtude } from '../lib/api'
import type { RapportConformite, CouvertureConformite, Etude } from '../lib/api'

var LIBELLE_COUVERTURE: { [key in CouvertureConformite]: string } = {
  Conforme: 'Conforme',
  Partielle: 'Partielle',
  NonCouverte: 'Non couverte',
  NonApplicable: 'Non applicable',
}

var CLASSE_COUVERTURE: { [key in CouvertureConformite]: string } = {
  Conforme: 'text-risk-low',
  Partielle: 'text-risk-high',
  NonCouverte: 'text-risk-critical',
  NonApplicable: 'text-steel-light',
}

export default function ConformiteEtude() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var _t = useT()
  var [referentiel, setReferentiel] = useState<'Iso27001' | 'Nis2'>('Iso27001')
  var [rapport, setRapport] = useState<RapportConformite | null>(null)
  var [etude, setEtude] = useState<Etude | null>(null)
  var [chargement, setChargement] = useState(true)
  var [erreur, setErreur] = useState('')

  useEffect(function () { getEtude(etudeId).then(setEtude).catch(function () {}) }, [etudeId])

  useEffect(function () {
    setChargement(true)
    chargerConformiteEtude(etudeId, referentiel)
      .then(function (r) { setRapport(r); setErreur('') })
      .catch(function () { setErreur('Impossible de charger le tableau de conformite.') })
      .finally(function () { setChargement(false) })
  }, [etudeId, referentiel])

  var s = rapport ? rapport.synthese : null
  var couvertes = s ? s.conforme + s.partielle : 0
  var pertinent = s ? s.total - s.nonApplicable : 0

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('conformite.eyebrow')}
        titre={_t('conformite.titre')}
        description={etude ? etude.nom : 'Couverture des exigences réglementaires par le contenu de l’étude.'}
      />

      <p className="mb-6 border border-paper-line bg-paper-dim px-3 py-2 text-[11px] text-steel">
        Croisement du socle de sécurité (Atelier 1) et du plan de traitement (Atelier 5) avec les exigences du référentiel.
        La correspondance ISO&nbsp;27001&nbsp;&rarr;&nbsp;NIS2 est <strong>indicative</strong> et doit être validée par l’analyste.
        <Link to={'/etudes/' + etudeId} className="ml-2 text-signature hover:underline">retour à l’étude</Link>
      </p>

      <div className="mb-4">
        <BoutonTelechargerRapport
          path={'/etudes/' + etudeId + '/rapports/conformite'}
          nomFichier={'conformite-' + etudeId + '.pdf'}
          className="inline-flex items-center gap-1.5 rounded-sm border border-paper-line px-3 py-1.5 text-xs font-medium text-ink transition hover:border-signature hover:text-signature"
        >
          Telecharger l annexe de conformite (PDF)
        </BoutonTelechargerRapport>
      </div>

      <div className="mb-6 flex gap-2 border-b border-paper-line">
        {[['Iso27001', 'ISO 27001'], ['Nis2', 'NIS2 (art. 21)']].map(function (o) {
          var actif = referentiel === o[0]
          return (
            <button
              key={o[0]}
              onClick={function () { setReferentiel(o[0] as 'Iso27001' | 'Nis2') }}
              className={'-mb-px border-b-2 px-3 py-2 text-xs font-medium transition ' + (actif ? 'border-signature text-signature' : 'border-transparent text-steel hover:text-ink')}
            >
              {o[1]}
            </button>
          )
        })}
      </div>

      {chargement && <p className="text-sm text-steel">Chargement...</p>}
      {!chargement && erreur && <div className="border border-risk-critical/30 bg-risk-critical/5 px-5 py-4 text-sm text-risk-critical">{erreur}</div>}

      {!chargement && !erreur && rapport && s && (
        <>
          <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              ['Conforme', s.conforme, 'text-risk-low'],
              ['Partielle', s.partielle, 'text-risk-high'],
              ['Non couverte', s.nonCouverte, 'text-risk-critical'],
              ['Non applicable', s.nonApplicable, 'text-steel-light'],
            ].map(function (c) {
              return (
                <div key={c[0] as string} className="border border-paper-line p-3">
                  <div className={'font-display text-2xl ' + (c[2] as string)}>{c[1]}</div>
                  <div className="font-mono text-[10px] text-steel-light">{(c[0] as string).toUpperCase()}</div>
                </div>
              )
            })}
          </div>

          <p className="mb-4 text-xs text-steel">
            {couvertes} / {pertinent} exigences applicables adressées{s.nonApplicable > 0 ? ' (' + s.nonApplicable + ' non applicable' + (s.nonApplicable > 1 ? 's' : '') + ')' : ''}.
          </p>

          {rapport.lignes.length === 0 ? (
            <EmptyState message="Aucune exigence." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[640px] border-collapse text-sm">
                <thead>
                  <tr className="border-b border-paper-line text-left">
                    <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">CODE</th>
                    <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">EXIGENCE</th>
                    <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">COUVERTURE</th>
                    <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">MESURES / SOCLE</th>
                  </tr>
                </thead>
                <tbody>
                  {rapport.lignes.map(function (l) {
                    return (
                      <tr key={l.code} className="border-b border-paper-line align-top">
                        <td className="py-2 font-mono text-xs text-ink">{l.code}</td>
                        <td className="py-2 pr-4 text-xs text-ink">
                          {l.titre}
                          <span className="block font-mono text-[10px] text-steel-light">{l.categorie}</span>
                        </td>
                        <td className={'py-2 font-mono text-[11px] ' + CLASSE_COUVERTURE[l.couverture]}>{LIBELLE_COUVERTURE[l.couverture]}</td>
                        <td className="py-2 text-[11px] text-steel">
                          {l.etatSocle && <span className="mr-2 font-mono text-[10px] text-steel-light">socle : {l.etatSocle}</span>}
                          {l.mesures.map(function (m) { return <div key={m.id}>&bull; {m.description}</div> })}
                          {!l.etatSocle && l.mesures.length === 0 && <span className="text-steel-light">&mdash;</span>}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  )
}
