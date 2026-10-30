# CANTEEN RUSH — MASTER SPEC (read this first, in full)

Audience: an AI coding assistant (Claude / GPT / similar) working for one of three team members, with NO access to the conversation that produced this document. Everything you need is in `docs/`. If something here contradicts what you see on disk, the disk wins for facts (file contents, counts) and this document wins for decisions (design, ownership, interfaces). If you must deviate from a decision, write the deviation and the reason in `docs/DECISIONS.md` (create it if absent) before acting.

Documents in this repo, in reading order:

| File | What it is |
|---|---|
| `docs/00_MASTER_SPEC.md` | This file. Assignment, decisions, game design, architecture, budgets, process, risks. |
| `docs/04_CONTRACTS.md` | C# interfaces, event bus, naming, layers, folder layout, assembly definitions. The integration contract between the three people. |
| `docs/05_ASSET_BIBLE.md` | Ground truth about the supplied Blender files: what each contains, what to keep/cut, scale factors, triangle budgets, export modules. |
| `docs/01_ROLE_P1_WORLD.md` | Brief for Person 1: asset pipeline, scene assembly, lighting, optimisation, builds. |
| `docs/02_ROLE_P2_GAMEPLAY.md` | Brief for Person 2: XR rig, interactions, orders, customers, game state, order UI. |
| `docs/03_ROLE_P3_AUDIO_HAPTICS_QA.md` | Brief for Person 3 (half capacity): audio, haptics, UI shell, QA, viva pack. |
| `docs/06_VIVA_PACK.md` | Likely viva questions with answers, demo script. |
| `docs/recon/*.md`, `docs/recon/previews/*.png` | Machine-generated per-file reports and flat-shaded renders of the source `.blend` files. Regenerate with `tools/blender/`. |

---

## 1. The assignment (verbatim from the course)

> **Objectives of the Mini Project**
> 1. Create a Unity 3D Game using the assets provided against your specific allotted floors.
> 2. Ensure proper Interaction, Spacial Audio, Haptics, UI and lighting effects within your game.
> 3. The game should be optimized such that it should run on a minimal compute config (mobile, headset, pc)
> 4. You are free to choose your target device (mob, Headset, pc) and target application (AR/VR/MR)
> 5. You can use emulators to demonstrate your game on the headset.
>
> **Instructions**
> 1. There are blender and Unity Files as part of the drive folder for specific floors
> 2. You are to clean up (combine them and create a unity project)
> 3. There are floors assigned to multiple teams. While you can speak to each other, the output/game should be different
> 4. You are free to use all AI tools to get things done (while your understanding will be tested during viva)

Consequences that drive this whole plan:
- **Understanding is examined (viva).** Every person must be able to explain every system, not just their own. Prefer simple, explainable solutions over clever ones. `docs/06_VIVA_PACK.md` exists for this.
- **Other teams have the same floor.** The game concept must be distinctive. (Canteen-service VR is the chosen concept; differentiate through execution: spatial audio cues, haptic language, polish, performance.)
- **"Run on minimal compute"** is graded. Performance is a feature with numbers (see §8), not an afterthought.
- **Five graded features:** Interaction, Spatial Audio, Haptics, UI, Lighting. Each must be visibly demonstrable in a 4-minute demo (see `docs/06_VIVA_PACK.md`).

The source drop is `Outdoor-20261009T052754Z-1-001.zip` (SHA-256 `1c5fc2a271858929f0b26d07ac049ef642a5eb029976315edba318292f28ba5f`, 517 MB uncompressed). It is NOT in git (too large; `.gitignore` blocks `*.zip` and `*.blend`). Every team member keeps their own copy and extracts it to `source/` (git-ignored) at the repo root, preserving the folder names `Outdoor/Part 1..4`, `Outdoor/Textures`, `Outdoor/Copy of washrooms.blend`.

---

## 2. Decisions already made (do not re-open without writing to DECISIONS.md)

| # | Decision | Why |
|---|---|---|
| D1 | **Platform: VR headset class (Meta Quest 2/3 as the performance reference), demonstrated on a Windows PC through the Unity XR Device Simulator.** No physical headset is required. Windows standalone build is the viva fallback. Android/Quest APK is a stretch goal only if someone has a headset. | The brief names headset emulators as acceptable. VR gives native, gradable interaction + haptics + spatial audio. Quest is the strictest realistic budget, so meeting it proves "minimal compute". |
| D2 | **Engine: Unity 6000.0.43f1 (Unity 6 LTS)**, already installed on the team lead's machine at `C:\Program Files\Unity\Hub\Editor\6000.0.43f1`. **URP** render pipeline. **XR Interaction Toolkit 3.x** + OpenXR + Input System. | Matches course tooling, XRI is the standard path, XR Device Simulator ships as an XRI sample. |
| D3 | **Concept: "Canteen Rush" — an Overcooked-lite VR food-service game.** Fill orders from the canteen counter/fridge/water cooler, load a tray, carry it outside to customers seated on the patio. | User choice. Uses the richest asset (canteen), gives natural hand interaction, audio cues, and haptic moments. |
| D4 | **Scope of the world: canteen kitchen (interactive) + one dining patio (Part 3) are the playable space.** Parts 1, 2, 4 and the washrooms are scenery/backdrop only (heavily reduced). | Part 3 is the only cleanly laid-out open floor with tables. See `05_ASSET_BIBLE.md`. (An earlier idea of using Part 1 or 2 as the dining area was rejected after inspection.) |
| D5 | **Menu is five items: Donut, Cola, Shake, Cake, Water.** NOTE: the `Dough` objects in the source are donuts (purple glaze, sprinkles), not pizzas. | Identified by close-up render (`docs/recon/previews/canteen_prop_Dough.png`). |
| D6 | **Locomotion: continuous move (left stick) + snap-turn 45° (right stick) + comfort vignette while moving. Teleport is an off-by-default option.** | The tray occupies one hand; continuous move works one-handed. |
| D7 | **Customers are stylised low-poly figures built from primitives (capsule body + sphere head), no skeletal animation.** | No character assets were supplied; keeps scope and triangle count down. |
| D8 | **One scene (`Main.unity`) + small per-person sandbox scenes.** Only P1 edits `Main.unity`. | Unity scene YAML merges badly; single-writer rule prevents conflicts. |
| D9 | **Washroom model is out of gameplay.** Optional set dressing only. A "wash hands before serving" mechanic is a stretch goal, default OFF. | Washroom was authored at ~1/12 scale and costs 666k triangles; low return. |
| D10 | **All tunable numbers live in ScriptableObjects (`GameConfig`, `ItemDef`, `OrderTemplate`),** never hard-coded in MonoBehaviours. | Lets anyone tune without code, easy to explain in viva. |

