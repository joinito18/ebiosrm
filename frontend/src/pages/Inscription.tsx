import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { inscription, ApiError } from '../lib/api'

export default function Inscription() {
  var navigate = useNavigate()
  var [nomAffiche, setNomAffiche] = useState('')
  var [email, setEmail] = useState('')
  var [motDePasse, setMotDePasse] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(e: React.FormEvent) {
    e.preventDefault()
    if (!nomAffiche.trim() || !email.trim() || !motDePasse) {
      setErreur('Tous les champs sont obligatoires.')
      return
    }
    if (motDePasse.length < 8) {
      setErreur('Le mot de passe doit contenir au moins 8 caracteres.')
      return
    }
    setErreur('')
    setEnCours(true)
    inscription(email.trim(), motDePasse, nomAffiche.trim())
      .then(function () { navigate('/etudes') })
      .catch(function (err) { setErreur(err instanceof ApiError ? err.message : 'Erreur lors de la creation du compte.') })
      .finally(function () { setEnCours(false) })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-paper px-6">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center font-display text-2xl text-ink">
          EBIOS<span className="text-signature">&middot;</span>RM
        </div>

        <form onSubmit={soumettre} className="border border-paper-line p-6">
          <h1 className="mb-5 font-mono text-[11px] tracking-wide text-steel-light">CREER UN COMPTE</h1>

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">NOM AFFICHE</label>
          <input
            type="text"
            value={nomAffiche}
            onChange={function (e) { setNomAffiche(e.target.value) }}
            className="mb-4 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
            autoFocus
          />

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">EMAIL</label>
          <input
            type="email"
            value={email}
            onChange={function (e) { setEmail(e.target.value) }}
            className="mb-4 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          />

          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">MOT DE PASSE</label>
          <input
            type="password"
            value={motDePasse}
            onChange={function (e) { setMotDePasse(e.target.value) }}
            className="mb-1 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          />
          <p className="mb-4 text-[10px] text-steel-light">8 caracteres minimum.</p>

          {erreur && <p className="mb-4 text-xs text-risk-critical">{erreur}</p>}

          <button
            type="submit"
            disabled={enCours}
            className="w-full rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
          >
            {enCours ? 'Creation...' : 'Creer le compte'}
          </button>

          <p className="mt-5 text-center text-xs text-steel">
            Deja un compte ? <Link to="/connexion" className="text-signature hover:underline">Se connecter</Link>
          </p>
        </form>
      </div>
    </div>
  )
}
