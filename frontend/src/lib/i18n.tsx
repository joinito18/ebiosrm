import { createContext, useContext, useEffect, useState } from 'react'

export type Langue = 'fr' | 'en'

/**
 * i18n minimaliste, sans dépendance. La coquille de l'application (navigation,
 * en-têtes de page, paramètres, vues transverses) est traduite ; le contenu
 * méthodologique détaillé des ateliers reste en français pour l'instant et
 * sera traduit progressivement.
 */
var DICTIONNAIRE: { [key in Langue]: { [cle: string]: string } } = {
  fr: {
    'nav.tableau': 'Tableau de bord',
    'nav.etudes': 'Etudes',
    'nav.portefeuille': 'Portefeuille',
    'nav.bibliotheque': 'Bibliotheque',
    'nav.rapports': 'Rapports',
    'nav.parametres': 'Parametres',
    'nav.etudeEnCours': 'ETUDE EN COURS',
    'nav.deconnexion': 'Deconnexion',

    'dash.membres': 'MEMBRES',
    'dash.journal': 'JOURNAL',
    'dash.conformite': 'CONFORMITE',
    'dash.suivi': 'SUIVI',

    'params.langue': 'Langue de l’interface',
    'params.langue.aide': 'Le contenu détaillé des ateliers reste en français pour l’instant.',

    'portefeuille.eyebrow': 'PILOTAGE MULTI-ETUDES',
    'portefeuille.titre': 'Portefeuille',
    'portefeuille.desc': 'Vue consolidée de toutes les études : exposition résiduelle, avancement du traitement, conformité NIS2.',
    'portefeuille.export': 'Exporter en Excel',

    'conformite.eyebrow': 'MAPPING DE CONFORMITE',
    'conformite.titre': 'Conformité',
    'conformite.pdf': 'Telecharger l’annexe de conformite (PDF)',

    'suivi.eyebrow': 'CADRE DE SUIVI VIVANT',
    'suivi.titre': 'Suivi',

    'biblio.eyebrow': 'ELEMENTS REUTILISABLES',
    'biblio.titre': 'Bibliotheque',

    'etudes.export.excel': 'Exporter le registre (Excel)',
    'etudes.export.word': 'Exporter la synthese (Word)',
    'commun.retourEtude': 'retour à l’étude',
    'commun.chargement': 'Chargement...',
  },
  en: {
    'nav.tableau': 'Dashboard',
    'nav.etudes': 'Studies',
    'nav.portefeuille': 'Portfolio',
    'nav.bibliotheque': 'Library',
    'nav.rapports': 'Reports',
    'nav.parametres': 'Settings',
    'nav.etudeEnCours': 'CURRENT STUDY',
    'nav.deconnexion': 'Sign out',

    'dash.membres': 'MEMBERS',
    'dash.journal': 'AUDIT LOG',
    'dash.conformite': 'COMPLIANCE',
    'dash.suivi': 'MONITORING',

    'params.langue': 'Interface language',
    'params.langue.aide': 'Detailed workshop content remains in French for now.',

    'portefeuille.eyebrow': 'MULTI-STUDY OVERSIGHT',
    'portefeuille.titre': 'Portfolio',
    'portefeuille.desc': 'Consolidated view of all studies: residual exposure, treatment progress, NIS2 coverage.',
    'portefeuille.export': 'Export to Excel',

    'conformite.eyebrow': 'COMPLIANCE MAPPING',
    'conformite.titre': 'Compliance',
    'conformite.pdf': 'Download compliance annex (PDF)',

    'suivi.eyebrow': 'LIVING MONITORING FRAMEWORK',
    'suivi.titre': 'Monitoring',

    'biblio.eyebrow': 'REUSABLE ITEMS',
    'biblio.titre': 'Library',

    'etudes.export.excel': 'Export risk register (Excel)',
    'etudes.export.word': 'Export summary (Word)',
    'commun.retourEtude': 'back to study',
    'commun.chargement': 'Loading...',
  },
}

var CtxLangue = createContext<{ langue: Langue; changer: (l: Langue) => void }>({ langue: 'fr', changer: function () {} })

export function ProviderLangue(props: { children: React.ReactNode }) {
  var [langue, setLangue] = useState<Langue>(function () {
    try { return (localStorage.getItem('ebiosrm_langue') as Langue) || 'fr' } catch (e) { return 'fr' }
  })

  function changer(l: Langue) {
    try { localStorage.setItem('ebiosrm_langue', l) } catch (e) { /* prive */ }
    setLangue(l)
  }

  useEffect(function () { document.documentElement.lang = langue }, [langue])

  return <CtxLangue.Provider value={{ langue: langue, changer: changer }}>{props.children}</CtxLangue.Provider>
}

export function useLangue() {
  return useContext(CtxLangue)
}

export function useT(): (cle: string) => string {
  var langue = useContext(CtxLangue).langue
  return function (cle: string) {
    return DICTIONNAIRE[langue][cle] || DICTIONNAIRE.fr[cle] || cle
  }
}