Open questions (ask the team lead, do not guess): deadline/viva date; whether any teammate owns a Quest headset; which GPU the viva laptop has; whether other teams' concepts are known (to keep ours different). See §17.

---

## 3. The team and ownership (2.5 people)

| Person | Capacity | Role | Owns (only they edit) |
|---|---|---|---|
| **P1** | 1.0 | World & Pipeline | `tools/blender/**`, `unity/CanteenRush/Assets/Art/**`, `Assets/Scenes/Main.unity`, `Assets/Lighting/**`, `Assets/Settings/**` (URP assets, quality), `docs/LAYOUT.md`, build outputs and build settings. |
| **P2** | 1.0 | Gameplay & XR | `Assets/Scripts/Core/**`, `Assets/Scripts/Gameplay/**`, `Assets/Prefabs/Gameplay/**`, `Assets/Data/**` (ScriptableObjects), `Assets/Scenes/Sandbox_P2.unity`, `Assets/Tests/EditMode/**`. |
| **P3** | 0.5 | Audio, Haptics, UI shell, QA, Viva | `Assets/Scripts/Audio/**`, `Assets/Scripts/Haptics/**`, `Assets/Scripts/UI/**`, `Assets/Audio/**`, `Assets/Prefabs/UI/**`, `Assets/Scenes/Sandbox_P3.unity`, `Assets/Tests/PlayMode/**`, `docs/06_VIVA_PACK.md`, `README.md`. |

Shared read-only for everyone: `docs/00..05`, `Assets/Scripts/Core/**` (written by P2 but consumed by all; changes go through a PR that P1 and P3 acknowledge).

Rules of engagement:
1. **Never edit a file you do not own.** Need a change? Open a GitHub issue (title prefix `[P1]`, `[P2]`, `[P3]` = who must act) or edit the contract doc by PR.
2. **Contracts first.** `docs/04_CONTRACTS.md` defines the event bus, enums, interfaces. P2 creates `Cr.Core` from it on day 1 (target: pushed within 2 hours). P1 and P3 code against it from then.
3. **Art-swap rule.** Every gameplay prefab has the shape `Root (logic, colliders, Rigidbody) / Visual (mesh renderers only)`. P2 ships prefabs with primitive placeholder visuals; P1 later replaces ONLY the `Visual` child. Nobody else touches `Visual`.
4. **Placeholder-first parallelism.** Nobody waits for another person's real output; everyone starts with stubs (greybox boxes, silent clips, no-op haptics) and swaps real content in at the milestone gates in §14.

Effort estimate (person-days, scale to the real deadline): P1 ≈ 11, P2 ≈ 12, P3 ≈ 6 (0.5 person ≈ 6 days over the same calendar span as 12 for a full person).

---

## 4. The product in one paragraph

You are a student cook in the PES University canteen on the **roof of the 13th floor**, in VR (the canteen kiosk and its patio stand on a roof deck with a parapet, the campus lies 42 m below; see `docs/LAYOUT.md` section 0). Customers sit at four patio tables outside. An order ticket slides onto the rail by the counter (e.g. "Donut + Cola") while a speech bubble with the same icons appears above the customer who ordered it. You grab a donut from the display case, a cola from the fridge (you pull the fridge door open on its hinge), fill a cup at the water cooler by holding it under the tap and pressing the pump, or take a shake or cake from the counter. You place items on a tray (they snap to slots), carry the tray out the door and across the patio, and set it on the customer's table. The game scores correctness and speed. A 3-minute round has three waves of increasing complexity. Every action has a spatial sound and a distinct haptic pulse; the light shifts from midday to golden hour across the round.

---

## 5. Source assets: ground truth (full detail in `docs/05_ASSET_BIBLE.md`)

Seven Blender files, all written with Blender 3.3.6 (the file header reads 3.03), openable in Blender 5.2 (installed). Counts below are EVALUATED (modifiers applied), from `docs/recon/*.md`.

