import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'
import { traduire } from '../../lib/i18n'

interface Props {
  children: ReactNode
}

interface State {
  erreur: Error | null
}

/**
 * Filet de securite : si un composant leve une exception au rendu (donnee
 * inattendue de l'API, bug), on affiche un ecran d'erreur lisible plutot
 * qu'une page blanche. Le rechargement repart d'un etat propre.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { erreur: null }

  static getDerivedStateFromError(erreur: Error): State {
    return { erreur }
  }

  componentDidCatch(erreur: Error, info: ErrorInfo) {
    console.error('Erreur non rattrapee dans l\'interface :', erreur, info.componentStack)
  }

  render() {
    if (!this.state.erreur) return this.props.children

    return (
      <div className="flex min-h-screen items-center justify-center bg-paper px-6">
        <div className="w-full max-w-md rounded-md border border-paper-line bg-white p-7 shadow-sm">
          <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{traduire('cmp.eb.eyebrow')}</div>
          <h1 className="mb-3 font-display text-2xl text-ink">{traduire('cmp.eb.titre')}</h1>
          <p className="mb-5 text-sm leading-relaxed text-steel">
            {traduire('cmp.eb.corps')}
          </p>
          <button
            onClick={function () { window.location.reload() }}
            className="w-full rounded-sm bg-signature px-4 py-2.5 text-xs font-medium text-white transition hover:bg-signature/90"
          >
            {traduire('cmp.eb.recharger')}
          </button>
        </div>
      </div>
    )
  }
}
