# P1 log (World & Pipeline)

Newest first. One line per decision or measured fact; the reasoning is in `docs/DECISIONS.md`, the layout in `docs/LAYOUT.md`, the numbers in `docs/PERF_REPORT.md`.

## 2026-10-30 (round 7: measured and shipped)
- Windows player built (107 MB, 40.9 MB zipped) and measured with `PerfProbe` on an Iris Xe laptop: 7.6-7.8 ms (PC level), 3.5-3.8 ms (Quest-class level) at 1600x900; 58k-100k visible triangles. Full table, caveats and the VR extrapolation (about 12 ms GPU at Quest 2 pixel count on this GPU) in `docs/PERF_REPORT.md`.
- Probe fixes: marker names are case-sensitive; the Quest-class quality level was excluded from Standalone builds, so it silently fell back to PC. Removed that exclusion in `QualitySettings.asset` (Windows default stays PC).
- Remaining for P1: swap `Visual` children once P2 pushes prefabs; re-measure after gameplay integration; optional polish listed below.

## 2026-10-30 (round 6: canopy and grill)
- User correction: the trees hold the roof; add a floor-to-roof grid grill on the sides. Built `World/Canopy` (slab, 12 columns = 4 instances of the 3-column `Env_Skyline`) and `GridGrill` (new Cr.World component). Verified headless: 222 NavMesh triangles, entrance on the NavMesh, 8/8 seats reachable (8.0-18.1 m from the entrance, 8.4-17.0 m from the player start), 24/24 chairs facing tables, 1 lightmap of 1024, 109k triangles.

## 2026-10-30 (round 5: rooftop)
- User correction: the game is on the 13th-floor roof of the campus, not a field. Replaced the grass field with a roof deck, parapet and a campus 42 m below; tables re-staggered; patio pulled in to 14 m. Verified: 98 NavMesh triangles, entrance on the NavMesh, 8/8 seats reachable, walks 8.4-17.0 m, 24/24 chairs facing tables, 2 lightmaps of 1024. Total scene triangles 98k (all LODs at full).
- Bug on the way: Not Walkable roof deck removed the patio floor from the NavMesh (see `docs/DECISIONS.md`).

## 2026-10-30 (round 4: finishing P1)
- Built the three backdrop modules (`Env_PodsBackdrop` 5.3k tris, `Env_Terrace` 12.6k, `Env_Skyline` 7.5k), `Prop_PlantPot` (+LOD1), `Prop_ChairBlue_LOD1`. All 18 Blender modules exit 0 with budgets and dimensions passing. Two source files were saved mid-edit (Part 2's `Cube` in Edit Mode) and one removed proxy object left a `None` in the view layer; both handled in `build_world.py`.
- `MaterialBuilder`: 104 URP/Lit materials from the Blender dumps, remapped onto every FBX. Supplied textures downscaled to <= 1024 px (`tools/make_textures.py`), applied to floors, patio ground, grass and the canteen marble. `TextureImportDefaults` pins import settings.
- `MainSceneBuilder` now also places backdrops (first placed 20 m out for a field, then pulled to 6 m for the roof), 6 plant pots with LODs, chair LODs, the boundary ring, per-slab textured floors, lights, probes, reflection probe, `SunCycle`, perf markers, and saves `World/Canteen|Patio|Backdrop` prefabs.
- Lighting: Baked Indirect, non-directional, CPU progressive lightmapper, 2 lightmaps of 1024, ~45 s bake. **Bug found and fixed:** the baked reflection cubemaps were garbage (every lit surface rendered magenta) because BC6H reflection compression failed on the dev machine; set `LightmapEditorSettings.reflectionCubemapCompression = Uncompressed` and bake with a graphics device (not `-nographics`).
- Occlusion culling baked (`OcclusionBatch`, run without `-quit`: it needs editor ticks).
- Added `tools/run_world_pipeline.sh` (whole chain), `tools/gen_pipeline_status.py`, `PerfProbe` (player-build measurement), Windows build step. Built-in module `screencapture` enabled in the manifest.
- `/Builds/` and `/perf/` added to `.gitignore`.

## 2026-10-30 (rounds 1-3)
- Round 3: patio structures marked Not Walkable (the customer entrance had snapped onto the service building's roof, an island); all 8 seats reachable, paths 9.9-14.3 m from the entrance.
- Round 2: fridge slid +0.6 m (aisle 1.66 m), colas into the fridge, patio 22 m out.
- Round 1: Unity project created and licensed; Blender pipeline and first modules; scene generator; verifier; contracts and handoff docs.

## Commands (all from the repo root)
```
blender -b --factory-startup --disable-autoexec --python tools/blender/build_world.py -- --source source --out unity/CanteenRush/Assets/Art
python tools/gen_pipeline_status.py
tools/run_world_pipeline.sh                      # materials build bake occlusion verify stats shots
python tools/unity_compile_check.py --editor unity/CanteenRush/Assets/Scripts/World/Editor/*.cs unity/CanteenRush/Assets/Scripts/World/*.cs tools/stubs/NavMeshStub.cs
```
Windows player: `Unity.exe -batchmode -nographics -quit -projectPath unity/CanteenRush -executeMethod CanteenRush.World.Editor.LightingBuilder.BuildWindowsBatch` (about 25 minutes, nearly all of it URP shader variants; output in `Builds/CanteenRush`, git-ignored).

## Open for later
- P2 has not pushed `Cr.Core` or gameplay prefabs, so step 11 of the P1 task list (swap `Visual` children) cannot happen yet. When they land: swap only `Visual`, tell P2 if a mesh's bounds differ by more than 10 %.
- Optional polish: alpha-cutout dot texture for the fence, label textures for the shake and cake, a second UV-correct tile for the apron.
