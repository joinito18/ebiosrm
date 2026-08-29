using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Catalogues fournis d'office, identiques sur toutes les installations (y
/// compris le mode bureau hors ligne). Construits en mémoire, jamais persistés :
/// une mise à jour du catalogue = une modification de ce fichier, pas une
/// migration de base ni un re-seed.
///
///   - Mesures : ISO/IEC 27002:2022 (93 mesures de l'Annexe A) + guide
///     d'hygiène informatique de l'ANSSI (42 mesures, v2 de 2017).
///   - Sources de risque : couples SR/OV types, inspirés des exemples de la
///     méthode EBIOS RM (Atelier 2).
/// </summary>
public static class CatalogueSysteme
{
    private static MesureBibliotheque H(int numero, string titre, string rubrique)
        => MesureBibliotheque.Systeme(ReferentielMesure.HygieneAnssi, numero.ToString(), titre, rubrique);

    private static readonly IReadOnlyList<MesureBibliotheque> _iso =
        CatalogueIso27002.Controles
            .Select(c => MesureBibliotheque.Systeme(ReferentielMesure.Iso27002, c.Code, c.Titre, c.Theme))
            .ToList();

    private static readonly IReadOnlyList<MesureBibliotheque> _hygiene = new[]
    {
        // --- Guide d'hygiène informatique de l'ANSSI (42 mesures) --------------
        H(1, "Former les équipes opérationnelles à la sécurité des systèmes d’information", "Sensibiliser et former"),
        H(2, "Sensibiliser les utilisateurs aux bonnes pratiques élémentaires de sécurité informatique", "Sensibiliser et former"),
        H(3, "Maîtriser les risques de l’infogérance", "Sensibiliser et former"),
        H(4, "Identifier les informations et serveurs les plus sensibles et maintenir un schéma du réseau", "Connaître le système d’information"),
        H(5, "Disposer d’un inventaire exhaustif des comptes privilégiés et le maintenir à jour", "Connaître le système d’information"),
        H(6, "Organiser les procédures d’arrivée, de départ et de changement de fonction des utilisateurs", "Connaître le système d’information"),
        H(7, "Autoriser la connexion au réseau de l’entité aux seuls équipements maîtrisés", "Connaître le système d’information"),
        H(8, "Identifier nommément chaque personne accédant au système et distinguer les rôles utilisateur / administrateur", "Authentifier et contrôler les accès"),
        H(9, "Attribuer les bons droits sur les ressources sensibles du système d’information", "Authentifier et contrôler les accès"),
        H(10, "Définir et vérifier des règles de choix et de dimensionnement des mots de passe", "Authentifier et contrôler les accès"),
        H(11, "Protéger les mots de passe stockés sur les systèmes", "Authentifier et contrôler les accès"),
        H(12, "Changer les éléments d’authentification par défaut sur les équipements et services", "Authentifier et contrôler les accès"),
        H(13, "Privilégier lorsque c’est possible une authentification forte", "Authentifier et contrôler les accès"),
        H(14, "Mettre en place un niveau de sécurité minimal sur l’ensemble du parc informatique", "Sécuriser les postes"),
        H(15, "Se protéger des menaces relatives à l’utilisation de supports amovibles", "Sécuriser les postes"),
        H(16, "Utiliser un outil de gestion centralisée afin d’homogénéiser les politiques de sécurité", "Sécuriser les postes"),
        H(17, "Activer et configurer le pare-feu local des postes de travail", "Sécuriser les postes"),
        H(18, "Chiffrer les données sensibles transmises par voie électronique", "Sécuriser les postes"),
        H(19, "Segmenter le réseau et mettre en place un cloisonnement entre les zones", "Sécuriser le réseau"),
        H(20, "S’assurer de la sécurité des réseaux d’accès Wi-Fi et de la séparation des usages", "Sécuriser le réseau"),
        H(21, "Utiliser des protocoles réseaux sécurisés dès qu’ils existent", "Sécuriser le réseau"),
        H(22, "Mettre en place une passerelle d’accès sécurisé à Internet", "Sécuriser le réseau"),
        H(23, "Cloisonner les services visibles depuis Internet du reste du système d’information", "Sécuriser le réseau"),
        H(24, "Protéger sa messagerie professionnelle", "Sécuriser le réseau"),
        H(25, "Sécuriser les interconnexions réseau dédiées avec les partenaires", "Sécuriser le réseau"),
        H(26, "Contrôler et protéger l’accès aux salles serveurs et aux locaux techniques", "Sécuriser le réseau"),
        H(27, "Interdire l’accès à Internet depuis les postes ou serveurs utilisés pour l’administration", "Sécuriser l’administration"),
        H(28, "Utiliser un réseau dédié et cloisonné pour l’administration du système d’information", "Sécuriser l’administration"),
        H(29, "Limiter au strict besoin opérationnel les droits d’administration sur les postes de travail", "Sécuriser l’administration"),
        H(30, "Prendre des mesures de sécurisation physique des terminaux nomades", "Gérer le nomadisme"),
        H(31, "Chiffrer les données sensibles, en particulier sur le matériel potentiellement perdable", "Gérer le nomadisme"),
        H(32, "Sécuriser la connexion réseau des postes utilisés en situation de nomadisme", "Gérer le nomadisme"),
        H(33, "Adopter des politiques de sécurité dédiées aux terminaux mobiles", "Gérer le nomadisme"),
        H(34, "Définir une politique de mise à jour des composants du système d’information", "Maintenir le système d’information à jour"),
        H(35, "Anticiper la fin de la maintenance des logiciels et systèmes et limiter les adhérences logicielles", "Maintenir le système d’information à jour"),
        H(36, "Activer et configurer les journaux des composants les plus importants", "Superviser, auditer, réagir"),
        H(37, "Définir et appliquer une politique de sauvegarde des composants critiques", "Superviser, auditer, réagir"),
        H(38, "Procéder à des contrôles et audits de sécurité réguliers puis appliquer les actions correctives", "Superviser, auditer, réagir"),
        H(39, "Désigner un point de contact en sécurité des systèmes d’information et le faire connaître", "Superviser, auditer, réagir"),
        H(40, "Définir une procédure de gestion des incidents de sécurité", "Superviser, auditer, réagir"),
        H(41, "Mener une analyse de risques formelle", "Pour aller plus loin"),
        H(42, "Privilégier l’usage de produits et de services qualifiés par l’ANSSI", "Pour aller plus loin"),
    };

