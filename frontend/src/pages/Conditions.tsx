import { Link } from 'react-router-dom'
import Markdown from '../components/shared/Markdown'
import { useLangue } from '../lib/i18n'
import conditionsFr from '../legal/conditions-fr.md?raw'
import conditionsEn from '../legal/conditions-en.md?raw'

export default function Conditions() {
  var langue = useLangue().langue
  var source = langue === 'en' ? conditionsEn : conditionsFr

  return (
    <div className="mx-auto max-w-[760px] px-6 py-10 lg:px-10 lg:py-14">
      <Link to="/etudes" className="mb-6 inline-block font-mono text-[11px] tracking-wide text-steel hover:text-signature">
        &larr; EBIOS Risk Manager
      </Link>
      <Markdown source={source} />
    </div>
  )
}
