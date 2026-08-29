# Atelier 3 — Scénarios stratégiques

## Ce que demande la méthode

L'Atelier 3 construit une vue de **haut niveau** des chemins d'attaque :

- **cartographier l'écosystème** : les parties prenantes (clients, partenaires,
  prestataires…) et leur **dangerosité** — dépendance, pénétration, maturité
  cyber, confiance — d'où une **zone** (veille, contrôle, danger) ;
- **construire les scénarios stratégiques** : depuis un couple SR/OV retenu,
  vers un événement redouté, en passant éventuellement par des parties
  prenantes ;
- décrire les **chemins d'attaque** de chaque scénario ;
- proposer des **mesures de sécurité sur l'écosystème** pour réduire la
  dangerosité des parties prenantes critiques, puis **réévaluer** la dangerosité
  résiduelle.

## Dans l'outil

### Parties prenantes

Section **Parties prenantes importantes** → **Ajouter une partie prenante** :
nom, rôles et attentes, représentant, catégorie (Client / Partenaire /
Prestataire / Autre). **Depuis la bibliothèque** propose des parties prenantes
types avec des niveaux indicatifs.

### Évaluation de la dangerosité

Section **Évaluation de la dangerosité** : pour chaque partie prenante, cotez
**dépendance**, **pénétration**, **maturité cyber** et **confiance** (1 à 4).
L'outil calcule un niveau et en déduit la **zone**. Un jugement d'expert
différent est possible, avec justification.

Les parties prenantes en zone **Contrôle** ou **Danger** sont *critiques* et
définissent le périmètre réel de l'écosystème.

### Cartographie

La section **Cartographie** affiche, en SVG généré côté serveur :

- le **radar de dangerosité** de l'écosystème (cercles concentriques
  veille / contrôle / danger, bascule initiale / résiduelle) ;
- l'**arbre des scénarios stratégiques** et de leurs chemins d'attaque.

Ces schémas sont repris dans le **rapport PDF de l'Atelier 3**.

### Scénarios stratégiques et chemins d'attaque

Section **Scénarios stratégiques** → **Ajouter un scénario stratégique** :
choisissez le **couple SR/OV**, l'**événement redouté** ciblé et décrivez le
scénario. La **gravité** est héritée de l'événement redouté.

Pour chaque scénario, section **Chemins d'attaque** → ajoutez un ou plusieurs
chemins (par exemple « attaque directe » et « rebond via le prestataire
d'infogérance »). Un chemin peut impliquer une partie prenante.

### Mesures sur l'écosystème

Section **Mesures de sécurité sur l'écosystème** : ajoutez des mesures sur les
parties prenantes critiques (contractualisation, audit, cloisonnement des
accès…). Réévaluez ensuite la **dangerosité résiduelle** (mêmes quatre
critères) : le radar résiduel se met à jour.

## Valider

**Valider l'atelier** exige au moins un scénario stratégique avec un chemin
d'attaque. Le rapport PDF reprend le radar, l'arbre et les scénarios.

## Conseils

- Un scénario stratégique reste **macro** : « l'attaquant compromet
  l'infogérant puis atteint le SI de production ». Le détail technique est
  l'objet de l'Atelier 4.
- La dangerosité n'est pas une accusation : une partie prenante simplement
  *négligente* peut être en zone danger sans intention hostile.
