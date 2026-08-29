namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Catalogue MITRE ATT&amp;CK Enterprise (techniques de premier niveau),
/// embarqué dans le code -- identique sur toutes les installations, y compris
/// le mode bureau hors ligne. Chaque technique est rattachée à sa tactique
/// ATT&amp;CK et à l'une des 4 phases de la séquence d'attaque EBIOS RM
/// (CONNAÎTRE / RENTRER / TROUVER / EXPLOITER), ce qui permet de proposer à
/// l'analyste, pour une action élémentaire donnée, les techniques pertinentes
/// à sa phase.
///
/// Liste volontairement non exhaustive (sous-techniques non incluses) : le
/// champ <c>TechniqueMitre</c> d'une action reste du texte libre, un
/// identifiant hors catalogue est accepté.
/// </summary>
public static class CatalogueMitre
{
    public sealed record Technique(string Id, string Nom, string Tactique, string PhaseEbios);

    private static Technique T(string id, string nom, string tactique)
        => new(id, nom, tactique, PhasePour(tactique));

    private static string PhasePour(string tactique) => tactique switch
    {
        "Reconnaissance" or "Resource Development" => "Connaitre",
        "Initial Access" or "Execution" => "Rentrer",
        "Persistence" or "Privilege Escalation" or "Defense Evasion"
            or "Credential Access" or "Discovery" or "Lateral Movement" => "Trouver",
        _ => "Exploiter", // Collection, Command and Control, Exfiltration, Impact
    };

