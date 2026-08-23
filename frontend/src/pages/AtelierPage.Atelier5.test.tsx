import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  AcceptationFormelleSection, ScenarioDeRisqueCard, PlanTraitementRisqueSection,
  SelectionScenariosDeRisque, libellesScenarios,
} from './AtelierPage'
import type { ScenarioDeRisque, PlanTraitementRisque } from '../lib/api'

vi.mock('../lib/api', async function () {
  var actual = await vi.importActual<typeof import('../lib/api')>('../lib/api')
  return {
    ...actual,
    creerScenarioDeRisque: vi.fn(),
    supprimerScenarioDeRisque: vi.fn(),
    definirNiveauRisqueInitialRetenue: vi.fn(),
    reinitialiserNiveauRisqueInitial: vi.fn(),
    evaluerRisqueResiduel: vi.fn(),
    definirNiveauRisqueResiduelRetenue: vi.fn(),
    reinitialiserNiveauRisqueResiduel: vi.fn(),
    accepterRisqueResiduel: vi.fn(),
    retirerAcceptation: vi.fn(),
    creerPlanTraitementRisque: vi.fn(),
    ajouterMesureTraitementRisque: vi.fn(),
    modifierMesureTraitementRisque: vi.fn(),
    supprimerMesureTraitementRisque: vi.fn(),
  }
})

import {
  evaluerRisqueResiduel, accepterRisqueResiduel, retirerAcceptation,
  creerPlanTraitementRisque, ajouterMesureTraitementRisque,
} from '../lib/api'

function scenario(overrides?: Partial<ScenarioDeRisque>): ScenarioDeRisque {
  return {
    id: 's1',
    cheminAttaqueId: 'c1',
    libelleChemin: 'Intrusion frontale',
    libelleCouple: 'Etatique -- Lucratif',
    gravite: 4,
    vraisemblanceInitiale: 'V3',
    niveauRisqueInitialCalcule: 'Eleve',
    niveauRisqueInitialRetenu: null,
    justificationNiveauRisqueInitial: null,
    niveauRisqueInitial: 'Eleve',
    classeAcceptationInitiale: 'Inacceptable',
    graviteResiduelle: null,
    vraisemblanceResiduelle: null,
    niveauRisqueResiduelCalcule: null,
    niveauRisqueResiduelRetenu: null,
    justificationNiveauRisqueResiduel: null,
    niveauRisqueResiduel: null,
    classeAcceptationResiduelle: null,
    accepteParDirection: false,
    nomProprietaireRisque: null,
    nomValidateurSecurite: null,
    nomSponsorExecutif: null,
    justificationAcceptation: null,
    dateAcceptationUtc: null,
    ...overrides,
  }
}

beforeEach(function () {
  vi.clearAllMocks()
})

