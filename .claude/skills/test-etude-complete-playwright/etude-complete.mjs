// Script gabarit : cree une etude et la fait passer par les 5 ateliers via
// de vraies interactions navigateur (clics, saisies), pas des raccourcis API.
// Usage : node etude-complete.mjs "Nom de l etude" [dossier_screenshots]
//
// Pre-requis : le frontend dev tourne sur localhost:5174 et le backend sur
// localhost:5197 (voir SKILL.md pour comment les demarrer). Playwright et
// chromium doivent etre installes -- si absent :
//   npm install playwright --no-save   (depuis un dossier hors du repo, ex. le scratchpad)
//   npx playwright install chromium
import { chromium } from 'playwright'

var NOM_ETUDE = process.argv[2]
var BASE = 'http://localhost:5174'
var SS_DIR = process.argv[3] || '.'
if (!NOM_ETUDE) { console.error('Usage: node etude-complete.mjs "Nom de l etude" [dossier_screenshots]'); process.exit(1) }
var slug = NOM_ETUDE.replace(/[^a-z0-9]+/gi, '-').toLowerCase()

var erreurs = []
function log(msg) { console.log('[' + NOM_ETUDE + '] ' + msg) }
function fail(msg) { erreurs.push(msg); console.log('[' + NOM_ETUDE + '] !!! ECHEC: ' + msg) }

var browser = await chromium.launch()
var page = await browser.newPage({ viewport: { width: 1280, height: 1000 } })
page.on('pageerror', function (e) { fail('pageerror: ' + e.message) })
page.on('response', async function (res) {
  if (res.request().method() !== 'GET' && res.status() >= 400) {
    var body = ''
    try { body = await res.text() } catch (e) {}
    fail('HTTP ' + res.status() + ' ' + res.request().method() + ' ' + res.url() + ' -> ' + body)
  }
})

// IMPORTANT : 'networkidle' ne se resout quasiment jamais avec le serveur de
// dev Vite (le websocket HMR maintient une activite reseau permanente) --
// toujours utiliser 'load' + un waitForTimeout explicite a la place.
async function aller(chemin) {
  await page.goto(BASE + chemin, { waitUntil: 'load' })
  await page.waitForTimeout(700)
}
async function clicAjout(texte) {
  await page.getByText(texte, { exact: false }).first().click()
  await page.waitForTimeout(250)
}
// exact:true est indispensable pour "Ajouter" seul : sans ca, Playwright
// remonte une erreur "strict mode violation" car plusieurs boutons
// contiennent "Ajouter" en sous-chaine ("Ajouter un bien support", etc.)
async function clicBouton(texte, exact) {
  await page.getByRole('button', { name: texte, exact: exact !== false }).first().click()
  await page.waitForTimeout(500)
}

// ============ CREATION ETUDE ============
await aller('/etudes')
await clicAjout('Nouvelle etude')
await page.waitForTimeout(300)
// Les 3 champs (Nom, Mission, Perimetre) n'ont PAS de placeholder -- juste un
// <label> flottant sans attribut for. Il faut les cibler par position.
var champsCreation = page.locator('input[type=text]')
await champsCreation.nth(0).fill(NOM_ETUDE)
await champsCreation.nth(1).fill('Proteger les actifs numeriques et la continuite d activite')
await champsCreation.nth(2).fill('Systeme d information et donnees clients de ' + NOM_ETUDE)
await clicBouton('Creer l etude', false)
await page.waitForTimeout(800)
var match = page.url().match(/etudes\/([a-f0-9-]+)/)
if (!match) { fail('etude non creee, url=' + page.url()); await browser.close(); console.log(JSON.stringify({ erreurs })); process.exit(1) }
var etudeId = match[1]
log('etude creee: ' + etudeId)

// ============ ATELIER 1 : Cadrage ============
await aller('/etudes/' + etudeId + '/ateliers/1')
await clicBouton('Demarrer l atelier', false)

