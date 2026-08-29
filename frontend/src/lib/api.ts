var API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5197/api/v1'

export interface Etude {
  id: string
  nom: string
  perimetre: string
  mission: string
  versionReferentielId: string
  statut: 'Brouillon' | 'EnCours' | 'Validee'
  statutAtelier2: 'Brouillon' | 'EnCours' | 'Validee'
  statutAtelier3: 'Brouillon' | 'EnCours' | 'Validee'
  statutAtelier4: 'Brouillon' | 'EnCours' | 'Validee'
  statutAtelier5: 'Brouillon' | 'EnCours' | 'Validee'
  creeLeUtc: string
  monRole?: RoleEtude | null
}

export type RoleEtude = 'Proprietaire' | 'Editeur' | 'Lecteur'

export interface MembreEtude {
  utilisateurId: string
  nomAffiche: string
  email: string
  role: RoleEtude
  ajouteLeUtc: string
  estMoi: boolean
}

export interface ValeurMetier {
  id: string
  etudeId: string
  description: string
  entiteProprietaire: string
  creeLeUtc: string
}

export interface BienSupport {
  id: string
  etudeId: string
  valeurMetierId: string
  description: string
  type: string
  entiteProprietaire: string
  creeLeUtc: string
}

export interface EvenementRedoute {
  id: string
  etudeId: string
  valeurMetierId: string
  description: string
  gravite: number
  creeLeUtc: string
}

export interface ReferentielApplicable {
  id: string
  nom: string
  etat: string
  theme?: string | null
  codeControle?: string | null
  etatActuel?: string | null
}

