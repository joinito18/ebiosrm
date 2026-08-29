import { useEffect, useState } from 'react'
import { X } from 'lucide-react'

/**
 * Panneau de sélection d'un élément de bibliothèque (mesure, source de risque).
 * Rendu en ligne sous le bouton qui l'ouvre, pas en modale : cohérent avec les
 * autres formulaires d'AtelierPage et sans piège de superposition.
 */
export default function SelecteurBibliotheque<T>(props: {
  titre: string
  charger: (q: string) => Promise<T[]>
  cle: (item: T) => string
  rendre: (item: T) => React.ReactNode
  onChoisir: (item: T) => void
  onFermer: () => void
  filtres?: { valeur: string; libelle: string }[]
  filtreActif?: string
  onFiltre?: (valeur: string) => void
}) {
  var [q, setQ] = useState('')
  var [items, setItems] = useState<T[]>([])
  var [chargement, setChargement] = useState(true)
  var [erreur, setErreur] = useState('')

  useEffect(function () {
    var annule = false
    setChargement(true)
    var minuteur = setTimeout(function () {
      props.charger(q)
        .then(function (r) { if (!annule) { setItems(r); setErreur('') } })
        .catch(function () { if (!annule) setErreur('Bibliotheque indisponible.') })
        .finally(function () { if (!annule) setChargement(false) })
    }, 200)
    return function () { annule = true; clearTimeout(minuteur) }
  }, [q, props.filtreActif])

  return (
    <div className="mb-3 border border-signature/40 bg-paper-dim p-3">
      <div className="mb-2 flex items-center justify-between">
        <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.titre.toUpperCase()}</span>
        <button onClick={props.onFermer} className="text-steel-light hover:text-ink"><X size={14} /></button>
      </div>

      <input
        type="text"
        autoFocus
        placeholder="Rechercher..."
        value={q}
        onChange={function (e) { setQ(e.target.value) }}
        className="mb-2 w-full border-b border-paper-line bg-transparent py-1 text-sm text-ink focus:border-signature focus:outline-none"
      />

      {props.filtres && props.filtres.length > 0 && (
        <div className="mb-2 flex flex-wrap gap-1.5">
          {props.filtres.map(function (f) {
            var actif = (props.filtreActif || '') === f.valeur
            return (
              <button
                key={f.valeur}
                onClick={function () { if (props.onFiltre) props.onFiltre(f.valeur) }}
                className={'border px-2 py-0.5 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}
              >
                {f.libelle}
              </button>
            )
          })}
        </div>
      )}

      {chargement && <p className="py-2 text-xs text-steel-light">Chargement...</p>}
      {erreur && <p className="py-2 text-xs text-risk-critical">{erreur}</p>}

      {!chargement && !erreur && (
        items.length === 0 ? (
          <p className="py-2 text-xs text-steel-light">Aucun element.</p>
        ) : (
          <ul className="max-h-64 divide-y divide-paper-line overflow-y-auto border-y border-paper-line">
            {items.map(function (item) {
              return (
                <li key={props.cle(item)}>
                  <button
                    onClick={function () { props.onChoisir(item) }}
                    className="w-full px-1 py-2 text-left text-xs text-ink transition hover:bg-signature/5"
                  >
                    {props.rendre(item)}
                  </button>
                </li>
              )
            })}
          </ul>
        )
      )}
    </div>
  )
}
