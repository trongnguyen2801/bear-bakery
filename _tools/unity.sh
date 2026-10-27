#!/bin/zsh
# usage: unity.sh <method> <logname> [extra args...]
cd /Users/trongnp/BearBakery
M=$1; L=$2; shift 2
/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/trongnp/BearBakery -executeMethod $M -logFile /Users/trongnp/BearBakery/Logs/$L.log -quit "$@" > /dev/null 2>&1
echo "EXIT $?"
grep -E "error CS|Scripts have compiler errors|\[Setup\]|\[Build\]|\[ModelTests\]|\[VerifyArt\]|\[BalanceSim\]|Exception" /Users/trongnp/BearBakery/Logs/$L.log | sort -u | head -60
