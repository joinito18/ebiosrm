import { Download } from 'lucide-react'
import PageHeader from '../components/shared/PageHeader'

var RAPPORTS = [
  { titre: 'Atelier 1 - Cadrage', etude: 'Societe de biotechnologie', version: 1, date: '10/08/2026' },
]

export default function Rapports() {
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="LIVRABLES"
        titre="Rapports"
        description="Documents generes a partir des snapshots figes de chaque atelier valide."
      />

      <div className="divide-y divide-paper-line border-y border-paper-line">
        {RAPPORTS.map(function (r) {
          return (
            <div key={r.titre + r.version} className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="text-sm font-medium text-ink">{r.titre}</div>
                <div className="mt-0.5 text-xs text-steel">{r.etude}</div>
              </div>
              <div className="flex flex-wrap items-center gap-4 sm:gap-6">
                <span className="font-mono text-[11px] text-steel-light">VERSION {r.version}</span>
                <span className="font-mono text-[11px] text-steel-light">{r.date}</span>
                <button className="flex items-center gap-1.5 rounded-sm border border-paper-line px-3 py-1.5 text-[11px] font-medium text-ink transition hover:border-signature hover:text-signature">
                  <Download size={13} />
                  PDF
                </button>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
