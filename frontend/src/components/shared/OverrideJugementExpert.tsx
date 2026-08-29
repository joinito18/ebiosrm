import { useState } from 'react'
import { useLectureSeule } from '../../lib/lectureSeule'

interface OverrideJugementExpertProps {
  valeurCalculee: string
  valeurRetenue?: string | null
  justification?: string | null
  options: { value: string; label: string }[]
  onDefinir: (valeurRetenue: string, justification: string) => Promise<unknown>
  onReinitialiser: () => Promise<unknown>
  // Valeur continue (ex. dangerosite) : saisie libre au lieu d'un select d'options fixes.
  valeurLibre?: boolean
}

// Bandeau repliable : le calcul reste consultable, mais jamais affiche en
// concurrence avec la valeur retenue (choix explicite -- une seule valeur
// visible partout ailleurs dans l'appli). Fermé par defaut.
export default function OverrideJugementExpert(props: OverrideJugementExpertProps) {
  var [ouvert, setOuvert] = useState(false)
  var [valeur, setValeur] = useState(props.valeurRetenue || (props.valeurLibre ? props.valeurCalculee : props.options[0]?.value) || '')
  var [justification, setJustification] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  var aUnEcart = !!props.valeurRetenue
  var lectureSeule = useLectureSeule()

  if (lectureSeule) {
    if (!aUnEcart) return null
    return (
      <div className="border-l-2 border-signature pl-3 text-xs text-steel">
        <span className="font-mono text-[10px] text-steel-light">Jugement d expert &mdash; valeur calculee : {props.valeurCalculee}.</span>
        {props.justification && <div className="mt-0.5">{props.justification}</div>}
      </div>
    )
  }

  function soumettre() {
    if (!justification.trim()) {
      setErreur('Une justification est obligatoire pour retenir une valeur differente du calcul.')
      return
    }
    setEnCours(true)
    setErreur('')
    props.onDefinir(valeur, justification)
      .then(function () { setJustification(''); setOuvert(false) })
      .catch(function () { setErreur('Erreur lors de l enregistrement.') })
      .finally(function () { setEnCours(false) })
  }

  function reinitialiser() {
    if (!window.confirm('Revenir a la valeur calculee automatiquement ?')) return
    setEnCours(true)
    props.onReinitialiser().finally(function () { setEnCours(false) })
  }

  if (!ouvert) {
    return (
      <button type="button" onClick={function () { setOuvert(true) }} className="font-mono text-[10px] text-steel-light hover:text-signature">
        {aUnEcart ? 'Jugement d expert (modifier)' : '+ Jugement d expert'}
      </button>
    )
  }

  return (
    <div className="space-y-1.5 border-l-2 border-signature pl-3">
      <div className="font-mono text-[10px] text-steel-light">Valeur calculee automatiquement : <span className="font-medium text-ink">{props.valeurCalculee}</span></div>
      {aUnEcart && props.justification && (
        <div className="text-xs text-steel"><span className="text-steel-light">Justification actuelle :</span> {props.justification}</div>
      )}
      <div className="grid grid-cols-[1fr_auto] gap-2">
        {props.valeurLibre ? (
          <input type="number" step="0.01" value={valeur} onChange={function (e) { setValeur(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none" />
        ) : (
          <select value={valeur} onChange={function (e) { setValeur(e.target.value) }} className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
            {props.options.map(function (o) { return <option key={o.value} value={o.value}>{o.label}</option> })}
          </select>
        )}
        {aUnEcart && <button type="button" onClick={reinitialiser} disabled={enCours} className="text-[11px] text-steel-light hover:text-risk-critical">Reinitialiser</button>}
      </div>
      <textarea
        placeholder="Justification (obligatoire)"
        value={justification}
        onChange={function (e) { setJustification(e.target.value) }}
        rows={2}
        className="w-full border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none"
      />
      {erreur && <p className="text-xs text-risk-critical">{erreur}</p>}
      <div className="flex gap-3">
        <button type="button" onClick={soumettre} disabled={enCours} className="text-xs font-medium text-signature hover:underline">{enCours ? 'Enregistrement...' : 'Retenir cette valeur'}</button>
        <button type="button" onClick={function () { setOuvert(false); setErreur('') }} className="text-xs text-steel-light hover:text-ink">Annuler</button>
      </div>
    </div>
  )
}
