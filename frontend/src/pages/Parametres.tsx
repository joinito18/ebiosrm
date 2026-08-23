import PageHeader from '../components/shared/PageHeader'

export default function Parametres() {
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="CONFIGURATION"
        titre="Parametres"
        description="Preferences du compte et de la plateforme."
      />

      <div className="max-w-md space-y-6">
        <div className="flex items-center justify-between border-b border-paper-line pb-4">
          <div>
            <div className="text-sm font-medium text-ink">Nom</div>
            <div className="text-xs text-steel">Analyste de risques</div>
          </div>
        </div>
        <div className="flex items-center justify-between border-b border-paper-line pb-4">
          <div>
            <div className="text-sm font-medium text-ink">Organisation</div>
            <div className="text-xs text-steel">CENADI</div>
          </div>
        </div>
        <div className="flex items-center justify-between border-b border-paper-line pb-4">
          <div>
            <div className="text-sm font-medium text-ink">Referentiel EBIOS</div>
            <div className="text-xs text-steel">EBIOS_RM_V1</div>
          </div>
        </div>
      </div>
    </div>
  )
}
