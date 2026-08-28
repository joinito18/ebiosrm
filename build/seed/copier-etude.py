#!/usr/bin/env python3
"""
Copie une étude complète d'une base PostgreSQL (dev/hébergée) vers une base
SQLite déjà créée (schéma via EnsureCreated). Toutes les clés sont conservées
telles quelles -- aucun remapping de FK, juste une conversion des types au
format attendu par le fournisseur SQLite d'EF Core :

  uuid        -> TEXT majuscules "D"       (956A1CEE-...)
  timestamptz -> TEXT "YYYY-MM-DD HH:MM:SS.ffffff" (sans fuseau)
  boolean     -> 0 / 1
  jsonb       -> texte tel quel
  uuid[]      -> tableau JSON de TEXT majuscules

Usage :
  python3 copier-etude.py <sqlite_cible> <nom_etude> [--public]

--public : force ProprietaireId = NULL (étude d'exemple visible et en lecture
           seule pour tous les comptes).
"""
import json
import sys
import uuid
import datetime
import sqlite3

import psycopg2
import psycopg2.extras

# Sans ca, psycopg2 renvoie les colonnes uuid en str minuscules ; on veut des
# objets uuid.UUID pour les reformater en majuscules (format attendu par EF/SQLite).
psycopg2.extras.register_uuid()

PG_DSN = "host=localhost port=5433 dbname=ebiosrm user=ebiosrm password=ebiosrm_dev"
SCHEMA = "core_engine"

# Ordre parents -> enfants. (table, colonne de filtrage, table_parent_pour_le_filtre)
#   parent_filtre = None  -> filtre direct sur EtudeId
#   parent_filtre = "x"   -> filtre sur <colonne> IN (ids deja copies de x)
PLAN = [
    ("etudes", "Id", "etude"),
    ("valeurs_metier", "EtudeId", None),
    ("biens_support", "EtudeId", None),
    ("evenements_redoutes", "EtudeId", None),
    ("socles_securite", "EtudeId", None),
    ("referentiels_applicables", "SocleSecuriteId", "socles_securite"),
    ("couples_sr_ov", "EtudeId", None),
    ("parties_prenantes", "EtudeId", None),
    ("mesures_ecosysteme", "PartiePrenanteId", "parties_prenantes"),
    ("scenarios_strategiques", "EtudeId", None),
    ("chemins_attaque", "EtudeId", None),
    ("evenements_intermediaires", "CheminAttaqueId", "chemins_attaque"),
    ("scenarios_operationnels", "EtudeId", None),
    ("modes_operatoires", "ScenarioOperationnelId", "scenarios_operationnels"),
    ("actions_elementaires", "ModeOperatoireId", "modes_operatoires"),
    ("scenarios_de_risque", "EtudeId", None),
    ("plans_traitement_risque", "EtudeId", None),
    ("mesures_traitement_risque", "PlanTraitementRisqueId", "plans_traitement_risque"),
    ("snapshots_atelier", "EtudeId", None),
]


# Colonnes PrimitiveCollection<List<Guid>> : tableau natif cote PostgreSQL,
# tableau JSON cote SQLite (EF Core 8).
COLONNES_TABLEAU_GUID = {"scenarios_de_risque_ids"}


def convertir(colonne, valeur):
    if valeur is None:
        return None
    if colonne in COLONNES_TABLEAU_GUID:
        if isinstance(valeur, str):  # litteral PostgreSQL "{a,b}"
            contenu = valeur.strip("{}")
            items = [x.strip().strip('"') for x in contenu.split(",")] if contenu else []
        else:
            items = list(valeur)
        return json.dumps([str(x).upper() for x in items])
    if isinstance(valeur, uuid.UUID):
        return str(valeur).upper()
    if isinstance(valeur, bool):
        return 1 if valeur else 0
    if isinstance(valeur, datetime.datetime):
        return valeur.strftime("%Y-%m-%d %H:%M:%S.%f")
    if isinstance(valeur, datetime.date):
        return valeur.isoformat()
    if isinstance(valeur, (dict, list)):
        return json.dumps(valeur, ensure_ascii=False)
    return valeur


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    cible = sys.argv[1]
    nom_etude = sys.argv[2]
    public = "--public" in sys.argv[3:]

    pg = psycopg2.connect(PG_DSN)
    pg.autocommit = True
    cur = pg.cursor(cursor_factory=psycopg2.extras.RealDictCursor)

    cur.execute(f'SELECT "Id" FROM {SCHEMA}.etudes WHERE "Nom" = %s', (nom_etude,))
    lignes = cur.fetchall()
    if len(lignes) != 1:
        sys.exit(f"Attendu 1 étude nommée {nom_etude!r}, trouvé {len(lignes)}.")
    etude_id = lignes[0]["Id"]
    print(f"Étude : {nom_etude}  ({etude_id})")

    sq = sqlite3.connect(cible)
    ids_par_table = {}
    total = 0

    for table, colonne, parent in PLAN:
        if parent == "etude":
            where, params = f'"{colonne}" = %s', (etude_id,)
        elif parent is None:
            where, params = f'"{colonne}" = %s', (etude_id,)
        else:
            ids_parent = ids_par_table.get(parent, [])
            if not ids_parent:
                ids_par_table[table] = []
                print(f"  {table}: 0 (parent vide)")
                continue
            where = f'"{colonne}" = ANY(%s::uuid[])'
            params = ([str(i) for i in ids_parent],)

        cur.execute(f'SELECT * FROM {SCHEMA}.{table} WHERE {where}', params)
        rows = cur.fetchall()
        ids_par_table[table] = [r["Id"] for r in rows if "Id" in r]

        for r in rows:
            if table == "etudes" and public:
                r["ProprietaireId"] = None
            cols = list(r.keys())
            placeholders = ",".join("?" for _ in cols)
            colnames = ",".join(f'"{c}"' for c in cols)
            valeurs = [convertir(c, r[c]) for c in cols]
            sq.execute(f'INSERT INTO "{table}" ({colnames}) VALUES ({placeholders})', valeurs)

        total += len(rows)
        print(f"  {table}: {len(rows)}")

    sq.commit()
    sq.close()
    pg.close()
    print(f"OK -- {total} lignes copiées dans {cible}")


if __name__ == "__main__":
    main()
