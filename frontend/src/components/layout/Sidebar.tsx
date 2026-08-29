import { useEffect, useState } from 'react'
import { NavLink, useNavigate, useParams } from 'react-router-dom'
import { LayoutDashboard, FolderOpen, FileText, Settings, LogOut, X, Library, LineChart, BookOpen } from 'lucide-react'
import { AtelierChainCompact } from '../methodology/AtelierChain'
import type { AtelierNode } from '../methodology/AtelierChain'
import { effacerToken, getEtude, obtenirUtilisateurCourant } from '../../lib/api'
import type { Etude, Utilisateur } from '../../lib/api'
import { useT, traduire } from '../../lib/i18n'

function NavItem(props: { to: string; icon: typeof LayoutDashboard; children: React.ReactNode; end?: boolean; onNavigate?: () => void }) {
  var Icon = props.icon
  return (
    <NavLink
      to={props.to}
      end={props.end !== undefined ? props.end : props.to === '/etudes'}
      onClick={props.onNavigate}
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

function statutDe(s: string | undefined): 'done' | 'current' | 'todo' {
  if (s === 'Validee') return 'done'
  if (s === 'EnCours') return 'current'
  return 'todo'
}
function progressionDe(s: 'done' | 'current' | 'todo'): number {
  return s === 'done' ? 100 : s === 'current' ? 50 : 0
}

function ateliersDepuisEtude(etude: Etude | null): AtelierNode[] {
  var statutAtelier1 = statutDe(etude?.statut)
  var statutAtelier2 = statutDe(etude?.statutAtelier2)
  var statutAtelier3 = statutDe(etude?.statutAtelier3)
  var statutAtelier4 = statutDe(etude?.statutAtelier4)
  var statutAtelier5 = statutDe(etude?.statutAtelier5)
  var statuts = [statutAtelier1, statutAtelier2, statutAtelier3, statutAtelier4, statutAtelier5]
  return [1, 2, 3, 4, 5].map(function (n, i) {
    return { numero: n, nom: traduire('atelier.' + n + '.nom'), statut: statuts[i], progression: progressionDe(statuts[i]) }
  })
}

export default function Sidebar(props: { ouvert: boolean; onFermer: () => void }) {
  var t = useT()
  var navigate = useNavigate()
  var params = useParams()
  var etudeId = params.etudeId
  var [etude, setEtude] = useState<Etude | null>(null)
  var [utilisateur, setUtilisateur] = useState<Utilisateur | null>(null)

  useEffect(function () {
    if (!etudeId) {
      setEtude(null)
      return
    }
    getEtude(etudeId).then(setEtude).catch(function () { setEtude(null) })
  }, [etudeId])

  useEffect(function () {
    obtenirUtilisateurCourant().then(setUtilisateur).catch(function () { setUtilisateur(null) })
  }, [])

  function seDeconnecter() {
    effacerToken()
    navigate('/connexion')
  }

  return (
    <>
      {props.ouvert && (
        <div
          className="fixed inset-0 z-40 bg-ink/50 lg:hidden"
          onClick={props.onFermer}
          aria-hidden="true"
        />
      )}

      <aside
        className={
          'fixed inset-y-0 left-0 z-50 flex w-[264px] shrink-0 flex-col bg-ink text-white transition-transform duration-200 ease-out lg:static lg:z-auto lg:flex lg:translate-x-0 ' +
          (props.ouvert ? 'translate-x-0' : '-translate-x-full')
        }
      >
        <div className="flex h-16 items-center justify-between gap-2.5 border-b border-ink-line px-5">
          <div className="font-display text-[19px] leading-none text-white">
            EBIOS<span className="text-signature">&middot;</span>RM
          </div>
          <button
            onClick={props.onFermer}
            aria-label="Fermer le menu"
            className="text-steel-light hover:text-white lg:hidden"
          >
            <X size={20} strokeWidth={1.75} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-5 py-6">
          <nav className="space-y-0.5">
            <NavItem to={etudeId ? '/etudes/' + etudeId : '/etudes'} icon={LayoutDashboard} end onNavigate={props.onFermer}>{t('nav.tableau')}</NavItem>
            <NavItem to="/etudes" icon={FolderOpen} onNavigate={props.onFermer}>{t('nav.etudes')}</NavItem>
            <NavItem to="/portefeuille" icon={LineChart} onNavigate={props.onFermer}>{t('nav.portefeuille')}</NavItem>
          </nav>

          {etudeId && (
            <>
              <div className="mb-4 mt-9 font-mono text-[10px] tracking-wide text-steel-light">
                {t('nav.etudeEnCours')}
              </div>
              <div className="mb-5 truncate font-display text-sm text-white">
                {etude ? etude.nom : t('commun.chargement')}
              </div>

              <AtelierChainCompact ateliers={ateliersDepuisEtude(etude)} etudeId={etudeId} />
            </>
          )}

          <div className="my-7 border-t border-ink-line" />

          <nav className="space-y-0.5">
            <NavItem to="/bibliotheque" icon={Library} onNavigate={props.onFermer}>{t('nav.bibliotheque')}</NavItem>
            <NavItem to="/rapports" icon={FileText} onNavigate={props.onFermer}>{t('nav.rapports')}</NavItem>
            <NavItem to="/aide" icon={BookOpen} onNavigate={props.onFermer}>{t('nav.aide')}</NavItem>
            <NavItem to="/parametres" icon={Settings} onNavigate={props.onFermer}>{t('nav.parametres')}</NavItem>
          </nav>
        </div>

        <div className="border-t border-ink-line px-5 py-4">
          <div className="flex items-center justify-between gap-2">
            <span className="truncate text-xs text-steel-light">{utilisateur ? utilisateur.nomAffiche : ''}</span>
            <button
              onClick={seDeconnecter}
              aria-label={t('nav.deconnexion')}
              className="flex shrink-0 items-center gap-1.5 text-[11px] font-medium text-steel-light hover:text-white"
            >
              <LogOut size={14} strokeWidth={1.75} />
              {t('nav.deconnexion')}
            </button>
          </div>
          <NavLink to="/conditions" onClick={props.onFermer} className="mt-2 block font-mono text-[9px] tracking-wide text-steel-faint hover:text-steel-light">
            {t('legal.conditions')}
          </NavLink>
        </div>
      </aside>
    </>
  )
}
