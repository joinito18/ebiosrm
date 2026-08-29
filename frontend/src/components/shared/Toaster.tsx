import { useEffect, useState } from 'react'
import { sabonner, retirerToast } from '../../lib/toast'
import type { Toast } from '../../lib/toast'
import { traduire } from '../../lib/i18n'

var STYLE_BARRE: Record<Toast['type'], string> = {
  succes: 'border-l-signature',
  erreur: 'border-l-risk-critical',
  info: 'border-l-steel',
}

export default function Toaster() {
  var [toasts, setToasts] = useState<Toast[]>([])

  useEffect(function () { return sabonner(setToasts) }, [])

  if (toasts.length === 0) return null

  return (
    <div className="pointer-events-none fixed bottom-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2">
      {toasts.map(function (t) {
        return (
          <div
            key={t.id}
            role="status"
            className={'pointer-events-auto flex items-start gap-3 rounded-md border border-paper-line border-l-2 bg-white px-4 py-3 text-sm text-ink shadow-md ' + STYLE_BARRE[t.type]}
          >
            <span className="flex-1 leading-snug">{t.message}</span>
            <button
              onClick={function () { retirerToast(t.id) }}
              aria-label={traduire('commun.fermer')}
              className="-mr-1 -mt-0.5 shrink-0 rounded px-1 text-steel-light transition hover:text-ink"
            >
              &times;
            </button>
          </div>
        )
      })}
    </div>
  )
}
