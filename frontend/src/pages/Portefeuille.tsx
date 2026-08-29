import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import EmptyState from '../components/shared/EmptyState'
import BadgeStatutAtelier from '../components/shared/BadgeStatutAtelier'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import { chargerPortefeuille } from '../lib/api'
import type { LignePortefeuille } from '../lib/api'
import { useT } from '../lib/i18n'

export default function Portefeuille() {
  var navigate = useNavigate()
  var t = useT()
  var [lignes, setLignes] = useState<LignePortefeuille[]>([])
  var [chargement, setChargement] = useState(true)
  var [erreur, setErreur] = useState('')

  useEffect(function () {
    chargerPortefeuille()
      .then(function (d) { setLignes(d || []); setErreur('') })
      .catch(function () { setErreur(t('portefeuille.indispo')) })
      .finally(function () { setChargement(false) })
  }, [])

  function somme(cle: keyof LignePortefeuille) {
    return lignes.reduce(function (t, l) { return t + (Number(l[cle]) || 0) }, 0)
  }
  var totalEleve = lignes.reduce(function (t, l) { return t + (l.risquesResiduels.Eleve || 0) }, 0)

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader eyebrow={t('portefeuille.eyebrow')} titre={t('portefeuille.titre')} description={t('portefeuille.desc')} />

      {!chargement && !erreur && lignes.length > 0 && (
        <div className="mb-4">
          <BoutonTelechargerRapport
            path="/portefeuille/export.xlsx"
            nomFichier="portefeuille.xlsx"
            className="inline-flex items-center gap-1.5 rounded-sm border border-paper-line px-3 py-1.5 text-xs font-medium text-ink transition hover:border-signature hover:text-signature"
          >
            {t('portefeuille.export')}
          </BoutonTelechargerRapport>
        </div>
      )}

      {chargement && <p className="text-sm text-steel">{t('commun.chargement')}</p>}
      {!chargement && erreur && <div className="border border-risk-critical/30 bg-risk-critical/5 px-5 py-4 text-sm text-risk-critical">{erreur}</div>}

      {!chargement && !erreur && (lignes.length === 0 ? (
        <EmptyState message={t('portefeuille.aucune')} />
      ) : (
        <>
          <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              [t('nav.etudes'), lignes.length, 'text-ink'],
              [t('portefeuille.stat.eleves'), totalEleve, totalEleve > 0 ? 'text-risk-critical' : 'text-risk-low'],
              [t('portefeuille.col.retard'), somme('mesuresEnRetard'), somme('mesuresEnRetard') > 0 ? 'text-risk-high' : 'text-risk-low'],
              [t('portefeuille.stat.mesures'), somme('mesuresTerminees') + ' / ' + somme('mesures'), 'text-ink'],
            ].map(function (c) {
              return (
                <div key={c[0] as string} className="border border-paper-line p-3">
                  <div className={'font-display text-2xl ' + (c[2] as string)}>{c[1]}</div>
                  <div className="font-mono text-[10px] text-steel-light">{(c[0] as string).toUpperCase()}</div>
                </div>
              )
            })}
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-paper-line text-left">
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{t('portefeuille.col.etude').toUpperCase()}</th>
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">A5</th>
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{t('portefeuille.col.residuels').toUpperCase()}</th>
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{t('portefeuille.col.avancement').toUpperCase()}</th>
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{t('portefeuille.col.retard').toUpperCase()}</th>
                  <th className="pb-2 text-right font-mono text-[9px] font-normal tracking-wide text-steel-light">NIS2</th>
                </tr>
              </thead>
              <tbody>
                {lignes.map(function (l) {
                  var r = l.risquesResiduels
                  return (
                    <tr
                      key={l.etudeId}
                      onClick={function () { navigate('/etudes/' + l.etudeId) }}
                      className="cursor-pointer border-b border-paper-line transition hover:bg-paper-dim/50"
                    >
                      <td className="py-3 text-sm font-medium text-ink">
                        {l.nom}
                        {l.risquesEleveResiduelNonAcceptes > 0 && (
                          <span className="ml-2 font-mono text-[10px] text-risk-critical">{l.risquesEleveResiduelNonAcceptes} {t('portefeuille.nonAcceptes')}</span>
                        )}
                      </td>
                      <td className="py-3"><BadgeStatutAtelier statut={l.statutAtelier5} /></td>
                      <td className="py-3 font-mono text-[11px] text-steel">
                        <span className="text-risk-low">{r.Faible || 0}</span> / <span className="text-risk-high">{r.Moyen || 0}</span> / <span className={(r.Eleve || 0) > 0 ? 'text-risk-critical' : 'text-steel'}>{r.Eleve || 0}</span>
                        {(r.NonEvalue || 0) > 0 && <span className="text-steel-light"> ({r.NonEvalue} {t('portefeuille.nonEvalue')})</span>}
                      </td>
                      <td className="py-3 font-mono text-[11px] text-steel">{l.mesuresTerminees} / {l.mesures}</td>
                      <td className={'py-3 font-mono text-[11px] ' + (l.mesuresEnRetard > 0 ? 'text-risk-high' : 'text-steel-light')}>{l.mesuresEnRetard || '—'}</td>
                      <td className="py-3 text-right font-mono text-[11px] text-steel">{l.tauxCouvertureNis2 == null ? '—' : l.tauxCouvertureNis2 + ' %'}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <p className="mt-3 text-[11px] text-steel-light">{t('portefeuille.noteRetard')}</p>
        </>
      ))}
    </div>
  )
}
