export type AtelierState = 'done' | 'current' | 'todo'

export interface AtelierNode {
  numero: number
  nom: string
  objectif?: string
  statut: AtelierState
  progression?: number
}

function padNumero(n: number): string {
  return n < 10 ? '0' + n : String(n)
}

function NodeMarker(props: { statut: AtelierState }) {
  var statut = props.statut
  if (statut === 'done') {
    return (
      <div className="flex h-5 w-5 items-center justify-center rounded-full bg-risk-low/90 text-white">
        <svg width="10" height="8" viewBox="0 0 10 8" fill="none">
          <path d="M1 4L3.5 6.5L9 1" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
    )
  }
  if (statut === 'current') {
    return (
      <div className="relative flex h-5 w-5 items-center justify-center rounded-full bg-signature">
        <span className="absolute h-8 w-8 animate-ping rounded-full bg-signature/20" />
        <span className="h-1.5 w-1.5 rounded-full bg-white" />
      </div>
    )
  }
  return <div className="h-5 w-5 rounded-full border border-steel-faint bg-transparent" />
}

export function AtelierChainCompact(props: { ateliers: AtelierNode[]; etudeId: string }) {
  var ateliers = props.ateliers
  var etudeId = props.etudeId
  return (
    <div className="relative pl-1">
      <div className="absolute bottom-2 left-[9px] top-2 w-px bg-ink-line" />
      <div className="space-y-4">
        {ateliers.map(function (atelier) {
          var linkTarget = '/etudes/' + etudeId + '/ateliers/' + atelier.numero
          var labelClass = 'truncate text-xs '
          if (atelier.statut === 'current') {
            labelClass = labelClass + 'font-medium text-white'
          } else if (atelier.statut === 'done') {
            labelClass = labelClass + 'text-steel-light'
          } else {
            labelClass = labelClass + 'text-steel'
          }
          return (
            <a key={atelier.numero} href={linkTarget} className="group relative flex scale-75 origin-left items-center gap-3 pl-0">
              <div className="z-10 bg-ink pr-0.5">
                <NodeMarker statut={atelier.statut} />
              </div>
              <div className="flex min-w-0 flex-1 items-baseline gap-2">
                <span className="font-mono text-[10px] text-steel-light">
                  {padNumero(atelier.numero)}
                </span>
                <span className={labelClass}>
                  {atelier.nom}
                </span>
              </div>
            </a>
          )
        })}
      </div>
    </div>
  )
}

export function AtelierChainExpanded(props: { ateliers: AtelierNode[]; etudeId: string }) {
  var ateliers = props.ateliers
  var etudeId = props.etudeId
  return (
    <div className="flex snap-x snap-mandatory items-stretch gap-0 overflow-x-auto rounded-lg border border-paper-line lg:overflow-hidden">
      {ateliers.map(function (atelier, i) {
        var linkTarget = '/etudes/' + etudeId + '/ateliers/' + atelier.numero
        var isLast = i === ateliers.length - 1
        var isCurrent = atelier.statut === 'current'
        var isDone = atelier.statut === 'done'

        var colClass = 'relative flex w-[180px] shrink-0 snap-start flex-col justify-between p-5 transition-all lg:w-auto lg:shrink '
        if (isCurrent) {
          colClass = colClass + 'lg:flex-[2.2] bg-white'
        } else if (isDone) {
          colClass = colClass + 'lg:flex-1 bg-paper'
        } else {
          colClass = colClass + 'lg:flex-[0.85] bg-paper-dim'
        }
        if (!isLast) {
          colClass = colClass + ' border-r border-paper-line'
        }

        var titleClass = 'font-display leading-snug '
        titleClass = titleClass + (isCurrent ? 'text-2xl text-ink' : isDone ? 'text-base text-ink' : 'text-sm text-steel')

        var barClass = 'h-full rounded-full '
        barClass = barClass + (isCurrent ? 'bg-signature' : isDone ? 'bg-risk-low' : 'bg-steel-faint')

        var barWidth = String(atelier.progression || 0) + '%'

        return (
          <div key={atelier.numero} className={colClass}>
            {isCurrent && (
              <div className="absolute inset-x-0 top-0 h-[3px] bg-signature" />
            )}

            <div>
              <div className="mb-2 flex items-center gap-2">
                <NodeMarker statut={atelier.statut} />
                <span className="font-mono text-[10px] tracking-wide text-steel-light">
                  ATELIER {padNumero(atelier.numero)}
                </span>
              </div>

              <div className={titleClass}>{atelier.nom}</div>

              {isCurrent && atelier.objectif && (
                <p className="mt-2 max-w-[220px] text-xs leading-relaxed text-steel">
                  {atelier.objectif}
                </p>
              )}
            </div>

            <div className="mt-4">
              <div className="h-[3px] w-full overflow-hidden rounded-full bg-paper-dim">
                <div className={barClass} style={{ width: barWidth }} />
              </div>

              {(isCurrent || isDone) && (
                <a href={linkTarget} className="mt-3 inline-flex items-center gap-1 font-mono text-[11px] font-medium text-signature hover:underline">
                  {isCurrent ? 'Reprendre l atelier' : 'Consulter'}
                </a>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}
