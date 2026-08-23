#!/bin/bash
set -e

API="http://localhost:5197/api/v1"
FRONT="http://localhost:5174"

echo "Verification du backend..."
curl -sf "$API/health" > /dev/null || { echo "ERREUR : backend injoignable sur $API. Lancez 'dotnet run' d'abord."; exit 1; }

echo "1. Creation de l etude..."
ETUDE=$(curl -s -X POST "$API/etudes" \
  -H "Content-Type: application/json" \
  -d '{"nom":"Etude seed automatique","perimetre":"Perimetre de test genere automatiquement"}')
ETUDE_ID=$(echo "$ETUDE" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
echo "   Etude creee : $ETUDE_ID"

echo "2. Demarrage de l atelier 1..."
curl -s -X POST "$API/etudes/$ETUDE_ID/demarrer-atelier1" > /dev/null

echo "3. Ajout de deux valeurs metier..."
VM1=$(curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier" \
  -H "Content-Type: application/json" \
  -d '{"description":"Recherche et developpement","entiteResponsable":"Direction technique"}')
VM1_ID=$(echo "$VM1" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")

VM2=$(curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier" \
  -H "Content-Type: application/json" \
  -d '{"description":"Production et distribution","entiteResponsable":"Direction operationnelle"}')
VM2_ID=$(echo "$VM2" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
echo "   Valeurs metier : $VM1_ID, $VM2_ID"

echo "4. Ajout de biens support..."
curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier/$VM1_ID/biens-support" \
  -H "Content-Type: application/json" \
  -d '{"description":"Serveur applicatif interne","type":"SystemeInformation","entiteResponsable":"DSI"}' > /dev/null

curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier/$VM2_ID/biens-support" \
  -H "Content-Type: application/json" \
  -d '{"description":"Reseau de production","type":"Reseau","entiteResponsable":"DSI"}' > /dev/null
echo "   2 biens support ajoutes"

echo "5. Ajout d evenements redoutes..."
curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier/$VM1_ID/evenements-redoutes" \
  -H "Content-Type: application/json" \
  -d '{"description":"Vol de propriete intellectuelle sur les projets en cours","gravite":3}' > /dev/null

curl -s -X POST "$API/etudes/$ETUDE_ID/valeurs-metier/$VM2_ID/evenements-redoutes" \
  -H "Content-Type: application/json" \
  -d '{"description":"Arret de la chaine de production pendant plus de 48h","gravite":4}' > /dev/null
echo "   2 evenements redoutes ajoutes"

echo "6. Creation du socle de securite..."
curl -s -X POST "$API/etudes/$ETUDE_ID/socle-securite" > /dev/null

echo "7. Ajout de controles ISO 27001 et d un referentiel libre..."
curl -s -X POST "$API/etudes/$ETUDE_ID/socle-securite/referentiels" \
  -H "Content-Type: application/json" \
  -d '{"nom":"A.5.15 -- Controle d acces","etat":"Conforme","theme":"Organisationnel","codeControle":"A.5.15"}' > /dev/null

curl -s -X POST "$API/etudes/$ETUDE_ID/socle-securite/referentiels" \
  -H "Content-Type: application/json" \
  -d '{"nom":"A.8.24 -- Utilisation de la cryptographie","etat":"NonConforme","theme":"Technologique","codeControle":"A.8.24"}' > /dev/null

curl -s -X POST "$API/etudes/$ETUDE_ID/socle-securite/referentiels" \
  -H "Content-Type: application/json" \
  -d '{"nom":"A.7.2 -- Entree physique","etat":"Conforme","theme":"Physique","codeControle":"A.7.2"}' > /dev/null

curl -s -X POST "$API/etudes/$ETUDE_ID/socle-securite/referentiels" \
  -H "Content-Type: application/json" \
  -d '{"nom":"PSSI de l organisation","etat":"Conforme"}' > /dev/null
echo "   4 referentiels ajoutes (3 ISO + 1 libre)"

echo ""
echo "=== Seed termine ==="
echo "Etude : $ETUDE_ID"
echo "Ouvrez directement dans le navigateur :"
echo "  Dossier de l etude : $FRONT/etudes/$ETUDE_ID"
echo "  Atelier 1           : $FRONT/etudes/$ETUDE_ID/ateliers/1"
echo ""
echo "L etude est en statut EnCours -- vous pouvez tester le bouton 'Valider l atelier'"
echo "manuellement pour verifier le flux de validation + snapshot + PDF."