    public static readonly IReadOnlyList<MesureBibliotheque> Mesures = _iso.Concat(_hygiene).ToList();

    private static SourceRisqueBibliotheque S(
        string cle, CategorieSourceRisque sr, string dsr, CategorieObjectifVise ov, string dov, string theme, int motiv, int ress)
        => SourceRisqueBibliotheque.Systeme(cle, sr, dsr, ov, dov, theme, motiv, ress);

    // Le thème est aligné sur les 4 valeurs proposées par le formulaire de
    // l'Atelier 2 (Organisationnel / Personnes / Physique / Technologique).
    public static readonly IReadOnlyList<SourceRisqueBibliotheque> SourcesRisque = new[]
    {
        S("etatique-espionnage", CategorieSourceRisque.Etatique,
            "Agence de renseignement étatique",
            CategorieObjectifVise.EspionnageEtatiqueOuIndustriel,
            "Captation de données stratégiques ou de propriété intellectuelle", "Organisationnel", 4, 4),
        S("etatique-prepositionnement", CategorieSourceRisque.Etatique,
            "Groupe étatique de pré-positionnement",
            CategorieObjectifVise.PrePositionnementStrategique,
            "Installation de portes dérobées en vue d'une action ultérieure", "Technologique", 4, 4),
        S("crime-organise-lucratif", CategorieSourceRisque.CrimeOrganise,
            "Groupe cybercriminel spécialisé dans le rançongiciel",
            CategorieObjectifVise.Lucratif,
            "Extorsion par chiffrement et menace de divulgation des données", "Technologique", 4, 3),
        S("crime-organise-revente", CategorieSourceRisque.CrimeOrganise,
            "Réseau de revente de données volées",
            CategorieObjectifVise.Lucratif,
            "Revente de données personnelles ou bancaires sur des places de marché clandestines", "Technologique", 3, 3),
        S("terroriste-sabotage", CategorieSourceRisque.Terroriste,
            "Groupe terroriste",
            CategorieObjectifVise.SabotageDestruction,
            "Atteinte à un service essentiel pour provoquer un impact médiatique et physique", "Organisationnel", 4, 2),
        S("activiste-influence", CategorieSourceRisque.ActivisteIdeologique,
            "Collectif hacktiviste",
            CategorieObjectifVise.InfluenceDestabilisation,
            "Défiguration, fuite ou déni de service pour porter un message idéologique", "Organisationnel", 3, 2),
        S("officine-espionnage", CategorieSourceRisque.OfficineSpecialisee,
            "Officine privée de renseignement / concurrent",
            CategorieObjectifVise.EspionnageEtatiqueOuIndustriel,
            "Espionnage économique commandité par un tiers", "Organisationnel", 3, 3),
        S("vengeur-entrave", CategorieSourceRisque.Vengeur,
            "Ancien employé ou prestataire mécontent",
            CategorieObjectifVise.EntraveAuFonctionnement,
            "Sabotage ou divulgation par ressentiment, en s'appuyant sur une connaissance interne", "Personnes", 3, 2),
        S("amateur-defi", CategorieSourceRisque.Amateur,
            "Attaquant opportuniste peu qualifié",
            CategorieObjectifVise.DefiAmusement,
            "Exploitation de vulnérabilités connues par jeu ou par défi", "Technologique", 2, 1),
    };