await clicAjout('Ajouter une valeur metier')
await page.getByPlaceholder('Description').last().fill('Base de donnees clients et dossiers de ' + NOM_ETUDE)
await page.getByPlaceholder('Entite proprietaire').last().fill('Direction commerciale')
await clicBouton('Ajouter', true)
await page.waitForTimeout(500)
if (await page.getByText('Base de donnees clients et dossiers de ' + NOM_ETUDE).count() === 0) fail('valeur metier non listee')

await clicAjout('Ajouter un bien support')
await page.getByPlaceholder('Description').last().fill('Serveur applicatif principal')
await page.getByPlaceholder('Entite proprietaire').last().fill('DSI - Exploitation')
await clicBouton('Ajouter', true)
await page.waitForTimeout(500)
if (await page.getByText('Serveur applicatif principal').count() === 0) fail('bien support non liste')

await clicAjout('Ajouter un evenement redoute')
await page.getByPlaceholder('Description').last().fill('Fuite de donnees personnelles vers l exterieur')
await clicBouton('Ajouter', true)
await page.waitForTimeout(500)
if (await page.getByText('Fuite de donnees personnelles vers l exterieur').count() === 0) fail('evenement redoute non liste')

var creerSocle = page.getByText('Creer le socle', { exact: false })
if (await creerSocle.count() > 0) { await creerSocle.click(); await page.waitForTimeout(500) }
var ajoutRef = page.getByText('Ajouter un controle', { exact: false })
if (await ajoutRef.count() > 0) {
  await ajoutRef.first().click()
  await page.waitForTimeout(300)
  var radioLibre = page.locator('label', { hasText: 'Autre referentiel' })
  if (await radioLibre.count() > 0) await radioLibre.click()
  var nomLibreField = page.getByPlaceholder(/Nom du referentiel/)
  if (await nomLibreField.count() > 0) await nomLibreField.fill('PSSI interne')
  var etatActuelField = page.getByPlaceholder(/Etat actuel observe/)
  if (await etatActuelField.count() > 0) await etatActuelField.fill('Supports amovibles non chiffres, controle d acces insuffisant')
  await clicBouton('Ajouter', true)
  await page.waitForTimeout(500)
} else fail('bouton ajouter controle socle introuvable')

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-atelier1.png', fullPage: true })
await clicBouton('Valider l atelier', false)
await page.waitForTimeout(700)
log('atelier 1 tente valide')

// ============ ATELIER 2 : Sources de risque ============
await aller('/etudes/' + etudeId + '/ateliers/2')
await clicBouton('Demarrer l atelier', false)

await clicAjout('Ajouter un couple')
await page.getByPlaceholder(/Description de la source de risque/).fill('Groupe de cybercriminalite specialise dans la revente de donnees')
await page.getByPlaceholder(/Description de l objectif vise/).fill('Revendre les donnees volees sur le marche noir')
await page.getByPlaceholder('Contexte / vulnerabilite associee').fill('Absence de chiffrement des sauvegardes chez le prestataire cloud')
// Forcer motivation/ressources a 4/4 (les 2 derniers select du formulaire) :
// le defaut du formulaire est 2/2, qui calcule "Moyennement pertinent" --
// pas retenu, donc aucun scenario stratégique ne pourrait etre cree en Atelier 3.
var selects = page.locator('select')
var nbSelects = await selects.count()
await selects.nth(nbSelects - 2).selectOption('4')
await selects.nth(nbSelects - 1).selectOption('4')
await clicBouton('Ajouter', true)
await page.waitForTimeout(600)
if (await page.getByText('COUPLES SOURCE DE RISQUE', { exact: false }).count() === 0) fail('section couples introuvable apres ajout')

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-atelier2.png', fullPage: true })
await clicBouton('Valider l atelier', false)
await page.waitForTimeout(700)
log('atelier 2 tente valide')

