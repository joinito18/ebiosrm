namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Aggregate Root : ScenarioStrategique (module Scenarios de risque, Atelier 3).
/// Relation 1:1 stricte avec un CoupleSourceRisqueObjectifVise -- appliquee
/// par une contrainte unique en base (CoupleSourceRisqueObjectifViseId) et
/// verifiee par le repository avant creation (ObtenirParCoupleIdAsync).
///
/// Cible obligatoirement un EvenementRedoute (donc une ValeurMetier, Atelier
/// 1) -- la Gravite du scenario est celle de cet ER, jamais dupliquee ici :
/// elle est lue en direct depuis l'agregat EvenementRedoute au moment de
/// l'affichage/rapport (P8, pas de valeur derivee stockee separement de sa
/// source). "La gravite est identique pour le scenario stratégique et tous
/// ses chemins d'attaque" (doc officielle Atelier 3, partie 2).
/// </summary>
public sealed class ScenarioStrategique
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid CoupleSourceRisqueObjectifViseId { get; private set; }
    public Guid EvenementRedouteId { get; private set; }
    public string Description { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private ScenarioStrategique() { }

    public static ScenarioStrategique Creer(Guid etudeId, Guid coupleSourceRisqueObjectifViseId, Guid evenementRedouteId, string description)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le scénario stratégique doit être rattaché à une étude.", nameof(etudeId));
        if (coupleSourceRisqueObjectifViseId == Guid.Empty)
            throw new ArgumentException("Le scénario stratégique doit être rattaché à un couple source de risque / objectif visé.", nameof(coupleSourceRisqueObjectifViseId));
        if (evenementRedouteId == Guid.Empty)
            throw new ArgumentException("Le scénario stratégique doit cibler un événement redouté.", nameof(evenementRedouteId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du scénario stratégique est obligatoire.", nameof(description));

        return new ScenarioStrategique
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            CoupleSourceRisqueObjectifViseId = coupleSourceRisqueObjectifViseId,
            EvenementRedouteId = evenementRedouteId,
            Description = description.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(Guid evenementRedouteId, string description)
    {
        if (evenementRedouteId == Guid.Empty)
            throw new ArgumentException("Le scénario stratégique doit cibler un événement redouté.", nameof(evenementRedouteId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du scénario stratégique est obligatoire.", nameof(description));
        EvenementRedouteId = evenementRedouteId;
        Description = description.Trim();
    }
}