export interface SocleSecurite {
  id: string
  etudeId: string
  referentiels: ReferentielApplicable[]
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export interface Utilisateur {
  id: string
  email: string
  nomAffiche: string
}

var CLE_JETON = 'ebiosrm_token'

export function stockerToken(token: string) {
  localStorage.setItem(CLE_JETON, token)
}

export function obtenirToken(): string | null {
  return localStorage.getItem(CLE_JETON)
}

export function effacerToken() {
  localStorage.removeItem(CLE_JETON)
}

export function estConnecte(): boolean {
  return obtenirToken() !== null
}

// Toute defaillance ressort en ApiError typee : reseau injoignable, reponse
// non-JSON (page d'erreur d'un proxy, backend Render en cours de reveil...),
// ou erreur applicative. Les pages font toutes `err instanceof ApiError ?
// err.message : ...` -- un throw non type leur ferait afficher un message
// generique trompeur ("verifiez localhost").
//
// Reveil du backend (plan gratuit Render) : le conteneur se met en veille
// apres ~15 min d'inactivite et met ~1 min a redemarrer. On absorbe ce delai
// avec quelques reessais automatiques (erreur reseau ou 502/503/504) plutot
// que de renvoyer une erreur a l'utilisateur des le premier appel a froid.
var ATTENTES_REESSAI_MS = [3000, 12000, 20000]

async function apiFetch(path: string, options?: RequestInit): Promise<any> {
  var headers: Record<string, string> = { 'Content-Type': 'application/json' }
  var token = obtenirToken()
  if (token) {
    headers['Authorization'] = 'Bearer ' + token
  }

  for (var tentative = 0; ; tentative++) {
    var estDerniereTentative = tentative >= ATTENTES_REESSAI_MS.length

    var response: Response
    try {
      response = await fetch(API_BASE + path, { headers, ...options })
    } catch (e) {
      if (!estDerniereTentative) {
        await attendre(ATTENTES_REESSAI_MS[tentative])
        continue
      }
      throw new ApiError(0, 'Impossible de joindre le serveur. Il est peut-etre en veille (redemarrage ~1 min) -- reessayez dans un instant.')
    }

    if (response.status === 401) effacerToken()
    if (response.status === 404) return null

    if ((response.status === 502 || response.status === 503 || response.status === 504) && !estDerniereTentative) {
      await attendre(ATTENTES_REESSAI_MS[tentative])
      continue
    }

    var text = await response.text()
    var body: any = null
    if (text) {
      try {
        body = JSON.parse(text)
      } catch (e) {
        // Corps non-JSON : typiquement une page d'erreur HTML de l'hebergeur
        // quand le backend est indisponible ou en train de demarrer.
        if (!response.ok) {
          throw new ApiError(response.status, messageIndisponibilite(response.status))
        }
      }
    }

    if (!response.ok) {
      var message = body && body.error ? body.error : messageIndisponibilite(response.status)
      throw new ApiError(response.status, message)
    }

    return body
  }
}

function attendre(ms: number): Promise<void> {
  return new Promise(function (resolve) { setTimeout(resolve, ms) })
}

function messageIndisponibilite(status: number): string {
  if (status === 502 || status === 503 || status === 504) {
    return 'Le serveur est momentanement indisponible (il redemarre peut-etre). Reessayez dans une minute.'
  }
  return 'Erreur serveur (' + status + '). Reessayez dans un instant.'
}

export function inscription(email: string, motDePasse: string, nomAffiche: string): Promise<{ token: string; utilisateur: Utilisateur }> {
  return apiFetch('/auth/inscription', { method: 'POST', body: JSON.stringify({ email, motDePasse, nomAffiche }) })
    .then(function (r) { stockerToken(r.token); return r })
}

export function connexion(email: string, motDePasse: string): Promise<{ token: string; utilisateur: Utilisateur }> {
  return apiFetch('/auth/connexion', { method: 'POST', body: JSON.stringify({ email, motDePasse }) })
    .then(function (r) { stockerToken(r.token); return r })
}

export function obtenirUtilisateurCourant(): Promise<Utilisateur> {
  return apiFetch('/auth/moi')
}

export function listEtudes(): Promise<Etude[]> {
  return apiFetch('/etudes')
}

export function getEtude(id: string): Promise<Etude | null> {
  return apiFetch('/etudes/' + id)
}

export interface EntreeJournal {
  id: string
  dateUtc: string
  nomUtilisateur: string
  action: string
  methode: string
  chemin: string
  statutHttp: number
}

export function listerJournal(etudeId: string, limite?: number): Promise<EntreeJournal[]> {
  return apiFetch('/etudes/' + etudeId + '/journal' + (limite ? '?limite=' + limite : ''))
}

export function listerMembres(etudeId: string): Promise<MembreEtude[]> {
  return apiFetch('/etudes/' + etudeId + '/membres')
}

export function ajouterMembre(etudeId: string, email: string, role: RoleEtude): Promise<unknown> {
  return apiFetch('/etudes/' + etudeId + '/membres', { method: 'POST', body: JSON.stringify({ email, role }) })
}

export function changerRoleMembre(etudeId: string, utilisateurId: string, role: RoleEtude): Promise<unknown> {
  return apiFetch('/etudes/' + etudeId + '/membres/' + utilisateurId, { method: 'PUT', body: JSON.stringify({ role }) })
}

export function retirerMembre(etudeId: string, utilisateurId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/membres/' + utilisateurId, { method: 'DELETE' })
}

export function createEtude(nom: string, perimetre: string, mission: string): Promise<Etude> {
  return apiFetch('/etudes', {
    method: 'POST',
    body: JSON.stringify({ nom: nom, perimetre: perimetre, mission: mission }),
  })
}

export function supprimerEtude(id: string): Promise<void> {
  return apiFetch('/etudes/' + id, { method: 'DELETE' })
}

export function dupliquerEtude(id: string, nom?: string): Promise<{ id: string }> {
  return apiFetch('/etudes/' + id + '/dupliquer', {
    method: 'POST',
    body: JSON.stringify({ nom: nom || null }),
  })
}

// contenu = le texte brut d'un fichier .json produit par l'export d'une etude.
export function importerEtude(contenu: string): Promise<{ id: string }> {
  return apiFetch('/etudes/importer', { method: 'POST', body: contenu })
}

// --- Bibliotheque (elements reutilisables d'une etude a l'autre) ---

export type ReferentielMesure = 'Libre' | 'Iso27002' | 'HygieneAnssi'

export interface MesureBiblio {
  id: string
  systeme: boolean
  referentiel: ReferentielMesure
  code: string | null
  titre: string
  description: string | null
  categorie: string | null
}

export interface SourceRisqueBiblio {
  id: string
  systeme: boolean
  sourceRisque: string
  descriptionSourceRisque: string
  objectifVise: string
  descriptionObjectifVise: string
  theme: string | null
  motivationTypique: number | null
  ressourcesTypiques: number | null
}

export function listerMesuresBiblio(referentiel?: string, q?: string): Promise<MesureBiblio[]> {
  var params = new URLSearchParams()
  if (referentiel) params.set('referentiel', referentiel)
  if (q) params.set('q', q)
  var suffixe = params.toString() ? '?' + params.toString() : ''
  return apiFetch('/bibliotheque/mesures' + suffixe)
}

export function ajouterMesureBiblio(m: { titre: string; description?: string | null; categorie?: string | null; code?: string | null; referentiel?: string }): Promise<MesureBiblio> {
  return apiFetch('/bibliotheque/mesures', {
    method: 'POST',
    body: JSON.stringify({
      titre: m.titre, description: m.description || null, categorie: m.categorie || null,
      code: m.code || null, referentiel: m.referentiel || 'Libre',
    }),
  })
}

export function supprimerMesureBiblio(id: string): Promise<void> {
  return apiFetch('/bibliotheque/mesures/' + id, { method: 'DELETE' })
}

export function listerSourcesRisqueBiblio(q?: string): Promise<SourceRisqueBiblio[]> {
  return apiFetch('/bibliotheque/sources-risque' + (q ? '?q=' + encodeURIComponent(q) : ''))
}

export function ajouterSourceRisqueBiblio(s: {
  sourceRisque: string; descriptionSourceRisque: string; objectifVise: string; descriptionObjectifVise: string
  theme?: string | null; motivationTypique?: number | null; ressourcesTypiques?: number | null
}): Promise<SourceRisqueBiblio> {
  return apiFetch('/bibliotheque/sources-risque', { method: 'POST', body: JSON.stringify(s) })
}

export function supprimerSourceRisqueBiblio(id: string): Promise<void> {
  return apiFetch('/bibliotheque/sources-risque/' + id, { method: 'DELETE' })
}

// --- Cartographie graphique de l'Atelier 3 (SVG genere cote serveur) ---

export type CartographieType = 'ecosysteme' | 'chemins-attaque'

// Renvoie le markup SVG (jeton injecte comme apiFetch). null si l'etude est
// introuvable. Utilise <div dangerouslySetInnerHTML> pour un rendu inline
// (redimensionnable, texte selectionnable) plutot qu'un <img>.
export async function chargerCartographieSvg(etudeId: string, type: CartographieType, residuel?: boolean): Promise<string | null> {
  var headers: Record<string, string> = {}
  var token = obtenirToken()
  if (token) headers['Authorization'] = 'Bearer ' + token

  var suffixe = type === 'ecosysteme' && residuel ? '?residuel=true' : ''
  var response = await fetch(API_BASE + '/etudes/' + etudeId + '/cartographie/' + type + '.svg' + suffixe, { headers })
  if (response.status === 401) effacerToken()
  if (response.status === 404) return null
  if (!response.ok) throw new ApiError(response.status, 'Impossible de charger la cartographie (' + response.status + ')')
  return await response.text()
}

export function demarrerAtelier1(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier1', { method: 'POST' })
}

export function validerAtelier1(etudeId: string): Promise<{ etude: Etude; snapshotVersion: number }> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier1', { method: 'POST' })
}

