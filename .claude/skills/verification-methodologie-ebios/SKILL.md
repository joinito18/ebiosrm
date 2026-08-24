---
name: verification-methodologie-ebios
description: Verifie qu'un comportement du frontend/backend correspond reellement a la methode EBIOS RM officielle, en consultant les sources ANSSI dans Sources/. A utiliser avant de trancher un doute methodologique (ou est-ce qu'un champ/une action doit se trouver, quel terme officiel utiliser) -- ne jamais deviner ou supposer.
---

## Pourquoi ce skill existe

Plusieurs bugs reels de ce projet venaient d'une hypothese non verifiee sur la methode (ex: la creation des parties prenantes avait ete placee a l'Atelier 2 alors qu'elle appartient entierement a l'Atelier 3 -- confirme seulement apres avoir grep les PDF sources). Ne jamais trancher "de memoire" un point methodologique : verifier.

## Sources disponibles

Dossier `Sources/` a la racine du repo, PDF de formation officiels EBIOS Risk Manager (Jamal SAAD) :

- `5 PRESENTATION+EBIOS.pdf` -- vue d'ensemble de la methode
- `6 Atelier+1+partie+1.pdf`, `8 Atelier+1++partie+3.pdf` -- Atelier 1 (Cadrage)
- `9 ATELIER+2+Source+des+risques.pdf` -- Atelier 2 (Sources de risque)
- `10 ATELIER+3++partie+1.pdf`, `11 ATELIER+3+partie+2.pdf`, `12 ATELIER+3+partie+3.pdf` -- Atelier 3 (Scenarios strategiques)
- `13 ATELIER+4+Partie+1.pdf`, `14 ATELIER+4+Partie+2.pdf` -- Atelier 4 (Scenarios operationnels)
- `15 ATELIER+5+partie+1.pdf`, `16 ATELIER+5+partie+2.pdf` -- Atelier 5 (Traitement du risque)
- `EBIOS Risk Manager - Fiches (1).pdf` -- fiches methode complementaires (dont Fiche 9, traitement du risque)

## Methode de verification

1. Recherche texte rapide : `pdftotext "Sources/<fichier>.pdf" - | grep -i "<terme>"`. Si un terme n'apparait dans AUCUN des PDF d'un atelier donne, il n'appartient probablement pas a cet atelier (c'est exactement comme le doute sur "partie prenante" a l'Atelier 2 a ete tranche : zero occurrence).
2. Pour un diagramme ou une mise en page (ex. schema de cartographie, grille d'evaluation) que le texte seul n'explique pas assez : utiliser le Read tool directement sur le PDF avec le parametre `pages` pour voir le rendu visuel de la page concernee.
3. Toujours citer la source exacte (nom de fichier + ce qui a ete trouve/pas trouve) quand on rapporte la conclusion a l'utilisateur -- ne pas se contenter d'un "je pense que".

## Repere rapide : structure officielle des 5 ateliers

- **Atelier 1 (Cadrage)** : perimetre, valeurs metier, biens supports, evenements redoutes (+gravite), socle de securite.
- **Atelier 2 (Sources de risque)** : identification des sources de risque (SR) et objectifs vises (OV), couples SR/OV, evaluation de la pertinence. **Ne contient PAS les parties prenantes de l'ecosysteme.**
- **Atelier 3 (Scenarios strategiques)** : *tout* ce qui concerne l'ecosysteme -- identification des parties prenantes, cartographie de menace (dependance/penetration/maturite cyber/confiance -> dangerosite), scenarios strategiques, chemins d'attaque, mesures de securite sur l'ecosysteme.
- **Atelier 4 (Scenarios operationnels)** : modes operatoires techniques (Connaitre/Rentrer/Trouver/Exploiter), vraisemblance.
- **Atelier 5 (Traitement du risque)** : scenarios de risque (chemin + scenario operationnel), niveau initial derive, plan de traitement (mesures classees Gouvernance/Protection/Defense/Resilience), niveau residuel reevalue, acceptation formelle, cadre de suivi.

## Corrections terminologiques deja actees (ne pas les re-suspecter)

- **"PACS"** est un terme obsolete de versions anterieures d'EBIOS RM -- le terme actuel (1.5) est **"Plan de traitement du risque"**. Si "PACS" reapparait quelque part (code, doc, UI), c'est un reliquat a corriger, pas une variante valide.
- **"Responsable"** (execute une mesure de securite) et **"Proprietaire"** (possede un actif ou un risque, ex. dans le registre d'acceptation de l'Atelier 5) sont deux roles ISO/CEI 27005:2022 distincts qui coexistent legitimement -- ne pas les fusionner ni les confondre.
- La formule de dangerosite `(Dependance x Penetration) / (Maturite cyber x Confiance)` et le systeme "+/++/+++"  pour le cout/complexite du plan de traitement sont les seules formes trouvees dans les sources officielles -- il n'existe **aucune legende officielle** associant un mot a chaque niveau de cout/complexite (verifie par recherche exhaustive) ; le libelle entre parentheses ("Faible"/"Modere"/"Eleve") est une interpretation du projet, a signaler comme telle si le sujet revient.
