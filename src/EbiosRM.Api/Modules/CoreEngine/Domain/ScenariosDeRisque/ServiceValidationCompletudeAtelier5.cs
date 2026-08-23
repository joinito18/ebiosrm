using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public sealed class ServiceValidationCompletudeAtelier5
{
    private readonly IScenarioDeRisqueRepository _scenarioDeRisqueRepository;

    public ServiceValidationCompletudeAtelier5(IScenarioDeRisqueRepository scenarioDeRisqueRepository)
    {
        _scenarioDeRisqueRepository = scenarioDeRisqueRepository;
    }

    public async Task<ResultatValidationCompletude> VerifierAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioDeRisqueRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var manques = new List<string>();
        if (scenarios.Count == 0)
            manques.Add("Au moins un scénario de risque est requis.");
        else if (scenarios.Any(s => s.NiveauRisqueResiduel is null))
            manques.Add("Le risque résiduel doit être évalué sur chaque scénario de risque.");

        return new ResultatValidationCompletude(manques.Count == 0, manques);
    }
}
