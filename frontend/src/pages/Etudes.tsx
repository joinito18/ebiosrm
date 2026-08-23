import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import { listEtudes, createEtude, ApiError } from '../lib/api'
import type { Etude } from '../lib/api'

export default function Etudes() {
  var navigate = useNavigate()
  var [etudes, setEtudes] = useState<Etude[]>([])
  var [chargement, setChargement] = useState(true)
  var [erreurListe, setErreurListe] = useState('')
  var [nomNouvelle, setNomNouvelle] = useState('')
  var [perimetreNouvelle, setPerimetreNouvelle] = useState('')
  var [missionNouvelle, setMissionNouvelle] = useState('')
  var [creationOuverte, setCreationOuverte] = useState(false)
  var [erreurCreation, setErreurCreation] = useState('')
  var [creationEnCours, setCreationEnCours] = useState(false)

  function charger() {
    setChargement(true)
    listEtudes()
      .then(function (data) {
        setEtudes(data)
        setErreurListe('')
      })
      .catch(function (err) {
        var message = err instanceof ApiError ? err.message : 'Impossible de contacter l API. Verifiez que le backend tourne sur localhost:5197.'
        setErreurListe(message)
      })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () { charger() }, [])

  function handleCreer() {
    if (!nomNouvelle.trim() || !perimetreNouvelle.trim() || !missionNouvelle.trim()) {
      setErreurCreation('Le nom, la mission et le perimetre sont obligatoires.')
      return
    }
    setErreurCreation('')
    setCreationEnCours(true)
    createEtude(nomNouvelle, perimetreNouvelle, missionNouvelle)
      .then(function (etude) {
        setNomNouvelle('')
        setPerimetreNouvelle('')
        setMissionNouvelle('')
        setCreationOuverte(false)
        navigate('/etudes/' + etude.id)
      })
      .catch(function (err) {
        var message = err instanceof ApiError ? err.message : 'Impossible de creer l etude. Verifiez que le backend tourne sur localhost:5197.'
        setErreurCreation(message)
      })
      .finally(function () { setCreationEnCours(false) })
  }

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="REGISTRE DES ETUDES"
        titre="Etudes"
        description="Ensemble des analyses de risques EBIOS RM conduites ou en cours."
      />

      <div className="mb-6 flex items-center justify-between gap-4">
        <div />
        <button
          onClick={function () { setCreationOuverte(!creationOuverte) }}
          className="flex items-center gap-2 rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90"
        >
          <Plus size={14} />
          Nouvelle etude
        </button>
      </div>

      {creationOuverte && (
        <div className="mb-8 border border-paper-line p-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">NOM</label>
              <input
                type="text"
                value={nomNouvelle}
                onChange={function (e) { setNomNouvelle(e.target.value) }}
                className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">MISSION</label>
              <input
                type="text"
                value={missionNouvelle}
                onChange={function (e) { setMissionNouvelle(e.target.value) }}
                className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">PERIMETRE</label>
              <input
                type="text"
                value={perimetreNouvelle}
                onChange={function (e) { setPerimetreNouvelle(e.target.value) }}
                className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
              />
            </div>
          </div>

          {erreurCreation && (
            <div className="mt-4 border border-risk-critical/30 bg-risk-critical/5 px-4 py-2.5 text-xs text-risk-critical">
              {erreurCreation}
            </div>
          )}

          <button
            onClick={handleCreer}
            disabled={creationEnCours}
            className="mt-4 rounded-sm bg-signature px-4 py-2 text-xs font-medium text-white transition hover:bg-signature/90 disabled:opacity-50"
          >
            {creationEnCours ? 'Creation...' : 'Creer l etude'}
          </button>
        </div>
      )}

      {chargement && <p className="text-sm text-steel">Chargement...</p>}

      {!chargement && erreurListe && (
        <div className="border border-risk-critical/30 bg-risk-critical/5 px-5 py-4 text-sm text-risk-critical">
          {erreurListe}
        </div>
      )}

      {!chargement && !erreurListe && etudes.length === 0 && (
        <p className="text-sm text-steel">Aucune etude pour le moment.</p>
      )}

      {!chargement && !erreurListe && etudes.length > 0 && (
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-paper-line text-left">
              <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">ETUDE</th>
              <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">PERIMETRE</th>
              <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">STATUT</th>
              <th className="pb-2 text-right font-mono text-[9px] font-normal tracking-wide text-steel-light">CREEE LE</th>
            </tr>
          </thead>
          <tbody>
            {etudes.map(function (etude) {
              return (
                <tr
                  key={etude.id}
                  onClick={function () { navigate('/etudes/' + etude.id) }}
                  className="cursor-pointer border-b border-paper-line transition hover:bg-paper-dim/50"
                >
                  <td className="py-3.5 text-sm font-medium text-ink">{etude.nom}</td>
                  <td className="py-3.5 text-xs text-steel">{etude.perimetre}</td>
                  <td className="py-3.5 text-xs text-steel">{etude.statut}</td>
                  <td className="py-3.5 text-right font-mono text-[11px] text-steel-light">
                    {new Date(etude.creeLeUtc).toLocaleDateString('fr-FR')}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </div>
  )
}