export function rouvrirAtelier1(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/rouvrir-atelier1', { method: 'POST' })
}

export function demarrerAtelier2(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier2', { method: 'POST' })
}

export function validerAtelier2(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier2', { method: 'POST' })
}

export function rouvrirAtelier2(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/rouvrir-atelier2', { method: 'POST' })
}

export function demarrerAtelier3(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier3', { method: 'POST' })
}

export function validerAtelier3(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier3', { method: 'POST' })
}

export function rouvrirAtelier3(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/rouvrir-atelier3', { method: 'POST' })
}

export function demarrerAtelier4(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier4', { method: 'POST' })
}

export function validerAtelier4(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier4', { method: 'POST' })
}

export function rouvrirAtelier4(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/rouvrir-atelier4', { method: 'POST' })
}

export function demarrerAtelier5(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier5', { method: 'POST' })
}

export function validerAtelier5(etudeId: string): Promise<{ etude: Etude; snapshotVersion: number }> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier5', { method: 'POST' })
}

export function rouvrirAtelier5(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/rouvrir-atelier5', { method: 'POST' })
}

export function listValeursMetier(etudeId: string): Promise<ValeurMetier[]> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier')
}

export function createValeurMetier(etudeId: string, description: string, entiteProprietaire: string): Promise<ValeurMetier> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier', {
    method: 'POST',
    body: JSON.stringify({ description: description, entiteProprietaire: entiteProprietaire }),
  })
}

export function updateValeurMetier(etudeId: string, id: string, description: string, entiteProprietaire: string): Promise<ValeurMetier> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + id, {
    method: 'PUT',
    body: JSON.stringify({ description: description, entiteProprietaire: entiteProprietaire }),
  })
}

export function deleteValeurMetier(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + id, { method: 'DELETE' })
}

export function listBiensSupport(etudeId: string): Promise<BienSupport[]> {
  return apiFetch('/etudes/' + etudeId + '/biens-support')
}