| File | Evaluated tris | Real-world scale? | Role in the game |
|---|---|---|---|
| `Part 1/Copy of canteen.blend` | 2,232,688 | Yes (fridge is 2.15 m) | THE kitchen. Counter display cases, fridge, water cooler, 12 donuts, 2 shakes, 1 cake, 2 cola cans, 13 plates, shell building. |
| `Part 3/Copy of Part3_final.blend` | 1,233,798 | **~10× too big** (chair 9.43 m tall) | THE dining patio: ground, perforated fence, service building, 4 tables, 24 blue chairs. |
| `Part 2/Copy of part 2_final.blend` | 276,630 | ~5× too big | Backdrop: slatted booth pods, 48 chairs, plant pots. |
| `Part 4/Copy of part 4_final.blend` | 49,263 | ~5× too big | Backdrop: terraced marble seating, pergola rails, 3 plant pots. |
| `Part 1/Copy of part 1_final.blend` | 2,694,187 | Uncertain (chairs 0.39 m tall → ~×2.3) | Backdrop only: trees + building masses. 121 chairs × 24k tris are removed. |
| `Copy of washrooms.blend` | 665,988 | **~12× too small** (door 0.17 m) | Optional scenery. Out of gameplay. |
| `Part 2/Copy of chair_blue_flower_pot.blend` | 10,768 | Yes | Minor prop. |

Hard problems found (each has an owner and a fix in the role docs):
1. **Triangle budget.** ~7M triangles raw. Worst offenders: 12 donuts × 176,864 = 2.1M; Part 3 fence `partition.021` = 618,336 (one perforated wall); 121+18 chairs in Part 1 × 24,080; 6 washroom basins × 62,336; cake 55,452.
2. **Inconsistent scales between files** (see table). Combining without rescale gives a patio the size of a stadium next to a washroom the size of a shoebox.
3. **Missing textures.** Every file references images on the original authors' machines (`C:\Users\...\Downloads\...`). Only 8 textures were supplied (`Outdoor/Textures`: floor.png, green-grass-texture.jpg, PES_text.jpeg, pot_yellow.jpeg, seating_granite.jpeg, stone-texture.jpg, throne.png, tile.jpeg). Missing and unrecoverable: plant/leaf/pot PBR sets, glass.jpg, PEWTER2.JPG, brushedMetal bump, studio016.hdr, Part4 marble, part2 partition texture. Plan: flat colours + the supplied tiles + CC0 replacements.
4. **Procedural content.** Geometry-Nodes modifiers (canteen 25, part 2: 6, part 4: 3, washrooms 1) and Subdivision (canteen 14 at level 2) must be applied before FBX export. Node-graph shaders do not export; recreate as Unity URP materials.
5. **No colliders, no audio, no people, no rig, no animation** in any file. Fridge doors exist as separate meshes with hinge/handle objects but nothing moves.
6. **Not designed as one scene.** Files have independent origins, 7 cameras, assorted lights (Blender Watts are meaningless in Unity). All lights/cameras are discarded.

---

## 6. Game design (full specification)

### 6.1 Items (`ItemId` enum, see contracts)

| ItemId | Display | Source model (canteen.blend) | Where the player gets it | Extra mechanic |
|---|---|---|---|---|
| `Donut` | Donut | `Dough` (12 objects, 0.32×0.32×0.14 m) | Display case shelves; each shelf slot respawns the item 5 s after it is taken | none |
| `Cola` | Cola | `pepsi` / `pepsi_drink` (0.21×0.21×0.32 m can) | Inside the fridge (spawn points on fridge shelves) | fridge door must be opened first (it is physically in the way) |
| `Shake` | Shake | `Object001` (0.22×0.22×0.62 m glass with cream) | Counter top, two spawn points; respawn 8 s | none |
| `Cake` | Cake | `Cylinder` (0.51×0.49×0.46 m, mat `torta`) | Counter top, one spawn point; respawn 10 s | none |
| `Water` | Water | NEW prop `Item_Cup` (the source `cup` object has 0 triangles) | Empty cups from a cup dispenser (new prop) become `Water` when filled | hold cup under cooler spout and keep the pump pressed; fills in 4 s |

Brand note: the source can is a Pepsi can. Use the generic name "Cola" in all UI/code.

### 6.2 Stations and exact interaction rules

1. **Display case (donuts):** three-sided open case; each of 12 donut slots is a `ItemSpawner` that keeps one donut sitting on its plate. Grab with grip. Taking a donut starts a 5 s respawn timer on that slot.
2. **Fridge:** upper door (`door.001`, 0.36×0.86×1.28 m) and lower door (`door`, 0.12×0.89×0.85 m), each with a `handle` and `hinge` mesh in the source. In Unity each door is its own GameObject with a `HingeJoint` (axis along hinge, limits 0°..110°, light spring back to 0° disabled) and an XR grab on the handle. Cola cans spawn on the shelves behind the upper door. The door emits `FridgeDoorMoved(openAngle01)` events; a detent event fires crossing 8° (latch click) and at 100° (stop).
3. **Water cooler (`WaterPump_low`, 0.54×0.62×1.32 m):** the pump head is an invisible `XRSimpleInteractable` collider placed over the pump (the source mesh cannot be split) activated by the trigger while the hand hovers/selects it. While activated AND a cup is inside the `FillZone` trigger under the spout, `CupFill.level` rises 0.25/s from 0 to 1 and fires `CupFilling(level01)` each frame. At 1.0 the cup becomes `Water`. Letting go early leaves a partially filled cup that is NOT valid as `Water`.
4. **Counter (shake, cake):** `ItemSpawner` slots, same logic as donuts.
5. **Tray (`Tray`, new prop ~0.45×0.30 m):** two trays on a rack by the counter. A tray is a grabbable Rigidbody with 4 `XRSocketInteractor` slots (2×2). Items only count toward an order while snapped in a slot. A tray tilted more than 20° from level starts firing `TrayTilt(angle)` warnings; above 35° items are released to physics (they slide off).
6. **Drop/spill rule:** any `ItemId` object whose world Y falls below 0.30 m and is not held or on a tray fires `ItemDropped(item, impactSpeed, position)`, costs a spill penalty, and despawns after 2 s.
7. **Serving:** each customer's table has a `ServeSocket` (an `XRSocketInteractor` accepting only Tray, tag `Tray`). Placing a tray on a table socket whose seat has an active order evaluates that order. Trays placed on a table with no active order are ignored (no penalty). After evaluation, the customer eats for 4 s then leaves; the tray's items are cleared and the tray returns to the rack after 3 s.

