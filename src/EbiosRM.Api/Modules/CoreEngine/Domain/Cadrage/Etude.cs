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
    public string Mission { get; private set; } = default!;
    public string VersionReferentielId { get; private set; } = default!;
    public StatutEtude Statut { get; private set; }
    public StatutEtude StatutAtelier2 { get; private set; }
    public StatutEtude StatutAtelier3 { get; private set; }
    public StatutEtude StatutAtelier4 { get; private set; }
    public StatutEtude StatutAtelier5 { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    /// <summary>
    /// Proprietaire de l'etude. Null = etude de demonstration publique,
    /// visible en lecture par tous les comptes mais non modifiable/supprimable
    /// par personne (cf. middleware de visibilite dans Program.cs). Les etudes
    /// existant avant l'introduction de cette colonne restent a null par la
    /// migration (comportement historique "tout le monde voit tout" conserve
    /// pour les donnees deja creees, isolation appliquee aux nouvelles).
    /// </summary>
    public Guid? ProprietaireId { get; private set; }

    private Etude() { }

    public static Etude Creer(string nom, string perimetre, string mission, Guid? proprietaireId = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'étude est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(perimetre))
            throw new ArgumentException("Le périmètre de l'étude est obligatoire.", nameof(perimetre));

        if (string.IsNullOrWhiteSpace(mission))
            throw new ArgumentException("La mission de l'étude est obligatoire.", nameof(mission));

        return new Etude
        {
            Id = Guid.NewGuid(),
            Nom = nom.Trim(),
            Perimetre = perimetre.Trim(),
            Mission = mission.Trim(),
            VersionReferentielId = "EBIOS_RM_V1",
            Statut = StatutEtude.Brouillon,
            StatutAtelier2 = StatutEtude.Brouillon,
            StatutAtelier3 = StatutEtude.Brouillon,
            StatutAtelier4 = StatutEtude.Brouillon,
            StatutAtelier5 = StatutEtude.Brouillon,
            CreeLeUtc = DateTime.UtcNow,
            ProprietaireId = proprietaireId
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

    /// <summary>
    /// Rouvre l'Atelier 1 après validation pour permettre une correction.
    /// Le snapshot déjà créé (P13) n'est jamais modifié ni supprimé — il reste
    /// consultable comme version figée. Une revalidation ultérieure créera
    /// une nouvelle version du snapshot (accord explicite : correction =
    /// nouvelle version, historique conservé, jamais écrasé).
    /// </summary>
    public void RouvrirAtelier1()
    {
        if (Statut != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 1 : l'étude doit être 'Validee' (statut actuel : '{Statut}').");
        Statut = StatutEtude.EnCours;
    }

    public void DemarrerAtelier2()
    {
        if (Statut != StatutEtude.Validee)
            throw new InvalidOperationException(
                "Impossible de démarrer l'atelier 2 : l'atelier 1 doit être validé au préalable.");
        if (StatutAtelier2 != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 2 : il est déjà au statut '{StatutAtelier2}'.");
        StatutAtelier2 = StatutEtude.EnCours;
    }

    public void ValiderAtelier2()
    {
        if (StatutAtelier2 != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 2 : il doit être 'EnCours' (statut actuel : '{StatutAtelier2}').");
        StatutAtelier2 = StatutEtude.Validee;
    }

    /// <summary>
    /// Rouvre l'Atelier 2 après validation. Le snapshot déjà créé (P13) n'est
    /// jamais modifié ni supprimé — il reste consultable comme version figée,
    /// même principe que RouvrirAtelier1. Une revalidation ultérieure créera
    /// une nouvelle version.
    /// </summary>
    public void RouvrirAtelier2()
    {
        if (StatutAtelier2 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 2 : il doit être 'Validee' (statut actuel : '{StatutAtelier2}').");
        StatutAtelier2 = StatutEtude.EnCours;
    }

    public void DemarrerAtelier3()
    {
        if (StatutAtelier2 != StatutEtude.Validee)
            throw new InvalidOperationException(
                "Impossible de démarrer l'atelier 3 : l'atelier 2 doit être validé au préalable.");
        if (StatutAtelier3 != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 3 : il est déjà au statut '{StatutAtelier3}'.");
        StatutAtelier3 = StatutEtude.EnCours;
    }

    public void ValiderAtelier3()
    {
        if (StatutAtelier3 != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 3 : il doit être 'EnCours' (statut actuel : '{StatutAtelier3}').");
        StatutAtelier3 = StatutEtude.Validee;
    }

    /// <summary>
    /// Rouvre l'Atelier 3 après validation. Même principe que RouvrirAtelier1/2 :
    /// le snapshot déjà créé (P13) reste figé et consultable.
    /// </summary>
    public void RouvrirAtelier3()
    {
        if (StatutAtelier3 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 3 : il doit être 'Validee' (statut actuel : '{StatutAtelier3}').");
        StatutAtelier3 = StatutEtude.EnCours;
    }

    public void DemarrerAtelier4()
    {
        if (StatutAtelier3 != StatutEtude.Validee)
            throw new InvalidOperationException(
                "Impossible de démarrer l'atelier 4 : l'atelier 3 doit être validé au préalable.");
        if (StatutAtelier4 != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 4 : il est déjà au statut '{StatutAtelier4}'.");
        StatutAtelier4 = StatutEtude.EnCours;
    }

    public void ValiderAtelier4()
    {
        if (StatutAtelier4 != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 4 : il doit être 'EnCours' (statut actuel : '{StatutAtelier4}').");
        StatutAtelier4 = StatutEtude.Validee;
    }

    /// <summary>
    /// Rouvre l'Atelier 4 après validation. Même principe que RouvrirAtelier1/2/3 :
    /// le snapshot déjà créé (P13) reste figé et consultable.
    /// </summary>
    public void RouvrirAtelier4()
    {
        if (StatutAtelier4 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 4 : il doit être 'Validee' (statut actuel : '{StatutAtelier4}').");
        StatutAtelier4 = StatutEtude.EnCours;
    }

    public void DemarrerAtelier5()
    {
        if (StatutAtelier4 != StatutEtude.Validee)
            throw new InvalidOperationException(
                "Impossible de démarrer l'atelier 5 : l'atelier 4 doit être validé au préalable.");
        if (StatutAtelier5 != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 5 : il est déjà au statut '{StatutAtelier5}'.");
        StatutAtelier5 = StatutEtude.EnCours;
    }

    public void ValiderAtelier5()
    {
        if (StatutAtelier5 != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 5 : il doit être 'EnCours' (statut actuel : '{StatutAtelier5}').");
        StatutAtelier5 = StatutEtude.Validee;
    }

    /// <summary>
    /// Rouvre l'Atelier 5 après validation. Même principe que RouvrirAtelier1/2/3/4 :
    /// le snapshot déjà créé (P13) reste figé et consultable.
    /// </summary>
    public void RouvrirAtelier5()
    {
        if (StatutAtelier5 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 5 : il doit être 'Validee' (statut actuel : '{StatutAtelier5}').");
        StatutAtelier5 = StatutEtude.EnCours;
    }
}