    // --- Parties prenantes types de l'écosystème (Atelier 3) -----------------
    // Les 4 niveaux (dépendance / pénétration / maturité cyber / confiance) sont
    // sur l'échelle 1..4 et restent INDICATIFS : ils pré-remplissent l'évaluation
    // de la dangerosité, l'analyste les confirme dans son contexte.
    private static PartiePrenanteBibliotheque PP(
        string cle, string nom, CategoriePartiePrenante cat, string roles, int dep, int pen, int mat, int conf, string? descCat = null)
        => PartiePrenanteBibliotheque.Systeme(cle, nom, cat, descCat, roles, dep, pen, mat, conf);

    public static readonly IReadOnlyList<PartiePrenanteBibliotheque> PartiesPrenantes = new[]
    {
        PP("infogerant", "Infogéreur", CategoriePartiePrenante.Prestataire,
            "Exploitation et administration de tout ou partie du système d'information", 4, 4, 3, 3),
        PP("hebergeur-iaas", "Hébergeur cloud (IaaS/PaaS)", CategoriePartiePrenante.Prestataire,
            "Hébergement des serveurs et des données dans une infrastructure mutualisée", 4, 3, 4, 3),
        PP("editeur-saas", "Éditeur de logiciel SaaS métier", CategoriePartiePrenante.Prestataire,
            "Fourniture d'une application métier en ligne hébergeant des données de l'organisation", 3, 3, 3, 3),
        PP("tma", "Prestataire de tierce maintenance applicative", CategoriePartiePrenante.Prestataire,
            "Correction et évolution des applications, avec accès au code et aux environnements", 3, 3, 2, 2),
        PP("operateur-telecom", "Opérateur télécom / fournisseur d'accès", CategoriePartiePrenante.Prestataire,
            "Fourniture des liens réseau et de l'accès à Internet", 4, 2, 3, 3),
        PP("mainteneur-ot", "Mainteneur d'équipements industriels (OT)", CategoriePartiePrenante.Prestataire,
            "Maintenance des automates et systèmes industriels, souvent par accès distant", 3, 3, 2, 2),
        PP("soc-mssp", "Prestataire de SOC / MSSP", CategoriePartiePrenante.Prestataire,
            "Supervision de sécurité, détection et réponse aux incidents", 3, 4, 4, 3),
        PP("sous-traitant-paie", "Sous-traitant paie / gestion RH", CategoriePartiePrenante.Prestataire,
            "Production de la paie et gestion administrative du personnel", 2, 2, 2, 3),
        PP("cabinet-comptable", "Cabinet comptable / commissaire aux comptes", CategoriePartiePrenante.Partenaire,
            "Tenue ou révision de la comptabilité, accès aux données financières", 2, 1, 2, 3),
        PP("regulateur", "Autorité de tutelle / régulateur", CategoriePartiePrenante.Autre,
            "Contrôle du respect d'obligations réglementaires sectorielles", 2, 1, 3, 4,
            "Autorité publique"),
        PP("maison-mere", "Maison mère / groupe", CategoriePartiePrenante.Partenaire,
            "Pilotage groupe, services partagés et interconnexions SI", 3, 3, 3, 4),
        PP("partenaire-edi", "Partenaire d'échange de données (EDI / API)", CategoriePartiePrenante.Partenaire,
            "Échanges automatisés de données métier de système à système", 3, 2, 2, 2),
        PP("prestataire-archivage", "Prestataire d'archivage / éditique", CategoriePartiePrenante.Prestataire,
            "Impression, mise sous pli et conservation de documents", 2, 2, 2, 3),
        PP("gardiennage", "Prestataire de gardiennage / nettoyage", CategoriePartiePrenante.Prestataire,
            "Accès physique récurrent aux locaux, hors heures ouvrées", 1, 2, 1, 2),
        PP("client-grand-compte", "Client grand compte intégré", CategoriePartiePrenante.Client,
            "Client disposant d'un accès à un portail ou d'une intégration SI dédiée", 2, 2, 3, 3),
    };

