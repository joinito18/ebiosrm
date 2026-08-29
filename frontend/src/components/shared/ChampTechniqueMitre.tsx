import { useEffect, useRef, useState } from 'react'
import { listerTechniquesMitre } from '../../lib/api'
import type { PhaseActionElementaire, TechniqueMitre } from '../../lib/api'
import { traduire } from '../../lib/i18n'

/**
 * Sélecteur de technique MITRE ATT&CK pour une action élémentaire. Bouton
 * compact affichant la technique courante ; au clic, liste déroulante filtrée
 * sur la phase EBIOS de l'action + recherche plein texte. « Aucune » remet à
 * vide.
 */
export default function ChampTechniqueMitre(props: {
  valeur?: string | null
  phase: PhaseActionElementaire
  onChange: (technique: string | null) => void
}) {
  var [ouvert, setOuvert] = useState(false)
  var [q, setQ] = useState('')
  var [items, setItems] = useState<TechniqueMitre[]>([])
  var conteneur = useRef<HTMLDivElement>(null)

  useEffect(function () {
    if (!ouvert) return
    var annule = false
    var minuteur = setTimeout(function () {
      listerTechniquesMitre(props.phase, q)
        .then(function (r) { if (!annule) setItems(r) })
        .catch(function () { if (!annule) setItems([]) })
    }, 150)
    return function () { annule = true; clearTimeout(minuteur) }
  }, [ouvert, q, props.phase])

  useEffect(function () {
    function auClic(e: MouseEvent) {
      if (conteneur.current && !conteneur.current.contains(e.target as Node)) setOuvert(false)
    }
    if (ouvert) document.addEventListener('mousedown', auClic)
    return function () { document.removeEventListener('mousedown', auClic) }
  }, [ouvert])

  return (
    <div ref={conteneur} className="relative">
      <button
        type="button"
        onClick={function () { setOuvert(!ouvert) }}
        className={'w-full truncate border-b border-paper-line bg-transparent py-1 text-left text-[11px] focus:border-signature focus:outline-none ' + (props.valeur ? 'text-signature' : 'text-steel-light')}
        title={props.valeur ? traduire('cmp.mitreTitre') + ' ' + props.valeur : traduire('cmp.mitreAssocier')}
      >
        {props.valeur || '+ ATT&CK'}
      </button>

      {ouvert && (
        <div className="absolute right-0 z-20 mt-1 w-72 border border-signature/40 bg-paper p-2 shadow-lg">
          <input
            type="text"
            autoFocus
            placeholder={traduire('cmp.mitreRecherche')}
            value={q}
            onChange={function (e) { setQ(e.target.value) }}
            className="mb-1.5 w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none"
          />
          <button
            type="button"
            onClick={function () { props.onChange(null); setOuvert(false) }}
            className="mb-1 block w-full px-1 py-1 text-left text-[11px] text-steel-light hover:bg-signature/5"
          >
            {traduire('cmp.mitreAucune')}
          </button>
          <ul className="max-h-56 overflow-y-auto border-t border-paper-line">
            {items.map(function (t) {
              return (
                <li key={t.id}>
                  <button
                    type="button"
                    onClick={function () { props.onChange(t.id); setOuvert(false) }}
                    className="block w-full px-1 py-1.5 text-left text-[11px] text-ink hover:bg-signature/5"
                  >
                    <span className="font-mono text-signature">{t.id}</span> {t.nom}
                    <span className="block text-[10px] text-steel-light">{t.tactique}</span>
                  </button>
                </li>
              )
            })}
            {items.length === 0 && <li className="px-1 py-2 text-[11px] text-steel-light">{traduire('cmp.mitreAucuneTech')}</li>}
          </ul>
        </div>
      )}
    </div>
  )
}
