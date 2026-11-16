#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

# Use the .NET SDK from PATH by default. Set DOTNET to another executable to override.
DOTNET="${DOTNET:-dotnet}"

# Generate the building animation (Pillow only). If Pillow is missing:
#   python3 -m pip install pillow
# Without Pillow, skip generation and use the animation assets in the repo.
if command -v python3 >/dev/null 2>&1 && python3 -c "import PIL" >/dev/null 2>&1; then
    python3 tools/generate_door_anim.py
else
    echo "Skipping animation generation: python3 or Pillow not found."
fi

"$DOTNET" build -c Release "$@"

echo
echo "Deployed to:"
echo "  $HOME/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-AnywhereDoor"
echo "  $HOME/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Steam/ONI-AnywhereDoor"
