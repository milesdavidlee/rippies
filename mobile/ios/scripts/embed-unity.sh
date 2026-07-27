#!/bin/sh
set -eu

REPOSITORY_ROOT="${SRCROOT}/../.."
case "${PLATFORM_NAME}" in
  iphonesimulator)
    UNITY_EXPORT="${REPOSITORY_ROOT}/unity/Rippies/Build/iOS-Simulator"
    ;;
  iphoneos)
    UNITY_EXPORT="${REPOSITORY_ROOT}/unity/Rippies/Build/iOS-Device"
    ;;
  *)
    echo "[RippiesUnity] Skipping unsupported platform ${PLATFORM_NAME}."
    exit 0
    ;;
esac

if [ ! -d "${UNITY_EXPORT}/Unity-iPhone.xcodeproj" ]; then
  echo "[RippiesUnity] No ${PLATFORM_NAME} export found; keeping the native fallback."
  echo "[RippiesUnity] Run mobile/ios/scripts/export-unity-ios.sh."
  exit 0
fi

UNITY_BUILD_ROOT="${RIPPIES_UNITY_BUILD_ROOT:-${TMPDIR%/}/RippiesUnity-${CONFIGURATION}-${PLATFORM_NAME}}"
/usr/bin/env -i \
  HOME="${HOME}" \
  USER="${USER}" \
  PATH="${PATH}" \
  TMPDIR="${TMPDIR}" \
  DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}" \
  /usr/bin/xcodebuild \
  -project "${UNITY_EXPORT}/Unity-iPhone.xcodeproj" \
  -target UnityFramework \
  -configuration "${CONFIGURATION}" \
  -sdk "${SDK_NAME}" \
  SYMROOT="${UNITY_BUILD_ROOT}" \
  PRODUCT_NAME=UnityFramework \
  EXECUTABLE_NAME=UnityFramework \
  ENABLE_DEBUG_DYLIB=NO \
  CODE_SIGNING_ALLOWED=NO \
  build

UNITY_FRAMEWORK="${UNITY_BUILD_ROOT}/${CONFIGURATION}-${PLATFORM_NAME}/UnityFramework.framework"
if [ ! -d "${UNITY_FRAMEWORK}" ]; then
  echo "error: UnityFramework was not produced at ${UNITY_FRAMEWORK}." >&2
  exit 1
fi

DESTINATION="${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}"
mkdir -p "${DESTINATION}"
rsync -a --delete "${UNITY_FRAMEWORK}/" "${DESTINATION}/UnityFramework.framework/"
rsync -a --delete "${UNITY_EXPORT}/Data/" "${DESTINATION}/UnityFramework.framework/Data/"

if [ "${PLATFORM_NAME}" = "iphonesimulator" ]; then
  codesign --force --sign - "${DESTINATION}/UnityFramework.framework"
elif [ "${CODE_SIGNING_ALLOWED:-NO}" = "YES" ] && [ -n "${EXPANDED_CODE_SIGN_IDENTITY:-}" ]; then
  codesign --force --sign "${EXPANDED_CODE_SIGN_IDENTITY}" "${DESTINATION}/UnityFramework.framework"
fi

echo "[RippiesUnity] Embedded ${UNITY_FRAMEWORK}."
