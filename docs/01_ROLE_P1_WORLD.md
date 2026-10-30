# ROLE P1 — WORLD & PIPELINE (1.0 person, ≈ 11 person-days)

You are the AI assistant working for **Person 1**. Read, in order: `docs/00_MASTER_SPEC.md` (all), `docs/04_CONTRACTS.md` (§1–3, 7–9), `docs/05_ASSET_BIBLE.md` (all). Then this file. You do not have the conversation that produced these documents; do not assume anything that is not written. If something is ambiguous, ask your human, then record the answer in `docs/DECISIONS.md`.

## Paste this at the start of your session

> You are the AI pair for Person 1 on the Canteen Rush VR mini-project (Unity 6000.0.43f1, URP, XR Interaction Toolkit). Read docs/00_MASTER_SPEC.md, docs/04_CONTRACTS.md, docs/05_ASSET_BIBLE.md and docs/01_ROLE_P1_WORLD.md before doing anything. You own only the paths listed under "Owns" in docs/01_ROLE_P1_WORLD.md. Never edit other people's files. Work in small tasks; after each, state what you verified. Source .blend files are untrusted: always run Blender with --disable-autoexec. Do not commit .blend files or the source zip.

## Mission

Turn seven messy Blender files (≈ 7M triangles, inconsistent scales, missing textures) into a clean, correctly scaled, lit and fast Unity world, and keep the project performing within the budget in the master spec §8. Provide the layout everyone else builds on.

## Owns (only you edit)

`tools/blender/**`, `unity/CanteenRush/Assets/Art/**`, `Assets/Scenes/Main.unity`, `Assets/Lighting/**`, `Assets/Settings/**`, `Assets/Scripts/World/**` (+ `Cr.World.asmdef`), `Assets/Prefabs/World/**`, `Packages/manifest.json`, `ProjectSettings/**`, `docs/LAYOUT.md`, `docs/VERSIONS.md`, `docs/PERF_REPORT.md`, `docs/LOG_P1.md`, builds.

You consume: `Cr.Core` (P2), gameplay prefabs (P2), audio/haptics prefabs (P3).
You provide: art modules, `Main.unity` assembly, layout doc, NavMesh, SeatPoints, lighting, SunCycle, performance report, builds.

## Task list with acceptance criteria

### M0 — Bootstrap (do first; blocks the others) — ≈ 1.5 days

1. **Create the Unity project** at `unity/CanteenRush` with Unity Hub (Editor 6000.0.43f1; if it is missing, install it). Template: "Universal 3D". Verify the editor opens and the template scene runs.
   *Done when:* project opens with 0 errors; folder structure from contracts §1 created (empty folders need `.gitkeep`).
2. **Packages** (Package Manager; record exact versions in `docs/VERSIONS.md`): XR Interaction Toolkit 3.x (latest compatible with Unity 6000.0), XR Plugin Management, OpenXR Plugin, Input System, AI Navigation, TextMeshPro (part of uGUI in Unity 6), Test Framework, Mathematics. Import XRI samples: **Starter Assets** and **XR Device Simulator**. Enable OpenXR in XR Plug-in Management for PC (Windows) and, if the Android module is installed, Android. Add the Oculus Touch / generic controller profile and Meta Quest feature group if offered.
   *Done when:* a scene with the Starter Assets XR Origin + a grabbable cube runs in Play mode with the XR Device Simulator and you can grab the cube with simulated controllers.
3. **Project settings:** Version Control = Visible Meta Files; Asset Serialization = Force Text; Color Space Linear; Physics fixed timestep 0.013889; layers/tags from contracts §3; set URP asset: Forward, MSAA 4×, HDR off, SRP Batcher on, shadows: 1 cascade, distance 25, resolution 1024, soft shadows off; depth/opaque texture off; Quality levels reduced to 2 ("Quest", "PC").
4. **Git:** ensure `.gitignore`/`.gitattributes` at repo root work (they exist); `git lfs install`; commit the empty project skeleton in a PR to `main`; add collaborators (your human must do the GitHub invites). Confirm LFS pointers are created for a test PNG (`git lfs ls-files`).
5. **Notify** P2 and P3 (via issue/PR description) that M0 is done; P2 pushes `Cr.Core` the same day.

