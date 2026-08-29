# Prise en main

Ce guide explique comment mener une analyse de risque **EBIOS Risk Manager**
avec l'outil, atelier par atelier. Chaque article rappelle d'abord ce que
demande la méthode ANSSI, puis détaille les écrans correspondants.

## Qu'est-ce qu'EBIOS Risk Manager

EBIOS RM est la méthode d'appréciation et de traitement du risque numérique
publiée par l'ANSSI (version 1.5, mars 2024, alignée sur l'ISO/CEI 27005:2022).
Elle se déroule en **cinq ateliers** qui s'enchaînent :

| Atelier | Objet | Produit principal |
|---|---|---|
| 1 — Cadrage et socle de sécurité | Délimiter l'étude, lister les valeurs métier et biens support, identifier les événements redoutés, évaluer le socle | Périmètre, événements redoutés cotés en gravité |
| 2 — Sources de risque | Identifier les couples « source de risque / objectif visé » pertinents | Couples SR/OV retenus |
| 3 — Scénarios stratégiques | Cartographier l'écosystème, construire les scénarios de haut niveau et leurs chemins d'attaque | Scénarios stratégiques, mesures sur l'écosystème |
| 4 — Scénarios opérationnels | Décrire techniquement les chemins d'attaque, coter la vraisemblance | Scénarios opérationnels, modes opératoires |
| 5 — Traitement du risque | Évaluer le risque, décider du traitement, formaliser le plan et l'acceptation | Plan de traitement, risques résiduels acceptés |

La méthode est **itérative** : on peut revenir en arrière, affiner, puis
revalider un atelier.

## Créer une étude

1. Menu **Études** → **Nouvelle étude**.
2. Renseignez le **nom**, la **mission** de l'objet étudié et le **périmètre**
   (ce qui est dans l'analyse et ce qui en est exclu).
3. L'étude est créée à l'état *brouillon*, les cinq ateliers sont vides.

Vous pouvez aussi **importer** une étude (fichier JSON d'export) ou
**dupliquer** une étude existante pour la réutiliser comme modèle
(menu Études, actions sur une ligne).

## Rôles et partage

Une étude a un **propriétaire** et peut être partagée par e-mail avec d'autres
comptes, avec trois rôles :

- **Lecteur** : consultation seule, y compris les rapports.
- **Éditeur** : peut modifier le contenu des ateliers.
- **Propriétaire** : en plus, peut partager, changer les rôles et supprimer
  l'étude.

Toutes les actions sont tracées dans un **journal d'audit** consultable par le
propriétaire.

## Naviguer dans l'outil

- **Tableau de bord** de l'étude : synthèse des chiffres clés et accès aux cinq
  ateliers.
- **Barre latérale** : progression des ateliers (brouillon / en cours /
  validé), accès à la bibliothèque, au portefeuille, aux rapports et aux
  paramètres.
- Chaque atelier se **démarre**, se remplit, puis se **valide** : la validation
  fige une version (*snapshot*) qui alimente les rapports et le suivi
  d'évolution.

## Valider un atelier

Le bouton **Valider l'atelier** vérifie la complétude minimale (par exemple :
au moins une valeur métier et un événement redouté en Atelier 1) puis
enregistre une version datée. Vous pouvez rouvrir un atelier validé pour le
corriger : une nouvelle validation créera une nouvelle version, l'ancienne
reste disponible pour comparer.

## Aller plus loin

- **Bibliothèque** : capitalisez mesures, sources de risque, parties prenantes,
  valeurs métier, biens support, événements redoutés et modes opératoires d'une
  étude à l'autre — voir le guide *Bibliothèque*.
- **Conformité** : croisez votre socle et votre plan de traitement avec
  ISO 27001 ou NIS2 — voir le guide *Conformité*.
- **Portefeuille et suivi** : pilotez plusieurs études et suivez l'évolution du
  risque dans le temps — voir le guide *Suivi et portefeuille*.