// ============ ATELIER 3 : Scenarios strategiques ============
// Parties prenantes + leur evaluation vivent ICI (pas en Atelier 2) --
// voir le skill verification-methodologie-ebios si ce point est remis en doute.
await aller('/etudes/' + etudeId + '/ateliers/3')
await clicBouton('Demarrer l atelier', false)

await clicAjout('Ajouter une partie prenante')
await page.getByPlaceholder('Nom').last().fill('Prestataire hebergement cloud')
await page.getByPlaceholder('Roles et attentes').last().fill('Hebergement infrastructure production')
await page.getByPlaceholder('Representant').last().fill('CloudSecure SA')
await clicBouton('Ajouter', true)
await page.waitForTimeout(600)
if (await page.getByText('Prestataire hebergement cloud').count() === 0) fail('partie prenante non listee')

// Le formulaire d'evaluation (Dependance/Penetration/Maturite/Confiance)
// s'affiche automatiquement pour toute partie jamais evaluee -- pas besoin
// de cliquer un lien "Evaluer" au prealable.
if (await page.getByText('EVALUER LA DANGEROSITE', { exact: false }).count() === 0) fail('formulaire evaluation dangerosite non affiche automatiquement')
var selectsEval = page.locator('select')
var nEval = await selectsEval.count()
await selectsEval.nth(nEval - 4).selectOption('4') // Dependance
await selectsEval.nth(nEval - 3).selectOption('4') // Penetration
await clicBouton('Enregistrer l evaluation', false)
await page.waitForTimeout(600)
if (await page.getByText('Zone de danger', { exact: false }).count() === 0) fail('zone de danger non affichee apres evaluation')

if (await page.getByText('+ Ajouter une mesure', { exact: false }).count() === 0) fail('bouton ajouter mesure absent pour partie critique')
await clicAjout('+ Ajouter une mesure')
await page.getByPlaceholder(/Description de la mesure/).fill('Exiger un plan de reversibilite et un second hebergeur qualifie')
await clicBouton('Ajouter', true)
await page.waitForTimeout(600)

var creerScenarioBtn = page.getByText('Creer un scenario', { exact: false })
if (await creerScenarioBtn.count() === 0) fail('bouton creer un scenario strategique absent (couple non retenu ?)')
else {
  await creerScenarioBtn.first().click()
  await page.waitForTimeout(300)
  await page.locator('textarea').first().fill('Le groupe cybercriminel exfiltre la base de donnees pour la revendre')
  await clicBouton('Creer le scenario', false)
  await page.waitForTimeout(700)
  if (await page.getByText('SCENARIOS STRATEGIQUES (1)', { exact: false }).count() === 0) fail('scenario strategique non cree')
}

var ajoutCheminBtn = page.getByText('+ Ajouter un chemin d attaque', { exact: false })
if (await ajoutCheminBtn.count() === 0) fail('bouton ajouter chemin d attaque absent')
else {
  await ajoutCheminBtn.first().click()
  await page.waitForTimeout(300)
  await page.getByPlaceholder(/Description du chemin/).fill('Compromission du prestataire cloud puis rebond vers le SI interne')
  await clicBouton('Creer le chemin', false)
  await page.waitForTimeout(700)
  if (await page.getByText('CHEMINS D ATTAQUE (1)', { exact: false }).count() === 0) fail('chemin d attaque non cree')
}

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-atelier3.png', fullPage: true })
await clicBouton('Valider l atelier', false)
await page.waitForTimeout(700)
log('atelier 3 tente valide')

// ============ ATELIER 4 : Scenarios operationnels ============
await aller('/etudes/' + etudeId + '/ateliers/4')
var demarrer4 = page.getByText('Demarrer l atelier', { exact: false })
if (await demarrer4.count() > 0) { await demarrer4.click(); await page.waitForTimeout(500) }

var creerOpBtn = page.getByText('Creer le scenario operationnel', { exact: false })
if (await creerOpBtn.count() === 0) fail('bouton creer scenario operationnel absent')
else { await creerOpBtn.first().click(); await page.waitForTimeout(600) }