### M1 — Greybox world — ≈ 1.5 days

6. **Decide the layout** and publish `docs/LAYOUT.md` per contracts §8. Proposed default (adjust only with reasons): origin at the canteen shell centre; canteen shell 7.3 × 7.25 m, door on the +X wall; counter run along the −X side (display cases at x ≈ −2.5 as in the source); player start at (0.5, 0, 0) facing −X (toward the counter, display cases and fridge; the door is behind the player, which suits the "turn around and carry the tray out" rhythm); the patio ground (16.3 × 9.3 m) centred ≈ 14 m from the door along +X, 4 tables in a 2×2 grid, customer entrance at the far patio edge; first table ≈ 7 m from the door, farthest ≈ 18 m from the counter (a 12–15 s walk at 1.8 m/s, which is the intended tension).
7. **Greybox in `Main.unity`:** boxes at correct scale for shell, counters, display cases, fridge, cooler, patio ground, 4 tables, 24 chairs (cubes), 8 `Marker_SeatPoint`s, `Marker_CustomerEntrance`; static flags set; **bake a NavMesh** (AI Navigation surface) over the patio floor and doorway; verify a NavMeshAgent can walk from the entrance to every seat.
8. **Performance baseline** on the greybox (Stats window screenshot in `docs/PERF_REPORT.md`) — proves the project settings are sane before art arrives.
   *Done when:* P2 can place their station prefabs into your greybox and run a full round.

### M2 — Real art in — ≈ 4 days (the long pole)

9. **Blender tooling:** write `tools/blender/config.json` and `build_world.py` per Asset Bible §9. Start with the biggest wins in this order and commit each module as soon as it passes: (a) `Item_Donut`, `Item_Cake`, `Item_Shake`, `Item_Cola`, `Item_Cup` (+ atlas); (b) `Env_CanteenShell` + fridge doors + cooler; (c) `Prop_ChairBlue`, `Prop_TablePatio`, `Env_Patio` (fence replaced by an alpha quad); (d) `Prop_Tray`, `Prop_TrayRack`, `Prop_CupDispenser`; (e) backdrops `Env_Terrace`, `Env_PodsBackdrop`, `Env_Skyline`; (f) optional washroom.
   *Done when per module:* script exit 0; triangle and dimension checks pass; module imports in Unity at the right size next to a 1.8 m reference.
10. **Materials:** build `M_*` URP/Lit materials (flat colours from Blender base colours; supplied textures where listed in Asset Bible §4); atlas for small props; glass only on display case fronts. Extract the packed `Map #4` images, downscale to 1024.
11. **Swap visuals:** replace the placeholder `Visual` child in every P2 gameplay prefab with the real mesh (open the prefab in Prefab mode, swap only `Visual`). Notify P2 if a real mesh's bounds differ from the placeholder by > 10% so the colliders are retuned (P2's change, not yours).
12. **Colliders for static world:** box colliders on counters, cases, fridge body, building walls; a single MeshCollider (non-convex, static) for the canteen shell only if boxes cannot represent the door opening; floor colliders; invisible blocker volumes at the playable boundary.
13. **Assemble prefabs** `Canteen.prefab`, `Patio.prefab`, `Backdrop.prefab` under `Assets/Prefabs/World/`; `Main.unity` references them; use `Chair` instancing via prefab with GPU instancing or `MeshRenderer` with shared material (no unique materials).
   *Done when:* Main.unity looks like a believable canteen + patio, nothing floats/clips, heights are right, `docs/LAYOUT.md` changelog updated.

### M3 — Lighting — ≈ 1.5 days

