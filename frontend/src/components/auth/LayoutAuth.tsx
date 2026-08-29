import { Link } from 'react-router-dom'
import { useT } from '../../lib/i18n'

export default function LayoutAuth(props: { children: React.ReactNode }) {
  var _t = useT()
  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <div className="flex items-center justify-between bg-ink px-6 py-6 text-white md:w-2/5 md:flex-col md:items-start md:justify-between md:px-12 md:py-14">
        <div className="font-display text-xl md:text-2xl">
          EBIOS<span className="text-signature">&middot;</span>RM
        </div>

        <div className="hidden md:block">
          <p className="max-w-xs font-display text-[28px] leading-snug text-white text-balance">
            {_t('auth.baseline')}
          </p>
          <p className="mt-4 max-w-xs text-sm leading-relaxed text-steel-light">
            {_t('auth.baselineSub')}
          </p>
        </div>

        <div className="font-mono text-[10px] tracking-wide text-steel-light">
          EBIOS RISK MANAGER
        </div>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center bg-paper px-6 py-10 md:py-12">
        <div className="w-full max-w-sm">{props.children}</div>
        <Link to="/conditions" className="mt-8 font-mono text-[10px] text-steel-light hover:text-signature">
          {_t('legal.conditions')}
        </Link>
      </div>
    </div>
  )
}
