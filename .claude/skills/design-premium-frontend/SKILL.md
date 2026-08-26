---
name: design-premium-frontend
description: Systeme de composants et regles visuelles issues de la refonte "premium" du frontend EBIOS RM (fini de rendre generique, plus institutionnel-editorial). A consulter avant d'ajouter une nouvelle section/page, pour ne pas reintroduire la duplication de className corrigee par cette refonte.
---

## Pourquoi ce skill existe

Un audit complet du frontend (avant la refonte) a trouve : le meme bouton retape a la main 9 fois, la paire d'actions Modifier/Suppr. dupliquee 26 fois, le style de champ texte duplique 59 fois, 6+ fonctions `couleurX` differentes rendant juste du texte colore brut au lieu d'un vrai badge, et zero systeme d'elevation (`grep shadow` sur tout `src` : 2 resultats). La palette de base (bleu Marianne officiel `#000091`, serif editorial Fraunces, IBM Plex Sans/Mono) n'etait pas le probleme -- l'execution recopiee-collee l'etait. Ce skill fixe les composants qui remplacent cette duplication, pour que le prochain ajout les reutilise au lieu de retaper une chaine `className`.

## Direction visuelle : "institutionnel-editorial"

Assumer et intensifier l'identite deja la, pas la remplacer. Explicitement exclu : degrades, glassmorphism, `rounded-full` generalise, animations voyantes -- c'est un outil institutionnel serieux, pas une appli grand public. Elevation subtile et chaleureuse (ombres teintees encre, pas gris neutre generique) : tokens `shadow-card`/`shadow-card-hover` dans `frontend/src/index.css`, utilises **uniquement** par `Card variant="elevated"` -- ne pas ajouter d'ombre a la main ailleurs.

## Composants (`frontend/src/components/shared/`)

- **`Button.tsx`** : variantes `primary` (action principale, fond signature) / `secondary` (bordure, action secondaire -- defaut) / `ghost` (texte seul, pour un "+ Ajouter" imbrique dans une sous-section) / `danger` (bordure, survol rouge). Tailles `sm`/`md`. Ne jamais retaper une chaine de bouton a la main -- si aucune variante ne convient, etendre `Button.tsx`, pas contourner.
- **`Badge.tsx`** : tout ce qui encode une severite/zone/classe (gravite, vraisemblance, niveau de risque, zone de dangerosite, classe d'acceptation, pertinence, statut) passe par `<Badge couleur="...">`, jamais par du texte colore brut (`<span className={'text-' + couleur}>`). Couleurs disponibles : `signature`/`risk-critical`/`risk-high`/`risk-moderate`/`risk-low`/`steel` -- **chaines completes en dur dans `STYLES`, pas de concatenation `'border-' + couleur + '/30'`**. Piege reel rencontre en construisant ce composant : Tailwind ne genere une classe que s'il la trouve telle quelle, en clair, dans le code source -- une classe assemblee par concatenation a l'execution n'existe pour Tailwind nulle part au moment du scan, donc aucun style n'est genere (echec silencieux, pas d'erreur). Si une nouvelle couleur de badge est necessaire, ajouter une entree complete a `STYLES`, ne jamais interpoler.
- **`Card.tsx`** : `variant="flat"` (defaut, bordure simple) pour le contenu de flux normal de page. `variant="elevated"` (ombre + `rounded-md`) reserve aux elements qui doivent vraiment se detacher (carte de scenario de risque, mesure d'ecosysteme, panneau de creation, carte de rapport). Ne pas mettre `elevated` partout -- sinon l'elevation ne signifie plus rien.
- **`EmptyState.tsx`** : `<EmptyState message="..."/>` remplace tout `<p className="text-xs text-steel">Aucun ... renseigne.</p>`. Icone par defaut fournie (lucide `Inbox`), overridable.
- **`RowActions.tsx`** : la paire Modifier/Suppr. (`onModifier` optionnel si la ligne n'est pas modifiable, `onSupprimer` obligatoire).

## Hierarchie typographique

- Titre de section de niveau atelier (VALEURS METIER, BIENS SUPPORT...) : `font-mono text-[11px] tracking-wide text-steel-light` -- deliberement un look "etiquette de metadonnee", ne pas changer.
- Titre de sous-section a l'interieur d'une carte imbriquee (ex. dans une carte de scenario de risque) : `font-display`, pour se distinguer visuellement du niveau au-dessus. Si un titre de sous-section utilise encore le style mono identique au niveau parent, c'est un oubli de cette regle.

## Mode edition : une seule convention

Accent `border-l-2 border-signature` sur le conteneur qui bascule en edition. Ne pas reintroduire les deux autres conventions trouvees pendant l'audit (bordure d'input qui vire simplement a `border-signature` sans accent structurel ; autres traitements ad hoc au cas par cas).

## Verification

Une modification visuelle sur `AtelierPage.tsx` ou une page utilisant ces composants n'est jamais consideree terminee sans (1) `npm run build` (2) capture Playwright reelle (voir skill `test-etude-complete-playwright`) -- le rendu Tailwind peut differer du JSX, et une classe Badge mal orthographiee echoue silencieusement (cf. piege ci-dessus) sans jamais lever d'erreur TypeScript ou de build.