export function createBienSupport(etudeId: string, valeurMetierId: string, description: string, type: string, entiteProprietaire: string): Promise<BienSupport> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + valeurMetierId + '/biens-support', {
    method: 'POST',
    body: JSON.stringify({ description: description, type: type, entiteProprietaire: entiteProprietaire }),
  })
}

export function updateBienSupport(etudeId: string, id: string, description: string, type: string, entiteProprietaire: string): Promise<BienSupport> {
  return apiFetch('/etudes/' + etudeId + '/biens-support/' + id, {
    method: 'PUT',
    body: JSON.stringify({ description: description, type: type, entiteProprietaire: entiteProprietaire }),
  })
}

export function deleteBienSupport(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/biens-support/' + id, { method: 'DELETE' })
}

export function listEvenementsRedoutes(etudeId: string): Promise<EvenementRedoute[]> {
  return apiFetch('/etudes/' + etudeId + '/evenements-redoutes')
}

export function createEvenementRedoute(etudeId: string, valeurMetierId: string, description: string, gravite: number): Promise<EvenementRedoute> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + valeurMetierId + '/evenements-redoutes', {
    method: 'POST',
    body: JSON.stringify({ description: description, gravite: gravite }),
  })
}

export function updateEvenementRedoute(etudeId: string, erId: string, description: string, gravite: number): Promise<EvenementRedoute> {
  return apiFetch('/etudes/' + etudeId + '/evenements-redoutes/' + erId, {
    method: 'PUT',
    body: JSON.stringify({ description: description, gravite: gravite }),
  })
}

export function deleteEvenementRedoute(etudeId: string, erId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/evenements-redoutes/' + erId, { method: 'DELETE' })
}

export function getSocleSecurite(etudeId: string): Promise<SocleSecurite | null> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite')
}

export function createSocleSecurite(etudeId: string): Promise<SocleSecurite> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite', { method: 'POST' })
}

export function addReferentiel(etudeId: string, nom: string, etat: string, theme?: string, codeControle?: string, etatActuel?: string): Promise<SocleSecurite> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite/referentiels', {
    method: 'POST',
    body: JSON.stringify({ nom: nom, etat: etat, theme: theme || null, codeControle: codeControle || null, etatActuel: etatActuel || null }),
  })
}

export function updateReferentiel(etudeId: string, referentielId: string, nom: string, etat: string, theme?: string, codeControle?: string, etatActuel?: string): Promise<SocleSecurite> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite/referentiels/' + referentielId, {
    method: 'PUT',
    body: JSON.stringify({ nom: nom, etat: etat, theme: theme || null, codeControle: codeControle || null, etatActuel: etatActuel || null }),
  })
}

export function deleteReferentiel(etudeId: string, referentielId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite/referentiels/' + referentielId, { method: 'DELETE' })
}

export interface CoupleSourceRisqueObjectifVise {
  id: string
  etudeId: string
  sourceRisque: string
  descriptionSourceRisque: string
  objectifVise: string
  descriptionObjectifVise: string
  contexteVulnerabilite: string
  theme: string
  motivation: number
  ressources: number
  pertinence: string
  pertinenceCalculee: string
  pertinenceRetenue?: string | null
  justificationPertinence?: string | null
  creeLeUtc: string
}

export interface MesureEcosysteme {
  id: string
  description: string
  creeLeUtc: string
}

export interface PartiePrenante {
  id: string
  etudeId: string
  nom: string
  rolesEtAttentes: string
  representant: string
  categorie: string
  descriptionCategorie?: string | null
  dependance?: number | null
  penetration?: number | null
  maturiteCyber?: number | null
  confiance?: number | null
  niveauDangerositeCalcule?: number | null
  niveauDangerositeRetenu?: number | null
  justificationDangerosite?: string | null
  niveauDangerosite?: number | null
  zone?: string | null
  mesures: MesureEcosysteme[]
  dependanceResiduelle?: number | null
  penetrationResiduelle?: number | null
  maturiteCyberResiduelle?: number | null
  confianceResiduelle?: number | null
  niveauDangerositeResiduelCalcule?: number | null
  niveauDangerositeResiduelRetenu?: number | null
  justificationDangerositeResiduelle?: string | null
  niveauDangerositeResiduel?: number | null
  zoneResiduelle?: string | null
  creeLeUtc: string
}

export function listCouplesSrOv(etudeId: string): Promise<CoupleSourceRisqueObjectifVise[]> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov')
}

