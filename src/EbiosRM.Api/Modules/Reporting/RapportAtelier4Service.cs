using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble les données du rapport Atelier 4 en lisant EXCLUSIVEMENT le dernier
/// SnapshotAtelier (P16 : le Reporting ne lit jamais un état en cours d'édition).
/// Même patron que RapportAtelier1/2/3Service.
/// </summary>
public sealed class RapportAtelier4Service
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportAtelier4Service(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportAtelier4Data?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 4, cancellationToken);
        if (snapshot is null)
            return null;

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier4Content>(snapshot.ContenuJson);
        if (contenu is null)
            return null;

        var cheminsParId = contenu.CheminsAttaque.ToDictionary(c => c.Id);
        var scenariosStrategiquesParId = contenu.ScenariosStrategiques.ToDictionary(s => s.Id);
        var couplesParId = contenu.Couples.ToDictionary(c => c.Id);
        var biensSupportParId = contenu.BiensSupport.ToDictionary(b => b.Id);

        var data = new List<ScenarioOperationnelData>();
        foreach (var s in contenu.ScenariosOperationnels)
        {
            cheminsParId.TryGetValue(s.CheminAttaqueId, out var chemin);
            var libelleChemin = chemin?.Description ?? "?";
            var libelleCouple = "?";
            if (chemin is not null && scenariosStrategiquesParId.TryGetValue(chemin.ScenarioStrategiqueId, out var scenarioStrat))
            {
                if (couplesParId.TryGetValue(scenarioStrat.CoupleSourceRisqueObjectifViseId, out var couple))
                {
                    var sr = couple.SourceRisque == "Autre" ? couple.DescriptionSourceRisque : couple.SourceRisque;
                    var ov = couple.ObjectifVise == "Autre" ? couple.DescriptionObjectifVise : couple.ObjectifVise;
                    libelleCouple = sr + " -- " + ov;
                }
            }

            var modes = s.ModesOperatoires.Select(m =>
            {
                var actions = m.ActionsElementaires.Select(a =>
                {
                    biensSupportParId.TryGetValue(a.BienSupportId, out var bien);
                    return new ActionElementaireData(a.Description, a.Phase.ToString(), bien?.Description ?? "(bien support inconnu)");
                }).ToList();
                return new ModeOperatoireData(
                    m.Description, actions, m.ProbabiliteSucces, m.DifficulteTechnique, m.Vraisemblance.ToString(),
                    m.VraisemblanceEstJugementExpert, m.JustificationVraisemblance);
            }).ToList();

            data.Add(new ScenarioOperationnelData(libelleCouple, libelleChemin, s.VraisemblanceGlobale?.ToString(), modes));
        }

        return new RapportAtelier4Data(contenu.NomEtude, data);
    }
}
