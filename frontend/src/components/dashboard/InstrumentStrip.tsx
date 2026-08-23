interface Metrique {
  valeur: string
  label: string
  detail?: string
}

var METRIQUES: Metrique[] = [
  { valeur: '12', label: 'ETUDES TOTALES', detail: '+2 ce mois' },
  { valeur: '7', label: 'EN COURS', detail: '58% du total' },
  { valeur: '4', label: 'RISQUES CRITIQUES', detail: 'A traiter' },
  { valeur: '76%', label: 'CONFORMITE', detail: 'Niveau satisfaisant' },
]

export default function InstrumentStrip() {
  return (
    <div className="flex divide-x divide-paper-line border-y border-paper-line">
      {METRIQUES.map(function (m) {
        return (
          <div key={m.label} className="flex-1 px-6 py-5 first:pl-0 last:pr-0">
            <div className="font-display text-[28px] leading-none text-ink">{m.valeur}</div>
            <div className="mt-2 font-mono text-[9px] tracking-wide text-steel-light">{m.label}</div>
            {m.detail && <div className="mt-0.5 text-[11px] text-steel">{m.detail}</div>}
          </div>
        )
      })}
    </div>
  )
}
