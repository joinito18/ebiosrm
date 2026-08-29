import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { connexion, ApiError } from '../lib/api'
import LayoutAuth from '../components/auth/LayoutAuth'
import { useT } from '../lib/i18n'

export default function Connexion() {
  var navigate = useNavigate()
  var _t = useT()
  var [email, setEmail] = useState('')
  var [motDePasse, setMotDePasse] = useState('')
  var [erreur, setErreur] = useState('')
  var [enCours, setEnCours] = useState(false)

  function soumettre(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim() || !motDePasse) {
      setErreur(_t('auth.champsRequis'))
      return
    }
    setErreur('')
    setEnCours(true)
    connexion(email.trim(), motDePasse)
      .then(function () { navigate('/etudes') })
      .catch(function (err) {
        var message = err instanceof ApiError && err.status !== 401 ? err.message : _t('auth.echecConnexion')
        setErreur(message)
      })
      .finally(function () { setEnCours(false) })
  }

  return (
    <LayoutAuth>
      <div className="mb-8">
        <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">EBIOS RISK MANAGER</div>
        <h1 className="font-display text-3xl text-ink">{_t('auth.connexion.titre')}</h1>
      </div>

      <form onSubmit={soumettre} className="rounded-md border border-paper-line bg-white p-7 shadow-sm">
        <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">{_t('auth.email').toUpperCase()}</label>
        <input
          type="email"
          value={email}
          onChange={function (e) { setEmail(e.target.value) }}
          className="mb-5 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
          autoFocus
        />

        <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">{_t('auth.motdepasse').toUpperCase()}</label>
        <input
          type="password"
          value={motDePasse}
          onChange={function (e) { setMotDePasse(e.target.value) }}
          className="mb-5 w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
        />

        {erreur && <p className="mb-4 text-xs text-risk-critical">{erreur}</p>}

        <button
          type="submit"
          disabled={enCours}
          className="w-full rounded-sm bg-signature px-4 py-2.5 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
        >
          {enCours ? _t('auth.connexionEnCours') : _t('auth.seConnecter')}
        </button>
      </form>

      <p className="mt-6 text-center text-xs text-steel">
        {_t('auth.pasDeCompte')} <Link to="/inscription" className="font-medium text-signature hover:underline">{_t('auth.lienInscription')}</Link>
      </p>
    </LayoutAuth>
  )
}
