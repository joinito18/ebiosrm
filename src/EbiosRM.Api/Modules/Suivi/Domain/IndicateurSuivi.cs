namespace EbiosRM.Api.Modules.Suivi.Domain;

/// <summary>
/// Sens dans lequel l'indicateur s'améliore. <see cref="Baisse"/> : plus la
/// valeur est basse, mieux c'est (ex. « nombre de mesures en retard »).
/// <see cref="Hausse"/> : plus haut, mieux c'est (ex. « taux de conformité »).
/// </summary>
public enum SensAmelioration
{
    Baisse,
    Hausse,
}

/// <summary>
/// Un indicateur de suivi (KRI) saisi manuellement pour une étude, avec sa
/// série de points de mesure dans le temps. Les indicateurs calculés
/// automatiquement (avancement du plan, risques résiduels élevés...) ne sont
/// pas persistés -- ils sont recalculés à la lecture.
/// </summary>
public sealed class IndicateurSuivi
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public string Nom { get; private set; } = default!;
    public string? Categorie { get; private set; }
    public string? Unite { get; private set; }

    /// <summary>Valeur cible visée (optionnelle).</summary>
    public double? Cible { get; private set; }

    /// <summary>Seuil au-delà (ou en-deçà, selon <see cref="Sens"/>) duquel l'indicateur est en alerte.</summary>
    public double? SeuilAlerte { get; private set; }

    public SensAmelioration Sens { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private readonly List<PointMesureIndicateur> _points = new();
    public IReadOnlyList<PointMesureIndicateur> Points => _points;

    private IndicateurSuivi() { }

    public static IndicateurSuivi Creer(
        Guid etudeId, string nom, string? categorie, string? unite,
        double? cible, double? seuilAlerte, SensAmelioration sens)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("L'indicateur doit être rattaché à une étude.", nameof(etudeId));
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'indicateur est obligatoire.", nameof(nom));

        return new IndicateurSuivi
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            Nom = nom.Trim(),
            Categorie = Nettoyer(categorie),
            Unite = Nettoyer(unite),
            Cible = cible,
            SeuilAlerte = seuilAlerte,
            Sens = sens,
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public void Modifier(string nom, string? categorie, string? unite, double? cible, double? seuilAlerte, SensAmelioration sens)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'indicateur est obligatoire.", nameof(nom));

        Nom = nom.Trim();
        Categorie = Nettoyer(categorie);
        Unite = Nettoyer(unite);
        Cible = cible;
        SeuilAlerte = seuilAlerte;
        Sens = sens;
    }

    public PointMesureIndicateur AjouterPoint(DateOnly date, double valeur, string? commentaire)
    {
        // Une seule mesure par date : on remplace si elle existe déjà.
        _points.RemoveAll(p => p.Date == date);
        var point = PointMesureIndicateur.Creer(date, valeur, commentaire);
        _points.Add(point);
        return point;
    }

    public void SupprimerPoint(Guid pointId)
    {
        var point = _points.FirstOrDefault(p => p.Id == pointId)
            ?? throw new ArgumentException("Point de mesure introuvable.", nameof(pointId));
        _points.Remove(point);
    }

    private static string? Nettoyer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public sealed class PointMesureIndicateur
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public double Valeur { get; private set; }
    public string? Commentaire { get; private set; }

    private PointMesureIndicateur() { }

    internal static PointMesureIndicateur Creer(DateOnly date, double valeur, string? commentaire)
        => new()
        {
            // Id volontairement non assigné (ValueGeneratedOnAdd).
            Date = date,
            Valeur = valeur,
            Commentaire = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim(),
        };
}
