import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getEtude, listerMembres, ajouterMembre, changerRoleMembre, retirerMembre, ApiError } from '../lib/api'
import type { Etude, MembreEtude, RoleEtude } from '../lib/api'
import { toastSucces, toastErreur } from '../lib/toast'

var ROLES: { valeur: RoleEtude; libelle: string; description: string }[] = [
  { valeur: 'Proprietaire', libelle: 'Proprietaire', description: 'Tout, plus la gestion des membres et la suppression de l etude' },
  { valeur: 'Editeur', libelle: 'Editeur', description: 'Modifie le contenu des ateliers, valide et rouvre les ateliers' },
  { valeur: 'Lecteur', libelle: 'Lecteur', description: 'Consultation et telechargement des rapports' },
]

export default function MembresEtude() {
  var params = useParams()
  var etudeId = params.etudeId as string
  var [etude, setEtude] = useState<Etude | null>(null)
  var [membres, setMembres] = useState<MembreEtude[]>([])
  var [chargement, setChargement] = useState(true)
  var [email, setEmail] = useState('')
  var [role, setRole] = useState<RoleEtude>('Editeur')
  var [enCours, setEnCours] = useState(false)

  function charger() {
    return Promise.all([getEtude(etudeId), listerMembres(etudeId)]).then(function (r) {
      setEtude(r[0])
      setMembres(r[1] || [])
    })
  }

  useEffect(function () {
    setChargement(true)
    charger().finally(function () { setChargement(false) })
  }, [etudeId])

  var jeSuisProprietaire = etude?.monRole === 'Proprietaire'

  function inviter(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim()) return
    setEnCours(true)
    ajouterMembre(etudeId, email.trim(), role)
      .then(function () {
        toastSucces('Membre ajoute.')
        setEmail('')
        return charger()
      })
      .catch(function (err) {
        toastErreur(err instanceof ApiError ? err.message : 'Impossible d ajouter ce membre.')
      })
      .finally(function () { setEnCours(false) })
  }

  function majRole(m: MembreEtude, nouveauRole: RoleEtude) {
    changerRoleMembre(etudeId, m.utilisateurId, nouveauRole)
      .then(function () { toastSucces('Role mis a jour.'); return charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Echec.') })
  }

  function retirer(m: MembreEtude) {
    if (!window.confirm('Retirer ' + m.nomAffiche + ' de l etude ?')) return
    retirerMembre(etudeId, m.utilisateurId)
      .then(function () { toastSucces('Membre retire.'); return charger() })
      .catch(function (err) { toastErreur(err instanceof ApiError ? err.message : 'Echec.') })
  }

  return (
    <div className="mx-auto max-w-[900px] px-6 py-10 lg:px-10 lg:py-14">
      <div className="mb-8 border-b border-paper-line pb-6">
        <Link to={'/etudes/' + etudeId} className="font-mono text-[11px] tracking-wide text-steel hover:text-signature">
          &larr; {etude ? etude.nom : 'Retour a l etude'}
        </Link>
        <h1 className="mt-3 font-display text-3xl text-ink">Membres de l etude</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-steel">
          Partagez cette etude avec votre equipe. Chaque membre a un role.
          {!jeSuisProprietaire && ' Seul un proprietaire peut modifier la liste.'}
        </p>
      </div>

      {chargement ? (
        <p className="text-sm text-steel">Chargement...</p>
      ) : (
        <>
          {jeSuisProprietaire && (
            <form onSubmit={inviter} className="mb-8 rounded-md border border-paper-line bg-white p-5 shadow-sm">
              <div className="mb-1 font-mono text-[10px] tracking-wide text-steel-light">AJOUTER UN MEMBRE</div>
              <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
                <div className="flex-1">
                  <label className="mb-1 block text-[11px] text-steel">Email du compte</label>
                  <input
                    type="email"
                    value={email}
                    onChange={function (e) { setEmail(e.target.value) }}
                    placeholder="collegue@organisation.fr"
                    className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-[11px] text-steel">Role</label>
                  <select value={role} onChange={function (e) { setRole(e.target.value as RoleEtude) }}
                    className="border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none">
                    {ROLES.map(function (r) { return <option key={r.valeur} value={r.valeur}>{r.libelle}</option> })}
                  </select>
                </div>
                <button type="submit" disabled={enCours}
                  className="rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50">
                  {enCours ? 'Ajout...' : 'Ajouter'}
                </button>
              </div>
              <p className="mt-2 text-[10px] text-steel-light">La personne doit deja avoir un compte sur la plateforme.</p>
            </form>
          )}

          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-paper-line text-left">
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">MEMBRE</th>
                  <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">ROLE</th>
                  <th className="pb-2"></th>
                </tr>
              </thead>
              <tbody>
                {membres.map(function (m) {
                  return (
                    <tr key={m.utilisateurId} className="border-b border-paper-line/60">
                      <td className="py-3 pr-4">
                        <div className="text-ink">{m.nomAffiche}{m.estMoi && <span className="ml-2 font-mono text-[10px] text-steel-light">(vous)</span>}</div>
                        <div className="font-mono text-[10px] text-steel-light">{m.email}</div>
                      </td>
                      <td className="py-3 pr-4">
                        {jeSuisProprietaire && !m.estMoi ? (
                          <select value={m.role} onChange={function (e) { majRole(m, e.target.value as RoleEtude) }}
                            className="border-b border-paper-line bg-transparent py-1 text-xs text-ink focus:border-signature focus:outline-none">
                            {ROLES.map(function (r) { return <option key={r.valeur} value={r.valeur}>{r.libelle}</option> })}
                          </select>
                        ) : (
                          <span className="text-steel">{m.role}</span>
                        )}
                      </td>
                      <td className="py-3 text-right">
                        {jeSuisProprietaire && !m.estMoi && (
                          <button onClick={function () { retirer(m) }} className="text-[11px] text-risk-critical hover:underline">Retirer</button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <div className="mt-8 space-y-1 border-t border-paper-line pt-4 text-[11px] text-steel-light">
            {ROLES.map(function (r) { return <div key={r.valeur}><span className="text-steel">{r.libelle}</span> &mdash; {r.description}</div> })}
          </div>
        </>
      )}
    </div>
  )
}
