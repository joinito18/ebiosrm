# EBIOS RM

Application web pour conduire une analyse de risque selon la méthode **EBIOS Risk
Manager** de l'ANSSI, de l'atelier 1 (cadrage) à l'atelier 5 (traitement du
risque), avec génération des rapports PDF.

- **Backend** : ASP.NET Core 8 (API minimale), Entity Framework Core, PostgreSQL ou SQLite.
- **Frontend** : React 19 + Vite + Tailwind CSS.
- **Authentification** : compte email / mot de passe, jeton JWT.

Trois façons de l'installer, de la plus simple à la plus flexible :

| Mode | Pour qui | Base de données |
|---|---|---|
| **Application de bureau** | un poste, un ou quelques utilisateurs | SQLite (fichier local) |
| **Docker autonome** | une organisation qui héberge chez elle | PostgreSQL en conteneur |
| **Déploiement web** | accès public / multi-postes | PostgreSQL managé |

---

## Application de bureau (Windows, macOS, Linux)

L'installation la plus simple : **un seul fichier**, aucun outil à installer
(ni .NET, ni Node, ni Docker), base de données incluse.

### Utilisateur final

- **Windows** : télécharger `EbiosRM-Setup.exe` depuis la
  [page des releases](https://github.com/joinito18/ebiosrm/releases), double-cliquer,
  suivre l'assistant. Un raccourci « EBIOS RM » est créé dans le menu Démarrer.
- **Ubuntu / Linux** : télécharger `EbiosRM-<version>-linux.tar.gz`, puis :

  ```bash
  tar xzf EbiosRM-*-linux.tar.gz
  cd EbiosRM-*            # ou le dossier extrait
  ./installer.sh          # ajoute "EBIOS RM" au menu des applications, sans sudo
  ```

  Ensuite, lancer **EBIOS RM** depuis la grille des applications (ou la commande
  `ebiosrm` en terminal). Sans installation : `./EbiosRM` directement dans le
  dossier extrait. Désinstaller : `./installer.sh --desinstaller`.
- **macOS** : télécharger l'archive correspondante, l'extraire, lancer
  l'exécutable `EbiosRM` (clic droit → Ouvrir la première fois, binaire non signé).

Au lancement, l'application démarre puis **ouvre le navigateur** sur
`http://localhost:5000`. Au tout premier démarrage, une **étude d'exemple
complète** (« Atlas Assurances Santé », 15 valeurs métier, les 5 ateliers
validés) est présente en lecture seule pour découvrir l'outil — créer un
compte pour commencer sa propre étude. `App:ChargerExemple=false` pour
démarrer sur une base vierge.

Les données sont stockées localement :

| Système | Emplacement |
|---|---|
| Windows | `%LOCALAPPDATA%\EbiosRM\ebiosrm.db` |
| macOS | `~/Library/Application Support/EbiosRM/ebiosrm.db` *(via `$XDG_DATA_HOME` sinon)* |
| Linux | `~/.local/share/EbiosRM/ebiosrm.db` |

C'est un fichier SQLite unique : pour sauvegarder ou transférer, il suffit de le copier.
La désinstallation ne supprime pas ces données.

### Construire les binaires soi-même

Prérequis : .NET SDK 8 + Node.js 20.

```bash
# Windows (PowerShell)
./build/build-desktop.ps1 -Rid win-x64
# macOS / Linux
./build/build-desktop.sh linux-x64        # ou osx-arm64, osx-x64
```

Résultat dans `build/output/<rid>/`. Pour fabriquer l'installeur Windows,
compiler ensuite `installer/ebiosrm.iss` avec [Inno Setup](https://jrsoftware.org/isinfo.php).
Le workflow `.github/workflows/release.yml` fait tout cela automatiquement sur
un tag `v*`.

L'étude d'exemple embarquée est le fichier `src/EbiosRM.Api/ressources/ebiosrm.seed.db`
(versionné). Pour la régénérer après une évolution du modèle ou de l'étude :
`bash build/seed/generer-seed.sh` (nécessite la base PostgreSQL de dev + son
étude « Atlas Assurances Santé », et `python3` + `psycopg2`).

---

## Installation complète autonome (Docker)

Tout tourne en conteneurs — frontend, backend et base de données. Aucune
dépendance externe, aucune donnée en ligne. Convient à un poste ou un serveur
Windows, macOS ou Linux.

### Prérequis

- **Windows / macOS** : [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Linux** : Docker Engine + plugin Compose (`docker compose version` doit répondre)

### Démarrage

```bash
git clone https://github.com/joinito18/ebiosrm.git
cd ebiosrm

cp .env.example .env
# Ouvrir .env et renseigner JWT_SECRET (chaîne aléatoire longue) :
#   openssl rand -base64 48        (Linux/macOS)
#   [Convert]::ToBase64String((1..48|%{Get-Random -Max 256}))   (PowerShell)

docker compose -f docker-compose.selfhost.yml up -d --build
```

Au premier lancement, le conteneur API crée le schéma de la base
automatiquement. Compter 1 à 2 minutes.

Application disponible sur **http://localhost:8080**
→ créer un compte sur la page « Créer un compte ».

### Exploitation

| Action | Commande |
|---|---|
| État des conteneurs | `docker compose -f docker-compose.selfhost.yml ps` |
| Journaux de l'API | `docker compose -f docker-compose.selfhost.yml logs -f api` |
| Arrêter | `docker compose -f docker-compose.selfhost.yml stop` |
| Arrêter et supprimer les conteneurs | `docker compose -f docker-compose.selfhost.yml down` |
| Mettre à jour (nouvelle version du code) | `git pull && docker compose -f docker-compose.selfhost.yml up -d --build` |
| **Tout effacer, y compris les données** | `docker compose -f docker-compose.selfhost.yml down -v` |

Les données de la base survivent aux `stop` / `down` / `up` (volume Docker
`ebiosrm_pgdata`). Seul `down -v` les détruit.

### Configuration (`.env`)

| Variable | Rôle | Défaut |
|---|---|---|
| `JWT_SECRET` | **Obligatoire.** Secret de signature des jetons de session. | — |
| `POSTGRES_PASSWORD` | Mot de passe de la base. | `ebiosrm_dev` |
| `WEB_PORT` | Port d'écoute du site sur la machine hôte. | `8080` |
| `APP_URL` | URL publique du site (liens des emails de réinitialisation). | `http://localhost:8080` |

### Accès depuis d'autres postes du réseau

Le site écoute sur toutes les interfaces de la machine hôte. Depuis un autre
poste : `http://<ip-de-la-machine>:8080`. Penser à mettre `APP_URL` à jour et à
autoriser le port dans le pare-feu.

### Sauvegarde / restauration de la base

```bash
# Sauvegarde
docker exec ebiosrm-postgres pg_dump -U ebiosrm ebiosrm > sauvegarde.sql

# Restauration (base vide)
docker exec -i ebiosrm-postgres psql -U ebiosrm -d ebiosrm < sauvegarde.sql
```

---

## Environnement de développement

Ici, seul PostgreSQL tourne en conteneur ; l'API et le frontend s'exécutent
directement sur la machine pour bénéficier du rechargement à chaud.

### Prérequis

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- Docker (pour la base uniquement)

### Mise en route

```bash
# 1. Base de données
docker compose up -d                 # démarre uniquement Postgres (port 5433)
./scripts/setup-test-db.sh           # crée la base ebiosrm_test (pour les tests)

# 2. Backend  (terminal 1)
cd src/EbiosRM.Api
dotnet ef database update            # applique les migrations sur la base de dev
dotnet run                           # API sur http://localhost:5197

# 3. Frontend (terminal 2)
cd frontend
npm install
npm run dev                          # site sur http://localhost:5173
```

Le frontend en dev cible `http://localhost:5197/api/v1` par défaut
(`frontend/src/lib/api.ts`).

### Tests

```bash
dotnet test                          # backend : unitaires + intégration (base ebiosrm_test requise)
cd frontend && npm run test          # frontend : Vitest
```

---

## Déploiement hébergé (référence)

L'instance publique tourne sur **Neon** (PostgreSQL) + **Render** (API, image
Docker) + **Vercel** (frontend statique). Le workflow `.github/workflows/ci.yml`
construit, teste, applique les migrations en production puis redéploie à chaque
push sur `master`. Détails dans `PROJECT_CONTEXT.md`.

Pour ce mode, le frontend lit l'URL de l'API dans `frontend/.env.production`
(`VITE_API_BASE`). En autohébergement Docker cette variable vaut `/api/v1`
(relatif) : nginx sert le site et relaie `/api/` vers le conteneur backend.
