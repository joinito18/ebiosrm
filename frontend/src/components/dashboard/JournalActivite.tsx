interface Evenement {
  heure: string
  titre: string
  detail: string
}

var EVENEMENTS: Evenement[] = [
  { heure: '14:32', titre: 'Atelier 1 valide', detail: 'Societe de biotechnologie - snapshot v1 cree' },
  { heure: '09:15', titre: 'Scenario strategique ajoute', detail: 'Plateforme e-commerce' },
  { heure: 'Hier', titre: 'Risque critique identifie', detail: 'Infrastructure Cloud' },
  { heure: 'Hier', titre: 'Referentiel PSSI marque conforme', detail: 'Societe de biotechnologie' },
]

export default function JournalActivite() {
  return (
    <div className="relative pl-1">
      <div className="absolute bottom-2 left-[3px] top-2 w-px bg-paper-line" />
      <div className="space-y-5">
        {EVENEMENTS.map(function (evt, i) {
          return (
            <div key={i} className="relative flex gap-4 pl-4">
              <div className="absolute left-0 top-1 h-1.5 w-1.5 rounded-full bg-steel" />
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline justify-between gap-2">
                  <span className="truncate text-xs font-medium text-ink">{evt.titre}</span>
                  <span className="shrink-0 font-mono text-[10px] text-steel-light">{evt.heure}</span>
                </div>
                <div className="mt-0.5 text-[11px] text-steel">{evt.detail}</div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
