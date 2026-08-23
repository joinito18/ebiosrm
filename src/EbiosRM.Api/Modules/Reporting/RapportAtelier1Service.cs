using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble les données du rapport Atelier 1 en lisant EXCLUSIVEMENT le dernier
/// SnapshotAtelier1 (P16 : le Reporting ne lit jamais un état en cours d'édition).
/// Aucune dépendance vers IEtudeRepository/IValeurMetierRepository/etc. --
/// c'est volontaire, pas un oubli.
/// </summary>
public sealed class RapportAtelier1Service
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportAtelier1Service(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportAtelier1Data?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 1, cancellationToken);
        if (snapshot is null)
            return null;

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier1Content>(snapshot.ContenuJson);
        if (contenu is null)
            return null;

        var valeursMetierData = contenu.ValeursMetier.Select(vm => new ValeurMetierData(
            vm.Description,
            vm.EntiteProprietaire,
            contenu.BiensSupport
                .Where(b => b.ValeurMetierId == vm.Id)
                .Select(b => new BienSupportData(b.Description, b.Type, b.EntiteProprietaire))
                .ToList()
        )).ToList();

        var evenementsRedoutesData = contenu.EvenementsRedoutes.Select(er =>
        {
            var valeurMetier = contenu.ValeursMetier.FirstOrDefault(vm => vm.Id == er.ValeurMetierId);
            return new EvenementRedouteData(
                valeurMetier?.Description ?? "(valeur métier inconnue)",
                er.Description,
                er.Gravite);
        }).ToList();

        var referentielsData = (contenu.SocleSecurite?.Referentiels ?? new List<ReferentielApplicableSnapshot>())
            .Select(r => new ReferentielApplicableData(r.Nom, r.EtatConformite, r.Theme, r.CodeControle, r.EtatActuel))
            .ToList();

        return new RapportAtelier1Data(
            contenu.NomEtude,
            contenu.Perimetre,
            contenu.Mission,
            contenu.StatutEtude,
            contenu.Version,
            contenu.DateValidationUtc,
            valeursMetierData,
            evenementsRedoutesData,
            referentielsData);
    }
}
