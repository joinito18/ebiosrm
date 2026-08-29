// Libelles d'affichage des enums du domaine, bilingues. Lus hors React via
// langueCourante() -- un changement de langue re-rend l'arbre, ce qui suffit.
import { langueCourante } from './i18n'

type Table = { [valeur: string]: string }

var FR: { [categorie: string]: Table } = {
  typeBienSupport: {
    SystemeInformation: 'Systeme d’information', Reseau: 'Reseau',
    RessourcesHumaines: 'Ressources humaines', Local: 'Local',
  },
  etatConformite: { Conforme: 'Conforme', NonConforme: 'Non conforme', NonApplicable: 'Non applicable' },
  categorieSR: {
    Etatique: 'Etatique', CrimeOrganise: 'Crime organise', Terroriste: 'Terroriste',
    ActivisteIdeologique: 'Activiste ideologique', OfficineSpecialisee: 'Officine specialisee',
    Amateur: 'Amateur', Vengeur: 'Vengeur', MalveillantPathologique: 'Malveillant pathologique', Autre: 'Autre',
  },
  categorieOV: {
    EspionnageEtatiqueOuIndustriel: 'Espionnage etatique ou industriel',
    PrePositionnementStrategique: 'Pre-positionnement strategique',
    InfluenceDestabilisation: 'Influence / destabilisation',
    EntraveAuFonctionnement: 'Entrave au fonctionnement',
    SabotageDestruction: 'Sabotage / destruction', Lucratif: 'Lucratif',
    DefiAmusement: 'Defi / amusement', Autre: 'Autre',
  },
  categoriePP: { Client: 'Client', Partenaire: 'Partenaire', Prestataire: 'Prestataire', Autre: 'Autre' },
  pertinence: {
    PeuPertinent: 'Peu pertinent', MoyennementPertinent: 'Moyennement pertinent',
    PlutotPertinent: 'Plutot pertinent', TresPertinent: 'Tres pertinent',
  },
  phase: { Connaitre: 'CONNAITRE', Rentrer: 'RENTRER', Trouver: 'TROUVER', Exploiter: 'EXPLOITER' },
  classeAcceptation: {
    AcceptableEnLEtat: 'Acceptable en l’etat', TolerableSousControle: 'Tolerable sous controle',
    Inacceptable: 'Inacceptable',
  },
  coutComplexite: { Plus: '+ (Faible)', PlusPlus: '++ (Modere)', PlusPlusPlus: '+++ (Eleve)' },
  statutMesure: { ALancer: 'A lancer', EnCours: 'En cours', Termine: 'Termine' },
  referentielMesure: { Libre: 'Libre', Iso27002: 'ISO 27002', HygieneAnssi: 'Hygiene ANSSI' },
  zoneDangerosite: { Veille: 'Veille', Controle: 'Controle', Danger: 'Danger' },
  niveauRisque: { Faible: 'Faible', Moyen: 'Moyen', Eleve: 'Eleve' },
  axeMesure: { Gouvernance: 'Gouvernance', Protection: 'Protection', Defense: 'Defense', Resilience: 'Resilience' },
  statutAtelier: { Validee: 'Validee', EnCours: 'En cours', Brouillon: 'Brouillon' },
  motivation: {
    '1': '1 -- Tres peu motive (interet limite, attaque opportuniste)',
    '2': '2 -- Significatif (gain limite, abandonne facilement)',
    '3': '3 -- Motive (objectif clair, investit temps et ressources)',
    '4': '4 -- Fortement motive (cible prioritaire, volonte durable)',
  },
  ressources: {
    '1': '1 -- Limitees (outils gratuits, attaques simples)',
    '2': '2 -- Moderees (outils specialises, petite equipe)',
    '3': '3 -- Importantes (attaques complexes et prolongees)',
    '4': '4 -- Illimitees (experts, operations de longue duree)',
  },
  probabilite: {
    '1': '1 -- Faible (< 10% de reussite)', '2': '2 -- Significative (> 10%)',
    '3': '3 -- Tres elevee (> 40%)', '4': '4 -- Quasi-certaine (> 90%)',
  },
  difficulte: {
    '1': '1 -- Faible (ressources engagees par l’attaquant faibles)',
    '2': '2 -- Moderee (ressources significatives)',
    '3': '3 -- Elevee (ressources importantes)',
    '4': '4 -- Tres elevee (ressources tres importantes)',
  },
}

