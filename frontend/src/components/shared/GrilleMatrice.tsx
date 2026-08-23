interface GrilleMatriceProps {
  matrice: string[][]
  ligneLabels: string[]
  colonneLabels: string[]
  ligneTitre: string
  colonneTitre: string
  ligneSelectionnee?: number
  colonneSelectionnee?: number
  couleurCellule: (valeur: string) => string
}

// Grille de calcul officielle, toujours affichée a cote de la saisie : met
// en evidence la case correspondant aux valeurs actuellement choisies, pour
// que l analyste voie la regle avant de valider, pas seulement le resultat
// apres coup.
export default function GrilleMatrice(props: GrilleMatriceProps) {
  return (
    <div className="overflow-x-auto">
      <table className="border-collapse text-[10px]">
        <thead>
          <tr>
            <th className="p-1 text-left font-mono text-steel-light">{props.ligneTitre} \ {props.colonneTitre}</th>
            {props.colonneLabels.map(function (label, i) {
              return <th key={i} className="p-1 text-left font-mono font-normal text-steel-light">{label}</th>
            })}
          </tr>
        </thead>
        <tbody>
          {props.ligneLabels.map(function (ligneLabel, ligneIdx) {
            return (
              <tr key={ligneIdx}>
                <td className="p-1 font-mono text-steel-light">{ligneLabel}</td>
                {props.colonneLabels.map(function (_, colIdx) {
                  var selectionnee = ligneIdx === props.ligneSelectionnee && colIdx === props.colonneSelectionnee
                  var valeur = props.matrice[ligneIdx][colIdx]
                  return (
                    <td
                      key={colIdx}
                      className={
                        'border p-1 text-center font-mono font-medium ' +
                        (selectionnee ? 'border-signature bg-signature-soft ' : 'border-paper-line ') +
                        props.couleurCellule(valeur)
                      }
                    >
                      {valeur}
                    </td>
                  )
                })}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
