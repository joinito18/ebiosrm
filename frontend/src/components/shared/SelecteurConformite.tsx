import { useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'
import { listerExigencesConformite } from '../../lib/api'
import type { ExigenceConformite } from '../../lib/api'

/**
 * Sélecteur multiple d'exigences de conformité (ISO 27001 / NIS2) couvertes
 * par une mesure de traitement. Chips retirables + liste déroulante avec
 * recherche et filtre par référentiel.
 */
export default function SelecteurConformite(props: {
  valeurs: string[]
  onChange: (codes: string[]) => void
}) {
  var [ouvert, setOuvert] = useState(false)
  var [q, setQ] = useState('')
  var [referentiel, setReferentiel] = useState('')
  var [exigences, setExigences] = useState<ExigenceConformite[]>([])
  var conteneur = useRef<HTMLDivElement>(null)

  useEffect(function () {
    listerExigencesConformite().then(setExigences).catch(function () { setExigences([]) })
  }, [])

  useEffect(function () {
    function auClic(e: MouseEvent) {
      if (conteneur.current && !conteneur.current.contains(e.target as Node)) setOuvert(false)
    }
    if (ouvert) document.addEventListener('mousedown', auClic)
    return function () { document.removeEventListener('mousedown', auClic) }
  }, [ouvert])

  function bascule(code: string) {
    if (props.valeurs.indexOf(code) >= 0) props.onChange(props.valeurs.filter(function (c) { return c !== code }))
    else props.onChange(props.valeurs.concat([code]))
  }

  var terme = q.trim().toLowerCase()
  var filtrees = exigences.filter(function (e) {
    if (referentiel && e.referentiel !== referentiel) return false
    if (!terme) return true
    return e.code.toLowerCase().indexOf(terme) >= 0 || e.titre.toLowerCase().indexOf(terme) >= 0
  }).slice(0, 60)

  function libelle(code: string) {
    var e = exigences.filter(function (x) { return x.code === code })[0]
    return e ? code : code
  }

  return (
    <div ref={conteneur} className="relative">
      <div className="flex flex-wrap items-center gap-1">
        {props.valeurs.map(function (code) {
          return (
            <span key={code} className="inline-flex items-center gap-1 border border-paper-line bg-paper-dim px-1.5 py-0.5 font-mono text-[10px] text-ink">
              {libelle(code)}
              <button type="button" onClick={function () { bascule(code) }} className="text-steel-light hover:text-risk-critical"><X size={10} /></button>
            </span>
          )
        })}
        <button type="button" onClick={function () { setOuvert(!ouvert) }} className="font-mono text-[10px] text-signature hover:underline">
          + Conformite
        </button>
      </div>

      {ouvert && (
        <div className="absolute left-0 z-20 mt-1 w-80 border border-signature/40 bg-paper p-2 shadow-lg">
          <div className="mb-1.5 flex gap-1">
            {[['', 'Tous'], ['Iso27001', 'ISO 27001'], ['Nis2', 'NIS2']].map(function (f) {
              var actif = referentiel === f[0]
              return (
                <button key={f[0]} type="button" onClick={function () { setReferentiel(f[0]) }} className={'border px-1.5 py-0.5 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}>{f[1]}</button>
              )
            })}
          </div>
          <input
            type="text"
            autoFocus
            placeholder="Rechercher (A.8.24, incidents...)"
            value={q}
            onChange={function (e) { setQ(e.target.value) }}
            className="mb-1.5 w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none"
          />
          <ul className="max-h-56 overflow-y-auto border-t border-paper-line">
            {filtrees.map(function (e) {
              var choisi = props.valeurs.indexOf(e.code) >= 0
              return (
                <li key={e.referentiel + e.code}>
                  <button
                    type="button"
                    onClick={function () { bascule(e.code) }}
                    className={'block w-full px-1 py-1.5 text-left text-[11px] hover:bg-signature/5 ' + (choisi ? 'text-signature' : 'text-ink')}
                  >
                    <span className="font-mono">{choisi ? '✓ ' : ''}{e.code}</span> {e.titre}
                    <span className="block text-[10px] text-steel-light">{e.referentiel === 'Nis2' ? 'NIS2' : 'ISO 27001'} — {e.categorie}</span>
                  </button>
                </li>
              )
            })}
            {filtrees.length === 0 && <li className="px-1 py-2 text-[11px] text-steel-light">Aucune exigence.</li>}
          </ul>
        </div>
      )}
    </div>
  )
}
