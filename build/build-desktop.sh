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

echo "==> Publication backend ($RID, autonome, fichier unique)"
rm -rf "$SORTIE"
dotnet publish "$API/EbiosRM.Api.csproj" \
  -c Release -r "$RID" --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=none \
  -o "$SORTIE"

# appsettings.Development.json n'a rien à faire dans une distribution.
rm -f "$SORTIE/appsettings.Development.json"

# Renomme l'exécutable hôte en "EbiosRM" (sans incidence : le nom de l'exe
# est indépendant du nom de l'assembly pour une publication fichier unique).
if [[ "$RID" == win-* ]]; then
  mv "$SORTIE/EbiosRM.Api.exe" "$SORTIE/EbiosRM.exe"
else
  mv "$SORTIE/EbiosRM.Api" "$SORTIE/EbiosRM"
  chmod +x "$SORTIE/EbiosRM"
fi

echo
echo "==> OK : $SORTIE"
ls -lh "$SORTIE"
