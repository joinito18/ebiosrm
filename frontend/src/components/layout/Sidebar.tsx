import { useEffect, useState } from 'react'
import { NavLink, useParams } from 'react-router-dom'
import { LayoutDashboard, FolderOpen, FileText, Settings } from 'lucide-react'
import { AtelierChainCompact } from '../methodology/AtelierChain'
import type { AtelierNode } from '../methodology/AtelierChain'
import { getEtude } from '../../lib/api'
import type { Etude } from '../../lib/api'

function NavItem(props: { to: string; icon: typeof LayoutDashboard; children: React.ReactNode }) {
  var Icon = props.icon
  return (
    <NavLink
      to={props.to}
      end={props.to === '/etudes'}
      className={function (state) {
        var base = 'flex items-center gap-3 rounded-md px-2.5 py-2 text-xs font-medium transition-colors '
        return base + (state.isActive
          ? 'bg-signature/20 text-white'
          : 'text-steel-light hover:bg-white/[.04] hover:text-white')
      }}
    >
      <Icon size={16} strokeWidth={1.75} />
      {props.children}
    </NavLink>
  )
}

function ateliersDepuisEtude(etude: Etude | null): AtelierNode[] {
  var statutAtelier1: 'done' | 'current' | 'todo' = 'todo'
  var statutAtelier2: 'done' | 'current' | 'todo' = 'todo'
  var statutAtelier3: 'done' | 'current' | 'todo' = 'todo'
  var statutAtelier4: 'done' | 'current' | 'todo' = 'todo'
  if (etude) {
    if (etude.statut === 'Validee') statutAtelier1 = 'done'
    else if (etude.statut === 'EnCours') statutAtelier1 = 'current'

    if (etude.statutAtelier2 === 'Validee') statutAtelier2 = 'done'
    else if (etude.statutAtelier2 === 'EnCours') statutAtelier2 = 'current'

    if (etude.statutAtelier3 === 'Validee') statutAtelier3 = 'done'
    else if (etude.statutAtelier3 === 'EnCours') statutAtelier3 = 'current'

    if (etude.statutAtelier4 === 'Validee') statutAtelier4 = 'done'
    else if (etude.statutAtelier4 === 'EnCours') statutAtelier4 = 'current'
  }
  return [
    { numero: 1, nom: 'Cadrage', statut: statutAtelier1, progression: statutAtelier1 === 'done' ? 100 : statutAtelier1 === 'current' ? 50 : 0 },
    { numero: 2, nom: 'Sources de risque', statut: statutAtelier2, progression: statutAtelier2 === 'done' ? 100 : statutAtelier2 === 'current' ? 50 : 0 },
    { numero: 3, nom: 'Scenarios strategiques', statut: statutAtelier3, progression: statutAtelier3 === 'done' ? 100 : statutAtelier3 === 'current' ? 50 : 0 },
    { numero: 4, nom: 'Scenarios operationnels', statut: statutAtelier4, progression: statutAtelier4 === 'done' ? 100 : statutAtelier4 === 'current' ? 50 : 0 },
    { numero: 5, nom: 'Traitement du risque', statut: 'todo', progression: 0 },
  ]
}

export default function Sidebar() {
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

  return (
    <aside className="hidden w-[264px] shrink-0 flex-col bg-ink text-white lg:flex">
      <div className="flex h-16 items-center gap-2.5 border-b border-ink-line px-5">
        <div className="font-display text-[19px] leading-none text-white">
          EBIOS<span className="text-signature">&middot;</span>RM
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-5 py-6">
        <nav className="space-y-0.5">
          <NavItem to="/etudes" icon={LayoutDashboard}>Tableau de bord</NavItem>
          <NavItem to="/etudes" icon={FolderOpen}>Etudes</NavItem>
        </nav>

        {etudeId && (
          <>
            <div className="mb-4 mt-9 font-mono text-[10px] tracking-wide text-steel-light">
              ETUDE EN COURS
            </div>
            <div className="mb-5 truncate font-display text-sm text-white">
              {etude ? etude.nom : 'Chargement...'}
            </div>

            <AtelierChainCompact ateliers={ateliersDepuisEtude(etude)} etudeId={etudeId} />
          </>
        )}

        <div className="my-7 border-t border-ink-line" />

        <nav className="space-y-0.5">
          <NavItem to="/rapports" icon={FileText}>Rapports</NavItem>
          <NavItem to="/parametres" icon={Settings}>Parametres</NavItem>
        </nav>
      </div>

      <div className="border-t border-ink-line px-5 py-4">
        <div className="flex items-center gap-2.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-full bg-white/10 font-mono text-[10px] text-white">
            AR
          </div>
          <div className="min-w-0">
            <div className="truncate text-xs font-medium text-white">Analyste de risques</div>
            <div className="truncate font-mono text-[10px] text-steel-light">CENADI</div>
          </div>
        </div>
      </div>
    </aside>
  )
}
