using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public sealed class ServiceValidationCompletudeAtelier3
{
    private readonly IScenarioStrategiqueRepository _scenarioRepository;

    public ServiceValidationCompletudeAtelier3(IScenarioStrategiqueRepository scenarioRepository)
    {
        _scenarioRepository = scenarioRepository;
    }

    public async Task<ResultatValidationCompletude> VerifierAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var manques = new List<string>();
        if (scenarios.Count == 0)
            manques.Add("Au moins un scénario stratégique est requis.");

        return new ResultatValidationCompletude(manques.Count == 0, manques);
    }
}
