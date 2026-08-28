# PROJECT_CONTEXT.md
## Plateforme professionnelle EBIOS Risk Manager — Mémoire technique permanente

> **Instruction pour toute IA reprenant ce projet** : ce document résume l'intégralité des décisions actées jusqu'à présent (Phases 1, 1.5 et 2). Il doit être lu intégralement avant de produire quoi que ce soit. Ne jamais remettre en question une décision déjà actée sans le signaler explicitement à l'utilisateur et attendre confirmation. Poursuivre avec le même niveau d'exigence : architecte senior, justification systématique de chaque choix, refus de la sur-ingénierie.

**Phases 1, 1.5, 2 et 3 : complétées et validées. État actuel : développement actif du Vertical Slice 1 — Atelier 1.**

---

## 1. Vision du projet

Concevoir une plateforme professionnelle d'aide à la décision pour l'analyse de risques cyber, **100% spécialisée EBIOS Risk Manager** (méthode de l'ANSSI), destinée à accompagner un analyste de risques à travers les 5 ateliers de la méthode jusqu'à la génération du rapport final.

**Ce que la plateforme N'EST PAS** :
- Pas un chatbot
- Pas une interface de formulaires
- Pas une plateforme GRC généraliste
- Pas un moteur multi-référentiel (ISO 27005, NIST, MITRE ATT&CK, CIS, MEHARI, OCTAVE, FAIR — **exclus de la V1**, l'architecture reste juste assez propre pour permettre leur ajout futur sans sur-ingénierie anticipée)

**Ce que la plateforme EST** : un véritable système d'aide à la décision qui exécute fidèlement la méthode EBIOS RM, assisté (jamais remplacé) par un Système Expert et une IA, tous deux **optionnels et désactivables sans impact sur le cœur fonctionnel**.

**Horizon** : produit exploité et maintenu pendant 10 ans. Chaque décision d'architecture est prise dans cette optique.

---

## 2. Objectifs métier

- Assister l'analyste dans les 5 ateliers EBIOS RM
- Accélérer le travail, réduire les erreurs humaines, améliorer la qualité des analyses
- Produire automatiquement les livrables par atelier + le rapport final
- Ne jamais remplacer le jugement humain (Direction, RSSI, Métiers restent décideurs)
- Respecter esprit et lettre d'ISO 27001 / ISO 27005 / ISO 31000

---

## 3. Contraintes fondamentales (non négociables, rappelées à chaque phase)

1. **V1 = EBIOS RM exclusivement.** Aucun autre référentiel/méthode intégré.
2. **Pas de microservices.** Architecture **Modular Monolith**, un seul déploiement, frontières de code strictes entre modules.
3. **Pas d'Event Bus / message broker** (pas de Kafka, RabbitMQ, etc.). **Domain Events in-process, synchrones**, dispatchés dans la même transaction applicative (simple `DomainEventDispatcher`).
4. **Pas de multi-tenant, pas de SaaS, pas de connecteurs externes** (pas d'AD, GLPI, ServiceNow, scanner de vulnérabilités, SIEM/SOAR, fonctions SOC).
5. **Le Workflow Engine ne contient jamais de logique métier EBIOS** — uniquement machine à états (statuts + transitions).
6. **L'EBIOS Core Engine est l'unique cerveau métier** — seule source de vérité, seul détenteur des règles, calculs, dépendances inter-ateliers, recalculs.
7. **Le Système Expert et l'IA sont strictement optionnels et consultatifs** — jamais de canal d'écriture vers le Core Engine ; le système fonctionne à 100% sans eux (dégradation gracieuse).
8. **Aucune abstraction créée uniquement en prévision d'un besoin futur** (ex. pas de "Référentiel<T>" générique pour préparer ISO 27005 — seul le versionnement/immutabilité du Référentiel EBIOS suffit à ne pas fermer la porte).
9. **Toujours privilégier la solution la plus simple qui respecte DDD / Clean Architecture / SOLID.**
10. V1 = application web responsive, conçue pour faciliter (sans l'implémenter) une évolution future vers Flutter / Desktop / API publique / multi-tenant / multi-organisation.

---

## 4. Résumé — Phase 1 (Compréhension métier & analyse fonctionnelle)

### 4.1 Les 5 ateliers EBIOS RM (référence méthodologique constante)

| Atelier | Objectif | Entrées | Sorties |
|---|---|---|---|
| A1 – Cadrage et socle de sécurité | Périmètre métier/technique | — | Valeurs métier, biens supports, ER + gravité, socle de sécurité |
| A2 – Sources de risque | Couples SR/OV pertinents | Valeurs métier, ER (A1) | Couples SR/OV retenus/secondaires |
| A3 – Scénarios stratégiques | Cartographie écosystème + scénarios haut niveau | A1, SR/OV (A2) | Cartographie menace, scénarios stratégiques, mesures écosystème |
| A4 – Scénarios opérationnels | Modes opératoires techniques | A1, A2, scénarios stratégiques (A3) | Scénarios opérationnels, vraisemblance |
| A5 – Traitement du risque | Stratégie de traitement | Socle (A1), mesures écosystème (A3), scénarios (A3+A4) | Stratégie, PACS, risques résiduels, cadre de suivi |

### 4.2 Chaîne de traçabilité — colonne vertébrale du domaine métier

```
1 couple SR/OV (A2)
   ⇒ 1 scénario stratégique (A3)
        ⇒ N chemins d'attaque (A3)
             ⇒ 1 scénario opérationnel par chemin (A4)
                  ⇒ 1 scénario de risque = gravité (A3) × vraisemblance (A4)
                       ⇒ niveau de risque (A5)
```

### 4.3 Acteurs

Direction, RSSI, DSI, Métiers, Architectes fonctionnels/SI, Juristes, Acheteurs, Spécialiste cybersécurité (optionnel selon atelier), **Analyste de risques (utilisateur pivot)**, Administrateur plateforme, Auditeur (lecture seule), IA Provider (acteur système non humain).

### 4.4 Business Capability Map (validée, 9 domaines)

1. **Pilotage de l'étude** (cadrage, Workflow Engine — cycle de vie + orchestration transitions, suivi révisions)
2. **Modélisation métier et actifs** (missions, valeurs métier, **Asset Management** biens supports, ER, socle de sécurité)
3. **Analyse de la menace** (SR/OV, pertinence, écosystème, mesures écosystème)
4. **Construction et évaluation des scénarios de risque** *(Core Domain)*
5. **Traitement et gouvernance du risque** (stratégie, PACS, risques résiduels, suivi)
6. **Restitution et gestion documentaire** — **Reporting** (livrables) séparé de **Document Management** (versions, signatures, archivage)
7. **EBIOS Core Engine** (renommé, ex-"Système Expert cœur") : Référentiel EBIOS, Facts, Moteur de règles, Moteur de calcul/scoring, Orchestrateur de traitements métier
8. **Système Expert** (rétrogradé en composant consultatif) : Détection d'incohérences, Recommandations, Moteur d'explication, Administration des règles d'aide
9. **Assistance IA** : Context Builder, Prompt Builder, AI Provider Abstraction, Guardrails, Response Validator, Parser, Explanation Service
10. **Identité, accès et audit** (transverse)

---

## 5. Résumé — Phase 1.5 (Architecture dynamique)

### 5.1 Hiérarchie à 3 niveaux de confiance (principe cardinal du projet)

```
EBIOS Core Engine   → exécute la méthode (source de vérité unique, déterministe)
Système Expert       → conseille, détecte, explique (au-dessus, jamais à la place)
Assistant IA          → suggère, reformule (encore plus consultatif)
```

### 5.2 Les 5 couches (dépendances strictement descendantes)

1. **Présentation** — appelle uniquement l'Orchestration. Aucune règle métier, même de validation de formulaire.
2. **Orchestration (Application Layer)** — contient le Workflow Engine. Coordonne les use cases, délègue tout calcul au Core Engine. Aucune logique métier EBIOS ici.
3. **Métier (Domain Layer)** — contient l'EBIOS Core Engine. N'appelle jamais les couches supérieures ni le Système Expert/IA. N'accède à l'infrastructure que via des ports (Hexagonal).
4. **Services d'assistance (Supporting)** — Système Expert, IA, Reporting, Document Management. Consomment le Domain Layer **en lecture seule uniquement**. Jamais d'écriture directe dans le métier.
5. **Infrastructure** — implémente les ports. Aucune logique métier.

### 5.3 Les 18 principes d'architecture (P1 à P18)

- **P1** : Workflow Engine ne contient jamais de logique métier EBIOS.
- **P2** : L'EBIOS Core Engine est la seule source de vérité métier.
- **P3** : Le Système Expert ne modifie jamais les données métier.
- **P4** : L'IA ne modifie jamais directement les données métier.
- **P5** : Le Reporting est uniquement consommateur des données.
- **P6** : Chaque donnée possède un propriétaire unique (single writer).
- **P7** : Aucune dépendance circulaire entre composants (graphe = DAG strict).
- **P8** : Toutes les validations métier passent par le Core Engine (pas de duplication en Présentation).
- **P9** : Séparation Commande/Requête (CQS) systématique.
- **P10** : Communication du Core Engine vers l'aval **uniquement par Domain Events**, jamais par appel direct — garantit P2/P3/P4 mécaniquement.
- **P11** : Idempotence des recalculs (état cible recalculé, pas de deltas cumulés).
- **P12** : Traçabilité systématique et immuable des décisions/calculs métier (audit horodaté, attribué).
- **P13** : Versionnement de l'état de l'étude (snapshots), pas seulement des documents.
- **P14** : Le Référentiel EBIOS est **immuable** pendant l'exécution d'une étude (version figée à la création, jamais rétroactive).
- **P15** : Dégradation gracieuse — Core Engine et Workflow Engine fonctionnent intégralement sans Système Expert ni IA.
- **P16** : Le Reporting ne lit que des données validées/figées (snapshots), jamais un brouillon en cours.
- **P17** : Isolation stricte des schémas de données par Bounded Context (pas d'accès direct cross-BC en base).
- **P18** : Explicabilité obligatoire pour toute suggestion (SE ou IA) effectivement appliquée — trace d'origine dans l'audit.

### 5.4 Flux d'exécution majeurs (détaillés en Phase 1.5, réutilisables tels quels)

1. Création d'une nouvelle étude
2. Ouverture d'un atelier (double contrôle : Workflow Engine = transition de statut ; Core Engine = pré-requis métier)
3. Validation d'un atelier (validation métier = Core Engine seul ; transition = Workflow Engine)
4. Passage à l'atelier suivant
5. **Modification d'une donnée d'un atelier précédent** (flux le plus critique)
6. Recalcul des éléments impactés (ciblé, idempotent, décidé exclusivement par le Core Engine via son graphe de dépendances internes)
7. Intervention du Système Expert (lecture seule → Recommandation immuable → application uniquement via Command standard)
8. Intervention de l'IA (Context Builder → Prompt Builder → AI Provider → Parser → Guardrails → Response Validator → Explanation Service → Suggestion immuable)
9. Génération d'un rapport d'atelier (Reporting lit un snapshot figé uniquement)
10. Génération du rapport final (agrégation des 5 snapshots + synthèse A5)

---

## 6. Résumé — Phase 2 (DDD)

### 6.1 Les 7 Bounded Contexts définitifs

| # | BC | Type | Contenu |
|---|---|---|---|
| BC1 | **Study Lifecycle Management** | Generic Subdomain | Ex-"Workflow Engine". Statuts + transitions uniquement. |
| BC2 | **EBIOS Core Engine** | **Core Domain** | Fusionne Cadrage, Sources de risque, Écosystème, Scénarios de risque, Traitement, **et Asset Management** (biens supports intégrés ici, pas de BC séparé). |
| BC3 | **Système Expert (Advisory)** | Supporting Subdomain | Détection d'incohérences, recommandations, explication. |
| BC4 | **Assistant IA** | Supporting Subdomain | Suggestions, avec ACL vers l'AI Provider externe. |
| BC5 | **Reporting** | Generic Subdomain | Stateless, pas d'Aggregate Root, pure lecture → génération. |
| BC6 | **Document Management** | Generic Subdomain | Versions, signatures, archivage. Aucun vocabulaire EBIOS. |
| BC7 | **Identity, Access & Audit** | Generic Subdomain | Transverse, Shared Kernel technique. |

**Décision importante à retenir** : Asset Management N'EST PAS un BC séparé — c'est un Aggregate Root (`BienSupport`) à l'intérieur du Core Engine.

### 6.2 Context Map — relations

- BC1 ↔ BC2 : **Separate Ways**
- BC3 → BC2 : **Customer/Supplier**, BC3 = Conformist
- BC4 → BC2 : **Customer/Supplier**, BC4 = Conformist
- BC4 → BC3 : **Customer/Supplier**, BC4 = Conformist (optionnel, sens unique)
- BC4 → AI Provider externe : **Anticorruption Layer** (Parser + Response Validator)
- BC5 → BC2 : **Customer/Supplier**, BC5 = Conformist (snapshots figés uniquement)
- BC5 → BC6 : **Customer/Supplier**, BC6 = Supplier passif
- BC7 : **Shared Kernel technique**

### 6.3 Aggregate Roots par module (BC2 — Core Engine)

**Module Référentiel** : `VersionRéférentielEBIOS`

**Module Cadrage** : `Étude`, `ValeurMétier`, `BienSupport`, `ÉvénementRedouté`, `SocleSécurité`

**Module Sources de risque** : `CoupleSourceRisqueObjectifVisé`

**Module Écosystème** : `PartiePrenante`

**Module Scénarios de risque** : `ScénarioStratégique`, `ScénarioOpérationnel`, `ScénarioDeRisque`

**Module Traitement** : `MesurePACS`, `DécisionTraitementRisque`

### 6.4 Domain Services du Core Engine

`ServiceCalculPertinence`, `ServiceCalculNiveauMenace`, `ServiceCalculVraisemblance` (Strategy), `ServiceCalculNiveauRisque`, `ServiceValidationComplétudeAtelier`, `ServiceAnalyseImpactModification`, `ServiceOrchestrationRecalcul`.

### 6.5 Invariants métier critiques à ne jamais violer

- Aucune valeur dérivée n'est jamais saisissable manuellement.
- Le Référentiel EBIOS référencé par une étude est figé à la création.
- Une partie prenante en Zone de Danger doit être couverte par un scénario stratégique avant validation de A3.
- Un `ScénarioOpérationnel` a une relation 1:1 stricte avec son `CheminAttaque`.
- Acceptation d'un risque résiduel Élevé = justification + rôle Direction obligatoires.
- Système Expert et IA n'ont aucune Command d'écriture directe vers le Core Engine.
- Reporting ne lit jamais un état en cours d'édition, uniquement des snapshots figés post-validation d'atelier.

### 6.6 Mécanisme Domain Events (technique)

In-process, synchrone, même transaction, `DomainEventDispatcher` simple (pas de broker).

---

## 7. Décisions ouvertes / points tranchés en Phase 3

1. Mécanisme de snapshot figé — **tranché et implémenté** : copie versionnée simple en base (`SnapshotAtelier1`, cf. §12/mise à jour finale).
2. Ordonnancement des abonnés du `DomainEventDispatcher` — sans impact métier en V1.
3. Granularité des tables/schémas physiques — reporté à la Phase 4.

---

## 8. Glossaire métier EBIOS (à respecter à la lettre)

| Terme | Définition |
|---|---|
| **ER** | Événement Redouté |
| **SR/OV** | Source de Risque / Objectif Visé |
| **PP** | Partie Prenante |
| **EI** | Événement Intermédiaire |
| **AE** | Action Élémentaire |
| **PACS** | Plan d'Amélioration Continue de la Sécurité |
| **Scénario stratégique** | 1 couple SR/OV = 1 scénario stratégique, N chemins d'attaque |
| **Scénario opérationnel** | 1 chemin d'attaque = 1 scénario opérationnel |
| **Scénario de risque** | 1 chemin + son scénario opérationnel = niveau de risque |
| **Vraisemblance** | expresse / standard / avancée |

---

## 9. Rôles utilisateurs (rappel RBAC)

Direction, RSSI, DSI, Métiers, Architectes fonctionnels/SI, Juristes, Acheteurs, Spécialiste cybersécurité, Analyste de risques, Administrateur, Auditeur (lecture seule).

---

## 10. Convention de méthode de travail sur ce projet

- Ne jamais commencer par écrire du code.
- Justifier chaque décision technique ; comparer les alternatives.
- S'arrêter et attendre validation explicite avant de poursuivre une nouvelle phase.
- Philosophie constante : **la solution la plus simple qui respecte DDD/Clean Architecture/SOLID**.

---

## 11. Phase 3 — décisions finales (architecture pragmatique)

D�veloppement actif par **Vertical Slices** :
1. **Slice 1 — Atelier 1 (EN COURS, cf. §12)**
2. Slice 2 — Atelier 2 … Slice 7 — Assistant IA (non commencés)

### Stack technique confirmée en usage réel

| Élément | Choix |
|---|---|
| Backend | .NET 8 (SDK 8.0.423), ASP.NET Core **Minimal API** |
| Frontend | React 18 + TypeScript + Vite (**non commencé**) |
| Base de données | PostgreSQL 16, conteneur Docker |
| ORM | EF Core 8.0.29, provider Npgsql 8.0.8 |
| EF Core Tools (CLI) | 8.0.10 (écart mineur non bloquant) |
| API | REST, `/api/v1/...`, Swagger en dev |
| Auth | ASP.NET Core Identity prévu, **pas encore implémenté** |

### Structure de solution réellement adoptée

```
EbiosRM/
├── EbiosRM.sln
├── docker-compose.yml
├── .gitignore
├── src/
│   └── EbiosRM.Api/
│       ├── Program.cs
│       ├── EbiosRM.Api.csproj
│       ├── Migrations/
│       ├── Infrastructure/Persistence/EbiosDbContext.cs
│       └── Modules/
│           ├── CoreEngine/
│           │   ├── Domain/Cadrage/
│           │   │   ├── Etude.cs, IEtudeRepository.cs
│           │   │   ├── ValeurMetier.cs, IValeurMetierRepository.cs
│           │   │   ├── BienSupport.cs, IBienSupportRepository.cs
│           │   │   ├── EvenementRedoute.cs, IEvenementRedouteRepository.cs
│           │   │   ├── SocleSecurite.cs, ISocleSecuriteRepository.cs
│           │   │   ├── SnapshotAtelier1.cs, SnapshotAtelier1Content.cs
│           │   │   ├── ISnapshotAtelier1Repository.cs
│           │   │   └── ServiceValidationCompletudeAtelier1.cs, ServiceCreationSnapshotAtelier1.cs
│           │   └── Infrastructure/
│           │       ├── EtudeRepository.cs, ValeurMetierRepository.cs
│           │       ├── BienSupportRepository.cs, EvenementRedouteRepository.cs
│           │       ├── SocleSecuriteRepository.cs, SnapshotAtelier1Repository.cs
│           └── Reporting/
│               ├── RapportAtelier1Data.cs
│               ├── RapportAtelier1Service.cs
│               └── RapportAtelier1PdfGenerator.cs
└── tests/EbiosRM.CoreEngine.UnitTests/   (créé, encore vide)
```

**Modules pas encore créés** : `StudyLifecycle` (BC1), `SystemeExpert` (BC3), `AssistantIA` (BC4), `DocumentManagement` (BC6), `Identity` (BC7).

### Règle de dépendance vérifiée manuellement

`Modules/CoreEngine/Domain` ne référence que lui-même — aucune dépendance vers `Infrastructure`, `Program.cs`, ou un futur module `SystemeExpert`/`AssistantIA`. Respecté jusqu'ici.

---

## 12. État réel d'implémentation — Vertical Slice 1 (Atelier 1)

| Élément | Statut |
|---|---|
| Projet .NET + Docker Compose + PostgreSQL | ✅ Fonctionnel, testé |
| `EbiosDbContext` (schéma `core_engine`) | ✅ Fonctionnel |
| `GET /api/v1/health` | ✅ Fonctionnel |
| `Etude` + Repository + endpoints | ✅ Fonctionnel, testé |
| `ValeurMetier` + Repository + endpoints | ✅ Fonctionnel, testé |
| `BienSupport` + Repository + endpoints | ✅ Fonctionnel, testé |
| `EvenementRedoute` (gravité 1-4) + Repository + endpoints | ✅ Fonctionnel, testé |
| `ServiceValidationCompletudeAtelier1` | ✅ Fonctionnel, testé de bout en bout |
| Workflow A1 Brouillon → EnCours → Validee | ✅ Fonctionnel, testé de bout en bout |
| `SocleSecurite` + `ReferentielApplicable` | ✅ Fonctionnel, testé |
| **Snapshot Atelier1 (P13)** — `SnapshotAtelier1`, versionné à chaque validation | ✅ Fonctionnel, testé de bout en bout |
| **Reporting A1 (BC5)** — PDF via QuestPDF, lit exclusivement le snapshot (P16) | ✅ Fonctionnel, testé de bout en bout |
| Frontend React + TypeScript + Vite | ⬜ Non commencé |
| Tests unitaires/intégration automatisés | ⬜ Non commencé (dossier créé, vide) |
| Slice 2 — Atelier 2 | ⬜ Non commencé |

### Décision actée

**Workflow Engine minimal = statut global sur `Etude.Statut`** (enum `StatutEtude`), pas de structure séparée par atelier. À réévaluer à l'ouverture du Slice 2, pas avant.

### Environnement de développement réel (Joel)

- Ubuntu 22.04, Dell Precision 3520
- .NET SDK 8.0.423
- Docker 29.1.3, Docker Compose v5.4.0 (sudo requis, correctif non appliqué)
- PostgreSQL 16 conteneur `ebiosrm-postgres`, port hôte **5433**, user `ebiosrm`, password `ebiosrm_dev`, database `ebiosrm`
- API sur `http://localhost:5197`
- Chemin projet : `~/Documents/EbiosRM`
- `dotnet-ef` global tool 8.0.10, PATH dans `~/.bashrc`

### Incidents déjà rencontrés et résolus

1. Mismatch EF Core Tools (10.x vs .NET 8) → réinstallé en 8.0.10.
2. `appsettings.json` corrompu (double écriture JSON) → réécrit.
3. Incohérence nom clé de connexion → aligné sur `EbiosDb` (définitif).
4. `sed` multi-lignes ayant corrompu `Program.cs` → toute modification passe désormais par `cat > fichier << 'EOF'` complet.
5. `catch (ArgumentOutOfRangeException)` mal ordonné après `catch (ArgumentException)` → ordre corrigé.
6. `EvenementRedouteRepository.cs` jamais créé malgré confirmation → toujours vérifier avec `ls`.
7. Idem pour `SnapshotAtelier1.cs`/`SnapshotAtelier1Content.cs` (jamais créés malgré discussion antérieure) → recréés, build revérifié.
8. `PROJECT_CONTEXT.md` lui-même jamais sauvegardé sur disque malgré plusieurs échanges le supposant à jour → **toujours vérifier avec `ls`/`cat` avant de supposer qu'un fichier de contexte existe**, même pour la documentation, pas seulement le code.

### Convention de travail spécifique à cette phase d'implémentation

- Terminal Linux réel, commandes collées une par une, résultat brut retourné.
- Format : **Objectif → fichiers concernés → code exact → explication courte → commande pour tester**.
- Ne jamais supposer qu'un fichier existe sans vérifier via `cat`/`ls`.
- Réécriture complète via `cat > fichier << 'EOF'`, jamais d'édition incrémentale fragile.
- Un seul composant à la fois, testé de bout en bout avant de passer au suivant.

---

## Mise à jour — Snapshot Atelier 1 (P13) + Reporting PDF (BC5)

Terminé, testé de bout en bout, committé (`8c2cf72`).

- `SnapshotAtelier1` (agrégat, Core Engine) : copie versionnée immuable créée à chaque validation de l'Atelier 1. Champ `Version` incrémenté à chaque revalidation (correction = nouvelle version, historique conservé, jamais écrasé).
- Module `Reporting` (BC5) : `RapportAtelier1Service` lit exclusivement le dernier snapshot (P16 respecté). `RapportAtelier1PdfGenerator` génère le PDF via QuestPDF (licence Community).
- Endpoint `GET /api/v1/etudes/{id}/rapports/atelier1`.
- `valider-atelier1` déclenche la création du snapshot, retourne `snapshotVersion`.
- Migration `AjoutSnapshotAtelier1` appliquée (`core_engine.snapshots_atelier1`, index unique `EtudeId+Version`).

Testé de bout en bout : création étude → demarrer-atelier1 → valeur métier + bien support + ER + socle sécurité → valider-atelier1 (`snapshotVersion: 1` confirmé) → GET rapport PDF (200, PDF valide) → contenu vérifié en base PostgreSQL.

**Prochaine action : fork non tranché entre Frontend (React/TS/Vite) et Tests automatisés — à décider avec l'utilisateur avant de continuer.**

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, sauf ambiguïté réelle.*

---

## Mise à jour — Session Frontend (design system + Dashboard)

**Stack confirmée en usage réel** : React 18 + TypeScript + Vite + Tailwind CSS v4 (config CSS-first via `@theme`, pas de `tailwind.config.js` classique) + Lucide React + React Router. Projet dans `frontend/` (au même niveau que `src/EbiosRM.Api`), backend non touché.

### Décision de direction visuelle (actée, à ne pas reproposer sans signal explicite)

Deux tentatives précédentes ("dashboard admin classique" puis "dashboard sombre premium générique" avec cartes KPI à icônes colorées, donut chart, radar chart) ont été explicitement rejetées par l'utilisateur comme trop proches d'un template SaaS générique — y compris une maquette de référence (générée ailleurs) présentée en cours de session et **elle aussi rejetée** pour la même raison. La direction retenue s'appuie sur le domaine métier lui-même plutôt que sur des conventions SaaS :

- **Palette** : `ink #101418` (chrome sombre), `paper #F6F5F1` (espace de travail clair), `signature #000091` (Bleu France officiel, utilisé avec parcimonie pour l'action/l'état actif), `steel`/`steel-light`/`steel-faint` (hiérarchie neutre), sémantique risque désaturée (`risk-critical #A23B3B`, `risk-high #B8752E`, `risk-moderate #A68A2A`, `risk-low #4C7A5E`).
- **Typographie** : `Fraunces` (serif, titres/ateliers, ton dossier officiel), `IBM Plex Sans` (UI/corps), `IBM Plex Mono` (données techniques : références, statuts, %, dates).
- **Signature visuelle du produit** : la progression des 5 ateliers n'est **jamais** représentée comme des cartes indépendantes — c'est une chaîne connectée (rail vertical compact en sidebar, version étendue à poids visuel asymétrique dans le dashboard : l'atelier courant domine, les ateliers à venir sont comprimés/estompés), reflétant littéralement la chaîne de traçabilité SR/OV → scénario → risque du domaine métier (§4.2).
- **Statistiques du dashboard** : délibérément dérivées du domaine EBIOS plutôt que de widgets SaaS génériques — matrice de risque Gravité × Vraisemblance (calcul central de la méthode) à la place d'un donut/radar chart, journal d'activité en rail vertical (même langage que la chaîne des ateliers, cohérent avec P12 traçabilité), bandeau d'indicateurs en typo serif sans cartes/icônes colorées.
- **À éviter explicitement, sur toute page future** : cartes blanches multiples, icônes colorées en badge, donut/radar chart, bleu SaaS générique, gradients décoratifs, tableau façon CRM avec avatars.

### Fichiers réellement créés/modifiés (vérifiés par build, pas supposés)

```
frontend/
└── src/
    ├── index.css                                  (tokens @theme : couleurs, fonts, reduced-motion)
    ├── App.tsx                                     (routing, inchangé dans sa structure)
    ├── components/
    │   ├── layout/
    │   │   ├── Sidebar.tsx                         (réécrit : rail EBIOS, plus de bleu marine générique)
    │   │   ├── Header.tsx                          (réécrit : minimal, breadcrumb + statut atelier)
    │   │   └── AppLayout.tsx                       (fond paper)
    │   ├── methodology/
    │   │   └── AtelierChain.tsx                    (AtelierChainCompact + AtelierChainExpanded, composant signature partagé sidebar/dashboard)
    │   └── dashboard/
    │       ├── InstrumentStrip.tsx                 (bandeau d'indicateurs, typo serif)
    │       ├── RiskMatrix.tsx                      (matrice Gravité × Vraisemblance)
    │       └── JournalActivite.tsx                 (rail vertical, même langage que AtelierChain)
    └── pages/
        └── Dashboard.tsx                           (réécrit : dossier d'étude + chaîne + matrice + journal + table études)
```

**Pages non touchées, restent en placeholder** : `/etudes`, `/ateliers/1` à `/ateliers/5`, `/rapports`, `/parametres` — hors périmètre de cette session (cf. brief utilisateur §14).

### État réel (vérifié : `npm run build` vert après corrections)

| Élément | Statut |
|---|---|
| Design system (tokens couleur/typo, Tailwind v4 `@theme`) | ✅ Fonctionnel |
| Sidebar (rail EBIOS compact) | ✅ Fonctionnel |
| Header (minimal) | ✅ Fonctionnel |
| Dashboard (dossier d'étude + chaîne + matrice + journal + table) | ✅ Build vert — **retour visuel utilisateur final en attente** |
| Pages Études / Ateliers 1-5 / Rapports / Paramètres | ⬜ Toujours en placeholder générique |

### Incidents rencontrés pendant cette session (à ajouter à la liste générale)

9. Coller des template literals (backticks) et des caractères Unicode (flèches `→`, tirets longs `—`) dans le terminal a corrompu le collage à plusieurs reprises (erreurs `TS1005`/`TS1382`/`TS17002` en cascade) → pour tout fichier `.tsx` généré depuis cette session, concaténation de chaînes (`'a' + b + 'c'`) plutôt que template literals, et caractères ASCII simples (pas de flèches/tirets longs/apostrophes typographiques) dans le code source généré. Toujours vérifier avec `grep`/`wc -l` après un `cat > fichier << 'EOF'` avant de relancer un build, plutôt que de re-tenter à l'aveugle.
10. Une variable déclarée mais jamais utilisée (`maxDot`) a fait échouer le build (`TS6133`, `noUnusedLocals` actif dans `tsconfig`) → confirme que le projet frontend a `noUnusedLocals`/`noUnusedParameters` stricts activés ; en tenir compte pour tout code futur.

### Prochaine action

Fork explicitement mis en pause par l'utilisateur ("laissons comme ça, on ajustera plus tard") : ni la suite du frontend (déclinaison sur Études/écrans d'atelier), ni les tests automatisés backend n'ont été retranchés. **Ne pas reprendre l'un ou l'autre sans une instruction explicite de l'utilisateur au démarrage de la prochaine session.**

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, ni sur la direction visuelle frontend déjà actée ci-dessus, sauf ambiguïté réelle.*

---

## Mise à jour — Déclinaison du design sur les pages restantes

Build vérifié vert (`npm run build`). Retour visuel utilisateur toujours en attente (capture d'écran demandée pour `/etudes` et `/ateliers/:numero`).

### Fichiers créés/modifiés dans cette session

```
frontend/src/
├── components/shared/PageHeader.tsx        (en-tête réutilisé : eyebrow mono + titre Fraunces + description)
├── pages/
│   ├── Etudes.tsx                          (remplace le Placeholder — table du registre des études)
│   ├── AtelierPage.tsx                     (page dynamique unique pour /ateliers/:numero, remplace les 5 Placeholder)
│   ├── Rapports.tsx                        (liste des livrables générés, style cohérent)
│   └── Parametres.tsx                      (remplace le Placeholder)
└── App.tsx                                 (routing mis à jour : /ateliers/:numero au lieu de 5 routes statiques ; suppression du composant Placeholder, plus aucune route ne l'utilise)
```

### Décisions notables

- **`/ateliers/:numero` est une seule page dynamique**, pas 5 fichiers séparés — lit `ATELIERS` (déjà défini dans `AtelierChain.tsx`) pour titre/objectif/état, affiche un contenu verrouillé si `statut === 'todo'`. Seul l'Atelier 1 a un contenu représentatif détaillé (valeurs métier, biens support, ER, socle) reflétant les vraies données de test du backend (étude "Société de biotechnologie") ; les ateliers 2 à 5 affichent un texte d'attente. **Aucune connexion API réelle** — données statiques en dur, à remplacer quand le frontend sera branché sur le backend (hors périmètre de cette session, purement visuelle).
- Le composant `Placeholder` dans `App.tsx` a été supprimé — toutes les routes ont désormais une vraie page.

### État réel (mis à jour)

| Élément | Statut |
|---|---|
| Dashboard | ✅ Build vert, retour visuel final en attente |
| Page Études | ✅ Build vert, retour visuel en attente |
| Page Atelier (dynamique, 5 routes) | ✅ Build vert, retour visuel en attente |
| Page Rapports | ✅ Build vert, retour visuel en attente |
| Page Paramètres | ✅ Build vert, retour visuel en attente |
| Connexion frontend ↔ API réelle | ⬜ Non commencé — tout le frontend actuel utilise des données statiques en dur |
| Tests automatisés backend | ⬜ Toujours non commencé |

### Prochaine action

En attente du retour visuel utilisateur sur `/etudes` et `/ateliers/:numero`. Ensuite, deux chantiers réels restent ouverts et non priorisés : (1) connecter le frontend à l'API réelle (remplacer les données statiques), (2) tests automatisés backend. Ne pas en démarrer un sans confirmation explicite au début de la prochaine session.

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, ni sur la direction visuelle frontend déjà actée, sauf ambiguïté réelle.*

---

## Mise à jour — Connexion du frontend à l'API réelle

Flux de bout en bout confirmé fonctionnel : `/etudes` (liste réelle) → création d'une étude (POST réel) → redirection vers `/etudes/:etudeId` (données réelles) → `/etudes/:etudeId/ateliers/1` (valeurs métier, biens support, ER, socle réels, boutons démarrer/valider réels).

### CORS (backend)

Ajouté dans `Program.cs` : `builder.Services.AddCors(...)` + `app.UseCors()`. Point technique important à ne pas reproduire en erreur : la première tentative utilisait `AddPolicy("Frontend", ...)` (politique **nommée**), ce qui échoue silencieusement avec le routing par endpoints des Minimal API — chaque endpoint doit alors réclamer explicitement la politique via `.RequireCors(...)`, sinon les requêtes `OPTIONS` (preflight) remontent un `405 HTTP Method Not Supported`. **Correctif retenu : `AddDefaultPolicy` + `app.UseCors()` sans nom**, qui s'applique automatiquement à tous les endpoints. Origines autorisées : `http://localhost:5174` et `http://localhost:5175` (les deux ports que Vite utilise en dev selon disponibilité).

### Client API frontend

Nouveau fichier `frontend/src/lib/api.ts` : wrapper `fetch` (`apiFetch`), classe `ApiError`, types TypeScript alignés sur les DTOs backend réels (`Etude`, `ValeurMetier`, `BienSupport`, `EvenementRedoute`, `SocleSecurite`, `ReferentielApplicable`), et une fonction par endpoint (`listEtudes`, `getEtude`, `createEtude`, `demarrerAtelier1`, `validerAtelier1`, `listValeursMetier`, `listBiensSupport`, `listEvenementsRedoutes`, `getSocleSecurite`, `rapportAtelier1Url`). Base URL en dur : `http://localhost:5197/api/v1`.

### Restructuration du routing frontend

`/etudes/:etudeId` remplace le concept d'"étude courante" implicite qui existait dans la version précédente (données statiques). Nouvelles routes : `/` → redirige vers `/etudes` ; `/etudes` (liste) ; `/etudes/:etudeId` (dashboard réel, ex-`Dashboard.tsx`) ; `/etudes/:etudeId/ateliers/:numero` (ex-`AtelierPage.tsx`). `Sidebar.tsx` lit `etudeId` via `useParams()` (fonctionne car `Sidebar`/`Header` sont rendus par `AppLayout`, qui fait partie de la branche de route correspondante) et affiche la chaîne des ateliers réelle une fois l'étude chargée.

### Honnêteté sur l'état réel des données (décision actée)

Le Workflow Engine backend ne suit que `Etude.Statut` global (Brouillon/EnCours/Validee), pas de statut par atelier. Conséquence assumée : seul l'**Atelier 1** reflète un état réel (done/current/todo dérivé de `Etude.Statut`) ; les Ateliers 2 à 5 restent **toujours verrouillés (`todo`)**, même si l'étude est validée, tant que le Slice 2+ n'existe pas côté backend. La matrice de risque et le journal d'activité (précédemment remplis de données représentatives fictives) ont été retirés du Dashboard connecté et remplacés par un message explicite indiquant qu'ils apparaîtront une fois les ateliers correspondants implémentés — **aucune donnée inventée n'est affichée une fois le frontend connecté au réel**.

### Incidents rencontrés pendant cette session (suite de la numérotation)

11. **CORS avec politique nommée + Minimal API = `405` silencieux sur `OPTIONS`.** Cause structurelle ASP.NET Core, pas une faute de frappe — cf. section CORS ci-dessus. Corrigé via `AddDefaultPolicy`.
12. **Un `cat > Program.cs << 'EOF'` censé ajouter le CORS n'a en réalité jamais modifié le fichier** (le `dotnet build` "vert" collé juste après provenait du binaire déjà compilé, pas d'une recompilation du nouveau contenu — un build réussi ne prouve pas qu'un fichier source a changé). Découvert uniquement parce que `grep -n "Cors" Program.cs` ne trouvait rien après coup. **Leçon renforcée : après tout `cat > fichier << 'EOF'` cense modifier un fichier existant, vérifier avec `grep`/`wc -l` le contenu réel avant de considérer le changement acquis — ne jamais se fier à un build réussi comme preuve qu'un fichier a été mis à jour.**
13. **Même symptôme côté frontend** : un `cat > src/App.tsx << 'EOF'` censé ajouter les routes `/etudes/:etudeId` n'avait jamais été exécuté jusqu'au bout — l'ancien routing (5 routes statiques `/ateliers/1` à `/ateliers/5`, sans `Navigate` importé) est resté en place plusieurs échanges durant, jusqu'à ce que l'erreur React Router `No routes matched location` le révèle. Renforce la leçon de l'incident 12 : vérifier systématiquement après coup, pas seulement en cas de build cassé.

### Bugs connus, non corrigés (a traiter a la prochaine reprise si besoin)

- `Header.tsx` affiche toujours des donnees statiques en dur ("Societe de biotechnologie", "ATELIER 02 EN COURS") — jamais reconnecte a `useParams()`/`getEtude()` contrairement a la Sidebar. Incoherent visuellement avec le reste desormais connecte.
- `Etudes.tsx`, fonction `handleCreer` : pas de `.catch()` sur l'appel `createEtude(...)` — un echec de creation reste silencieux pour l'utilisateur (aucun message d'erreur affiche), meme si le flux fonctionne dans le cas nominal.

### État réel (mis à jour)

| Élément | Statut |
|---|---|
| CORS backend | ✅ Fonctionnel (AddDefaultPolicy) |
| Client API frontend (`lib/api.ts`) | ✅ Fonctionnel |
| Liste des études (réelle) | ✅ Fonctionnel |
| Création d'étude (réelle) | ✅ Fonctionnel (gestion d'erreur incomplète, cf. ci-dessus) |
| Dashboard étude (réel) | ✅ Fonctionnel |
| Atelier 1 (réel : lecture + démarrer + valider + PDF) | ✅ Fonctionnel |
| Ateliers 2-5 | ⬜ Verrouillés, honnête (backend inexistant) |
| Header dynamique | ⬜ Reste statique, à corriger |
| Tests automatisés backend | ⬜ Toujours non commencé |

### Prochaine action

Aucune priorité fixée. Reste ouverts : corriger le Header (connexion réelle) et la gestion d'erreur de création d'étude ; construire les Slices 2-5 backend ; écrire les tests automatisés backend ; décliner les formulaires de création (valeurs métier, biens support, ER, socle) côté Atelier 1, actuellement en lecture seule uniquement côté frontend.

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, ni sur la direction visuelle frontend déjà actée, sauf ambiguïté réelle.*

---

## Mise à jour — Socle de Sécurité aligné sur ISO/IEC 27001:2022 Annexe A

Build backend et frontend verts. Retour visuel utilisateur en attente.

### Décision actée

Le Socle de Sécurité n'est plus une liste libre de référentiels tapés à la main — c'est désormais une checklist basée sur les 93 contrôles officiels de l'Annexe A ISO/IEC 27001:2022 (37 Organisationnel, 8 Personnes, 14 Physique, 34 Technologique — vérifié par recherche web, comptage confirmé exact via `grep -c` sur le fichier généré). L'option « référentiel libre » (pour PSSI, RGPD, etc., qui ne sont pas des contrôles ISO 27001) est conservée en parallèle.

**Approche retenue** (délibérément minimale, cf. discussion avec l'utilisateur) : pas de remaniement lourd du backend, pas de seed de 93 lignes en base par étude, pas de nouvel agrégat. Le catalogue des 93 contrôles est une **constante statique côté frontend uniquement** (`frontend/src/lib/iso27001.ts`). Le backend gagne seulement deux colonnes optionnelles sur `ReferentielApplicable` pour capturer le thème et le code quand un contrôle ISO est sélectionné.

Champ **Priorité** explicitement écarté par l'utilisateur — non implémenté.

### Fichiers modifiés (backend)

- `Modules/CoreEngine/Domain/Cadrage/SocleSecurite.cs` : `ReferentielApplicable.Creer(...)` et `SocleSecurite.AjouterReferentiel(...)` acceptent désormais `theme` et `codeControle` optionnels (nullable, défaut `null` pour un référentiel libre).
- `Infrastructure/Persistence/EbiosDbContext.cs` : mapping des colonnes `Theme` (varchar 100) et `CodeControle` (varchar 20), optionnelles.
- Migration `AjoutThemeCodeControleReferentiel` appliquée (`ALTER TABLE core_engine.referentiels_applicables ADD "CodeControle" ...` / `ADD "Theme" ...`).
- `Program.cs` : `AjouterReferentielRequest` a deux paramètres optionnels de plus (`Theme`, `CodeControle`), propagés dans l'appel à `socle.AjouterReferentiel(...)`.

### Fichiers modifiés (frontend)

- **Nouveau** `frontend/src/lib/iso27001.ts` : `CATALOGUE_ISO_27001` (93 entrées `{ code, theme, nom }`, noms officiels traduits en français) + `THEMES_ISO`.
- `frontend/src/lib/api.ts` : `ReferentielApplicable` gagne `theme?`/`codeControle?` ; `addReferentiel(...)` accepte ces deux paramètres optionnels en plus.
- `frontend/src/pages/AtelierPage.tsx`, fonction `SocleSection` : réécrite. Formulaire à bascule (radio) « Contrôle ISO 27001 » (menu déroulant groupé par thème via `<optgroup>`, 93 options) vs « Autre référentiel » (saisie libre, comportement identique à avant). L'affichage regroupe les référentiels déjà ajoutés par thème ; les référentiels sans thème (saisie libre) apparaissent sous « Autres référentiels ».

### État réel (mis à jour)

| Élément | Statut |
|---|---|
| Backend : champs Theme/CodeControle sur ReferentielApplicable | ✅ Fonctionnel (build + migration confirmés) |
| Frontend : catalogue des 93 contrôles ISO 27001:2022 | ✅ Fonctionnel, comptage vérifié (37/8/14/34) |
| Frontend : formulaire Socle de Sécurité (ISO vs libre) | ✅ Build vert, retour visuel utilisateur en attente |
| Formulaires de création (valeurs métier, biens support, ER) | ✅ Fonctionnels (session précédente) |
| Tests automatisés backend | ⬜ Toujours non commencé |
| Slice 2 (Atelier 2) | ⬜ Toujours non commencé |

### Prochaine action

Retour visuel utilisateur sur le nouveau formulaire Socle de Sécurité en attente. Ensuite, aucune priorité fixée entre : tests automatisés backend, Slice 2, ou poursuite du frontend.

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, ni sur la direction visuelle frontend déjà actée, ni sur le choix du catalogue ISO 27001 statique côté frontend (plutôt qu'un seed backend), sauf ambiguïté réelle.*

---

## Mise à jour — Champ « État actuel » (texte libre) sur le Socle de Sécurité

Build backend et frontend verts, chaîne complète bout en bout.

### Décision actée

Sur proposition explicite de l'utilisateur (avec référence au tableau ISO 27001 déjà utilisé pour le mémoire), `ReferentielApplicable` distingue désormais deux notions séparées, toutes deux conservées (Option A retenue face à Option B qui aurait remplacé l'une par l'autre) :
- `Etat` (`EtatConformite` : Conforme/NonConforme/NonApplicable) — statut structuré, utilisé pour la couleur/le tri, calculs futurs de taux de conformité.
- `EtatActuel` (`string?`, texte libre, max 2000 caractères) — description factuelle de ce qui est réellement observé (ex. « Supports amovibles non chiffrés », « Architecture 3 couches, 20 VLAN, DMZ, Pare-feu Cisco ASA »), distincte du jugement de conformité.

### Chaîne de fichiers modifiée (backend, dans l'ordre)

1. `Modules/CoreEngine/Domain/Cadrage/SocleSecurite.cs` : `ReferentielApplicable.Creer(...)` et `SocleSecurite.AjouterReferentiel(...)` acceptent `etatActuel` optionnel.
2. `Infrastructure/Persistence/EbiosDbContext.cs` : mapping `referentiel.Property(r => r.EtatActuel).HasMaxLength(2000)`.
3. Migration `AjoutEtatActuelReferentiel` appliquée.
4. `Program.cs` : `AjouterReferentielRequest` + paramètre `EtatActuel`, propagé dans `socle.AjouterReferentiel(...)`.
5. `SnapshotAtelier1Content.cs` : `ReferentielApplicableSnapshot` + champ `EtatActuel`.
6. `ServiceCreationSnapshotAtelier1.cs` : propagation dans la projection.
7. `RapportAtelier1Data.cs` : `ReferentielApplicableData` + champ `EtatActuel`.
8. `RapportAtelier1Service.cs` : propagation dans la projection.
9. `RapportAtelier1PdfGenerator.cs` : colonne « État actuel » ajoutée aux deux tableaux (contrôles ISO groupés par thème, et « Autres référentiels »).

### Frontend

- `src/lib/api.ts` : `ReferentielApplicable.etatActuel?` ; `addReferentiel(...)` accepte `etatActuel` optionnel en dernier paramètre.
- `src/pages/AtelierPage.tsx`, `SocleSection` : `<textarea>` ajouté au formulaire (« État actuel observé »load, placeholder avec l'exemple donné par l'utilisateur) ; l'affichage montre le texte libre en petit sous chaque contrôle déjà ajouté, si renseigné.

### Outil créé pendant cette session

`~/Documents/EbiosRM/scripts/seed-etude-test.sh` : script bash/curl qui crée en une seule commande une étude complète (2 valeurs métier, 2 biens support, 2 ER, socle avec 3 contrôles ISO répartis sur 3 thèmes + 1 référentiel libre), pour tester visuellement le frontend sans remplir les formulaires à la main. Affiche les URLs directes à ouvrir en fin d'exécution. Ne couvre pas encore le champ `EtatActuel` (créé avant cette fonctionnalité) — à enrichir si besoin lors d'un futur test.

### État réel (mis à jour)

| Élément | Statut |
|---|---|
| Socle de sécurité : contrôles ISO 27001 + référentiels libres | ✅ Fonctionnel |
| Champ État actuel (texte libre) | ✅ Fonctionnel, build vert, test manuel en attente de confirmation utilisateur |
| Script de seed pour tests rapides | ✅ Fonctionnel (hors champ EtatActuel) |
| Tests automatisés (Playwright/xUnit) | ⬜ Non commencé, discuté mais pas retenu comme priorité immédiate |
| Slice 2 (Atelier 2) | ⬜ Non commencé |

### Note sur la répartition du travail

L'utilisateur a demandé si des tâches de ce projet pouvaient être confiées à une autre session Claude ou à un autre outil (ChatGPT) pour économiser les tokens. Réponse donnée : le travail EBIOS RM sur Excel (Ateliers 3/4/5, autodiagnostic, MITRE ATT&CK) est un workstream complètement séparé et peut être délégué sans perte. À l'intérieur de ce codebase .NET/React en revanche, toute nouvelle tâche doit partir de `PROJECT_CONTEXT.md` lu intégralement pour respecter l'architecture et les conventions déjà actées (notamment : réécriture complète de fichier plutôt que `sed` multi-lignes, vérification systématique `grep`/`wc -l` après toute commande censée modifier un fichier, ne jamais se fier à un build réussi comme preuve qu'un fichier source a changé). Une fois ce fichier à jour (ce qui est le cas maintenant), démarrer le Slice 2 ou un autre chantier indépendant dans une session séparée est raisonnable.

### Prochaine action

Confirmation utilisateur du test manuel du champ État actuel (ajout, affichage, PDF). Ensuite, aucune priorité fixée entre Slice 2, tests automatisés, ou poursuite du frontend.

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3 déjà actées, ni sur la direction visuelle frontend, ni sur les décisions de modélisation du Socle de Sécurité (catalogue ISO statique frontend + Etat/EtatActuel séparés), sauf ambiguïté réelle.*

---

## Mise à jour — Liberté de modification pour l'analyste (backend terminé, frontend en attente)

### Décision actée

Sur demande explicite de l'utilisateur : l'analyste doit pouvoir librement modifier/supprimer les données déjà saisies dans un atelier, et pouvoir rouvrir une étude validée pour la corriger. Les deux volets ont été retenus ensemble (pas seulement l'un ou l'autre).

### Ce qui est fait, backend, vérifié par build (0 Warning, 0 Error), AUCUNE migration nécessaire (uniquement endpoints + méthodes de domaine, pas de nouvelle colonne)

**1. Réouverture d'étude** :
- `Etude.cs` : nouvelle méthode `RouvrirAtelier1()` (Validee → EnCours). Ne touche jamais au snapshot déjà créé (P13/P16 respectés — il reste consultable comme version figée).
- `Program.cs` : `POST /api/v1/etudes/{id}/rouvrir-atelier1`.

**2. Valeurs métier** :
- `ValeurMetier.cs` : méthode `Modifier(description, entiteResponsable)`.
- `IValeurMetierRepository`/`ValeurMetierRepository` : ajout de `ObtenirParIdAsync`, `MettreAJourAsync`, `SupprimerAsync` (n'existaient pas avant).
- `Program.cs` : `PUT /api/v1/etudes/{etudeId}/valeurs-metier/{id}` et `DELETE` équivalent.

**3. Biens support** :
- `BienSupport.cs` : méthode `Modifier(description, type, entiteResponsable)` (ne permet pas de rattacher à une autre valeur métier — INV7 reste vérifié uniquement à la création, pas de besoin identifié de le permettre).
- `IBienSupportRepository`/`BienSupportRepository` : mêmes ajouts que ValeurMetier.
- `Program.cs` : `PUT /api/v1/etudes/{etudeId}/biens-support/{id}` et `DELETE` équivalent.

**4. Événements redoutés** :
- `EvenementRedoute.cs` : nouvelle méthode `ModifierDescription(description)`, distincte de `RecoterGravite` déjà existante (justification métier : la gravité aura un impact sur un futur recalcul des scénarios, la description n'en a pas — donc gardées séparées intentionnellement).
- `IEvenementRedouteRepository`/`EvenementRedouteRepository` : ajout de `SupprimerAsync` (le reste existait déjà).
- `Program.cs` : `PUT /api/v1/etudes/{etudeId}/evenements-redoutes/{erId}` (description + gravité combinées en un seul appel, garde aussi l'ancien `PUT .../gravite` séparé pour compatibilité) et `DELETE` équivalent.

**5. Référentiels du socle de sécurité** (ISO 27001 ou libres) :
- `SocleSecurite.cs` : `ReferentielApplicable` N'EST PAS un Aggregate Root séparé (owned entity EF Core) — donc pas de nouveau repository. Méthodes ajoutées directement sur `SocleSecurite` : `ModifierReferentiel(referentielId, nom, etat, theme, codeControle, etatActuel)` et `SupprimerReferentiel(referentielId)`, qui délèguent à `ReferentielApplicable.Modifier(...)` (nouvelle méthode) en interne.
- `Program.cs` : `PUT /api/v1/etudes/{etudeId}/socle-securite/referentiels/{referentielId}` et `DELETE` équivalent, via `ISocleSecuriteRepository.MettreAJourAsync` déjà existant.

### Incident majeur de cette session (à ne jamais reproduire)

Un patch `sed -i 'NUMEROa\...'` sur les Événements redoutés a inséré le nouveau bloc **à l'intérieur** d'un `catch` existant non fermé, cassant la structure du fichier tout en laissant `dotnet build` réussir (le compilateur a "digéré" ça en signalant seulement un `warning CS0162: Unreachable code`, pas une erreur bloquante). Le problème n'a été détecté que par vérification manuelle du warning, pas par le build seul. **Deux tentatives de patch ciblé (`sed` puis Python avec bloc `old`/`new`) ont échoué silencieusement** avant qu'on se résolve à faire un `cat Program.cs` complet et à réécrire le fichier entier en une seule fois.

**Leçon renforcée, à appliquer strictement dans toute session future sur ce projet** :
- Un `dotnet build` réussi avec des **warnings** n'est PAS un signal à ignorer — un `CS0162 Unreachable code` en particulier indique presque toujours une structure de blocs (`try`/`catch`/accolades) cassée quelque part, même si ça compile.
- Après 2 échecs de patch ciblé consécutifs sur un même fichier, arrêter d'insister avec `sed`/patch partiel — demander le fichier complet et le réécrire en entier. C'est plus fiable que de chercher à minimiser la taille de la commande.
- `grep -c` de vérification doit porter sur un motif qui ne peut PAS être ambigu (ex. compter les endpoints par leur route exacte, pas par un mot-clé générique) — un `grep -c "MotClé"` qui renvoie 0 peut aussi bien signifier "le texte n'existe pas" que "ma regex est mal formée" ; toujours croiser avec un deuxième indicateur (ex. `wc -l`, ou relire une section précise avec `sed -n`).

### État réel du backend (mis à jour, vérifié)

| Type de donnée | Ajouter | Modifier | Supprimer |
|---|---|---|---|
| Étude (réouverture) | — | ✅ `RouvrirAtelier1` | — |
| Valeur métier | ✅ | ✅ | ✅ |
| Bien support | ✅ | ✅ | ✅ |
| Événement redouté | ✅ | ✅ (description + gravité) | ✅ |
| Référentiel socle (ISO/libre) | ✅ | ✅ | ✅ |

### Frontend — RESTE À FAIRE (prochaine session, point de reprise exact)

**Rien n'a encore été touché côté frontend pour cette fonctionnalité.** À construire :

1. **Bouton "Rouvrir l'atelier"** dans `AtelierPage.tsx`, visible quand `etude.statut === 'Validee'`, appelant `POST /rouvrir-atelier1` (nouvelle fonction à ajouter dans `src/lib/api.ts` : `rouvrirAtelier1(etudeId)`).
2. **Pour chacune des 4 sections** (`ValeursMetierSection`, `BiensSupportSection`, `EvenementsRedoutesSection`, `SocleSection` dans `AtelierPage.tsx`) : ajouter un mode édition par ligne (bouton crayon → transforme la ligne en formulaire pré-rempli, comme `InlineForm` mais pour une entité existante plutôt qu'une nouvelle) et un bouton suppression (avec confirmation, `window.confirm` suffit pour l'instant — pas de modale custom nécessaire).
3. **`src/lib/api.ts`** : ajouter les fonctions `updateValeurMetier`, `deleteValeurMetier`, `updateBienSupport`, `deleteBienSupport`, `updateEvenementRedoute`, `deleteEvenementRedoute`, `updateReferentiel`, `deleteReferentiel`, `rouvrirAtelier1` — toutes suivent exactement le même schéma que les fonctions `create...` déjà présentes, juste avec `method: 'PUT'`/`'DELETE'` et l'ID dans l'URL.

**Instruction de démarrage pour la prochaine session** : "Lis PROJECT_CONTEXT.md en entier. Le chantier 'Liberté de modification' (réouverture + modifier/supprimer sur les 4 types, backend ET frontend) est terminé et vérifié (build backend + build frontend verts). Rien n'est en attente sur ce chantier. Voir section 'Mise à jour — Frontend liberté de modification terminé' en fin de document pour le détail et la prochaine action."

*Fin du contexte. Ne pas redemander de validation sur les Phases 1/1.5/2/3, la direction visuelle frontend, les décisions de modélisation du Socle de Sécurité, ni la décision de liberté de modification déjà actées, sauf ambiguïté réelle.*

---

## Mise à jour — Frontend liberté de modification terminé

Fait dans cette session, build frontend vert vérifié après chaque étape (`npm run build`, `tsc -b && vite build`), aucune étape non testée.

1. **`src/lib/api.ts`** : 9 fonctions ajoutées (`rouvrirAtelier1`, `updateValeurMetier`, `deleteValeurMetier`, `updateBienSupport`, `deleteBienSupport`, `updateEvenementRedoute`, `deleteEvenementRedoute`, `updateReferentiel`, `deleteReferentiel`), même schéma que les `create...` existantes.
2. **`src/pages/AtelierPage.tsx`** :
   - Bouton "Rouvrir l'atelier" (visible si `statut === 'Validee'`, à côté du lien PDF), avec `window.confirm` d'avertissement.
   - `ValeursMetierSection`, `BiensSupportSection`, `EvenementsRedoutesSection` : mode édition inline par ligne (bouton "Modifier" → champs pré-remplis + OK/Annuler) et bouton "Suppr." avec `window.confirm`. Même pattern répété à l'identique dans les 3 sections.
   - `SocleSection` : édition/suppression sur chaque `ReferentielApplicable` (nom, état, état actuel — le thème et le code ISO ne sont pas éditables, cohérent avec le backend qui ne les expose pas en modification).

**Convention établie pendant cette session, à réutiliser** : patchs ciblés via script Python (bloc `old`/`new` avec `assert` avant remplacement) plutôt que réécriture complète du fichier à chaque étape — plus économe en tokens que la convention précédente ("toujours tout réécrire"), tant que chaque bloc `old` est recopié caractère pour caractère depuis le dernier `cat` confirmé. Si un `assert` échoue, revenir à la réécriture complète en dernier recours (ancienne convention, toujours valable comme filet de sécurité).

**Chantier "Liberté de modification" (backend + frontend) : terminé à 100%.**

### État réel consolidé (mis à jour)

| Élément | Statut |
|---|---|
| Backend : réouverture + modifier/supprimer (4 types) | ✅ Terminé, vérifié |
| Frontend : réouverture + modifier/supprimer (4 types) | ✅ Terminé, vérifié (build vert) |
| Socle de sécurité : ISO 27001 + référentiels libres + État actuel | ✅ Fonctionnel |
| Tests automatisés (Playwright/xUnit) | ⬜ Non commencé, non priorisé |
| Slice 2 (Atelier 2) | ⬜ Non commencé |

### Test manuel navigateur — pas encore fait

Aucun test dans un vrai navigateur n'a été effectué pour cette fonctionnalité (édition/suppression/réouverture) — uniquement `npm run build` (compilation TypeScript + bundle Vite, pas un test fonctionnel). À faire avant de considérer le chantier réellement validé de bout en bout : ouvrir une étude, tester modifier/supprimer sur chacun des 4 types, tester la réouverture après validation.

### Prochaine action

Deux options, aucune priorité tranchée : (1) test manuel navigateur du chantier qui vient d'être terminé, (2) démarrage du Slice 2 (Atelier 2 — Sources de risque), qui reprendra la même méthode (Domain d'abord, endpoints, puis frontend) que le Slice 1.

---

## Annexe — Contenu actuel des fichiers clés (snapshot texte)

Cette section évite à une nouvelle session de redemander `cat` sur ces fichiers. **Toujours vérifier avec `cat`/`diff` avant de patcher** si un doute existe (l'utilisateur peut avoir fait des changements entre-temps) — mais en l'absence de doute, ce contenu est fiable et à jour au moment de cette mise à jour du contexte.

### Backend — `src/EbiosRM.Api/Modules/CoreEngine/Domain/Cadrage/Etude.cs`

```csharp
namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum StatutEtude
{
    Brouillon,
    EnCours,
    Validee
}

public sealed class Etude
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public string Perimetre { get; private set; } = default!;
    public string VersionReferentielId { get; private set; } = default!;
    public StatutEtude Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private Etude() { }

    public static Etude Creer(string nom, string perimetre)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de l'étude est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(perimetre))
            throw new ArgumentException("Le périmètre de l'étude est obligatoire.", nameof(perimetre));

        return new Etude
        {
            Id = Guid.NewGuid(),
            Nom = nom.Trim(),
            Perimetre = perimetre.Trim(),
            VersionReferentielId = "EBIOS_RM_V1",
            Statut = StatutEtude.Brouillon,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void DemarrerAtelier1()
    {
        if (Statut != StatutEtude.Brouillon)
            throw new InvalidOperationException(
                $"Impossible de démarrer l'atelier 1 : l'étude est déjà au statut '{Statut}'.");
        Statut = StatutEtude.EnCours;
    }

    public void ValiderAtelier1()
    {
        if (Statut != StatutEtude.EnCours)
            throw new InvalidOperationException(
                $"Impossible de valider l'atelier 1 : l'étude doit être 'EnCours' (statut actuel : '{Statut}').");
        Statut = StatutEtude.Validee;
    }

    public void RouvrirAtelier1()
    {
        if (Statut != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de rouvrir l'atelier 1 : l'étude doit être 'Validee' (statut actuel : '{Statut}').");
        Statut = StatutEtude.EnCours;
    }
}
```

### Backend — `.../Domain/Cadrage/ValeurMetier.cs`

```csharp
namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public sealed class ValeurMetier
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public string Description { get; private set; } = default!;
    public string EntiteResponsable { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private ValeurMetier() { }

    public static ValeurMetier Creer(Guid etudeId, string description, string entiteResponsable)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("La valeur métier doit être rattachée à une étude existante.", nameof(etudeId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la valeur métier est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        return new ValeurMetier
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            Description = description.Trim(),
            EntiteResponsable = entiteResponsable.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(string description, string entiteResponsable)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la valeur métier est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        Description = description.Trim();
        EntiteResponsable = entiteResponsable.Trim();
    }
}
```

### Backend — `.../Domain/Cadrage/BienSupport.cs`

```csharp
namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum TypeBienSupport
{
    SystemeInformation,
    Reseau,
    RessourcesHumaines,
    Local
}

public sealed class BienSupport
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid ValeurMetierId { get; private set; }
    public string Description { get; private set; } = default!;
    public TypeBienSupport Type { get; private set; }
    public string EntiteResponsable { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private BienSupport() { }

    public static BienSupport Creer(Guid etudeId, Guid valeurMetierId, string description, TypeBienSupport type, string entiteResponsable)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être rattaché à une étude.", nameof(etudeId));
        if (valeurMetierId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être associé à une valeur métier (INV7).", nameof(valeurMetierId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du bien support est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        return new BienSupport
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ValeurMetierId = valeurMetierId,
            Description = description.Trim(),
            Type = type,
            EntiteResponsable = entiteResponsable.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(string description, TypeBienSupport type, string entiteResponsable)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du bien support est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        Description = description.Trim();
        Type = type;
        EntiteResponsable = entiteResponsable.Trim();
    }
}
```

### Backend — `.../Domain/Cadrage/EvenementRedoute.cs`

```csharp
namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public sealed class EvenementRedoute
{
    public const int GraviteMin = 1;
    public const int GraviteMax = 4;

    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid ValeurMetierId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Gravite { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private EvenementRedoute() { }

    public static EvenementRedoute Creer(Guid etudeId, Guid valeurMetierId, string description, int gravite)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("L'événement redouté doit être rattaché à une étude.", nameof(etudeId));
        if (valeurMetierId == Guid.Empty)
            throw new ArgumentException("L'événement redouté doit être rattaché à une valeur métier.", nameof(valeurMetierId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement redouté est obligatoire.", nameof(description));
        if (gravite < GraviteMin || gravite > GraviteMax)
            throw new ArgumentOutOfRangeException(nameof(gravite), gravite,
                $"La gravité doit être comprise entre {GraviteMin} et {GraviteMax} (échelle EBIOS RM, INV8).");

        return new EvenementRedoute
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ValeurMetierId = valeurMetierId,
            Description = description.Trim(),
            Gravite = gravite,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void RecoterGravite(int nouvelleGravite)
    {
        if (nouvelleGravite < GraviteMin || nouvelleGravite > GraviteMax)
            throw new ArgumentOutOfRangeException(nameof(nouvelleGravite), nouvelleGravite,
                $"La gravité doit être comprise entre {GraviteMin} et {GraviteMax} (échelle EBIOS RM, INV8).");
        Gravite = nouvelleGravite;
    }

    public void ModifierDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement redouté est obligatoire.", nameof(description));
        Description = description.Trim();
    }
}
```

### Backend — `.../Domain/Cadrage/SocleSecurite.cs`

```csharp
namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum EtatConformite
{
    Conforme,
    NonConforme,
    NonApplicable
}

public sealed class ReferentielApplicable
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public EtatConformite Etat { get; private set; }
    public string? Theme { get; private set; }
    public string? CodeControle { get; private set; }
    public string? EtatActuel { get; private set; }

    private ReferentielApplicable() { }

    public static ReferentielApplicable Creer(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du référentiel est obligatoire.", nameof(nom));

        return new ReferentielApplicable
        {
            Nom = nom.Trim(),
            Etat = etat,
            Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim(),
            CodeControle = string.IsNullOrWhiteSpace(codeControle) ? null : codeControle.Trim(),
            EtatActuel = string.IsNullOrWhiteSpace(etatActuel) ? null : etatActuel.Trim()
        };
    }

    public void Modifier(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du référentiel est obligatoire.", nameof(nom));

        Nom = nom.Trim();
        Etat = etat;
        Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim();
        CodeControle = string.IsNullOrWhiteSpace(codeControle) ? null : codeControle.Trim();
        EtatActuel = string.IsNullOrWhiteSpace(etatActuel) ? null : etatActuel.Trim();
    }
}

public sealed class SocleSecurite
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    private readonly List<ReferentielApplicable> _referentiels = new();
    public IReadOnlyList<ReferentielApplicable> Referentiels => _referentiels;

    private SocleSecurite() { }

    public static SocleSecurite Creer(Guid etudeId)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le socle de sécurité doit être rattaché à une étude.", nameof(etudeId));
        return new SocleSecurite { Id = Guid.NewGuid(), EtudeId = etudeId };
    }

    public void AjouterReferentiel(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        _referentiels.Add(ReferentielApplicable.Creer(nom, etat, theme, codeControle, etatActuel));
    }

    public void ModifierReferentiel(Guid referentielId, string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        var referentiel = _referentiels.FirstOrDefault(r => r.Id == referentielId);
        if (referentiel is null)
            throw new ArgumentException("Référentiel introuvable dans ce socle de sécurité.", nameof(referentielId));
        referentiel.Modifier(nom, etat, theme, codeControle, etatActuel);
    }

    public void SupprimerReferentiel(Guid referentielId)
    {
        var referentiel = _referentiels.FirstOrDefault(r => r.Id == referentielId);
        if (referentiel is null)
            throw new ArgumentException("Référentiel introuvable dans ce socle de sécurité.", nameof(referentielId));
        _referentiels.Remove(referentiel);
    }
}
```

### Backend — `src/EbiosRM.Api/Program.cs` (fichier complet, 26 endpoints, vérifié 0 Warning/0 Error)

Routes disponibles, méthode + chemin :
```
GET    /api/v1/health
POST   /api/v1/etudes
GET    /api/v1/etudes/{id}
GET    /api/v1/etudes
POST   /api/v1/etudes/{id}/demarrer-atelier1
POST   /api/v1/etudes/{id}/valider-atelier1
POST   /api/v1/etudes/{id}/rouvrir-atelier1
POST   /api/v1/etudes/{etudeId}/valeurs-metier
GET    /api/v1/etudes/{etudeId}/valeurs-metier
PUT    /api/v1/etudes/{etudeId}/valeurs-metier/{id}
DELETE /api/v1/etudes/{etudeId}/valeurs-metier/{id}
POST   /api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetierId}/biens-support
GET    /api/v1/etudes/{etudeId}/biens-support
PUT    /api/v1/etudes/{etudeId}/biens-support/{id}
DELETE /api/v1/etudes/{etudeId}/biens-support/{id}
POST   /api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetierId}/evenements-redoutes
GET    /api/v1/etudes/{etudeId}/evenements-redoutes
PUT    /api/v1/etudes/{etudeId}/evenements-redoutes/{erId}/gravite
PUT    /api/v1/etudes/{etudeId}/evenements-redoutes/{erId}
DELETE /api/v1/etudes/{etudeId}/evenements-redoutes/{erId}
POST   /api/v1/etudes/{etudeId}/socle-securite
POST   /api/v1/etudes/{etudeId}/socle-securite/referentiels
PUT    /api/v1/etudes/{etudeId}/socle-securite/referentiels/{referentielId}
DELETE /api/v1/etudes/{etudeId}/socle-securite/referentiels/{referentielId}
GET    /api/v1/etudes/{etudeId}/socle-securite
GET    /api/v1/etudes/{etudeId}/rapports/atelier1
```

Records de requête (`Program.cs`, en bas de fichier) :
```csharp
record CreerEtudeRequest(string Nom, string Perimetre);
record CreerValeurMetierRequest(string Description, string EntiteResponsable);
record CreerBienSupportRequest(string Description, string Type, string EntiteResponsable);
record CreerEvenementRedouteRequest(string Description, int Gravite);
record RecoterGraviteRequest(int NouvelleGravite);
record AjouterReferentielRequest(string Nom, string Etat, string? Theme = null, string? CodeControle = null, string? EtatActuel = null);
```

Le `PUT .../biens-support/{id}` et `PUT .../valeurs-metier/{id}` réutilisent `CreerBienSupportRequest`/`CreerValeurMetierRequest` (mêmes champs, pas de record dédié). Le `PUT .../evenements-redoutes/{erId}` réutilise `CreerEvenementRedouteRequest` (Description + Gravite combinées, si `request.Gravite != er.Gravite` alors `RecoterGravite` est aussi appelée). Le `PUT .../socle-securite/referentiels/{referentielId}` réutilise `AjouterReferentielRequest`.

*(Le corps intégral du fichier Program.cs a été donné dans cette conversation lors de la dernière réécriture complète, section "Liberté de modification pour l'analyste" — non redupliqué ici pour la longueur, mais chaque endpoint suit le schéma standard déjà établi : `ObtenirParIdAsync` → vérif null/EtudeId → `try/catch (ArgumentException)` → méthode domaine → `MettreAJourAsync`/`SupprimerAsync` → `Results.Ok`/`NoContent`/`BadRequest`.)*

### Frontend — `frontend/src/lib/api.ts` (état actuel, AVANT les 9 fonctions du "reste à faire")

```typescript
var API_BASE = 'http://localhost:5197/api/v1'

export interface Etude {
  id: string
  nom: string
  perimetre: string
  versionReferentielId: string
  statut: 'Brouillon' | 'EnCours' | 'Validee'
  creeLeUtc: string
}

export interface ValeurMetier {
  id: string
  etudeId: string
  description: string
  entiteResponsable: string
  creeLeUtc: string
}

export interface BienSupport {
  id: string
  etudeId: string
  valeurMetierId: string
  description: string
  type: string
  entiteResponsable: string
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

async function apiFetch(path: string, options?: RequestInit): Promise<any> {
  var response = await fetch(API_BASE + path, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (response.status === 404) {
    return null
  }

  var body = null
  var text = await response.text()
  if (text) {
    body = JSON.parse(text)
  }

  if (!response.ok) {
    var message = body && body.error ? body.error : 'Erreur API (' + response.status + ')'
    throw new ApiError(response.status, message)
  }

  return body
}

export function listEtudes(): Promise<Etude[]> {
  return apiFetch('/etudes')
}

export function getEtude(id: string): Promise<Etude | null> {
  return apiFetch('/etudes/' + id)
}

export function createEtude(nom: string, perimetre: string): Promise<Etude> {
  return apiFetch('/etudes', {
    method: 'POST',
    body: JSON.stringify({ nom: nom, perimetre: perimetre }),
  })
}

export function demarrerAtelier1(etudeId: string): Promise<Etude> {
  return apiFetch('/etudes/' + etudeId + '/demarrer-atelier1', { method: 'POST' })
}

export function validerAtelier1(etudeId: string): Promise<{ etude: Etude; snapshotVersion: number }> {
  return apiFetch('/etudes/' + etudeId + '/valider-atelier1', { method: 'POST' })
}

export function listValeursMetier(etudeId: string): Promise<ValeurMetier[]> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier')
}

export function listBiensSupport(etudeId: string): Promise<BienSupport[]> {
  return apiFetch('/etudes/' + etudeId + '/biens-support')
}

export function listEvenementsRedoutes(etudeId: string): Promise<EvenementRedoute[]> {
  return apiFetch('/etudes/' + etudeId + '/evenements-redoutes')
}

export function getSocleSecurite(etudeId: string): Promise<SocleSecurite | null> {
  return apiFetch('/etudes/' + etudeId + '/socle-securite')
}

export function rapportAtelier1Url(etudeId: string): string {
  return API_BASE + '/etudes/' + etudeId + '/rapports/atelier1'
}

export function createValeurMetier(etudeId: string, description: string, entiteResponsable: string): Promise<ValeurMetier> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier', {
    method: 'POST',
    body: JSON.stringify({ description: description, entiteResponsable: entiteResponsable }),
  })
}

export function createBienSupport(etudeId: string, valeurMetierId: string, description: string, type: string, entiteResponsable: string): Promise<BienSupport> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + valeurMetierId + '/biens-support', {
    method: 'POST',
    body: JSON.stringify({ description: description, type: type, entiteResponsable: entiteResponsable }),
  })
}

export function createEvenementRedoute(etudeId: string, valeurMetierId: string, description: string, gravite: number): Promise<EvenementRedoute> {
  return apiFetch('/etudes/' + etudeId + '/valeurs-metier/' + valeurMetierId + '/evenements-redoutes', {
    method: 'POST',
    body: JSON.stringify({ description: description, gravite: gravite }),
  })
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
```

### Frontend — `frontend/src/components/shared/InlineForm.tsx` (inchangé depuis sa création)

```tsx
import { useState } from 'react'
import { Plus, X } from 'lucide-react'

export default function InlineForm(props: { label: string; children: (fermer: () => void) => React.ReactNode }) {
  var [ouvert, setOuvert] = useState(false)

  if (!ouvert) {
    return (
      <button onClick={function () { setOuvert(true) }} className="mt-3 flex items-center gap-1.5 font-mono text-[11px] font-medium text-signature hover:underline">
        <Plus size={12} />
        {props.label}
      </button>
    )
  }

  return (
    <div className="mt-3 border border-paper-line p-4">
      <div className="mb-3 flex items-center justify-between">
        <span className="font-mono text-[10px] tracking-wide text-steel-light">{props.label.toUpperCase()}</span>
        <button onClick={function () { setOuvert(false) }} className="text-steel-light hover:text-ink">
          <X size={14} />
        </button>
      </div>
      {props.children(function () { setOuvert(false) })}
    </div>
  )
}
```

### Frontend — `frontend/src/pages/AtelierPage.tsx` (état actuel complet, AVANT modification/suppression)

Structure : imports (dont `../lib/iso27001` pour `CATALOGUE_ISO_27001`/`THEMES_ISO`/`ControleIso`) → constantes `NOMS_ATELIERS`/`TYPES_BIEN_SUPPORT`/`ETATS_CONFORMITE` → composant `AtelierPage` (charge étude + 4 listes, boutons Démarrer/Valider/Télécharger PDF selon statut) → 4 fonctions de section :
- `ValeursMetierSection` : liste + `InlineForm` d'ajout (description, entité responsable).
- `BiensSupportSection` : liste + `InlineForm` d'ajout (select valeur métier, description, select type, entité responsable).
- `EvenementsRedoutesSection` : liste + `InlineForm` d'ajout (select valeur métier, description, select gravité 1-4).
- `SocleSection` : liste groupée par thème (avec `etatActuel` affiché sous chaque ligne si renseigné) + `InlineForm` d'ajout avec bascule radio "Contrôle ISO 27001" (select groupé par `<optgroup>`, 93 options) / "Autre référentiel" (input libre), select état (Conforme/NonConforme/NonApplicable), textarea "État actuel observé".

Chaque section ne fait qu'ajouter pour l'instant (`createXxx`), aucune n'a encore de mode édition ni de suppression — c'est exactement le travail du "reste à faire". Le contenu ligne par ligne complet a été donné plusieurs fois dans cette conversation lors des réécritures successives ; la dernière version en date est celle où `SocleSection` inclut le champ `etatActuel`.

### Frontend — `frontend/src/lib/iso27001.ts`

Contient `CATALOGUE_ISO_27001` (93 entrées `{ code, theme, nom }`, vérifié 37 Organisationnel + 8 Personnes + 14 Physique + 34 Technologique) et `THEMES_ISO` (tableau ordonné des 4 thèmes). Contenu statique, inchangé depuis sa création — fiable à 100%, ne jamais redemander de `cat` dessus.

---

## Mise à jour — PDF premium (polices + palette) et champ Mission

**Terminé et vérifié** (`dotnet build` vert, `npm run build` vert, PDF réel généré et ouvert visuellement).

### 1. Corrections de données préalables
Accents restaurés dans `iso27001.ts`, doublon Code/Nom supprimé du Socle de Sécurité, tri par code par thème appliqué, `snapshotVersion`/`DateValidationUtc` propagés jusqu'au PDF (déjà dans `RapportAtelier1Data`, lus depuis le snapshot — P16 respecté).

### 2. Polices — Fraunces + IBM Plex Sans/Mono
8 fichiers `.ttf` dans `src/EbiosRM.Api/Assets/Fonts/`. Copiés vers l'output via `<None Include="Assets\Fonts\*.ttf"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` dans `.csproj`. Enregistrés au démarrage via `RegistrerPolices()` dans `Program.cs`.

**Noms de familles réels** (vérifiés `fc-scan`, à réutiliser tels quels) : `Fraunces 72pt`, `Fraunces 72pt SemiBold`, `IBM Plex Sans`, `IBM Plex Sans Medium`, `IBM Plex Sans SemiBold`, `IBM Plex Mono`, `IBM Plex Mono Medium`.

**Piège rencontré, ne pas reproduire** : `QuestPDF.Drawing.FontManager.GetRegisteredFontsFamiliesNames()` n'existe pas (halluciné une fois). En cas de doute sur l'API QuestPDF, vérifier plutôt via `fc-scan` sur les fichiers.

### 3. `Modules/Reporting/RapportAtelier1PdfGenerator.cs` — réécrit
Palette "Bleu France" (`#000091` primaire, `#E3E3FD` clair, `#161616` encre, `#B34000` alerte, `#18753C` conforme). Fraunces SemiBold pour titres, IBM Plex Sans pour corps, IBM Plex Mono pour libellés/codes. En-tête avec version snapshot + date validation. Valeurs métier/Biens supports : un seul tableau zébré 5 colonnes. Socle de sécurité : tableau zébré 3 colonnes (Référentiel/État actuel/État, couleur selon conformité). Helper `CelluleZebra(...)` réutilisable.

### 4. Champ `Mission` ajouté sur `Etude`
Propagé de bout en bout : `Etude.Creer(nom, perimetre, mission)` → migration `AjoutMissionEtude` (appliquée) → snapshot → PDF (section Cadrage) → formulaire `Etudes.tsx` → `api.ts`. Études créées avant la migration ont `Mission = ''` (pas un bug).

### Convention confirmée
Patchs ciblés Python (`old`/`new`/`assert`), y compris multi-fichiers en une commande. Toujours `grep -n`/`sed -n` avant de patcher un fichier jamais vu dans la session en cours.

### Prochaine action (mise à jour)
**Backend Atelier 2 terminé et vérifié à 100%** : migration `AjoutAtelier2SourcesDeRisque` appliquée, CRUD complet testé par curl sur les deux agrégats -- Couples SR/OV (POST/GET/PUT avec recalcul correct de pertinence via ServiceCalculPertinence/DELETE, tous codes HTTP corrects) et Parties Prenantes (POST/GET). PDF A1 revérifié fonctionnel après toutes les modifications de Program.cs.

Reste à faire pour compléter le Slice 2 : (1) frontend Atelier 2 (api.ts + composants, même méthode que Slice 1), (2) RapportAtelier2PdfGenerator selon la structure détaillée donnée par l'utilisateur dans la conversation (intro, tableau parties prenantes 3 col., section méthodo avec 2 tableaux Motivation/Ressources + matrice, tableau récap par thème ISO 6 colonnes x4 thèmes, cartographie de synthèse par niveau de pertinence, listes retenus/sous surveillance).

### Incident majeur résolu -- leçon impérative pour toute session future
Un bug a fait perdre énormément de temps : lors de l'ajout des endpoints Atelier 2 dans Program.cs, un patch antérieur (session précédente) avait laissé 3 lignes orphelines dupliquées juste avant `app.Run()` (`return Results.File(...)`, `});` -- vestige d'un copier-coller mal terminé). Plusieurs tentatives de patch ciblé (Python `old`/`new`/`assert`) ont semblé réussir ("OK" affiché) sans que le changement soit réellement visible ensuite -- cause probable : le texte `old` recherché existait à un autre endroit du fichier que celui visé, donc le remplacement s'appliquait ailleurs sans erreur apparente. La seule sortie fiable a été de demander le fichier `Program.cs` COMPLET (`cat`) et de localiser le problème visuellement/par script sur tout le contenu réel, plutôt que de continuer à patcher à l'aveugle par petits bouts.

**Règle renforcée pour la suite** : si un `assert` de patch Python passe ("OK") mais que le comportement observé (build, curl, contenu du fichier) ne correspond pas au changement attendu, NE PAS retenter un patch ciblé sur le même fichier -- demander directement `cat` du fichier complet et repartir sur un diagnostic visuel intégral. Deux échecs de patch consécutifs sur un même fichier = signal pour changer de méthode immédiatement, pas insister.

---

## Mise à jour -- Slice 2 (Atelier 2) terminé : backend + frontend + PDF

**Décision actée** : pas de snapshot figé pour A2 (ni probablement A3-A5) -- contrairement à A1, ces rapports lisent les agrégats vivants en temps réel (`RapportAtelier2Service` interroge directement `ICoupleSourceRisqueObjectifViseRepository`/`IPartiePrenanteRepository`, pas de `SnapshotAtelier2`). Le snapshot d'A1 reste en place tel quel, non remis en cause. Justification : A2+ sont plus itératifs par nature, et il n'existe pas encore de notion de "validation d'atelier" dans le Workflow Engine pour A2-A5 (seul `valider-atelier1` existe) -- généraliser le snapshot supposerait de généraliser d'abord ce mécanisme, chantier bien plus lourd.

**Frontend Atelier 2** : `PartiesPrenantesSection` et `CouplesSrOvSection` ajoutées dans `AtelierPage.tsx`, débloquées via `estAtelier2 = numero === 2`. Couples groupés par les 4 thèmes ISO (avec message explicite si un thème est vide). `api.ts` enrichi de 8 fonctions (CRUD complet couples + parties prenantes).

**Backend Atelier 2** : `CoupleSourceRisqueObjectifVise` (avec `ServiceCalculPertinence`, matrice Motivation x Ressources statique déjà vérifiée par tests curl) et `PartiePrenante`, CRUD complet testé de bout en bout.

**PDF Atelier 2** (`RapportAtelier2PdfGenerator.cs`, `RapportAtelier2Service.cs`, `RapportAtelier2Data.cs`) : structure conforme à la spécification détaillée donnée par l'utilisateur -- intro, tableau parties prenantes (3 col.), section méthodologie (texte + 2 tableaux Motivation/Ressources + matrice de pertinence statique reproduite en dur dans le générateur), tableau récapitulatif par thème (6 colonnes, 4 sous-tableaux avec titre+description, gère le cas thème vide), cartographie de synthèse (répartition par pertinence avec %), listes couples retenus (Atelier 3) / sous surveillance. Endpoint `GET /api/v1/etudes/{id}/rapports/atelier2`. **Testé en conditions réelles avec le script `/tmp/seed-atelier2.sh` (3 parties prenantes, 5 couples répartis sur les 4 thèmes) -- PDF téléchargé et validé visuellement par l'utilisateur.**

### Incident rencontré et résolu (distinct de celui déjà documenté)
Un premier `cat > RapportAtelier2PdfGenerator.cs << 'EOF' ... EOF` très volumineux ne s'est pas exécuté (ou a échoué silencieusement) alors que `dotnet build` juste après affichait "Build succeeded" -- en réalité ce build ne référençait pas encore la classe (elle n'était pas encore appelée depuis `Program.cs` à ce moment précis de la séquence), donc rien ne pouvait signaler son absence. Le vrai problème n'est apparu qu'à l'étape suivante (câblage DI + endpoint), avec une erreur `CS0246` claire. **Leçon** : après un `cat > gros_fichier.cs`, toujours vérifier son existence via `ls`/`grep -n "class NomDeLaClasse"` avant de considérer l'étape terminée -- ne pas se fier uniquement à un `dotnet build` vert si le fichier créé n'est pas encore référencé ailleurs dans le projet à ce stade.

### Prochaine action
Slice 2 (Atelier 2) complet : backend + frontend + PDF, tout vérifié. Décision "pas de snapshot pour A2+" à réappliquer par défaut pour A3/A4/A5 sauf contre-indication. Reste non tranché : démarrer le Slice 3 (Atelier 3 -- Scénarios stratégiques), ou traiter des chantiers transverses en attente (test manuel navigateur, intro/conclusion PDF).

*Fin du contexte.*

---

## Mise à jour — Recherche documentaire officielle EBIOS RM (dossier `Sources/`) et modèle cible pour les Ateliers 3-5

**Nature de cette session : documentaire, pas de code touché.** Travail effectué en dehors du dépôt de code (dans un répertoire de scratchpad), aboutissant à deux livrables PDF publiés dans `docs/architecture/`. Aucun fichier de `src/` ou `frontend/` n'a été modifié. Cette section documente une **directive de méthode** et un **modèle de domaine cible corrigé pour les Ateliers 3, 4 et 5 (pas encore codés)** — à utiliser comme spécification de référence quand le Slice 3 démarrera.

### Directive de méthode actée (à appliquer dans toute session future touchant au domaine métier EBIOS)

Sur instruction explicite de l'utilisateur : **le code existant et la connaissance générale du modèle EBIOS RM ne sont pas des vérités absolues.** Le dossier `/home/joel/Documents/EbiosRM/Sources/` (13 PDF officiels/pédagogiques ANSSI, dont `EBIOS Risk Manager - Fiches (1).pdf`, 85 pages, et les fiches par atelier `Atelier+1...5`) fait foi. Toute modélisation de domaine (nouveaux agrégats, cardinalités, énumérations) doit être vérifiée par citation exacte de ce dossier avant d'être actée, plutôt que déduite du code déjà écrit ou de connaissances générales sur EBIOS RM.

### Pourquoi cette vérification a eu lieu

Un audit UML complet (diagrammes cas d'utilisation, classes couvrant les 5 ateliers, composants, séquence, état, déploiement) a été construit avec l'utilisateur en partant du code existant (Ateliers 1-2 réels) et de complétions raisonnées pour les Ateliers 3-5 (pas encore codés). Plusieurs incohérences ont été détectées **par l'utilisateur lui-même en posant des questions de compréhension** (pas par une relecture automatique), ce qui a motivé la vérification systématique contre la documentation officielle plutôt que de continuer à raisonner par déduction.

### Corrections actées sur le modèle cible (Ateliers 3-5, confirmées contre les PDF officiels)

Ces points **ne sont pas encore implémentés en code** (seuls A1/A2 existent) mais font désormais foi comme spécification pour le Slice 3 et suivants :

1. **Chaîne de traçabilité précisée** (la version résumée en §4.2 de ce document était déjà correcte dans ses grandes lignes ; ce qui suit en est le détail officiel) :
   ```
   Couple SR/OV (A2)
     ⇒ 1 ScénarioStratégique (relation 1:1 stricte)
          ⇒ N CheminAttaque
               ⇒ 1 ScénarioOpérationnel par CheminAttaque (relation 1:1 stricte)
                    ⇒ 1 ScénarioDeRisque = CheminAttaque + ScénarioOpérationnel,
                       évalué avant ET après application du PACS (résiduel)
   ```
   `ScénarioDeRisque` remplace ce qui avait été envisagé comme "RisqueResiduel" dans les brouillons de diagramme — c'est le même objet évalué à deux moments (avant/après traitement), pas deux classes séparées.

2. **Un `ScénarioStratégique` peut attaquer directement un `BienSupport`/`ValeurMétier` sans traverser aucune `PartiePrenante`** (attaque directe, sans passer par l'écosystème). Cardinalité corrigée : `ScénarioStratégique` ↔ `PartiePrenante` est **0..N**, pas 1..N. Un `ÉvénementIntermédiaire` (EI) est créé par franchissement de `PartiePrenante`, donc 0 si attaque directe.

3. **Un `BienSupport` peut être partagé par plusieurs `ValeurMétier`**, et une `ValeurMétier` peut exister sans aucun `BienSupport` rattaché (cardinalité corrigée en many-to-many optionnelle, pas 1:N stricte).

4. **PACS (Plan d'Amélioration Continue de la Sécurité)** — nom officiel confirmé (remplace tout usage antérieur de "PlanTraitementRisque"). Structuré en **4 axes fixes obligatoires** : Gouvernance, Protection, Défense, Résilience. Chaque `MesurePACS` est rattachée à l'un de ces 4 axes.

5. **Le risque résiduel peut être accepté sans traitement complémentaire** si jugé faible (déjà noté comme invariant en §6.5 de ce document — confirmé conforme à la doctrine officielle, pas une exception à ajouter).

6. **Modèle d'attaque propre à EBIOS RM** : CONNAÎTRE → RENTRER → TROUVER → EXPLOITER (4 phases), **distinct du Cyber Kill Chain de Lockheed Martin (7 phases)** qui avait été utilisé par erreur dans une première ébauche de diagramme de composant/séquence. Toute modélisation future d'`ActionÉlémentaire`/`CheminAttaque` doit utiliser ce modèle à 4 phases, pas le Kill Chain.

7. **Formule officielle du niveau de menace** (module Écosystème, A3) : `NiveauMenace = (Dépendance × Pénétration) / (Maturité cyber × Confiance)`. À utiliser telle quelle si/quand `ServiceEvaluationMenaceEcosysteme` est implémenté.

8. **Structure itérative officielle de la méthode, pas seulement linéaire A1→A5** : l'Atelier 5 peut renvoyer vers l'Atelier 1 (cycle stratégique complet — reprise du cadrage) ou vers l'Atelier 2 (cycle opérationnel partiel — reprise sources de risque/écosystème sans tout refaire). Ce n'est pas une correction d'erreur mais un fonctionnement normal de la méthode (cf. `05b-state-etude.puml`, transition `Finalisee → EnCours`). Le futur Workflow Engine des Ateliers 2-5 (aujourd'hui inexistant, cf. §11 "Décision actée" sur le Workflow minimal) devra pouvoir représenter cette reprise ciblée, pas seulement un statut global linéaire.

9. **Les rôles/acteurs (Direction, RSSI, Métiers, etc., §4.3/§9 de ce document) sont une simplification RBAC volontaire du diagramme de cas d'utilisation**, pas une correspondance littérale et exhaustive avec les acteurs cités dans la documentation officielle — à garder en tête si un futur écran de gestion des droits est construit, pour ne pas sur-interpréter cette liste comme une nomenclature officielle figée.

### Principe de conception à appliquer à toute énumération EBIOS RM exposée à l'analyste (directive actée, transverse)

**Toute énumération métier présentée à l'analyste doit systématiquement inclure une option "Autre"**, pour lui permettre de saisir une valeur qui n'existe pas encore dans la liste. C'est déjà le cas dans le code actuel pour `CategorieSourceRisque` et `CategorieObjectifVise` (`Modules/CoreEngine/Domain/SourcesRisque/CoupleSourceRisqueObjectifVise.cs`) — ce pattern doit être reproduit pour **toute nouvelle énumération** introduite lors du Slice 3 et suivants (types de mesures PACS, types d'actions élémentaires, etc.).

### Point résolu — `CategorieSourceRisque`/`CategorieObjectifVise` corrigées contre la documentation officielle

**Tranché et implémenté** (agent de recherche en arrière-plan abandonné pour cette tâche, cf. incident ci-dessous — résolu par lecture directe des sources à la place). Deux sources concordantes ont servi de référence : un extrait cité par l'utilisateur du guide officiel ANSSI (grille des catégories de sources de risque, profils d'attaquants) et le support pédagogique `Sources/9 ATELIER+2+Source+des+risques.pdf` (page 3, "Exemple de catégories SR et OV"), qui donne la même liste avec les mêmes intitulés.

**Catégories officielles de sources de risque (SR)** — 8 profils d'attaquants : Étatique, Crime organisé, Terroriste, Activiste idéologique, Officine spécialisée, Amateur, Vengeur, **Malveillant pathologique** (catégorie manquante dans le code — sa description officielle cite explicitement "concurrent déloyal, client malhonnête, escroc, fraudeur" comme exemples, donc "Concurrent" n'est PAS une catégorie à part entière mais un exemple à l'intérieur de "Malveillant pathologique").

**Catégories officielles d'objectifs visés (OV)** — 7 catégories : Espionnage étatique ou industriel (vol de secrets), Pré-positionnement stratégique, Influence/déstabilisation/atteinte à l'image, Entrave au fonctionnement, Sabotage/destruction, Lucratif (fraude, détournement d'usage), Défi/amusement.

**Découverte importante en vérifiant** : le frontend (`frontend/src/pages/AtelierPage.tsx`, `CATEGORIES_SR`/`CATEGORIES_OV`, lignes ~719-720) utilisait déjà ces intitulés officiels corrects, alors que le backend (`CoupleSourceRisqueObjectifVise.cs`) avait encore les anciens noms (`Activiste`, `Concurrent`, `VengeurInterne`, `AmateurOpportuniste` côté SR ; `Espionnage`, `Ideologique`, `Ludique`, `Destabilisation`, `Vengeance` côté OV — dont plusieurs n'étaient d'ailleurs pas des catégories officielles du tout). **C'était un bug fonctionnel réel et actif** : `Program.cs` fait un `Enum.TryParse<CategorieSourceRisque>`/`Enum.TryParse<CategorieObjectifVise>` sur la chaîne envoyée par le frontend (lignes ~542-546 et ~582-586) — sélectionner "Activiste idéologique", "Amateur", "Vengeur", "Malveillant pathologique", ou n'importe quelle catégorie OV autre que "Lucratif" dans le formulaire d'ajout de couple SR/OV renvoyait un `400 Bad Request` côté backend. Non détecté plus tôt car les tests curl/seed n'avaient utilisé que des catégories qui coïncidaient par hasard des deux côtés (`CrimeOrganise`/`Lucratif`).

**Correctif appliqué** (build backend + build frontend vérifiés verts) :
- `CoupleSourceRisqueObjectifVise.cs` : `CategorieSourceRisque` = `Etatique, CrimeOrganise, Terroriste, ActivisteIdeologique, OfficineSpecialisee, Amateur, Vengeur, MalveillantPathologique, Autre`. `CategorieObjectifVise` = `EspionnageEtatiqueOuIndustriel, PrePositionnementStrategique, InfluenceDestabilisation, EntraveAuFonctionnement, SabotageDestruction, Lucratif, DefiAmusement, Autre`.
- `frontend/src/pages/AtelierPage.tsx` : `'Autre'` ajouté en fin de `CATEGORIES_SR` et `CATEGORIES_OV` (absent des deux tableaux jusqu'ici — appliqué le principe "option Autre systématique" ci-dessus, qui n'était donc pas encore respecté côté frontend malgré l'être côté backend).
- Aucune migration nécessaire : colonnes `SourceRisque`/`ObjectifVise` mappées en `HasConversion<string>()` (varchar), pas de contrainte `CHECK` en base. Données de dev vérifiées (`docker exec ebiosrm-postgres psql ...`) : une seule paire de test existante, `CrimeOrganise`/`Lucratif`, dont les noms sont inchangés par ce correctif — aucune donnée cassée.
- Aucun autre fichier ne référence les noms des membres de l'enum en dur (`Program.cs` utilise `Enum.TryParse` générique ; PDF Atelier 2 groupe par thème ISO, pas par catégorie SR/OV) — recherche confirmée par grep sur tout le dépôt.
- **Bug additionnel corrigé au passage** : ni la liste à l'écran (`AtelierPage.tsx`) ni le PDF (`RapportAtelier2PdfGenerator.cs`) n'affichaient la description libre (`DescriptionSourceRisque`/`DescriptionObjectifVise`, pourtant obligatoire à la saisie) — un couple en catégorie "Autre" se serait donc affiché comme "Autre -- Autre" sans aucun contenu lisible, rendant l'option inutilisable en lecture malgré sa présence dans le formulaire. Corrigé par un libellé calculé qui bascule sur la description libre quand la catégorie vaut "Autre" : `RapportAtelier2Data.cs` gagne `LibelleSourceRisque`/`LibelleObjectifVise` (propriétés calculées sur le record `CoupleSrOvData`, alimentées par `RapportAtelier2Service.cs`), logique équivalente en ligne côté `AtelierPage.tsx`. Propagé au tableau PDF et aux listes retenus/sous surveillance. Build backend + frontend revérifiés verts après ce correctif.
- **Ajustement UX demandé par l'utilisateur après relecture** : le champ de description existait déjà pour toutes les catégories (donc techniquement présent pour "Autre"), mais rien ne le signalait visuellement comme *le* champ de saisie de la nouvelle catégorie. Corrigé dans `AtelierPage.tsx` (`CouplesSrOvSection`) : quand `sourceRisque`/`objectifVise` vaut `'Autre'`, le placeholder du champ correspondant change ("Précisez la catégorie de source de risque" / "... d'objectif visé" au lieu de "Description de la source de risque"/"... de l'objectif visé") et sa bordure passe en bleu signature (`border-signature`) au lieu du gris neutre habituel -- un seul champ par catégorie, pas de doublon caché. Aucun nouveau champ backend nécessaire (réutilise `DescriptionSourceRisque`/`DescriptionObjectifVise` existants). Build frontend revérifié vert.

### Incident — fiabilité des agents de recherche en arrière-plan (fork) sur les tâches d'extraction documentaire longues

Sur cette tâche précise (extraire toutes les énumérations d'un PDF de 85 pages avec citations de page), les agents fork lancés en arrière-plan ont échoué de façon variée : complétion avec 0 utilisation d'outil (juste un message de statut recopié), ou complétion après un nombre d'outils significatif mais avec un résultat qui reformule la conversation précédente au lieu de rapporter un contenu réellement lu dans le PDF. **Leçon** : pour une extraction factuelle longue et précise à partir d'un document source, un prompt très explicite ("tu dois utiliser l'outil Read maintenant, ceci n'est pas un rapport de statut", chemins de fichiers exacts, plages de pages exactes) réduit le risque mais ne l'élimine pas — vérifier le nombre réel d'utilisations d'outils dans la notification de complétion avant de faire confiance au résultat, et en dernier recours, lire soi-même les pages concernées plutôt que de redéléguer indéfiniment.

### Livrables produits (documentation, hors code)

- `docs/architecture/EBIOS-RM-diagrammes-UML.pdf` — 8 pages, A3 paysage. Diagrammes UML professionnels (PlantUML + Graphviz, pas de notation Mermaid) : cas d'utilisation, classes (5 ateliers + transverse Snapshot/Reporting), composants, séquence, état (Étude, Atelier, MesurePACS), déploiement, vue d'ensemble avec boucles itératives A5→A1/A5→A2.
- `docs/architecture/EBIOS-RM-explique-simplement.pdf` — 16 pages, A4 portrait. Document pédagogique (monolithe vs microservices, choix PostgreSQL, C# vs Python, guide de lecture pas à pas pour chaque diagramme), synchronisé avec le PDF UML ci-dessus.
- Ces deux PDF constituent la **spécification de référence du modèle cible pour les Ateliers 3-5** — à consulter avant de coder le Slice 3, en complément de ce fichier.

### Prochaine action

Le point `CategorieSourceRisque`/`CategorieObjectifVise` est **résolu et corrigé** (cf. ci-dessus) — bug de désalignement frontend/backend corrigé au passage, builds vérifiés verts. Reste ouvert :
1. **Test manuel navigateur** du formulaire "Ajouter un couple SR/OV" (Atelier 2) pour confirmer que les catégories auparavant cassées (Activiste idéologique, Amateur, Vengeur, Malveillant pathologique, et les catégories OV hors Lucratif) fonctionnent bien de bout en bout maintenant.
2. **Code** : démarrer le Slice 3 (Atelier 3 — Scénarios stratégiques) en s'appuyant sur le modèle cible corrigé plus haut dans cette section (`ScénarioStratégique` 1:1 avec couple SR/OV, `PartiePrenante` 0..N, `CheminAttaque` 1..N, formule de menace).
3. Appliquer le principe "option Autre systématique" à toute nouvelle énumération introduite dans le Slice 3.

---

## Mise à jour — Validation de l'Atelier 2 (statut dédié) + téléchargement PDF toujours disponible

**Terminé et vérifié de bout en bout par curl** (build backend + frontend verts, migration appliquée, flux complet testé sur l'API réelle après redémarrage du process).

### Décision actée

Le point "à réévaluer à l'ouverture du Slice 2" (§11) est tranché : plutôt qu'une généralisation du Workflow Engine (statut par atelier générique), on reproduit **le pattern déjà existant d'Atelier 1**, dupliqué pour l'Atelier 2 seulement (pas de sur-ingénierie anticipée pour A3-A5, qui n'existent pas encore) :

- `Etude` gagne un second champ de statut, `StatutAtelier2` (réutilise l'enum `StatutEtude` existant -- pas de nouvel enum dupliqué), indépendant de `Statut` (qui reste dédié à l'Atelier 1).
- Méthodes symétriques : `DemarrerAtelier2()` (exige `Statut == Validee`, c.-à-d. Atelier 1 déjà validé -- dépendance méthodologique réelle, A2 a besoin des valeurs métier/ER d'A1), `ValiderAtelier2()`, `RouvrirAtelier2()` (pas de snapshot à préserver, cohérent avec la décision déjà actée "pas de snapshot pour A2").
- `ServiceValidationCompletudeAtelier2` (nouveau, `Modules/CoreEngine/Domain/SourcesRisque/`) : complétude = au moins un couple SR/OV existant (l'unique sortie réelle de cet atelier). Réutilise le record `ResultatValidationCompletude` déjà défini pour A1 plutôt que d'en dupliquer un.
- 3 nouveaux endpoints, même schéma qu'A1 : `POST /demarrer-atelier2`, `POST /valider-atelier2` (400 + `elementsManquants` si incomplet), `POST /rouvrir-atelier2`.
- Migration `AjoutStatutAtelier2` (colonne `StatutAtelier2 varchar(50)`, defaultValue corrigé manuellement de `""` à `"Brouillon"` après génération -- EF Core génère `""` par défaut pour une colonne string non-nullable ajoutée, ce qui aurait cassé la désérialisation de l'enum pour toute étude existante).

### Frontend

`AtelierPage.tsx` : bloc statut/boutons pour l'Atelier 2 calqué sur celui de l'Atelier 1 (`etude.statutAtelier2` au lieu de `etude.statut`), avec une différence assumée : **le bouton "Télécharger le rapport PDF" est toujours visible, à n'importe quel statut** (pas seulement après validation comme pour A1) -- parce que le rapport A2 n'est pas un snapshot figé mais une lecture live des agrégats (décision déjà actée), donc rien ne justifie de le cacher avant validation. `lib/api.ts` gagne `demarrerAtelier2`/`validerAtelier2`/`rouvrirAtelier2`/`rapportAtelier2Url`, et `Etude` gagne le champ `statutAtelier2`.

### Incident rencontré (à retenir)

Après `dotnet build` vert et migration appliquée, les tests curl sur `demarrer-atelier2`/`valider-atelier2` renvoyaient `404` -- pas un bug de code : le process `EbiosRM.Api` tournant sur le port 5197 avait été démarré **avant** ces modifications (`dotnet build` ne redémarre jamais un process déjà lancé). Résolu en tuant le process (`kill <pid>`) et en relançant `dotnet run --no-build`. **Leçon** : après toute modification de `Program.cs` ajoutant des endpoints, si l'API tourne déjà en arrière-plan depuis une session précédente, un redémarrage explicite du process est nécessaire avant de tester -- un `dotnet build` vert ne suffit pas, contrairement au réflexe habituel de ce document (incidents 12/13) qui portait sur des fichiers non modifiés, pas sur un process obsolète.

### Test de bout en bout réalisé (curl, étude "Test workflow A2")

`demarrer-atelier2` avant validation A1 → `400` (garde-fou respecté) → `demarrer-atelier1` + données minimales + `valider-atelier1` → `demarrer-atelier2` → `200` → `valider-atelier2` sans couple → `400` avec `elementsManquants` → ajout d'un couple catégorie `ActivisteIdeologique`/OV `Autre` (`descriptionObjectifVise: "Perturber la production"`) → `valider-atelier2` → `200` → téléchargement PDF → `200`, PDF valide, contenu vérifié via `pdftotext` : affiche bien `"Autre : Perturber la production"` (confirme que le correctif d'affichage de la session précédente fonctionne en conditions réelles, pas juste en théorie).

### Bug corrigé (relecture utilisateur)

Le message affiché quand `statutAtelier2 === 'Validee'` (copié-collé du message équivalent d'A1 puis mal adapté) était **auto-contradictoire** : il affirmait "ajouter des éléments ne mettra pas à jour le rapport PDF" puis justifiait par "il est généré à la demande à partir des données actuelles" -- ces deux affirmations se contredisent. Une première correction avait réécrit le texte pour dire l'inverse (correct factuellement), mais **l'utilisateur a explicitement demandé la suppression pure et simple de cette note** (pas seulement sa reformulation) -- décision actée, ne pas la réintroduire sans demande explicite. Le bloc `{etude.statutAtelier2 === 'Validee' && (...)}` a été retiré de `AtelierPage.tsx` ; l'atelier 2 validé n'affiche donc plus aucune bannière informative, seul le statut dans l'en-tête change. Build frontend revérifié vert.

### Bug corrigé -- chaine des ateliers (Sidebar + Dashboard) ne refletait pas le statut reel de l'Atelier 2

Signale par l'utilisateur : apres validation de l'Atelier 2, retour au dossier de l'etude -- "ATELIER 02 Sources de risque" n'apparaissait pas coche/valide dans la chaine visuelle. Cause : `ateliersDepuisEtude()` dans `Sidebar.tsx` et le tableau `ateliers` dans `Dashboard.tsx` avaient l'Atelier 2 **hardcode en `'todo'`** (`{ numero: 2, ..., statut: 'todo', progression: 0 }`) -- reste de l'epoque ou seul `etude.statut` (Atelier 1) existait et ou l'honnetete des donnees imposait de figer A2-A5 en "todo" (cf. section "Connexion du frontend a l'API reelle" plus haut dans ce document). Cette section n'avait jamais ete revisitee apres l'ajout de `StatutAtelier2`.

**Corrige** dans les deux fichiers : meme logique de derivation que pour l'Atelier 1 (`Validee` -> `done`, `EnCours` -> `current`, sinon `todo`), appliquee cette fois a `etude.statutAtelier2`. Les Ateliers 3-5 restent `'todo'` en dur (toujours honnete : ces slices n'existent pas encore cote backend). Build frontend revérifié vert.

**Point de vigilance pour la suite** : si un futur slice (A3+) ajoute un `StatutAtelierN` supplementaire, penser systematiquement a mettre a jour CES DEUX fichiers (`Sidebar.tsx` ET `Dashboard.tsx`, logique dupliquee dans les deux) -- c'est précisément l'oubli qui vient de se produire pour A2.

### Prochaine action

Test manuel navigateur (formulaire complet Atelier 2 + nouveaux boutons de statut + téléchargement PDF + chaine des ateliers a jour) toujours en attente. Ensuite : démarrage du Slice 3 (Atelier 3).

---

## Mise à jour — Slice 3 (Atelier 3) démarré : évaluation de la menace par partie prenante

**Première brique du Slice 3, terminée et testée de bout en bout par curl.** Méthode : même discipline que les slices précédentes -- domaine d'abord, un composant à la fois, testé avant de passer au suivant. Découpage retenu pour tout le Slice 3 (annoncé à l'utilisateur) : (1) évaluation menace [FAIT], (2) scénarios stratégiques, (3) chemins d'attaque + événements intermédiaires, dans cet ordre.

### Décision actée

L'évaluation de la menace se fait sur `PartiePrenante`, agrégat **déjà existant** (créé côté "Atelier 2" dans ce codebase, cf. divergence déjà notée §"rôles/acteurs" -- ici aussi l'atelier officiel place la partie prenante en tête d'Atelier 3, mais ce codebase les crée plus tôt ; l'évaluation, elle, reste bien une action distincte, ajoutée maintenant). Pas de nouvel agrégat : `PartiePrenante` gagne 4 champs nullable (`Dependance`, `Penetration`, `MaturiteCyber`, `Confiance`, échelle 1-4 réutilisant `EchelleMin`/`EchelleMax`, même convention que `CoupleSourceRisqueObjectifVise`) + `NiveauMenace` (double, nullable, calculé -- jamais saisi).

### Backend (build vert, migration appliquée, testé curl)

- `PartiePrenante.cs` : 4 champs nullable + `NiveauMenace`, méthode `EvaluerMenace(dependance, penetration, maturiteCyber, confiance, niveauMenace)` (le niveau est calculé en amont, jamais dans l'entité -- même principe que `ServiceCalculPertinence`/`CoupleSourceRisqueObjectifVise.Creer`).
- `ServiceCalculNiveauMenace.cs` (nouveau, `Domain/SourcesRisque/`) : formule officielle `(Dépendance x Pénétration) / (Maturité cyber x Confiance)`, arrondie à 2 décimales, valide chaque paramètre sur l'échelle 1-4.
- Migration `AjoutEvaluationMenacePartiePrenante` : 5 colonnes nullable ajoutées à `parties_prenantes` (pas de piège `defaultValue` cette fois, nullable par défaut).
- Endpoint `PUT /api/v1/etudes/{etudeId}/parties-prenantes/{id}/menace` (réutilise `MettreAJourAsync` déjà existant sur `IPartiePrenanteRepository`, pas de nouvelle méthode repository nécessaire).
- **Testé par curl** : évaluation hors échelle (`dependance:5`) → `400` ; évaluation valide (`4,3,2,2`) → `niveauMenace:3` (vérifié `4*3/(2*2)=3.0` exact).

### Frontend (build vert)

- `lib/api.ts` : `PartiePrenante` gagne les 5 champs optionnels, fonction `evaluerMenace(etudeId, id, dependance, penetration, maturiteCyber, confiance)`.
- `AtelierPage.tsx` : nouvelle route fonctionnelle `numero === 3` (`estAtelier3`, retirée de `estVerrouille`). Affiche un bandeau "chantier en cours" honnête (seule l'évaluation menace est disponible, scénarios stratégiques et chemins d'attaque à venir) + `EvaluationMenaceSection` (nouveau composant) : liste des parties prenantes (créées en Atelier 2) avec statut "Non évaluée" ou "Niveau X" coloré (seuils arbitraires : ≥4 critique, ≥1.5 élevé, sinon faible -- **seuils à valider avec l'utilisateur, pas une donnée officielle EBIOS RM**, contrairement à la formule elle-même qui l'est), bouton "Évaluer"/"Réévaluer" ouvrant un mini-formulaire 4 champs (1-4 chacun) inline.
- Pas de nouveau statut d'atelier (`StatutAtelier3`) à ce stade -- prématuré tant que le reste du Slice 3 (scénarios stratégiques) n'existe pas ; la chaîne des ateliers (Sidebar/Dashboard) continue donc, à raison, d'afficher l'Atelier 3 en `'todo'`.

### Incident (déjà connu, reconfirmé)

Après migration + build backend vert, les premiers tests curl échouaient (endpoint inexistant) -- même cause que la fois précédente : process API déjà lancé avant les modifications. Redémarré (`kill` + `dotnet run --no-build`) avant de retester. Confirme que cette étape doit désormais être systématique après toute modification de `Program.cs`, pas seulement documentée comme un incident isolé.

### Prochaine action

Suite du Slice 3 : agrégat `ScenarioStrategique` (relation 1:1 avec `CoupleSourceRisqueObjectifVise`, cf. modèle cible documenté plus haut dans ce fichier), puis `CheminAttaque`/`EvenementIntermediaire`. Test manuel navigateur de l'évaluation menace (formulaire, couleurs, réévaluation) toujours en attente comme le reste des tests manuels accumulés.

---

## Mise à jour — Slice 3 (Atelier 3) : agrégat ScenarioStrategique (backend uniquement, frontend pas encore fait)

**Deuxième brique du Slice 3, terminée côté backend et testée de bout en bout par curl.** Frontend pas encore construit -- prochain arrêt naturel avant de continuer.

### Modélisation actée

Nouveau module `Modules/CoreEngine/Domain/ScenariosDeRisque/` (correspond au module "Scénarios de risque" du BC2 déjà documenté en §6.3 de ce fichier). `ScenarioStrategique` : `Id`, `EtudeId`, `CoupleSourceRisqueObjectifViseId` (1:1, contrainte unique en base), `Description` (texte libre narratif "de la source de risque vers l'objectif visé"), `CreeLeUtc`. Volontairement minimal à ce stade -- la gravité/vraisemblance/niveau de risque arriveront avec `CheminAttaque`/`ScenarioOperationnel`/`ScenarioDeRisque` (prochaine brique), pas ici.

**Règle métier ajoutée (pas seulement la relation 1:1)** : un scénario stratégique ne peut être créé que sur un couple SR/OV **retenu** (`Pertinence` = `TresPertinent` ou `PlutotPertinent`), cohérent avec la logique du PDF Atelier 2 ("Couples retenus pour l'Atelier 3" vs "sous surveillance"). Vérifié par l'endpoint avant création (pas dans l'entité elle-même, qui reste agnostique de la pertinence du couple -- cohérent avec P8, la validation métier passe par l'orchestration/endpoint qui a accès aux deux agrégats, pas par un couplage direct entre agrégats).

### Fichiers créés

- `Domain/ScenariosDeRisque/ScenarioStrategique.cs`, `IScenarioStrategiqueRepository.cs`
- `Infrastructure/ScenarioStrategiqueRepository.cs`
- `EbiosDbContext.cs` : `DbSet<ScenarioStrategique>`, mapping avec `HasIndex(...).IsUnique()` sur `CoupleSourceRisqueObjectifViseId`
- Migration `AjoutScenarioStrategique` (nouvelle table `scenarios_strategiques`)
- `Program.cs` : `POST /etudes/{etudeId}/couples-sr-ov/{coupleId}/scenario-strategique`, `GET /etudes/{etudeId}/scenarios-strategiques`, `PUT`/`DELETE .../scenarios-strategiques/{id}`

### Testé par curl (bout en bout)

Couple `PeuPertinent` (motivation=1, ressources=1) → création scénario → `400` (règle "couple retenu" respectée). Couple `TresPertinent` (motivation=4, ressources=4) → création scénario → `201`. Nouvelle tentative sur le même couple → `400` ("relation 1:1"). Liste → `200`, contenu correct.

### Prochaine action

Frontend pour `ScenarioStrategique` (liste des couples retenus depuis l'Atelier 2, formulaire de création de scénario par couple, affichage dans la section Atelier 3 déjà créée). Ensuite : `CheminAttaque` + `EvenementIntermediaire` (troisième et dernière brique de la liste annoncée pour ce Slice), qui porteront la gravité/vraisemblance.

---

## Mise à jour — Slice 3 : frontend ScenarioStrategique fait (build vert, retour visuel utilisateur en attente)

**Fait à la demande explicite de l'utilisateur ("fais alors le frontend avant que je controle") -- construit sans validation intermédiaire, contrôle visuel à venir.**

- `lib/api.ts` : type `ScenarioStrategique`, `listScenariosStrategiques`, `createScenarioStrategique`, `updateScenarioStrategique`, `deleteScenarioStrategique`.
- `AtelierPage.tsx` : `charger()` pour `numero === 3` récupère désormais aussi `couples` et `scenarios` (en plus de `parties`, déjà chargé pour l'évaluation menace). Nouveau composant `ScenariosStrategiquesSection` : liste des scénarios déjà créés (édition/suppression inline, même pattern que les autres sections) + liste séparée "Couples retenus en attente de scénario" (calculée côté frontend : couples avec `pertinence` Très/Plutôt pertinent ET sans scénario existant -- filtre miroir de la règle métier backend, mais backend reste la seule source de vérité, ce filtre est juste un confort d'affichage) avec un formulaire de création inline par couple. Helper `libelleCouple()` réutilise le pattern déjà établi pour `CouplesSrOvSection` (bascule sur la description libre si `sourceRisque`/`objectifVise` vaut "Autre").
- Build frontend (`npm run build`) vert. **Pas de test manuel navigateur ni de test curl refait après ce lot** -- contrôle utilisateur en attente comme demandé.

### Prochaine action

Retour visuel/fonctionnel de l'utilisateur sur cette section. Ensuite : `CheminAttaque` + `EvenementIntermediaire` (troisième et dernière brique du Slice 3), qui porteront la gravité/vraisemblance et fermeront la chaîne jusqu'au `ScenarioDeRisque`.

---

## Mise à jour — Rapport PDF Atelier 3 créé + jeu de données de test complet

**Demande utilisateur** : "créer une fois un atelier 3 complet bien rempli, je vais essayer de télécharger le pdf". **Constat fait avant d'agir** : aucun rapport PDF n'existait pour l'Atelier 3 (seuls A1/A2 en ont un) -- construit avant de générer les données de test, sinon le téléchargement aurait échoué en 404.

### Rapport Atelier 3 (backend, build vert, testé par curl -- même décision "pas de snapshot" qu'A2, lecture live)

- `RapportAtelier3Data.cs` : `PartiePrenanteMenaceData` (nom, rôles, représentant, 4 critères + niveau de menace, tous nullable) et `ScenarioStrategiqueData` (libellés SR/OV avec bascule "Autre" -> description libre, pertinence, description).
- `RapportAtelier3Service.cs` : lit `IPartiePrenanteRepository` + `IScenarioStrategiqueRepository` + `ICoupleSourceRisqueObjectifViseRepository` (jointure en mémoire via `Dictionary` pour retrouver le couple de chaque scénario).
- `RapportAtelier3PdfGenerator.cs` : même palette/typographie que A1/A2 (Bleu France, Fraunces/IBM Plex), section "Cartographie de la menace de l'écosystème" (tableau couleur selon seuil : ≥4 rouge, ≥1.5 orange, sinon vert -- mêmes seuils que le frontend, cf. section précédente) + section "Scénarios stratégiques" (un bloc par scénario, libellé du couple + pertinence colorée + description).
- Endpoint `GET /api/v1/etudes/{id}/rapports/atelier3`. Piège évité de justesse : `PageSizes` nécessite `using QuestPDF.Helpers;`, absent au premier essai (`CS0103`), corrigé immédiatement.

### Jeu de données de test créé via l'API réelle (pas de script réutilisable écrit cette fois, curl direct)

Étude "BioGenTech - Étude complète Atelier 3" (thème société de biotechnologie, cohérent avec les données de test déjà utilisées ailleurs dans ce projet) : Atelier 1 validé (2 valeurs métier, 2 biens support, 2 ER gravité 4), Atelier 2 validé (3 parties prenantes, 4 couples SR/OV -- 1 très pertinent, 2 plutôt pertinents, 1 peu pertinent délibérément inclus pour vérifier le garde-fou), Atelier 3 : les 3 parties prenantes évaluées (niveaux de menace obtenus : 3.00 / 4.50 / 0.17), 3 scénarios stratégiques créés sur les couples retenus, tentative sur le couple peu pertinent explicitement testée et refusée (`400`, comportement attendu).

**PDF téléchargé et vérifié par `pdftotext`** avant transmission : 1 page, tableau de menace avec les bons calculs, 3 scénarios avec libellé/pertinence/description corrects, couple non retenu absent comme attendu.

**Étude laissée en base pour test manuel navigateur** (id `eb8e9950-cfa0-4d38-8418-66c50445f0e8`, cf. URL donnée à l'utilisateur). Pas de bouton de téléchargement PDF Atelier 3 dans le frontend à ce stade (hors périmètre de cette demande ponctuelle) -- URL donnée directement à l'utilisateur pour tester.

### Prochaine action

Retour utilisateur sur le PDF Atelier 3 (contenu + éventuellement demande d'un bouton de téléchargement dans `AtelierPage.tsx`, pas encore fait). Ensuite : `CheminAttaque` + `EvenementIntermediaire` (troisième et dernière brique du Slice 3).

---

## Mise à jour — Jeu de données VRAIMENT complet (tous les champs remplis, A1 à A3)

**Le jeu de données précédent était incomplet** : l'utilisateur l'a signalé explicitement ("ton test n'est pas complet ... remplis tous les champs"). Écart identifié en le relisant : le **Socle de Sécurité n'avait jamais été rempli** dans le premier test (aucun appel à `POST .../socle-securite`), et les couples SR/OV ne couvraient que 2 des 4 thèmes.

### Nouvelle étude créée, cette fois réellement exhaustive

**"BioGenTech - Étude complète A1-A3"** (id `189711fa-bb37-46a4-94bb-698b84fcb9b4`), construite de zéro (pas de correction de l'étude précédente, pour que le snapshot Atelier 1 soit lui-même complet dès sa création) :

- **Atelier 1** : 3 valeurs métier (au lieu de 2), 6 biens support couvrant les 4 types de l'enum `TypeBienSupport` (`SystemeInformation`, `Local`, `Reseau`, `RessourcesHumaines`), 6 événements redoutés avec gravités variées (2 à 4, pas seulement 4), **Socle de Sécurité rempli avec 4 contrôles ISO 27001:2022 réels couvrant les 4 thèmes** (codes vérifiés exacts par `grep` sur `frontend/src/lib/iso27001.ts` avant de les utiliser : A.5.1, A.6.3, A.7.1, A.8.24) + 1 référentiel libre (RGPD), chacun avec `Etat` ET `EtatActuel` renseignés. Validé (snapshot v1 inclut désormais tout).
- **Atelier 2** : 4 parties prenantes (au lieu de 3), 7 couples SR/OV couvrant explicitement **les 4 thèmes** (Technologique x3, Organisationnel x2, Personnes x1, Physique x1), dont **un couple en catégorie "Autre"** pour SR et OV simultanément (pour vérifier ce chemin bout en bout dans le rapport), et un couple volontairement peu pertinent conservé pour vérifier la liste "sous surveillance". Validé.
- **Atelier 3** : les 4 parties prenantes évaluées (niveaux de menace obtenus : 3.00 / 4.50 / 0.17 / 1.50 -- bon étalement), 6 scénarios stratégiques créés sur les 6 couples retenus (2 `TresPertinent` + 4 `PlutotPertinent`), tentative sur le couple `PeuPertinent` explicitement testée et refusée (`400`).

### Vérification faite avant transmission (pas seulement "curl 200")

Les 3 PDF (A1, A2, A3) téléchargés et lus intégralement via `pdftotext -layout` (pas juste vérifié que le fichier est un PDF valide) : toutes les valeurs métier/biens support/événements redoutés/contrôles socle apparaissent avec le bon contenu ; le couple catégorie "Autre" s'affiche bien avec sa description libre comme libellé (pas juste "Autre") dans le rapport Atelier 3 ; les listes "retenus"/"sous surveillance" du rapport Atelier 2 sont correctes.

**Leçon retenue** : pour un jeu de données de test qualifié de "complet", vérifier explicitement qu'AUCUN agrégat n'est laissé de côté (une checklist mentale : tous les agrégats de l'atelier, tous les champs optionnels de chacun, toutes les branches d'un enum/thème utilisées au moins une fois) plutôt que de réutiliser tel quel un jeu de données minimal déjà existant.

### Prochaine action

Retour utilisateur sur ce jeu de données complet. Ensuite : `CheminAttaque` + `EvenementIntermediaire` (troisième et dernière brique du Slice 3). L'étude précédente ("Test A2 menace", "Test scenario strategique", etc., créées lors des tests curl antérieurs) et celle-ci coexistent en base -- aucune n'a été supprimée, la base de dev accumule les études de test (sans impact, pas de contrainte d'unicité sur le nom).

---

## Mise à jour — Workflow de validation Atelier 3 (statut + boutons + PDF), signalé manquant par l'utilisateur

**Signalé par l'utilisateur en testant l'interface** ("il n'y a pas de bouton valider ni télécharger" sur l'Atelier 3) -- gap réel : contrairement à A1/A2, aucun statut/bouton n'avait été construit pour A3, seul l'endpoint PDF existait côté backend sans lien dans l'UI. Corrigé en reproduisant exactement le pattern déjà établi pour A2 (3e fois que ce pattern est dupliqué -- cf. remarque plus bas sur une possible généralisation future).

### Backend (build vert, migration appliquée, testé curl, process API redémarré)

- `Etude.cs` : `StatutAtelier3` (réutilise `StatutEtude`) + `DemarrerAtelier3()` (exige `StatutAtelier2 == Validee`), `ValiderAtelier3()`, `RouvrirAtelier3()` (pas de snapshot, comme A2).
- `ServiceValidationCompletudeAtelier3.cs` (nouveau, `Domain/ScenariosDeRisque/`) : complétude = au moins un scénario stratégique existant.
- Migration `AjoutStatutAtelier3` : **même piège que la fois précédente** (`defaultValue: ""` généré par EF Core au lieu de `"Brouillon"`), corrigé manuellement avant application -- confirme que ce n'est pas un hasard isolé mais un comportement systématique d'`AddColumn<string>` sur une colonne non-nullable ajoutée après coup : **à vérifier systématiquement sur toute future migration de ce type**.
- Endpoints `POST demarrer-atelier3`/`valider-atelier3`/`rouvrir-atelier3`, mêmes schéma et codes de retour qu'A1/A2.
- Testé sur l'étude BioGenTech existante (6 scénarios déjà créés) : démarrer → `200`, valider → `200` (complétude déjà satisfaite), rouvrir → `200` (remis exprès à `EnCours` pour que l'utilisateur puisse cliquer lui-même sur "Valider" dans l'interface).

### Frontend (build vert)

- `lib/api.ts` : `Etude.statutAtelier3`, `demarrerAtelier3`/`validerAtelier3`/`rouvrirAtelier3`, `rapportAtelier3Url`.
- `AtelierPage.tsx` : bloc statut/boutons pour l'Atelier 3 identique à celui d'A2 (bouton PDF toujours visible, pas seulement après validation -- même raisonnement "pas de snapshot" qu'A2), remplace l'ancien bandeau statique "chantier en cours" (conservé en dessous, à titre informatif).
- **`Sidebar.tsx` ET `Dashboard.tsx` mis à jour cette fois-ci dès le départ** (pas oublié comme pour A2 précédemment) : `statutAtelier3` dérivé et propagé dans `ateliersDepuisEtude()`/`ateliers`, l'Atelier 3 peut désormais apparaître `done`/`current` dans la chaîne visuelle.

### Remarque pour une future session (pas d'action immédiate)

Ce pattern (`StatutAtelierN` + `DemarrerN`/`ValiderN`/`RouvrirN` + `ServiceValidationCompletudeAtelierN` + endpoints + câblage Sidebar/Dashboard) vient d'être dupliqué une 3e fois à l'identique (A1 en manuel/historique, A2, A3). **Si un besoin similaire se présente pour A4/A5, envisager une généralisation** (ex. `Dictionary<int, StatutEtude>` ou une petite abstraction dédiée) plutôt qu'une 4e/5e duplication -- mais ne pas le faire maintenant de façon anticipée (P8/philosophie du projet : pas d'abstraction avant un vrai besoin répété au-delà de 2-3 occurrences).

### Prochaine action

Confirmation utilisateur que les boutons Atelier 3 fonctionnent bien dans le navigateur. Ensuite : `CheminAttaque` + `EvenementIntermediaire`.

---

## Mise à jour — Audit de conformité Atelier 3 contre la documentation officielle : refonte en 3 briques

**Signalé par l'utilisateur** : "il y a beaucoup de manquement sur l'implémentation de cet atelier, par exemple l'application ne permet pas à l'analyste d'évaluer les parties prenantes [...] relis les fichiers 10, 11 et 12 sur l'Atelier 3". Les 3 parties du support officiel (`Sources/10 ATELIER+3+partie+1.pdf`, `11 ATELIER+3+partie+2.pdf`, `12 ATELIER+3+partie+3.pdf`, formation Jamal SAAD) ont été relues intégralement (22 pages).

### Écarts identifiés contre la documentation officielle (audit complet, avant tout correctif)

L'Atelier 3 officiel a 3 sous-étapes, chacune avec des manques réels :

1. **Cartographie de menace de l'écosystème** : pas de **catégorie** de partie prenante (Clients/Partenaires/Prestataires -- structurant pour la cartographie officielle) ; les 4 critères de menace (Dépendance/Pénétration/Maturité cyber/Confiance) ont une **échelle officielle précise avec définition textuelle par niveau 1-4** que le formulaire n'affichait pas (l'analyste devait deviner) ; **aucune classification en zone** (Veille/Contrôle/Danger) alors que c'est le livrable central -- *"c'est dans l'atelier 3 que le périmètre de l'analyse de risque est véritablement défini"*, les parties prenantes en zone Contrôle/Danger sont dites **critiques**.
2. **Scénarios stratégiques** : doivent cibler un **événement redouté précis** (donc une valeur métier) et **hériter sa gravité** -- absent du modèle actuel. **Aucun "chemin d'attaque"** n'existe encore (1 scénario = N chemins, chaque chemin = séquence SR → [0..N Parties Prenantes avec Événement Intermédiaire] → ER) -- ce n'est pas un détail additionnel mais la moitié structurelle manquante de l'atelier.
3. **Mesures de sécurité sur l'écosystème** : chantier entièrement absent (menace initiale vs résiduelle après mesure, par partie prenante critique / chemin d'attaque).

**Découpage validé avec l'utilisateur** ("découpons et lançons le chantier") : Brique 1 (catégorie + échelle officielle + zones) → Brique 2 (chemins d'attaque + gravité liée à l'ER) → Brique 3 (mesures écosystème + menace résiduelle), dans cet ordre.

### Brique 1 — TERMINÉE (backend + frontend + PDF, build vert, testé)

**Backend** (`Modules/CoreEngine/Domain/SourcesRisque/PartiePrenante.cs`) :
- `CategoriePartiePrenante` (enum `Client, Partenaire, Prestataire, Autre` -- repris de l'exemple officiel "société de biotechnologie", + `DescriptionCategorie` pour "Autre", même principe "option Autre systématique" que les autres énumérations). Obligatoire à la création/modification (`Creer`/`Modifier` étendus).
- `ZoneMenace` (enum `Veille, Controle, Danger`) : propriété **calculée** `Zone` sur `PartiePrenante` (jamais persistée, `[Ignore]` côté EF -- cohérent avec l'invariant "aucune valeur dérivée stockée séparément de son calcul"), dérivée via `ServiceCalculNiveauMenace.DeterminerZone(niveau)`. **Seuils par défaut documentés comme non-officiels** (la doc ne fixe pas de seuils universels, l'exemple biotech utilise des seuils contextuels 0.2/0.9/2.5) : retenu `< 1` Veille, `[1, 4)` Contrôle, `>= 4` Danger (symétrique autour de 1.0, valeur obtenue à échelle médiane).
- Migration `AjoutCategoriePartiePrenante` : **même piège de `defaultValue` que les fois précédentes**, corrigé (`"Autre"` pour `Categorie`, texte explicite pour `DescriptionCategorie` sur les lignes existantes).
- Endpoints `POST`/`PUT parties-prenantes` étendus (`Categorie`, `DescriptionCategorie`), `Enum.TryParse` avec message d'erreur listant les valeurs autorisées (même pattern que SR/OV).
- Testé par curl : catégorie invalide → `400` ; ancienne donnée migrée → `categorie: "Autre"` + description explicite ; nouvelle création avec catégorie → zone `null` tant que non évaluée, puis correctement calculée après évaluation (vérifié : 3.00→Contrôle, 4.50→Danger, 0.17→Veille, 1.50→Contrôle).

**Frontend** (`AtelierPage.tsx`) :
- `PartiesPrenantesSection` (Atelier 2) : select catégorie (+ "Précisez" conditionnel pour "Autre", même pattern que SR/OV) dans les formulaires d'ajout ET d'édition. Catégorie affichée dans la liste.
- `EvaluationMenaceSection` (Atelier 3) : nouveau composant `ChampEchelleMenace` -- chaque select 1-4 affiche désormais sous lui la **définition textuelle officielle complète** du niveau sélectionné (`ECHELLE_MENACE`, 4 critères x 4 niveaux, texte exact repris du support officiel). Affichage de la zone (libellé + couleur) au lieu du niveau brut. Résumé "N partie(s) prenante(s) critique(s)" en bas de section.

**PDF** (`RapportAtelier3*.cs`) : nouvelle section "Grille officielle d'évaluation de la menace" en tête de rapport (tableau 4 niveaux x 4 critères avec texte complet + tableau zone/acceptabilité/recommandation, mêmes textes que le frontend -- une seule source de vérité textuelle dupliquée sciemment des deux côtés comme le reste du projet, pas de partage de constantes front/back). Tableau de cartographie enrichi : colonne Catégorie, colonne "Niveau / Zone" colorée. Phrase de synthèse "N partie(s) prenante(s) critique(s)" avec liste nominative. Vérifié par `pdftotext` : rendu correct sur les 4 parties prenantes de l'étude BioGenTech (zones Contrôle/Danger/Veille toutes représentées).

### Prochaine action

Brique 2 : `ScenarioStrategique` doit cibler un `EvenementRedoute` (hérite sa `Gravite`), puis agrégat `CheminAttaque` (1..N par scénario, traverse 0..N `PartiePrenante` en générant un `EvenementIntermediaire` par franchissement). C'est la brique la plus structurante des 3.

---

## Mise à jour — Brique 2 (chemins d'attaque + cible ER/gravité) TERMINÉE

**Fait en autonomie** : l'utilisateur s'est absenté 1h en demandant de continuer le développement sans validation intermédiaire ("continue le développement en continu sans avoir besoin de ma validation") -- accordé explicitement, poursuite du découpage déjà validé (Brique 1 → 2 → 3).

### Modélisation actée

- `ScenarioStrategique` gagne `EvenementRedouteId` (Guid, obligatoire) -- **la `Gravite` n'est PAS dupliquée/stockée** sur le scénario : elle est lue en direct depuis l'`EvenementRedoute` ciblé au moment de l'affichage (P8, cohérent avec la doc officielle : *"gravité des impacts, identique pour le scénario stratégique et tous ses chemins d'attaque"*).
- Nouvel agrégat `CheminAttaque` (`Domain/ScenariosDeRisque/CheminAttaque.cs`) : `Id, EtudeId, ScenarioStrategiqueId, Description`, porte une collection **owned** `EvenementIntermediaire` (`Id, PartiePrenanteId, Description, Ordre`) -- même pattern EF que `SocleSecurite.Referentiels` (`OwnsMany`). Un chemin direct (canal d'exfiltration direct, 0 partie prenante traversée) a simplement 0 événement intermédiaire.

### Backend (build vert, migration appliquée, testé curl de bout en bout)

- Migration `AjoutCheminAttaqueEtCibleScenario` : colonne `EvenementRedouteId` sur `scenarios_strategiques` (defaultValue `Guid.Empty` pour les 6 scénarios de test existants -- **corrigés manuellement après coup** via `PUT` avec un vrai `EvenementRedouteId`, cf. incident ci-dessous, pas un problème pour de la donnée de dev) ; nouvelles tables `chemins_attaque` et `evenements_intermediaires` (FK `CASCADE` sur suppression du chemin).
- Endpoints : `POST/GET/PUT/DELETE .../scenarios-strategiques/{id}` (étendus avec `EvenementRedouteId`, validé contre `IEvenementRedouteRepository`), `POST .../scenarios-strategiques/{id}/chemins-attaque`, `GET .../chemins-attaque`, `PUT/DELETE .../chemins-attaque/{id}`, `POST/PUT/DELETE .../chemins-attaque/{id}/evenements-intermediaires[/{eiId}]`.

### Deux incidents rencontrés et résolus (leçons génériques, pas spécifiques à cette brique)

1. **`DbUpdateConcurrencyException` (0 rows affected) en ajoutant un `EvenementIntermediaire`** : `CheminAttaqueRepository.MettreAJourAsync` appelait `_db.CheminsAttaque.Update(chemin)` avant `SaveChangesAsync` -- **exactement le bug déjà documenté et corrigé pour `SocleSecuriteRepository`**, reproduit ici sans le voir en copiant le mauvais pattern (celui de `ScenarioStrategiqueRepository`, qui n'a pas de collection owned donc n'a jamais ce problème). **Leçon renforcée** : pour tout agrégat avec une collection owned (`OwnsMany`), `MettreAJourAsync` doit être `SaveChangesAsync` seul, jamais précédé d'un `.Update()` explicite sur une entité déjà suivie par le même `DbContext`. Vérifier ce point systématiquement pour tout futur agrégat à collection owned.
2. **Même exception persistant après le premier correctif** : cause distincte -- `EvenementIntermediaire.Creer()` assignait `Id = Guid.NewGuid()` côté client alors que le mapping EF déclare `ValueGeneratedOnAdd()` sur cette propriété (copié depuis `CheminAttaque.Creer()`, un agrégat racine, sans réaliser que la convention diffère pour une entité owned). `ReferentielApplicable.Creer()` -- le seul autre exemple d'entité owned du projet -- ne fixe jamais `Id`. **Règle à retenir** : pour toute entité owned mappée avec `ValueGeneratedOnAdd()`, ne jamais assigner `Id` dans la méthode `Creer()`.

### Frontend (build vert)

- `lib/api.ts` : `ScenarioStrategique.evenementRedouteId`, `createScenarioStrategique`/`updateScenarioStrategique` prennent désormais l'ER cible ; nouveaux types `CheminAttaque`/`EvenementIntermediaire` et fonctions CRUD complètes (`listCheminsAttaque`, `createCheminAttaque`, `deleteCheminAttaque`, `createEvenementIntermediaire`, `deleteEvenementIntermediaire` -- `updateCheminAttaque`/`updateEvenementIntermediaire` existent côté API mais **pas encore câblées côté UI** : correction possible uniquement par suppression/recréation pour l'instant, limitation mineure assumée pour tenir les délais de cette session).
- `AtelierPage.tsx` : `ScenariosStrategiquesSection` gagne un select "événement redouté cible" (libellé = valeur métier + description + gravité) dans les formulaires de création ET modification, affiche la gravité colorée et la cible en toutes lettres sur chaque scénario. Nouveau `CheminsAttaqueSection` (+ `CheminsParScenario` + `CheminRow`, 3 composants imbriqués) : un bloc par scénario listant ses chemins, chaque chemin affichant ses événements intermédiaires numérotés (partie prenante + description) avec formulaires d'ajout inline pour chemin et pour EI.

### PDF (`RapportAtelier3*.cs`)

Section "Scénarios stratégiques et chemins d'attaque" réécrite : chaque scénario affiche désormais sa cible (valeur métier + ER) et sa gravité colorée, puis la liste de ses chemins d'attaque en retrait (bordure gauche bleu clair), chaque chemin listant ses événements intermédiaires numérotés ou la mention "chemin direct" si aucun. Vérifié par `pdftotext` sur l'étude BioGenTech enrichie (2 chemins sur le scénario Étatique dont un direct et un via l'hébergeur cloud, 1 chemin sur le scénario CrimeOrganise via le prestataire SCADA, 4 scénarios sans chemin encore -- affichent correctement "Aucun chemin d'attaque défini pour ce scénario").

### Prochaine action

Brique 3 (dernière) : mesures de sécurité sur l'écosystème (par partie prenante critique / chemin d'attaque), avec menace initiale vs résiduelle et comparaison avant/après -- chantier actuellement entièrement absent. Ensuite, limitations mineures assumées à combler si besoin : édition (pas seulement suppression/recréation) pour `CheminAttaque`/`EvenementIntermediaire`, correction manuelle ponctuelle des `EvenementRedouteId` de test (déjà faite pour les 6 scénarios BioGenTech).

---

## Mise à jour — Brique 3 (mesures écosystème + menace résiduelle) TERMINÉE — chantier de conformité Atelier 3 CLÔTURÉ

**Toujours en autonomie** (utilisateur absent, développement continu accordé explicitement). Les 3 briques identifiées lors de l'audit contre la documentation officielle (`Sources/10-11-12 ATELIER+3*.pdf`) sont maintenant terminées : catégorie/échelle officielle/zones (Brique 1), chemins d'attaque/cible ER/gravité (Brique 2), mesures écosystème/menace résiduelle (Brique 3).

### Modélisation actée

- `PartiePrenante` gagne une collection owned `Mesures` (`MesureEcosysteme` : `Id, Description, CreeLeUtc` -- texte libre, pas de structuration par critère ciblé, cohérent avec l'exemple officiel où plusieurs mesures contribuent à une même menace résiduelle recalculée globalement) et un **second jeu d'évaluation** `DependanceResiduelle/PenetrationResiduelle/MaturiteCyberResiduelle/ConfianceResiduelle/NiveauMenaceResiduel` (nullable, distinct de l'évaluation initiale qui reste inchangée comme référence "avant mesures") + `ZoneResiduelle` calculée (même principe que `Zone`).
- Réutilise `ServiceCalculNiveauMenace`/`ServiceCalculNiveauMenace.DeterminerZone` tel quel pour la réévaluation résiduelle -- aucune duplication de logique de calcul.

### Backend (build vert, migration appliquée sans piège cette fois -- tous les nouveaux champs nullable, pas de `defaultValue` nécessaire)

- Migration `AjoutMesuresEcosystemeEtMenaceResiduelle` : nouvelle table `mesures_ecosysteme` (owned, `OwnsMany` même pattern que `chemins_attaque`/`evenements_intermediaires`), 5 colonnes résiduelles nullable sur `parties_prenantes`.
- **Bug préventivement corrigé avant qu'il ne se manifeste** : `PartiePrenanteRepository.MettreAJourAsync` appelait encore `.Update()` (harmless jusqu'ici car `PartiePrenante` n'avait pas de collection owned) -- corrigé par anticipation en ajoutant `Mesures` comme owned collection, exactement le même bug que `CheminAttaqueRepository`/`SocleSecuriteRepository` aurait présenté à la première tentative d'ajout de mesure. `ObtenirParIdAsync`/`ListerParEtudeAsync` gagnent `.Include(p => p.Mesures)`.
- Endpoints : `PUT .../parties-prenantes/{id}/menace-residuelle` (même forme que `/menace`), `POST/PUT/DELETE .../parties-prenantes/{id}/mesures[/{mesureId}]`.
- **Testé par curl, résultat conforme à l'exemple officiel du support de formation** : partie prenante F3-équivalente (Fournisseur SCADA, menace initiale 4.50/Danger) → 2 mesures ajoutées (réduction pénétration 3→2, hausse maturité cyber 1→2) → réévaluation → **menace résiduelle 1.50/Contrôle** (le doc officiel donne un exemple similaire : 3→2 après mesures sur son cas F3).

### Frontend (build vert)

- `lib/api.ts` : `MesureEcosysteme`, `PartiePrenante` étendu (mesures + 6 champs résiduels), `evaluerMenaceResiduelle`, `ajouterMesureEcosysteme`, `supprimerMesureEcosysteme`.
- `AtelierPage.tsx` : nouveau `MesuresEcosystemeSection` (+ `MesuresPartiePrenante`) -- une carte par partie prenante **critique uniquement** (zone Contrôle/Danger, cohérent avec la doc : seules celles-ci nécessitent un traitement), affichant menace initiale → résiduelle côte à côte, liste des mesures avec ajout/suppression, formulaire de réévaluation réutilisant `ChampEchelleMenace` (même composant que l'évaluation initiale, donc mêmes définitions officielles affichées).
- **Limitation de la Brique 2 comblée dans la foulée** : `CheminRow` gagne l'édition inline (pas seulement suppression) pour la description du chemin et pour chaque événement intermédiaire, en câblant `updateCheminAttaque`/`updateEvenementIntermediaire` qui existaient déjà côté API mais n'étaient pas utilisées. Testé par curl (`200` sur les deux).

### PDF (`RapportAtelier3*.cs`)

Nouvelle section "Mesures de sécurité sur l'écosystème" entre la cartographie et les scénarios stratégiques : une carte par partie prenante critique (liste des mesures + "MENACE INITIALE -> MENACE RÉSIDUELLE" coloré par zone). Structure calquée sur le tableau de l'exemple officiel (`Partie prenante | Mesures de sécurité | Menace initiale | Menace résiduelle`). Vérifié par `pdftotext` : les 3 parties prenantes critiques de l'étude BioGenTech s'affichent correctement, dont une non réévaluée ("Non réévaluée", comportement honnête).

### Vérifications finales de cette session

- `dotnet test` (suite xUnit existante, 51 tests, sans rapport avec ces changements mais sert de garde-fou de non-régression globale) : **51/51 verts**.
- `npm run build` : vert après chaque lot de modifications frontend, aucune régression TypeScript.
- Process API redémarré à chaque changement de `Program.cs`/domaine (leçon appliquée systématiquement cette session, plus jamais oubliée).

### Bilan du chantier "audit de conformité Atelier 3" (déclenché par l'utilisateur : "il y a beaucoup de manquement... l'application ne permet pas à l'analyste d'évaluer les parties prenantes")

Les 3 sous-étapes officielles de l'Atelier 3 sont maintenant couvertes de bout en bout (backend + frontend + PDF) : (1) cartographie de menace de l'écosystème avec catégorie/échelle officielle/zones, (2) scénarios stratégiques ciblant un ER avec gravité héritée + chemins d'attaque + événements intermédiaires, (3) mesures de sécurité + menace résiduelle. Limitations mineures encore assumées, non bloquantes : pas de visualisation cartographique graphique (cercles concentriques façon exemple officiel -- seuls des tableaux/badges texte), seuils de zone par défaut non officiels (documenté §Brique 1), pas de statut de validation dépendant de la complétude réelle de ces 3 sous-étapes (`ServiceValidationCompletudeAtelier3` ne vérifie encore que "au moins un scénario existe").

### Prochaine action

**Test manuel navigateur de tout l'Atelier 3** (jamais fait par l'utilisateur sur cette refonte complète -- priorité au retour de l'utilisateur). Ensuite, au choix : combler les limitations mineures listées ci-dessus, ou démarrer l'Atelier 4 (Scénarios opérationnels -- vraisemblance, 1:1 avec chaque chemin d'attaque selon le modèle déjà documenté plus haut dans ce fichier).

---

## Mise à jour — Relecture post-implémentation : 2 bugs de suppression en cascade trouvés et corrigés

**Toujours en autonomie.** Après la clôture du chantier des 3 briques, relecture volontaire du code nouvellement écrit (`Program.cs`, endpoints `DELETE`) avant de considérer le chantier réellement terminé -- a payé : deux bugs réels trouvés, pas de simples chipotages.

### Bugs trouvés

Aucune contrainte FK réelle n'existe entre agrégats séparés dans ce projet (référencement par `Id` seul, cohérent avec le style déjà en place -- ex. `CoupleSourceRisqueObjectifViseId` sur `ScenarioStrategique`). Conséquence non anticipée lors de l'écriture de la Brique 2 : **supprimer un `ScenarioStrategique` ou le `CoupleSourceRisqueObjectifVise` dont il dépend laissait les `CheminAttaque`/`EvenementIntermediaire` orphelins en base** -- pas d'erreur visible immédiatement (le frontend ne les affiche que nichés sous leur scénario, donc un orphelin "disparaît" de l'écran), mais :
- `GET .../chemins-attaque` (liste étude entière) aurait continué à les renvoyer indéfiniment, gonflant silencieusement les données.
- Suppression en cascade attendue par cohérence avec le reste du projet (ex. suppression d'un chemin d'attaque supprime déjà ses événements intermédiaires, via FK `CASCADE` réelle celle-là car `EvenementIntermediaire` est owned).

### Correctifs (build vert, testé par curl sur une étude jetable dédiée, pas la donnée BioGenTech)

- `DELETE .../scenarios-strategiques/{id}` : supprime désormais explicitement tous ses `CheminAttaque` (`ICheminAttaqueRepository.ListerParScenarioAsync` + boucle `SupprimerAsync`) avant de supprimer le scénario.
- `DELETE .../couples-sr-ov/{id}` : si un `ScenarioStrategique` existe pour ce couple (relation 1:1), le supprime en cascade (avec ses chemins d'attaque, même logique que ci-dessus) avant de supprimer le couple.
- **Test de bout en bout** : étude jetable créée, couple → scénario → chemin d'attaque créés (1 chemin présent), suppression du couple → `204`, vérifié après coup : 0 chemin restant, 0 scénario restant. Cascade complète confirmée.
- `dotnet test` (51/51) et `npm run build` revérifiés verts après ce correctif.

### Prochaine action

Chantier "audit de conformité Atelier 3" et sa relecture post-implémentation sont maintenant réellement clos. En attente du retour de l'utilisateur pour : test manuel navigateur, puis choix entre limitations mineures restantes (visualisation cartographique graphique, seuils de zone configurables, complétude de validation A3) ou démarrage de l'Atelier 4.

---

## Mise à jour — Slice 4 (Atelier 4, Scénarios opérationnels) démarré et terminé en autonomie

**Toujours en développement continu accordé par l'utilisateur (absent).** Avant de coder, lecture complète des 2 parties officielles (`Sources/13-14 ATELIER+4*.pdf`, 15 pages), même méthode que pour l'Atelier 3 -- payant : le modèle et les grilles de cotation étaient précisément spécifiés, permettant une implémentation directement conforme sans aller-retour.

### Modélisation actée (fidèle à la doc officielle)

- **1 `CheminAttaque` (Atelier 3) ⇒ 1 `ScenarioOperationnel` (relation 1:1 stricte, contrainte unique en base)**, décrivant "éventuellement plusieurs modes opératoires" (doc officielle).
- `ModeOperatoire` (entité owned de `ScenarioOperationnel`, même pattern que `MesureEcosysteme`/`EvenementIntermediaire`) : `Description` + 4 champs optionnels `ActionsConnaitre/Rentrer/Trouver/Exploiter` (séquence type EBIOS RM à 4 phases, déjà documentée plus haut dans ce fichier -- pas le Cyber Kill Chain) + `ProbabiliteSucces`/`DifficulteTechnique` (échelle 1-4) + `Vraisemblance` **calculée** (jamais stockée, cf. `Zone`/`ZoneResiduelle`) via une nouvelle grille officielle.
- `ServiceCalculVraisemblance` (nouveau, même pattern que `ServiceCalculPertinence`) : matrice 4x4 Probabilité de succès x Difficulté technique → `NiveauVraisemblance` (V1 à V4), reproduite exactement depuis la diapositive officielle "Quelles métriques pour une cotation fine ?" (méthode d'évaluation **affinée**, retenue plutôt que la méthode "expresse" -- cotation directe globale -- jugée moins structurante pour une application outillée).
- `ScenarioOperationnel.VraisemblanceGlobale` = **la plus vraisemblable (MAX) de ses modes opératoires** -- calculée, jamais stockée. Conforme à l'exemple officiel vérifié par curl : 3 modes cotés V3/V1/V2 → scénario global **V3** (résultat identique à l'exemple du support de formation).

### Backend (build vert, migration sans piège -- nouvelles tables uniquement, testé par curl de bout en bout)

- Fichiers : `ServiceCalculVraisemblance.cs`, `ScenarioOperationnel.cs` (agrégat + `ModeOperatoire` owned), `IScenarioOperationnelRepository.cs`/`ScenarioOperationnelRepository.cs` (leçon `Update()`/collection owned appliquée dès l'écriture, pas après coup cette fois).
- Migration `AjoutScenarioOperationnel` : tables `scenarios_operationnels` + `modes_operatoires` (owned).
- Endpoints : `POST .../chemins-attaque/{id}/scenario-operationnel` (1:1, refuse le doublon), `GET .../scenarios-operationnels`, `DELETE .../scenarios-operationnels/{id}`, `POST/PUT/DELETE .../scenarios-operationnels/{id}/modes-operatoires[/{modeId}]`.
- **Cascades de suppression étendues** (les 3 endpoints `DELETE` déjà corrigés lors de la relecture post-Atelier-3 -- couple, scénario stratégique, chemin d'attaque -- emportent désormais aussi le `ScenarioOperationnel` 1:1 du chemin concerné, pour ne pas réintroduire immédiatement le même type de bug orphelin avec ce nouvel agrégat).
- **Bug de compilation mineur rencontré et corrigé** : deux `catch (ArgumentOutOfRangeException)` placés après un `catch (ArgumentException)` déjà englobant (héritage) → `CS0160`, corrigé en supprimant les blocs redondants (le message d'erreur était de toute façon identique dans les deux branches).
- **Testé par curl, résultat exactement conforme à l'exemple officiel** : scénario opérationnel créé sur un chemin d'attaque existant (Atelier 3, BioGenTech), 3 modes opératoires ajoutés avec les cotations de l'exemple du support (P3/D2→V3, P1/D3→V1, P2/D2→V2) → vraisemblance globale **V3** confirmée. Modification d'un mode (P4/D1→V4) recalcule correctement la vraisemblance globale (V4).

### Frontend (build vert)

- `lib/api.ts` : `ModeOperatoire`, `ScenarioOperationnel`, `ModeOperatoireInput`, fonctions CRUD complètes (créer/lister/supprimer scénario, ajouter/modifier/supprimer mode opératoire).
- `AtelierPage.tsx` : route `numero === 4` activée (retirée de `estVerrouille`). Nouveau `ScenariosOperationnelsSection` (+ `OperationnelParChemin`, `ModeOperatoireRow`, `AjoutModeOperatoire`, 4 composants imbriqués, même style que la Brique 2 d'A3) : groupe par scénario stratégique → par chemin d'attaque → crée/affiche le scénario opérationnel 1:1 → liste ses modes opératoires avec les 4 champs de phase (optionnels), la cotation probabilité/difficulté (selects avec libellés explicites, ex. "3 -- Très élevée (> 40%)"), et la vraisemblance colorée. Vraisemblance globale affichée en tête de chaque chemin.
- **Assumé et annoncé explicitement dans l'UI** ("Chantier en cours -- pas encore de statut de validation ni de rapport PDF pour cet atelier") : pas de `StatutAtelier4`/workflow démarrer-valider-rouvrir, pas de rapport PDF pour cet atelier -- décision de scope pour tenir le rythme du développement autonome, cohérent avec l'honnêteté déjà pratiquée ailleurs dans ce projet (mieux vaut le dire explicitement que laisser croire que c'est fait).

### Prochaine action

Retour utilisateur attendu avant d'aller plus loin. Chantiers ouverts, non priorisés : (1) workflow de validation + rapport PDF pour l'Atelier 4 (si souhaité, mêmes patterns déjà établis 3 fois), (2) Atelier 5 (Traitement du risque -- PACS, `ScenarioDeRisque` combinant Gravité x Vraisemblance déjà esquissé plus haut dans ce fichier), (3) limitations mineures d'A3 encore ouvertes (cf. mise à jour précédente), (4) test manuel navigateur cumulé sur A3 et A4, jamais fait par l'utilisateur sur ces refontes.

---

## Mise à jour — Atelier 4 mis à parité avec A1-A3 : workflow de validation + rapport PDF

**Suite de la session en développement continu, à la demande de l'utilisateur ("continuons") après un point d'étape sur l'avancement global.** Fermeture du chantier ouvert identifié précédemment : l'Atelier 4 avait le cœur métier (scénarios opérationnels/modes opératoires/vraisemblance) mais ni statut de validation ni rapport, contrairement aux Ateliers 1-3.

### Backend (build vert, migration sans piège -- 5e fois consécutive que ce pattern est dupliqué à l'identique, cf. remarque déjà actée sur une généralisation future si A5 en a aussi besoin)

- `Etude.cs` : `StatutAtelier4` + `DemarrerAtelier4()` (exige `StatutAtelier3 == Validee`), `ValiderAtelier4()`, `RouvrirAtelier4()`.
- `ServiceValidationCompletudeAtelier4.cs` : complétude = au moins un scénario opérationnel **avec au moins un mode opératoire** (plus strict que les autres ateliers -- un scénario opérationnel vide n'a pas de sens fonctionnel, contrairement à un couple SR/OV ou une partie prenante qui peuvent exister seuls).
- Migration `AjoutStatutAtelier4` : même piège `defaultValue: ""` que d'habitude, corrigé en `"Brouillon"`.
- Endpoints `demarrer/valider/rouvrir-atelier4`, mêmes schéma et codes qu'A1-A3.
- `RapportAtelier4Data/Service/PdfGenerator.cs` (nouveaux) : rapport groupé par scénario opérationnel (libellé du couple SR/OV + chemin d'attaque cible + vraisemblance globale colorée), détail de chaque mode opératoire (description, 4 phases CONNAÎTRE/RENTRER/TROUVER/EXPLOITER si renseignées, probabilité/difficulté, vraisemblance individuelle colorée), plus une section méthodologie reproduisant la grille officielle Probabilité x Difficulté (même table que celle utilisée pour le calcul, donnée en légende comme pour A2/A3).
- **Testé par curl de bout en bout sur l'étude BioGenTech réelle** (pas une étude jetable cette fois, données réelles déjà en place) : `demarrer-atelier4` refusé tant qu'A3 n'est pas validé (`400`) → A3 validé → A4 démarré → A4 validé (`200`, complétude satisfaite par les scénarios déjà créés) → PDF téléchargé et vérifié par `pdftotext`, contenu exact (couple, chemin, 3 modes opératoires avec leurs cotations, dont le mode réévalué à V4 plus tôt dans la session).

### Frontend (build vert)

- `lib/api.ts` : `Etude.statutAtelier4`, `demarrerAtelier4`/`validerAtelier4`/`rouvrirAtelier4`, `rapportAtelier4Url`.
- `AtelierPage.tsx` : bloc statut/boutons pour l'Atelier 4 identique aux 3 précédents (bouton PDF toujours visible, pas de snapshot). Bandeau "chantier en cours" retiré (n'est plus vrai).
- `Sidebar.tsx`/`Dashboard.tsx` : `statutAtelier4` câblé dès cette session (pas oublié, leçon d'A2 bien intégrée maintenant -- 3e fois de suite que ces deux fichiers sont mis à jour en même temps que le reste).

### État réel de l'étude BioGenTech à la fin de cette session

`statut` (A1) = `Validee`, `statutAtelier2` = `Validee`, `statutAtelier3` = `Validee`, `statutAtelier4` = `Validee`. **Les 4 premiers ateliers sont donc entièrement complets et validés sur cette étude de démonstration** -- utile telle quelle pour un test manuel navigateur de bout en bout (dossier d'étude complet, chaîne visuelle Sidebar/Dashboard entièrement cochée sur 4/5).

### Bilan de progression global (tableau à jour, remplace celui donné oralement à l'utilisateur en cours de session)

| Atelier | Backend | Frontend | Validation | PDF |
|---|---|---|---|---|
| 1 -- Cadrage | ✅ | ✅ | ✅ | ✅ (snapshot) |
| 2 -- Sources de risque | ✅ | ✅ | ✅ | ✅ (live) |
| 3 -- Scénarios stratégiques | ✅ conforme doc officielle | ✅ | ✅ | ✅ |
| 4 -- Scénarios opérationnels | ✅ conforme doc officielle | ✅ | ✅ | ✅ |
| 5 -- Traitement du risque | ⬜ | ⬜ | ⬜ | ⬜ |

### Prochaine action

Atelier 5 (Traitement du risque) est le seul chantier structurel restant pour une couverture complète des 5 ateliers. Avant de s'y lancer sans confirmation (contrairement à A4 qui prolongeait un travail déjà en cours), il serait cohérent de vérifier auprès de l'utilisateur -- A5 introduit des concepts pas encore modélisés ici : `ScenarioDeRisque` (combinaison Gravité x Vraisemblance → Niveau de risque, avant/après PACS), les 4 axes du PACS (Gouvernance/Protection/Défense/Résilience, déjà documentés plus haut), et la décision de traitement (réduction/transfert/évitement/acceptation). Sinon, test manuel navigateur en attente sur tout le périmètre A1-A4.

---

## Mise à jour — Recherche terminologie EBIOS RM 1.5 + renommage Menace→Dangerosité et Responsable→Propriétaire

**À la demande explicite de l'utilisateur**, avant de commencer l'Atelier 5 ("avant de lancer fait une recherche sur internet sur les document publie par les grandes entreprises ou les grands analystes recents pour savoir comment il gere cette derniere etape"), recherche web sur les pratiques professionnelles récentes de plan de traitement du risque (grandes entreprises, ISO/CEI 27005:2022, ENISA, ISACA), avant tout code sur A5.

### Découverte non anticipée : EBIOS RM 1.5 (ANSSI, 26 mars 2024)

La recherche a révélé une mise à jour officielle de la méthode, corroborée par au moins 2 sources indépendantes (cyber.gouv.fr page officielle + article détaillé ALL4TEC, recoupé avec advens.com), alignant la terminologie sur ISO/CEI 27005:2022 :

- **"PACS" (Plan d'Amélioration Continue de la Sécurité) → "Plan de traitement du risque"**.
- **"Menace" (pour une partie prenante) → "Dangerosité"** (supprime la connotation fausse d'intention hostile délibérée -- une partie prenante peut être "menaçante" sans le vouloir, ex. défaillance technique d'un sous-traitant).
- **"Responsable" (VM/BS) → "Propriétaire"** (aligné sur la notion ISO 27005:2022 de "risk/asset owner").
- Autres évolutions notées mais non traitées pour l'instant (hors-scope, pas d'impact sur le code déjà écrit) : distinction croisement écosystème/parties prenantes explicite en fin d'A2, distinction chemins d'attaque directs/indirects, mécanismes de "surveillance" (concept ISO 27005:2022), opérateurs logiques ET/OU sur les scénarios opérationnels.

Cette découverte crée une divergence entre le code déjà livré (Ateliers 1 et 3, terminologie EBIOS RM 1.0) et la version officielle actuelle. Question posée explicitement à l'utilisateur : renommer partout vers 1.5, ou garder 1.0 et noter la divergence. **Réponse : "Tout renommer vers la terminologie 1.5 (recommandé)."**

### Renommage Menace→Dangerosité (Atelier 3), terminé cette session

- `PartiePrenante.cs` : `ZoneMenace`→`ZoneDangerosite` (enum), `NiveauMenace`/`NiveauMenaceResiduel`→`NiveauDangerosite`/`NiveauDangerositeResiduel`, `EvaluerMenace()`/`EvaluerMenaceResiduelle()`→`EvaluerDangerosite()`/`EvaluerDangerositeResiduelle()`. Noms de propriétés `Zone`/`ZoneResiduelle` conservés (seul le type sous-jacent change).
- `ServiceCalculNiveauMenace.cs` supprimé, remplacé par `ServiceCalculNiveauDangerosite.cs` -- même formule (Dépendance x Pénétration) / (Maturité cyber x Confiance), méthode `DeterminerZone()` conservée.
- Endpoints renommés (`/menace`→`/dangerosite`, `/menace-residuelle`→`/dangerosite-residuelle`), rapport A3 (données, service, PDF -- libellés "Cartographie de la dangerosité", grille `LignesEchelleDangerosite`) et frontend (`api.ts`, `AtelierPage.tsx`, `Dashboard.tsx`) mis à jour intégralement.
- Migration `RenommageMenaceEnDangerosite` : simple `RenameColumn` x2, aucun piège `defaultValue` (pur renommage, pas de nouvelle colonne). Vérifié par curl avant/après sur l'étude BioGenTech réelle : données préservées.
- `iso27001.ts` (catalogue de contrôles ISO 27001) contient encore le mot "menaces" par endroits -- **volontairement non touché**, terminologie du référentiel ISO officiel, sans rapport avec le vocabulaire EBIOS RM.

### Renommage Responsable→Propriétaire (Atelier 1), terminé cette session

Même méthode, appliquée à `ValeurMetier.cs` et `BienSupport.cs` (`EntiteResponsable`→`EntiteProprietaire`), `SnapshotAtelier1Content.cs` (les deux records de snapshot), `ServiceCreationSnapshotAtelier1.cs`, `EbiosDbContext.cs` (mapping EF), `Program.cs` (endpoints + records `CreerValeurMetierRequest`/`CreerBienSupportRequest`), le rapport A1 complet (`RapportAtelier1Data/Service/PdfGenerator.cs` -- les en-têtes de colonnes PDF étaient déjà génériques, "Entité (VM)"/"Entité (bien)", donc aucun texte à changer là), et le frontend (`api.ts`, `AtelierPage.tsx` -- labels de formulaire "Entité responsable"→"Entité propriétaire").

Un test unitaire (`ValeurMetierTests.cs`) référençait encore l'ancien nom de propriété, corrigé au passage.

Migration `RenommageResponsableEnProprietaire` : simple `RenameColumn` x2 (`valeurs_metier`, `biens_support`), aucun piège `defaultValue`. **Point d'attention spécifique à ce renommage (absent du cas Dangerosité)** : contrairement à A3 qui n'a pas de snapshot figé, l'Atelier 1 fige son état dans `SnapshotAtelier1` (P13) sous forme de JSON sérialisé -- le renommage du champ C# change la clé JSON sérialisée (`EntiteResponsable`→`EntiteProprietaire`), donc l'ancien snapshot déjà stocké en base pour BioGenTech aurait silencieusement désérialisé un `EntiteProprietaire` vide/null (clé JSON absente) si le rapport avait été régénéré sans revalidation. **Corrigé en rouvrant puis revalidant l'Atelier 1 de l'étude BioGenTech** (`rouvrir-atelier1` → `valider-atelier1`), créant un nouveau snapshot (version 2, historique de la version 1 conservé sans écrasement, conforme au principe déjà établi ailleurs dans ce fichier). PDF régénéré et vérifié par `pdftotext` : "Version 2", entités propriétaires correctement affichées ("Direction scientifique", etc.).

### Vérifications finales (les deux renommages)

`dotnet build` vert, `dotnet test` 51/51 vert (après correction du test unitaire), `npm run build` vert, `grep -rn "menace\|responsable"` (insensible à la casse) vide sur tout `frontend/src/` et sur tout `src/` hors fichiers de migration historiques (immuables par nature, non retouchés -- seule `EbiosDbContextModelSnapshot.cs`, qui reflète l'état courant du modèle et non l'historique, a été régénérée par la commande `dotnet ef migrations add`).

### Prochaine action

Les deux renommages terminologiques sont clos. Le chantier Atelier 5 peut démarrer avec la terminologie 1.5 dès le départ ("Plan de traitement du risque", pas "PACS"). Périmètre déjà esquissé lors de la mise à jour précédente : `ScenarioDeRisque` (Gravité x Vraisemblance → Niveau de risque, matrice R1-R5 officielle), `ServiceCalculNiveauRisque`, décision de traitement (traiter/accepter), mesures du plan de traitement du risque (4 axes Gouvernance/Protection/Défense/Résilience, "Propriétaire" -- pas "Responsable" -- pour rester cohérent avec le renommage qui vient d'être fait, freins/difficultés, coût/complexité, échéance, statut à lancer/en cours/terminé), recalcul du risque résiduel et acceptation formelle de la Direction. Un 4e point (cadre de suivi/indicateurs, inspiré des pratiques ISO 27005:2022/ENISA identifiées en recherche) avait été proposé comme hors-scope initial -- à retrancher explicitement ou reconfirmer avec l'utilisateur avant de commencer, plutôt que de trancher unilatéralement une deuxième fois.

---

## Mise à jour — Audit pré-Atelier 5, jugement d'expert (override) sur A2/A3/A4, ActionElementaire (A4)

**Avant de démarrer l'Atelier 5**, l'utilisateur a demandé un audit complet de l'existant (A1-A4) : cohérence avec les diagrammes de référence du projet, transparence des calculs pour l'analyste, et état exact des contraintes de séquence du workflow. Constat central de l'audit (mené par un agent dédié, cf. transcript de session pour le rapport complet) : **aucun des 3 calculs dérivés déjà livrés (Pertinence A2, Dangerosité A3, Vraisemblance A4) n'était transparent** -- grille/formule jamais visible pendant la saisie, résultat visible seulement après un aller-retour serveur, et surtout **aucun moyen pour l'analyste de documenter un désaccord avec le calcul mécanique**, alors que la méthode EBIOS RM reconnaît explicitement le jugement d'expert comme méthode d'évaluation valide. Deux écarts structurels identifiés en plus : `ActionElementaire` (Atelier 4) n'existait pas -- 4 champs texte libre remplaçaient une vraie décomposition en actions ciblant un `BienSupport` précis, cassant le bouclage Atelier 1 → Atelier 4 mis en avant par le diagramme de référence du projet ; et aucune authentification/RBAC n'existe (fait découvert incidemment, hors-scope de ce chantier).

Après clarification avec l'utilisateur (poser une question ouverte "à quoi sert ce lien vers un bien support" a été nécessaire -- **ne pas supposer que l'utilité d'un choix de modélisation est évidente, l'expliquer avec des exemples concrets de l'appli, pas des généralités abstraites**), deux features ont été actées et implémentées dans la foulée, avec un plan détaillé validé via le mode plan avant tout code :

### Feature B -- `ActionElementaire` structurée (Atelier 4), livrée en premier

`ModeOperatoire.ActionsConnaitre/Rentrer/Trouver/Exploiter` (4 strings libres) remplacés par une vraie collection owned `ActionElementaire` (1..*, cardinalité imposée -- `ArgumentException` si vide), chaque action portant `Description`, `Phase` (nouvel enum `PhaseActionElementaire`) et `BienSupportId` (cible un bien réel de l'Atelier 1, existence + appartenance à l'étude vérifiées dans `Program.cs` via `IBienSupportRepository`, même schéma que la vérification `CheminAttaque` déjà en place). EF Core : `OwnsMany` imbriqué dans le `OwnsMany` existant (`ScenarioOperationnel → ModeOperatoire → ActionElementaire`), premier cas de collection owned à 2 niveaux de profondeur dans ce projet -- fonctionne sans changement à la leçon "pas de `.Update()`" déjà en place sur `ScenarioOperationnelRepository` (vérifié par curl, y compris en cumulant Feature A dessus, cf. plus bas). Migration `RestructurationActionsElementaires` : perte de données assumée et documentée sur les 4 anciens champs texte (aucun bien support n'avait jamais été renseigné avant, donc aucun backfill automatique possible) -- les 3 modes opératoires de l'étude BioGenTech recréés manuellement via curl après migration, avec de vrais `BienSupportId`. Reporting A4 (PDF) adapté : regroupement des actions par phase avec la cible affichée ("→ nom du bien support").

### Feature A -- Jugement d'expert (override + justification), même pattern répliqué 3x

Convention actée et appliquée identiquement partout : `<X>Calculee` (toujours calculée par le service de domaine existant, inchangé) / `<X>Retenue` + `Justification<X>` (nullable, persistées, l'écart de l'analyste) / `<X>` = propriété calculée `Retenue ?? Calculee` -- c'est cette dernière que tout le code existant (endpoints, reporting, frontend) continue de lire sans aucun changement. Écriture via des endpoints dédiés (`PUT`/`DELETE .../<calcul>-retenue`), jamais fondus dans le PUT d'édition normal, pour qu'une modification des entrées ne puisse jamais effacer silencieusement un écart déjà enregistré (sticky jusqu'à `DELETE` explicite). **Choix explicite de l'utilisateur, à respecter dans toute extension future** : jamais deux valeurs affichées côte à côte (source de confusion) -- une seule valeur visible partout (badges, listes, PDF), l'écart apparaît comme une note ("Niveau déterminé par jugement d'expert de l'analyste" + justification) à la place du chiffre brut, jamais en concurrence avec lui.

- **Pertinence (A2)** : `CoupleSourceRisqueObjectifVise.Pertinence` → `PertinenceCalculee` (renommage simple, migration `RenameColumn` sans perte). Frontend : nouveau composant partagé `GrilleMatrice.tsx` (matrice 4x4 mise en évidence sur la cellule courante) affiché en permanence sous les selects Motivation/Ressources -- corrige le vrai manque identifié par l'audit (aucune grille visible nulle part avant). `updateCoupleSrOv` (existait dans `api.ts`, jamais câblé) enfin utilisé : cette section n'avait aucune édition possible avant.
- **Dangerosité (A3)** : même schéma doublé (initiale + résiduelle) sur `PartiePrenante`. Migration générée par le scaffolder EF **incorrecte** au premier essai (a inversé les colonnes `NiveauDangerosite`→`NiveauDangerositeRetenu` et `NiveauDangerositeResiduel`→`NiveauDangerositeResiduelRetenu`, ce qui aurait mélangé les valeurs initiale/résiduelle de BioGenTech) -- **leçon à retenir : toujours relire une migration générée par `dotnet ef migrations add` qui contient plusieurs `RenameColumn` sur la même table avant de l'appliquer**, le scaffolder peut mal apparier quand plusieurs colonnes de noms proches sont renommées/ajoutées en même temps. Corrigée à la main (migration `OverrideDangerosite`), vérifiée par curl que les 4 parties prenantes de BioGenTech gardent leurs valeurs exactes après coup. Frontend : pas de `GrilleMatrice` ici (4 entrées continues, pas une matrice 2D) -- aperçu live recalculé localement à chaque changement de select à la place.
- **Vraisemblance (A4)** : seul cas où une valeur passe de "jamais stockée" (`[Ignore]`) à "partiellement stockée" (`VraisemblanceRetenue` persistée). Migration cette fois correcte du premier coup (purement additive, aucun renommage nécessaire).

Nouveau `frontend/src/lib/calculsEbios.ts` : miroir TS des 3 `ServiceCalcul*.cs` (matrices + formule), dupliqué sciemment sans test de parité automatisé -- même choix déjà fait pour les textes de grille du PDF. Nouveau composant partagé `OverrideJugementExpert.tsx` (bandeau repliable, fermé par défaut, affiche la valeur calculée en lecture seule + champ de saisie + justification obligatoire + bouton réinitialiser) réutilisé identiquement sur les 3 calculs.

### Diagrammes -- régénérés, pas juste documentés en texte

Découverte utile en cours de route : les 2 PDF de `docs/architecture/` ne sont pas des fichiers figés sans source -- leur vraie source est du Mermaid embarqué dans du HTML publié comme Artifact plus tôt dans cette session (`uml-ebiosrm.html`/`explication-ebiosrm.html`, retrouvés dans le scratchpad de session, copiés dans le repo sous `docs/architecture/uml-source/artifact-*.html`). Un dossier PlantUML parallèle existait aussi dans le scratchpad (`02-classes.puml` etc., mis à jour également par cohérence) mais s'est avéré être une exploration abandonnée, jamais utilisée pour produire les PDF réels -- gardé comme référence alternative, mais `artifact-*.html` est la seule source qui compte.

**Piège rencontré en régénérant les PDF localement** (absent en environnement Artifact, qui échappe correctement avant insertion) : charger le HTML brut dans un navigateur fait interpréter le `<` de chaque stéréotype Mermaid (`<<domain service>>`) comme le début d'une balise HTML, cassant silencieusement TOUT bloc `<pre class="mermaid">` qui en contient ("Syntax error in text"). Diagnostiqué par bisection (isoler le fragment fautif en le rendant seul via Chrome headless jusqu'à trouver la ligne coupable), corrigé en échappant `<`/`>`/`&` à l'intérieur des seuls blocs `<pre class="mermaid">` avant rendu. Pipeline reproductible capturé dans `docs/architecture/uml-source/regenerate-pdfs.sh` (Chrome headless + mermaid.min.js récupéré à la volée, `@page` CSS pour forcer le bon format papier -- A3 paysage pour les diagrammes, A4 pour l'explicatif, tailles vérifiées identiques aux PDF déjà publiés). `plantuml.jar`/`graphviz` (22M+7.8M de binaires, jamais utilisés pour les PDF réels) retirés du repo après coup pour ne pas alourdir l'historique git.

Contenu mis à jour dans les 2 PDF : attributs `Retenue`/`Justification` sur les 3 classes concernées, `Phase` déplacé de `ModeOperatoire` vers `ActionElementaire` (corrigeait une erreur de placement déjà présente avant ce chantier), noms de service de domaine corrigés pour matcher le code réel (`ServiceEvaluationMenaceEcosysteme`→`ServiceCalculNiveauDangerosite`, `ServiceEvaluationVraisemblance`→`ServiceCalculVraisemblance` -- ces deux noms divergeaient du code depuis le début, corrigé au passage), et les mentions "Menace"/"EntiteResponsable"/"PACS" encore présentes dans le texte explicatif (oubliées lors du renommage terminologique de la mise à jour précédente) alignées sur EBIOS RM 1.5.

### Vérifications

`dotnet build` vert, `dotnet test` 63/63 verts (12 nouveaux tests : `ActionElementaire`/cardinalité 1..*, override/reset round-trip par calcul), `npm run build` vert. Round-trips curl complets sur l'étude BioGenTech réelle pour les 2 features (création, override, reset, vérification PDF via capture d'écran -- `pdftotext` s'est révélé peu fiable pour les tableaux multi-lignes de ce projet, l'ordre de lecture linéarisé mélange les colonnes ; toujours vérifier visuellement via `pdftoppm` en cas de doute avant de conclure à une erreur).

### Prochaine action

Les deux features sont closes et l'Atelier 5 peut démarrer sur des bases saines : terminologie 1.5 dès le départ, pattern jugement d'expert déjà établi et prêt à être répliqué sur `NiveauRisque` (Gravité x Vraisemblance), `ActionElementaire` bouclant correctement vers l'Atelier 1. Le point de workflow (démarrage libre vs séquentiel) évoqué dans l'audit n'a pas été tranché -- pas de RBAC existant, donc pas bloquant, à revisiter si l'utilisateur le souhaite.

---

## Mise à jour — Généralisation du snapshot d'A1 à A2/A3/A4

**Demande de l'utilisateur** : analyser le système de snapshot de l'Atelier 1 pour vérifier si c'est la meilleure pratique, et l'uniformiser sur les 4 ateliers -- constat de départ : impossible de justifier pourquoi seul A1 avait ce mécanisme.

### Diagnostic

Retrouvé dans ce fichier (section "Slice 2") : la décision de ne pas donner de snapshot à A2 était justifiée à l'époque par *"il n'existe pas encore de notion de validation d'atelier pour A2-A5"*. Cette condition n'existe plus depuis longtemps (A2/A3/A4 ont leur propre `demarrer/valider/rouvrir` depuis plusieurs mises à jour), mais personne n'était revenu généraliser le snapshot en conséquence -- une décision provisoire jamais révisée après que sa justification est devenue caduque, pas un choix délibéré. Autre indice trouvé dans le code : `valider-atelier1` contient déjà un commentaire *"P13 -- jamais d'étude 'Validee' sans son snapshot... voir audit architectural, constat critique"*, preuve qu'un audit antérieur avait déjà identifié ce risque et corrigé A1 spécifiquement, sans généraliser.

Vérifié à l'utilisateur avant d'agir (recherche web) : ce n'est pas une lubie du projet -- ISO 27001 §7.5.2/7.5.3 exige explicitement la séparation brouillon/approuvé avec versionnement numérique, et le "snapshot pattern" est un standard reconnu en architecture logicielle pour les systèmes à conséquences (audit, conformité). Le vrai risque de conformité était l'écart entre A1 et A2/A3/A4, pas le mécanisme lui-même.

### Conception actée avec l'utilisateur

- Généraliser tel quel le mécanisme d'A1 à A2/A3/A4 : transaction unique statut+snapshot à la validation, versionnement, reporting qui ne lit plus que le snapshot (jamais les agrégats vivants).
- Bouton "Télécharger le PDF" désormais visible uniquement après validation sur les 4 ateliers (aligné sur A1, plus de "toujours visible").
- **Bandeau d'avertissement retiré** ("l'atelier est validé, modifier ne met pas à jour le PDF...") -- jugé inutile par l'utilisateur, retiré aussi d'A1 où il existait déjà, pour une cohérence totale entre les 4 ateliers (pas de nouvelle asymétrie créée).
- Confirmé avec l'utilisateur avant de coder, avec un exemple concret : modifier une donnée pendant que l'atelier est "Validée" ne change PAS le PDF déjà généré -- il faut Rouvrir → modifier → Revalider pour qu'un nouveau snapshot (version N+1) soit créé et que le PDF change. C'est voulu (P16), pas un bug.

### Implémentation

`SnapshotAtelier1` → `SnapshotAtelier` générique (ajout `NumeroAtelier`), même principe que `docs/architecture/uml-source/02-classes.puml` qui prévoyait déjà cette classe générique depuis le début (le code avait divergé de son propre diagramme de référence). `ISnapshotAtelier1Repository`/`SnapshotAtelier1Repository` → `ISnapshotAtelierRepository`/`SnapshotAtelierRepository`, méthodes paramétrées par `numeroAtelier`.

**Piège de migration rencontré et corrigé** : le scaffolder EF a d'abord proposé un `DropTable`+`CreateTable` pour la table `snapshots_atelier1`→`snapshots_atelier`, ce qui aurait détruit les 13 snapshots déjà en base (dont les 2 versions de BioGenTech). Corrigé à la main en `RenameTable` + `AddColumn NumeroAtelier` (defaultValue 1, sémantiquement correct ici puisque toutes les lignes existantes viennent bien de l'ancien mécanisme spécifique à A1 -- pas le piège habituel de `defaultValue: ""`) + renommage de la contrainte PK via `ALTER TABLE ... RENAME CONSTRAINT` (le `RenameIndex` d'EF ne suffit pas pour une PK). Vérifié après coup : les 13 lignes existantes intactes, `NumeroAtelier = 1` correctement backfillé.

Pour A2/A3/A4, même patron que A1 reproduit trois fois : un `SnapshotAtelierNContent.cs` (DTO de sérialisation pur, références par Id conservées -- pas de libellés pré-résolus, le service de rapport fait la même jointure qu'avant, juste depuis le contenu figé au lieu des agrégats vivants, changement minimal de la logique déjà éprouvée) + un `ServiceCreationSnapshotAtelierN.cs` (assemble depuis les repos vivants, appelé uniquement à la validation) + `RapportAtelierNService.cs` réécrit pour ne dépendre que d'`ISnapshotAtelierRepository` (plus aucune dépendance aux repos vivants, comme A1 déjà) + `Program.cs` : `valider-atelierN` enveloppé dans la même transaction unique statut+snapshot que A1 (tout ou rien).

### Vérifications

`dotnet build`/`test` (63/63 verts, aucun changement de comportement testé unitairement). Round-trip complet par curl sur l'étude BioGenTech réelle, atelier par atelier (A2, A3, A4) : `rouvrir` → `valider` → `snapshotVersion: 1` confirmé dans la réponse → PDF téléchargé et vérifié. **Test spécifique du mécanisme central** : override de jugement d'expert posé sur un couple SR/OV *sans* revalider → PDF retéléchargé → contenu **strictement identique** (diff `pdftotext` ne montre que l'horodatage du pied de page) → confirmé que la modification n'apparaît pas tant qu'on n'a pas revalidé → `rouvrir` + `valider` → `snapshotVersion: 2` → PDF retéléchargé → la note "jugement d'expert" apparaît bien cette fois. Vérification visuelle (capture d'écran via Chrome headless, `pdftotext` s'étant déjà montré peu fiable sur les tableaux multi-lignes de ce projet) que les 4 ateliers affichent désormais la même UI : bouton PDF absent tant que non validé, "Rouvrir + Télécharger" une fois validé, aucun bandeau nulle part.

État final de l'étude BioGenTech : les 4 ateliers ont chacun au moins un snapshot en base (`SELECT NumeroAtelier, Version FROM snapshots_atelier` → A1 v1/v2, A2 v1/v2, A3 v1, A4 v1).

### Prochaine action

Les 4 ateliers implémentés sont maintenant strictement uniformes sur le mécanisme de snapshot/reporting -- aucune justification à donner sur une différence de traitement, le point de départ de la demande utilisateur est clos. L'Atelier 5, quand il sera construit, doit suivre le même patron dès le départ (pas de dérogation "on verra plus tard" comme cela avait été le cas pour A2-A4).

---

## Mise à jour — Audit de couverture de test + comblement des lacunes (backend, intégration, frontend)

**Demande de l'utilisateur** : "es ce que tout type de teste a ete fait depuis l'atelier 1 jusqu'a maintenant" puis "faisons un test complet de tout maintenant et tu passe en mode automatique".

### Constat de l'audit initial

Avant ce chantier : 63 tests xUnit concentrés sur 7 classes seulement (sur toutes celles du domaine), **aucun test d'intégration** (aucun endpoint jamais exercé automatiquement -- uniquement du curl manuel pendant les sessions, jamais rejoué), **aucun test frontend** (pas de Vitest/Jest/Testing Library/Cypress dans `package.json`), **aucune CI**. Toute la confiance reposait sur la mémoire de ce qui avait été testé manuellement, pas sur un filet de sécurité rejouable.

### Ce qui a été fait

**Couverture unitaire backend complétée (61 → 159 tests domaine)** : ajout de tests pour toutes les classes/services qui n'en avaient jamais eu -- `BienSupport`, `SocleSecurite`/`ReferentielApplicable`, `ScenarioStrategique`, `CheminAttaque`/`EvenementIntermediaire`, `ServiceCalculNiveauDangerosite` (formule + seuils de zone, exhaustif), `ServiceCalculVraisemblance` (grille 4x4 complète, même patron que `ServiceCalculPertinenceTests` déjà existant), et surtout les 4 `ServiceValidationCompletudeAtelierN` et les 4 `ServiceCreationSnapshotAtelierN` -- ces derniers nécessitaient des doublures de repository en mémoire (`tests/EbiosRM.Api.Tests/TestDoubles/FakeRepositories.cs`, une classe `FakeXxxRepository` par interface, pas de librairie de mock, cohérent avec le reste du projet) pour être testables sans base de données réelle.

**Tests d'intégration HTTP créés de zéro (0 → 2, dont un parcours complet)** : `Microsoft.AspNetCore.Mvc.Testing` ajouté au projet de tests, `Program.cs` gagne `public partial class Program {}` (requis par `WebApplicationFactory<Program>`, sans quoi la classe générée par les top-level statements est inaccessible). Base Postgres dédiée `ebiosrm_test` créée sur le même serveur que le Postgres de dev (jamais `ebiosrm`, qui contient les données de démo BioGenTech) -- script `scripts/setup-test-db.sh` pour la recréer/migrer de façon reproductible (à relancer après chaque nouvelle migration EF). `CycleDeVieCompletTests.cs` : un seul test délibérément long et linéaire (pas des tests isolés artificiellement indépendants) qui crée une étude réelle et la fait traverser les 4 ateliers via l'API HTTP complète -- démarrer/saisir/valider sur chacun, les 2 mécanismes d'override (pertinence A2, vraisemblance A4) avec vérification explicite que modifier sans revalider ne change PAS le PDF déjà généré (taille de fichier comparée avant/après), la cascade de suppression (couple → scénario → chemin → scénario opérationnel), et un cas d'erreur (bien support d'une autre étude refusé, 400). C'est le test qui manquait le plus : il aurait détecté toute régression sur un endpoint, ce qu'aucun test précédent ne pouvait faire.

**Outillage de test frontend créé de zéro** : Vitest + React Testing Library + `@testing-library/user-event` ajoutés, `vite.config.ts` étendu (bloc `test`), `npm run test` disponible. Tests écrits pour les 2 composants partagés les plus critiques et les plus récents : `GrilleMatrice.tsx` (affichage des valeurs, mise en évidence de la cellule sélectionnée) et `OverrideJugementExpert.tsx` (fermé par défaut, jamais deux valeurs affichées en concurrence -- test explicite de ce principe --, refus de soumission sans justification, soumission correcte, bouton "Réinitialiser" conditionné à l'existence d'un écart).

**Corrigé au passage** : les commentaires de `RouvrirAtelier2/3/4()` dans `Etude.cs` affirmaient encore "pas de snapshot à préserver ici" -- faux depuis la généralisation du snapshot de la mise à jour précédente, jamais mis à jour à ce moment-là. Trouvé en écrivant les tests de `ServiceCreationSnapshotAtelierN`, corrigé pour les 3 ateliers.

### Vérifications

`dotnet build`/`test` : 161 verts (159 unitaires + 2 intégration). `npm run build` vert. `npx vitest run` : 10 verts. Vérification manuelle navigateur complémentaire (capture d'écran Chrome headless, seule méthode disponible en l'absence d'outil de navigateur dédié) sur les pages jusqu'ici jamais vérifiées cette session : liste des études, tableau de bord d'une étude, Atelier 3, et l'Atelier 5 (placeholder honnête "pas encore implémenté", confirmé correct).

### Ce qui reste un vrai manque (périmètre volontairement non couvert, à ne pas prétendre testé)

Pas de CI/CD (aucun pipeline `.github/workflows` -- les tests ne s'exécutent que si quelqu'un pense à les lancer). Pas de test de sécurité (aucune authentification n'existe, jamais vérifié comme un choix assumé). Pas de test de charge/performance, d'accessibilité, ni de compatibilité navigateurs. Les migrations `Down()` ne sont jamais réellement exécutées (seul `Up()` est vérifié par l'usage). Le test d'intégration couvre un parcours nominal complet mais pas tous les cas d'erreur possibles sur chacun des ~150 endpoints. "Test complet de tout" reste donc une amélioration substantielle du filet de sécurité, pas une garantie absolue -- à ne jamais présenter comme telle à l'utilisateur.

### Prochaine action (close, remplacée par le chantier ci-dessous)

Base de test solide pour aborder l'Atelier 5 : le patron `FakeRepositories` + `EbiosApiFactory` est réutilisable tel quel pour les futurs agrégats (`ScenarioDeRisque`, `PlanTraitementRisque`). Envisager d'ajouter une CI (GitHub Actions : `dotnet test` + `npm run build` + `npm run test` à chaque push) comme prochain filet de sécurité naturel, non demandé explicitement mais cohérent avec ce chantier.

---

## Mise à jour — Atelier 5 (Traitement du risque), couverture complète des 5 ateliers

Avant tout code, recherche approfondie sur 3 sources indépendantes (demande explicite de l'utilisateur, "c'est la partie la plus sensible, elle doit fournir le document de synthèse final") : les 2 PDF de formation ANSSI officiels du dossier `Sources/` (`15 ATELIER+5+partie+1.pdf`, `16 ATELIER+5+partie+2.pdf`), le supplément ANSSI "Fiches méthode" (Fiche 9), et l'outil open-source réel `ebios-rm-pro` (MIT) inspecté pour son modèle de données de production, complétée par ISO/CEI 27005:2022 sur l'acceptation formelle du risque.

**Correction de terminologie actée** : une note antérieure de ce fichier (§ juste au-dessus) proposait "Propriétaire" pour qui exécute une mesure de traitement, par analogie avec le renommage VM/BS `EntiteResponsable`→`EntiteProprietaire`. C'était une erreur de généralisation. Le tableau officiel ANSSI a pour colonnes exactes *Mesure de sécurité | Scénarios de risques associés | **Responsable** | Freins et difficultés | Coût/Complexité | Échéance | Statut* — confirmé indépendamment par le champ `resp` du modèle de données d'`ebios-rm-pro`. "Responsable" (qui exécute une mesure) et "Propriétaire du risque" (qui possède un risque, registre d'acceptation) sont deux rôles ISO/CEI 27005:2022 distincts qui coexistent sans contradiction avec le renommage précédent.

### Modèle de domaine livré

- **`ScenarioDeRisque`** (`Domain/ScenariosDeRisque/ScenarioDeRisque.cs`) : agrégat léger ancré 1:1 sur `CheminAttaqueId` (même schéma que `ScenarioOperationnel`). Le niveau de risque **initial** n'est jamais stocké ni dupliqué (P8) : Gravité (de l'`EvenementRedoute` visé) et Vraisemblance (`ScenarioOperationnel.VraisemblanceGlobale`) sont lues en direct par le nouveau service `ServiceAssemblageScenariosDeRisque`, qui joint les agrégats vivants à la demande (utilisé à la fois par `GET .../scenarios-de-risque` et par la création du snapshot). Seul l'écart de jugement d'expert sur ce niveau initial est stocké. Le risque **résiduel**, à l'inverse, exige une nouvelle saisie (Gravité résiduelle + Vraisemblance résiduelle) et son propre niveau calculé — mêmes conventions que `PartiePrenante`/dangerosité résiduelle.
- **`ServiceCalculNiveauRisque`** : grille officielle Gravité(1-4) × Vraisemblance(V1-V4) → Faible/Moyen/Eleve (seuils par défaut du projet, ajustables, pas des valeurs universelles imposées par la doc), plus `DeterminerClasseAcceptation` (Faible→Acceptable en l'état, Moyen→Tolérable sous contrôle, Eleve→Inacceptable).
- **Acceptation formelle par la Direction**, directement sur `ScenarioDeRisque` (6 champs plats, comme les 4 champs de Dangerosité plutôt qu'une entité séparée) : `NomProprietaireRisque`/`NomValidateurSecurite` toujours exigés, `NomSponsorExecutif`+`JustificationAcceptation` exigés uniquement si le résiduel reste Eleve (ISO/CEI 27005:2022 + exemple officiel "la direction a maintenu R3 à un niveau résiduel élevé... [raison]"). **Ce projet comble ici une lacune identifiée dans `ebios-rm-pro`**, qui n'a aucune formalisation de cette acceptation.
- **`PlanTraitementRisque`/`MesureTraitementRisque`** (`Domain/ScenariosDeRisque/PlanTraitementRisque.cs`) : même moule que `SocleSecurite`/`ReferentielApplicable` (1 plan par étude, collection owned de mesures). 4 axes fixes (`AxeMesure` : Gouvernance/Protection/Défense/Résilience), `NiveauCoutComplexite` (+/++/+++), `StatutMesure` (ALancer/EnCours/Termine). Many-to-many mesure↔scénarios de risque via `List<Guid> ScenariosDeRisqueIds` (pas d'entité de jointure classique, cohérent avec l'absence de vraies FK entre agrégats séparés dans tout ce projet) — **premier usage réel de `PrimitiveCollection` d'EF Core 8** dans ce projet (mappe nativement vers une colonne Postgres `uuid[]`).
  - **Piège EF rencontré** : `PrimitiveCollection(m => m.ScenariosDeRisqueIds)` sur la propriété publique (typée `IReadOnlyList<Guid>`, get-only) échoue au scaffolding ("cannot be used as a primitive collection because it is read-only"). Corrigé en ciblant directement le champ privé par son nom : `mesure.PrimitiveCollection<List<Guid>>("_scenariosDeRisqueIds")` — fonctionne car EF matérialise alors sur le type de champ réel (`List<Guid>`), pas sur le type d'interface exposé publiquement. Migration vérifiée : colonne bien générée en `uuid[]` (pas `text`/`jsonb`).
- Cascade de suppression étendue aux 4 endroits existants qui cascadaient déjà vers `ScenarioOperationnel` (`couples-sr-ov`, `scenarios-strategiques`, `chemins-attaque`, et **nouveau** : `scenarios-operationnels` lui-même, qui ne cascadait vers rien avant) — helper unique `SupprimerScenarioDeRisqueEtReferencesAsync` qui supprime le `ScenarioDeRisque` et nettoie sa référence dans `PlanTraitementRisque.Mesures` sans jamais supprimer la mesure elle-même.
- `ServiceValidationCompletudeAtelier5` (pattern déjà en place pour A2/A3/A4) : au moins un scénario de risque requis, avec risque résiduel évalué sur chacun avant validation.
- Snapshot/reporting Atelier 5 suit exactement le patron uniforme A1-A4 (`SnapshotAtelier5Content`, `ServiceCreationSnapshotAtelier5`, `RapportAtelier5Data/Service/PdfGenerator`, `numeroAtelier: 5`).

### Document de synthèse final — nouveau module distinct

Conformément à la décision actée avant codage : le document présenté à la Direction en fin d'étude est un **rapport séparé** du rapport d'Atelier 5 (`Modules/Reporting/RapportSyntheseGlobaleData/Service/PdfGenerator.cs`), qui lit les **5** `SnapshotAtelier` d'une étude (numéros 1 à 5) — retourne `null`/400 si l'un des 5 manque, donc uniquement disponible une fois l'Atelier 5 validé. Consolide identité de l'étude, chiffres clés A1-A4 (valeurs métier, biens support, événements redoutés, parties prenantes critiques, scénarios stratégiques/opérationnels), cartographie avant/après, avancement du plan de traitement, registre d'acceptation. Endpoint `GET .../rapports/synthese`. L'infrastructure de snapshot généralisée (chantier précédent) a permis ce module sans aucun nouveau mécanisme de persistance.

### Vérification effectuée

`dotnet build`/`dotnet ef migrations add AjoutAtelier5TraitementRisque` (migration relue manuellement — `defaultValue` de `StatutAtelier5` corrigé de `""` à `"Brouillon"` avant application, même vigilance que sur les migrations précédentes) → `dotnet ef database update` sur `ebiosrm` (dev) et `ebiosrm_test` (intégration). `dotnet test` : 209 verts (207 unitaires incl. 48 nouveaux tests A5 + 2 intégration, le test `CycleDeVieCompletTests` étendu avec le parcours A5 complet avant la cascade de suppression finale). `npm run build`/`tsc --noEmit`/`npm run test` verts côté frontend (`ScenariosDeRisqueSection`, `PlanTraitementRisqueSection`, `AcceptationFormelleSection` ajoutés à `AtelierPage.tsx`, réutilisant `GrilleMatrice`/`OverrideJugementExpert` tels quels). Vérification manuelle complète par curl sur une étude réelle (cycle démarrer→matérialiser→évaluer résiduel→plan→accepter→valider A5→télécharger les 2 PDF, cartographie et grille conformes à l'exemple officiel) et par capture d'écran Chrome headless du navigateur (formulaire d'acceptation exigeant dynamiquement sponsor+justification quand le résiduel est Eleve, plan groupé par axe, cascade de suppression vérifiée : le scénario de risque disparaît et la mesure perd sa référence sans être supprimée).

Couverture des 5 ateliers EBIOS RM désormais complète (domaine, EF Core, endpoints, snapshot P13/P16 uniforme, reporting PDF par atelier + synthèse globale, frontend, tests unitaires et d'intégration).

## Mise à jour — Déploiement public gratuit (Neon + Render + Vercel)

Demande explicite : rendre l'application testable à distance par un tiers, sans coût. Pile retenue après plusieurs impasses : **Fly.io écarté** (exige une carte bancaire même pour son quota gratuit) ; **Neon** (Postgres serverless, gratuit sans carte) + **Render** (API, service Docker gratuit sans carte) + **Vercel** (frontend statique, gratuit) retenus.

**Points techniques notables** :
- `CORS` passé en `AllowAnyOrigin()` (le split frontend/API sur deux origines différentes ne tolère pas la liste blanche `localhost` de dev).
- **Bug de portabilité corrigé avant déploiement** : les polices QuestPDF (Fraunces, IBM Plex) n'étaient jamais enregistrées par code, l'app comptait implicitement sur des polices système installées sur le poste de dev — invisible en local, aurait cassé silencieusement le rendu des PDF une fois conteneurisé. Corrigé par `QuestPDF.Drawing.FontManager.RegisterFont` sur les `.ttf` de `Assets/Fonts` au démarrage (`Program.cs`), rendant l'app auto-suffisante.
- `Dockerfile` multi-stage (`mcr.microsoft.com/dotnet/sdk:8.0` build → `mcr.microsoft.com/dotnet/aspnet:8.0` runtime), image poussée sur **GitHub Container Registry** (dépôt `github.com/joinito18/ebiosrm`, public par choix explicite de l'utilisateur) car Render exige soit un dépôt Git connecté via leur dashboard (pas automatisable par API), soit une image de registre — GHCR + un jeton classique `write:packages` (le token OAuth par défaut de `gh auth login` n'a pas ce scope) était la voie la plus scriptable.
- Service Render créé et administré entièrement via leur **API REST** (`api.render.com`, distincte de `dashboard.render.com`) : création du service, des identifiants de registre, des variables d'environnement (`Jwt__Secret`, `ConnectionStrings__EbiosDb`), déclenchement de déploiements — tout scriptable sans jamais ouvrir le dashboard.
- **Panne Render prolongée rencontrée** : `dashboard.render.com` et tous les `*.onrender.com` hébergés sont devenus injoignables (`connection reset` TLS) pendant plusieurs heures, confirmé indépendamment depuis deux réseaux différents (le sandbox et le poste de l'utilisateur), alors que `api.render.com` restait fonctionnel — panne d'infrastructure côté Render non reconnue sur leur page de statut publique, pas un problème côté projet.

Étude de démonstration entièrement peuplée par script (`build_etude.py`, réutilisable) pour valider le rendu en conditions réelles : 15 valeurs métier, 10 biens support, 5 ateliers validés de bout en bout.

## Mise à jour — Responsivité mobile

Audit déclenché par un retour utilisateur ("le côté gauche n'est pas visible sur mobile"). Deux bugs bloquants trouvés par lecture de code puis confirmés par Playwright à 375px (pas seulement supposés) :
- **Menu latéral totalement invisible sous 1024px** (`Sidebar.tsx` utilisait `hidden ... lg:flex` sans aucun repli mobile, ni bouton pour le faire réapparaître) — corrigé par un tiroir coulissant (bouton hamburger dans `Header.tsx`, fond semi-transparent cliquable pour fermer, fermeture automatique à la navigation).
- **Parcours méthodologique horizontal tronqué à 3 ateliers sur 5** (`AtelierChainExpanded` utilisait `overflow-hidden` sans point de rupture responsive, les colonnes en trop étaient rognées silencieusement) — corrigé en défilement horizontal avec `snap-x` sous `lg`.

Corrections mineures associées : grille de chiffres clés du tableau de bord (`flex-wrap` + `divide-x` produisait des bordures parasites au retour à la ligne, remplacé par une grille CSS avec `gap-px`), tableau des études (colonne périmètre masquée sur mobile plutôt que de déborder), formulaires de mesure de traitement (`grid-cols-3` illisible sur petit écran).

## Mise à jour — Authentification

Nécessité posée par le déploiement public : sans mur d'entrée, n'importe qui avec le lien pouvait lire/modifier/supprimer n'importe quelle étude. Décisions actées avec l'utilisateur avant codage : **comptes individuels** (pas un compte unique partagé, cohérent avec les rôles ANSSI Métiers/RSSI/Direction déjà mentionnés dans la méthodologie sans y être encore rattachés) ; **inscription libre** (self-service, pas de création manuelle par un admin) ; **un seul niveau d'accès** (mur d'entrée uniquement, pas de permissions différenciées — une fois connecté, un utilisateur peut tout faire comme avant, et **toutes les études restent visibles par tous les comptes**, espace de travail partagé sans notion de propriétaire).

**Choix technique** : jeton JWT porté en en-tête `Authorization`, pas de cookies de session — le frontend (Vercel) et l'API (Render) étant sur deux origines différentes, un cookie cross-origin exigerait `SameSite=None`/`Secure` + configuration CORS `credentials`, plus fragile qu'un en-tête explicite.

**Modèle de domaine** : nouveau module `Modules/Identity/` (`Utilisateur.cs`, hors du `CoreEngine` car ce n'est pas un concept métier EBIOS RM). Hachage du mot de passe via `Microsoft.AspNetCore.Identity.PasswordHasher<object>` — le paramètre générique `object` plutôt que `Utilisateur` contourne un problème d'œuf-et-la-poule (l'API `PasswordHasher<TUser>.HashPassword` exige une instance de `TUser`, mais `Utilisateur` ne peut exister avant d'avoir son hash puisque son constructeur est privé et `Creer` l'exige) ; l'implémentation par défaut n'utilise de toute façon jamais l'instance passée. `ServiceAuthentification` orchestre inscription/connexion et émission du JWT (expiration 7 jours, pas de refresh token).

**Protection globale plutôt que endpoint par endpoint** : `AddAuthorization(options => options.FallbackPolicy = ...RequireAuthenticatedUser())` protège tous les endpoints existants par défaut sans toucher aux dizaines de `app.Map*` déjà en place — seuls `/auth/inscription`, `/auth/connexion` et `/api/v1/health` sont explicitement exemptés via `.AllowAnonymous()`.

**Deux pièges .NET rencontrés** :
1. Lire `builder.Configuration["Jwt:Secret"]` **avant** `builder.Build()` (au moment de configurer `AddJwtBearer`) semblait correct mais échouait dans les tests d'intégration (`WebApplicationFactory`) : son injection de configuration in-memory ne fusionne dans `builder.Configuration` que juste avant `Build()`, donc toute lecture eager antérieure voit encore la config de base. Corrigé en déplaçant la lecture **à l'intérieur** du lambda `options =>` de `AddJwtBearer` (exécuté paresseusement, à la première requête entrante) — même raisonnement que `builder.Configuration.GetConnectionString("EbiosDb")` déjà lu paresseusement dans `AddDbContext`.
2. Le handler JWT d'ASP.NET Core **renomme silencieusement le claim `sub`** vers l'URI XML historique (`ClaimTypes.NameIdentifier`) par défaut — un `principal.FindFirst("sub")` renvoyait toujours `null` côté `/auth/moi` malgré un jeton valide. Corrigé par `JwtSecurityTokenHandler.DefaultMapInboundClaims = false` en tout début de `Program.cs`.

**Régression découverte après coup** (cf. section Cadre de suivi ci-dessous) : les 6 liens de téléchargement de rapport PDF étaient de simples `<a href>`, qui ne portent pas l'en-tête `Authorization` — cassés silencieusement par le `FallbackPolicy`. Corrigés par téléchargement `fetch` + blob local (`BoutonTelechargerRapport.tsx`), vérifié par un téléchargement réel via Playwright (fichier sur disque, pas seulement un code 200).

Frontend : pages `Connexion.tsx`/`Inscription.tsx` (publiques, hors `AppLayout`), garde de route `RouteProtegee.tsx`, jeton en `localStorage` injecté automatiquement par `apiFetch`, bouton de déconnexion + nom de l'utilisateur courant dans `Sidebar.tsx`.

Limitations assumées, hors périmètre de cette itération : pas de réinitialisation de mot de passe, pas de vérification d'email, pas de cloisonnement des études par organisation/équipe.

## Mise à jour — Suppression d'étude

Manque identifié : aucun `DELETE /etudes/{id}` n'existait, seulement des suppressions de sous-éléments — une étude créée par erreur restait coincée indéfiniment. `ServiceSuppressionEtude` (`Domain/Cadrage/`) purge **table par table** par `EtudeId` (`ExecuteDeleteAsync` d'EF Core 8, dans une transaction) plutôt que de rejouer la cascade fine des endpoints unitaires (ex. `couples-sr-ov`) : chaque agrégat porte déjà `EtudeId` directement, et les entités owned (`Referentiels`, `Mesures`, `EvenementsIntermediaires`, `ModesOperatoires`...) ont une vraie contrainte `ON DELETE CASCADE` en base — comportement par défaut d'EF Core pour les relations `OwnsMany`, vérifié dans les migrations avant de s'y fier. Bouton corbeille sur la liste des études (`Etudes.tsx`), `e.stopPropagation()` pour ne pas déclencher la navigation de la ligne.

## Mise à jour — Cadre de suivi (4e livrable officiel)

Dernier livrable officiel EBIOS RM manquant. Différence structurante avec tous les autres rapports : **il lit l'état courant, pas un `SnapshotAtelier` figé** — sa raison d'être est de suivre une progression qui continue après la validation de l'Atelier 5 (mesures qui passent à "Terminé" au fil des mois, risques résiduels réévalués), un document figé au moment de la validation n'aurait ici aucun sens. `RapportCadreDeSuiviService` réutilise `ServiceAssemblageScenariosDeRisque` (déjà conçu pour lire des agrégats vivants) et `IPlanTraitementRisqueRepository` directement, sans passer par les snapshots. Disponible dès l'Atelier 5 **démarré** (pas besoin d'attendre sa validation complète, contrairement à la synthèse globale) — vérifié par un test d'intégration dédié qui matérialise ce contraste explicitement (la synthèse refuse, le cadre de suivi répond) et qui change le statut d'une mesure entre deux appels pour prouver que le contenu suit sans nouvelle validation.

## Mise à jour — CI/CD (GitHub Actions)

Manque documenté de longue date dans ce fichier ("pas de CI/CD, les tests ne s'exécutent que si quelqu'un pense à les lancer") — devenu actionnable une fois le dépôt sur GitHub (`github.com/joinito18/ebiosrm`, cf. section Déploiement). `.github/workflows/ci.yml` : deux jobs indépendants déclenchés sur push/PR vers `master`.
- **`backend`** : service Postgres 16 en conteneur (`services:`, port `5433` pour matcher exactement la connection string codée en dur dans `EbiosApiFactory.cs`), `dotnet restore`/`build`/puis migrations EF Core appliquées explicitement sur `ebiosrm_test` (`dotnet ef database update --connection ...`, l'outil `dotnet-ef` installé à la volée) **avant** `dotnet test` — sans cette étape les tests d'intégration échoueraient sur une base vide.
- **`frontend`** : `npm ci` (pas `npm install`, reproductible depuis `package-lock.json`) → `npm run build` → `npx vitest run`.

Chaque commande a été rejouée manuellement en local avant de faire confiance au fichier YAML (`dotnet restore/build/test EbiosRM.sln --configuration Release`, `dotnet ef database update --project ...`), puis le premier run réel sur GitHub Actions vérifié via `gh run view` (pas seulement supposé correct parce que ça marchait en local) : les deux jobs passent (backend 59s avec les 227 tests, frontend 21s avec les 25 tests).

## Mise à jour — Refonte visuelle des pages de connexion

Retour utilisateur : les pages `Connexion`/`Inscription` (simple boîte centrée sur fond uni) étaient jugées trop austères. Refondues en mise en page à deux panneaux (`LayoutAuth.tsx`, partagé par les deux pages) : bandeau de marque sombre (`bg-ink`, wordmark + accroche + mention méthode ANSSI, masqué en grande partie sur mobile pour ne garder qu'un bandeau compact) et formulaire sur carte blanche flottant sur le fond `bg-paper` — mêmes tokens de couleur/police et même style de champ (`border-b`, pas de bordure pleine) que le reste de l'application, pour ne pas introduire un langage visuel concurrent.

**Vérification** : capture d'écran desktop + mobile (375px) via Playwright, puis re-exécution complète du script `test-etude-complete-playwright` (0 erreur) pour confirmer que le nouveau balisage n'a pas cassé les sélecteurs du parcours automatisé.

**Aparté méthodologique** : capturer ces pages a révélé que `fonts.googleapis.com` (chargé via `@import` dans `index.css`, préexistant) est injoignable depuis ce sandbox précisément (timeout DNS), faisant indéfiniment patienter `page.goto` avant `domcontentloaded` -- rien à voir avec l'application (un navigateur réel resolvant normalement ce domaine), corrigé pour les besoins du test en bloquant la route via `page.route(...).abort()`. À charge du diagnostic suivant de ne pas confondre ce genre de blocage sandbox avec un vrai bug applicatif.

**Signalement utilisateur clarifié dans la foulée** : rapport d'un échec de connexion/inscription "sur mobile" — reproduit uniquement sur le déploiement en ligne, jamais en local (vérifié explicitement par un test Playwright mobile bout-en-bout : inscription réussie, 201, redirection correcte). Confirmé comme un symptôme de plus de la panne Render prolongée (cf. section Déploiement), pas un bug de code.

## Mise à jour — Refonte visuelle premium (design system + migration AtelierPage)

Retour utilisateur : l'application « ressemble à quelque chose de générique ». Audit complet du frontend avant tout changement (agent dédié, tous les fichiers de `pages`/`components` lus intégralement) : la palette et la typo de base n'étaient pas le problème (bleu Marianne officiel, serif Fraunces, IBM Plex Sans/Mono — déjà distinctif), l'exécution l'était — zéro système d'élévation (`grep shadow` : 2 résultats hors périmètre sur tout `src`), le même bouton retapé à la main 9 fois, la paire d'actions Modifier/Suppr. dupliquée 26 fois, 6+ fonctions `couleurX` rendant du texte coloré brut au lieu d'un vrai badge, `RiskMatrix.tsx` fini mais jamais importé nulle part (code mort), `Rapports.tsx` affichant une étude factice codée en dur, `Parametres.tsx` une coquille vide.

- **`index.css`** : tokens `--shadow-card`/`--shadow-card-hover` (ombres teintées encre, pas gris neutre) et `--ease-premium`.
- **5 nouveaux composants partagés** (`frontend/src/components/shared/`) : `Button` (variantes primary/secondary/ghost/danger), `Badge` (remplace les fonctions `couleurX` — piège rencontré et documenté dans le skill dédié : les classes Tailwind doivent être des chaînes littérales complètes dans `STYLES`, jamais concaténées, sinon échec silencieux sans erreur de build), `Card` (`flat`/`elevated`), `EmptyState`, `RowActions`. `BadgeStatutAtelier` devient un simple wrapper de `Badge`.
- Pages simples migrées : `Etudes`, `Dashboard` (branche enfin `RiskMatrix` sur les vraies données de scénarios), `Rapports` (remplace les données factices par un vrai index cross-études), `Parametres` (vrai contenu : compte + déconnexion), `Header`.
- **`AtelierPage.tsx`** (2929 lignes) migré section par section (tous les 5 ateliers) : tous les boutons CTA → `Button`, toutes les paires Modifier/Suppr. → `RowActions`, toute sévérité/zone/classe affichée (gravité, vraisemblance, niveau de risque, zone de dangerosité, classe d'acceptation, pertinence) → `Badge`, états vides → `EmptyState`, mode édition uniformisé sur `border-l-2 border-signature`. Les paires de confirmation/annulation en style lien texte (« OK »/« Annuler », « Ajouter »/« Annuler » dans les formulaires en ligne déjà ouverts) ont été délibérément laissées telles quelles : pattern déjà cohérent partout, distinct du bouton CTA plein dupliqué que l'audit visait.
- Nouveau skill `.claude/skills/design-premium-frontend/SKILL.md` documentant le système pour que les futurs ajouts ne réintroduisent pas la duplication corrigée ici.

**Exécution** : la migration mécanique d'`AtelierPage.tsx` a été déléguée à un agent fork (tâche longue, répétitive) qui a **planté après ~16 minutes** (« stalled : no progress for 600s ») après avoir couvert environ 90 % du fichier (tous les `RowActions`/`EmptyState` faits, la plupart des `Badge`, tous les boutons CTA top-level) — le fichier restait syntaxiquement valide et compilait (`tsc` propre) au moment du plantage. Les ~10 % restants (4 appels `couleurZone` encore concaténés en classe brute au lieu de passer par `Badge`) ont été terminés directement plutôt que de relancer un second fork.

**Vérification** : `npm run build` + `npx tsc -b --noEmit` propres, `npx vitest run` (25/25), captures Playwright des 5 ateliers en desktop sur l'étude réelle Atlas Assurances Santé + des 4 pages simples en mobile (375px) — deux régressions mobiles réelles trouvées et corrigées à cette étape (`Badge` sans `whitespace-nowrap` se repliait sur deux lignes dans le header à largeur étroite ; grille de statistiques du Dashboard à `grid-cols-2` avec 5 éléments laissant une cellule vide visible en dernière ligne, corrigée en `grid-cols-1` sous `sm`), puis un run complet du script `test-etude-complete-playwright` (0 erreur, étude de test nettoyée via `DELETE /api/v1/etudes/{id}`).

## Mise à jour — Correction du rendu des graphiques PDF sur données peu abondantes

Signalement utilisateur : « les graphiques s'affichent très mal quand la quantité des données traitées n'est pas énorme ». Reproduit concrètement (pas supposé) en générant la synthèse globale d'une étude à données minimales (1 valeur métier, 1 contrôle de socle, 1 seul thème ISO 27001, 1 seule mesure de traitement) puis en rastérisant le PDF (`pdftoppm`) pour l'inspecter page par page. Trois bugs réels confirmés dans `RapportPdfStyle.cs` (fonctions de graphique **partagées par tous les rapports** — Atelier 1, Atelier 5, Cadre de suivi, Synthèse globale — donc un seul correctif profite aux quatre) :

- **`GraphiqueRadar`** dégénère en un point + un trait dès qu'il y a moins de 3 axes (aucune aire ne peut se former avec 1 ou 2 thèmes) — corrigé en masquant la cartographie radar dans `RapportSyntheseGlobalePdfGenerator` quand `ParTheme.Count < 3` (seule la barre de conformité s'affiche alors, en pleine largeur).
- **`GraphiqueBarres`** n'avait aucune piste de fond visible : une valeur à 0% (ex. un seul axe de traitement encore « à lancer ») ne laissait plus rien à l'écran, donnant l'impression d'un graphique cassé plutôt que d'un 0 légitime — corrigé par une piste grise pleine hauteur dessinée systématiquement sous chaque barre. L'étiquette de valeur d'une barre à 100% était aussi rognée contre le bord supérieur du SVG (aucune marge haute réservée) — corrigé en ajoutant une marge dédiée à l'étiquette.
- **Écart de hauteur entre barres et radar** quand les deux sont affichés côte à côte (le radar a un ratio carré, la barre un ratio large — donc des hauteurs rendues très différentes une fois mis à l'échelle par largeur), laissant un grand espace mort visible sous le graphique le plus court. Corrigé en bornant les deux colonnes à une hauteur fixe commune (`.Height(160).AlignMiddle()`) avec mise à l'échelle `.FitArea()` (et non `.FitWidth()`, qui ignore la hauteur disponible et provoquait une `DocumentLayoutException` une fois la hauteur bornée) ; un espacement (`row.Spacing(24)`) a aussi été ajouté entre les deux colonnes, un chevauchement entre l'étiquette d'axe la plus à gauche du radar et le bord de la barre la plus à droite n'étant devenu visible qu'une fois les hauteurs alignées.

**Vérification** : reproduction avant/après par rastérisation PDF (`pdftoppm`) sur l'étude minimale (bug confirmé disparu, page count passé de 4 à 3 grâce à la suppression de l'espace mort) et sur l'étude réelle Atlas Assurances Santé à données abondantes (4 thèmes, pas de régression), `dotnet build` propre, suite de tests complète (227/227), et génération réelle des rapports Atelier 5 et Cadre de suivi (qui réutilisent les mêmes fonctions de graphique) pour confirmer l'absence de régression croisée.

**Suite immédiate — camembert et diagrammes plus imposants (scope limité à la synthèse globale)** : demande de suivi pour remplacer l'anneau fin du socle de sécurité par un vrai camembert (secteurs pleins) et agrandir les autres graphiques. Nouvelle fonction `Camembert` dans `RapportPdfStyle.cs` (secteurs SVG `M-L-A-Z` depuis le centre ; un segment à 100% est tracé en cercle plein car un arc SVG ne peut pas boucler sur 360°) — remplace `AnneauMultiSegments` uniquement dans `RapportSyntheseGlobalePdfGenerator` (Atelier 1/5 gardent leur anneau, non demandé). Comme un camembert n'a pas de trou central pour un chiffre, le taux global (« 55% conforme ») est affiché en tête de la légende à la place. Tailles augmentées, dans ce seul rapport : camembert 100→140, ligne barres+radar 160→210, barre « répartition des contrôles » 220→300, anneau du plan de traitement 90→120, barres « avancement par axe » 280→380, grille de cartographie des risques 34→42 (nouveau paramètre `tailleCase` sur `GrilleCartographie`/`CartographieCompleteAvecLegende`, défaut 34 conservé pour ne pas affecter le rapport Atelier 5 qui partage la même fonction). Revérifié sur l'étude minimale (le camembert à 100% dessine un cercle plein correct, pas de régression) et sur Atlas Assurances Santé, `dotnet test` (227/227).

## Mise à jour — Déploiement automatisé bout en bout (Vercel + Render/GHCR)

Incident découvert en investiguant un signalement utilisateur (« je ne vois pas les modifications, pas de CRUD, rapports pas à jour ») : ni Vercel ni Render ne se redéployaient automatiquement depuis GitHub — les deux étaient restés bloqués sur d'anciennes versions du code, potentiellement depuis bien avant cette session. `DELETE /api/v1/etudes/{id}` renvoyait 405 en production alors qu'il existe dans le code depuis longtemps : preuve directe que l'image Docker déployée datait d'avant son ajout.

- **Frontend** : `vercel git connect` (CLI) reconnecte l'intégration GitHub — un push sur `master` redéploie désormais automatiquement.
- **Backend** : nouveau job `deploy-backend` dans `ci.yml` (après `backend`/`frontend`/`e2e` verts, uniquement sur push vers `master`) : reconstruit l'image Docker, la pousse sur `ghcr.io/joinito18/ebiosrm-api:latest`, puis déclenche le redéploiement Render via son Deploy Hook. Nécessite 2 secrets de dépôt : `GHCR_PAT` (token classique `write:packages`) et `RENDER_DEPLOY_HOOK_URL`.
- **Bug Render additionnel corrigé au passage** : `DOTNET_hostBuilder__reloadConfigOnChange=false` (variable d'environnement Render) — sans ça, `WebApplication.CreateBuilder()` tente de surveiller `appsettings.json` via inotify, et le conteneur Render a atteint la limite d'instances inotify de l'hôte partagé, plantant `Unhandled exception ... IOException` au tout premier démarrage. Comportement par défaut inutile en conteneur (le fichier de config ne change jamais après le build de l'image).
- **Découverte annexe** : `ebiosrm.vercel.app` (URL supposée du projet) est en réalité un site tiers sans rapport ("EBIOS RM Pro v5", Next.js) — deviné sans vérification, erreur reconnue. La vraie URL est `ebiosrm-ten.vercel.app` (Vercel a ajouté un suffixe car "ebiosrm" était déjà pris globalement).

**Vérification** : rebuild + push GHCR + redéploiement manuels d'abord (pour débloquer immédiatement), puis un commit ne touchant que `ci.yml` repoussé pour valider le pipeline `deploy-backend` de bout en bout sur un vrai run (4 jobs verts, service resté joignable en continu pendant le redéploiement). Contenu vérifié en production après coup : vrai camembert visible dans la synthèse globale, page Rapports listant les vrais boutons de téléchargement, une seule étude restante après suppression manuelle d'« Étude Test » demandée par l'utilisateur.

## Mise à jour — Isolation des études par utilisateur

Suite à un état des lieux honnête demandé par l'utilisateur (« la méthodologie EBIOS RM est solide, mais toutes les études sont visibles par tous les comptes — pas de notion d'organisation ») : chantier explicitement choisi parmi 4 proposés (isolation, limitation anti-abus, réinitialisation de mot de passe, export/sauvegarde JSON), avec une exigence explicite de l'utilisateur : l'étude de démonstration (Atlas Assurances Santé, 15 valeurs métier) doit rester visible par tous les comptes malgré l'isolation.

- **`Etude.ProprietaireId`** (`Guid?`, nouvelle colonne nullable, migration `AjoutProprietaireEtude`) : `null` = étude de démonstration publique (comportement historique conservé pour toutes les études déjà en base avant cette migration, backfill implicite via la valeur par défaut NULL) ; un GUID réel = propriétaire exclusif, assigné automatiquement à la création (`POST /etudes` lit le claim `sub` du jeton JWT).
- **Vérification centralisée dans un seul middleware** (`Program.cs`, juste après `UseAuthorization`) plutôt que dans chacun des 50+ endpoints keyés par `etudeId`/`id` -- une omission dans un seul handler aurait laissé une fuite. Le middleware lit la valeur de route (`etudeId` ou `id` selon l'endpoint), charge l'étude, et : renvoie 404 si elle appartient à un autre utilisateur (existence non révélée) ; renvoie 403 sur toute méthode d'écriture (tout sauf GET/HEAD) si l'étude est publique (`ProprietaireId == null`), qu'importe qui fait la demande -- personne ne peut modifier/supprimer la démo par accident, y compris son propre compte si jamais recréée. `GET /api/v1/etudes` (liste) filtré séparément via `IEtudeRepository.ListerVisiblesAsync`.
- **Erreur d'environnement rencontrée en cours de route, sans lien avec ce chantier** : le runtime `Microsoft.NETCore.App` puis `Microsoft.AspNetCore.App` avaient disparu du disque (`/usr/share/dotnet/shared/` vide) alors que `dpkg`/`apt` les croyaient toujours installés -- suppression manuelle externe au projet, pas une régression de ce chantier. Résolu par `sudo apt install --reinstall dotnet-runtime-8.0 dotnet-hostfxr-8.0 dotnet-apphost-pack-8.0 dotnet-targeting-pack-8.0` puis la même chose pour `aspnetcore-runtime-8.0`/`aspnetcore-targeting-pack-8.0`.

**Vérification** : migration appliquée aux deux bases locales (`ebiosrm` dev et `ebiosrm_test` -- la seconde avait été oubliée au premier passage, causant 7 échecs de test avant correction), suite complète `dotnet test` (227/227), puis test fonctionnel réel avec deux comptes distincts créés à la volée : étude privée du compte A invisible dans la liste du compte B, 404 sur accès direct et sur suppression par B, 200 pour A sur sa propre étude ; étude publique (Atlas Assurances Santé) confirmée en 200 sur GET et 403 avec message explicite sur DELETE, peu importe le compte.

**Incident de déploiement causé par ce chantier, corrigé dans la foulée** : le pipeline `deploy-backend` (section précédente) reconstruit et redéploie l'image, mais n'appliquait aucune migration à la base de production -- le push de ce chantier (qui ajoute une colonne) a donc cassé la prod (500 sur `GET /api/v1/etudes`) dès que le nouveau code a tourné contre l'ancien schéma Neon, pendant ~18 minutes avant d'être remarqué. Corrigé en urgence par une migration manuelle (`dotnet ef database update` avec la chaîne de connexion Neon récupérée depuis Render -> Environment -> `ConnectionStrings__EbiosDb`), puis fixé durablement en ajoutant une étape "Appliquer les migrations en production" dans `deploy-backend`, **avant** la reconstruction/redéploiement de l'image -- nouveau secret de dépôt `PROD_DB_CONNECTION_STRING`.

## Mise à jour — Limitation anti-abus à l'inscription

2e des 4 chantiers choisis par l'utilisateur suite à l'état des lieux honnête (voir section isolation ci-dessus). N'importe qui pouvait créer des comptes en masse sans aucune vérification (pas de captcha, pas de confirmation email).

- `Microsoft.AspNetCore.RateLimiting` (intégré au framework, aucune dépendance externe) : politique `"inscription"`, fenêtre fixe de 5 requêtes/heure par IP, appliquée uniquement à `POST /api/v1/auth/inscription` (`RequireRateLimiting`) -- `/auth/connexion` volontairement non limité pour l'instant (pas demandé, aurait un effet différent -- protection anti brute-force plutôt qu'anti-spam).
- **Piège Render évité** : Render est un reverse proxy, donc `Connection.RemoteIpAddress` vaut l'IP du proxy pour toutes les requêtes sans `UseForwardedHeaders()` configuré -- sans ça, tous les utilisateurs auraient partagé un seul quota au lieu d'un quota par IP réelle. `KnownNetworks`/`KnownProxies` vidés explicitement (IP du proxy Render non fixe/connaissable à l'avance, pratique standard sur ce type de plateforme).

**Vérification** : suite complète `dotnet test` (227/227, la crainte que les nombreuses inscriptions de test partagent un seul quota IP dans `WebApplicationFactory` ne s'est pas vérifiée), puis test réel contre le serveur local : 5 inscriptions consécutives à 201, 6e et 7e à 429, connexion sur un compte existant non affectée (401 normal, pas 429).

**Bug de production trouvé immédiatement après déploiement, corrigé dans la foulée** : le rate limit ne se déclenchait jamais en ligne (6 inscriptions consécutives toutes à 201) alors qu'il fonctionnait en local. Cause : `ForwardedHeadersOptions.ForwardLimit` vaut `1` par défaut -- un seul maillon de la chaîne `X-Forwarded-For` est traité, ce qui résout un hop de proxy interne à Render (potentiellement instable d'une requête à l'autre) plutôt que l'IP publique réelle et stable du client, dès qu'il y a plusieurs proxys enchaînés avant le conteneur. Corrigé en mettant `ForwardLimit = null` (remonte toute la chaîne jusqu'au client). Reconfirmé en production après correction : 5 inscriptions à 201, comportement bloquant attendu à partir de la 6e.

## Mise à jour — Export/sauvegarde d'étude en JSON

3e des 4 chantiers choisis par l'utilisateur. Scope volontairement réduit à l'**export seul** (pas d'import/duplication) : reconstruire une étude à l'identique demanderait de remapper les clés étrangères de chaque entité entre elles (biens support -> valeur métier, scénarios stratégiques -> couple + parties prenantes, opérationnels -> chemin d'attaque, etc.) pour une dizaine de types d'entités -- un moteur de remapping disproportionné par rapport au besoin de sauvegarde exprimé. Décision annoncée à l'utilisateur avant d'implémenter, pas découverte après coup.

- **`GET /api/v1/etudes/{etudeId}/export`** : agrège en un seul JSON tout le contenu éditable des 5 ateliers (valeurs métier, biens support, événements redoutés, socle de sécurité, couples SR/OV, parties prenantes, scénarios stratégiques, chemins d'attaque, scénarios opérationnels, scénarios de risque, plan de traitement) via les repositories existants -- pas de nouvelle requête SQL bricolée. Les snapshots figés (déjà des rapports PDF dérivés) volontairement exclus. Bénéficie automatiquement du middleware de visibilité des études (même règle 404 que tout autre endpoint `etudeId`), aucune règle d'accès à écrire en plus.
- **Frontend** : icône de téléchargement (`Download`, lucide-react) ajoutée à côté de la corbeille sur chaque ligne de `Etudes.tsx`, réutilisant tel quel `BoutonTelechargerRapport`/`telechargerRapport()` déjà existants (fetch + blob, pas de `<a href>` qui ne porterait pas le jeton) -- fonctionne pour du JSON exactement comme pour un PDF, aucune modification du composant partagé nécessaire.

**Vérification** : `dotnet test` (227/227), test réel contre le serveur local confirmant le contenu exact de l'export sur Atlas Assurances Santé (15 valeurs métier, 10 biens support, 15 événements redoutés, 5 couples, 4 parties prenantes, 3 scénarios stratégiques/chemins/opérationnels/de risque, 7 mesures, 11 contrôles de socle -- tous les chiffres connus de cette étude tout au long de la session), isolation confirmée sur ce nouvel endpoint (404 pour un compte non propriétaire), `npm run build` + `vitest run` (25/25) côté frontend, et téléchargement réel déclenché via Playwright (`page.waitForEvent('download')`, pas une simple vérification HTTP 200) avec relecture du fichier JSON téléchargé.

## Mise à jour — Incident prod « API injoignable sur mobile » + installation autonome Docker

**Déclencheur** : l'utilisateur signale que le site déployé affiche « impossible de contacter l'API » sur mobile.

### Diagnostic (2 causes, toutes côté config Vercel, aucune côté code)
1. **`VITE_API_BASE` défini nulle part sur Vercel**, et `frontend/.env.production` (qui le fixe à l'URL Render) était **git-ignoré** par la règle `.env.*` du `.gitignore` racine. Le build Vercel retombait donc sur le fallback `http://localhost:5197` de `api.ts` -> le site ne marchait que sur une machine ayant le backend local (d'où « OK sur desktop, KO sur mobile »).
2. **Root Directory du projet Vercel = `.`** au lieu de `frontend` -> tous les builds de prod échouaient depuis des heures (`vite: command not found`, exit 127) ; le site en ligne était un vieux build. Découvert via `vercel ls` (déploiements en `● Error`, 2-3 s) + `vercel inspect --logs`.

### Corrections livrées (commits sur `master`)
- `frontend/.env.production` **versionné** (exception `!frontend/.env.production` dans `.gitignore`) -- ne contient que l'URL publique de l'API, aucun secret. Vite le charge au build de prod.
- **`api.ts` / `apiFetch` durci** : erreur réseau, réponse non-JSON (page d'erreur HTML d'un proxy), 502/503/504 -> ressortent toutes en `ApiError` typée avec message clair, au lieu de faire remonter un `TypeError`/`SyntaxError` que les pages traduisaient par le trompeur « vérifiez que le backend tourne sur localhost:5197 ». Messages `localhost:5197` retirés de `Etudes.tsx`.
- **Réessais automatiques** dans `apiFetch` (3 s, 12 s, 20 s) sur erreur réseau / 502-504 : absorbe le réveil du backend Render (plan gratuit : veille après ~15 min, ~1 min de redémarrage) au lieu d'échouer au premier appel à froid.
- **`.github/workflows/keep-alive.yml`** : ping `GET /api/v1/health` toutes les 10 min de 06h-22h UTC (fenêtre d'usage réelle -> reste sous le quota Render de 750 h/mois, ~510 h consommées ; dépôt public -> minutes Actions gratuites). Le health check testant la connexion DB, ça réveille aussi Neon. Testé : réveil en ~24 s, `{"status":"ok","database":"connected"}`.

### État Vercel restant à corriger (non-code, l'utilisateur doit le faire)
Le **Root Directory** du projet Vercel doit être mis à `frontend` (Settings -> Build and Deployment). Sans ça, les `git push` continuent d'échouer au build ; en attendant, la prod a été redéployée **manuellement** par `npx vercel deploy --prod` depuis `frontend/` (le déploiement en ligne actuel embarque bien le fix de l'URL d'API, vérifié : bundle -> `ebiosrm-api.onrender.com`, plus de `localhost`). L'accès CLI est limité (classifier de l'agent + limite de déploiements/jour du plan gratuit Vercel).

### Installation complète autonome (Docker Compose) -- demande explicite « frontend + backend indépendants »
`docker compose --profile selfhost up -d --build` démarre **3 conteneurs** : `postgres` + `api` (build depuis `src/EbiosRM.Api/Dockerfile`) + `web` (nouveau `frontend/Dockerfile` : build Vite multi-stage -> nginx). Le `docker-compose.yml` utilise un **profil `selfhost`** : sans le profil, `docker compose up` ne démarre que Postgres (workflow de dev inchangé).
- **`frontend/nginx.conf`** : sert la SPA (`try_files ... /index.html` pour les deep links) **et** relaie `/api/` vers `http://api:8080`. Le frontend est buildé avec `VITE_API_BASE=/api/v1` (relatif) -> aucun nom d'hôte à connaître au build.
- **`Program.cs`** : nouveau `if (Configuration.GetValue<bool>("ApplyMigrationsOnStartup")) await db.Database.MigrateAsync()` juste après `builder.Build()`. Désactivé par défaut (en SaaS c'est le pipeline qui migre) ; le compose selfhost le met à `true` -> le conteneur API crée/met à jour le schéma seul au premier lancement.
- **`.env.example`** (racine, versionné) : `JWT_SECRET` (obligatoire), `POSTGRES_PASSWORD`, `WEB_PORT`, `APP_URL`. Le `.env` réel reste git-ignoré.
- **`README.md`** créé (n'existait pas) : installation autonome Docker, environnement de dev, tests, sauvegarde/restauration pg_dump, accès réseau.
- **Vérifié** : les 3 images buildent ; `up` OK ; auto-migration sur base vierge = 32 migrations appliquées + schéma `core_engine` complet ; parcours end-to-end via nginx (inscription -> JWT -> `POST /etudes` 201 -> `GET /etudes`) ; SPA + deep link `/etudes` = 200 ; bundle servi -> `/api/v1` relatif. `dotnet build` + `npm run build` + `vitest` (25/25) verts.

*Fin du contexte.*

