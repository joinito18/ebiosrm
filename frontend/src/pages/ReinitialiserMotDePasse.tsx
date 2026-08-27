import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { reinitialiserMotDePasse, ApiError } from '../lib/api'
import LayoutAuth from '../components/auth/LayoutAuth'

export default function ReinitialiserMotDePasse() {
  var navigate = useNavigate()
  var [params] = useSearchParams()
  var token = params.get('token') || ''

  var [motDePasse, setMotDePasse] = useState('')
  var [confirmation, setConfirmation] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)
  var [reussi, setReussi] = useState(false)

  function soumettre(e: React.FormEvent) {
    e.preventDefault()
    if (motDePasse.length < 8) {
      setErreur('Le mot de passe doit contenir au moins 8 caracteres.')
      return
    }
    if (motDePasse !== confirmation) {
      setErreur('Les deux mots de passe ne correspondent pas.')
      return
    }
    setErreur('')
    setEnCours(true)
    reinitialiserMotDePasse(token, motDePasse)
      .then(function () {
        setReussi(true)
        setTimeout(function () { navigate('/connexion') }, 2500)
      })
      .catch(function (err) {
        setErreur(err instanceof ApiError ? err.message : 'Erreur lors de la reinitialisation.')
      })
      .finally(function () { setEnCours(false) })
  }

  return (
    <LayoutAuth>
      <div className="mb-8">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">NOUVEAU MOT DE PASSE</div>
        <h1 className="font-display text-3xl text-ink">Choisir un mot de passe</h1>
      </div>

      {!token ? (
        <div className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <p className="text-sm leading-relaxed text-ink">
            Ce lien de reinitialisation est incomplet ou invalide.
          </p>
          <p className="mt-3 text-xs text-steel-light">
            Refaites une demande depuis la page « Mot de passe oublie ».
          </p>
        </div>
      ) : reussi ? (
        <div className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <p className="text-sm leading-relaxed text-ink">
            Votre mot de passe a ete reinitialise. Redirection vers la connexion...
          </p>
        </div>
      ) : (
        <form onSubmit={soumettre} className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">NOUVEAU MOT DE PASSE</label>
          <input
            type="password"
            value={motDePasse}
            onChange={function (e) { setMotDePasse(e.target.value) }}
            className="mb-5 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
            autoFocus
          />

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">CONFIRMER LE MOT DE PASSE</label>
          <input
            type="password"
            value={confirmation}
            onChange={function (e) { setConfirmation(e.target.value) }}
            className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          />
          <p className="mb-5 mt-1.5 text-[10px] text-steel-light">8 caracteres minimum.</p>

          {erreur && <p className="mb-4 text-xs text-risk-critical">{erreur}</p>}

          <button
            type="submit"
            disabled={enCours}
            className="w-full rounded-sm bg-signature px-4 py-2.5 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
          >
            {enCours ? 'Enregistrement...' : 'Enregistrer le nouveau mot de passe'}
          </button>
        </form>
      )}

      <p className="mt-6 text-center text-xs text-steel">
        <Link to="/connexion" className="font-medium text-signature hover:underline">Retour a la connexion</Link>
      </p>
    </LayoutAuth>
  )
}
