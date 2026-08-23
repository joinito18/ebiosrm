#!/bin/bash
# Crée (si besoin) et migre la base Postgres dédiée aux tests d'intégration
# (tests/EbiosRM.Api.Tests/Integration/) -- jamais "ebiosrm" (données de démo
# BioGenTech), toujours "ebiosrm_test" sur le même serveur Postgres du
# docker-compose local. À lancer une fois après un premier clone, et à
# nouveau après chaque nouvelle migration EF Core ajoutée au projet.
set -euo pipefail
cd "$(dirname "$0")/../src/EbiosRM.Api"

HOST=localhost
PORT=5433
USER=ebiosrm
PASSWORD=ebiosrm_dev

PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -U "$USER" -d postgres -tc \
  "SELECT 1 FROM pg_database WHERE datname = 'ebiosrm_test'" | grep -q 1 \
  || PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -U "$USER" -d postgres -c "CREATE DATABASE ebiosrm_test;"

export ConnectionStrings__EbiosDb="Host=$HOST;Port=$PORT;Database=ebiosrm_test;Username=$USER;Password=$PASSWORD"
dotnet ef database update

echo "Base ebiosrm_test prête."
