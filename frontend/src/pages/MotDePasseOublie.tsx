import { useState } from 'react'
import { Link } from 'react-router-dom'
import { demanderReinitialisationMotDePasse } from '../lib/api'
import LayoutAuth from '../components/auth/LayoutAuth'

export default function MotDePasseOublie() {
  var [email, setEmail] = useState('')
  var [envoye, setEnvoye] = useState(false)
  var [enCours, setEnCours] = useState(false)

  function soumettre(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim()) return
    setEnCours(true)
    // Reponse volontairement identique cote serveur que le compte existe ou non :
    // on affiche donc toujours le meme message de confirmation, meme en cas d'erreur.
    demanderReinitialisationMotDePasse(email.trim())
      .catch(function () { /* message identique quoi qu'il arrive */ })
      .finally(function () {
        setEnvoye(true)
        setEnCours(false)
      })
  }

  return (
    <LayoutAuth>
      <div className="mb-8">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">MOT DE PASSE OUBLIE</div>
        <h1 className="font-display text-3xl text-ink">Reinitialiser</h1>
      </div>

      {envoye ? (
        <div className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <p className="text-sm leading-relaxed text-ink">
            Si un compte est associe a <span className="font-medium">{email.trim()}</span>, un email
            contenant un lien de reinitialisation vient d'etre envoye. Ce lien est valable 1 heure.
          </p>
          <p className="mt-3 text-xs text-steel-light">
            Pensez a verifier vos courriers indesirables.
          </p>
        </div>
      ) : (
        <form onSubmit={soumettre} className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">EMAIL</label>
          <input
            type="email"
            value={email}
            onChange={function (e) { setEmail(e.target.value) }}
            className="mb-5 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
            autoFocus
          />

          <button
            type="submit"
            disabled={enCours}
            className="w-full rounded-sm bg-signature px-4 py-2.5 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
          >
            {enCours ? 'Envoi...' : 'Envoyer le lien'}
          </button>
        </form>
      )}

      <p className="mt-6 text-center text-xs text-steel">
        <Link to="/connexion" className="font-medium text-signature hover:underline">Retour a la connexion</Link>
      </p>
    </LayoutAuth>
  )
}
