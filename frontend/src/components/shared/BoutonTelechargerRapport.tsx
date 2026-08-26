import { telechargerRapport, ApiError } from '../../lib/api'

export default function BoutonTelechargerRapport(props: { path: string; nomFichier: string; children: React.ReactNode; className?: string }) {
  function handleClick() {
    telechargerRapport(props.path, props.nomFichier)
      .catch(function (err) { window.alert(err instanceof ApiError ? err.message : 'Erreur lors du telechargement.') })
  }

  return (
    <button onClick={handleClick} className={props.className}>
      {props.children}
    </button>
  )
}
