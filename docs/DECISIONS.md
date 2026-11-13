# Decisions log

Append-only. Newest at the bottom. Format: `## YYYY-MM-DD — title`, then decision, reason, who agreed.

## 2026-10-30 — Initial decisions D1–D10
See `docs/00_MASTER_SPEC.md` §2. Summary: VR (Quest-class budget) shown via XR Device Simulator; Unity 6000.0.43f1 + URP + XRI 3.x; concept "Canteen Rush"; world = canteen + Part 3 patio, other parts backdrop; menu = Donut/Cola/Shake/Cake/Water (the source "Dough" objects are donuts); continuous move + snap turn; primitive customers; single scene with single-writer ownership; washroom out of gameplay; tunables in ScriptableObjects.

## 2026-10-30 — Corrections found during recon
- Source `Dough` meshes are donuts, not pizzas (close-up render `docs/recon/previews/canteen_prop_Dough.png`).
- Part 3 has 24 chairs + 4 tables (not 28 chairs).
- The canteen `cup` object has zero triangles; a cup must be modelled.

## Open (needs the team lead)
1. Viva / submission date. 2. Does anyone own a Quest headset? 3. GPU of the viva laptop. 4. Other teams' concepts. 5. Android build module installed?

## 2026-10-30 — M0 started by P1; Unity license blocker
- Project skeleton created at `unity/CanteenRush` from the Universal 3D template; packages pinned in `docs/VERSIONS.md`.
- Headless open failed: "No valid Unity Editor license found" (no ULF, not signed in to Unity Hub on this machine). Fix: open Unity Hub → sign in → activate a free Personal license (interactive; cannot be scripted). Until then nothing can be compiled, imported or baked here; Blender-side pipeline work is unaffected.
- Layer 11 is named `WorldUI` (not `UI`) because Unity's built-in layer 5 is already `UI`. Contracts updated.
- Quality levels: the template's two levels are kept — Standalone → `PC_RPAsset`, Android/iOS → `Mobile_RPAsset` (the Quest-class budget).

## 2026-10-30 — P1 round 4: backdrops, materials, lighting
- **Skyline scale ×2.0.** Part 1's chairs imply ×2.3 and its trees/pots imply ×1.0. At ×1.0 the trees are 3.8 m tall and vanish behind the patio fence; ×2.0 gives 7.6 m silhouettes that read from the counter. They are only backdrop, so nothing depends on the exact value.
- **Washroom cut.** `Env_Washroom` was optional (spec §5, "out of gameplay"). Nothing in the player's reach would show it, so it costs triangles for no screen time. The cut-list contract allows it.
- **Plant pot ×0.2 = 0.94 m**, not the 1.2–2.0 m the first guess assumed; accepted because pot + plant at ~1 m suits a patio.
- **Mixed lighting mode: Baked Indirect, non-directional lightmaps, CPU Progressive.** Shadowmask would add a second per-scene texture set and only improves baked shadows beyond the 25 m realtime shadow distance, where the patio is barely visible. CPU lightmapper because it bakes on any machine, including a laptop without a discrete GPU (the whole bake is under 1 minute here). Two 1024² lightmaps against a budget of three.
- **Lightmap UVs only for canteen and patio.** Backdrops have no second UV set and are lit by light probes.
- **Materials per Blender material, not an atlas.** 104 URP/Lit assets, one shader. With the SRP Batcher the cost is per-shader-variant, not per-material, so an atlas buys nothing measurable here and would have cost a day of baking; the report's "materials" column makes this checkable. The shake and cake stay flat-coloured (label textures were never supplied).
- **SunCycle is fed by gameplay, not by Cr.World.** `Cr.World` does not reference `Cr.Core` (it must compile before P2 pushes). P2 subscribes: `GameEvents.OnRoundTimeChanged += t => sunCycle.SetRemaining(t, config.RoundSeconds);`.
- **Occlusion culling:** baked over the whole scene with default cell sizes; the canteen interior is a single open-front kiosk, so the gain is small and is reported honestly in `docs/PERF_REPORT.md`.

## 2026-10-30 — Setting: roof of the 13th floor (user decision)
- The user corrected the setting: the canteen is on the 13th-floor roof of the campus, so there must be a **roof and a parapet, not an open field**. Built as `World/Roof`: one deck under everything, parapet 1.15 m with caps, the campus 42 m below as a ground plane plus 46 plain blocks (seeded, so every build is identical). `RoofHeight = 42` is 13 storeys of 3.2 m.
- **Seating rearranged**: zig-zag, 9.5 to 18 m from the kiosk, alternating sides; `PatioDistance` 22 to 14 m. The old layout left a 16 m empty apron that only made sense on open ground.
- The service building on the patio is now the stair and lift core. The pods, the terrace and the trees are other corners of the roof, 6 m off the patio (the earlier 20 m clearance was for a field backdrop and is dropped). The player cannot reach them: `World/Boundary` walls.
- The roof deck is **walkable** in the NavMesh. A Not Walkable deck under the patio floor (1 to 3 cm apart) removed the patio from the NavMesh in the bake, and no seat was reachable; the walls and caps carry the Not Walkable modifier instead.
- Not done, would need the user: any lift or stair prop beyond the existing building, a glass railing instead of the solid parapet, a real campus model below.

