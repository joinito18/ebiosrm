namespace EbiosRM.Api.Modules.Audit.Domain;

/// <summary>
/// Traduit une requête d'écriture (méthode HTTP + chemin) en une phrase
/// lisible pour le journal d'audit. Pas de couverture exhaustive des ~80
/// endpoints : on reconnaît les verbes d'atelier et les ressources connues,
/// et on retombe sur un libellé générique sinon.
/// </summary>
public static class DescriptionAction
{
    private static readonly Dictionary<string, string> Ressources = new()
    {
        ["valeurs-metier"] = "valeur métier",
        ["biens-support"] = "bien support",
        ["evenements-redoutes"] = "événement redouté",
        ["socle-securite"] = "socle de sécurité",
        ["referentiels"] = "référentiel du socle",
        ["couples-sr-ov"] = "couple source de risque / objectif visé",
        ["parties-prenantes"] = "partie prenante",
        ["mesures"] = "mesure sur l'écosystème",
        ["scenarios-strategiques"] = "scénario stratégique",
        ["chemins-attaque"] = "chemin d'attaque",
        ["evenements-intermediaires"] = "événement intermédiaire",
        ["scenarios-operationnels"] = "scénario opérationnel",
        ["modes-operatoires"] = "mode opératoire",
        ["scenarios-de-risque"] = "scénario de risque",
        ["plan-traitement-risque"] = "plan de traitement du risque",
        ["scenario-strategique"] = "scénario stratégique",
        ["scenario-operationnel"] = "scénario opérationnel",
        ["scenario-de-risque"] = "scénario de risque",
        ["membres"] = "membre de l'étude",
    };

    public static string Deriver(string methode, string chemin)
    {
        // Segments après "/api/v1/etudes/{guid}/".
        var segments = chemin.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var i = Array.FindIndex(segments, s => s == "etudes");
        var apres = i >= 0 && segments.Length > i + 2
            ? segments[(i + 2)..]
            : Array.Empty<string>();

        if (apres.Length == 0)
            return methode == "DELETE" ? "Suppression de l'étude" : "Modification de l'étude";

        var premier = apres[0];

        // Verbes de workflow d'atelier : demarrer-atelierN, valider-atelierN, rouvrir-atelierN.
        foreach (var (prefixe, libelle) in new[] { ("demarrer-atelier", "Démarrage"), ("valider-atelier", "Validation"), ("rouvrir-atelier", "Réouverture") })
        {
            if (premier.StartsWith(prefixe, StringComparison.Ordinal))
                return $"{libelle} de l'atelier {premier[prefixe.Length..]}";
        }

        if (premier is "acceptation")
            return methode == "DELETE" ? "Retrait de l'acceptation formelle du risque" : "Acceptation formelle du risque";
        if (premier.Contains("retenue") || premier.Contains("dangerosite") || premier.Contains("vraisemblance")
            || premier.Contains("pertinence") || premier.Contains("niveau-risque"))
            return methode == "DELETE"
                ? "Retour à la valeur calculée (jugement d'expert retiré)"
                : "Ajustement d'une valeur par jugement d'expert";

        // Dernier segment non-GUID = la ressource concernée.
        var ressource = apres.LastOrDefault(s => !Guid.TryParse(s, out _)) ?? premier;
        var libelleRessource = Ressources.GetValueOrDefault(ressource, ressource.Replace('-', ' '));

        return methode switch
        {
            "POST" => $"Création : {libelleRessource}",
            "PUT" or "PATCH" => $"Modification : {libelleRessource}",
            "DELETE" => $"Suppression : {libelleRessource}",
            _ => $"{methode} : {libelleRessource}",
        };
    }
}
