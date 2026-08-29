import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getEtude, listerJournal } from '../lib/api'
import type { Etude, EntreeJournal } from '../lib/api'
import { useT, langueCourante } from '../lib/i18n'

var COULEUR_METHODE: Record<string, string> = {
  POST: 'text-risk-low',
  PUT: 'text-risk-moderate',
  PATCH: 'text-risk-moderate',
  DELETE: 'text-risk-critical',
}

function formatDate(iso: string): string {
  var loc = langueCourante() === 'en' ? 'en-GB' : 'fr-FR'
  var d = new Date(iso)
  return d.toLocaleDateString(loc) + ' ' + d.toLocaleTimeString(loc, { hour: '2-digit', minute: '2-digit' })
}

export default function JournalEtude() {
  var params = useParams()
  var _t = useT()
  var etudeId = params.etudeId as string
  var [etude, setEtude] = useState<Etude | null>(null)
  var [entrees, setEntrees] = useState<EntreeJournal[]>([])
  var [chargement, setChargement] = useState(true)

  useEffect(function () {
    setChargement(true)
    Promise.all([getEtude(etudeId), listerJournal(etudeId, 500)])
      .then(function (r) {
        setEtude(r[0])
        setEntrees(r[1] || [])
      })
      .finally(function () { setChargement(false) })
  }, [etudeId])

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <div className="mb-8 border-b border-paper-line pb-6">
        <Link to={'/etudes/' + etudeId} className="font-mono text-[11px] tracking-wide text-steel hover:text-signature">
          &larr; {etude ? etude.nom : _t('commun.retour')}
        </Link>
        <h1 className="mt-3 font-display text-3xl text-ink">{_t('journal.titre')}</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-steel">{_t('journal.intro')}</p>
      </div>

      {chargement && <p className="text-sm text-steel">{_t('commun.chargement')}</p>}

      {!chargement && entrees.length === 0 && (
        <p className="text-sm text-steel-light">{_t('journal.aucune')}</p>
      )}

      {!chargement && entrees.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] border-collapse text-sm">
            <thead>
              <tr className="border-b border-paper-line text-left">
                <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('journal.col.date').toUpperCase()}</th>
                <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('journal.col.auteur').toUpperCase()}</th>
                <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('journal.col.action').toUpperCase()}</th>
              </tr>
            </thead>
            <tbody>
              {entrees.map(function (e) {
                return (
                  <tr key={e.id} className="border-b border-paper-line/60">
                    <td className="whitespace-nowrap py-2.5 pr-4 font-mono text-[11px] text-steel-light">{formatDate(e.dateUtc)}</td>
                    <td className="py-2.5 pr-4 text-ink">{e.nomUtilisateur}</td>
                    <td className="py-2.5 text-steel">
                      <span className={'mr-2 font-mono text-[10px] ' + (COULEUR_METHODE[e.methode] || 'text-steel-light')}>{e.methode}</span>
                      {e.action}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
