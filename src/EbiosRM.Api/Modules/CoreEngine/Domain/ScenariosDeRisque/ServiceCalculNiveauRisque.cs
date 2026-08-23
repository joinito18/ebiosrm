using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Domain Service : dérive le NiveauRisque d'un scénario de risque à partir
/// de la grille officielle EBIOS RM "Gravité x Vraisemblance" (Atelier 5,
/// exemple officiel "société de biotechnologie"). Ces seuils sont un choix
/// par défaut du projet, ajustables -- pas des seuils universels imposés par
/// la doc officielle, qui ne prescrit qu'un principe de classement, pas des
/// valeurs figées pour toute organisation. Aucune autre méthode ne doit
/// assigner NiveauRisque directement (P8, même principe que
/// ServiceCalculPertinence/ServiceCalculNiveauDangerosite/ServiceCalculVraisemblance).
/// </summary>
public static class ServiceCalculNiveauRisque
{
    // Indices : [Gravite - 1, (int)Vraisemblance], Gravite 1-4, Vraisemblance V1-V4.
    private static readonly NiveauRisque[,] Matrice = new NiveauRisque[4, 4]
    {
        // Vraisemblance :          V1                    V2                   V3                   V4
        /* Gravite 1 */    { NiveauRisque.Faible, NiveauRisque.Faible, NiveauRisque.Moyen,  NiveauRisque.Moyen },
        /* Gravite 2 */    { NiveauRisque.Faible, NiveauRisque.Faible, NiveauRisque.Moyen,  NiveauRisque.Eleve },
        /* Gravite 3 */    { NiveauRisque.Faible, NiveauRisque.Moyen,  NiveauRisque.Eleve,  NiveauRisque.Eleve },
        /* Gravite 4 */    { NiveauRisque.Faible, NiveauRisque.Moyen,  NiveauRisque.Eleve,  NiveauRisque.Eleve },
    };

    public static NiveauRisque Calculer(int gravite, NiveauVraisemblance vraisemblance)
    {
        if (gravite < EvenementRedoute.GraviteMin || gravite > EvenementRedoute.GraviteMax)
            throw new ArgumentOutOfRangeException(
                nameof(gravite), gravite,
                $"La gravité doit être comprise entre {EvenementRedoute.GraviteMin} et {EvenementRedoute.GraviteMax}.");

        return Matrice[gravite - 1, (int)vraisemblance];
    }

    public static ClasseAcceptation DeterminerClasseAcceptation(NiveauRisque niveau) => niveau switch
    {
        NiveauRisque.Faible => ClasseAcceptation.AcceptableEnLEtat,
        NiveauRisque.Moyen => ClasseAcceptation.TolerableSousControle,
        NiveauRisque.Eleve => ClasseAcceptation.Inacceptable,
        _ => throw new ArgumentOutOfRangeException(nameof(niveau), niveau, "Niveau de risque inconnu.")
    };
}
