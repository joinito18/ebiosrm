using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

public sealed class ServiceValidationCompletudeAtelier2
{
    private readonly ICoupleSourceRisqueObjectifViseRepository _coupleRepository;

    public ServiceValidationCompletudeAtelier2(ICoupleSourceRisqueObjectifViseRepository coupleRepository)
    {
        _coupleRepository = coupleRepository;
    }

    public async Task<ResultatValidationCompletude> VerifierAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var couples = await _coupleRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var manques = new List<string>();
        if (couples.Count == 0)
            manques.Add("Au moins un couple source de risque / objectif visé est requis.");

        return new ResultatValidationCompletude(manques.Count == 0, manques);
    }
}
