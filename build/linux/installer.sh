#!/usr/bin/env bash
# Installe EBIOS RM pour l'utilisateur courant (aucun sudo) :
#  - copie l'application dans ~/.local/opt/ebiosrm
#  - ajoute une entree dans le menu des applications (Activites / grille GNOME)
#  - cree la commande "ebiosrm" dans ~/.local/bin
#
# A lancer depuis le dossier extrait de l'archive :
#   tar xzf EbiosRM-*-linux.tar.gz && cd EbiosRM-* && ./installer.sh
#
# Desinstallation : ./installer.sh --desinstaller
set -euo pipefail

DEST="$HOME/.local/opt/ebiosrm"
BIN="$HOME/.local/bin/ebiosrm"
DESKTOP="$HOME/.local/share/applications/ebiosrm.desktop"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ "${1:-}" == "--desinstaller" ]]; then
  rm -rf "$DEST" "$BIN" "$DESKTOP"
  echo "EBIOS RM desinstalle. (Vos donnees dans ~/.local/share/EbiosRM sont conservees.)"
  exit 0
fi

echo "Installation dans $DEST ..."
rm -rf "$DEST"
mkdir -p "$DEST" "$(dirname "$BIN")" "$(dirname "$DESKTOP")"
cp -r "$SRC"/. "$DEST"/
rm -f "$DEST/installer.sh"
chmod +x "$DEST/EbiosRM"

ln -sf "$DEST/EbiosRM" "$BIN"

sed -e "s|__EXEC__|$DEST/EbiosRM|" -e "s|__ICON__|$DEST/favicon.svg|" \
  "$SRC/EbiosRM.desktop" > "$DESKTOP"
chmod +x "$DESKTOP"
update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

echo
echo "Termine."
echo "  - Lancer depuis le menu des applications : \"EBIOS RM\""
echo "  - ou en terminal : ebiosrm   (si ~/.local/bin est dans le PATH)"
echo "  - Donnees : ~/.local/share/EbiosRM/ebiosrm.db"
