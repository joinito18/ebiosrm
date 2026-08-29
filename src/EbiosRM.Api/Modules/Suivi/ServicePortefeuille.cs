using EbiosRM.Api.Modules.Collaboration.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.Suivi;

/// <summary>
/// Vue consolidée de toutes les études visibles par un utilisateur : pour
/// chaque étude, l'avancement des ateliers, la répartition des risques
/// résiduels, l'avancement du plan de traitement et le taux de couverture
/// NIS2. Destinée au pilotage multi-études (reporting COMEX / RSSI).
/// </summary>
public sealed class ServicePortefeuille
{
    private readonly IEtudeRepository _etudes;
    private readonly IEtudeMembreRepository _membres;
    private readonly ServiceMetriquesEtude _metriques;

    public ServicePortefeuille(IEtudeRepository etudes, IEtudeMembreRepository membres, ServiceMetriquesEtude metriques)
    {
        _etudes = etudes;
        _membres = membres;
        _metriques = metriques;
    }

    public sealed record LignePortefeuille(
        Guid EtudeId,
        string Nom,
        string Statut,
        string StatutAtelier5,
        string? MonRole,
        int ScenariosDeRisque,
        Dictionary<string, int> RisquesResiduels,
        int RisquesEleveResiduelNonAcceptes,
        int Mesures,
        int MesuresTerminees,
        int MesuresEnRetard,
        double? TauxCouvertureNis2);

    public async Task<List<LignePortefeuille>> ConstruireAsync(Guid utilisateurId, CancellationToken ct)
    {
        var etudes = await _etudes.ListerVisiblesAsync(utilisateurId, ct);
        var roles = (await _membres.ListerParUtilisateurAsync(utilisateurId, ct))
            .ToDictionary(m => m.EtudeId, m => m.Role.ToString());

        var lignes = new List<LignePortefeuille>();
        foreach (var e in etudes)
        {
            var m = await _metriques.ConstruireAsync(e.Id, ct);
            lignes.Add(new LignePortefeuille(
                e.Id, e.Nom, e.Statut.ToString(), e.StatutAtelier5.ToString(),
                roles.GetValueOrDefault(e.Id),
                m.ScenariosDeRisque, m.RisquesResiduels, m.RisquesEleveResiduelNonAcceptes,
                m.Mesures, m.MesuresTerminees, m.MesuresEnRetard, m.TauxCouvertureNis2));
        }

        // Les études les plus exposées d'abord.
        return lignes
            .OrderByDescending(l => l.RisquesEleveResiduelNonAcceptes)
            .ThenByDescending(l => l.RisquesResiduels.GetValueOrDefault("Eleve"))
            .ThenBy(l => l.Nom, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