export function createCoupleSrOv(
  etudeId: string, sourceRisque: string, descriptionSourceRisque: string,
  objectifVise: string, descriptionObjectifVise: string, contexteVulnerabilite: string,
  theme: string, motivation: number, ressources: number
): Promise<CoupleSourceRisqueObjectifVise> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov', {
    method: 'POST',
    body: JSON.stringify({
      sourceRisque: sourceRisque, descriptionSourceRisque: descriptionSourceRisque,
      objectifVise: objectifVise, descriptionObjectifVise: descriptionObjectifVise,
      contexteVulnerabilite: contexteVulnerabilite, theme: theme,
      motivation: motivation, ressources: ressources,
    }),
  })
}

export function updateCoupleSrOv(
  etudeId: string, id: string, sourceRisque: string, descriptionSourceRisque: string,
  objectifVise: string, descriptionObjectifVise: string, contexteVulnerabilite: string,
  theme: string, motivation: number, ressources: number
): Promise<CoupleSourceRisqueObjectifVise> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov/' + id, {
    method: 'PUT',
    body: JSON.stringify({
      sourceRisque: sourceRisque, descriptionSourceRisque: descriptionSourceRisque,
      objectifVise: objectifVise, descriptionObjectifVise: descriptionObjectifVise,
      contexteVulnerabilite: contexteVulnerabilite, theme: theme,
      motivation: motivation, ressources: ressources,
    }),
  })
}

export function deleteCoupleSrOv(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov/' + id, { method: 'DELETE' })
}

export function definirPertinenceRetenue(etudeId: string, id: string, pertinenceRetenue: string, justification: string): Promise<CoupleSourceRisqueObjectifVise> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov/' + id + '/pertinence-retenue', {
    method: 'PUT',
    body: JSON.stringify({ pertinenceRetenue: pertinenceRetenue, justification: justification }),
  })
}

export function reinitialiserPertinence(etudeId: string, id: string): Promise<CoupleSourceRisqueObjectifVise> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov/' + id + '/pertinence-retenue', { method: 'DELETE' })
}

export function listPartiesPrenantes(etudeId: string): Promise<PartiePrenante[]> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes')
}

export function createPartiePrenante(
  etudeId: string, nom: string, rolesEtAttentes: string, representant: string, categorie: string, descriptionCategorie?: string,
): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes', {
    method: 'POST',
    body: JSON.stringify({ nom: nom, rolesEtAttentes: rolesEtAttentes, representant: representant, categorie: categorie, descriptionCategorie: descriptionCategorie || null }),
  })
}

export function updatePartiePrenante(
  etudeId: string, id: string, nom: string, rolesEtAttentes: string, representant: string, categorie: string, descriptionCategorie?: string,
): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id, {
    method: 'PUT',
    body: JSON.stringify({ nom: nom, rolesEtAttentes: rolesEtAttentes, representant: representant, categorie: categorie, descriptionCategorie: descriptionCategorie || null }),
  })
}

export function deletePartiePrenante(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id, { method: 'DELETE' })
}

export function evaluerDangerosite(
  etudeId: string, id: string, dependance: number, penetration: number, maturiteCyber: number, confiance: number,
): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite', {
    method: 'PUT',
    body: JSON.stringify({ dependance: dependance, penetration: penetration, maturiteCyber: maturiteCyber, confiance: confiance }),
  })
}

export function evaluerDangerositeResiduelle(
  etudeId: string, id: string, dependance: number, penetration: number, maturiteCyber: number, confiance: number,
): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite-residuelle', {
    method: 'PUT',
    body: JSON.stringify({ dependance: dependance, penetration: penetration, maturiteCyber: maturiteCyber, confiance: confiance }),
  })
}

export function definirDangerositeRetenue(etudeId: string, id: string, niveauRetenu: number, justification: string): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite-retenue', {
    method: 'PUT',
    body: JSON.stringify({ niveauRetenu: niveauRetenu, justification: justification }),
  })
}

export function reinitialiserDangerosite(etudeId: string, id: string): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite-retenue', { method: 'DELETE' })
}

export function definirDangerositeResidueleRetenue(etudeId: string, id: string, niveauRetenu: number, justification: string): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite-residuelle-retenue', {
    method: 'PUT',
    body: JSON.stringify({ niveauRetenu: niveauRetenu, justification: justification }),
  })
}

export function reinitialiserDangerositeResiduelle(etudeId: string, id: string): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + id + '/dangerosite-residuelle-retenue', { method: 'DELETE' })
}

