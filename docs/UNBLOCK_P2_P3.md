# What P1 has ready for P2 and P3 (2026-10-30)

Read with `docs/LAYOUT.md`. Everything here was authored without opening Unity (no license on the author's machine), but all C# compiles against the real Unity 6000.0.43f1 assemblies (`python tools/unity_compile_check.py ...`; only the AI Navigation package types are stubbed). First person who opens the project: **let Unity import, commit the new `.meta` files for the FBXs and `Packages/packages-lock.json`, then tell the others.**

## 1. Getting a playable world in one click

1. Open `unity/CanteenRush` in Unity 6000.0.43f1 (Hub, sign in, free Personal license). Wait for the first import.
2. Menu **Canteen Rush > World > Build Main Scene**. It writes `Assets/Scenes/Main.unity` and `Main_NavMesh.asset`, adds the scene to Build Settings and logs the open-side direction, table and chair counts.
3. Batch form: `Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod CanteenRush.World.Editor.MainSceneBuilder.BuildBatch`.

`Main.unity` is **generated until M2**. Do not edit it by hand (P1 only, per the ownership rules). Need something placed? Ask P1, or add a prefab named as below: the builder instantiates it automatically when it exists.

| If this prefab exists | the builder does |
|---|---|
| `Assets/Prefabs/Systems/Rig.prefab` (P2) | places it at `PlayerStart` (else a plain camera at 1.6 m) |
| `Assets/Prefabs/Systems/Systems.prefab` (P2) | puts it under `_Systems` |
| `Assets/Prefabs/Systems/Audio.prefab`, `Haptics.prefab` (P3) | under `_Audio`, `_Haptics` |
| `Assets/Prefabs/Gameplay/Station_FridgeDoorUpper.prefab` / `...Lower.prefab` (P2) | places it at the FBX door node (the hinge pivot) and hides the FBX door |

## 2. What the scene contains

- `World/Canteen`: `Env_Canteen.fbx` at the origin (kiosk, display cases, counters, plates, fridge + doors, cooler) and `Floor_Canteen` (tag `SurfaceTile`). `World/Roof`: the roof deck of the 13th floor (this is a rooftop, not a field), parapet and the campus far below. `World/Canopy`: the roof over the canteen and patio, held by branching tree-shaped columns, with a lattice grill closing both long sides floor to roof (ball and item throws cannot leave through it: it has a collider). `World/Patio`: `Env_Patio.fbx` shifted 14 m onto the open side (its service building is the stair/lift core), `Apron` (tag `SurfaceStone`), 4 tables (zig-zag, 9.5 to 18 m out) and 24 chairs instantiated from markers (chairs rotated to face their table), `Marker_CustomerEntrance`. The patio `Ground` is tagged `SurfaceStone`.
- **8 `SeatPoint` components** (`CanteenRush.World.SeatPoint`, `Index` 0..7, `forward` toward the table), tag `SeatPoint`, under `World/Patio/SeatPoints`. Seats `2t`, `2t+1` belong to table `t` (tables ordered nearest-to-canteen first).
- **18 `SpawnPoint` components** (`ItemKey` = `Donut`/`Cola`/`Shake`/`Cake`/`Cup`) under `Stations/SpawnPoints`: 12 donut plates, 2 cola, 2 shake, 1 cake, 1 cup dispenser. Place your `ItemSpawner` prefab at each.
- NavMesh over the `Environment` layer (patio floor, apron, canteen floor) plus `NavMeshBootstrap` (rebakes at Start if the baked data is missing). Walk target = `SeatPoint`; spawn = `Marker_CustomerEntrance`.
- Layers per `docs/04_CONTRACTS.md` section 3: world geometry on `Environment`; tables and chairs on `Stations` (so Items and Trays collide with them, and they do not carve the NavMesh).
- `PlayerStart` under `_Rig`: between display case A and the fridge, facing the open side.
- **P3:** `World/AudioZones/AudioZone_Indoor` and `AudioZone_Outdoor` are trigger BoxColliders on layer `Triggers` (indoor = kiosk bounds; outdoor = a 120 m box around the patio, overlapping indoor, so your zone component should let Indoor win while the player is inside it). Surface tags for footsteps: `SurfaceTile` (canteen floor), `SurfaceStone` (patio ground and apron); anything else defaults to stone.
- **Lighting is done (M3).** One mixed hard-shadow directional `Sun` (the only realtime light), two baked warm rectangle lights over the counter, one baked point light at the fridge, a light-probe group (counter, fridge, door, 5x3 patio grid), one baked 128 px reflection probe, two 1024 lightmaps. Interior is warm (3500 K), exterior cool (5600 K).
- **P2: hook the sun to the round clock.** `Lighting/SunCycle` (`CanteenRush.World.SunCycle`) needs one line in your `GameManager` wiring: `GameEvents.OnRoundTimeChanged += t => sunCycle.SetRemaining(t, config.RoundSeconds);` (find it with `FindFirstObjectByType<SunCycle>()`). It shifts sun elevation 55 to 12 degrees, 5600 to 3800 K, ambient, fog and sky exposure at 10 Hz and allocates nothing.
- **P3: audio zones.** Add your `AudioZoneTrigger` component to `World/AudioZones/AudioZone_Indoor` and `AudioZone_Outdoor` (BoxColliders are already there). Do not move or resize them; ask P1.
- **P3: it is a rooftop.** Outdoors means 13 floors up: a steady wind bed fits better than birds or traffic nearby, with a faint far-off campus hum from below; the parapet is the only thing between the player and the drop, so a little wind gust near it is a cheap cue.
- **Prefabs** `Assets/Prefabs/World/Canteen.prefab`, `Patio.prefab`, `Backdrop.prefab` are the scene's connected world assemblies. Never edit them; they are regenerated.
- **Decor you do not own and should not touch:** 3 backdrop modules, 6 plant pots (`World/Patio/Plants`, on the `Environment` layer so they do carve the NavMesh a little), invisible `World/Boundary` walls, `Lighting/PerfProbe` markers.

## 3. Art you can already use (`Assets/Art`, see `docs/PIPELINE_STATUS.md`)

`Items/Item_Donut|Cola|Shake|Cake|Cup.fbx` (the cup has a `Cup_Water` child to toggle), `Props/Prop_Tray|TrayRack|CupDispenser|TablePatio|ChairBlue.fbx`, `Environment/Env_Canteen|Env_Patio.fbx`. Items are centred or base-centred at their own origin, real-world metres. Materials are real URP/Lit assets now (`Assets/Art/Materials/M_<Module>__<name>.mat`), shared across prefabs and GPU-instancing enabled; do not create per-prefab copies. Use them as the `Visual` child of your prefabs (art-swap rule); the colliders are yours.

## 4. Known gaps and things to expect

- The shake is brown and the cake dark grey: their label textures were never supplied, so they stay flat-coloured (a deliberate cut, `docs/DECISIONS.md`).
- Water cooler: bottle and pump are one mesh. Make the pump a separate invisible interactable collider over the pump head.
- The aisle between the cases and the fridge is 1.66 m (the fridge was already slid 0.6 m). If it still feels tight in the headset tell P1.
- The fence is a plain wall (the dot pattern is lost; an alpha texture is a polish item, not a requirement).
- Real-world tooling: after any art change run `tools/run_world_pipeline.sh` (materials, scene, bake, occlusion, verify, stats, screenshots). A scene rebuild discards the lightmaps, so the script always bakes after building.
- `Cr.Core` does not exist yet (P2's M0 task). `Cr.World` deliberately does not reference it.

## 5. Type-checking without a license

`python tools/unity_compile_check.py [--editor] FILE.cs ... [tools/stubs/NavMeshStub.cs]` compiles files against the real Unity engine and editor DLLs from the installed 6000.0.43f1 (override with `UNITY_DATA`). It catches syntax errors, wrong API names and bad signatures; it cannot resolve package types (XRI, Input System, TMP) unless you add a small stub. Use it on `Cr.Core` (pure C#, no packages) before the first push.