### 6.3 Orders, waves, customers, patience

- `OrderTemplate` (ScriptableObject): list of `ItemId` (1..3 items). Patience seconds = `25 + 15 × itemCount` (40 s for 1 item, 55 s for 2, 70 s for 3). All numbers come from `GameConfig`.
- **Waves** (60 s each, total 180 s):
  - Wave 1: single-item orders, max 2 customers at once.
  - Wave 2: 1–2 item orders, max 3 customers at once.
  - Wave 3: 2–3 item orders, max 4 customers at once.
- New customer spawn rule: every `spawnInterval` seconds (Wave 1: 14 s, Wave 2: 11 s, Wave 3: 9 s), if active customers < wave max and a free seat exists, spawn one at the patio entrance, walk to a free seat (NavMesh), then place the order.
- **Seats:** 4 tables; gameplay uses 2 seats per table = 8 active `SeatPoint`s. The other 16 chairs are static decoration.
- **Customer states:** `Arriving → Ordering → Waiting → Eating → Leaving` (or `Angry → Leaving` on timeout). Visual feedback: patience ring colour green→amber→red; bubble shows item icons; angry shows a red bubble.

### 6.4 Scoring (all numbers tunable in `GameConfig`)

- Per correct item delivered: **+100**.
- Speed bonus per order: `round(50 × patienceRemaining / patienceMax)`.
- Perfect-combo multiplier for orders with n ≥ 2 items delivered with no extras/missing: `× (1 + 0.25 × (n − 1))` applied to the sum of per-item points (not the speed bonus).
- Wrong or extra item on the tray: **−25 each**.
- Missing item: you only score for the ones present; the order still completes.
- Spill (item dropped): **−10 each**.
- Timeout (patience reaches 0): **−50**, customer leaves angry.
- Stars at round end: ★ ≥ 300, ★★ ≥ 700, ★★★ ≥ 1200. (Tune after playtests; the 3-star target should need ~80% efficiency.)
- Score never goes below 0.

### 6.5 Game states (`RoundState`)

`Boot → Menu → Tutorial → Playing(Wave1,2,3) → Results → Menu`. Tutorial runs once per session (three highlighted steps: grab a donut, put it on the tray, serve the first customer). Pause is out of scope (cut list). Menu offers Play and Quit; Results offers Replay and Menu.

### 6.6 Player and comfort

- Standing/room-scale tracked; in the simulator you drive the rig with keyboard+mouse via the XR Device Simulator.
- Continuous move on the left stick at 1.8 m/s; snap-turn 45° on the right stick; a vignette fades in during movement (comfort). Seated mode toggle (raises the origin so a seated player reaches the counter): P2, in the settings board.
- Hands: use the Starter Assets controller models (XRI sample) — no custom hands.

### 6.7 Stretch goals (default OFF; must not destabilise the core)

1. Hand-wash step at a washroom basin before a food item is picked up (uses the otherwise unused washroom assets). P3 if time.
2. Android/Quest APK build and on-device profiling (P1) if a headset is available.
3. Second difficulty modifier: "rush hour" mode with a 20% shorter patience.

---

## 7. Technical architecture

(Exact types, signatures and folder names are in `docs/04_CONTRACTS.md`. Below is the rationale.)

- **Assembly definitions** keep three people from breaking each other's compile: `Cr.Core` (enums, `GameEvents` bus, data classes, pure logic such as `OrderMatcher`, `ScoreCalculator`) has no dependencies. `Cr.Gameplay`, `Cr.Audio`, `Cr.Haptics`, `Cr.UI` each reference `Cr.Core` and **never each other**. They communicate only through `GameEvents` (static C# events). This is the single most important architectural rule: audio, haptics and UI are *listeners* — they never call gameplay code and gameplay code never calls them.
- **Pure logic is separate from MonoBehaviours** so it can be unit-tested in EditMode without the engine: `OrderMatcher.Evaluate(order, trayItems)`, `ScoreCalculator.Score(result, patience01, config)`, `WaveDirector` (a plain class driven by an injected clock).
- **Data-driven:** `GameConfig`, `ItemDef`, `OrderTemplate`, `HapticPreset`, `AudioCueSet` are ScriptableObjects under `Assets/Data/`.
- **Object pooling** for donuts/cups/cans (spawners reuse instances); fixed cap of 40 active Rigidbodies.
- **Update discipline:** no per-frame `FindObjectOfType`, no per-frame LINQ/allocations in gameplay loops (Quest GC spikes). Cache references in `Awake`.
- **Physics:** fixed timestep 1/72 s (0.013889), solver iterations 6 / velocity iterations 1, items use continuous-dynamic collision, mesh colliders only on static architecture and only if convex proxies cannot work (prefer primitive colliders).
- **XR stack:** Unity XR Plug-in Management with OpenXR; XR Interaction Toolkit 3.x: `XROrigin`, `XRInteractionManager`, `NearFarInteractor` or `XRDirectInteractor` + ray as per the Starter Assets rig, `XRGrabInteractable`, `XRSocketInteractor`, `XRSimpleInteractable`, `TrackedDeviceGraphicRaycaster` for world-space UI, `XR UI Input Module`. The XRI package API changed noticeably between 2.x and 3.x — **before writing code, read the installed package version's documentation and sample scene; verify every type name used in this spec against the installed version** (see §16 risk R5).
- **Interaction layers:** XRI uses `InteractionLayerMask`. Defined in contracts: `Grabbable`, `Tray`, `Socket-Item`, `Socket-Tray`, `UI`.

