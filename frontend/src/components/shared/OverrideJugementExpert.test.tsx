import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import OverrideJugementExpert from './OverrideJugementExpert'

var OPTIONS = [
  { value: 'PeuPertinent', label: 'Peu pertinent' },
  { value: 'TresPertinent', label: 'Tres pertinent' },
]

describe('OverrideJugementExpert', function () {
  it('est ferme par defaut et ne montre pas la valeur calculee', function () {
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        options={OPTIONS}
        onDefinir={vi.fn()}
        onReinitialiser={vi.fn()}
      />
    )

    expect(screen.getByText('+ Jugement d expert')).toBeInTheDocument()
    expect(screen.queryByText(/Valeur calculee automatiquement/)).not.toBeInTheDocument()
  })

  it('affiche un libelle different quand un ecart existe deja', function () {
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        valeurRetenue="PeuPertinent"
        justification="Deja neutralise."
        options={OPTIONS}
        onDefinir={vi.fn()}
        onReinitialiser={vi.fn()}
      />
    )

    expect(screen.getByText('Jugement d expert (modifier)')).toBeInTheDocument()
  })

  it('affiche la valeur calculee seulement une fois le bandeau ouvert -- jamais deux valeurs en concurrence par defaut', async function () {
    var user = userEvent.setup()
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        options={OPTIONS}
        onDefinir={vi.fn()}
        onReinitialiser={vi.fn()}
      />
    )

    await user.click(screen.getByText('+ Jugement d expert'))

    expect(screen.getByText(/Valeur calculee automatiquement/)).toBeInTheDocument()
    expect(screen.getByText('TresPertinent')).toBeInTheDocument()
  })

  it('refuse de soumettre sans justification', async function () {
    var user = userEvent.setup()
    var onDefinir = vi.fn()
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        options={OPTIONS}
        onDefinir={onDefinir}
        onReinitialiser={vi.fn()}
      />
    )
    await user.click(screen.getByText('+ Jugement d expert'))

    await user.click(screen.getByText('Retenir cette valeur'))

    expect(onDefinir).not.toHaveBeenCalled()
    expect(screen.getByText(/justification est obligatoire/)).toBeInTheDocument()
  })

  it('soumet la valeur et la justification quand les deux sont fournies', async function () {
    var user = userEvent.setup()
    var onDefinir = vi.fn().mockResolvedValue(undefined)
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        options={OPTIONS}
        onDefinir={onDefinir}
        onReinitialiser={vi.fn()}
      />
    )
    await user.click(screen.getByText('+ Jugement d expert'))
    await user.type(screen.getByPlaceholderText('Justification (obligatoire)'), 'Contexte non capture par la formule.')

    await user.click(screen.getByText('Retenir cette valeur'))

    expect(onDefinir).toHaveBeenCalledWith('PeuPertinent', 'Contexte non capture par la formule.')
  })

  it('ne propose de reinitialiser que si un ecart existe deja', async function () {
    var user = userEvent.setup()
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        options={OPTIONS}
        onDefinir={vi.fn()}
        onReinitialiser={vi.fn()}
      />
    )
    await user.click(screen.getByText('+ Jugement d expert'))

    expect(screen.queryByText('Reinitialiser')).not.toBeInTheDocument()
  })

  it('reinitialiser demande confirmation puis appelle onReinitialiser', async function () {
    var user = userEvent.setup()
    var onReinitialiser = vi.fn().mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    render(
      <OverrideJugementExpert
        valeurCalculee="TresPertinent"
        valeurRetenue="PeuPertinent"
        justification="Deja neutralise."
        options={OPTIONS}
        onDefinir={vi.fn()}
        onReinitialiser={onReinitialiser}
      />
    )
    await user.click(screen.getByText('Jugement d expert (modifier)'))

    await user.click(screen.getByText('Reinitialiser'))

    expect(onReinitialiser).toHaveBeenCalled()
  })
})
