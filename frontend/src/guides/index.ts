// Guides d'utilisation. Source unique : les fichiers .md de ce dossier, aussi
// embarques cote backend (EbiosRM.Api.csproj) pour le manuel PDF.
import priseEnMain from './01-prise-en-main.md?raw'
import atelier1 from './02-atelier-1-cadrage.md?raw'
import atelier2 from './03-atelier-2-sources-risque.md?raw'
import atelier3 from './04-atelier-3-scenarios-strategiques.md?raw'
import atelier4 from './05-atelier-4-scenarios-operationnels.md?raw'
import atelier5 from './06-atelier-5-traitement.md?raw'
import bibliotheque from './07-bibliotheque.md?raw'
import conformite from './08-conformite.md?raw'
import suivi from './09-suivi-portefeuille.md?raw'
import exports from './10-exports-partage.md?raw'

export interface Guide {
  slug: string
  titre: string
  resume: string
  contenu: string
}

export const GUIDES: Guide[] = [
  { slug: 'prise-en-main', titre: 'Prise en main', resume: "Vue d'ensemble d'EBIOS RM et de l'outil, creation d'une etude, roles.", contenu: priseEnMain },
  { slug: 'atelier-1', titre: 'Atelier 1 — Cadrage et socle', resume: 'Valeurs metier, biens support, evenements redoutes, socle de securite.', contenu: atelier1 },
  { slug: 'atelier-2', titre: 'Atelier 2 — Sources de risque', resume: 'Couples source de risque / objectif vise, pertinence, couples retenus.', contenu: atelier2 },
  { slug: 'atelier-3', titre: 'Atelier 3 — Scenarios strategiques', resume: 'Ecosysteme, dangerosite des parties prenantes, chemins d\'attaque.', contenu: atelier3 },
  { slug: 'atelier-4', titre: 'Atelier 4 — Scenarios operationnels', resume: 'Modes operatoires, actions elementaires, MITRE, vraisemblance.', contenu: atelier4 },
  { slug: 'atelier-5', titre: 'Atelier 5 — Traitement du risque', resume: 'Scenarios de risque, plan de traitement, risque residuel, acceptation.', contenu: atelier5 },
  { slug: 'bibliotheque', titre: 'Bibliotheque', resume: 'Catalogues systeme, bibliotheque personnelle, partage communautaire.', contenu: bibliotheque },
  { slug: 'conformite', titre: 'Conformite', resume: 'Tableau de couverture ISO 27001 / NIS2, annexe PDF.', contenu: conformite },
  { slug: 'suivi', titre: 'Suivi et portefeuille', resume: 'Vue portefeuille, evolution N/N-1, indicateurs de suivi (KRI).', contenu: suivi },
  { slug: 'exports', titre: 'Exports et partage', resume: 'Rapports PDF, exports Word/Excel, import/export JSON, multi-langue.', contenu: exports },
]
