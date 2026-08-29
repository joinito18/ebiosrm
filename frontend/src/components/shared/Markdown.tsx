/**
 * Rendu Markdown minimaliste, sans dependance (comme le reste du projet).
 * Couvre ce qui est utilise dans les guides de docs/guides : titres (# a ####),
 * paragraphes, listes a puces et numerotees (un niveau + sous-niveau par
 * indentation de 2 espaces), gras, code inline, liens, blocs de code, citations,
 * regles horizontales et tableaux pipe.
 */
import React from 'react'

function inline(texte: string, cle: string): React.ReactNode {
  // Decoupe sur **gras**, `code` et [lien](url) en gardant les separateurs.
  var motif = /(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g
  var morceaux = texte.split(motif)
  return morceaux.map(function (m, i) {
    var k = cle + '-' + i
    if (/^\*\*[^*]+\*\*$/.test(m)) return <strong key={k}>{m.slice(2, -2)}</strong>
    if (/^`[^`]+`$/.test(m)) return <code key={k} className="rounded bg-paper-dim px-1 py-0.5 font-mono text-[0.85em] text-signature">{m.slice(1, -1)}</code>
    var lien = m.match(/^\[([^\]]+)\]\(([^)]+)\)$/)
    if (lien) {
      var externe = /^https?:/.test(lien[2])
      return <a key={k} href={lien[2]} target={externe ? '_blank' : undefined} rel={externe ? 'noreferrer' : undefined} className="text-signature underline hover:no-underline">{lien[1]}</a>
    }
    return <React.Fragment key={k}>{m}</React.Fragment>
  })
}

export default function Markdown(props: { source: string }) {
  var lignes = props.source.replace(/\r\n/g, '\n').split('\n')
  var blocs: React.ReactNode[] = []
  var i = 0
  var para: string[] = []
  var puces: { indent: number; texte: string; ordonnee: boolean }[] = []

  function viderPara() {
    if (para.length === 0) return
    blocs.push(<p key={'p' + blocs.length} className="my-3 leading-relaxed text-ink">{inline(para.join(' '), 'p' + blocs.length)}</p>)
    para = []
  }

  function viderPuces() {
    if (puces.length === 0) return
    var cle = 'l' + blocs.length
    var base = puces[0].indent
    var elements = puces.map(function (p, idx) {
      var sousNiveau = p.indent > base
      return (
        <li key={cle + '-' + idx} className={'my-1 ' + (sousNiveau ? 'ml-5 list-[circle]' : '')}>
          {inline(p.texte, cle + '-' + idx)}
        </li>
      )
    })
    var Balise = puces[0].ordonnee ? 'ol' : 'ul'
    blocs.push(React.createElement(Balise, { key: cle, className: (puces[0].ordonnee ? 'list-decimal' : 'list-disc') + ' my-3 space-y-0.5 pl-6 text-ink' }, elements))
    puces = []
  }

  while (i < lignes.length) {
    var ligne = lignes[i]

    // Bloc de code ```
    if (/^```/.test(ligne.trim())) {
      viderPara(); viderPuces()
      var code: string[] = []
      i++
      while (i < lignes.length && !/^```/.test(lignes[i].trim())) { code.push(lignes[i]); i++ }
      i++
      blocs.push(<pre key={'c' + blocs.length} className="my-3 overflow-x-auto rounded border border-paper-line bg-paper-dim p-3 font-mono text-xs text-ink">{code.join('\n')}</pre>)
      continue
    }

    // Tableau pipe : ligne d'en-tete + separateur | --- |
    if (/^\|.*\|$/.test(ligne.trim()) && i + 1 < lignes.length && /^\|[\s:|-]+\|$/.test(lignes[i + 1].trim())) {
      viderPara(); viderPuces()
      var cellules = function (l: string) { return l.trim().replace(/^\||\|$/g, '').split('|').map(function (c) { return c.trim() }) }
      var entetes = cellules(ligne)
      i += 2
      var corps: string[][] = []
      while (i < lignes.length && /^\|.*\|$/.test(lignes[i].trim())) { corps.push(cellules(lignes[i])); i++ }
      blocs.push(
        <div key={'t' + blocs.length} className="my-3 overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead><tr className="border-b border-paper-line text-left">{entetes.map(function (h, hi) { return <th key={hi} className="py-1.5 pr-4 font-mono text-[10px] tracking-wide text-steel-light">{inline(h, 't' + hi)}</th> })}</tr></thead>
            <tbody>{corps.map(function (r, ri) { return <tr key={ri} className="border-b border-paper-line align-top">{r.map(function (c, ci) { return <td key={ci} className="py-1.5 pr-4 text-ink">{inline(c, 't' + ri + '-' + ci)}</td> })}</tr> })}</tbody>
          </table>
        </div>
      )
      continue
    }

    // Titres
    var titre = ligne.match(/^(#{1,4})\s+(.*)$/)
    if (titre) {
      viderPara(); viderPuces()
      var niveau = titre[1].length
      var classes = ['mt-8 mb-3 font-display text-2xl text-ink', 'mt-7 mb-2 font-display text-xl text-ink', 'mt-6 mb-2 font-mono text-xs tracking-wide text-steel-light uppercase', 'mt-4 mb-1 text-sm font-semibold text-ink'][niveau - 1]
      var Tag = ('h' + Math.min(niveau + 1, 6)) as keyof React.JSX.IntrinsicElements
      blocs.push(React.createElement(Tag, { key: 'h' + blocs.length, className: classes }, inline(titre[2], 'h' + blocs.length)))
      i++
      continue
    }

    // Regle horizontale
    if (/^---+$/.test(ligne.trim())) {
      viderPara(); viderPuces()
      blocs.push(<hr key={'hr' + blocs.length} className="my-6 border-paper-line" />)
      i++
      continue
    }

    // Citation
    if (/^>\s?/.test(ligne)) {
      viderPara(); viderPuces()
      var cite: string[] = []
      while (i < lignes.length && /^>\s?/.test(lignes[i])) { cite.push(lignes[i].replace(/^>\s?/, '')); i++ }
      blocs.push(<blockquote key={'q' + blocs.length} className="my-3 border-l-2 border-signature/40 pl-3 text-steel">{inline(cite.join(' '), 'q' + blocs.length)}</blockquote>)
      continue
    }

    // Listes
    var puce = ligne.match(/^(\s*)([-*]|\d+\.)\s+(.*)$/)
    if (puce) {
      viderPara()
      puces.push({ indent: puce[1].length, texte: puce[3], ordonnee: /\d+\./.test(puce[2]) })
      i++
      continue
    }

    // Ligne vide
    if (ligne.trim() === '') {
      viderPara(); viderPuces()
      i++
      continue
    }

    para.push(ligne.trim())
    i++
  }
  viderPara(); viderPuces()

  return <div className="text-[15px]">{blocs}</div>
}