export function ajouterMesureEcosysteme(etudeId: string, partiePrenanteId: string, description: string): Promise<PartiePrenante> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + partiePrenanteId + '/mesures', {
    method: 'POST',
    body: JSON.stringify({ description: description }),
  })
}

export function supprimerMesureEcosysteme(etudeId: string, partiePrenanteId: string, mesureId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/parties-prenantes/' + partiePrenanteId + '/mesures/' + mesureId, { method: 'DELETE' })
}

export interface ScenarioStrategique {
  id: string
  etudeId: string
  coupleSourceRisqueObjectifViseId: string
  evenementRedouteId: string
  description: string
  creeLeUtc: string
}

export function listScenariosStrategiques(etudeId: string): Promise<ScenarioStrategique[]> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-strategiques')
}

export function createScenarioStrategique(etudeId: string, coupleId: string, evenementRedouteId: string, description: string): Promise<ScenarioStrategique> {
  return apiFetch('/etudes/' + etudeId + '/couples-sr-ov/' + coupleId + '/scenario-strategique', {
    method: 'POST',
    body: JSON.stringify({ evenementRedouteId: evenementRedouteId, description: description }),
  })
}

export function updateScenarioStrategique(etudeId: string, id: string, evenementRedouteId: string, description: string): Promise<ScenarioStrategique> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-strategiques/' + id, {
    method: 'PUT',
    body: JSON.stringify({ evenementRedouteId: evenementRedouteId, description: description }),
  })
}

export function deleteScenarioStrategique(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-strategiques/' + id, { method: 'DELETE' })
}

export interface EvenementIntermediaire {
  id: string
  partiePrenanteId: string
  description: string
  ordre: number
}

export interface CheminAttaque {
  id: string
  etudeId: string
  scenarioStrategiqueId: string
  description: string
  evenementsIntermediaires: EvenementIntermediaire[]
  creeLeUtc: string
}

export function listCheminsAttaque(etudeId: string): Promise<CheminAttaque[]> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque')
}

export function createCheminAttaque(etudeId: string, scenarioId: string, description: string): Promise<CheminAttaque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-strategiques/' + scenarioId + '/chemins-attaque', {
    method: 'POST',
    body: JSON.stringify({ description: description }),
  })
}

export function updateCheminAttaque(etudeId: string, id: string, description: string): Promise<CheminAttaque> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + id, {
    method: 'PUT',
    body: JSON.stringify({ description: description }),
  })
}

export function deleteCheminAttaque(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + id, { method: 'DELETE' })
}

export function createEvenementIntermediaire(etudeId: string, cheminId: string, partiePrenanteId: string, description: string): Promise<CheminAttaque> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + cheminId + '/evenements-intermediaires', {
    method: 'POST',
    body: JSON.stringify({ partiePrenanteId: partiePrenanteId, description: description }),
  })
}

export function updateEvenementIntermediaire(etudeId: string, cheminId: string, eiId: string, description: string): Promise<CheminAttaque> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + cheminId + '/evenements-intermediaires/' + eiId, {
    method: 'PUT',
    body: JSON.stringify({ description: description }),
  })
}

export function deleteEvenementIntermediaire(etudeId: string, cheminId: string, eiId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + cheminId + '/evenements-intermediaires/' + eiId, { method: 'DELETE' })
}

export type PhaseActionElementaire = 'Connaitre' | 'Rentrer' | 'Trouver' | 'Exploiter'

export const PHASES_ACTION_ELEMENTAIRE: PhaseActionElementaire[] = ['Connaitre', 'Rentrer', 'Trouver', 'Exploiter']

export interface ActionElementaire {
  id: string
  description: string
  phase: PhaseActionElementaire
  bienSupportId: string
  techniqueMitre?: string | null
}

export interface ActionElementaireInput {
  description: string
  phase: PhaseActionElementaire
  bienSupportId: string
  techniqueMitre?: string | null
}

export interface TechniqueMitre {
  id: string
  nom: string
  tactique: string
  phaseEbios: PhaseActionElementaire
}

export function listerTechniquesMitre(phase?: string, q?: string): Promise<TechniqueMitre[]> {
  var params = new URLSearchParams()
  if (phase) params.set('phase', phase)
  if (q) params.set('q', q)
  var suffixe = params.toString() ? '?' + params.toString() : ''
  return apiFetch('/referentiels/mitre' + suffixe)
}

export interface ModeOperatoire {
  id: string
  description: string
  actionsElementaires: ActionElementaire[]
  probabiliteSucces: number
  difficulteTechnique: number
  vraisemblanceCalculee: string
  vraisemblanceRetenue?: string | null
  justificationVraisemblance?: string | null
  vraisemblance: string
}

