namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Libellés bilingues (FR/EN) des valeurs d'enum du domaine, pour les rapports
/// PDF. Miroir de <c>frontend/src/lib/libelles.ts</c> -- toute évolution d'un
/// côté doit être répercutée de l'autre. Le paramètre <c>anglais</c> par défaut
/// à <c>false</c> garde le comportement historique (rapports en français).
/// </summary>
public static class LibellesRapport
{
    private static string Choisir(bool anglais, string fr, string en) => anglais ? en : fr;

    public static string Pertinence(string? valeur, bool anglais = false) => valeur switch
    {
        "TresPertinent" => Choisir(anglais, "Tres pertinent", "Highly relevant"),
        "PlutotPertinent" => Choisir(anglais, "Plutot pertinent", "Fairly relevant"),
        "MoyennementPertinent" => Choisir(anglais, "Moyennement pertinent", "Moderate relevance"),
        "PeuPertinent" => Choisir(anglais, "Peu pertinent", "Low relevance"),
        _ => "--",
    };

    public static string Zone(string? zone, bool anglais = false) => zone switch
    {
        "Danger" => Choisir(anglais, "Zone de danger", "Danger zone"),
        "Controle" => Choisir(anglais, "Zone de controle", "Control zone"),
        "Veille" => Choisir(anglais, "Zone de veille", "Watch zone"),
        _ => "--",
    };

    public static string NiveauRisque(string? niveau, bool anglais = false) => niveau switch
    {
        "Eleve" => Choisir(anglais, "Eleve", "High"),
        "Moyen" => Choisir(anglais, "Moyen", "Medium"),
        "Faible" => Choisir(anglais, "Faible", "Low"),
        _ => niveau ?? "--",
    };

    public static string ClasseAcceptation(string? classe, bool anglais = false) => classe switch
    {
        "AcceptableEnLEtat" => Choisir(anglais, "Acceptable en l'etat", "Acceptable as is"),
        "TolerableSousControle" => Choisir(anglais, "Tolerable sous controle", "Tolerable under control"),
        "Inacceptable" => Choisir(anglais, "Inacceptable", "Unacceptable"),
        _ => "--",
    };

    public static string StatutMesure(string? statut, bool anglais = false) => statut switch
    {
        "ALancer" => Choisir(anglais, "A lancer", "To do"),
        "EnCours" => Choisir(anglais, "En cours", "In progress"),
        "Termine" => Choisir(anglais, "Termine", "Done"),
        _ => statut ?? "--",
    };

    public static string Couverture(string? couverture, bool anglais = false) => couverture switch
    {
        "Conforme" => Choisir(anglais, "Conforme", "Compliant"),
        "Partielle" => Choisir(anglais, "Partielle", "Partial"),
        "NonApplicable" => Choisir(anglais, "Non applicable", "Not applicable"),
        _ => Choisir(anglais, "Non couverte", "Not covered"),
    };

    public static string Axe(string? axe, bool anglais = false) => axe switch
    {
        "Gouvernance" => Choisir(anglais, "Gouvernance", "Governance"),
        "Protection" => Choisir(anglais, "Protection", "Protection"),
        "Defense" => Choisir(anglais, "Defense", "Defence"),
        "Resilience" => Choisir(anglais, "Resilience", "Resilience"),
        _ => axe ?? "--",
    };

    public static string Phase(string? phase, bool anglais = false) => phase switch
    {
        "Connaitre" => Choisir(anglais, "CONNAITRE", "KNOW"),
        "Rentrer" => Choisir(anglais, "RENTRER", "GET IN"),
        "Trouver" => Choisir(anglais, "TROUVER", "FIND"),
        "Exploiter" => Choisir(anglais, "EXPLOITER", "EXPLOIT"),
        _ => phase ?? "--",
    };

    public static string TypeBienSupport(string? type, bool anglais = false) => type switch
    {
        "SystemeInformation" => Choisir(anglais, "Systeme d'information", "Information system"),
        "Reseau" => Choisir(anglais, "Reseau", "Network"),
        "RessourcesHumaines" => Choisir(anglais, "Ressources humaines", "Human resources"),
        "Local" => Choisir(anglais, "Local", "Premises"),
        _ => type ?? "--",
    };

    public static string Theme(string? theme, bool anglais = false) => theme switch
    {
        "Organisationnel" => Choisir(anglais, "Organisationnel", "Organisational"),
        "Personnes" => Choisir(anglais, "Personnes", "People"),
        "Physique" => Choisir(anglais, "Physique", "Physical"),
        "Technologique" => Choisir(anglais, "Technologique", "Technological"),
        _ => theme ?? "--",
    };

    public static string EtatConformite(string? etat, bool anglais = false) => etat switch
    {
        "Conforme" => Choisir(anglais, "Conforme", "Compliant"),
        "NonConforme" => Choisir(anglais, "Non conforme", "Non-compliant"),
        "NonApplicable" => Choisir(anglais, "Non applicable", "Not applicable"),
        _ => etat ?? "--",
    };
}
