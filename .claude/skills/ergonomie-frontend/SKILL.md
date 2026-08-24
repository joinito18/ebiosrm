---
name: ergonomie-frontend
description: Checklist a appliquer pour une passe d'ergonomie/lisibilite sur le frontend EBIOS RM (AtelierPage.tsx et le reste de frontend/src/pages). A utiliser quand on nettoie une page, ajoute une section, ou qu'on soupconne un affichage brut/redondant.
---

Checklist batie sur les defauts reellement trouves dans ce projet (pas generique). Parcourir dans l'ordre.

## 1. Enums bruts affiches sans libelle

Le piege le plus frequent et le plus discret : un enum C#/TS affiche via `.ToString()` ou `{valeur}` direct produit du PascalCase colle, illisible (`EspionnageEtatiqueOuIndustriel`, `NonConforme`, `TresPertinent`).

- Cote frontend : chercher les `.map(function (x) { return <option key={x} value={x}>{x}</option> })` -- si le label affiche est la variable brute et pas un lookup dans une map `LIBELLE_*`, c'est suspect.
- Cote backend : chercher `.ToString()` sur un enum, ou une comparaison `== "Autre"` qui laisse passer la valeur brute sinon (`RapportAtelier*Service.cs`, `ServiceAssemblageScenariosDeRisque.cs` avaient ce defaut).
- **Le meme bug existe presque toujours a deux endroits** : le frontend interactif ET le service C# qui alimente les rapports PDF. Corriger un cote sans verifier l'autre laisse les PDF casses (deja arrive : `ServiceAssemblageScenariosDeRisque.cs` + 3 `Rapport*Service.cs`/`Rapport*Data.cs`).
- Maps deja existantes dans `AtelierPage.tsx` a reutiliser/etendre plutot que dupliquer : `LIBELLE_CATEGORIE_SR`, `LIBELLE_CATEGORIE_OV`, `LIBELLE_TYPE_BIEN_SUPPORT`, `LIBELLE_ETAT_CONFORMITE`, `LIBELLE_PERTINENCE`, `LIBELLE_STATUT_ATELIER` (extrait dans `components/shared/BadgeStatutAtelier.tsx`), `LIBELLE_COUT_COMPLEXITE`, `LIBELLE_STATUT_MESURE`, `LIBELLE_CLASSE_ACCEPTATION`, `LIBELLE_PHASE`.
- Cote backend, la contrepartie C# vit dans `LibellesSourceRisqueObjectifVise` (`Modules/CoreEngine/Domain/SourcesRisque/CoupleSourceRisqueObjectifVise.cs`).
- Convention du projet : pas d'accents dans les libelles ("securite", "etatique"), meme si grammaticalement incorrect -- coherence avec le reste du code source.

## 2. Doublons d'information sur une meme page

Symptome observe : une meme donnee (categorie+representant d'une partie prenante) affichee 3 fois de suite dans des sections consecutives (cartographie, liste d'evaluation, encart mesures) avec un formatage legerement different a chaque fois. Reflexe : quand une section reaffiche une info deja visible juste au-dessus sur la meme page, la retirer et ne garder que le champ vraiment utile a cet endroit (nom + l'action en cours, pas toute l'identite).

## 3. Statuts en texte brut plutot qu'un badge

Chercher les `{etude.statut}` / `{etude.statutAtelierX}` affiches nus -- utiliser `<BadgeStatutAtelier statut={...} />` (`components/shared/BadgeStatutAtelier.tsx`), qui donne un badge colore (vert = Validee, bleu = EnCours, gris = Brouillon) au lieu d'un enum brut.

## 4. Paragraphes explicatifs trop longs

Chaque section a une phrase d'intro pedagogique. Elle doit tenir en **une phrase courte** rappelant juste "quoi faire ici", pas reexpliquer toute la methode EBIOS RM (ca, c'est le role de la notice technique dans `docs/architecture/`, pas de l'UI).

## 5. Etat fige / code en dur qui ne suit plus les donnees reelles

Deja trouve deux fois : `Dashboard.tsx` et `Sidebar.tsx` codaient en dur le statut de l'Atelier 5 a `'todo'`, reliquat du moment ou seul l'Atelier 1 existait. Reflexe : quand une page affiche un etat derive d'une entite (`etude.statutAtelierX`), verifier qu'elle lit vraiment le champ et ne code pas une valeur par defaut oubliee. Chercher aussi les messages de type "cette fonctionnalite n'est pas encore implementee" -- verifier qu'ils sont encore vrais.

## 6. Verification obligatoire : Playwright, pas juste la lecture du code

Une correction visuelle n'est jamais consideree faite sans capture d'ecran reelle (voir le skill `test-etude-complete-playwright`). Le rendu Tailwind/flex peut differer de ce que le JSX laisse penser (ex. deux boutons adjacents sans `gap` qui se retrouvent colles a l'ecran alors que le JSX semblait correct).
