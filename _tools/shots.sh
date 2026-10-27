#!/bin/zsh
# usage: shots.sh W H [scenario] [extra]
cd /Users/trongnp/BearBakery; setopt NULL_GLOB
W=$1; H=$2; SC=${3:-shots}; shift 3 2>/dev/null
SLOT=auto${W}x${H}_$SC
rm -f "$HOME/Library/Application Support/com.prototype.tiembanhgau/$SLOT.json"*
mkdir -p Docs/screens
open -W -n Builds/macOS/TiemBanhGau.app --args -screen-width $W -screen-height $H -screen-fullscreen 0 -bbAuto $SC -bbOut /Users/trongnp/BearBakery/Docs/screens -bbSlot $SLOT -logFile /Users/trongnp/BearBakery/Logs/player_${W}x${H}_$SC.log "$@"
grep -E "SELFTEST|PERF|STATS|Exception|NullRef" Docs/screens/autolog_${W}x${H}_$SC.txt Logs/player_${W}x${H}_$SC.log | sort -u | head -30
