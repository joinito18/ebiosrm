var GRAVITES = [4, 3, 2, 1]
var VRAISEMBLANCES = [1, 2, 3, 4]

function niveauColor(gravite: number, vraisemblance: number): string {
  var produit = gravite * vraisemblance
  if (produit >= 12) return 'bg-risk-critical'
  if (produit >= 8) return 'bg-risk-high'
  if (produit >= 4) return 'bg-risk-moderate'
  return 'bg-risk-low'
}

var MATRICE: number[][] = [
  [0, 1, 2, 1],
  [1, 2, 3, 2],
  [2, 4, 5, 3],
  [3, 4, 1, 0],
]

export default function RiskMatrix() {
  return (
    <div>
      <div className="flex">
        <div className="flex w-6 flex-col items-center justify-center">
          <span className="rotate-180 font-mono text-[9px] tracking-wide text-steel-light" style={{ writingMode: 'vertical-rl' }}>
            GRAVITE
          </span>
        </div>

        <div className="flex-1">
          <div className="grid grid-cols-4 gap-1.5">
            {GRAVITES.map(function (gravite, rowIndex) {
              return VRAISEMBLANCES.map(function (vraisemblance, colIndex) {
                var count = MATRICE[rowIndex][colIndex]
                var size = count === 0 ? 0 : 10 + Math.min(count, 5) * 3
                return (
                  <div
                    key={String(gravite) + '-' + String(vraisemblance)}
                    className="flex aspect-square items-center justify-center rounded-sm bg-paper-dim"
                    title={'Gravite ' + gravite + ' x Vraisemblance ' + vraisemblance + ' : ' + count + ' scenarios'}
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
            VRAISEMBLANCE
          </div>
        </div>
      </div>

      <div className="mt-5 flex flex-wrap gap-x-5 gap-y-1.5 border-t border-paper-line pt-4">
        {[
          ['Critique', 'bg-risk-critical'],
          ['Eleve', 'bg-risk-high'],
          ['Modere', 'bg-risk-moderate'],
          ['Faible', 'bg-risk-low'],
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
