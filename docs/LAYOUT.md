# LAYOUT v1 (P1) — world frames, placement, and what is still unverified

Status: **v2, verified in the editor by `SceneVerifier` (round 3)**; v1 was written without opening Unity (no editor license on the author's machine yet, see `docs/DECISIONS.md`). Everything below is derived from measured Blender data (`docs/recon/`, `Assets/Art/build_report.json`, interior renders). Items marked **[verify in editor]** must be checked at M1. Update the changelog at the bottom with every change.

## 0. Setting (decided 2026-10-30)

The whole game is on the **roof deck of the 13th floor of the campus** (about 42 m above the ground). Everything else (canteen kiosk, patio seating, the three former "backdrop" modules) stands on one concrete roof deck with a 1.15 m parapet all round; beyond the parapet the player sees the campus far below (a ground plane plus 46 plain blocks 50 to 215 m away, some taller than the roof). There is no field, no grass at the playing level. `MainSceneBuilder` builds `World/Roof` (deck, four parapet walls with caps, `Campus`), `RoofHeight = 42`.

**The roofed area (user correction, 2026-10-30).** The branching tree shapes from Part 1 are not plants: they are the **columns that hold the roof**. `World/Canopy` is a concrete canopy slab (0.3 m, resting on the column tips, 7.9 m above the deck) carried by 12 columns (four rows of three, two end to end on each long side of the patio, columns 5 m apart), with a **floor-to-roof lattice grill** (`GridGrill`, bars every 0.5 m, 0.045 m thick, one mesh per wall) closing both long sides. Through the grill the player sees the booth pods and the terrace (Parts 2 and 4) standing elsewhere on the deck, then the parapet and the campus below. The front (toward the stair core) and the back of the kiosk stay open to the roof deck.

The deck is as large as the outermost module plus 4 m. The player only ever reaches the canteen and the patio: the invisible `World/Boundary` walls stop them there.

## 1. Frames

- **Canteen frame** = the source `canteen.blend` world frame, metres, Blender axes (X right, Y forward, Z up). `Env_Canteen.fbx` nodes keep their source positions, so dropping the FBX at the Unity origin reproduces the authored layout exactly. All canteen-side markers live in this frame.
- **Patio frame** = the source `Part3_final.blend` frame × 0.1. `Env_Patio.fbx` nodes keep source positions. The patio is shifted as one rigid piece into the canteen frame (§3).
- FBX → Unity axis conversion is done by Unity's importer (the exporter uses forward −Z, up Y). Never type Blender coordinates into Unity by hand; read positions from the imported nodes/markers instead. **[verify in editor]** which Unity direction the canteen front faces (see §2).

## 2. Canteen (`Env_Canteen.fbx`, 11 mesh nodes + 18 marker empties)

Blender-frame facts (metres):

| Thing | Node / marker | Position (centre) | Size |
|---|---|---|---|
| Kiosk shell (roof, side walls, back wall) | `Shell` | (−0.3, 0.0, 1.6) | 7.29 × 7.25 × 3.27 (x −3.94..3.35, y −3.61..3.65) |
| Open side | — | **−X side is open** (interior renders show sky/outside looking −X); the back wall is at x = +3.35 | |
| Display cases | `DisplayCase_A`, `DisplayCase_B` | (−2.5, 0.0, 0.9), (−2.5, −2.2, 0.9) | 1.29 × 2.24 × 1.23 each |
| Counters | `Counter_A`, `Counter_B` | (−2.3, 2.2, 0.7), (−1.1, 2.7, 0.5) | 1.56 × 2.21 × 0.79; 0.82 × 1.25 × 1.02 |
| Plates | `Plates` (13 round plates, z 0.6 and 1.3) | x −2.5, y −2.4..1.6 | 0.49 dia |
| Fridge | `FridgeBody` | (−0.4, −0.5, 1.1) | 0.62 × 0.88 × 2.15 |
| Fridge doors | `Door_Upper`, `Door_Lower` | node pivot **on the hinge axis at (−0.7, −0.9)**, z 1.6 / 0.5; door swings toward −X | upper 0.43 × 0.79 × 1.28; lower 0.14 × 0.89 × 0.85 |
| Water cooler | `WaterCooler` (bottle + pump are ONE mesh, not separable) | (−1.1, 2.6, 1.7) | 0.53 × 0.62 × 1.32 |
| Fixtures | `Fixtures` (light, light box, fan) | (−0.3, −0.8, 2.1), fan (−0.1, −0.3, 2.0) | |

Markers in `Env_Canteen.fbx` (empties; positions are authoritative for spawners): `Spawn_Donut_00..11` (donut centre), `Spawn_Cola_00..01`, `Spawn_Shake_00..01`, `Spawn_Cake_00`, `Spawn_Cup_00` (the source's empty `cup` object — used as the cup dispenser location).

Reading of the geometry: the player works **behind** the display cases (x > −1.85), customers' side is −X. The fridge faces the aisle. **The walkable aisle between the cases and the fridge is about 0.8–1.2 m** — narrow. **[verify in editor]** by standing in it with the rig; if it is too tight, move `Fridge*` +X by up to 0.6 m as a unit (body, both doors, markers) and record it in the changelog.

Seams to fix at M2: the shell's wall panels at y = ±3.35..3.65 are separate thin faces (area 2.0); verify there are no light leaks and no z-fighting between `Shell` and the counters. Counter heights (0.5–0.8 m) are low relative to the display cases (0.9–2.1 m top) — a grabbing height check is part of M1.

## 3. Patio (`Env_Patio.fbx`, 7 mesh nodes + 28 markers; `Prop_TablePatio`, `Prop_ChairBlue`)

- Ground plane 16.34 × 9.26 m; service building (3.49 × 5.93 × 2.31 m) at one end; **the 163 m perforated fence became a plain 16.33 × 0.056 × 1.9 m wall** (12 triangles). Give it an alpha-cutout dot texture or leave it solid. It runs along one long edge.
- 4 `Table_00..03` markers (base centre) and 24 `Chair_00..23` markers (base centre). **Chairs carry no orientation** in the export: an editor script must rotate each chair to face its nearest table (6 chairs per table in the source). Gameplay seats = the 2 chairs per table nearest the walkway (SeatPoints 0..7).
- Placement: the patio is moved rigidly so its ground centre is `PatioDistance = 14 m` from the kiosk centre along the open side (it was 22 m on the field version, which left an empty 16 m apron). The service building at its far end is the **stair and lift core**: customers walk out of it.
- **Seating is re-staggered for the roof** (the source's 2x2 grid is discarded): table `t` sits at 9.5 / 12.3 / 15.2 / 18.0 m from the kiosk centre, alternating 2.4 m to either side of the axis, and each table's six chairs keep the offsets they had from their own table in the source. Seats 0..7 are measured 8.4 to 17.0 m from the player start (spec: first table about 7 m, farthest about 18 m).
- Customer entrance: at the end of the patio opposite the canteen (marker to be created in Unity at M1).

## 4. Backdrop modules (built, placed by `MainSceneBuilder.PlaceBackdrops`)

`Env_PodsBackdrop` (booth pods) stands 6 m past the patio's open long edge (the edge without the fence) and `Env_Terrace` (marble terrace and pergola) 6 m beyond the fence side, both outside the grill. `Env_Skyline` is no longer a backdrop: its `Skyline_Trees` node is the canopy's column set. All rest on the roof deck, cast no shadows, have no colliders and are on the default layer, so they never enter the NavMesh. An invisible box ring (`World/Boundary`, 6 m high) keeps the player inside the canteen + patio rectangle; the verifier prints each module's gap to the walkable deck (4.3 m).

## 5. Player and navigation (built)

Player start: behind `DisplayCase_A`, between the cases and the fridge, looking toward the open side. NavMesh: baked from render meshes on the `Environment` layer only; every patio node except `Ground` is Not Walkable (a `NavMeshModifier`), furniture is on `Stations` so it neither blocks nor carves. Boundary: `World/Boundary` box colliders, which the NavMesh ignores. Plant pots (6 of them, `Environment` layer) do carve small holes next to the fence and the canteen apron. The roof deck itself is walkable (an overlapping Not Walkable deck swallowed the patio floor, which sits 1 to 3 cm above it); the parapet walls and caps are Not Walkable.

## 6. How the scene is made

Run order after any art change: `build_world.py` (Blender) → `MaterialBuilder.BuildBatch` → `MainSceneBuilder.BuildBatch` → `LightingBuilder.BakeBatch` → `LightingBuilder.OcclusionBatch` (without `-quit`) → `SceneVerifier.Run`. A rebuild discards the lightmaps, so always bake after building. Commands are in `docs/LOG_P1.md`.

`Main.unity` is generated by `MainSceneBuilder` (see `docs/UNBLOCK_P2_P3.md`): it measures the imported FBX bounds to find which side of the kiosk is open (the side the display cases face), snaps that to an axis, then puts the patio about 16 m out along it. That keeps the placement independent of the Blender to Unity axis conversion. Constants (`PatioDistance`, `TableRadius`) are at the top of the file.

## 7. Changelog

| Date | Change | Who |
|---|---|---|
| 2026-10-30 | v1 from measured data; no editor yet. | P1 |
| 2026-10-30 | `MainSceneBuilder`, `SeatPoint`, `SpawnPoint`, `NavMeshBootstrap` added; scene generated by script. | P1 |
| 2026-10-30 | Verified in Unity headless: aisle 1.66 m after fridge +0.6 m (Blender frame, doors and markers moved with it); colas moved into the fridge; PatioDistance 22 m; 24/24 chairs face their table; patio structures (all but Ground) marked Not Walkable via NavMeshModifier because the entrance had snapped onto the building roof; entrance on the NavMesh and all 8 seats reachable (paths 10.0-14.5 m from entrance, 15.5-18.9 m from player start). | P1 |
| 2026-10-30 | Backdrops placed (pods, terrace, 2× skyline), 6 plant pots with LODs, chairs get LOD1 (screen heights 0.054 / cull 0.02 ≈ 15 m / 40 m), boundary ring, supplied textures on floors, light layout and probes, mixed-mode bake (Baked Indirect, non-directional), occlusion data, `World/Canteen`, `Patio`, `Backdrop` prefabs saved. | P1 |
| 2026-10-30 | **Rooftop setting.** Field and grass removed; `World/Roof` (deck, parapet, campus 42 m below) added; `PatioDistance` 22 to 14 m; tables re-staggered (9.5 to 18 m, alternating sides); backdrops now sit 6 m from the patio on the deck; all 8 seats reachable, 8.4 to 17.0 m from the player start. | P1 |
| 2026-10-30 | **Canopy and grill.** Tree structures reinterpreted as roof columns; `World/Canopy` (slab + 12 columns: 4 instances of the 3-column module) and two `GridGrill` walls added; skyline backdrop copies removed; deck grows to include them. 8/8 seats still reachable, 1 lightmap (the roof shades the patio), 109k triangles total. | P1 |
