#!/usr/bin/env bash
# Fabrique src/EbiosRM.Api/ressources/ebiosrm.seed.db : une base SQLite
# contenant l'étude d'exemple "Atlas Assurances Santé", embarquée dans le
# .exe et déposée dans le dossier de données au tout premier lancement.
#
# Pré-requis : la base PostgreSQL de dev tourne (docker compose up -d) et
# contient l'étude "Atlas Assurances Santé" ; python3 + psycopg2 installés.
set -euo pipefail

RACINE="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API="$RACINE/src/EbiosRM.Api"
RESSOURCES="$API/ressources"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

ETUDE="${1:-Atlas Assurances Sante}"

echo "==> Schéma SQLite vierge (EnsureCreated)"
dotnet build "$API/EbiosRM.Api.csproj" -c Release -v quiet
Database__Provider=Sqlite App__DossierDonnees="$TMP" \
  Jwt__Secret="seed-$(head -c16 /dev/urandom | base64)" \
  App__OuvrirNavigateur=false ASPNETCORE_URLS=http://localhost:5091 ASPNETCORE_ENVIRONMENT=Production \
  dotnet run --project "$API/EbiosRM.Api.csproj" -c Release --no-build --no-launch-profile > "$TMP/api.log" 2>&1 &
PID=$!
for _ in $(seq 1 30); do
  curl -sf -o /dev/null http://localhost:5091/api/v1/health && break || sleep 1
done
kill "$PID" 2>/dev/null || true
wait "$PID" 2>/dev/null || true
sqlite3 "$TMP/ebiosrm.db" "PRAGMA wal_checkpoint(TRUNCATE);" >/dev/null 2>&1 || true

echo "==> Copie de l'étude d'exemple depuis PostgreSQL"
python3 "$RACINE/build/seed/copier-etude.py" "$TMP/ebiosrm.db" "$ETUDE" --public

echo "==> Compactage + dépôt dans ressources/"
mkdir -p "$RESSOURCES"
sqlite3 "$TMP/ebiosrm.db" "VACUUM;"
rm -f "$TMP/ebiosrm.db-wal" "$TMP/ebiosrm.db-shm"
cp "$TMP/ebiosrm.db" "$RESSOURCES/ebiosrm.seed.db"

echo
echo "==> OK : $RESSOURCES/ebiosrm.seed.db  ($(du -h "$RESSOURCES/ebiosrm.seed.db" | cut -f1))"
