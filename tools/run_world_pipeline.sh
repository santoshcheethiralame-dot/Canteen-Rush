#!/usr/bin/env bash
# Rebuilds the whole Unity world headless: materials -> scene -> bake -> occlusion -> checks -> screenshots.
# Usage: tools/run_world_pipeline.sh [step ...]   (default: all; steps: materials build bake occlusion verify stats shots)
# Needs the Unity license activated. The bake and shots steps run WITH a graphics device on purpose: reflection probes baked
# under -nographics came out as garbage on the dev machine. Logs go to $LOGDIR (default: ./Builds/logs).
set -u
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.0.43f1/Editor/Unity.exe}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJ="$(cd "$ROOT/unity/CanteenRush" && pwd -W 2>/dev/null || pwd)"
LOGDIR="${LOGDIR:-$ROOT/Builds/logs}"
mkdir -p "$LOGDIR"
NS=CanteenRush.World.Editor

run() {  # name method flags...
  local name="$1" method="$2"; shift 2
  echo "== $name"
  "$UNITY" -batchmode "$@" -projectPath "$PROJ" -executeMethod "$NS.$method" -logFile "$LOGDIR/$name.log"
  echo "   exit $?  (log: $LOGDIR/$name.log)"
}

steps=("$@"); [ ${#steps[@]} -eq 0 ] && steps=(materials build bake occlusion verify stats shots)
for s in "${steps[@]}"; do
  case "$s" in
    materials)  run materials MaterialBuilder.BuildBatch -nographics -quit ;;
    build)      run build MainSceneBuilder.BuildBatch -nographics -quit ;;
    bake)       run bake LightingBuilder.BakeBatch -quit ;;
    occlusion)  timeout 900 "$UNITY" -batchmode -nographics -projectPath "$PROJ" -executeMethod "$NS.LightingBuilder.OcclusionBatch" -logFile "$LOGDIR/occlusion.log"; echo "== occlusion exit $?" ;;  # no -quit: needs editor ticks
    verify)     run verify SceneVerifier.Run -nographics -quit ;;
    stats)      run stats LightingBuilder.StatsBatch -nographics -quit ;;
    shots)      run shots LightingBuilder.ScreenshotBatch -quit ;;
    *) echo "unknown step $s"; exit 2 ;;
  esac
done
