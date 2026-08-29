# Atelier 5 — Traitement du risque

## Ce que demande la méthode

L'Atelier 5 conclut l'analyse :

- **assembler les scénarios de risque** : gravité (de l'événement redouté) ×
  vraisemblance (du scénario opérationnel) → **niveau de risque initial** ;
- décider du **traitement** (réduire, transférer, éviter, accepter) et
  formaliser un **plan de traitement du risque** : mesures, responsables,
  échéances, coût / complexité ;
- **réévaluer** le risque après mesures → **risque résiduel** ;
- **accepter formellement** les risques résiduels (avec sponsor et
  justification quand le résiduel reste élevé).

## Dans l'outil

### Démarrer l'atelier

**Atelier 5** → **Démarrer l'atelier**. Les scénarios de risque sont assemblés
automatiquement à partir des ateliers précédents.

### Scénarios de risque

Section **Scénarios de risque** : chaque ligne croise un scénario stratégique /
opérationnel avec sa gravité et sa vraisemblance. Le **niveau de risque
initial** est calculé ; un jugement d'expert est possible, avec justification.

### Plan de traitement

Section **Plan de traitement du risque** → **Créer le plan**, puis **Ajouter
une mesure de traitement** :

- **Libellé** de la mesure et **scénarios couverts**.
- **Axe** de traitement, **responsable**, **échéance** (texte libre : `MM/AAAA`,
  `JJ/MM/AAAA`…).
- **Coût / complexité** et **statut** (à faire / en cours / terminée).
- **Codes de conformité** (ISO 27001 / NIS2) associés — voir le guide
  *Conformité*.

**Depuis la bibliothèque** propose des mesures (ISO 27002, hygiène ANSSI, vos
mesures). Le bouton **→ biblio.** capitalise une mesure de l'étude dans votre
bibliothèque.

### Risque résiduel

Pour chaque scénario, **Évaluer le risque résiduel** : recotez la
vraisemblance (et éventuellement la gravité) en tenant compte des mesures. Le
**niveau résiduel** est recalculé.

### Acceptation formelle

Section **Acceptation formelle** : pour chaque risque résiduel, enregistrez la
**décision** (accepté / non accepté) et sa **classe** (acceptable en l'état,
tolérable sous contrôle, inacceptable). Quand le résiduel est **élevé**,
l'outil exige un **sponsor** et une **justification**.

## Valider

**Valider l'atelier** demande un plan de traitement et une décision
d'acceptation pour chaque risque résiduel élevé. La validation :

- génère les **rapports PDF** (plan de traitement, grille de risque,
  cartographie résiduelle) ;
- crée une **version** (*snapshot*) — vous pouvez lui donner un **libellé de
  campagne** (« Revue annuelle 2026 ») pour le suivi d'évolution N / N-1.

## Conseils

- Une mesure peut couvrir **plusieurs scénarios** ; un scénario peut être
  couvert par **plusieurs mesures**.
- Le risque résiduel se recote sur la **vraisemblance** en priorité : les
  mesures agissent surtout sur la faisabilité de l'attaque, rarement sur la
  gravité.
- Un risque **accepté** doit rester **tracé et revu** : voir le guide *Suivi et
  portefeuille*.
