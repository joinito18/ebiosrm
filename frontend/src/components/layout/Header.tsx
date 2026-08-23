import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getEtude } from '../../lib/api'
import type { Etude } from '../../lib/api'

var LIBELLES_STATUT: { [key: string]: string } = {
  Brouillon: 'BROUILLON',
  EnCours: 'ATELIER 01 EN COURS',
  Validee: 'ATELIER 01 VALIDE',
}

export default function Header() {
  var params = useParams()
  var etudeId = params.etudeId
  var [etude, setEtude] = useState<Etude | null>(null)

  useEffect(function () {
    if (!etudeId) {
      setEtude(null)
      return
    }
    getEtude(etudeId).then(setEtude).catch(function () { setEtude(null) })
  }, [etudeId])

  var libelleStatut = etude ? (LIBELLES_STATUT[etude.statut] || etude.statut) : ''

  return (
    <header className="flex h-16 shrink-0 items-center justify-between border-b border-paper-line bg-paper px-6 lg:px-10">
      <div className="flex items-center gap-2 font-mono text-[11px] text-steel">
        <span>Etudes</span>
        {etude && (
          <>
            <span className="text-steel-faint">/</span>
            <span className="text-ink">{etude.nom}</span>
          </>
        )}
      </div>

      <div className="flex items-center gap-4">
        {etude && (
          <span className="rounded-sm border border-paper-line bg-white px-2 py-1 font-mono text-[10px] tracking-wide text-steel">
            {libelleStatut}
          </span>
        )}
        <div className="flex h-7 w-7 items-center justify-center rounded-full bg-signature font-mono text-[10px] text-white">
          AR
        </div>
      </div>
    </header>
  )
}
