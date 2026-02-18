#!/usr/bin/env bash
# sync_maps.sh — Syncs Content/Maps/*.tmx files into StarterTD.csproj and MapData.cs.
# Run this after adding or removing any .tmx file in Content/Maps/.
# Usage: ./sync_maps.sh [--dry-run]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAPS_DIR="$SCRIPT_DIR/Content/Maps"
CSPROJ="$SCRIPT_DIR/StarterTD.csproj"
MAPDATA="$SCRIPT_DIR/Engine/MapData.cs"
DRY_RUN=false

if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=true
    echo "[dry-run] No files will be modified."
fi

# --- Collect map IDs from .tmx files (filename without extension) ---
# Uses a while-read loop for compatibility with macOS default bash 3.
MAP_IDS=()
while IFS= read -r f; do
    MAP_IDS+=("$(basename "$f" .tmx)")
done < <(find "$MAPS_DIR" -maxdepth 1 -name "*.tmx" | sort)

if [[ ${#MAP_IDS[@]} -eq 0 ]]; then
    echo "No .tmx files found in $MAPS_DIR. Nothing to sync."
    exit 0
fi

echo "Found ${#MAP_IDS[@]} map(s): ${MAP_IDS[*]}"

# --- Update StarterTD.csproj ---
# Replaces the <ItemGroup> block containing Content\Maps entries.
# Passes map IDs as a colon-delimited string so awk can receive it without newline issues.
IDS_COLON=$(IFS=:; echo "${MAP_IDS[*]}")

UPDATED_CSPROJ=$(awk -v ids="$IDS_COLON" '
    # When we enter an ItemGroup, buffer it until we know if it contains Maps entries
    /^  <ItemGroup>/ { in_block=1; buffer=$0 "\n"; next }

    in_block {
        buffer = buffer $0 "\n"
        if ($0 ~ /Content\\Maps/) { is_maps_block=1 }
        if (/^  <\/ItemGroup>/) {
            if (is_maps_block) {
                # Emit replacement block built from the colon-delimited ID list
                print "  <ItemGroup>"
                n = split(ids, arr, ":")
                for (i = 1; i <= n; i++) {
                    id = arr[i]
                    print "    <Content Include=\"Content\\Maps\\" id ".tmx\">"
                    print "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>"
                    print "    </Content>"
                }
                print "  </ItemGroup>"
            } else {
                printf "%s", buffer
            }
            in_block=0; is_maps_block=0; buffer=""
        }
        next
    }

    { print }
' "$CSPROJ")

# --- Update Engine/MapData.cs GetAvailableMaps() ---
# Replaces the single-line: new() { "id1", "id2" };
IDS_QUOTED=$(printf '"%s", ' "${MAP_IDS[@]}")
IDS_QUOTED="${IDS_QUOTED%, }"   # strip trailing ", "
MAPDATA_LINE="        new() { $IDS_QUOTED };"

# Escape special sed characters in the replacement string
MAPDATA_LINE_ESCAPED=$(printf '%s\n' "$MAPDATA_LINE" | sed 's/[&/\]/\\&/g')
UPDATED_MAPDATA=$(sed "s|        new() { .* };|$MAPDATA_LINE_ESCAPED|" "$MAPDATA")

# --- Apply or preview ---
if $DRY_RUN; then
    echo ""
    echo "=== StarterTD.csproj Maps ItemGroup would become ==="
    echo "$UPDATED_CSPROJ" | grep -A 99 'Content\\Maps' | grep -B 99 '</ItemGroup>' | head -20
    echo ""
    echo "=== Engine/MapData.cs GetAvailableMaps() line would become ==="
    echo "$UPDATED_MAPDATA" | grep 'new() {'
else
    printf '%s\n' "$UPDATED_CSPROJ" > "$CSPROJ"
    printf '%s\n' "$UPDATED_MAPDATA" > "$MAPDATA"
    echo "Updated $CSPROJ"
    echo "Updated $MAPDATA"
    echo "Done. Run 'dotnet build' to verify."
fi