var EN: { [categorie: string]: Table } = {
  typeBienSupport: {
    SystemeInformation: 'Information system', Reseau: 'Network',
    RessourcesHumaines: 'Human resources', Local: 'Premises',
  },
  etatConformite: { Conforme: 'Compliant', NonConforme: 'Non-compliant', NonApplicable: 'Not applicable' },
  categorieSR: {
    Etatique: 'State', CrimeOrganise: 'Organised crime', Terroriste: 'Terrorist',
    ActivisteIdeologique: 'Ideological activist', OfficineSpecialisee: 'Specialised firm',
    Amateur: 'Amateur', Vengeur: 'Avenger', MalveillantPathologique: 'Pathological attacker', Autre: 'Other',
  },
  categorieOV: {
    EspionnageEtatiqueOuIndustriel: 'State or industrial espionage',
    PrePositionnementStrategique: 'Strategic pre-positioning',
    InfluenceDestabilisation: 'Influence / destabilisation',
    EntraveAuFonctionnement: 'Operational disruption',
    SabotageDestruction: 'Sabotage / destruction', Lucratif: 'Financial gain',
    DefiAmusement: 'Challenge / fun', Autre: 'Other',
  },
  categoriePP: { Client: 'Customer', Partenaire: 'Partner', Prestataire: 'Supplier', Autre: 'Other' },
  pertinence: {
    PeuPertinent: 'Low relevance', MoyennementPertinent: 'Moderate relevance',
    PlutotPertinent: 'Fairly relevant', TresPertinent: 'Highly relevant',
  },
  phase: { Connaitre: 'KNOW', Rentrer: 'GET IN', Trouver: 'FIND', Exploiter: 'EXPLOIT' },
  classeAcceptation: {
    AcceptableEnLEtat: 'Acceptable as is', TolerableSousControle: 'Tolerable under control',
    Inacceptable: 'Unacceptable',
  },
  coutComplexite: { Plus: '+ (Low)', PlusPlus: '++ (Moderate)', PlusPlusPlus: '+++ (High)' },
  statutMesure: { ALancer: 'To do', EnCours: 'In progress', Termine: 'Done' },
  referentielMesure: { Libre: 'Custom', Iso27002: 'ISO 27002', HygieneAnssi: 'ANSSI hygiene' },
  zoneDangerosite: { Veille: 'Watch', Controle: 'Control', Danger: 'Danger' },
  niveauRisque: { Faible: 'Low', Moyen: 'Medium', Eleve: 'High' },
  axeMesure: { Gouvernance: 'Governance', Protection: 'Protection', Defense: 'Defence', Resilience: 'Resilience' },
  statutAtelier: { Validee: 'Validated', EnCours: 'In progress', Brouillon: 'Draft' },
  motivation: {
    '1': '1 -- Barely motivated (limited interest, opportunistic attack)',
    '2': '2 -- Significant (limited gain, gives up easily)',
    '3': '3 -- Motivated (clear objective, invests time and resources)',
    '4': '4 -- Strongly motivated (priority target, lasting resolve)',
  },
  ressources: {
    '1': '1 -- Limited (free tools, simple attacks)',
    '2': '2 -- Moderate (specialised tools, small team)',
    '3': '3 -- Substantial (complex, prolonged attacks)',
    '4': '4 -- Unlimited (experts, long-running operations)',
  },
  probabilite: {
    '1': '1 -- Low (< 10% success)', '2': '2 -- Significant (> 10%)',
    '3': '3 -- Very high (> 40%)', '4': '4 -- Near-certain (> 90%)',
  },
  difficulte: {
    '1': '1 -- Low (attacker commits few resources)',
    '2': '2 -- Moderate (significant resources)',
    '3': '3 -- High (substantial resources)',
    '4': '4 -- Very high (very substantial resources)',
  },
}

var TABLES: { fr: typeof FR; en: typeof EN } = { fr: FR, en: EN }

/** Libelle d'une valeur d'enum, dans la langue courante. */
export function libelle(categorie: keyof typeof FR, valeur: string | null | undefined): string {
  if (valeur == null) return ''
  var l = langueCourante()
  return (TABLES[l][categorie] && TABLES[l][categorie][valeur]) || FR[categorie]?.[valeur] || valeur
}

/** Cles ordonnees d'une categorie (pour construire les <select>). */
export function clesDe(categorie: keyof typeof FR): string[] {
  return Object.keys(FR[categorie] || {})
}

/** Options {value,label} pretes pour un <select>. */
export function optionsDe(categorie: keyof typeof FR): { value: string; label: string }[] {
  return clesDe(categorie).map(function (v) { return { value: v, label: libelle(categorie, v) } })
}
