import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Menu } from 'lucide-react'
import Badge from '../shared/Badge'
import { COULEUR_STATUT_ATELIER } from '../shared/BadgeStatutAtelier'
import { getEtude } from '../../lib/api'
import type { Etude } from '../../lib/api'
import { useT } from '../../lib/i18n'
import { libelle } from '../../lib/libelles'

export default function Header(props: { onOuvrirMenu: () => void }) {
  var params = useParams()
  var _t = useT()
  var etudeId = params.etudeId
  var [etude, setEtude] = useState<Etude | null>(null)

  useEffect(function () {
    if (!etudeId) {
      setEtude(null)
      return
    }
    getEtude(etudeId).then(setEtude).catch(function () { setEtude(null) })
  }, [etudeId])

  var libelleStatut = etude ? libelle('statutAtelier', etude.statut).toUpperCase() : ''

  return (
    <header className="flex h-16 shrink-0 items-center justify-between gap-3 border-b border-paper-line bg-paper px-4 sm:px-6 lg:px-10">
      <div className="flex min-w-0 items-center gap-3">
        <button
          onClick={props.onOuvrirMenu}
          aria-label={_t('nav.etudes')}
          className="text-steel hover:text-ink lg:hidden"
        >
          <Menu size={20} strokeWidth={1.75} />
        </button>
        <div className="flex min-w-0 items-center gap-2 font-mono text-[11px] text-steel">
          <span className="shrink-0">{_t('nav.etudes')}</span>
          {etude && (
            <>
              <span className="shrink-0 text-steel-faint">/</span>
              <span className="truncate text-ink">{etude.nom}</span>
            </>
          )}
        </div>
      </div>

      <div className="flex items-center gap-4">
        {etude && (
          <Badge couleur={COULEUR_STATUT_ATELIER[etude.statut] || 'steel'}>
            {libelleStatut}
          </Badge>
        )}
      </div>
    </header>
  )
}
