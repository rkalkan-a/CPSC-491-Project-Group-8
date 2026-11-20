#!/usr/bin/env bash
# Builds a tested, packaged macOS release of the game in one command.
#
#   tools/release/build_release.sh                  run all tests, build, smoke-test and package
#   tools/release/build_release.sh --version 1.0.0  set the version shown in the app (default: the project's)
#   tools/release/build_release.sh --skip-tests     build without running the tests first (not for a real release)
#   tools/release/build_release.sh --unity <path>   use a specific Unity editor binary
#
# Steps, and it stops at the first one that fails:
#   1. checks: Unity editor found, project not open in another Unity window
#   2. runs every EditMode and PlayMode test in batch mode and stops if any fail
#   3. builds the macOS app (Assets/_Game/Editor/ReleaseBuild.cs)
#   4. smoke test: launches the built app headless and checks it reaches the Main Menu without errors
#   5. packages it: a zip of the .app, its SHA-256 checksum, and release notes
#
# Everything goes to Builds/release/<name>/ (Builds/ is in .gitignore). Nothing is uploaded or pushed.
# No secrets are needed: it uses the Unity license already signed in on this machine through Unity Hub.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

VERSION=""
SKIP_TESTS=0
UNITY="${UNITY_PATH:-}"
SMOKE_SECONDS=8

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) VERSION="${2:?--version needs a value}"; shift 2 ;;
    --skip-tests) SKIP_TESTS=1; shift ;;
    --unity) UNITY="${2:?--unity needs a path}"; shift 2 ;;
    -h|--help) sed -n '2,19p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown option: $1 (see --help)" >&2; exit 2 ;;
  esac
done

step() { printf '\n==> %s\n' "$*"; }
fail() { printf '\nRELEASE FAILED: %s\n' "$*" >&2; exit 1; }
stamp() { date '+%Y-%m-%d %H:%M:%S %Z'; }

# ---------------------------------------------------------------- 1. checks
step "1/5 Checks"
[[ "$(uname)" == "Darwin" ]] || fail "this script builds the macOS app, so it has to run on a Mac"

PROJECT_UNITY="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
if [[ -z "$UNITY" ]]; then
  UNITY="/Applications/Unity/Hub/Editor/$PROJECT_UNITY/Unity.app/Contents/MacOS/Unity"
fi
if [[ ! -x "$UNITY" ]]; then
  echo "Installed editors:" >&2
  ls /Applications/Unity/Hub/Editor 2>/dev/null >&2 || echo "  (none found)" >&2
  fail "Unity $PROJECT_UNITY not found at $UNITY. Install it from Unity Hub, or pass --unity <path to the Unity binary>"
fi
UNITY_VERSION="$("$UNITY" -version 2>/dev/null | tail -1)"
echo "Unity:   $UNITY_VERSION ($UNITY)"

# Unity refuses to open a project that's already open, and the error only shows up deep in a log
if [[ -f Temp/UnityLockfile ]] && lsof Temp/UnityLockfile >/dev/null 2>&1; then
  fail "the project is open in Unity. Close the Unity window for this project and run again"
fi

COMMIT="$(git rev-parse --short HEAD)"
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
DIRTY=""
if ! git diff --quiet HEAD -- Assets Packages ProjectSettings; then
  DIRTY="-dirty"
  echo "Warning: Assets, Packages or ProjectSettings have uncommitted changes, so this build won't match commit $COMMIT exactly."
fi
[[ -n "$VERSION" ]] || VERSION="$(sed -n 's/^  bundleVersion: //p' ProjectSettings/ProjectSettings.asset)"
NAME="CPSC491-Group8-$VERSION-$COMMIT$DIRTY"
OUT="$ROOT/Builds/release/$NAME"
LOGS="$OUT/logs"
rm -rf "$OUT"
mkdir -p "$LOGS"
echo "Version: $VERSION"
echo "Commit:  $COMMIT$DIRTY on $BRANCH"
echo "Output:  $OUT"
STARTED="$(stamp)"

# ---------------------------------------------------------------- 2. tests
# prints "passed failed total" from a Unity test results file
count_results() {
  python3 -I - "$1" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(r.get("passed", "0"), r.get("failed", "0"), r.get("total", "0"))
PY
}

TEST_SUMMARY="skipped (--skip-tests)"
if [[ $SKIP_TESTS -eq 0 ]]; then
  step "2/5 Tests"
  TEST_SUMMARY=""
  for MODE in EditMode PlayMode; do
    RESULTS="$LOGS/$MODE-results.xml"
    echo "$MODE: started $(stamp)"
    set +e
    "$UNITY" -batchmode -nographics -projectPath "$ROOT" -runTests -testPlatform "$MODE" \
      -testResults "$RESULTS" -logFile "$LOGS/$MODE-tests.log"
    CODE=$?
    set -e
    [[ -f "$RESULTS" ]] || fail "$MODE tests didn't produce results (exit $CODE). See $LOGS/$MODE-tests.log"
    read -r PASSED FAILED TOTAL < <(count_results "$RESULTS")
    echo "$MODE: $PASSED/$TOTAL passed, $FAILED failed"
    TEST_SUMMARY+="$MODE $PASSED/$TOTAL passed; "
    if [[ $CODE -ne 0 || $FAILED -ne 0 ]]; then
      fail "$MODE tests failed ($FAILED failing). Fix them before releasing. Details: $RESULTS"
    fi
  done
  TEST_SUMMARY="${TEST_SUMMARY%; }"
