import { traduire } from '../../lib/i18n'
var GRAVITES = [4, 3, 2, 1]
var VRAISEMBLANCES = [1, 2, 3, 4]

function niveauColor(gravite: number, vraisemblance: number): string {
  var produit = gravite * vraisemblance
  if (produit >= 12) return 'bg-risk-critical'
  if (produit >= 8) return 'bg-risk-high'
  if (produit >= 4) return 'bg-risk-moderate'
  return 'bg-risk-low'
}

export interface ScenarioPourMatrice {
  gravite: number
  vraisemblanceInitiale?: string | null
}

function construireMatrice(scenarios: ScenarioPourMatrice[]): number[][] {
  var matrice = GRAVITES.map(function () { return VRAISEMBLANCES.map(function () { return 0 }) })
  scenarios.forEach(function (s) {
    if (!s.vraisemblanceInitiale) return
    var vraisemblance = Number(s.vraisemblanceInitiale.replace('V', ''))
    var ligne = GRAVITES.indexOf(s.gravite)
    var colonne = VRAISEMBLANCES.indexOf(vraisemblance)
    if (ligne === -1 || colonne === -1) return
    matrice[ligne][colonne] = matrice[ligne][colonne] + 1
  })
  return matrice
}

export default function RiskMatrix(props: { scenarios: ScenarioPourMatrice[] }) {
  var matrice = construireMatrice(props.scenarios)

  return (
    <div>
      <div className="flex min-w-0">
        <div className="flex w-6 shrink-0 flex-col items-center justify-center">
          <span className="rotate-180 font-mono text-[9px] tracking-wide text-steel-light" style={{ writingMode: 'vertical-rl' }}>
            {traduire('cmp.rm.gravite')}
          </span>
        </div>

        <div className="min-w-0 flex-1">
          <div className="grid grid-cols-4 gap-1.5">
            {GRAVITES.map(function (gravite, rowIndex) {
              return VRAISEMBLANCES.map(function (vraisemblance, colIndex) {
                var count = matrice[rowIndex][colIndex]
                var size = count === 0 ? 0 : 10 + Math.min(count, 5) * 3
                return (
                  <div
                    key={String(gravite) + '-' + String(vraisemblance)}
                    className="flex aspect-square items-center justify-center rounded-sm bg-paper-dim"
                    title={traduire('cmp.rm.gravite') + ' ' + gravite + ' x ' + traduire('cmp.rm.vraisemblance') + ' ' + vraisemblance + ' : ' + count + ' ' + traduire('cmp.rm.scenarios')}
                  >
                    {count > 0 && (
                      <div
                        className={'rounded-full ' + niveauColor(gravite, vraisemblance)}
                        style={{ width: size, height: size }}
                      />
                    )}
                  </div>
                )
              })
            })}
          </div>

          <div className="mt-2 flex justify-between px-1">
            {VRAISEMBLANCES.map(function (v) {
              return (
                <span key={v} className="font-mono text-[9px] text-steel-light">
                  {v}
                </span>
              )
            })}
          </div>
          <div className="mt-0.5 text-center font-mono text-[9px] tracking-wide text-steel-light">
            {traduire('cmp.rm.vraisemblance')}
          </div>
        </div>
      </div>

      <div className="mt-5 flex flex-wrap gap-x-5 gap-y-1.5 border-t border-paper-line pt-4">
        {[
          [traduire('cmp.rm.critique'), 'bg-risk-critical'],
          [traduire('cmp.rm.eleve'), 'bg-risk-high'],
          [traduire('cmp.rm.modere'), 'bg-risk-moderate'],
          [traduire('cmp.rm.faible'), 'bg-risk-low'],
        ].map(function (pair) {
          return (
            <div key={pair[0]} className="flex items-center gap-1.5">
              <span className={'h-2 w-2 rounded-full ' + pair[1]} />
              <span className="text-[11px] text-steel">{pair[0]}</span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
