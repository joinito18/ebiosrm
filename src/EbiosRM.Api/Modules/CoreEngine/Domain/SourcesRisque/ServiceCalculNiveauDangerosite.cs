namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

/// <summary>
/// Domain Service : calcule le niveau de dangerosité d'une partie prenante
/// de l'écosystème (Atelier 3), formule officielle EBIOS Risk Manager :
/// NiveauDangerosite = (Dépendance x Pénétration) / (Maturité cyber x Confiance).
/// "Dangerosité" est le terme officiel depuis EBIOS RM 1.5 (mars 2024,
/// conformité ISO/CEI 27005:2022) -- remplace "Menace" (fichier et classe
/// renommés en conséquence, ex-ServiceCalculNiveauMenace). Aucune autre
/// méthode du système ne doit calculer ou assigner NiveauDangerosite
/// directement (P8, même principe que ServiceCalculPertinence).
/// </summary>
public static class ServiceCalculNiveauDangerosite
{
    public static double Calculer(int dependance, int penetration, int maturiteCyber, int confiance)
    {
        VerifierEchelle(dependance, nameof(dependance));
        VerifierEchelle(penetration, nameof(penetration));
        VerifierEchelle(maturiteCyber, nameof(maturiteCyber));
        VerifierEchelle(confiance, nameof(confiance));

        return Math.Round((double)(dependance * penetration) / (maturiteCyber * confiance), 2);
    }

    private static void VerifierEchelle(int valeur, string nomParametre)
    {
        if (valeur < PartiePrenante.EchelleMin || valeur > PartiePrenante.EchelleMax)
            throw new ArgumentOutOfRangeException(nomParametre, valeur,
                $"Doit être compris entre {PartiePrenante.EchelleMin} et {PartiePrenante.EchelleMax}.");
    }

    /// <summary>
    /// Classe le niveau de dangerosité en zone (Veille / Contrôle / Danger),
    /// grille officielle EBIOS RM. La documentation officielle ne fixe pas
    /// de seuils numériques universels (l'exemple "société de biotechnologie"
    /// utilise des seuils propres au contexte de l'étude) -- seuils par
    /// défaut retenus ici, symétriques autour de 1.0 (niveau obtenu quand les
    /// 4 critères sont à leur valeur médiane) : à ajuster si un besoin de
    /// seuils contextuels par étude apparaît plus tard.
    /// </summary>
    public static ZoneDangerosite DeterminerZone(double niveauDangerosite)
    {
        if (niveauDangerosite >= 4) return ZoneDangerosite.Danger;
        if (niveauDangerosite >= 1) return ZoneDangerosite.Controle;
        return ZoneDangerosite.Veille;
    }
}
