import { createContext, useContext } from 'react'

/**
 * Vrai quand l'utilisateur consulte l'etude courante en Lecteur : les
 * composants d'action partages (InlineForm, RowActions) se masquent, les
 * boutons de workflow d'atelier ne sont pas rendus. Le backend refuse de
 * toute facon toute ecriture (403), ceci evite juste des boutons trompeurs.
 */
const LectureSeuleContext = createContext(false)

export function LectureSeuleProvider(props: { valeur: boolean; children: React.ReactNode }) {
  return <LectureSeuleContext.Provider value={props.valeur}>{props.children}</LectureSeuleContext.Provider>
}

export function useLectureSeule(): boolean {
  return useContext(LectureSeuleContext)
}
