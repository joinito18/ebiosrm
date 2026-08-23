namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Échelle de vraisemblance officielle EBIOS RM (Atelier 4) : V1 (peu
/// vraisemblable) à V4 (certain ou déjà produit).
/// </summary>
public enum NiveauVraisemblance
{
    V1,
    V2,
    V3,
    V4
}

/// <summary>
/// Domain Service : dérive la Vraisemblance d'un mode opératoire à partir de
/// la grille officielle EBIOS RM "Probabilité de succès x Difficulté
/// technique" (méthode d'évaluation affinée, Atelier 4 partie 2). Aucune
/// autre méthode ne doit assigner Vraisemblance directement (P8, même
/// principe que ServiceCalculPertinence/ServiceCalculNiveauDangerosite).
/// </summary>
public static class ServiceCalculVraisemblance
{
    public const int EchelleMin = 1;
    public const int EchelleMax = 4;

    // Indices : [ProbabiliteSucces - 1, DifficulteTechnique - 1], échelle 1 à 4 chacun.
    private static readonly NiveauVraisemblance[,] Matrice = new NiveauVraisemblance[4, 4]
    {
        // Difficulte technique :     1 (Faible)                2 (Moderee)               3 (Elevee)                4 (Tres elevee)
        /* Probabilite 1 (Faible) */          { NiveauVraisemblance.V2, NiveauVraisemblance.V2, NiveauVraisemblance.V1, NiveauVraisemblance.V1 },
        /* Probabilite 2 (Significative) */   { NiveauVraisemblance.V3, NiveauVraisemblance.V2, NiveauVraisemblance.V2, NiveauVraisemblance.V1 },
        /* Probabilite 3 (Tres elevee) */     { NiveauVraisemblance.V3, NiveauVraisemblance.V3, NiveauVraisemblance.V2, NiveauVraisemblance.V2 },
        /* Probabilite 4 (Quasi-certaine) */  { NiveauVraisemblance.V4, NiveauVraisemblance.V3, NiveauVraisemblance.V3, NiveauVraisemblance.V2 },
    };

    public static NiveauVraisemblance Calculer(int probabiliteSucces, int difficulteTechnique)
    {
        VerifierEchelle(probabiliteSucces, nameof(probabiliteSucces));
        VerifierEchelle(difficulteTechnique, nameof(difficulteTechnique));

        return Matrice[probabiliteSucces - 1, difficulteTechnique - 1];
    }

    private static void VerifierEchelle(int valeur, string nomParametre)
    {
        if (valeur < EchelleMin || valeur > EchelleMax)
            throw new ArgumentOutOfRangeException(nomParametre, valeur,
                $"Doit être compris entre {EchelleMin} et {EchelleMax}.");
    }
}
