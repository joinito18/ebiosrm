using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public sealed class ServiceValidationCompletudeAtelier4
{
    private readonly IScenarioOperationnelRepository _scenarioOperationnelRepository;

    public ServiceValidationCompletudeAtelier4(IScenarioOperationnelRepository scenarioOperationnelRepository)
    {
        _scenarioOperationnelRepository = scenarioOperationnelRepository;
    }

    public async Task<ResultatValidationCompletude> VerifierAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioOperationnelRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var manques = new List<string>();
        if (scenarios.Count == 0)
            manques.Add("Au moins un scénario opérationnel est requis.");
        else if (scenarios.All(s => s.ModesOperatoires.Count == 0))
            manques.Add("Au moins un mode opératoire est requis sur un scénario opérationnel.");

        return new ResultatValidationCompletude(manques.Count == 0, manques);
    }
}
