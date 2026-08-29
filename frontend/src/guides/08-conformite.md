# Conformité

Le module **Conformité** croise le contenu d'une étude avec un référentiel
réglementaire pour produire un **tableau de couverture**.

## Principe

Pour chaque exigence du référentiel, l'outil regarde :

- l'état des contrôles correspondants dans le **socle de sécurité** (Atelier 1) ;
- les **mesures du plan de traitement** (Atelier 5) portant le **code de
  conformité** correspondant.

Il en déduit une **couverture** : Conforme, Partielle, Non couverte, Non
applicable.

## Référentiels disponibles

- **ISO/CEI 27001:2022** — les 93 exigences de l'Annexe A.
- **NIS2** — les 10 domaines de l'article 21, avec une **correspondance
  indicative** vers les contrôles ISO (une exigence NIS2 est considérée
  couverte au niveau `max(mesure directe, contrôles ISO associés)`).

> La correspondance ISO → NIS2 est indicative et doit être validée par
> l'analyste.

## Dans l'outil

### Associer un code de conformité à une mesure

Dans l'Atelier 5, sur une mesure de traitement, le sélecteur **Conformité**
permet de cocher les codes ISO 27001 / NIS2 que la mesure adresse (chips
multi-sélection).

### Consulter le tableau

Menu **Conformité** de l'étude (ou lien depuis le tableau de bord). Choisissez
le référentiel : le tableau liste chaque exigence, sa couverture, l'état de
socle et les mesures qui la traitent. Un encart donne le nombre d'exigences
applicables adressées.

### Annexe PDF

Le bouton **Télécharger l'annexe de conformité (PDF)** produit un document
reprenant le tableau, à joindre à un dossier d'homologation ou à un audit.

## Conseils

- Renseigner les codes de conformité **au fil de l'eau** en Atelier 5 évite un
  gros travail de rattachement a posteriori.
- « Non applicable » est une réponse légitime : justifiez-la dans l'état de
  socle ou la mesure.
- La conformité n'est pas l'objectif d'EBIOS RM (qui vise le risque) mais un
  **sous-produit utile** pour démontrer la couverture réglementaire.