    // --- Valeurs métier types (Atelier 1) -----------------------------------
    private static ValeurMetierBibliotheque VM(string cle, string intitule, string nature, string entite)
        => ValeurMetierBibliotheque.Systeme(cle, intitule, nature, entite);

    public static readonly IReadOnlyList<ValeurMetierBibliotheque> ValeursMetier = new[]
    {
        VM("paie", "Processus de paie et d'administration du personnel", "Processus", "Direction des ressources humaines"),
        VM("facturation", "Processus de facturation et de recouvrement", "Processus", "Direction financière"),
        VM("production", "Processus de production / chaîne industrielle", "Processus", "Direction industrielle"),
        VM("vente", "Processus de vente et de prise de commande", "Processus", "Direction commerciale"),
        VM("rd", "Processus de R&D et de conception produit", "Processus", "Direction R&D"),
        VM("logistique", "Processus logistique et supply chain", "Processus", "Direction supply chain"),
        VM("sav", "Processus de support et de relation client", "Processus", "Direction de la relation client"),
        VM("services-numeriques", "Site web et services numériques exposés", "Processus", "Direction du numérique"),
        VM("crm", "Référentiel clients (CRM)", "Information", "Direction commerciale"),
        VM("donnees-salaries", "Données personnelles des salariés", "Information", "Direction des ressources humaines"),
        VM("pi", "Propriété intellectuelle et savoir-faire", "Information", "Direction R&D"),
        VM("donnees-financieres", "Données financières et comptables", "Information", "Direction financière"),
        VM("contrats", "Contrats et engagements juridiques", "Information", "Direction juridique"),
        VM("donnees-sante", "Données de santé des patients", "Information", "Direction des soins"),
        VM("parametres-production", "Données et paramètres de production industrielle", "Information", "Direction industrielle"),
    };

    // --- Biens support types (Atelier 1) -----------------------------------
    private static BienSupportBibliotheque BS(string cle, string intitule, TypeBienSupport type, string entite)
        => BienSupportBibliotheque.Systeme(cle, intitule, type, entite);

