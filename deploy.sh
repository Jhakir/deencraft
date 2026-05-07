#!/usr/bin/env bash
# deploy.sh — Build and deploy Deencraft to Firebase Hosting
# Usage:
#   ./deploy.sh               # build + deploy
#   ./deploy.sh --deploy-only # skip Unity build, just firebase deploy
# Requires: Unity (UNITY_PATH), Firebase CLI (npm i -g firebase-tools)
set -euo pipefail

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_OUTPUT="${PROJECT_DIR}/webgl-build/Build"
LOG_FILE="${PROJECT_DIR}/Logs/WebGLBuild.log"

echo "╔══════════════════════════════════════╗"
echo "║      Deencraft — Deploy Script       ║"
echo "╚══════════════════════════════════════╝"

# ── Step 1: Unity WebGL Build (skip with --deploy-only) ──────────────────────
if [[ "${1:-}" != "--deploy-only" ]]; then
  echo ""
  echo "▸ Building WebGL with Unity …"
  mkdir -p "$(dirname "$LOG_FILE")"

  "$UNITY_PATH"                                   \
    -batchmode                                    \
    -quit                                         \
    -projectPath "$PROJECT_DIR"                   \
    -executeMethod DeenCraft.Editor.WebGLBuildScript.Build \
    -logFile "$LOG_FILE"                          \
    2>&1

  if [ ! -d "$BUILD_OUTPUT" ]; then
    echo "✗ Build output not found: $BUILD_OUTPUT"
    echo "  Check Logs/WebGLBuild.log for errors."
    exit 1
  fi
  echo "✓ Build complete → webgl-build/Build"
fi

# ── Step 2: Firebase deploy ───────────────────────────────────────────────────
echo ""
echo "▸ Deploying to Firebase Hosting …"
firebase deploy --only hosting --project deencraft-app

echo ""
echo "✓ Deployed! Visit https://deencraft-app.web.app"
