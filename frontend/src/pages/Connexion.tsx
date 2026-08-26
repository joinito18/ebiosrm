import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { connexion, ApiError } from '../lib/api'

export default function Connexion() {
  var navigate = useNavigate()
  var [email, setEmail] = useState('')
  var [motDePasse, setMotDePasse] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim() || !motDePasse) {
      setErreur('Email et mot de passe sont obligatoires.')
      return
    }
    setErreur('')
    setEnCours(true)
    connexion(email.trim(), motDePasse)
      .then(function () { navigate('/etudes') })
      .catch(function (err) {
        var message = err instanceof ApiError && err.status !== 401 ? err.message : 'Email ou mot de passe incorrect.'
        setErreur(message)
      })
      .finally(function () { setEnCours(false) })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-paper px-6">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center font-display text-2xl text-ink">
          EBIOS<span className="text-signature">&middot;</span>RM
        </div>

        <form onSubmit={soumettre} className="border border-paper-line p-6">
          <h1 className="mb-5 font-mono text-[11px] tracking-wide text-steel-light">CONNEXION</h1>

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">EMAIL</label>
          <input
            type="email"
            value={email}
            onChange={function (e) { setEmail(e.target.value) }}
            className="mb-4 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
            autoFocus
          />

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">MOT DE PASSE</label>
          <input
            type="password"
            value={motDePasse}
            onChange={function (e) { setMotDePasse(e.target.value) }}
            className="mb-4 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          />

          {erreur && <p className="mb-4 text-xs text-risk-critical">{erreur}</p>}

          <button
            type="submit"
            disabled={enCours}
            className="w-full rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
          >
            {enCours ? 'Connexion...' : 'Se connecter'}
          </button>

          <p className="mt-5 text-center text-xs text-steel">
            Pas encore de compte ? <Link to="/inscription" className="text-signature hover:underline">Creer un compte</Link>
          </p>
        </form>
      </div>
    </div>
  )
}