    public static readonly IReadOnlyList<BienSupportBibliotheque> BiensSupport = new[]
    {
        BS("annuaire", "Annuaire d'entreprise (Active Directory / LDAP)", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("messagerie", "Messagerie électronique", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("erp", "ERP / progiciel de gestion intégré", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("poste-travail", "Poste de travail bureautique", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("serveur-fichiers", "Serveur de fichiers / partage réseau", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("site-web", "Site web / plateforme e-commerce", TypeBienSupport.SystemeInformation, "Direction du numérique"),
        BS("sauvegardes", "Sauvegardes et système de restauration", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("virtualisation", "Hyperviseur / infrastructure de virtualisation", TypeBienSupport.SystemeInformation, "Direction des systèmes d'information"),
        BS("lan", "Réseau local (LAN) et équipements actifs", TypeBienSupport.Reseau, "Direction des systèmes d'information"),
        BS("wan", "Interconnexion opérateur / WAN / VPN", TypeBienSupport.Reseau, "Direction des systèmes d'information"),
        BS("wifi", "Réseau Wi-Fi", TypeBienSupport.Reseau, "Direction des systèmes d'information"),
        BS("scada", "Système industriel / SCADA / automates", TypeBienSupport.Reseau, "Direction industrielle"),
        BS("equipe-admin", "Équipe d'administration système et réseau", TypeBienSupport.RessourcesHumaines, "Direction des systèmes d'information"),
        BS("infogerant-rh", "Prestataire d'infogérance", TypeBienSupport.RessourcesHumaines, "Direction des systèmes d'information"),
        BS("developpeurs", "Développeurs / équipe applicative", TypeBienSupport.RessourcesHumaines, "Direction des systèmes d'information"),
        BS("salle-serveurs", "Salle serveurs / centre de données", TypeBienSupport.Local, "Services généraux"),
        BS("bureaux", "Locaux de bureaux", TypeBienSupport.Local, "Services généraux"),
        BS("site-industriel", "Site industriel / usine", TypeBienSupport.Local, "Direction industrielle"),
    };

    // --- Événements redoutés types (Atelier 1) -----------------------------
    // Gravité INDICATIVE sur l'échelle EBIOS RM 1..4, à réajuster au contexte.
    private static EvenementRedouteBibliotheque ER(string cle, string intitule, int gravite, string impacts)
        => EvenementRedouteBibliotheque.Systeme(cle, intitule, gravite, impacts);

    public static readonly IReadOnlyList<EvenementRedouteBibliotheque> EvenementsRedoutes = new[]
    {
        ER("indispo-si", "Indisponibilité prolongée du SI de production", 4, "Financier, Opérationnel, Image"),
        ER("rancongiciel", "Chiffrement de l'ensemble du SI par rançongiciel", 4, "Financier, Opérationnel, Image, Juridique"),
        ER("fuite-clients", "Divulgation du fichier clients", 3, "Juridique, Image, Financier"),
        ER("fuite-salaries", "Fuite de données personnelles des salariés", 3, "Juridique (RGPD), Image, Social"),
        ER("vol-pi", "Vol de propriété intellectuelle / secrets industriels", 4, "Concurrentiel, Financier, Stratégique"),
        ER("alteration-facturation", "Altération des données de facturation", 3, "Financier, Juridique, Opérationnel"),
        ER("fraude-virement", "Fraude au virement / compromission de la chaîne comptable", 3, "Financier, Image"),
        ER("indispo-ecommerce", "Indisponibilité du site e-commerce en période critique", 3, "Financier, Image"),
        ER("arret-production", "Arrêt de la chaîne de production industrielle", 4, "Financier, Opérationnel, Sécurité des personnes"),
        ER("fuite-sante", "Divulgation de données de santé de patients", 4, "Juridique, Image, Éthique"),
        ER("defiguration", "Défiguration du site web institutionnel", 2, "Image"),
        ER("compromission-admin", "Compromission des comptes à privilèges (administrateurs)", 4, "Opérationnel, Confidentialité, Intégrité"),
        ER("perte-donnees", "Perte définitive de données faute de sauvegarde exploitable", 4, "Opérationnel, Financier, Juridique"),
        ER("usurpation-marque", "Usurpation de l'identité numérique de l'organisation", 2, "Image, Financier (clients)"),
        ER("non-conformite", "Non-conformité réglementaire entraînant une sanction", 3, "Juridique, Financier, Image"),
    };
}
