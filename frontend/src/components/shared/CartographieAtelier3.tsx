import { useEffect, useState } from 'react'
import { chargerCartographieSvg } from '../../lib/api'
import type { CartographieType } from '../../lib/api'

function VueSvg(props: { etudeId: string; type: CartographieType; residuel?: boolean; rafraichir: number }) {
  var [svg, setSvg] = useState<string | null>(null)
  var [erreur, setErreur] = useState('')
  var [chargement, setChargement] = useState(true)

  useEffect(function () {
    var annule = false
    setChargement(true)
    chargerCartographieSvg(props.etudeId, props.type, props.residuel)
      .then(function (s) { if (!annule) { setSvg(s); setErreur('') } })
      .catch(function () { if (!annule) setErreur('Cartographie indisponible.') })
      .finally(function () { if (!annule) setChargement(false) })
    return function () { annule = true }
  }, [props.etudeId, props.type, props.residuel, props.rafraichir])

  if (chargement) return <p className="py-4 text-xs text-steel-light">Generation du schema...</p>
  if (erreur) return <p className="py-4 text-xs text-risk-critical">{erreur}</p>
  if (!svg) return null

  return (
    <div
      className="w-full overflow-x-auto [&_svg]:h-auto [&_svg]:w-full [&_svg]:max-w-[860px]"
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  )
}

/**
 * Cartographie graphique de l'Atelier 3 : radar de la dangerosité de
 * l'écosystème (cercles concentriques, méthode ANSSI) + arbre des scénarios
 * stratégiques et de leurs chemins d'attaque. Même géométrie que le rapport
 * PDF (générée côté serveur, cf. CartographieSvg).
 *
 * `rafraichir` : incrémenter cette prop force le rechargement des SVG après
 * une modification (évaluation d'une partie prenante, ajout d'un chemin...).
 */
export default function CartographieAtelier3(props: { etudeId: string; rafraichir: number }) {
  var [residuel, setResiduel] = useState(false)

  return (
    <div className="space-y-8">
      <section>
        <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
          <h3 className="font-mono text-[11px] tracking-wide text-steel-light">CARTOGRAPHIE DE LA DANGEROSITE DE L ECOSYSTEME</h3>
          <div className="flex gap-1">
            {[['Initiale', false], ['Apres mesures', true]].map(function (o) {
              var actif = residuel === o[1]
              return (
                <button
                  key={String(o[1])}
                  onClick={function () { setResiduel(o[1] as boolean) }}
                  className={'border px-2 py-0.5 font-mono text-[10px] transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}
                >
                  {o[0]}
                </button>
              )
            })}
          </div>
        </div>
        <VueSvg etudeId={props.etudeId} type="ecosysteme" residuel={residuel} rafraichir={props.rafraichir} />
      </section>

      <section>
        <h3 className="mb-2 font-mono text-[11px] tracking-wide text-steel-light">SCENARIOS STRATEGIQUES ET CHEMINS D ATTAQUE</h3>
        <VueSvg etudeId={props.etudeId} type="chemins-attaque" rafraichir={props.rafraichir} />
      </section>
    </div>
  )
}