    public static readonly IReadOnlyList<Technique> Techniques = new[]
    {
        // --- Reconnaissance ---
        T("T1595", "Active Scanning", "Reconnaissance"),
        T("T1592", "Gather Victim Host Information", "Reconnaissance"),
        T("T1589", "Gather Victim Identity Information", "Reconnaissance"),
        T("T1590", "Gather Victim Network Information", "Reconnaissance"),
        T("T1591", "Gather Victim Org Information", "Reconnaissance"),
        T("T1598", "Phishing for Information", "Reconnaissance"),
        T("T1597", "Search Closed Sources", "Reconnaissance"),
        T("T1596", "Search Open Technical Databases", "Reconnaissance"),
        T("T1593", "Search Open Websites/Domains", "Reconnaissance"),
        T("T1594", "Search Victim-Owned Websites", "Reconnaissance"),

        // --- Resource Development ---
        T("T1583", "Acquire Infrastructure", "Resource Development"),
        T("T1586", "Compromise Accounts", "Resource Development"),
        T("T1584", "Compromise Infrastructure", "Resource Development"),
        T("T1587", "Develop Capabilities", "Resource Development"),
        T("T1585", "Establish Accounts", "Resource Development"),
        T("T1588", "Obtain Capabilities", "Resource Development"),
        T("T1608", "Stage Capabilities", "Resource Development"),

        // --- Initial Access ---
        T("T1189", "Drive-by Compromise", "Initial Access"),
        T("T1190", "Exploit Public-Facing Application", "Initial Access"),
        T("T1133", "External Remote Services", "Initial Access"),
        T("T1200", "Hardware Additions", "Initial Access"),
        T("T1566", "Phishing", "Initial Access"),
        T("T1091", "Replication Through Removable Media", "Initial Access"),
        T("T1195", "Supply Chain Compromise", "Initial Access"),
        T("T1199", "Trusted Relationship", "Initial Access"),
        T("T1078", "Valid Accounts", "Initial Access"),

        // --- Execution ---
        T("T1059", "Command and Scripting Interpreter", "Execution"),
        T("T1203", "Exploitation for Client Execution", "Execution"),
        T("T1559", "Inter-Process Communication", "Execution"),
        T("T1106", "Native API", "Execution"),
        T("T1053", "Scheduled Task/Job", "Execution"),
        T("T1129", "Shared Modules", "Execution"),
        T("T1072", "Software Deployment Tools", "Execution"),
        T("T1569", "System Services", "Execution"),
        T("T1204", "User Execution", "Execution"),
        T("T1047", "Windows Management Instrumentation", "Execution"),

        // --- Persistence ---
        T("T1098", "Account Manipulation", "Persistence"),
        T("T1547", "Boot or Logon Autostart Execution", "Persistence"),
        T("T1037", "Boot or Logon Initialization Scripts", "Persistence"),
        T("T1176", "Browser Extensions", "Persistence"),
        T("T1554", "Compromise Client Software Binary", "Persistence"),
        T("T1136", "Create Account", "Persistence"),
        T("T1543", "Create or Modify System Process", "Persistence"),
        T("T1546", "Event Triggered Execution", "Persistence"),
        T("T1574", "Hijack Execution Flow", "Persistence"),
        T("T1525", "Implant Internal Image", "Persistence"),
        T("T1505", "Server Software Component", "Persistence"),

        // --- Privilege Escalation ---
        T("T1548", "Abuse Elevation Control Mechanism", "Privilege Escalation"),
        T("T1134", "Access Token Manipulation", "Privilege Escalation"),
        T("T1484", "Domain or Tenant Policy Modification", "Privilege Escalation"),
        T("T1611", "Escape to Host", "Privilege Escalation"),
        T("T1068", "Exploitation for Privilege Escalation", "Privilege Escalation"),
        T("T1055", "Process Injection", "Privilege Escalation"),

        // --- Defense Evasion ---
        T("T1140", "Deobfuscate/Decode Files or Information", "Defense Evasion"),
        T("T1480", "Execution Guardrails", "Defense Evasion"),
        T("T1211", "Exploitation for Defense Evasion", "Defense Evasion"),
        T("T1222", "File and Directory Permissions Modification", "Defense Evasion"),
        T("T1564", "Hide Artifacts", "Defense Evasion"),
        T("T1562", "Impair Defenses", "Defense Evasion"),
        T("T1070", "Indicator Removal", "Defense Evasion"),
        T("T1036", "Masquerading", "Defense Evasion"),
        T("T1112", "Modify Registry", "Defense Evasion"),
        T("T1027", "Obfuscated Files or Information", "Defense Evasion"),
        T("T1218", "System Binary Proxy Execution", "Defense Evasion"),
        T("T1550", "Use Alternate Authentication Material", "Defense Evasion"),
        T("T1497", "Virtualization/Sandbox Evasion", "Defense Evasion"),

        // --- Credential Access ---
        T("T1557", "Adversary-in-the-Middle", "Credential Access"),
        T("T1110", "Brute Force", "Credential Access"),
        T("T1555", "Credentials from Password Stores", "Credential Access"),
        T("T1212", "Exploitation for Credential Access", "Credential Access"),
        T("T1187", "Forced Authentication", "Credential Access"),
        T("T1606", "Forge Web Credentials", "Credential Access"),
        T("T1056", "Input Capture", "Credential Access"),
        T("T1556", "Modify Authentication Process", "Credential Access"),
        T("T1040", "Network Sniffing", "Credential Access"),
        T("T1003", "OS Credential Dumping", "Credential Access"),
        T("T1528", "Steal Application Access Token", "Credential Access"),
        T("T1558", "Steal or Forge Kerberos Tickets", "Credential Access"),
        T("T1552", "Unsecured Credentials", "Credential Access"),

        // --- Discovery ---
        T("T1087", "Account Discovery", "Discovery"),
        T("T1580", "Cloud Infrastructure Discovery", "Discovery"),
        T("T1526", "Cloud Service Discovery", "Discovery"),
        T("T1482", "Domain Trust Discovery", "Discovery"),
        T("T1083", "File and Directory Discovery", "Discovery"),
        T("T1046", "Network Service Discovery", "Discovery"),
        T("T1135", "Network Share Discovery", "Discovery"),
        T("T1201", "Password Policy Discovery", "Discovery"),
        T("T1069", "Permission Groups Discovery", "Discovery"),
        T("T1057", "Process Discovery", "Discovery"),
        T("T1018", "Remote System Discovery", "Discovery"),
        T("T1518", "Software Discovery", "Discovery"),
        T("T1082", "System Information Discovery", "Discovery"),
        T("T1016", "System Network Configuration Discovery", "Discovery"),
        T("T1049", "System Network Connections Discovery", "Discovery"),
        T("T1033", "System Owner/User Discovery", "Discovery"),

        // --- Lateral Movement ---
        T("T1210", "Exploitation of Remote Services", "Lateral Movement"),
        T("T1534", "Internal Spearphishing", "Lateral Movement"),
        T("T1570", "Lateral Tool Transfer", "Lateral Movement"),
        T("T1563", "Remote Service Session Hijacking", "Lateral Movement"),
        T("T1021", "Remote Services", "Lateral Movement"),
        T("T1080", "Taint Shared Content", "Lateral Movement"),

        // --- Collection ---
        T("T1560", "Archive Collected Data", "Collection"),
        T("T1119", "Automated Collection", "Collection"),
        T("T1185", "Browser Session Hijacking", "Collection"),
        T("T1115", "Clipboard Data", "Collection"),
        T("T1530", "Data from Cloud Storage", "Collection"),
        T("T1602", "Data from Configuration Repository", "Collection"),
        T("T1213", "Data from Information Repositories", "Collection"),
        T("T1005", "Data from Local System", "Collection"),
        T("T1039", "Data from Network Shared Drive", "Collection"),
        T("T1074", "Data Staged", "Collection"),
        T("T1114", "Email Collection", "Collection"),
        T("T1113", "Screen Capture", "Collection"),

        // --- Command and Control ---
        T("T1071", "Application Layer Protocol", "Command and Control"),
        T("T1132", "Data Encoding", "Command and Control"),
        T("T1001", "Data Obfuscation", "Command and Control"),
        T("T1568", "Dynamic Resolution", "Command and Control"),
        T("T1573", "Encrypted Channel", "Command and Control"),
        T("T1105", "Ingress Tool Transfer", "Command and Control"),
        T("T1095", "Non-Application Layer Protocol", "Command and Control"),
        T("T1571", "Non-Standard Port", "Command and Control"),
        T("T1572", "Protocol Tunneling", "Command and Control"),
        T("T1090", "Proxy", "Command and Control"),
        T("T1219", "Remote Access Software", "Command and Control"),
        T("T1102", "Web Service", "Command and Control"),

        // --- Exfiltration ---
        T("T1020", "Automated Exfiltration", "Exfiltration"),
        T("T1030", "Data Transfer Size Limits", "Exfiltration"),
        T("T1048", "Exfiltration Over Alternative Protocol", "Exfiltration"),
        T("T1041", "Exfiltration Over C2 Channel", "Exfiltration"),
        T("T1011", "Exfiltration Over Other Network Medium", "Exfiltration"),
        T("T1052", "Exfiltration Over Physical Medium", "Exfiltration"),
        T("T1567", "Exfiltration Over Web Service", "Exfiltration"),
        T("T1029", "Scheduled Transfer", "Exfiltration"),
        T("T1537", "Transfer Data to Cloud Account", "Exfiltration"),

        // --- Impact ---
        T("T1531", "Account Access Removal", "Impact"),
        T("T1485", "Data Destruction", "Impact"),
        T("T1486", "Data Encrypted for Impact", "Impact"),
        T("T1565", "Data Manipulation", "Impact"),
        T("T1491", "Defacement", "Impact"),
        T("T1561", "Disk Wipe", "Impact"),
        T("T1499", "Endpoint Denial of Service", "Impact"),
        T("T1495", "Firmware Corruption", "Impact"),
        T("T1490", "Inhibit System Recovery", "Impact"),
        T("T1498", "Network Denial of Service", "Impact"),
        T("T1496", "Resource Hijacking", "Impact"),
        T("T1489", "Service Stop", "Impact"),
        T("T1529", "System Shutdown/Reboot", "Impact"),
    };

    private static readonly Dictionary<string, Technique> ParId =
        Techniques.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());

    public static Technique? Trouver(string? id)
        => id is not null && ParId.TryGetValue(id.Trim().ToUpperInvariant(), out var t) ? t : null;

    /// <summary>Libellé « T1078 — Valid Accounts » si connue, sinon l'identifiant brut.</summary>
    public static string Libelle(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        var t = Trouver(id);
        return t is null ? id.Trim() : $"{t.Id} — {t.Nom}";
    }
}
