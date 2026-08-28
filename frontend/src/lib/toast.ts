// Notifications ephemeres, utilisables depuis n'importe ou (y compris hors
// composant React). Un unique <Toaster /> monte dans App les affiche.

export type TypeToast = 'succes' | 'erreur' | 'info'

export interface Toast {
  id: number
  type: TypeToast
  message: string
}

type Abonne = (toasts: Toast[]) => void

let toasts: Toast[] = []
const abonnes = new Set<Abonne>()
let compteur = 0

function notifier() {
  for (const a of abonnes) a(toasts)
}

export function sabonner(fn: Abonne): () => void {
  abonnes.add(fn)
  fn(toasts)
  return function () { abonnes.delete(fn) }
}

export function retirerToast(id: number) {
  toasts = toasts.filter(function (t) { return t.id !== id })
  notifier()
}

export function toast(message: string, type: TypeToast = 'info', dureeMs = 5000) {
  const id = ++compteur
  toasts = [...toasts, { id, type, message }]
  notifier()
  if (dureeMs > 0) {
    setTimeout(function () { retirerToast(id) }, dureeMs)
  }
}

export const toastSucces = (m: string) => toast(m, 'succes')
export const toastErreur = (m: string) => toast(m, 'erreur', 8000)
