#!/bin/bash
# Régénère les 2 PDF de docs/architecture/ à partir des sources Mermaid-en-HTML
# (artifact-diagrammes-uml.html / artifact-explique-simplement.html) -- CE sont
# les vraies sources des PDF livrés, pas les .puml de ce dossier (exploration
# PlantUML abandonnée en cours de route au profit de Mermaid directement dans
# l'Artifact -- gardée ici à titre de référence alternative, jamais utilisée
# pour produire les PDF réels).
#
# Piège rencontré et corrigé : les blocs <pre class="mermaid"> contiennent du
# Mermaid brut avec des stéréotypes <<domain service>> -- si on charge le HTML
# tel quel dans un navigateur (file://), le parseur HTML interprète le premier
# "<" comme le début d'une balise et casse le diagramme ("Syntax error in
# text"). Ça ne se voit PAS dans l'Artifact viewer (qui échappe correctement
# avant insertion), seulement en rendu local -- d'où l'étape d'échappement
# ci-dessous, obligatoire.
#
# Prérequis : Java (plantuml.jar présent mais non utilisé ici), google-chrome,
# et un accès réseau pour récupérer mermaid.min.js (mis en cache une fois).
set -euo pipefail
cd "$(dirname "$0")"

[ -f mermaid.min.js ] || curl -sL -o mermaid.min.js https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js

render() {
  local src="$1" out="$2" pagesize="$3"
  python3 - "$src" "$pagesize" > "/tmp/$(basename "$src" .html)-render.html" << 'PYEOF'
import re, sys
src, pagesize = sys.argv[1], sys.argv[2]
with open(src) as f:
    content = f.read()

def repl(m):
    inner = m.group(1)
    return '<pre class="mermaid">' + inner.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;') + '</pre>'
content = re.sub(r'<pre class="mermaid">(.*?)</pre>', repl, content, flags=re.DOTALL)

inject = f'<script src="mermaid.min.js"></script>\n<script>mermaid.initialize({{startOnLoad:true, securityLevel:"loose"}});</script>\n<style>@page {{ size: {pagesize}; margin: 12mm; }}</style>\n'
final = '<!doctype html><html><head><meta charset="utf-8">' + content
final = final.replace('</title>', '</title>' + inject, 1)
final += '</html>'
print(final)
PYEOF
  local render_file="/tmp/$(basename "$src" .html)-render.html"
  cp "$render_file" ./_tmp_render.html
  google-chrome --headless --disable-gpu --no-sandbox --virtual-time-budget=10000 \
    --print-to-pdf="$out" --print-to-pdf-no-header --no-pdf-header-footer \
    "file://$(pwd)/_tmp_render.html" 2>/dev/null
  rm -f ./_tmp_render.html "$render_file"
  if pdftotext "$out" - 2>/dev/null | grep -q "Syntax error"; then
    echo "ECHEC : erreur de syntaxe Mermaid dans $out -- ne pas livrer, corriger la source d'abord." >&2
    exit 1
  fi
  echo "OK: $out"
}

render artifact-diagrammes-uml.html ../EBIOS-RM-diagrammes-UML.pdf "A3 landscape"
render artifact-explique-simplement.html ../EBIOS-RM-explique-simplement.pdf "A4"