14. **Lighting per master spec §9:** Lighting Settings asset (Progressive GPU/CPU, mixed mode Baked Indirect or Shadowmask — pick and document), light placement, light probes (counter, fridge, door, patio grid), canteen reflection probe, emissive materials, bake. Keep lightmaps ≤ 3 × 1024 (adjust Lightmap Scale in Global Illumination per renderer).
15. **`SunCycle` script** (Cr.World): inputs `GameEvents.OnRoundTimeChanged` (seconds remaining) and `GameConfig.RoundSeconds`; drives sun rotation (55° → 12°), colour temperature (5600 K → 3800 K via `Light.colorTemperature` with `useColorTemperature`), ambient gradient and fog colour. No allocations per frame; update at ≤ 10 Hz is fine (smooth with lerp).
16. **Audio zone triggers:** place two trigger volumes at the canteen door calling P3's `AudioZoneTrigger` (contracts §6).
   *Done when:* screenshots at the counter, at the door and mid-patio show clearly warm-interior vs cool-exterior contrast; SunCycle visibly shifts light over a scripted 180 s.

### M4 — Optimisation — ≈ 2 days

17. **Measure** at the three standard positions (counter, door, mid-patio) with the Stats window, Frame Debugger and Profiler; write numbers into `docs/PERF_REPORT.md` against the master spec §8 table.
18. **Optimise to pass:** static + dynamic batching review, GPU instancing on chairs/tables, LOD Groups on chairs/plants/backdrop (LOD1 at 15 m, cull at 40 m), occlusion culling bake (canteen interior), texture compression/size audit, remove unused materials, disable realtime shadows on small props, reduce lightmap resolutions, remove overdraw hotspots (alpha fence is cheap if small, avoid large transparent surfaces), shader stripping.
19. **Build:** Windows standalone build (IL2CPP optional; Mono is fine) → `Builds/` (git-ignored) and a zipped copy for the viva; if the Android module is available and a headset exists, an APK (stretch).
   *Done when:* spec §8 targets met or each miss is documented with a reason and a mitigation; build runs on a second machine.

### M5/M6 — Support

20. Fix integration bugs in the world (floating items, bad colliders, NavMesh holes, lightmap seams). Keep `docs/LOG_P1.md` current. Rehearse the demo; be ready to explain: scale normalisation, decimation choices, lightmapping vs realtime, batching, LOD, occlusion culling, why Quest-class budgets.

## Constraints and traps

- Never edit gameplay scripts or prefab roots. Only `Visual` children and world prefabs.
- Do not upgrade packages or change layers/collision matrix after M0 without a PR and notice to all.
- Blender 5.2 Python API: `bpy.data.objects.remove(obj)` has no `remove_data` argument. `render.render` with Workbench works headless; EEVEE may not.
- Keep FBX names exactly as in Asset Bible §7 — other people reference them.
- Textures: never import a source image bigger than 1024 px without a stated reason. Remove unused imports before committing (LFS quota).
- Do not "fix" the source `.blend` files in place; build outputs from them.
- If a module's budget cannot be met, cut detail, not features, and note it.

## What you hand to others (checklist)

- To P2 (M1): `Main.unity` greybox + `docs/LAYOUT.md` + NavMesh + 8 seat points. (M2): real meshes in prefabs.
- To P3 (M1): audio zone trigger positions; surface tags (`SurfaceTile` on canteen floor, `SurfaceStone` on patio) so footsteps can pick surfaces.
- To the team (M4): perf report; (M5) Windows build + zip.

## Definition of done for you

Main.unity loads clean (0 errors, 0 warnings from your content), runs ≥ 72 FPS equivalent on the dev PC in the Stats window at the three positions with triangle/draw-call numbers inside the table, lighting baked and committed, `docs/LAYOUT.md`, `docs/VERSIONS.md`, `docs/PERF_REPORT.md`, `docs/LOG_P1.md` written, Windows build zipped.
