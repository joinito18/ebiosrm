import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { connexion, ApiError } from '../lib/api'
import LayoutAuth from '../components/auth/LayoutAuth'

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
    <LayoutAuth>
      <div className="mb-8">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">BIENVENUE</div>
        <h1 className="font-display text-3xl text-ink">Connexion</h1>
      </div>

      <form onSubmit={soumettre} className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
        <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">EMAIL</label>
        <input
          type="email"
          value={email}
          onChange={function (e) { setEmail(e.target.value) }}
          className="mb-5 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          autoFocus
        />

        <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">MOT DE PASSE</label>
        <input
          type="password"
          value={motDePasse}
          onChange={function (e) { setMotDePasse(e.target.value) }}
          className="mb-2 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
        />

        <div className="mb-5 text-right">
          <Link to="/mot-de-passe-oublie" className="text-[11px] text-steel hover:text-signature hover:underline">
            Mot de passe oublie ?
          </Link>
        </div>

        {erreur && <p className="mb-4 text-xs text-risk-critical">{erreur}</p>}

        <button
          type="submit"
          disabled={enCours}
          className="w-full rounded-sm bg-signature px-4 py-2.5 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
        >
          {enCours ? 'Connexion...' : 'Se connecter'}
        </button>
      </form>

      <p className="mt-6 text-center text-xs text-steel">
        Pas encore de compte ? <Link to="/inscription" className="font-medium text-signature hover:underline">Creer un compte</Link>
      </p>
    </LayoutAuth>
  )
}