describe('AcceptationFormelleSection', function () {
  it('n exige pas de sponsor ni de justification quand le risque residuel est Faible', async function () {
    var user = userEvent.setup()
    vi.mocked(accepterRisqueResiduel).mockResolvedValue(scenario())
    var onChange = vi.fn()
    render(<AcceptationFormelleSection etudeId="e1" scenario={scenario({ niveauRisqueResiduel: 'Faible' })} onChange={onChange} />)

    expect(screen.queryByPlaceholderText(/Sponsor executif/)).not.toBeInTheDocument()

    await user.type(screen.getByPlaceholderText('Proprietaire du risque'), 'Direction generale')
    await user.type(screen.getByPlaceholderText('Validateur securite'), 'RSSI')
    await user.click(screen.getByText('Accepter formellement'))

    await waitFor(function () {
      expect(accepterRisqueResiduel).toHaveBeenCalledWith('e1', 's1', 'Direction generale', 'RSSI', undefined, undefined)
    })
  })

  it('refuse la soumission sans proprietaire ni validateur', async function () {
    var user = userEvent.setup()
    render(<AcceptationFormelleSection etudeId="e1" scenario={scenario({ niveauRisqueResiduel: 'Faible' })} onChange={vi.fn()} />)

    await user.click(screen.getByText('Accepter formellement'))

    expect(screen.getByText(/proprietaire du risque et le validateur securite sont obligatoires/)).toBeInTheDocument()
    expect(accepterRisqueResiduel).not.toHaveBeenCalled()
  })

  it('exige un sponsor executif et une justification quand le risque residuel est Eleve', async function () {
    var user = userEvent.setup()
    render(<AcceptationFormelleSection etudeId="e1" scenario={scenario({ niveauRisqueResiduel: 'Eleve' })} onChange={vi.fn()} />)

    expect(screen.getByPlaceholderText(/Sponsor executif/)).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Justification/)).toBeInTheDocument()

    await user.type(screen.getByPlaceholderText('Proprietaire du risque'), 'Direction generale')
    await user.type(screen.getByPlaceholderText('Validateur securite'), 'RSSI')
    await user.click(screen.getByText('Accepter formellement'))

    expect(screen.getByText(/sponsor executif et une justification ecrite/)).toBeInTheDocument()
    expect(accepterRisqueResiduel).not.toHaveBeenCalled()
  })

  it('accepte un risque eleve une fois sponsor et justification renseignes', async function () {
    var user = userEvent.setup()
    vi.mocked(accepterRisqueResiduel).mockResolvedValue(scenario())
    var onChange = vi.fn()
    render(<AcceptationFormelleSection etudeId="e1" scenario={scenario({ niveauRisqueResiduel: 'Eleve' })} onChange={onChange} />)

    await user.type(screen.getByPlaceholderText('Proprietaire du risque'), 'Direction generale')
    await user.type(screen.getByPlaceholderText('Validateur securite'), 'RSSI')
    await user.type(screen.getByPlaceholderText(/Sponsor executif/), 'PDG')
    await user.type(screen.getByPlaceholderText(/Justification/), 'Risque maintenu, surveillance renforcee.')
    await user.click(screen.getByText('Accepter formellement'))

    await waitFor(function () {
      expect(accepterRisqueResiduel).toHaveBeenCalledWith('e1', 's1', 'Direction generale', 'RSSI', 'PDG', 'Risque maintenu, surveillance renforcee.')
      expect(onChange).toHaveBeenCalled()
    })
  })

  it('affiche en lecture seule une acceptation deja formalisee et permet de la retirer', async function () {
    var user = userEvent.setup()
    vi.mocked(retirerAcceptation).mockResolvedValue(scenario())
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    var onChange = vi.fn()
    render(<AcceptationFormelleSection etudeId="e1" scenario={scenario({
      niveauRisqueResiduel: 'Faible', accepteParDirection: true,
      nomProprietaireRisque: 'Direction generale', nomValidateurSecurite: 'RSSI',
    })} onChange={onChange} />)

    expect(screen.getByText('Direction generale')).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('Proprietaire du risque')).not.toBeInTheDocument()

    await user.click(screen.getByText('Retirer l acceptation'))

    await waitFor(function () {
      expect(retirerAcceptation).toHaveBeenCalledWith('e1', 's1')
      expect(onChange).toHaveBeenCalled()
    })
  })
})

describe('ScenarioDeRisqueCard', function () {
  it('affiche le niveau initial derive et masque le residuel tant qu il n est pas evalue', function () {
    render(<ScenarioDeRisqueCard etudeId="e1" description="Intrusion frontale" scenario={scenario()} onChange={vi.fn()} />)

    expect(screen.getByText(/derive : gravite 4 x vraisemblance V3/)).toBeInTheDocument()
    expect(screen.getAllByText('Eleve')[0]).toBeInTheDocument()
    expect(screen.queryByText(/ACCEPTATION FORMELLE/)).not.toBeInTheDocument()
  })

  it('evalue le risque residuel avec la gravite et la vraisemblance selectionnees', async function () {
    var user = userEvent.setup()
    vi.mocked(evaluerRisqueResiduel).mockResolvedValue(scenario())
    var onChange = vi.fn()
    render(<ScenarioDeRisqueCard etudeId="e1" description="Intrusion frontale" scenario={scenario()} onChange={onChange} />)

    await user.click(screen.getByText('Evaluer le risque residuel'))

    await waitFor(function () {
      expect(evaluerRisqueResiduel).toHaveBeenCalledWith('e1', 's1', 4, 'V1')
      expect(onChange).toHaveBeenCalled()
    })
  })

  it('affiche la section d acceptation une fois le risque residuel evalue', function () {
    render(<ScenarioDeRisqueCard etudeId="e1" description="Intrusion frontale" scenario={scenario({ niveauRisqueResiduel: 'Faible' })} onChange={vi.fn()} />)

    expect(screen.getByText(/ACCEPTATION FORMELLE/)).toBeInTheDocument()
  })
})

