import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import Card from '../components/shared/Card'
import EmptyState from '../components/shared/EmptyState'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import { listEtudes, ApiError } from '../lib/api'
import type { Etude } from '../lib/api'
import { useT, useLangue } from '../lib/i18n'

var BOUTON_SECONDAIRE = 'flex items-center gap-1.5 rounded-sm border border-paper-line px-3 py-1.5 text-[11px] font-medium text-ink transition duration-200 ease-premium hover:border-signature hover:text-signature'

function AteliersValides(props: { etude: Etude }) {
  var e = props.etude
  var _t = useT()
  var langue = useLangue().langue
  var suffixe = langue === 'en' ? '?langue=en' : ''
  var ateliers = [
    { numero: 1, statut: e.statut },
    { numero: 2, statut: e.statutAtelier2 },
    { numero: 3, statut: e.statutAtelier3 },
    { numero: 4, statut: e.statutAtelier4 },
    { numero: 5, statut: e.statutAtelier5 },
  ].filter(function (a) { return a.statut === 'Validee' })

  if (ateliers.length === 0) {
    return <span className="text-xs text-steel-light">{_t('rapports.aucunAtelier')}</span>
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {ateliers.map(function (a) {
        return (
          <BoutonTelechargerRapport
            key={a.numero}
            path={'/etudes/' + e.id + '/rapports/atelier' + a.numero + suffixe}
            nomFichier={'rapport-atelier' + a.numero + '-' + e.id + '.pdf'}
            className={BOUTON_SECONDAIRE}
          >
            {_t('rapports.atelier')} {a.numero}
          </BoutonTelechargerRapport>
        )
      })}
      {e.statutAtelier5 === 'Validee' && (
        <BoutonTelechargerRapport
          path={'/etudes/' + e.id + '/rapports/synthese' + suffixe}
          nomFichier={'synthese-' + e.id + '.pdf'}
          className={BOUTON_SECONDAIRE}
        >
          {_t('rapports.synthese')}
        </BoutonTelechargerRapport>
      )}
      {e.statutAtelier5 !== 'Brouillon' && (
        <BoutonTelechargerRapport
          path={'/etudes/' + e.id + '/rapports/cadre-de-suivi' + suffixe}
          nomFichier={'cadre-de-suivi-' + e.id + '.pdf'}
          className={BOUTON_SECONDAIRE}
        >
          {_t('rapports.cadreSuivi')}
        </BoutonTelechargerRapport>
      )}
    </div>
  )
}

export default function Rapports() {
  var navigate = useNavigate()
  var _t = useT()
  var [etudes, setEtudes] = useState<Etude[]>([])
  var [chargement, setChargement] = useState(true)
  var [erreur, setErreur] = useState('')

  useEffect(function () {
    listEtudes()
      .then(setEtudes)
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : _t('commun.apiIndispo')) })
      .finally(function () { setChargement(false) })
  }, [])

  var etudesAvecRapport = etudes.filter(function (e) {
    return e.statut === 'Validee' || e.statutAtelier2 === 'Validee' || e.statutAtelier3 === 'Validee' || e.statutAtelier4 === 'Validee' || e.statutAtelier5 === 'Validee'
  })

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('rapports.eyebrow')}
        titre={_t('rapports.titre')}
        description={_t('rapports.desc')}
      />

      {chargement && <p className="text-sm text-steel">{_t('commun.chargement')}</p>}

      {!chargement && erreur && (
        <div className="border border-risk-critical/30 bg-risk-critical/5 px-5 py-4 text-sm text-risk-critical">{erreur}</div>
      )}

      {!chargement && !erreur && etudesAvecRapport.length === 0 && (
        <EmptyState message={_t('rapports.aucunRapport')} />
      )}

      {!chargement && !erreur && etudesAvecRapport.length > 0 && (
        <div className="space-y-4">
          {etudesAvecRapport.map(function (etude) {
            return (
              <Card key={etude.id} variant="elevated" className="p-5">
                <div className="mb-3 flex items-center justify-between gap-4">
                  <button
                    onClick={function () { navigate('/etudes/' + etude.id) }}
                    className="font-display text-lg text-ink hover:text-signature"
                  >
                    {etude.nom}
                  </button>
                </div>
                <AteliersValides etude={etude} />
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
