#!/usr/bin/env bash
# Construit l'application de bureau : frontend embarqué + backend .NET publié
# en exécutable autonome (aucune installation de .NET / Node requise chez
# l'utilisateur final).
#
#   ./build/build-desktop.sh [rid]
#
# rid (Runtime IDentifier) : win-x64 (défaut), linux-x64, osx-x64, osx-arm64.
# Résultat : build/output/<rid>/
set -euo pipefail

RID="${1:-win-x64}"
RACINE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SORTIE="$RACINE/build/output/$RID"
API="$RACINE/src/EbiosRM.Api"

echo "==> Frontend (VITE_API_BASE=/api/v1)"
cd "$RACINE/frontend"
npm ci
VITE_API_BASE=/api/v1 npm run build

echo "==> Copie du frontend dans wwwroot"
rm -rf "$API/wwwroot"
mkdir -p "$API/wwwroot"
cp -r dist/* "$API/wwwroot/"

# Autonome (aucun .NET requis) mais PAS fichier unique : un exe fichier unique
# se ré-extrait à chaque premier lancement (~15 s sans retour visible depuis le
# menu -> "rien ne se passe"). Un dossier de fichiers démarre instantanément.
echo "==> Publication backend ($RID, autonome)"
rm -rf "$SORTIE"
dotnet publish "$API/EbiosRM.Api.csproj" \
  -c Release -r "$RID" --self-contained \
  -p:DebugType=none \
  -o "$SORTIE"

# appsettings.Development.json n'a rien à faire dans une distribution.
rm -f "$SORTIE/appsettings.Development.json" "$SORTIE/web.config"

# Renomme l'exécutable hôte en "EbiosRM" (l'apphost, indépendant du nom d'assembly).
if [[ "$RID" == win-* ]]; then
  mv "$SORTIE/EbiosRM.Api.exe" "$SORTIE/EbiosRM.exe"
else
  mv "$SORTIE/EbiosRM.Api" "$SORTIE/EbiosRM"
  chmod +x "$SORTIE/EbiosRM"
fi

# Sous Linux : script d'installation dans le menu des applications + icone.
if [[ "$RID" == linux-* ]]; then
  cp "$RACINE/build/linux/installer.sh" "$SORTIE/installer.sh"
  cp "$RACINE/build/linux/EbiosRM.desktop" "$SORTIE/EbiosRM.desktop"
  cp "$RACINE/frontend/public/favicon.svg" "$SORTIE/favicon.svg"
  chmod +x "$SORTIE/installer.sh"
fi

echo
echo "==> OK : $SORTIE"
ls -lh "$SORTIE"
