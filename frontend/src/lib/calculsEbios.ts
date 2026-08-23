// Miroirs TS des ServiceCalcul* du backend (src/EbiosRM.Api/Modules/CoreEngine/Domain/...),
// dupliqués sciemment côté frontend pour l'aperçu live pendant la saisie --
// pas de test de parité automatisé (même choix déjà fait pour les textes de
// grille du rapport PDF, cf. PROJECT_CONTEXT.md). Si la formule change côté
// backend, penser à répercuter ici.

// Miroir de ServiceCalculPertinence.cs -- matrice Motivation x Ressources.
export var MATRICE_PERTINENCE: string[][] = [
  ['PeuPertinent', 'PeuPertinent', 'MoyennementPertinent', 'MoyennementPertinent'],
  ['PeuPertinent', 'MoyennementPertinent', 'PlutotPertinent', 'PlutotPertinent'],
  ['MoyennementPertinent', 'PlutotPertinent', 'PlutotPertinent', 'TresPertinent'],
  ['MoyennementPertinent', 'PlutotPertinent', 'TresPertinent', 'TresPertinent'],
]

export function calculerPertinence(motivation: number, ressources: number): string {
  return MATRICE_PERTINENCE[motivation - 1][ressources - 1]
}

// Miroir de ServiceCalculVraisemblance.cs -- matrice Probabilite de succes x Difficulte technique.
export var MATRICE_VRAISEMBLANCE: string[][] = [
  ['V2', 'V2', 'V1', 'V1'],
  ['V3', 'V2', 'V2', 'V1'],
  ['V3', 'V3', 'V2', 'V2'],
  ['V4', 'V3', 'V3', 'V2'],
]

export function calculerVraisemblance(probabiliteSucces: number, difficulteTechnique: number): string {
  return MATRICE_VRAISEMBLANCE[probabiliteSucces - 1][difficulteTechnique - 1]
}

// Miroir de ServiceCalculNiveauDangerosite.cs -- formule (Dependance x Penetration) / (Maturite x Confiance).
export function calculerNiveauDangerosite(dependance: number, penetration: number, maturiteCyber: number, confiance: number): number {
  return Math.round(((dependance * penetration) / (maturiteCyber * confiance)) * 100) / 100
}

export function determinerZoneDangerosite(niveau: number): string {
  if (niveau >= 4) return 'Danger'
  if (niveau >= 1) return 'Controle'
  return 'Veille'
}

// Miroir de ServiceCalculNiveauRisque.cs -- matrice Gravite x Vraisemblance
// (grille officielle "societe de biotechnologie", seuils par defaut du
// projet, ajustables).
export var MATRICE_RISQUE: string[][] = [
  ['Faible', 'Faible', 'Moyen', 'Moyen'],
  ['Faible', 'Faible', 'Moyen', 'Eleve'],
  ['Faible', 'Moyen', 'Eleve', 'Eleve'],
  ['Faible', 'Moyen', 'Eleve', 'Eleve'],
]

export function calculerNiveauRisque(gravite: number, indexVraisemblance: number): string {
  return MATRICE_RISQUE[gravite - 1][indexVraisemblance]
}

export function determinerClasseAcceptation(niveau: string): string {
  if (niveau === 'Faible') return 'AcceptableEnLEtat'
  if (niveau === 'Moyen') return 'TolerableSousControle'
  return 'Inacceptable'
}
