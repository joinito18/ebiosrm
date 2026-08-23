namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

/// <summary>
/// Domain Service : dérive la Pertinence d'un couple SR/OV à partir de la
/// matrice officielle Motivation x Ressources fournie par l'utilisateur.
/// Aucune autre méthode du système ne doit assigner NiveauPertinence
/// directement (P8 : toutes les validations/calculs métier passent par le
/// Core Engine, jamais de valeur dérivée saisie manuellement).
/// </summary>
public static class ServiceCalculPertinence
{
    // Indices : [Motivation - 1, Ressources - 1], échelle 1 à 4 chacun.
    private static readonly NiveauPertinence[,] Matrice = new NiveauPertinence[4, 4]
    {
        // Ressources :      1 (Limitées)                    2 (Significatives)              3 (Importantes)                 4 (Illimitées)
        /* Motivation 1 */ { NiveauPertinence.PeuPertinent,        NiveauPertinence.PeuPertinent,        NiveauPertinence.MoyennementPertinent, NiveauPertinence.MoyennementPertinent },
        /* Motivation 2 */ { NiveauPertinence.PeuPertinent,        NiveauPertinence.MoyennementPertinent, NiveauPertinence.PlutotPertinent,      NiveauPertinence.PlutotPertinent },
        /* Motivation 3 */ { NiveauPertinence.MoyennementPertinent, NiveauPertinence.PlutotPertinent,     NiveauPertinence.PlutotPertinent,      NiveauPertinence.TresPertinent },
        /* Motivation 4 */ { NiveauPertinence.MoyennementPertinent, NiveauPertinence.PlutotPertinent,     NiveauPertinence.TresPertinent,        NiveauPertinence.TresPertinent },
    };

    public static NiveauPertinence Calculer(int motivation, int ressources)
    {
        if (motivation < CoupleSourceRisqueObjectifVise.EchelleMin || motivation > CoupleSourceRisqueObjectifVise.EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(motivation), motivation,
                $"La motivation doit être comprise entre {CoupleSourceRisqueObjectifVise.EchelleMin} et {CoupleSourceRisqueObjectifVise.EchelleMax}.");
        if (ressources < CoupleSourceRisqueObjectifVise.EchelleMin || ressources > CoupleSourceRisqueObjectifVise.EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(ressources), ressources,
                $"Les ressources doivent être comprises entre {CoupleSourceRisqueObjectifVise.EchelleMin} et {CoupleSourceRisqueObjectifVise.EchelleMax}.");

        return Matrice[motivation - 1, ressources - 1];
    }
}
