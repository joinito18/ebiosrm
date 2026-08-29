import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'
import Card from '../components/shared/Card'
import Button from '../components/shared/Button'
import { obtenirUtilisateurCourant, effacerToken } from '../lib/api'
import type { Utilisateur } from '../lib/api'
import { useLangue, useT } from '../lib/i18n'
import type { Langue } from '../lib/i18n'

export default function Parametres() {
  var navigate = useNavigate()
  var [utilisateur, setUtilisateur] = useState<Utilisateur | null>(null)
  var { langue, changer } = useLangue()
  var t = useT()

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
        eyebrow={t('params.eyebrow')}
        titre={t('params.titre')}
        description={t('params.desc')}
      />

      <div className="max-w-md space-y-4">
        <Card variant="elevated" className="p-5">
          <div className="mb-1 font-mono text-[10px] tracking-wide text-steel-light">{t('params.compte').toUpperCase()}</div>
          <div className="text-sm font-medium text-ink">{utilisateur ? utilisateur.nomAffiche : t('commun.chargement')}</div>
          <div className="text-xs text-steel">{utilisateur ? utilisateur.email : ''}</div>
          <div className="mt-4 border-t border-paper-line pt-4">
            <Button variante="danger" onClick={seDeconnecter}>
              <LogOut size={13} strokeWidth={1.75} />
              {t('params.deconnexion')}
            </Button>
          </div>
        </Card>

        <Card variant="elevated" className="p-5">
          <div className="mb-2 font-mono text-[10px] tracking-wide text-steel-light">{t('params.langue').toUpperCase()}</div>
          <div className="flex gap-2">
            {(['fr', 'en'] as Langue[]).map(function (l) {
              var actif = langue === l
              return (
                <button
                  key={l}
                  onClick={function () { changer(l) }}
                  className={'border px-3 py-1.5 text-xs font-medium transition ' + (actif ? 'border-signature bg-signature text-white' : 'border-paper-line text-steel hover:border-signature')}
                >
                  {l === 'fr' ? 'Français' : 'English'}
                </button>
              )
            })}
          </div>
          <div className="mt-2 text-[11px] text-steel-light">{t('params.langue.aide')}</div>
        </Card>

        <Card variant="elevated" className="p-5">
          <div className="mb-1 font-mono text-[10px] tracking-wide text-steel-light">{t('params.referentiel').toUpperCase()}</div>
          <div className="text-sm font-medium text-ink">EBIOS Risk Manager</div>
          <div className="text-xs text-steel">EBIOS_RM_V1</div>
        </Card>
      </div>
    </div>
  )
}
