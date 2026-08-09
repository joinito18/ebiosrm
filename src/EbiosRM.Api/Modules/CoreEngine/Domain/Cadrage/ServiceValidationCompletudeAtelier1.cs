namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public sealed record ResultatValidationCompletude(bool EstComplet, List<string> ElementsManquants);

public sealed class ServiceValidationCompletudeAtelier1
{
    private readonly IValeurMetierRepository _valeurMetierRepository;
    private readonly IBienSupportRepository _bienSupportRepository;
    private readonly IEvenementRedouteRepository _evenementRedouteRepository;

    public ServiceValidationCompletudeAtelier1(
        IValeurMetierRepository valeurMetierRepository,
        IBienSupportRepository bienSupportRepository,
        IEvenementRedouteRepository evenementRedouteRepository)
    {
        _valeurMetierRepository = valeurMetierRepository;
        _bienSupportRepository = bienSupportRepository;
        _evenementRedouteRepository = evenementRedouteRepository;
    }

    public async Task<ResultatValidationCompletude> VerifierAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var valeurs = await _valeurMetierRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var biens = await _bienSupportRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var evenements = await _evenementRedouteRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var manques = new List<string>();

        if (valeurs.Count == 0)
            manques.Add("Au moins une valeur métier est requise.");

        if (biens.Count == 0)
            manques.Add("Au moins un bien support est requis.");

        if (evenements.Count == 0)
            manques.Add("Au moins un événement redouté est requis.");

        return new ResultatValidationCompletude(manques.Count == 0, manques);
    }
}
