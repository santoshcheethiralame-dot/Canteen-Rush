# DEMO SCRIPT — P1 (World, Lighting, Performance)

P1 owns the **world, the lighting and the performance numbers**. In the 4-minute demo (`docs/DEMO_GUIDE.md`) that is three beats: the opening line, the patio/lighting beat at **3:10** and the performance close at **3:30**. This file gives the exact words for those, a 90-second solo walkthrough for the viva, and the facts you must be able to defend. Numbers are from `docs/PERF_REPORT.md` (Windows player build, Intel Iris Xe, 1600x900, mono).

Controls while you drive (desktop mode): **W A S D** walk, mouse look, **Shift** sprint, **F1** hides the key list, **F5** swaps to the VR simulator.

---

## 1. Your beats inside the team demo

### 0:00 — Opening (30 seconds, you speak while the menu board is on screen)
**Do:** menu board visible, do nothing yet.
**Say:** "Canteen Rush is set on the roof deck of the 13th floor of our campus, about 42 metres up. The whole world was built from seven Blender files of about 7 million triangles, cleaned, rescaled and rebuilt in Unity for a Quest-class budget. I will come back to the lighting and the numbers at the end."

### 2:20 — Door crossing (one line, supports the audio beat)
**Do:** walk through the opening right of the display cases onto the patio.
**Say:** "Inside is lit by baked warm area lights; outside is the cool open sky. Same lightmap, two moods, no extra lights."

### 3:10 — Lighting (about 20 seconds)
**Do:** stand still on the patio, look slowly across the tables, then up at the sky.
**Say:** "Over the three-minute round one thing moves: the sun. Its angle goes from 55 degrees down to 12, its colour from 5600 K to 3800 K, and the ambient colour follows. Everything static is baked into a single 1024 lightmap, so the cost of this whole sunset is one realtime light."
**If asked what is baked:** "Architecture and furniture are baked indirect. Only the sun and moving objects like trays and food are realtime; light probes keep moving objects matching the baked scene."

### 3:30 — Performance close (about 30 seconds)
**Do:** open the Stats overlay or show the perf slide (`docs/PERF_REPORT.md` table).
**Say:** "Measured on an integrated Intel Iris Xe laptop in a Windows build: 58 to 100 thousand visible triangles against a 250 thousand budget, at most 88 visible renderers, and 3.5 to 3.8 milliseconds a frame on the Quest-class quality level, 7.6 to 7.8 on the PC level. The Quest budget is 11 milliseconds. One honest gap: draw calls were not measured directly, only the proxies for them, and we did not have a headset to measure stereo."

---

## 2. Solo walkthrough for the viva (90 seconds, if the examiner says "walk me through your part")

1. **The problem.** "The drive had seven Blender files, around 7 million triangles, inconsistent scales and missing textures. A 177,000-triangle donut and a 618,000-triangle fence would have blown any mobile budget."
2. **The pipeline.** "A Blender script rebuilds each module to a triangle budget set in `tools/blender/config.json`: the donut became about 2,000 triangles, the fence became 12. FBX names are fixed in the Asset Bible because everyone else references them. `tools/run_world_pipeline.sh` rebuilds the whole world from the sources."
3. **Unity side.** "104 shared URP Lit materials, one shader, GPU instancing and the SRP Batcher on, static batching on the architecture, all textures at most 1024 pixels."
4. **Lighting.** "One mixed sun, a few baked lights, one 1024 non-directional lightmap, light probes and one 128-pixel reflection probe. `SunCycle` is about 40 lines and drives the sun and ambient over the round."
5. **Culling.** "Chairs and plant pots have a lower LOD at about 15 m and are culled at about 40 m; occlusion culling is baked."
6. **Results.** "Everything is inside the spec budget except the one deliberate miss: the PC quality level uses two shadow cascades at 40 m. The Quest-class level uses one at 25 m."

---

## 3. Questions P1 should expect, with short answers

| Question | Answer |
|---|---|
| Why bake lighting? | Static architecture never changes, so baked GI gives soft indirect light for free at runtime. Only the sun and dynamic objects are realtime, which is what a mobile GPU can afford. |
| What is the difference between baked and realtime? | Baked light is computed once in the editor into lightmaps and light probes. Realtime is computed every frame and costs GPU time per light and per shadow-casting object. |
| How do moving objects (trays, food) get lit? | Light probes sample the baked lighting at their position, so a donut matches the room it is in. |
| Why one lightmap of 1024? | The budget allows three; the scene is small and mostly flat colour, so one is enough and saves memory and bandwidth. |
| How did you cut triangles? | Per-module triangle budgets in Blender (decimation), replacing detail with boxes where the player never looks closely, LODs on repeated objects, and no colliders or shadows on far scenery. |
| What is static batching and SRP Batcher? | Static batching merges non-moving meshes into big batches; the SRP Batcher cuts the CPU cost of switching materials that share a shader. Both reduce draw-call overhead. |
| Why does 7.7 ms on a laptop not prove it runs on a Quest? | The Quest draws two eyes at about 7 megapixels. At about 1.7 ms per megapixel measured here, that is roughly 12 ms of GPU time on this laptop, close to the limit. A Quest GPU is different, so this is a warning, not a prediction. |
| What did you not measure? | Draw calls and SetPass (needs a graphics editor session), stereo rendering, real headset hardware, and the effect of occlusion culling on its own. |
| How would you improve it? | Re-run the probe with full gameplay load, measure draw calls in the Frame Debugger, test in stereo on a headset, and compare with and without occlusion culling. |
| Why is the game on a roof? | The assigned floor was an open canteen kiosk with a patio; a roof deck with a canopy, parapet and a campus far below gives an enclosed, bounded play space and keeps the far backdrop cheap (a plane and boxes with no colliders or shadows). |

---

## 4. Numbers to memorise
- **Triangles:** 58k to 100k visible per view (budget 250k); 109k in the whole scene (budget 600k).
- **Frame time:** 3.5 to 3.8 ms Quest-class level, 7.6 to 7.8 ms PC level (Iris Xe, 1600x900, mono). Budget 11 ms.
- **Draw calls:** not measured; proxies are at most 88 visible renderers, at most 60 materials, 1 shader.
- **Lighting:** 1 realtime light (the sun), 1 lightmap of 1024, sun 55 to 12 degrees, 5600 K to 3800 K.
- **Size:** 107 MB build on disk, 40.9 MB zipped.

## 5. Before you present
- Rebuild or pull the latest `Builds/CanteenRush/` so desktop mode is in the exe.
- Rehearse the walk from the counter through the door to a mid-patio spot, so the lighting beat starts from a good view of the tables and the sky.
- Have `docs/PERF_REPORT.md` and `docs/screenshots/editor_patio_day.png` and `editor_patio_golden_hour.png` open for the day-to-golden-hour comparison.
