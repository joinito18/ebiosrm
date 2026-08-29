# Atelier 4 — Scénarios opérationnels

## Ce que demande la méthode

L'Atelier 4 descend au **niveau technique** :

- pour chaque chemin d'attaque de l'Atelier 3, décrire un **scénario
  opérationnel** ;
- le décomposer en un ou plusieurs **modes opératoires** (variantes techniques
  possibles) ;
- chaque mode opératoire se décompose en **actions élémentaires** réparties sur
  la séquence type **CONNAÎTRE / RENTRER / TROUVER / EXPLOITER** ;
- chaque action élémentaire **cible un bien support** précis de l'Atelier 1 ;
- **coter la vraisemblance** de chaque mode opératoire (probabilité de succès ×
  difficulté technique), d'où la vraisemblance globale du scénario.

## Dans l'outil

### Démarrer et créer les scénarios

**Atelier 4** → **Démarrer l'atelier**. Chaque chemin d'attaque de l'Atelier 3
apparaît ; pour chacun, **Créer le scénario opérationnel**.

### Modes opératoires

Pour un scénario, **Ajouter un mode opératoire** :

- **Description** du mode.
- **Actions élémentaires** : une ligne par action, avec sa **phase**
  (CONNAÎTRE / RENTRER / TROUVER / EXPLOITER), sa **description**, le **bien
  support ciblé** et, en option, une **technique MITRE ATT&CK**.
- **Probabilité de succès** (1 à 4) et **difficulté technique** (1 à 4). La
  grille officielle en déduit la **vraisemblance** ; la matrice s'affiche en
  direct.

Le bouton **Depuis la bibliothèque** propose des modes opératoires types
(rançongiciel par hameçonnage, intrusion par un accès distant exposé, rebond
par un prestataire, exploitation d'une vulnérabilité web, domination Active
Directory). Il pré-remplit la description, les cotations et les actions ;
**pensez ensuite à associer chaque action au bon bien support de votre étude**
(le libellé de cible importé n'est qu'un repère).

### Techniques MITRE ATT&CK

Le champ **technique** propose un catalogue ATT&CK Enterprise filtré par phase
EBIOS RM. Il aide à décrire le geste et à objectiver la vraisemblance. La
technique retenue est reprise dans le rapport de l'Atelier 4.

### Vraisemblance globale

La vraisemblance d'un scénario opérationnel est celle de son mode opératoire
**le plus vraisemblable** (le plus favorable à l'attaquant). Un jugement
d'expert différent est possible par mode, avec justification.

## Valider

**Valider l'atelier** demande au moins un scénario opérationnel avec un mode
opératoire complet. Le rapport PDF détaille les modes opératoires, leurs
actions par phase et la vraisemblance.

## Conseils

- Ajustez la **granularité** : inutile de décrire 20 actions si 4 suffisent à
  raisonner la vraisemblance.
- Une action élémentaire finit toujours par **toucher un bien support concret**
  identifié en Atelier 1 — c'est le bouclage de la chaîne.
- Deux modes opératoires du même scénario peuvent avoir des vraisemblances très
  différentes : c'est normal, le scénario retient la pire.
