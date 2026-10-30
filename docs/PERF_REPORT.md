# Performance report (P1) — 2026-10-30

Scene: `Main.unity` as of commit after `51c1a3a` (rooftop, canopy, grill, 4 tables, 24 chairs, 6 plant pots, baked lighting, occlusion data). Measured in the **Windows player build** (`Builds/CanteenRush`, Mono, D3D11), not the editor, with `PerfProbe` (`-perfprobe`): 90 warm-up frames, then 300 sampled frames at each of the three standard positions, vsync off.

**Machine:** Intel Iris Xe (integrated GPU), 1600x900 window. This is deliberately a weak GPU, but it is not a Quest 2 and it renders one eye, not two. Read the extrapolation section before quoting any of these as "Quest numbers".

## Results

| Position | Quality level | avg ms | p95 ms | max ms | fps | GPU ms | Visible tris | Visible renderers | Materials | Shaders |
|---|---|---|---|---|---|---|---|---|---|---|
| Counter | PC | 7.61 | 8.41 | 9.77 | 131 | 6.69 | 99,945 | 88 | 60 | 1 |
| Door | PC | 7.65 | 8.71 | 9.59 | 131 | 6.68 | 86,311 | 75 | 48 | 1 |
| Mid-patio | PC | 7.81 | 8.89 | 10.01 | 128 | 6.78 | 67,480 | 49 | 44 | 1 |
| Counter | Mobile (Quest-class asset) | 3.82 | 4.52 | 5.24 | 262 | 2.58 | 99,945 | 88 | 60 | 1 |
| Door | Mobile | 3.62 | 4.25 | 4.79 | 276 | 2.53 | 86,311 | 75 | 48 | 1 |
| Mid-patio | Mobile | 3.47 | 4.14 | 5.22 | 288 | 2.28 | 67,480 | 49 | 44 | 1 |
| Counter | Mobile at 1920x1200 (the largest window the display allowed) | 5.69 | 6.84 | 8.60 | 176 | 4.03 | 99,909 | 85 | 60 | 1 |
| Door | Mobile at 1920x1200 | 5.75 | 8.06 | 10.05 | 174 | 4.08 | 84,715 | 71 | 46 | 1 |
| Mid-patio | Mobile at 1920x1200 | 5.82 | 6.79 | 8.46 | 172 | 4.17 | 58,052 | 46 | 40 | 1 |

"Visible tris/renderers/materials" are what the camera actually drew (`Renderer.isVisible`, after frustum, LOD and occlusion culling), counted by the probe. They are **not** Unity's Stats-window "Tris" and "Batches", which this headless setup cannot read.

Screenshots (player build): `docs/screenshots/player_counter.png`, `player_door.png`, `player_patio.png`. Lighting contrast: `editor_interior_warm.png` (3500 K interior against the cool exterior seen through the grill), `editor_patio_day.png` against `editor_patio_golden_hour.png` (the same view with `SunCycle` at the start and end of the round).

## Against the spec §8 budget

| Metric | Limit / target | Measured | Verdict |
|---|---|---|---|
| Frame time | 13.9 ms / ≤ 11 ms | 3.5-3.8 ms (Mobile level), 7.6-7.8 ms (PC level), one eye, 1.4 MP, integrated GPU | Pass at this resolution; **see the VR extrapolation** |
| Triangles drawn per view | 500k / ≤ 250k | 58k-100k | Pass |
| Triangles in the scene (all LODs at LOD0) | 1.0M / ≤ 600k | 109,262 | Pass |
| Draw calls / SetPass | 250 / ≤ 150 | **Not measured.** Proxy: ≤ 88 visible renderers, ≤ 60 materials, 1 shader; static batching on 106,806 of 109,262 triangles; SRP Batcher on | Expected pass, unverified |
| Realtime lights | 1 | 1 (the sun); 3 baked lights | Pass |
| Realtime shadow cascades | 1 at ≤ 25 m | Mobile asset: 25 m; the **PC asset uses 2 cascades at 40 m** on purpose | Pass for the Quest-class level, deliberate miss on PC |
| Texture memory | 250 MB / ≤ 150 MB | about 2.5 MB of material textures + 1 lightmap of 1024 (a few MB) | Pass |
| Lightmaps | 6 x 1024 / ≤ 3 x 1024 | 1 x 1024 (non-directional) | Pass |
| Active Rigidbodies | 60 / ≤ 40 | 0 in the world (P2's items add to this) | Open for P2 |
| Audio voices | 24 / ≤ 16 | P3 | Not P1 |
| Build size | 500 MB / ≤ 250 MB | 107 MB on disk, 40.9 MB zipped (`Builds/CanteenRush_win64.zip`, git-ignored) | Pass |

## What was done for performance

- Triangle budgets per module in `tools/blender/config.json` (a 176,864-triangle donut became 1,998; a 618,336-triangle fence became 12). Whole built environment 43,864 triangles before instancing.
- One shader (URP/Lit), GPU instancing on every material, SRP Batcher on, 104 shared material assets, static batching on all architecture.
- Chairs (24) and plant pots (6) have LOD1 (300 / 350 triangles) at about 15 m and are culled at about 40 m.
- Canopy, deck and parapet are single boxes; the grill walls are one mesh each (about 1,000 triangles); the campus below is a plane and 46 boxes with no collider, no shadow casting, no lightmap.
- Everything static is baked (Baked Indirect, non-directional, one 1024 lightmap); only the sun is realtime. Backdrops and campus cast no shadows. Reflection probe 128 px, uncompressed.
- Occlusion culling baked over the whole scene. **No with/without comparison was run**, so its benefit is not quantified; with an open-front kiosk it is probably small.
- Textures are all ≤ 1024 px; most surfaces are flat colour.

## VR extrapolation (the part to be honest about)

The GPU cost scales almost linearly with pixels here: 2.3-2.6 ms at 1.44 MP and 4.0-4.2 ms at 2.3 MP on the Mobile level (about 1.7 ms per megapixel). A Quest 2 draws two eyes of 1832x1920, about 7 MP. At 1.7 ms/MP this machine would need roughly **12 ms of GPU time at Quest 2 resolution**, against a 13.9 ms limit. A Quest 2's GPU is not this GPU, so this is an order-of-magnitude warning, not a prediction, and nobody has run the game on a headset. Cheap levers if it matters: render scale 0.8 (36 % fewer pixels), fixed foveated rendering (an Android-build setting), MSAA 2x instead of 4x. The demo plan is the XR Device Simulator on a PC, where the 3.5-7.8 ms figures apply.

## Not measured / limits of this report

- Draw calls and SetPass (above): needs the Stats window or Frame Debugger with a graphics editor session.
- Stereo rendering: measured mono.
- Android / Quest hardware: no Android module installed, no headset.
- Gameplay load: P2's items, customers and UI are not in the scene yet, so CPU time will rise. Re-run `PerfProbe` after integration.
- The build had not been run on a second machine at the time of writing.

## How to reproduce

```
Unity.exe -batchmode -nographics -quit -projectPath unity/CanteenRush -executeMethod CanteenRush.World.Editor.LightingBuilder.BuildWindowsBatch
Builds/CanteenRush/CanteenRush.exe -perfprobe -perfout <dir> -perfquality 1 -screen-width 1600 -screen-height 900 -screen-fullscreen 0     # 1 = PC, 0 = Mobile
```
`perf_results.json` and three screenshots land in `<dir>`. The Mobile level is selectable in standalone builds only because its Standalone exclusion was removed from `QualitySettings.asset` (the default for Windows stays PC).
