// Guides d'utilisation. Source unique : les fichiers .md de ce dossier
// (fr/ implicite = racine, en/ = traductions), aussi embarques cote backend
// (EbiosRM.Api.csproj) pour le manuel PDF.
import type { Langue } from '../lib/i18n'

import fr01 from './01-prise-en-main.md?raw'
import fr02 from './02-atelier-1-cadrage.md?raw'
import fr03 from './03-atelier-2-sources-risque.md?raw'
import fr04 from './04-atelier-3-scenarios-strategiques.md?raw'
import fr05 from './05-atelier-4-scenarios-operationnels.md?raw'
import fr06 from './06-atelier-5-traitement.md?raw'
import fr07 from './07-bibliotheque.md?raw'
import fr08 from './08-conformite.md?raw'
import fr09 from './09-suivi-portefeuille.md?raw'
import fr10 from './10-exports-partage.md?raw'

import en01 from './en/01-getting-started.md?raw'
import en02 from './en/02-workshop-1-scope.md?raw'
import en03 from './en/03-workshop-2-risk-origins.md?raw'
import en04 from './en/04-workshop-3-strategic-scenarios.md?raw'
import en05 from './en/05-workshop-4-operational-scenarios.md?raw'
import en06 from './en/06-workshop-5-treatment.md?raw'
import en07 from './en/07-library.md?raw'
import en08 from './en/08-compliance.md?raw'
import en09 from './en/09-tracking-portfolio.md?raw'
import en10 from './en/10-exports-sharing.md?raw'

export interface Guide {
  slug: string
  titre: string
  resume: string
  contenu: string
}

var META: { slug: string; fr: [string, string]; en: [string, string] }[] = [
  { slug: 'prise-en-main', fr: ['Prise en main', "Vue d'ensemble d'EBIOS RM et de l'outil, creation d'une etude, roles."], en: ['Getting started', "Overview of EBIOS RM and the tool, creating a study, roles."] },
  { slug: 'atelier-1', fr: ['Atelier 1 — Cadrage et socle', 'Valeurs metier, biens support, evenements redoutes, socle de securite.'], en: ['Workshop 1 — Scope and baseline', 'Business values, supporting assets, feared events, security baseline.'] },
  { slug: 'atelier-2', fr: ['Atelier 2 — Sources de risque', 'Couples source de risque / objectif vise, pertinence, couples retenus.'], en: ['Workshop 2 — Risk origins', 'Risk origin / target objective pairs, relevance, selected pairs.'] },
  { slug: 'atelier-3', fr: ['Atelier 3 — Scenarios strategiques', "Ecosysteme, dangerosite des parties prenantes, chemins d'attaque."], en: ['Workshop 3 — Strategic scenarios', 'Ecosystem, stakeholder threat level, attack paths.'] },
  { slug: 'atelier-4', fr: ['Atelier 4 — Scenarios operationnels', 'Modes operatoires, actions elementaires, MITRE, vraisemblance.'], en: ['Workshop 4 — Operational scenarios', 'Operating modes, elementary actions, MITRE, likelihood.'] },
  { slug: 'atelier-5', fr: ['Atelier 5 — Traitement du risque', 'Scenarios de risque, plan de traitement, risque residuel, acceptation.'], en: ['Workshop 5 — Risk treatment', 'Risk scenarios, treatment plan, residual risk, acceptance.'] },
  { slug: 'bibliotheque', fr: ['Bibliotheque', 'Catalogues systeme, bibliotheque personnelle, partage communautaire.'], en: ['Library', 'System catalogues, personal library, community sharing.'] },
  { slug: 'conformite', fr: ['Conformite', 'Tableau de couverture ISO 27001 / NIS2, annexe PDF.'], en: ['Compliance', 'ISO 27001 / NIS2 coverage table, PDF annex.'] },
  { slug: 'suivi', fr: ['Suivi et portefeuille', 'Vue portefeuille, evolution N/N-1, indicateurs de suivi (KRI).'], en: ['Tracking and portfolio', 'Portfolio view, N/N-1 evolution, tracking indicators (KRI).'] },
  { slug: 'exports', fr: ['Exports et partage', 'Rapports PDF, exports Word/Excel, import/export JSON, multi-langue.'], en: ['Exports and sharing', 'PDF reports, Word/Excel exports, JSON import/export, languages.'] },
]

var CONTENU_FR = [fr01, fr02, fr03, fr04, fr05, fr06, fr07, fr08, fr09, fr10]
var CONTENU_EN = [en01, en02, en03, en04, en05, en06, en07, en08, en09, en10]

export const GUIDES: Guide[] = META.map(function (m, i) {
  return { slug: m.slug, titre: m.fr[0], resume: m.fr[1], contenu: CONTENU_FR[i] }
})

var GUIDES_EN: Guide[] = META.map(function (m, i) {
  return { slug: m.slug, titre: m.en[0], resume: m.en[1], contenu: CONTENU_EN[i] }
})

export function guidesPour(langue: Langue): Guide[] {
  return langue === 'en' ? GUIDES_EN : GUIDES
}