export interface ScenarioOperationnel {
  id: string
  etudeId: string
  cheminAttaqueId: string
  modesOperatoires: ModeOperatoire[]
  vraisemblanceGlobale?: string | null
  creeLeUtc: string
}

export function listScenariosOperationnels(etudeId: string): Promise<ScenarioOperationnel[]> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels')
}

export function createScenarioOperationnel(etudeId: string, cheminAttaqueId: string): Promise<ScenarioOperationnel> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + cheminAttaqueId + '/scenario-operationnel', { method: 'POST' })
}

export function deleteScenarioOperationnel(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + id, { method: 'DELETE' })
}

export interface ModeOperatoireInput {
  description: string
  actions: ActionElementaireInput[]
  probabiliteSucces: number
  difficulteTechnique: number
}

export function ajouterModeOperatoire(etudeId: string, scenarioOperationnelId: string, mode: ModeOperatoireInput): Promise<ScenarioOperationnel> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + scenarioOperationnelId + '/modes-operatoires', {
    method: 'POST',
    body: JSON.stringify(mode),
  })
}

export function modifierModeOperatoire(etudeId: string, scenarioOperationnelId: string, modeId: string, mode: ModeOperatoireInput): Promise<ScenarioOperationnel> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + scenarioOperationnelId + '/modes-operatoires/' + modeId, {
    method: 'PUT',
    body: JSON.stringify(mode),
  })
}

export function supprimerModeOperatoire(etudeId: string, scenarioOperationnelId: string, modeId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + scenarioOperationnelId + '/modes-operatoires/' + modeId, { method: 'DELETE' })
}

export function definirVraisemblanceRetenue(etudeId: string, scenarioOperationnelId: string, modeId: string, vraisemblanceRetenue: string, justification: string): Promise<ScenarioOperationnel> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + scenarioOperationnelId + '/modes-operatoires/' + modeId + '/vraisemblance-retenue', {
    method: 'PUT',
    body: JSON.stringify({ vraisemblanceRetenue: vraisemblanceRetenue, justification: justification }),
  })
}

export function reinitialiserVraisemblance(etudeId: string, scenarioOperationnelId: string, modeId: string): Promise<ScenarioOperationnel> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-operationnels/' + scenarioOperationnelId + '/modes-operatoires/' + modeId + '/vraisemblance-retenue', { method: 'DELETE' })
}

// Les rapports sont des PDF proteges par jeton : un <a href> classique ne
// porte pas l'en-tete Authorization (navigation navigateur, pas fetch), donc
// on telecharge via fetch (jeton injecte comme apiFetch) puis on declenche
// le telechargement via un blob local plutot qu'une simple URL.
export async function telechargerRapport(path: string, nomFichier: string): Promise<void> {
  var headers: Record<string, string> = {}
  var token = obtenirToken()
  if (token) {
    headers['Authorization'] = 'Bearer ' + token
  }

  var response = await fetch(API_BASE + path, { headers })

  if (response.status === 401) {
    effacerToken()
  }

  if (!response.ok) {
    var message = 'Erreur lors du telechargement (' + response.status + ')'
    try {
      var body = JSON.parse(await response.text())
      if (body && body.error) message = body.error
    } catch (e) { /* corps non JSON, on garde le message generique */ }
    throw new ApiError(response.status, message)
  }

  var blob = await response.blob()
  var url = URL.createObjectURL(blob)
  var lien = document.createElement('a')
  lien.href = url
  lien.download = nomFichier
  document.body.appendChild(lien)
  lien.click()
  document.body.removeChild(lien)
  URL.revokeObjectURL(url)
}

export interface ScenarioDeRisque {
  id: string
  cheminAttaqueId: string
  libelleChemin: string
  libelleCouple: string
  gravite: number
  vraisemblanceInitiale?: string | null
  niveauRisqueInitialCalcule?: string | null
  niveauRisqueInitialRetenu?: string | null
  justificationNiveauRisqueInitial?: string | null
  niveauRisqueInitial?: string | null
  classeAcceptationInitiale?: string | null
  graviteResiduelle?: number | null
  vraisemblanceResiduelle?: string | null
  niveauRisqueResiduelCalcule?: string | null
  niveauRisqueResiduelRetenu?: string | null
  justificationNiveauRisqueResiduel?: string | null
  niveauRisqueResiduel?: string | null
  classeAcceptationResiduelle?: string | null
  accepteParDirection: boolean
  nomProprietaireRisque?: string | null
  nomValidateurSecurite?: string | null
  nomSponsorExecutif?: string | null
  justificationAcceptation?: string | null
  dateAcceptationUtc?: string | null
}

