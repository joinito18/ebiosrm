# Atelier 1 — Cadrage et socle de sécurité

## Ce que demande la méthode

L'Atelier 1 pose le cadre de l'étude :

- définir le **périmètre métier et technique** et les **participants** ;
- identifier les **valeurs métier** (informations ou processus dont la
  compromission a un impact) et leurs **biens support** (ce sur quoi elles
  reposent : applications, réseaux, personnes, locaux) ;
- identifier les **événements redoutés** (atteinte à une valeur métier) et leur
  attribuer une **gravité** sur l'échelle 1 à 4 ;
- évaluer le **socle de sécurité** : l'écart entre les mesures déjà en place et
  un référentiel (règles d'hygiène ANSSI, ISO 27002…).

## Dans l'outil

### Démarrer l'atelier

Depuis le tableau de bord de l'étude, ouvrez **Atelier 1** puis **Démarrer
l'atelier**.

### Valeurs métier

Section **Valeurs métier** → **Ajouter une valeur métier** :

- **Description** : l'information ou le processus (« Processus de paie »,
  « Référentiel clients »).
- **Entité propriétaire** : la direction responsable (au sens *risk owner*
  d'ISO 27005).

Le bouton **Depuis la bibliothèque** pré-remplit ces champs à partir d'un
catalogue de valeurs métier types.

Recommandation méthodo : viser **5 à 10 valeurs métier**. La règle est souple,
l'outil ne bloque pas.

### Biens support

Section **Biens support** → **Ajouter un bien support** :

- **Valeur métier associée** : un bien support sert au moins une valeur métier.
- **Description** et **type** : Système d'information, Réseau, Ressources
  humaines, Local.
- **Entité propriétaire**.

**Depuis la bibliothèque** propose des biens support types (annuaire AD,
messagerie, ERP, salle serveurs…), filtrables par type.

### Événements redoutés

Section **Événements redoutés** → **Ajouter un événement redouté** :

- **Valeur métier associée**.
- **Description** de l'atteinte (« Indisponibilité prolongée du SI de
  production », « Divulgation du fichier clients »).
- **Gravité** de 1 (mineure) à 4 (critique).

La gravité est **recotable** ensuite (un recalcul des scénarios dépendants s'en
suivra en Atelier 5). **Depuis la bibliothèque** propose des événements
redoutés types avec une gravité indicative à ajuster.

### Socle de sécurité

Section **Socle de sécurité** → **Créer le socle**. Deux façons d'ajouter un
contrôle :

- **ISO/CEI 27001:2022 Annexe A** : choisissez un contrôle dans le catalogue,
  indiquez son **état** (Conforme / Non conforme / Non applicable) et, le cas
  échéant, l'**état actuel** (ce qui est réellement fait).
- **Référentiel libre** : saisissez vous-même l'intitulé, l'état et le thème.

Le socle sert de base au **tableau de conformité** (guide *Conformité*) et
apparaît dans le rapport de l'Atelier 1.

## Valider

**Valider l'atelier** exige au minimum une valeur métier et un événement
redouté. La validation génère le **rapport PDF de l'Atelier 1** (identité de
l'étude, valeurs métier, biens support, événements redoutés cotés, écart de
socle).

## Erreurs fréquentes

- Confondre **valeur métier** (le *quoi*, métier) et **bien support** (le
  *support*, technique).
- Coter la gravité en fonction de la probabilité : à ce stade, la gravité ne
  dépend **que de l'impact**, pas de la vraisemblance (qui vient en Atelier 4).
- Multiplier les biens support très fins : rester au niveau utile pour la suite
  (un bien support = quelque chose qu'une attaque peut viser).
