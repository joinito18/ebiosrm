interface PageHeaderProps {
  eyebrow: string
  titre: string
  description?: string
}

export default function PageHeader(props: PageHeaderProps) {
  return (
    <div className="mb-10 border-b border-paper-line pb-8">
      <div className="mb-3 font-mono text-[11px] tracking-wide text-steel">
        {props.eyebrow}
      </div>
      <h1 className="font-display text-[32px] leading-tight text-ink">
        {props.titre}
      </h1>
      {props.description && (
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-steel">
          {props.description}
        </p>
      )}
    </div>
  )
}
