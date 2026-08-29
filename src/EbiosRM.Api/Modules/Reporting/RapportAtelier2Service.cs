using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble les données du rapport Atelier 2 en lisant EXCLUSIVEMENT le dernier
/// SnapshotAtelier (P16 : le Reporting ne lit jamais un état en cours d'édition).
/// Même patron que RapportAtelier1Service.
/// </summary>
public sealed class RapportAtelier2Service
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportAtelier2Service(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportAtelier2Data?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken, bool anglais = false)
    {
        var snapshot = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 2, cancellationToken);
        if (snapshot is null)
            return null;

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier2Content>(snapshot.ContenuJson);
        if (contenu is null)
            return null;

        List<CoupleSrOvData> ParThemeVers(string theme) =>
            contenu.Couples
                .Where(c => c.Theme == theme)
                .Select(c => new CoupleSrOvData(
                    c.SourceRisque,
                    c.ObjectifVise,
                    c.ContexteVulnerabilite,
                    c.Motivation,
                    c.Ressources,
                    c.Pertinence,
                    c.PertinenceEstJugementExpert,
                    c.JustificationPertinence)
                {
                    DescriptionSourceRisque = c.DescriptionSourceRisque,
                    DescriptionObjectifVise = c.DescriptionObjectifVise,
                    Anglais = anglais
                })
                .ToList();

        var total = contenu.Couples.Count;
        var niveaux = Enum.GetValues<NiveauPertinence>()
            .Reverse() // TresPertinent en premier
            .Select(n =>
            {
                var nombre = contenu.Couples.Count(c => c.Pertinence == n.ToString());
                var pourcentage = total > 0 ? Math.Round(100.0 * nombre / total, 2) : 0.0;
                return new NiveauPertinenceData(n.ToString(), nombre, pourcentage);
            })
            .ToList();

        return new RapportAtelier2Data(
            contenu.NomEtude,
            ParThemeVers("Technologique"),
            ParThemeVers("Organisationnel"),
            ParThemeVers("Personnes"),
            ParThemeVers("Physique"),
            new RepartitionPertinenceData(niveaux, total));
    }
}
