namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum StatutEtude
{
    Brouillon,
    EnCours,
    Validee
}

public sealed class Etude
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public string Perimetre { get; private set; } = default!;
    public string VersionReferentielId { get; private set; } = default!;
    public StatutEtude Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private Etude() { }

    public static Etude Creer(string nom, string perimetre)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'étude est obligatoire.", nameof(nom));

        if (string.IsNullOrWhiteSpace(perimetre))
            throw new ArgumentException("Le périmètre de l'étude est obligatoire.", nameof(perimetre));

        return new Etude
        {
            Id = Guid.NewGuid(),
            Nom = nom.Trim(),
            Perimetre = perimetre.Trim(),
            VersionReferentielId = "EBIOS_RM_V1",
            Statut = StatutEtude.Brouillon,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void DemarrerAtelier1()
    {
        if (Statut != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 1 : l'étude est déjà au statut '{Statut}'.");

        Statut = StatutEtude.EnCours;
    }

    public void ValiderAtelier1()
    {
        if (Statut != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 1 : l'étude doit être 'EnCours' (statut actuel : '{Statut}').");

        Statut = StatutEtude.Validee;
    }
}
