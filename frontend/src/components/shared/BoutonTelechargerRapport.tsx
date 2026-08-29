import { useState } from 'react'
import { telechargerRapport, ApiError } from '../../lib/api'
import { toastErreur } from '../../lib/toast'
import { traduire, langueCourante } from '../../lib/i18n'

// Ajoute ?langue=en aux telechargements de rapports/manuel quand l'IHM est en
// anglais -- sauf si l'appelant a deja precise la langue. Les exports Word/Excel
// et autres chemins ne sont pas concernes (le serveur les ignore de toute facon).
function avecLangue(path: string): string {
  if (langueCourante() !== 'en') return path
  if (path.indexOf('langue=') >= 0) return path
  if (path.indexOf('/rapports/') < 0 && path.indexOf('/aide/') < 0) return path
  return path + (path.indexOf('?') >= 0 ? '&' : '?') + 'langue=en'
}

export default function BoutonTelechargerRapport(props: { path: string; nomFichier: string; children: React.ReactNode; className?: string }) {
  var [enCours, setEnCours] = useState(false)

  function handleClick() {
    setEnCours(true)
    telechargerRapport(avecLangue(props.path), props.nomFichier)
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