### 7.1 Scene structure of `Main.unity` (P1 assembles; P2/P3 deliver prefabs)

```
Main
├── _Systems            (GameManager, WaveDirector, OrderService, ScoreService, GameEvents debug)  [P2 prefab]
├── _Rig                (XR Origin Rig, locomotion, vignette)                                        [P2 prefab]
├── _Audio              (AudioManager, mixer, reverb zones, ambience emitters)                       [P3 prefab]
├── _Haptics            (HapticsService)                                                            [P3 prefab]
├── World
│   ├── Canteen         (shell, counters, fridge, cooler, display case)                             [P1]
│   ├── Patio           (ground, fence, building, tables, chairs, seat points)                      [P1]
│   └── Backdrop        (Part 1 trees, Part 2 pods, Part 4 terrace, optional washroom)              [P1]
├── Stations            (ItemSpawners, FridgeDoors, WaterCooler, TrayRack, CupDispenser)            [P2 prefabs placed by P1]
├── Customers           (runtime-spawned root)                                                       [P2]
├── UI                  (OrderRail, WristHUD, MenuBoard, ResultsBoard, TutorialHints)               [P2: OrderRail/bubbles; P3: rest]
└── Lighting            (Sun, reflection probes, light probe groups, lightmaps, day-night driver)   [P1]
```

### 7.2 Systems and data flow

```
Input (XR) ──► Interactables (Gameplay) ──► GameEvents ──► Audio / Haptics / UI (listeners)
                        │                         ▲
                        ▼                         │
          OrderService ◄─ WaveDirector ◄─ GameManager (state machine, clock)
                        │
                        ▼
      CustomerManager (spawn, NavMesh walk, seat) ── patience ticks ──► GameEvents
                        │
                        ▼
      TraySocket on table ► OrderMatcher.Evaluate ► ScoreCalculator ► ScoreService ► GameEvents
```

---

## 8. Performance budget (reference device: Meta Quest 2, 72 Hz = 13.9 ms/frame)

| Metric | Hard limit | Target |
|---|---|---|
| Frame time on reference class hardware | 13.9 ms | ≤ 11 ms |
| Triangles rendered per eye-frame (visible) | 500k | ≤ 250k |
| Triangles in the whole scene (all LODs counted at LOD0) | 1.0M | ≤ 600k |
| Draw calls / SetPass (SRP Batcher on) | 250 | ≤ 150 |
| Realtime lights | 1 | 1 (sun, shadows) — everything else baked |
| Realtime shadow cascades | 1 | 1 at ≤ 25 m, 1024 res, soft shadows off |
| Texture memory | 250 MB | ≤ 150 MB (ASTC 6x6 on Android; DXT5/BC7 on PC) |
| Lightmaps | 6 × 1024 | ≤ 3 × 1024 |
| Active Rigidbodies | 60 | ≤ 40 |
| Audio voices | 24 | ≤ 16 |
| Build size (APK or PC) | 500 MB | ≤ 250 MB |

Settings to apply (P1 owns): URP, Forward renderer; Single Pass Instanced stereo; MSAA 4×; HDR off; no post-processing volume; SRP Batcher on; GPU instancing on shared materials; static batching on architecture; LOD groups on chairs/plants; occlusion culling baked for canteen interior; depth texture/opaque texture off; shadows: sun only; fixed foveated rendering enabled for Android builds.

How to measure (P1, with P3 verifying): Unity Profiler + Frame Debugger + Stats window; `Window > Analysis > Rendering Debugger`; report triangle/draw-call numbers for three camera positions (inside canteen at counter, at the door, mid-patio) in `docs/PERF_REPORT.md` at M5. The numbers in this table are the pass criteria.

---

## 9. Lighting spec (P1)

- **Baked GI** for all static architecture (Progressive GPU lightmapper if the GPU supports it, else CPU). Lightmap resolution ~15–20 texels/m interior, ~8 outdoors. Generate Lightmap UVs on FBX import for every environment mesh.
- **Light layout:**
  - Canteen interior: 2 baked rectangle (area) lights over the counter (the source file has two area lights — they are the intended positions), 1 baked point light near the fridge. Warm (≈3500 K).
  - Patio: one mixed directional sun (shadow type: baked for static, realtime for dynamic objects up to 25 m). Cool-neutral (≈5600 K).
  - Emissive materials (baked): the `light` and `light box` meshes in the canteen, and the PES sign (`PES_text.jpeg` / `PESU` mesh in Part 1).
- **Light probes** around the counter, fridge, door and a grid across the patio so moving items (donuts, trays) are lit consistently. One **reflection probe** in the canteen (baked, 128 px).
- **Day→golden-hour driver** (`SunCycle`, ~40 lines): over the 180 s round the sun elevation lerps 55° → 12°, colour temperature 5600 K → 3800 K, ambient gradient lerps; skybox exposure drops slightly. Because GI is baked, drive only the *realtime* sun, ambient colour and fog colour; do not rebake. (Mixed-lighting mode: Baked Indirect or Shadowmask. Choose Shadowmask only if the budget allows; document the choice.)
- **Mood contrast** is a graded feature: warm interior vs. cool exterior must be obvious in screenshots.
- Glass (display case front, fridge door glass): opaque tinted + reflection probe preferred; true transparency only on the display case front (single quad, alpha 0.25) because transparency overdraw is expensive on tiled mobile GPUs.