else
  step "2/5 Tests skipped (--skip-tests)"
fi

# ---------------------------------------------------------------- 3. build
step "3/5 Build macOS app"
echo "Build: started $(stamp)"
set +e
"$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -buildTarget StandaloneOSX \
  -executeMethod DungeonCrawler.EditorTools.ReleaseBuild.BuildMac \
  -releaseOutput "$OUT" -releaseVersion "$VERSION" -logFile "$LOGS/build.log"
CODE=$?
set -e
grep '\[ReleaseBuild\]' "$LOGS/build.log" || true
APP="$OUT/CPSC491-Group8.app"
[[ $CODE -eq 0 && -d "$APP" ]] || fail "the build failed (exit $CODE). See $LOGS/build.log"
PLIST="$APP/Contents/Info.plist"
APP_VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$PLIST" 2>/dev/null || echo unknown)"
EXE_NAME="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$PLIST")"
EXE="$APP/Contents/MacOS/$EXE_NAME"
[[ -x "$EXE" ]] || fail "the app has no executable at $EXE"
echo "Built $APP (version $APP_VERSION, $(du -sh "$APP" | cut -f1))"

# ---------------------------------------------------------------- 4. smoke test
step "4/5 Smoke test: launch the built app"
SMOKE_LOG="$LOGS/smoke-test.log"
"$EXE" -batchmode -nographics -logFile "$SMOKE_LOG" >/dev/null 2>&1 &
PID=$!
sleep "$SMOKE_SECONDS"
if kill -0 "$PID" 2>/dev/null; then
  STILL_RUNNING=1
  kill "$PID" 2>/dev/null || true
  wait "$PID" 2>/dev/null || true
else
  STILL_RUNNING=0
  wait "$PID" 2>/dev/null || true
fi
[[ -f "$SMOKE_LOG" ]] || fail "the app didn't write a log, so it probably didn't start"
ERRORS="$(grep -cE 'Exception|Crash!!!|Segmentation fault' "$SMOKE_LOG" || true)"
if [[ $STILL_RUNNING -eq 0 ]]; then
  fail "the app quit within $SMOKE_SECONDS seconds of launching. See $SMOKE_LOG"
fi
if [[ "$ERRORS" -ne 0 ]]; then
  grep -nE 'Exception|Crash!!!|Segmentation fault' "$SMOKE_LOG" | head -5 >&2
  fail "the app logged $ERRORS error(s) while starting. See $SMOKE_LOG"
fi
# the game logs every scene it loads, so this proves it got past startup to the title screen
grep -q "Loaded scene 'Main Menu'" "$SMOKE_LOG" || fail "the app started but never loaded the Main Menu scene. See $SMOKE_LOG"
SMOKE_SUMMARY="launched headless, loaded the Main Menu scene, ran $SMOKE_SECONDS seconds with no exceptions"
echo "App $SMOKE_SUMMARY"

# ---------------------------------------------------------------- 5. package
step "5/5 Package"
ZIP="$OUT/$NAME-macOS.zip"
# ditto keeps the .app's permissions and symlinks, which zip doesn't
ditto -c -k --keepParent "$APP" "$ZIP"
(cd "$OUT" && shasum -a 256 "$(basename "$ZIP")" > "$(basename "$ZIP").sha256")
SHA="$(cut -d' ' -f1 "$ZIP.sha256")"

cat > "$OUT/RELEASE_NOTES.md" <<EOF
# CPSC 491 Group 8: release $VERSION

| | |
|---|---|
| Version | $VERSION (app reports $APP_VERSION) |
| Commit | $COMMIT on \`$BRANCH\`${DIRTY:+ (with uncommitted changes)} |
| Built | $STARTED to $(stamp) on $(sw_vers -productName) $(sw_vers -productVersion) $(uname -m) |
| Unity | $UNITY_VERSION |
| Tests | $TEST_SUMMARY |
| Smoke test | $SMOKE_SUMMARY |
| Package | \`$(basename "$ZIP")\` ($(du -h "$ZIP" | cut -f1 | tr -d " ")) |
| SHA-256 | \`$SHA\` |

## Install
1. Unzip \`$(basename "$ZIP")\`.
2. Open \`CPSC491-Group8.app\`. The build isn't signed with an Apple developer ID, so the first time macOS
   may block it: right-click the app, choose Open, then Open again.
3. To check the download: \`shasum -a 256 -c $(basename "$ZIP").sha256\`

## Logs
Test results, the build log and the smoke-test log are in \`logs/\`.
EOF

step "Release ready"
echo "$OUT"
ls -1 "$OUT"
