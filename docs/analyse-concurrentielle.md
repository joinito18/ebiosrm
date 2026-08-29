# Analyse concurrentielle — écarts avec l'état de l'art international

> Rédigée le 2026-08-28. À partir d'une connaissance du marché arrêtée début 2026
> et de recherches ciblées. À réviser périodiquement.

## 1. Positionnement actuel de l'application

**Points forts réels :**

- Couverture **complète et fidèle des 5 ateliers EBIOS RM v1.5**, jusqu'à
  l'Atelier 5 (traitement du risque + acceptation formelle). Peu d'outils
  gratuits vont aussi loin.
- **Chaîne de traçabilité stricte** : valeur métier → bien support → événement
  redouté → couple SR/OV → scénario stratégique → chemin d'attaque → scénario
  opérationnel → mode opératoire → action élémentaire → scénario de risque →
  mesure de traitement.
- **Jugement d'expert traçable** : override « collant » des valeurs calculées
  (pertinence A2, dangerosité A3, vraisemblance A4, niveau de risque A5) avec
  justification, sans jamais afficher deux valeurs concurrentes.
- **3 modes de déploiement** dont un **desktop 100 % hors ligne** (SQLite,
  runtime embarqué, frontend et polices locaux) avec étude d'exemple.
- Reporting PDF : 5 rapports d'atelier + synthèse globale + cadre de suivi.

## 2. Panorama des outils comparables

| Outil | Type | Positionnement |
|---|---|---|
| **Agile Risk Manager** (ALL4TEC) | Commercial | **Seul outil labellisé ANSSI**. EBIOS RM natif, éditeur graphique (v3.0), bibliothèques de référentiels, multi-utilisateur tracé, web + desktop |
| **MONARC** (CASES Luxembourg) | Open source AGPL | ISO 27005, **bibliothèques de modèles réutilisables** (menaces/vulnérabilités/mesures), import/export de modèles, DPIA/RGPD, gain de temps via profils partagés |
| **EGERIE Risk Manager** | Commercial FR | GRC large, **tableaux de bord dynamiques**, intégrations écosystème GRC, collaboratif |
| **Tenacy** | Commercial FR | Pilotage cyber global, **connectivité** avec d'autres solutions |
| **SimpleRisk / Eramba / verinice** | Open source | Registre de risques, plans d'action, **mapping conformité** (ISO 27001, NIST, BSI) |
| **Safe Security (ex-RiskLens) / Kovrr / Black Kite** | Commercial | **Quantification FAIR**, impact financier en €, ingestion continue, reporting conseil d'administration |

## 3. Analyse des écarts

Priorité : 🔴 critique (bloquant usage pro partagé) · 🟠 important (attendu du marché) · 🟢 différenciant.

### 🔴 Critiques

| Écart | État de l'art | Impact |
|---|---|---|
| **Pas de modèle d'organisation / rôles** | ARM, EGERIE : plusieurs analystes par étude, rôles (contributeur / valideur / lecteur), workflow de revue | 1 étude = 1 propriétaire → inutilisable en équipe RSSI |
| **Pas de journal d'audit** | Tous les outils pro : « qui a modifié quoi, quand », horodaté, exportable | Une analyse de risque est un livrable opposable (audit, assurance, autorité) — sans traçabilité des modifications, pas de valeur probante |
| **Pas d'import** (export JSON seul) | MONARC : import/export de modèles, échange entre analyses | Impossible de transférer une étude, repartir d'un modèle, fusionner |
| **Réinitialisation MDP + vérification email absentes** | Standard | Déjà sur une branche pour le MDP ; bloquant pour un déploiement multi-utilisateurs |

### 🟠 Importants

| Écart | Référence |
|---|---|
| **Bibliothèques réutilisables** (sources de risque, modes opératoires types, mesures, parties prenantes) | Signature de MONARC ; ARM fournit une bibliothèque enrichissable. Ici tout se ressaisit à chaque étude |
| **Intégration MITRE ATT&CK** (Atelier 4) | Utilisée pour estimer la vraisemblance des scénarios opérationnels et structurer les modes opératoires |
| **Cartographie graphique de l'écosystème** (A3) | ARM v3.0 = éditeur graphique. Ici : tableaux/badges. La méthode ANSSI attend les cercles concentriques contrôlé / veille / danger |
| **Visualisation des chemins d'attaque** (arbres/graphes) | ARM produit des diagrammes de scénarios ; ici listes texte |
| **Catalogues de mesures multi-référentiels** | ISO 27002, NIST CSF/800-53, CIS, hygiène ANSSI, sectoriels. Ici : catalogue ISO statique frontend |
| **Mapping de conformité** | Croisement ISO 27001 Annexe A, **NIS2**, **DORA**, RGPD. Cœur de verinice/Eramba. Sujet réglementaire majeur 2025-2026 |
| **Tableau de bord exécutif / vue portefeuille** | EGERIE, Tenacy : consolidation multi-études, tendances, reporting COMEX |
| **Cadre de suivi *vivant*** | Ici : PDF figé. Ailleurs : suivi des KRI avec données réelles dans le temps, alertes sur dérive |
| **Ré-évaluation / versions d'une étude dans le temps** | Comparer N / N-1, évolution du risque résiduel |
| **Export Word / Excel** | Les analystes retravaillent le livrable ; PDF + JSON insuffisants en pratique |
| **Multi-langue** | FR uniquement |

