import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import Card from '../components/shared/Card'
import Button from '../components/shared/Button'
import { obtenirUtilisateurCourant, effacerToken } from '../lib/api'
import type { Utilisateur } from '../lib/api'

export default function Parametres() {
  var navigate = useNavigate()
  var [utilisateur, setUtilisateur] = useState<Utilisateur | null>(null)

  useEffect(function () {
    obtenirUtilisateurCourant().then(setUtilisateur).catch(function () { setUtilisateur(null) })
  }, [])

  function seDeconnecter() {
    effacerToken()
    navigate('/connexion')
  }

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="CONFIGURATION"
        titre="Parametres"
        description="Preferences du compte et de la plateforme."
      />

      <div className="max-w-md space-y-4">
        <Card variant="elevated" className="p-5">
          <div className="mb-1 font-mono text-[10px] tracking-wide text-steel-light">COMPTE</div>
          <div className="text-sm font-medium text-ink">{utilisateur ? utilisateur.nomAffiche : 'Chargement...'}</div>
          <div className="text-xs text-steel">{utilisateur ? utilisateur.email : ''}</div>
          <div className="mt-4 border-t border-paper-line pt-4">
            <Button variante="danger" onClick={seDeconnecter}>
              <LogOut size={13} strokeWidth={1.75} />
              Se deconnecter
            </Button>
          </div>
        </Card>

        <Card variant="elevated" className="p-5">
          <div className="mb-1 font-mono text-[10px] tracking-wide text-steel-light">REFERENTIEL</div>
          <div className="text-sm font-medium text-ink">Referentiel EBIOS</div>
          <div className="text-xs text-steel">EBIOS_RM_V1</div>
        </Card>
      </div>
    </div>
  )
}
