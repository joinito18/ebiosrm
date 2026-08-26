export default function LayoutAuth(props: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <div className="flex items-center justify-between bg-ink px-6 py-6 text-white md:w-2/5 md:flex-col md:items-start md:justify-between md:px-12 md:py-14">
        <div className="font-display text-xl md:text-2xl">
          EBIOS<span className="text-signature">&middot;</span>RM
        </div>

        <div className="hidden md:block">
          <p className="max-w-xs font-display text-[28px] leading-snug text-white text-balance">
            Pilotez vos etudes de risque EBIOS RM de bout en bout.
          </p>
          <p className="mt-4 max-w-xs text-sm leading-relaxed text-steel-light">
            Cadrage, sources de risque, scenarios strategiques et operationnels, traitement du risque -- les 5 ateliers de la methode ANSSI, dans un seul outil.
          </p>
        </div>

        <div className="font-mono text-[10px] tracking-wide text-steel-light">
          METHODE ANSSI -- EBIOS RISK MANAGER
        </div>
      </div>

      <div className="flex flex-1 items-center justify-center bg-paper px-6 py-10 md:py-12">
        <div className="w-full max-w-sm">{props.children}</div>
      </div>
    </div>
  )
}
