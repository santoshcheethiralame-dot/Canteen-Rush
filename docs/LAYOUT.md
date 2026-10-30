# LAYOUT v1 (P1) — world frames, placement, and what is still unverified

Status: **v2, verified in the editor by `SceneVerifier` (round 3)**; v1 was written without opening Unity (no editor license on the author's machine yet, see `docs/DECISIONS.md`). Everything below is derived from measured Blender data (`docs/recon/`, `Assets/Art/build_report.json`, interior renders). Items marked **[verify in editor]** must be checked at M1. Update the changelog at the bottom with every change.

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
- Placement into the canteen frame: move the whole patio so its ground centre sits at roughly (−16, 0) in the canteen frame, i.e. a rigid translation of about (−23.8, −4.6, 0) in Blender axes from the patio's source frame. This puts the near edge ≈ 8 m from the counters and the far tables ≈ 20 m away (≈ 11 s walk at 1.8 m/s). **[verify in editor]** feel; the patio can slide ±4 m along the canteen's X axis without touching anything else.
- Customer entrance: at the end of the patio opposite the canteen (marker to be created in Unity at M1).

## 4. Backdrop modules (not built yet)

`Env_PodsBackdrop` (Part 2, ×0.2), `Env_Terrace` (Part 4, ×0.2), `Env_Skyline` (Part 1, scale unresolved). Positions TBD at M2: behind and to the sides of the patio, ≥ 20 m from any playable position.

## 5. Player and navigation (to create in Unity at M1)

Player start: behind `DisplayCase_A`, between the cases and the fridge, looking toward the open side. NavMesh: ground plane only (the canteen floor is not navigated; customers never enter it). Walkable boundary colliders: kiosk back wall, patio perimeter, an invisible 25 m ring around the whole scene.

## 6. How the scene is made

`Main.unity` is generated by `MainSceneBuilder` (see `docs/UNBLOCK_P2_P3.md`): it measures the imported FBX bounds to find which side of the kiosk is open (the side the display cases face), snaps that to an axis, then puts the patio about 16 m out along it. That keeps the placement independent of the Blender to Unity axis conversion. Constants (`PatioDistance`, `TableRadius`) are at the top of the file.

## 7. Changelog

| Date | Change | Who |
|---|---|---|
| 2026-10-30 | v1 from measured data; no editor yet. | P1 |
| 2026-10-30 | `MainSceneBuilder`, `SeatPoint`, `SpawnPoint`, `NavMeshBootstrap` added; scene generated by script. | P1 |
| 2026-10-30 | Verified in Unity headless: aisle 1.66 m after fridge +0.6 m (Blender frame, doors and markers moved with it); colas moved into the fridge; PatioDistance 22 m; 24/24 chairs face their table; patio structures (all but Ground) marked Not Walkable via NavMeshModifier because the entrance had snapped onto the building roof; entrance on the NavMesh and all 8 seats reachable (paths 10.0-14.5 m from entrance, 15.5-18.9 m from player start). | P1 |
