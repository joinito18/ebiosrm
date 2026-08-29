import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2, Download, Copy, Upload } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import Button from '../components/shared/Button'
import Card from '../components/shared/Card'
import EmptyState from '../components/shared/EmptyState'
import BadgeStatutAtelier from '../components/shared/BadgeStatutAtelier'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import { listEtudes, createEtude, supprimerEtude, dupliquerEtude, importerEtude, ApiError } from '../lib/api'
import { toastSucces, toastErreur } from '../lib/toast'
import type { Etude } from '../lib/api'
import { useT, langueCourante } from '../lib/i18n'

export default function Etudes() {
  var navigate = useNavigate()
  var _t = useT()
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
        var message = err instanceof ApiError ? err.message : _t('etudes.echecListe')
        setErreurListe(message)
      })
      .finally(function () { setChargement(false) })
  }

  useEffect(function () { charger() }, [])

  function handleCreer() {
    if (!nomNouvelle.trim() || !perimetreNouvelle.trim() || !missionNouvelle.trim()) {
      setErreurCreation(_t('etudes.champsRequis'))
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
        var message = err instanceof ApiError ? err.message : _t('etudes.echecCreation')
        setErreurCreation(message)
      })
      .finally(function () { setCreationEnCours(false) })
  }

  var [duplicationEnCours, setDuplicationEnCours] = useState('')
  var [importEnCours, setImportEnCours] = useState(false)
  var champFichier = useRef<HTMLInputElement>(null)

  function handleFichierChoisi(e: React.ChangeEvent<HTMLInputElement>) {
    var fichier = e.target.files && e.target.files[0]
    e.target.value = '' // permet de re-choisir le meme fichier
    if (!fichier) return
    setImportEnCours(true)
    fichier.text()
      .then(function (contenu) { return importerEtude(contenu) })
      .then(function (res) {
        toastSucces(_t('etudes.importee'))
        navigate('/etudes/' + res.id)
      })
      .catch(function (err) {
        toastErreur(err instanceof ApiError ? err.message : _t('etudes.echecImport'))
      })
      .finally(function () { setImportEnCours(false) })
  }

  function handleDupliquer(e: React.MouseEvent, etude: Etude) {
    e.stopPropagation()
    var nom = window.prompt(_t('etudes.nomCopie'), etude.nom + ' (' + _t('etudes.copie') + ')')
    if (nom === null) return
    setDuplicationEnCours(etude.id)
    dupliquerEtude(etude.id, nom.trim() || undefined)
      .then(function (res) {
        toastSucces(_t('etudes.dupliquee'))
        navigate('/etudes/' + res.id)
      })
      .catch(function (err) {
        toastErreur(err instanceof ApiError ? err.message : _t('etudes.echecDup'))
      })
      .finally(function () { setDuplicationEnCours('') })
  }

  function handleSupprimer(e: React.MouseEvent, etude: Etude) {
    e.stopPropagation()
    if (!window.confirm(_t('etudes.confirmSuppr'))) return
    supprimerEtude(etude.id)
      .then(function () {
        toastSucces(_t('etudes.supprimee'))
        charger()
      })
      .catch(function (err) {
        var message = err instanceof ApiError ? err.message : _t('etudes.echecSuppr')
        setErreurListe(message)
      })
  }

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow={_t('etudes.eyebrow')}
        titre={_t('etudes.titre')}
        description={_t('etudes.desc')}
      />

      <div className="mb-6 flex items-center justify-between gap-4">
        <div />
        <div className="flex items-center gap-3">
          <input
            ref={champFichier}
            type="file"
            accept="application/json,.json"
            onChange={handleFichierChoisi}
            className="hidden"
          />
          <Button
            variante="secondary"
            taille="md"
            disabled={importEnCours}
            onClick={function () { if (champFichier.current) champFichier.current.click() }}
          >
            <Upload size={14} />
            {importEnCours ? _t('commun.chargement') : _t('etudes.importer')}
          </Button>
          <Button variante="primary" taille="md" onClick={function () { setCreationOuverte(!creationOuverte) }}>
            <Plus size={14} />
            {_t('etudes.nouvelle')}
          </Button>
        </div>
      </div>

      {creationOuverte && (
        <Card variant="elevated" className="mb-8 p-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">{_t('etudes.form.nom').toUpperCase()}</label>
              <input
                type="text"
                value={nomNouvelle}
                onChange={function (e) { setNomNouvelle(e.target.value) }}
                className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">{_t('etudes.form.mission').toUpperCase()}</label>
              <input
                type="text"
                value={missionNouvelle}
                onChange={function (e) { setMissionNouvelle(e.target.value) }}
                className="w-full border-b border-paper-line bg-transparent py-1.5 text-sm text-ink focus:border-signature focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block font-mono text-[10px] tracking-wide text-steel-light">{_t('etudes.form.perimetreCourt').toUpperCase()}</label>
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

          <Button variante="primary" taille="md" disabled={creationEnCours} onClick={handleCreer} className="mt-4">
            {creationEnCours ? _t('commun.chargement') : _t('etudes.creer')}
          </Button>
        </Card>
      )}

      {chargement && <p className="text-sm text-steel">{_t('commun.chargement')}</p>}

      {!chargement && erreurListe && (
        <div className="border border-risk-critical/30 bg-risk-critical/5 px-5 py-4 text-sm text-risk-critical">
          {erreurListe}
        </div>
      )}

      {!chargement && !erreurListe && etudes.length === 0 && (
        <EmptyState message={_t('etudes.aucuneLongue')} />
      )}

      {!chargement && !erreurListe && etudes.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[480px] border-collapse">
            <thead>
              <tr className="border-b border-paper-line text-left">
                <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('portefeuille.col.etude').toUpperCase()}</th>
                <th className="hidden pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light sm:table-cell">{_t('etudes.col.perimetre').toUpperCase()}</th>
                <th className="pb-2 font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('etudes.col.statut').toUpperCase()}</th>
                <th className="pb-2 text-right font-mono text-[9px] font-normal tracking-wide text-steel-light">{_t('etudes.creeLe').toUpperCase()}</th>
                <th className="pb-2"><span className="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              {etudes.map(function (etude) {
                return (
                  <tr
                    key={etude.id}
                    onClick={function () { navigate('/etudes/' + etude.id) }}
                    className="cursor-pointer border-b border-paper-line transition duration-200 ease-premium hover:bg-paper-dim/50"
                  >
                    <td className="py-3.5 text-sm font-medium text-ink">{etude.nom}</td>
                    <td className="hidden py-3.5 text-xs text-steel sm:table-cell">{etude.perimetre}</td>
                    <td className="py-3.5"><BadgeStatutAtelier statut={etude.statut} /></td>
                    <td className="py-3.5 text-right font-mono text-[11px] text-steel-light">
                      {new Date(etude.creeLeUtc).toLocaleDateString(langueCourante() === 'en' ? 'en-GB' : 'fr-FR')}
                    </td>
                    <td className="py-3.5 pl-3 text-right">
                      <div className="flex items-center justify-end gap-3" onClick={function (e) { e.stopPropagation() }}>
                        <BoutonTelechargerRapport
                          path={'/etudes/' + etude.id + '/export'}
                          nomFichier={'etude-' + etude.nom.replace(/[^a-z0-9]+/gi, '-').toLowerCase() + '.json'}
                          className="text-steel-light transition hover:text-signature"
                        >
                          <span aria-label={'Exporter ' + etude.nom} title={_t('etudes.exporter')}>
                            <Download size={14} />
                          </span>
                        </BoutonTelechargerRapport>
                        <button
                          onClick={function (e) { handleDupliquer(e, etude) }}
                          disabled={duplicationEnCours === etude.id}
                          aria-label={'Dupliquer ' + etude.nom}
                          title={_t('etudes.dupliquer')}
                          className="text-steel-light transition hover:text-signature disabled:opacity-40"
                        >
                          <Copy size={14} />
                        </button>
                        {etude.monRole === 'Proprietaire' && (
                          <button
                            onClick={function (e) { handleSupprimer(e, etude) }}
                            aria-label={'Supprimer ' + etude.nom}
                            className="text-steel-light transition hover:text-risk-critical"
                          >
                            <Trash2 size={14} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
