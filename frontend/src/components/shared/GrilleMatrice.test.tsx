import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import GrilleMatrice from './GrilleMatrice'

var MATRICE = [
  ['A', 'B'],
  ['C', 'D'],
]

function couleur() {
  return 'text-ink'
}

describe('GrilleMatrice', function () {
  it('affiche chaque valeur de la matrice', function () {
    render(
      <GrilleMatrice
        matrice={MATRICE}
        ligneLabels={['L1', 'L2']}
        colonneLabels={['C1', 'C2']}
        ligneTitre="Ligne"
        colonneTitre="Colonne"
        couleurCellule={couleur}
      />
    )

    expect(screen.getByText('A')).toBeInTheDocument()
    expect(screen.getByText('B')).toBeInTheDocument()
    expect(screen.getByText('C')).toBeInTheDocument()
    expect(screen.getByText('D')).toBeInTheDocument()
  })

  it('met en evidence uniquement la cellule selectionnee', function () {
    render(
      <GrilleMatrice
        matrice={MATRICE}
        ligneLabels={['L1', 'L2']}
        colonneLabels={['C1', 'C2']}
        ligneTitre="Ligne"
        colonneTitre="Colonne"
        ligneSelectionnee={1}
        colonneSelectionnee={0}
        couleurCellule={couleur}
      />
    )

    var celluleSelectionnee = screen.getByText('C')
    var celluleNonSelectionnee = screen.getByText('A')
    expect(celluleSelectionnee.className).toContain('border-signature')
    expect(celluleNonSelectionnee.className).not.toContain('border-signature')
  })

  it('n a aucune cellule en evidence quand rien n est selectionne', function () {
    render(
      <GrilleMatrice
        matrice={MATRICE}
        ligneLabels={['L1', 'L2']}
        colonneLabels={['C1', 'C2']}
        ligneTitre="Ligne"
        colonneTitre="Colonne"
        couleurCellule={couleur}
      />
    )

    for (var texte of ['A', 'B', 'C', 'D']) {
      expect(screen.getByText(texte).className).not.toContain('border-signature')
    }
  })
})