## 2026-10-30 — Roofed area: columns and grill (user decision)
- User: the tree-like structures **hold the roof**, and the sides need a **grid-type grill from the floor to the roof**. Built as `World/Canopy`: a canopy slab on the column tips and two `GridGrill` walls at 8.3 m either side of the axis. The Part 1 "pillar" meshes are used as the columns (scale 2.0 gives 7.6 m), bark-coloured `M_Column`, with mesh colliders on the Environment layer and Not Walkable.
- `GridGrill` is a component, not a mesh asset: it rebuilds one mesh (about 1,000 triangles per wall) on enable (`ExecuteAlways`), so there is no binary asset to merge and the pitch/bar size are two fields. Cost: one draw call and real-time lattice shadows on the deck.
- The canopy puts the whole patio in realtime shade of the sun, so the day/golden-hour shift now shows mostly on the grill shadows, the sky seen through it and the ambient. Mixed mode stays Baked Indirect; the shadow distance (25 m on the Quest asset, 40 m on PC) covers every playing position.
- Skyline backdrop copies and the tree-bark override were removed. Lightmaps dropped to 1 (the roof is large but flat).

## 2026-11-13 — P3: audio, haptics, UI shell, QA (P3; P2 to approve the event change in the PR)
- **Menu/Results reach gameplay through three new events, not a shared ScriptableObject.** The role brief suggested a `PlayerSettings` asset that P2's rig reads, but Play, Replay and Menu also have to start or stop the round, and P3's assemblies may not reference `Cr.Gameplay`. Added to `GameEvents` (additive, contracts §4.1/§4.2 updated): `PlayRequested`, `MenuRequested`, `ComfortChanged(seated, teleport)`. P2's `GameManager` subscribes `Play`/`Menu`, `ComfortSettings` subscribes `SetSeated`/`SetTeleport`: one line each in P2's files, made in the same PR for P2 to review. Settings are not persisted (cut list §17).
- **Spatializer plugin: none (Unity's built-in panning).** The Meta XR Audio SDK needs the Meta scoped registry and a manifest change (P1 owns packages), nobody has a headset to hear the difference, and the spec allows shipping without one if it costs more than half a day. Every 3D source still has `spatialBlend 1` and `spatialize = true`, so adding a plugin later is a Project Settings change only.
- **Audio clips are procedurally synthesised** by `tools/p3/gen_p3_assets.py` (CC0, team-authored), not downloaded. Downloads were not possible from P3's session without separate approval, and synthesis gives a license-clean, reproducible set with one or more clips for every cue. Swapping in recorded CC0 clips later only means replacing WAV files with the same names. Logged in `Assets/Audio/CREDITS.md`.
- **`amb_patio_birds` is a rooftop wind bed with sparse birds.** P1's note: the patio is on a 13th-floor roof, so wind fits better than ground-level birds. The cue ID stays as in the spec table.
- **`item_drop_glass`** is a separate cue set for the Shake's "glass break variant" of `item_drop` (spec §10), so thud and glass variants never mix.
- **Haptic hands the events do not carry:** "last holder" = hand of the latest `ItemGrabbed/Released`; `PumpPress` goes to the hand not holding the cup; `UiTick` to the right hand. `DoorLatch`/`DoorStop` use the last holder because `FridgeDoorMoved` has no hand parameter (an additive `Hand` on the door events would fix this; not requested yet).
- **Audio zones are polled, not trigger-based.** `AudioService` checks every 0.1 s which `AudioZoneTrigger` boxes contain the AudioListener (Indoor wins). The XR rig's camera has no collider/Rigidbody, so `OnTriggerEnter` would not fire. `AudioSceneInstaller` adds the component to P1's `AudioZone_Indoor/Outdoor` at runtime, so `Main.unity` is not edited.
- **UI text uses the built-in `LegacyRuntime` font via `UnityEngine.UI.Text`,** the same as P2's order UI (one font across the game), instead of TextMeshPro. TMP Essential Resources are not imported in the project. Boards are built in code (`UiFactory`) so the prefabs stay one-object and diff-friendly.
- **Runtime fallback install.** `Main.unity` was generated before P3's prefabs existed, so its `_Audio`, `_Haptics` and `UI` roots are empty. `UiShell` fills an empty root after scene load from `Prefabs/UI/Resources/P3/` (copies made by the same generator). When P1 next runs `MainSceneBuilder`, `Audio.prefab`/`Haptics.prefab` are placed by the builder and the fallback does nothing for those. **Ask P1:** add `AddIfExists("Assets/Prefabs/UI/UIShell.prefab", ui.transform)` to the builder.
- **Not verified in Unity.** P3's machine has no working editor: the pinned 6000.0.43f1 is not installed, the installed 6000.3.26f1 is missing its Package Manager, and the disk was too full to install the pinned version. All C# was type-checked with Roslyn against the Unity 6 engine and editor DLLs (package types stubbed), and each P3 assembly compiles with only the references its asmdef allows. Assets (prefabs, mixer, scene, data) were written as YAML by the generator script. **First person to open the project:** let it import, run the PlayMode tests (Window > General > Test Runner > PlayMode), open `Sandbox_P3`, and report anything red to P3.
