import { useState } from 'react'
import { telechargerRapport, ApiError } from '../../lib/api'
import { toastErreur } from '../../lib/toast'
import { traduire } from '../../lib/i18n'

export default function BoutonTelechargerRapport(props: { path: string; nomFichier: string; children: React.ReactNode; className?: string }) {
  var [enCours, setEnCours] = useState(false)

  function handleClick() {
    setEnCours(true)
    telechargerRapport(props.path, props.nomFichier)
      .catch(function (err) {
        toastErreur(err instanceof ApiError ? err.message : traduire('cmp.errTelechargement'))
      })
      .finally(function () { setEnCours(false) })
  }

  return (
    <button
      onClick={handleClick}
      disabled={enCours}
      aria-busy={enCours}
      className={props.className}
      style={enCours ? { opacity: 0.55, cursor: 'wait' } : undefined}
    >
      {props.children}
    </button>
  )
}
