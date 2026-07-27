#!/bin/sh
set -eu

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="$(CDPATH= cd -- "${SCRIPT_DIRECTORY}/../../.." && pwd)"
UNITY_PROJECT="${REPOSITORY_ROOT}/unity/Rippies"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/MacOS/Unity}"
MODE="${1:-simulator}"

case "${MODE}" in
  simulator)
    METHOD="Rippies.Reveal.Editor.IosLibraryExporter.ExportSimulator"
    OUTPUT="${UNITY_PROJECT}/Build/iOS-Simulator"
    ;;
  device)
    METHOD="Rippies.Reveal.Editor.IosLibraryExporter.ExportDevice"
    OUTPUT="${UNITY_PROJECT}/Build/iOS-Device"
    ;;
  *)
    echo "Usage: $0 [simulator|device]" >&2
    exit 64
    ;;
esac

"${UNITY_EDITOR}" \
  -batchmode \
  -quit \
  -projectPath "${UNITY_PROJECT}" \
  -executeMethod "${METHOD}" \
  -rippiesOutput "${OUTPUT}" \
  -logFile -
