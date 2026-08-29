import { useParams, Link, useNavigate } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import Markdown from '../components/shared/Markdown'
import BoutonTelechargerRapport from '../components/shared/BoutonTelechargerRapport'
import { useLangue } from '../lib/i18n'
import { guidesPour } from '../guides'

var T = {
  fr: {
    eyebrow: 'Documentation',
    titre: "Guides d'utilisation",
    desc: "Comment mener une analyse EBIOS Risk Manager avec l'outil, atelier par atelier.",
    pdf: 'Telecharger le manuel complet (PDF)',
  },
  en: {
    eyebrow: 'Documentation',
    titre: 'User guides',
    desc: 'How to run an EBIOS Risk Manager assessment with the tool, workshop by workshop.',
    pdf: 'Download the full manual (PDF)',
  },
}

export default function Aide() {
  var params = useParams()
  var navigate = useNavigate()
  var langue = useLangue().langue
  var guides = guidesPour(langue)
  var t = T[langue] || T.fr
  var slug = params.slug || guides[0].slug
  var guide = guides.filter(function (g) { return g.slug === slug })[0] || guides[0]

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader eyebrow={t.eyebrow} titre={t.titre} description={t.desc} />

      <div className="mb-6">
        <BoutonTelechargerRapport
          path={'/aide/manuel.pdf' + (langue === 'en' ? '?langue=en' : '')}
          nomFichier={langue === 'en' ? 'ebiosrm-manual.pdf' : 'manuel-ebiosrm.pdf'}
          className="inline-flex items-center gap-1.5 rounded-sm border border-paper-line px-3 py-1.5 text-xs font-medium text-ink transition hover:border-signature hover:text-signature"
        >
          {t.pdf}
        </BoutonTelechargerRapport>
      </div>

      <div className="grid gap-8 lg:grid-cols-[240px_1fr]">
        <nav className="lg:border-r lg:border-paper-line lg:pr-6">
          <ul className="space-y-0.5">
            {guides.map(function (g) {
              var actif = g.slug === guide.slug
              return (
                <li key={g.slug}>
                  <Link
                    to={'/aide/' + g.slug}
                    className={'block rounded-sm px-2 py-1.5 text-sm transition ' + (actif ? 'bg-signature/10 font-medium text-signature' : 'text-steel hover:bg-paper-dim hover:text-ink')}
                  >
                    {g.titre}
                  </Link>
                </li>
              )
            })}
          </ul>
        </nav>

        <article className="min-w-0">
          <select
            value={guide.slug}
            onChange={function (e) { navigate('/aide/' + e.target.value) }}
            className="mb-4 w-full border-b border-paper-line bg-transparent py-2 text-sm text-ink focus:border-signature focus:outline-none lg:hidden"
          >
            {guides.map(function (g) { return <option key={g.slug} value={g.slug}>{g.titre}</option> })}
          </select>
          <Markdown source={guide.contenu} />
        </article>
      </div>
    </div>
  )
}