describe('SelectionScenariosDeRisque', function () {
  it('affiche un message si aucun scenario de risque n existe', function () {
    render(<SelectionScenariosDeRisque scenariosDeRisque={[]} selection={[]} onChange={vi.fn()} />)
    expect(screen.getByText(/Aucun scenario de risque materialise/)).toBeInTheDocument()
  })

  it('bascule la selection au clic sur une case', async function () {
    var user = userEvent.setup()
    var onChange = vi.fn()
    render(<SelectionScenariosDeRisque scenariosDeRisque={[scenario()]} selection={[]} onChange={onChange} />)

    await user.click(screen.getByRole('checkbox'))

    expect(onChange).toHaveBeenCalledWith(['s1'])
  })
})

describe('libellesScenarios', function () {
  it('resout les libelles des scenarios existants et signale les scenarios supprimes', function () {
    var libelles = libellesScenarios([scenario()], ['s1', 'inconnu'])
    expect(libelles).toEqual(['Etatique -- Lucratif -- Intrusion frontale', '(scenario supprime)'])
  })
})

describe('PlanTraitementRisqueSection', function () {
  it('propose de creer le plan quand il n existe pas encore', async function () {
    var user = userEvent.setup()
    vi.mocked(creerPlanTraitementRisque).mockResolvedValue({ id: 'p1', etudeId: 'e1', mesures: [] })
    var onChange = vi.fn()
    render(<PlanTraitementRisqueSection etudeId="e1" plan={null} scenariosDeRisque={[]} onChange={onChange} />)

    await user.click(screen.getByText('+ Creer le plan de traitement du risque'))

    await waitFor(function () {
      expect(creerPlanTraitementRisque).toHaveBeenCalledWith('e1')
      expect(onChange).toHaveBeenCalled()
    })
  })

  it('groupe les mesures par axe et signale les axes vides', function () {
    var plan: PlanTraitementRisque = {
      id: 'p1', etudeId: 'e1',
      mesures: [{
        id: 'm1', description: 'Chiffrement des flux', axe: 'Protection', scenariosDeRisqueIds: ['s1'],
        responsable: 'RSSI', freinsEtDifficultes: null, coutComplexite: 'PlusPlus', echeance: '6 mois',
        statut: 'ALancer', creeLeUtc: '2026-01-01T00:00:00Z',
      }],
    }
    render(<PlanTraitementRisqueSection etudeId="e1" plan={plan} scenariosDeRisque={[scenario()]} onChange={vi.fn()} />)

    expect(screen.getByText('Chiffrement des flux')).toBeInTheDocument()
    expect(screen.getAllByText('Aucune mesure.')).toHaveLength(3)
  })

  it('ajoute une mesure avec au moins un scenario de risque selectionne', async function () {
    var user = userEvent.setup()
    var plan: PlanTraitementRisque = { id: 'p1', etudeId: 'e1', mesures: [] }
    vi.mocked(ajouterMesureTraitementRisque).mockResolvedValue(plan)
    var onChange = vi.fn()
    render(<PlanTraitementRisqueSection etudeId="e1" plan={plan} scenariosDeRisque={[scenario()]} onChange={onChange} />)

    await user.click(screen.getByText('Ajouter une mesure de traitement'))
    await user.type(screen.getByPlaceholderText('Description de la mesure'), 'Chiffrement des flux')
    await user.type(screen.getByPlaceholderText('Responsable'), 'RSSI')
    await user.click(screen.getByText(/Etatique -- Lucratif/))
    await user.click(screen.getByText('Ajouter'))

    await waitFor(function () {
      expect(ajouterMesureTraitementRisque).toHaveBeenCalledWith('e1', {
        description: 'Chiffrement des flux', axe: 'Gouvernance', scenariosDeRisqueIds: ['s1'],
        responsable: 'RSSI', freinsEtDifficultes: null, coutComplexite: 'Plus', echeance: null, statut: 'ALancer',
      })
      expect(onChange).toHaveBeenCalled()
    })
  })

  it('refuse d ajouter une mesure sans scenario de risque selectionne', async function () {
    var user = userEvent.setup()
    var plan: PlanTraitementRisque = { id: 'p1', etudeId: 'e1', mesures: [] }
    render(<PlanTraitementRisqueSection etudeId="e1" plan={plan} scenariosDeRisque={[scenario()]} onChange={vi.fn()} />)

    await user.click(screen.getByText('Ajouter une mesure de traitement'))
    await user.type(screen.getByPlaceholderText('Description de la mesure'), 'Chiffrement des flux')
    await user.type(screen.getByPlaceholderText('Responsable'), 'RSSI')
    await user.click(screen.getByText('Ajouter'))

    expect(screen.getByText(/obligatoires/)).toBeInTheDocument()
    expect(ajouterMesureTraitementRisque).not.toHaveBeenCalled()
  })
})