---

## 10. Spatial audio spec (P3)

- Every in-world sound is a 3D `AudioSource`: `spatialBlend = 1`, `dopplerLevel = 0`, custom logarithmic rolloff, `spread` 0–30° for point sources. Output to an `AudioMixer` with groups `Master → {SFX, Ambience, Music, UI, Voice}` and snapshots `Indoor` / `Outdoor` (outdoor: brighter, more reverb send reduced; indoor: low-pass 12 kHz, small-room reverb).
- **Spatializer plugin:** Project Settings > Audio > Spatializer Plugin. If the Meta XR Audio SDK is installable, use it for HRTF. If not, ship without a plugin (Unity's stereo panning still gives left/right/distance cues); document which was used. Either way every source stays flagged as spatial.
- **Cue table** (ID → trigger, loop, 3D range min/max in metres, notes):

| Cue ID | Trigger | Loop | Range | Notes |
|---|---|---|---|---|
| `amb_fridge_hum` | always, at fridge | yes | 1 / 9 | low hum; fades when door open → louder (`FridgeDoorMoved`) |
| `amb_canteen_room` | inside canteen | yes | 2D bed via `Indoor` snapshot | murmur/fans |
| `amb_patio_birds` | always, 4 emitters around the patio | yes | 3 / 25 | randomised start offsets |
| `amb_patio_crowd` | customer count | yes | 3 / 20 | volume ∝ active customers |
| `fridge_latch` | `FridgeDoorMoved` detent | no | 1 / 8 | click (open and close variants) |
| `pump_press` | `PumpActivated` | no | 1 / 6 | clank |
| `water_fill_loop` | `CupFilling` | yes while filling | 1 / 6 | pitch rises with level (0.9→1.2) |
| `grab_soft` / `release_soft` | `ItemGrabbed` / `ItemReleased` | no | 1 / 5 | per `ItemDef.audioMaterial` (plastic, glass, paper) |
| `tray_clink` | tray item impacts | no | 1 / 8 | volume ∝ impact speed (0–4 m/s), ±5% pitch |
| `item_drop` | `ItemDropped` | no | 1 / 12 | spill thud + glass break variant for Shake |
| `order_bell` | `OrderPlaced` | no | 2 / 30 | **spatial pointer**: sounds AT the customer's table so players can find who ordered — this is the signature spatial-audio mechanic |
| `customer_happy` / `customer_angry` | `OrderServed` / `OrderExpired` | no | 2 / 20 | short vocal bark, no words |
| `footsteps_tile` / `footsteps_stone` | player movement over surface tags | no | 2D | surface chosen by raycast tag |
| `ui_click`, `ui_confirm`, `ui_error` | UI events | no | 2D | 2D UI group |
| `music_loop` | round active | yes | 2D | low-pass inside canteen via snapshot |
| `round_end_sting` | `RoundStateChanged(Results)` | no | 2D |  |

- **Occlusion/obstruction:** `AudioOcclusion` component on ambience/customer emitters: each 0.1 s raycast from listener to source on the `Environment` layer; if blocked, lerp `AudioLowPassFilter.cutoffFrequency` 22000 → 1200 Hz and volume × 0.7.
- **Reverb:** `AudioReverbZone` over canteen interior (preset `Room`) and none outdoors; mixer send for the rest.
- **Voice budget:** pool of 12 one-shot `AudioSource`s; steal the quietest. Total concurrent ≤ 16.
- **Content sources (must be license-clean; record every file in `Assets/Audio/CREDITS.md` with URL + license + date):** Kenney.nl (CC0), freesound.org filtered to CC0, Pixabay Content License, Sonniss GDC bundles. No YouTube rips. Commit original WAV/OGG via Git LFS. Normalise to −16 LUFS, trim silence, mono for 3D sources, 44.1 kHz.

---

## 11. Haptics spec (P3)

- All haptics go through one service so devices/APIs can be swapped: `IHapticsService` (contracts). Concrete: `XRHapticsService` sends impulses to the left/right controller via the installed XRI version's haptic API (XRI 3.x: `HapticImpulsePlayer` on the controller objects; verify against the installed package). It subscribes to `GameEvents`; gameplay code never calls it.
- The XR Device Simulator cannot vibrate hardware. Therefore ship a **`HapticsDebugHud`**: a small world-space icon above each controller that flashes with amplitude/duration of every pulse and writes a line to a visible on-screen log (toggle with a key). This is what the viva audience will see.
- **Preset table** (amplitude 0–1, duration seconds; `Ramp` = continuous following a value):

| Preset | Trigger event | Amplitude | Duration | Hand |
|---|---|---|---|---|
| `Grab` | `ItemGrabbed` | 0.25 | 0.04 | grabbing hand |
| `Release` | `ItemReleased` | 0.10 | 0.03 | releasing hand |
| `SocketSnap` | `ItemSocketed` | 0.35 | 0.06 | last holder hand |
| `DoorLatch` | `FridgeDoorMoved` detent | 0.45 | 0.05 | hand on handle |
| `DoorStop` | door reaches limit | 0.60 | 0.08 | hand on handle |
| `PumpPress` | `PumpActivated(true)` | 0.50 | 0.07 | pump hand |
| `WaterFill` | `CupFilling(level)` | Ramp: 0.10 + 0.35 × level | continuous 0.05 refresh | cup-holding hand |
| `TrayTiltWarn` | `TrayTilt(angle)` > 20° | Ramp: map 20°→35° to 0.15→0.7 | continuous | tray hand |
| `ServeOk` | `OrderServed` perfect | double pulse 0.5 / 0.5 | 0.06 + gap 0.05 + 0.06 | both |
| `ServePartial` | `OrderServed` imperfect | 0.35 | 0.12 | both |
| `Spill` | `ItemDropped` | 0.85 | 0.10 | last holder hand |
| `OrderExpired` | `OrderExpired` | 0.70 | 0.30 | both |
| `UiTick` | UI hover | 0.15 | 0.02 | pointing hand |

- Design principle to state in the viva: **haptics encode meaning** (short = contact, ramp = continuous process, long = failure), and every haptic pairs with a sound so the player can tell what happened through two senses.
- Rate-limit: never more than one pulse per 30 ms per hand; ramps coalesce.

---

## 12. UI spec (split: P2 order-related, P3 the rest)

Principles: diegetic where possible; world-space Canvases (scale 0.001, `Render Mode: World Space`, `TrackedDeviceGraphicRaycaster`), text via TextMeshPro, character height ≥ 3 cm at 1.5 m reading distance, high contrast, no text smaller than 24 pt at canvas scale, icons for items so no reading is needed in a rush. One font family, two weights.

| Element | Owner | Behaviour |
|---|---|---|
| **Order rail** (on the counter wall, up to 4 tickets) | P2 | Each ticket: item icons, seat number, patience bar. Slides in on `OrderPlaced`, flashes at 25% patience, removed on served/expired. |
| **Customer bubble** (above each seated customer) | P2 | Same icons + patience ring; billboard-faces the camera on Y only. |
| **Wrist HUD** (left wrist, appears when palm faces up/gaze) | P3 | Timer, score, wave. For the simulator: attached to the left controller, always visible. |
| **Menu board** (Play, Quit, Seated toggle, Teleport toggle) | P3 | Ray/poke buttons, `ui_click`/haptic `UiTick`. |
| **Results board** | P3 | Score breakdown, stars, Replay, Menu. |
| **Tutorial hints** | P3 (UI) + P2 (trigger logic) | 3 steps; highlights the next target with a pulsing outline (emission toggle). |
| **Comfort vignette** | P2 | Fullscreen-in-front-of-eyes inverse-circle quad; alpha tied to move speed. |

---

## 13. Testing strategy

- **EditMode unit tests (P2, in `Assets/Tests/EditMode`):** `OrderMatcherTests` (exact, extra, missing, duplicates, order-independence), `ScoreCalculatorTests` (every formula in §6.4, zero-floor), `WaveDirectorTests` (spawn cadence with a fake clock, wave transitions, seat exhaustion). Run in CI-less fashion: Test Runner window before every PR. P2 writes the tests BEFORE the implementations (TDD).
- **PlayMode smoke tests (P3, `Assets/Tests/PlayMode`):** scene loads without errors; one full simulated round (scripted `GameEvents`) ends in Results; no exceptions logged; audio and haptics services respond to each event; frame time sampling over 60 s stays under limit on the dev machine (informational).
- **Manual QA checklist (P3 owns, `docs/QA_CHECKLIST.md`):** every interaction in §6.2 per station; every cue in §10; every haptic in §11; comfort; menu/results; restart-without-leaks (play 3 rounds back-to-back; memory flat).
- **Perf verification (P1 + P3):** §8 report at M5.
- Definition of Done for any task: compiles with zero errors and zero new warnings in your assembly; tests for your logic pass; you ran the sandbox scene; your PR description lists what you tested.

---

## 14. Milestones and gates (calendar-agnostic; scale to the real deadline)

| Gate | Name | Exit criteria | Who |
|---|---|---|---|
| **M0** | Bootstrapped | Unity project created at `unity/CanteenRush`, URP, packages installed, XR Device Simulator sample works with a cube you can grab. `Cr.Core` pushed with all enums/events from the contracts. All three have cloned the repo, Git LFS works, `Main.unity` loads. | P1 (project), P2 (Core) |
| **M1** | Greybox playable | `Main.unity` has the canteen+patio laid out in **primitive boxes at correct scale** (`docs/LAYOUT.md` published), player start, 8 seat points, NavMesh baked. P2's stations/trays/customers work on primitives; a full round is playable. P3's audio and haptics respond to events with placeholder sounds. | all |
| **M2** | Real art in | Cleaned FBX modules imported; `Visual` children swapped on all gameplay prefabs; textures/materials assigned; colliders correct; scale verified against a 1.8 m reference figure. | P1 |
| **M3** | Feature complete | Everything in §6 except stretch goals; all cues in §10 have real sounds; all presets in §11 wired; menu/results/tutorial; lighting baked, day-night driver working. | all |
| **M4** | Optimised | §8 table met in the three measurement positions; `docs/PERF_REPORT.md` written; 3 consecutive rounds, no leaks. | P1 + P3 |
| **M5** | Release candidate | Windows build runs on a clean machine; viva pack complete; demo rehearsed twice; tag `v1.0-rc`. | P3 + all |
| **M6** | Submission | Final fixes only; tag `v1.0`. | all |

Dependency summary: P2 and P3 are NOT blocked by P1's art (they use primitives until M2). P1 is NOT blocked by gameplay (placeholder prefabs). The only hard blockers are M0 items: P1 creates the Unity project; P2 pushes `Cr.Core`.

---

## 15. Repository and process rules

- Repo: https://github.com/santoshcheethiralame-dot/Canteen-Rush (public/private as the owner set it; license MIT). The team lead must add the other two as collaborators.
- **Layout:** `docs/`, `tools/blender/`, `unity/CanteenRush/` (the Unity project root), `source/` (git-ignored: extracted zip).
- **Branches:** `main` is always buildable. Personal branches: `p1/<topic>`, `p2/<topic>`, `p3/<topic>`. Merge to `main` by pull request; the owner of the touched area approves; squash-merge. Rebase on `main` before opening a PR. Never force-push `main`.
- **Unity settings (P1 sets at M0, nobody changes):** Version Control = Visible Meta Files; Asset Serialization = Force Text; `.meta` files are always committed with their assets.
- **Large files:** binary assets (FBX, PNG/JPG/TGA/EXR/HDR, WAV/OGG/MP3, PSD, unitypackage) are tracked by Git LFS via `.gitattributes`. Never commit `.blend` files, the source zip, `Library/`, `Temp/`, `Logs/`, `Builds/`. LFS quota on a free GitHub account is 1 GB storage and 1 GB/month bandwidth — keep committed binaries lean (compress textures, trim audio) and never commit the same large file repeatedly.
- **Scenes and prefabs:** single-writer rules in §3. Use prefab variants, not scene edits, to share changes.
- **Commit messages:** imperative, ≤ 72-char subject, body explains why. Reference the issue/milestone (e.g. `M2: swap donut visual`).
- **Docs are code:** if you change an interface, update `docs/04_CONTRACTS.md` in the same PR.
- **AI-assistance disclosure:** the course permits AI tools; the viva tests understanding. Each person keeps a short `docs/LOG_P1.md` / `LOG_P2.md` / `LOG_P3.md` (what you built, what the AI suggested, what you changed/rejected and why) — this is viva ammunition. Third-party assets and AI-generated assets are listed in `docs/CREDITS.md`.

---

## 16. Risk register

| ID | Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R1 | Decimation destroys donut/shake detail (sprinkles, cream) | High | Med | Decimate to ~2k tris with preserved normals; if ugly, rebuild as torus + glaze mesh + baked texture. Hero props are small on screen. | P1 |
| R2 | Scale mismatch yields wrong-sized world | High | High | Verify every file against the 1.8 m reference figure and doors (~2.1 m) before export; estimated factors are in `05_ASSET_BIBLE.md` but must be confirmed visually. | P1 |
| R3 | Missing textures → flat, ugly world | Certain | Med | Plan flat-colour + supplied tiles + CC0 (ambientCG, Poly Haven) from the start; bake a few hero textures. | P1 |
| R4 | Geometry-Nodes/procedural shaders do not survive FBX | High | Med | Apply modifiers before export; rebuild materials in Unity by a name mapping table. | P1 |
| R5 | XRI 3.x API differs from what an AI model remembers | High | High | Read the installed package docs/samples first; keep all XRI-specific code behind thin wrappers (`IHapticsService`, `ItemGrabbable`); record the exact package versions in `docs/VERSIONS.md` at M0. | P2/P3 |
| R6 | Unity YAML merge conflicts | Med | High | Single-writer scene/prefab ownership (§3); sandbox scenes; small PRs. | all |
| R7 | LFS quota exhausted | Med | Med | Only commit optimised exports; no raw PSDs/blends; check `git lfs ls-files -s`. | all |
| R8 | Simulator hides haptic/audio quality | Med | Med | `HapticsDebugHud`; subtitles for cues; test with headphones. | P3 |
| R9 | Another team picks the same concept | Med | Low | Differentiators: spatial order-bell pointer, haptic language, day→golden-hour arc, performance report. | all |
| R10 | Customers feel lifeless without animation | Med | Low | Walk bob, head-turn toward player, bubble animation, VO barks. Accept and move on. | P2 |
| R11 | Performance miss on integrated GPU | Med | High | Budgets in §8; profile at M1 greybox already (not just at M4). | P1 |
| R12 | Android build module not installed | Med | Low | Stretch only; check in Unity Hub `Installs > Add modules`. | P1 |
| R13 | Viva: someone cannot explain another's system | Med | High | `06_VIVA_PACK.md`, cross-explain sessions at M3 and M5, per-person logs. | P3 |

---

## 17. Cut list (a contract, not a backlog)

NOT in scope, do not build: multiplayer/networking, hand tracking, AR/passthrough, procedurally generated menus, save/load, localisation, character animation/rigging/IK, liquid simulation, destructible objects, pause menu, settings persistence, achievements/leaderboards, voiced dialogue with words, washroom gameplay (unless stretch), day-night rebaking.

## 18. Open questions for the team lead (record answers in `docs/DECISIONS.md`)

1. Viva / submission date → sets the calendar for §14.
2. Does anyone have a Quest headset? (decides whether the Android build is worth attempting.)
3. Specs of the viva laptop (GPU) → performance headroom.
4. Do we know the other teams' concepts? (differentiation).
5. Is Unity Hub's Android Build Support module installed on any machine?

## 19. Glossary

- **XRI:** XR Interaction Toolkit (`com.unity.xr.interaction.toolkit`). **OpenXR:** the cross-vendor XR runtime API. **URP:** Universal Render Pipeline. **SRP Batcher:** URP draw-call batching for shaders sharing a variant. **APV/Light probes:** baked lighting samples for moving objects. **LOD:** level of detail mesh swap. **LFS:** Git Large File Storage. **ScriptableObject (SO):** Unity data asset. **asmdef:** assembly definition (separate compile unit). **Greybox:** primitive-box stand-in level. **Evaluated triangles:** triangle count after modifiers are applied (what FBX export would write). **Hand:** `Hand.Left` / `Hand.Right`, the physical controller.
