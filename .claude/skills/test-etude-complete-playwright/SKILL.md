---
name: test-etude-complete-playwright
description: Fait passer une etude complete par les 5 ateliers EBIOS RM via de vraies interactions navigateur (Playwright), pour verifier que le frontend fonctionne reellement de bout en bout -- pas seulement que le code compile. A utiliser apres une modification du frontend ou du backend touchant un formulaire, un workflow d'atelier, ou un calcul.
---

## Pourquoi passer par le navigateur et pas juste par l'API

Plusieurs bugs de ce projet (select qui ne se pre-selectionne jamais, texte qui se concatene avec une suggestion, section rendue au mauvais endroit) n'etaient visibles qu'en interaction navigateur reelle -- l'API repondait 200 dans tous les cas, `dotnet build`/`npx tsc` passaient, et pourtant l'ecran etait casse. Ne jamais declarer un correctif frontend termine sans ce genre de verification.

## Demarrer l'environnement

- **Backend** : `ASPNETCORE_ENVIRONMENT=Development` doit etre positionne (sinon la connection string n'est pas chargee -- erreur "ConnectionString property has not been initialized"). Lancer en arriere-plan avec `setsid` pour survivre a la fin de la commande shell :
  ```
  cd src/EbiosRM.Api && ASPNETCORE_ENVIRONMENT=Development setsid nohup ./bin/Debug/net8.0/EbiosRM.Api --urls http://localhost:5197 > /tmp/backend.log 2>&1 < /dev/null &
  disown
  ```
  Verifier avec `curl -s http://localhost:5197/api/v1/etudes`. Apres une modification du code C#, il faut `dotnet build` puis **tuer et relancer** ce process (pas de hot-reload).
- **Frontend** : `npm run dev -- --port 5174` depuis `frontend/`, ou verifier qu'un process vite existant tourne deja (`ss -ltnp | grep 5174`) avant d'en relancer un -- deux instances sur le meme port causent des fuites de process (vite bascule sur 5175 sans prevenir, source de confusion). Le frontend n'a pas besoin de rebuild manuel pour du dev (HMR), mais `npm run build` reste la verification finale a faire avant de conclure.
- **Base de donnees** : Postgres sur le port **5433** (pas 5432), identifiants dans `src/EbiosRM.Api/appsettings.Development.json`. Tables dans le schema `core_engine`, pas `public` -- `psql -h localhost -p 5433 -U ebiosrm -d ebiosrm -c '\dt core_engine.*'`.

## Installer Playwright (une fois par machine/environnement)

```
npm install playwright --no-save   # depuis un dossier HORS du repo (ex. le scratchpad) pour ne pas toucher package.json
npx playwright install chromium    # --with-deps echoue sans sudo, l'omettre -- le binaire seul suffit
```

## Utiliser le script gabarit

`etude-complete.mjs` (a cote de ce fichier) fait passer une etude nommee par la ligne de commande a travers les 5 ateliers, avec des verifications a chaque etape (presence des boutons/sections attendues, HTTP >=400 remontes, exceptions JS remontees). Sortie non-zero si des echecs sont enregistres.

```
node etude-complete.mjs "Nom de l etude" /chemin/vers/dossier/screenshots
```

Sans 2e argument, pas de captures d'ecran (juste les verifications texte). Prendre les captures quand le but est une revue visuelle, les omettre pour un test de non-regression rapide.

**Toujours relire le script avant de le relancer tel quel** si des selecteurs de l'UI ont change depuis (labels de bouton, placeholders) -- il n'est pas garanti a vie, c'est un point de depart a jour au moment ou il a ete ecrit.

## Pieges connus (deja rencontres, evitent de perdre du temps a les rediagnostiquer)

- `waitUntil: 'networkidle'` ne se resout quasiment jamais avec le serveur de dev Vite (le websocket HMR maintient une activite reseau permanente) -- toujours `waitUntil: 'load'` + un `waitForTimeout` explicite.
- `getByRole('button', { name: 'Ajouter' })` sans `exact: true` declenche une "strict mode violation" : plusieurs boutons contiennent "Ajouter" en sous-chaine ("Ajouter un bien support", etc).
- Le formulaire de creation d'etude (Nom/Mission/Perimetre) n'a **aucun placeholder** -- cibler les inputs par position (`input[type=text]` nth 0/1/2), pas par `getByPlaceholder`.
- Un couple SR/OV cree avec les valeurs par defaut du formulaire (motivation=2, ressources=2) calcule "Moyennement pertinent" -- **pas retenu**, donc aucun scenario stratégique ne pourra etre cree pour lui en Atelier 3. Toujours forcer motivation/ressources a 4/4 dans un test si le but est de descendre jusqu'a l'Atelier 5.
- Les parties prenantes et leur evaluation de dangerosite se creent en **Atelier 3**, pas Atelier 2 (voir le skill `verification-methodologie-ebios`).
- Pas d'endpoint `DELETE /api/v1/etudes/{id}` -- pour nettoyer les etudes de test creees pendant une session, `TRUNCATE` directement les tables `core_engine.*` via `psql` (lister les etudes avec `select "Id","Nom" from core_engine.etudes;` avant, pour ne pas supprimer une etude que l'utilisateur veut garder -- toujours demander si un doute).