export function listScenariosDeRisque(etudeId: string): Promise<ScenarioDeRisque[]> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque')
}

export function creerScenarioDeRisque(etudeId: string, cheminAttaqueId: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/chemins-attaque/' + cheminAttaqueId + '/scenario-de-risque', { method: 'POST' })
}

export function supprimerScenarioDeRisque(etudeId: string, id: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id, { method: 'DELETE' })
}

export function definirNiveauRisqueInitialRetenue(etudeId: string, id: string, niveauRetenu: string, justification: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/niveau-risque-initial-retenue', {
    method: 'PUT',
    body: JSON.stringify({ niveauRetenu: niveauRetenu, justification: justification }),
  })
}

export function reinitialiserNiveauRisqueInitial(etudeId: string, id: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/niveau-risque-initial-retenue', { method: 'DELETE' })
}

export function evaluerRisqueResiduel(etudeId: string, id: string, graviteResiduelle: number, vraisemblanceResiduelle: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/risque-residuel', {
    method: 'PUT',
    body: JSON.stringify({ graviteResiduelle: graviteResiduelle, vraisemblanceResiduelle: vraisemblanceResiduelle }),
  })
}

export function definirNiveauRisqueResiduelRetenue(etudeId: string, id: string, niveauRetenu: string, justification: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/niveau-risque-residuel-retenue', {
    method: 'PUT',
    body: JSON.stringify({ niveauRetenu: niveauRetenu, justification: justification }),
  })
}

export function reinitialiserNiveauRisqueResiduel(etudeId: string, id: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/niveau-risque-residuel-retenue', { method: 'DELETE' })
}

export function accepterRisqueResiduel(
  etudeId: string, id: string, nomProprietaireRisque: string, nomValidateurSecurite: string, nomSponsorExecutif?: string, justification?: string,
): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/acceptation', {
    method: 'POST',
    body: JSON.stringify({
      nomProprietaireRisque: nomProprietaireRisque, nomValidateurSecurite: nomValidateurSecurite,
      nomSponsorExecutif: nomSponsorExecutif || null, justification: justification || null,
    }),
  })
}

export function retirerAcceptation(etudeId: string, id: string): Promise<ScenarioDeRisque> {
  return apiFetch('/etudes/' + etudeId + '/scenarios-de-risque/' + id + '/acceptation', { method: 'DELETE' })
}

export interface MesureTraitementRisque {
  id: string
  description: string
  axe: string
  scenariosDeRisqueIds: string[]
  responsable: string
  freinsEtDifficultes?: string | null
  coutComplexite: string
  echeance?: string | null
  statut: string
  creeLeUtc: string
}

export interface PlanTraitementRisque {
  id: string
  etudeId: string
  mesures: MesureTraitementRisque[]
}

export interface MesureTraitementRisqueInput {
  description: string
  axe: string
  scenariosDeRisqueIds: string[]
  responsable: string
  freinsEtDifficultes?: string | null
  coutComplexite: string
  echeance?: string | null
  statut: string
}

export function getPlanTraitementRisque(etudeId: string): Promise<PlanTraitementRisque | null> {
  return apiFetch('/etudes/' + etudeId + '/plan-traitement-risque')
}

export function creerPlanTraitementRisque(etudeId: string): Promise<PlanTraitementRisque> {
  return apiFetch('/etudes/' + etudeId + '/plan-traitement-risque', { method: 'POST' })
}

export function ajouterMesureTraitementRisque(etudeId: string, mesure: MesureTraitementRisqueInput): Promise<PlanTraitementRisque> {
  return apiFetch('/etudes/' + etudeId + '/plan-traitement-risque/mesures', {
    method: 'POST',
    body: JSON.stringify(mesure),
  })
}

export function modifierMesureTraitementRisque(etudeId: string, mesureId: string, mesure: MesureTraitementRisqueInput): Promise<PlanTraitementRisque> {
  return apiFetch('/etudes/' + etudeId + '/plan-traitement-risque/mesures/' + mesureId, {
    method: 'PUT',
    body: JSON.stringify(mesure),
  })
}

export function supprimerMesureTraitementRisque(etudeId: string, mesureId: string): Promise<void> {
  return apiFetch('/etudes/' + etudeId + '/plan-traitement-risque/mesures/' + mesureId, { method: 'DELETE' })
}