### 🟢 Différenciants

| Opportunité | Contexte |
|---|---|
| **Quantification financière (FAIR / Monte Carlo)** | Le marché bascule vers le risque en euros. Un pont EBIOS RM → FAIR sur les scénarios de risque serait unique côté open source |
| **Assistance IA** | Génération assistée de scénarios, suggestion de mesures, revue de cohérence de la chaîne de traçabilité |
| **Intégrations sortantes** | Plan de traitement → Jira/ITSM ; biens support ← CMDB ; croisement scan de vulnérabilités |
| **API documentée + connecteurs** | L'API REST existe déjà (Swagger présent) — la documenter et la présenter comme point d'intégration |
| **Labellisation ANSSI** | Seul ARM l'a. Processus formel long, mais viser la conformité au référentiel structurerait la feuille de route |
| **Bibliothèque de connaissances ANSSI intégrée** | Fiches méthode, exemples, glossaire contextuel dans l'UI (sources déjà dans `Sources/`) |

## 4. Ce que l'application fait déjà mieux que beaucoup

- Rigueur de la chaîne de traçabilité (plus stricte que MONARC, orienté « scénarios par actif »).
- Mode hors ligne / air-gap natif avec étude d'exemple.
- Override du jugement d'expert traçable et « collant ».
- Gratuit et entièrement auto-hébergeable (comme MONARC ; pas ARM/EGERIE).

## 5. Feuille de route recommandée (par ordre)

1. ~~**Socle collaboratif** : modèle organisation + rôles + partage d'étude + **journal d'audit**~~. **Fait** (3 rôles Lecteur/Éditeur/Propriétaire, partage par email, journal append-only, gating lecture seule de l'UI).
2. ~~**Import JSON** (miroir de l'export) + **duplication d'étude** → base de « modèles »~~. **Fait** : `POST /etudes/{id}/dupliquer` (copie interne, accessible aux Lecteurs et à l'étude de démo) et `POST /etudes/importer` (fichier `.json` d'un export, autre installation / transfert). Moteur de ré-attribution des clés partagé (`RecableurClesEtude`), les 5 ateliers repartent en brouillon.
3. **Bibliothèques réutilisables** + **catalogues multi-référentiels** (NIST, CIS, hygiène ANSSI).
4. **Cartographie graphique** (écosystème A3 en cercles concentriques + arbres de chemins d'attaque A3/A4).
5. **Intégration MITRE ATT&CK** (Atelier 4).
6. **Mapping de conformité** ISO 27001 / NIS2 / DORA.
7. **Cadre de suivi vivant** (KRI) + **vue portefeuille** + **ré-évaluation annuelle** (comparaison N / N-1).
8. **Export Word/Excel**, **multi-langue**.
9. Différenciant : **pont EBIOS RM → FAIR** (quantification €) et/ou **assistance IA**.

## Sources

- ALL4TEC — Agile Risk Manager : <https://www.all4tec.com/solutions/agile-risk-manager/>
- ALL4TEC — Release 3.0 : <https://www.all4tec.com/blog/notes-de-version/release-3-0-agile-risk-manager-mise-a-jour-majeure-de-notre-solution-ebios-rm/>
- Club EBIOS — Agile Risk Manager : <https://club-ebios.org/site/en/all4tec-agile-risk-manager/>
- MONARC — comparaison des méthodes : <https://www.monarc.lu/publications/comparison-between-monarc-and-different-risk-management-methods/>
- securitymadein.lu — MONARC : <https://securitymadein.lu/services/monarc-1/>
- ISIT — EGERIE Risk Manager : <https://www.isit.fr/fr/produit/egerie-risk-manager.php>
- Silicon.fr — outiller le risque cyber : <https://www.silicon.fr/Thematique/cybersecurite-1371/Breves/Gestion-du-risque-cyber-pourquoi-il-faut-outiller-387156.htm>
- Kovrr — CRQ buyer's guide 2026 : <https://www.kovrr.com/blog-post/the-best-cyber-risk-quantification-tools-in-2026-a-buyers-guide>
- Safe Security — benchmarking 2026 : <https://safe.security/resources/blog/benchmarking-your-cybersecurity-program-in-2026/>
- Ayi Nedjimi Consultants — cartographie EBIOS RM 2026 : <https://ayinedjimi-consultants.fr/articles/cartographie-risques-cyber-ebios-rm>
