namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Contrairement aux autres rapports (P16 : lecture exclusive des
/// SnapshotAtelier figés), le cadre de suivi lit l'état COURANT du plan de
/// traitement et des scénarios de risque -- c'est le seul livrable dont le
/// but est justement de suivre une progression qui continue après la
/// validation de l'Atelier 5 (mesures qui passent à "Terminé" au fil des
/// mois, risques dont le résiduel est réévalué). Un document figé au moment
/// de la validation n'aurait ici aucun sens.
/// </summary>
public sealed record RapportCadreDeSuiviData(
    string NomEtude,
    string Perimetre,
    DateTime DateGeneration,
    List<ScenarioDeRisqueData> ScenariosDeRisque,
    List<MesureTraitementRisqueData> Mesures,
    Dictionary<string, int> AvancementParStatut);