var ajoutModeBtn = page.getByText('Ajouter un mode operatoire', { exact: false })
if (await ajoutModeBtn.count() === 0) fail('bouton ajouter mode operatoire absent')
else {
  await ajoutModeBtn.first().click()
  await page.waitForTimeout(300)
  await page.getByPlaceholder('Description du mode operatoire').fill('Phishing cible suivi d un mouvement lateral')
  await page.getByPlaceholder('Description de l action').fill('Reconnaissance OSINT des employes et prestataires')
  await clicBouton('Ajouter', true)
  await page.waitForTimeout(700)
  if (await page.getByText('MODE(S) OPERATOIRE(S)', { exact: false }).count() === 0) fail('mode operatoire non cree')
}

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-atelier4.png', fullPage: true })
await clicBouton('Valider l atelier', false)
await page.waitForTimeout(700)
log('atelier 4 tente valide')

// ============ ATELIER 5 : Traitement du risque ============
await aller('/etudes/' + etudeId + '/ateliers/5')
var demarrer5 = page.getByText('Demarrer l atelier', { exact: false })
if (await demarrer5.count() > 0) { await demarrer5.click(); await page.waitForTimeout(500) }

var materialiserBtn = page.getByText('Materialiser le scenario de risque', { exact: false })
if (await materialiserBtn.count() === 0) fail('bouton materialiser scenario de risque absent')
else { await materialiserBtn.first().click(); await page.waitForTimeout(700) }

if (await page.getByText('NIVEAU INITIAL', { exact: false }).count() === 0) fail('niveau initial du risque non affiche')

var evaluerResiduelBtn = page.getByText('Evaluer le risque residuel', { exact: false })
if (await evaluerResiduelBtn.count() === 0) fail('bouton evaluer risque residuel absent')
else {
  await evaluerResiduelBtn.first().click()
  await page.waitForTimeout(700)
  if (await page.getByText('RISQUE RESIDUEL', { exact: false }).count() === 0) fail('niveau residuel non affiche')
}

var creerPlanBtn = page.getByText('Creer le plan de traitement', { exact: false })
if (await creerPlanBtn.count() === 0) fail('bouton creer plan de traitement absent')
else { await creerPlanBtn.first().click(); await page.waitForTimeout(600) }

var ajoutMesureTraitBtn = page.getByText('Ajouter une mesure de traitement', { exact: false })
if (await ajoutMesureTraitBtn.count() === 0) fail('bouton ajouter mesure de traitement absent')
else {
  await ajoutMesureTraitBtn.first().click()
  await page.waitForTimeout(300)
  await page.getByPlaceholder('Description de la mesure').fill('Chiffrement systematique des sauvegardes et rotation des cles d acces')
  await page.getByPlaceholder('Responsable').fill('RSSI')
  var checkbox = page.locator('input[type=checkbox]').first()
  if (await checkbox.count() > 0) await checkbox.check()
  await clicBouton('Ajouter', true)
  await page.waitForTimeout(700)
}

var proprietaireField = page.getByPlaceholder('Proprietaire du risque')
if (await proprietaireField.count() > 0) {
  await proprietaireField.fill('Directeur des systemes d information')
  await page.getByPlaceholder('Validateur securite').fill('RSSI')
  await clicBouton('Accepter formellement', false)
  await page.waitForTimeout(700)
} else fail('formulaire acceptation formelle absent')

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-atelier5.png', fullPage: true })
await clicBouton('Valider l atelier', false)
await page.waitForTimeout(700)
log('atelier 5 tente valide')

if (SS_DIR !== '.') await page.screenshot({ path: SS_DIR + '/' + slug + '-final.png', fullPage: true })

await browser.close()
console.log('[' + NOM_ETUDE + '] etudeId=' + etudeId)
console.log('[' + NOM_ETUDE + '] TOTAL ERREURS: ' + erreurs.length)
erreurs.forEach(function (e) { console.log('  - ' + e) })
process.exit(erreurs.length > 0 ? 1 : 0)
