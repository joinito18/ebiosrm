import { useState } from 'react'
import { Plus, X } from 'lucide-react'

export default function InlineForm(props: { label: string; children: (fermer: () => void) => React.ReactNode }) {
  var [ouvert, setOuvert] = useState(false)

  if (!ouvert) {
    return (
      <button
        onClick={function () { setOuvert(true) }}
        className="mt-3 flex items-center gap-1.5 font-mono text-[11px] font-medium text-signature hover:underline"
      >
        <Plus size={12} />
        {props.label}
      </button>
    )
  }

  return (
    <div className="mt-3 border border-paper-line p-4">
      <div className="mb-3 flex items-center justify-between">
        <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.label.toUpperCase()}</span>
        <button onClick={function () { setOuvert(false) }} className="text-steel-light hover:text-ink">
          <X size={14} />
        </button>
      </div>
      {props.children(function () { setOuvert(false) })}
    </div>
  )
}
