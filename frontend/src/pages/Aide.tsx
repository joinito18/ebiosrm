import { useParams, Link, useNavigate } from 'react-router-dom'
import PageHeader from '../components/shared/PageHeader'
import Markdown from '../components/shared/Markdown'
import { GUIDES } from '../guides'

export default function Aide() {
  var params = useParams()
  var navigate = useNavigate()
  var slug = params.slug || GUIDES[0].slug
  var guide = GUIDES.filter(function (g) { return g.slug === slug })[0] || GUIDES[0]

  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 lg:px-10 lg:py-14">
      <PageHeader
        eyebrow="Documentation"
        titre="Guides d'utilisation"
        description="Comment mener une analyse EBIOS Risk Manager avec l'outil, atelier par atelier."
      />

      <div className="grid gap-8 lg:grid-cols-[240px_1fr]">
        <nav className="lg:border-r lg:border-paper-line lg:pr-6">
          <ul className="space-y-0.5">
            {GUIDES.map(function (g) {
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
            {GUIDES.map(function (g) { return <option key={g.slug} value={g.slug}>{g.titre}</option> })}
          </select>
          <Markdown source={guide.contenu} />
        </article>
      </div>
    </div>
  )
}
