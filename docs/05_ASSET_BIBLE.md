# ASSET BIBLE — ground truth about the supplied Blender files

Primary reader: P1 (World & Pipeline). Everyone else: read §1, §2 and §7 so you know what the art can and cannot do.

Everything here was measured by script (`tools/blender/dump_recon.py`, Blender 5.2.0 LTS) or seen in flat-shaded renders (`tools/blender/render_preview.py`, `render_isolated.py`). Raw reports: `docs/recon/<file>.md` (every object, every material, every image). Previews: `docs/recon/previews/*.png`. **Items marked (inferred) come from names/dimensions, not from a confirmed visual — verify in Blender before relying on them.** Previews are Workbench renders: material base colours only, no image textures, no baked lighting, so they under-represent the final look.

Reproduce a report: run `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --disable-autoexec "<file>.blend" --python tools/blender/dump_recon.py -- out.md`. ALWAYS pass `--disable-autoexec` (the files are untrusted third-party data; do not let embedded scripts run).

---

## 1. Source files at a glance

| File (inside zip `Outdoor/`) | Objects | Evaluated tris | Lights / cams | Bounds (m) | Verdict |
|---|---|---|---|---|---|
| `Part 1/Copy of canteen.blend` | 69 mesh | 2,232,688 | 4 lights, 1 cam | 16.86 × 13.51 × 7.24 | **Core. Keep and slim down.** |
| `Part 3/Copy of Part3_final.blend` | 52 | 1,233,798 | 1 light, 1 cam | 163.37 × 93.17 × 23.19 | **Core (patio). Rescale ×0.1, slim down.** |
| `Part 2/Copy of part 2_final.blend` | 59 | 276,630 | 0 / 0 | 49.80 × 42.62 × 21.07 | Backdrop. Rescale ×0.2, slim down. |
| `Part 4/Copy of part 4_final.blend` | 20 | 49,263 | 1 / 1 | 39.59 × 22.33 × 17.65 | Backdrop. Rescale ×0.2. |
| `Part 1/Copy of part 1_final.blend` | 156 | 2,694,187 | 0 / 0 | 6.71 × 11.19 × 4.44 | Backdrop only; keep trees + masses, cut chairs. Scale uncertain. |
| `Copy of washrooms.blend` | 48 | 665,988 | 1 / 1 | 1.14 × 1.26 × 1.49 | Optional scenery. Rescale ×12. |
| `Part 2/Copy of chair_blue_flower_pot.blend` | 3 | 10,768 | 1 / 1 | 1.48 × 1.34 × 1.46 | Optional prop (plant pot). |

All files were saved by Blender 3.3.6 (header `BLENDER-v303`). They open in 5.2 without conversion problems. Units: metric, scale_length = 1.0 in every file (so the scale problems are real geometry scale, not unit settings).

Textures actually supplied in `Outdoor/Textures/`: `floor.png` (21 KB), `green-grass-texture.jpg` (148 KB), `PES_text.jpeg` (69 KB), `pot_yellow.jpeg` (16 KB), `seating_granite.jpeg` (229 KB), `stone-texture.jpg` (3 MB), `throne.png` (4.2 MB), `tile.jpeg` (109 KB).

---

## 2. Scale normalisation (the first thing P1 must settle)

Each file was authored independently at a different scale. Reference true sizes: standing person 1.7–1.8 m, door 2.0–2.1 m, dining chair seat height ≈ 0.45 m and overall ≈ 0.9 m, table 0.75 m high, 2.2 × 0.75 m for a 6-seater, fridge ≈ 1.8–2.1 m.

| File | Evidence (measured) | Suggested uniform scale to apply | Confidence |
|---|---|---|---|
| canteen | fridge 2.15 m, counter 2.24 × 1.29 × 1.23 m, shell 7.29 × 7.25 × 3.27 m | **1.0** | High |
| Part 3 | chair 5.9 × 6.0 × 9.43 m tall; table 7.4 × 22.0 × 7.3 m (→ 0.74 × 2.2 × 0.73 m); fence 163 m long; patio ground 163 × 93 m | **0.1** | High (chair 0.94 m, table 2.2 × 0.74 × 0.73 m — textbook values) |
| Part 2 | chair 2.50 × 2.32 × 4.58 m tall; plant 7.4 × 6.1 × 7.9 m | **0.2** | Medium (chair → 0.92 m; check pod walls/doors) |
| Part 4 | pot 2.5 × 2.3 × 2.5 m; partition 39.6 m long; seats 5.5 × 6.6 × 7.7 m | **0.2** | Medium |
| Part 1 | chair 0.25 × 0.25 × 0.39 m; pillar/tree 3.5 × 4.0 × 3.8 m; pot 0.73 m | chairs imply ≈ **2.3**, trees/pots imply ≈ **1.0** | **Low — internally inconsistent; measure** |
| washrooms | door 0.17 m tall, toilet 0.09 m, basin 0.04 m, whole room 0.48 m | **≈ 12** (door → 2.0 m) | Medium |
| chair_blue_flower_pot | pot+plant 1.5 m | 0.4 (plant ~0.6 m) — or ignore | Low |

**Verification procedure (do before any export):** in Blender, add a 1.75 m reference cube/figure at the origin next to each module; check chair seat height ≈ 0.45 m, door ≈ 2.05 m, table height ≈ 0.75 m; adjust the factor. Record the final factors in `docs/LAYOUT.md` and in `tools/blender/config.json` (the single table the build script reads). Apply scale in Blender (`Ctrl+A > Scale`) so FBX scale is 1.0 in Unity.

---

## 3. Per-file inventory, decisions, targets

### 3.1 `canteen.blend` — the kitchen (P1 priority 1)

Everything is at real scale. Floor plane 13.51 m square and a wall plane 13.51 × 7.23 m are placeholders (2 tris each, boolean on the wall) — **discard both** and build real floor/walls from the shell.

| Source object(s) | What it is | Dim (m) | Tris now | Decision | Target tris |
|---|---|---|---|---|---|
| `cafeteria` | Building shell (7.29 × 7.25 × 3.27), mat `canteen`; centre (−0.3, 0, 1.6) | 7.29×7.25×3.27 | 108 | Keep. Verify it is hollow with a door opening (inspect in Blender; if not, model a door gap). | ≤ 300 |
| `Cube`, `Cube.001` | Two glass-fronted **display cases** with metal body, mat `glass/metal/marble`; centres (−2.5, 0.0, 0.9) and (−2.5, −2.2, 0.9) | 1.29×2.24×1.23 | 92 each | Keep. Donut shelves. | ≤ 300 each |
| `Cube.002` | Counter section (centre (−2.3, 2.2, 0.7)) | 1.56×2.21×0.79 | 28 | Keep. Shake/cake counter. | 28 |
| `Cube.003` | Cooler stand / counter piece (centre (−1.1, 2.7, 0.5)) | 0.82×1.25×1.02 | 28 | Keep. Water cooler base. | 28 |
| `Circle`..`Circle.012` (13) | Round **plates/trays** at shelf heights z = 0.6 and 1.3, Subsurf lvl 2 | 0.49×0.49×0.04 | 1,408 each | Apply modifier then decimate. 12 hold donuts, 1 holds the cake. | ≤ 150 each |
| `Dough`..`Dough.011` (12) | **Donuts** (purple glaze, sprinkles, 4 blue pearls) — confirmed visually | 0.32×0.32×0.14 | **176,864 each** (2.12M total) | **Replace by one decimated mesh `Item_Donut`** (see §5.2). Do not export 12 copies. | ≈ 2,000 |
| `Cylinder` (mat `torta`) | **Cake** (tiered, centre (−2.6, −2.6, 1.8)) | 0.51×0.49×0.46 | 55,452 | Decimate | ≈ 3,000 |
| `Object001`, `Object001.001` | **Milkshake glasses** with whipped cream (cream is sculpted — see `previews/canteen_prop_Object001.png`) | 0.22×0.22×0.62 | 7,576 each | Decimate, keep one as `Item_Shake` | ≈ 1,500 |
| `pepsi`, `pepsi_drink` | **Cola cans** (Pepsi-branded textures lost) | 0.21×0.21×0.32 | 2,918 each | Decimate, de-brand (flat colour + stripe), one `Item_Cola` | ≈ 800 |
| `fridge` | Fridge body (mat silver / white plastic), centre (−0.4, −0.5, 1.1) | 0.62×0.88×2.15 | 684 | Keep | ≤ 700 |
| `door.001` | Upper fridge door | 0.36×0.86×1.28 | 108 | **Separate mesh, pivot on hinge edge** | 108 |
| `door` | Lower fridge door | 0.12×0.89×0.85 | 108 | Separate mesh, pivot on hinge edge | 108 |
| `hinge` ×3, `handle` ×2 (`handle`, `handle.001`) | Hinge bars and door handles | hinge 0.03×0.04×2.18; handle 0.10×0.13×1.24 | 636/28 | Keep (hinge ×3 may be merged into 1) | ≤ 700 total |
| `shelf` ×4, `shelf edge` ×3, `divider`, `drawer` ×2, `large drawer` ×3 | Fridge interior | up to 0.56×0.86 | 2–232 each | Keep; interior only visible through open door, merge by material | ≤ 1,500 total |
| `WaterPump_low` | **Water cooler** (blue 19 L bottle + hand pump), centre (−1.1, 2.6, 1.7) | 0.54×0.62×1.32 | 10,264 | Decimate | ≈ 3,000 |
| `cup` | Cup with Subsurf lvl 2 and 2 materials but **0 triangles** (geometry missing/empty) | — | 0 | **Cannot be used. Model `Item_Cup` fresh** (tapered cylinder, 12 sides) | ≈ 300 (new) |
| `fan`, `fan box`, `light`, `light box`, `leg` ×4, `Object` | Ceiling fan, light fixtures, small hardware | small | ≤ 1,216 | Keep `light`/`light box` (emissive); `fan` optional; drop `leg`/`Object` if not visible | ≤ 800 total |
| 4 lights (2 area 78.5 W, 1 point 1000 W, 1 point 94 W), 1 camera | Authoring lights | — | — | **Discard.** Re-author lighting in Unity at the same positions (area lights over the counters). | — |

Missing textures in canteen: `floor.png` (supplied: `Textures/floor.png`), `stone-texture.jpg` (supplied), `glass.jpg` (4096², packed — **the only packed images in the whole drop**: `Map #4`, `Map #4.001`, 4096² each; these are the shake/cake label textures), `PEWTER2.JPG`, `brushedMetal_4_bump.jpg`, `studio016.hdr` (all missing). `Map #4` is packed inside the .blend and CAN be extracted (Blender: Image Editor > Image > Save As, or `bpy.data.images['Map #4'].save(filepath=...)`).

Materials to recreate in Unity (by name): canteen, black metal, silver, white plastic, plastic_blue, bottle, water, glass/glass.001 (alpha), marble (→ `stone-texture.jpg` tiling), floor (brick procedural → `floor.png`), torta, Material_001/_002/_004 (donut: base dough brown, glaze purple, sprinkles), Material #46/#81(.001) (shake/cake with baked label from `Map #4`), metal_pepsi/metal.001 (can), light (emission). Everything else: flat colour from the Blender base colour.

**Placement note:** items sit on shelf slots at (x = −2.5, y ∈ {0.7, 0.1, −0.5, −1.8, ...}, z ∈ {0.7, 1.3}). Spawner empties go at those positions; `Item_Donut` prefab sits on a plate.

### 3.2 `Part3_final.blend` — the dining patio (P1 priority 1)

Scale ×0.1 (see §2). After scaling: ground 16.3 × 9.3 m; fence 16.3 m × 0.056 m × 1.9 m.

| Source object(s) | What it is (some inferred from the preview) | After ×0.1 | Tris now | Decision | Target |
|---|---|---|---|---|---|
| `Plane` | Ground/pavement plane | 16.3 × 9.3 m | 2 | Keep, texture with `tile.jpeg` or `seating_granite.jpeg` or `stone-texture.jpg`, tile ~1 m | 2 |
| `partition.021` | **Perforated fence/screen** — the tiled dot pattern in the preview | 16.3 × 0.056 × 1.9 | **618,336** | **Replace**: 1 quad (or 4 quads) with an alpha-clipped dot-pattern texture (generate in Blender via bake or draw in code) | ≤ 20 |
| `Cube.005` | Service **building** (louvred facade, dark flat roof, glazing) | 3.5 × 5.9 × 2.3 | 3,608 | Keep | ≤ 3,600 |
| `Cube.002` (+ similar) | Large boxes (12.6 × 30.6 × 5 → 1.3 × 3.1 × 0.5 m) (inferred: planter/bench/steps) | — | 188 | Keep if visible, else drop | ≤ 200 |
| `Cube_Cube.001/.002/.003/.121` | **4 tables** | 0.74 × 2.2 × 0.73 | 196 each | Keep, instance | 196 each |
| `Cube_Cube.004..026, .114` (24 objects, 24,080 tris each; the 28-object `Cube_Cube` group = 24 chairs + the 4 tables above) | **Blue chairs** (24, i.e. 6 per table) | 0.59 × 0.60 × 0.94 | 24,080 each | **Replace by one `Prop_ChairBlue` mesh**, decimated to ~1,200 tris, instanced ×24 | ≈ 1,200 |
| `Cylinder.001..003` | Posts/columns or door frames (12 × 1.25 × 19 → 1.2 × 0.13 × 1.9 m) (inferred) | — | 1,748 each | Keep or drop if not visible | ≤ 600 each |
| `door_Plane`, `door.001_Plane.001` | Doors (0.05 × 0.73 × 1.5 m after scale) | — | 276 | Keep | 276 |
| `Cube`..`Cube.009` (8 objects 6.76 × 0.49 × 9.79 → 0.68 × 0.05 × 0.98 m) | Panels (inferred: window/louvre panels) | — | 60 | Keep | 60 |
| `Handle_1*`, `Base_1*` (4 + 4) | Door handles and bases (aluminium) | — | 3.3k–6.4k each | Decimate heavily or drop | ≤ 300 each |
| `cabin` | Empty-ish mesh, 0 tris | — | 0 | Drop | — |
| `Area` (light 157 W), camera | — | — | — | Discard | — |

Missing: `stone-texture.jpg` (supplied), `studio016.hdr` (missing, not needed). Patio materials are mostly flat colours (beige, grey, blue_trim, white, Aluminium, glass), which export fine.

**Gameplay on the patio:** 4 tables × 6 chairs. Use 2 chairs per table as the gameplay seats (8 total, `SeatPoint` 0..7), the rest are decoration. Cull the extra chairs if the budget demands (min: 8 gameplay + 8 decoration).

### 3.3 `part 2_final.blend` — booth pods (backdrop; P1 priority 3)

Scale ×0.2. 59 objects: 48 chairs (5,369 tris each, blue, 257,712 total), 2 plant meshes `leaves`/`leaves.005` (6,155 each, include pots), 9 `Cube` slat/partition elements (6 with Geometry-Nodes modifiers → apply; 420 tris each; 2 big `Cube.009/.014` 1,938 tris each = the slatted walls). Preview: slatted pod walls enclose tables with chairs and large potted plants.
Decision: keep 3–4 pods' worth only if they are visible from the patio (they are far behind it as a backdrop). Decimate chairs to ~800 tris, instance. Target total ≤ 25,000 tris. If time is short, replace the whole file by 6 slatted-wall quads with an alpha texture.
Missing: all leaf/pot PBR textures, `part2_partition_and_blocks_textures.jpg`, `Part4_marble_texture.jpg`. Use flat colours (green leaves, terracotta pot).

### 3.4 `part 4_final.blend` — terrace (backdrop; P1 priority 3)

Scale ×0.2. 20 objects, 49,263 tris. `partition.046` is a 39.6 m slatted screen (1,570 tris), `rail_left_1/right_1` and `top_left_1/right_1` are pergola rails/roof (648/24), `seat_right_2/4/5`, `pillar_left_1` are marble terraced seating (2,122 each), `light_right_3.xx` ×6 are emissive light strips (2,122 each), `Pot1..3` + `Cube.033/.034` are plants/pots (6,155 / 3,332; Geometry-Nodes + Solidify → apply). Keep all; it is already cheap. Apply modifiers. Target ≤ 30,000 total. Preview: stepped terrace behind a long louvred wall.

### 3.5 `part 1_final.blend` — trees and masses (backdrop only; P1 priority 4)

156 objects, 2.69M tris. Dominated by 121 `Cube_Cube.xxx` + 18 `Chair` objects, **24,080 tris each**, all 0.25 × 0.25 × 0.39 m (tables and chairs in rows — red chairs). **Cut all of them.** Keep: 3 `pillar.001..003` (16,032 tris each, 3.5 × 4.0 × 3.8 m; the branching **tree canopies** seen in the preview — decimate to ~2,500 each); `Cube.001_Cube.004` (21,392 tris, a column/lamp "snow" material — decimate or drop); the big blocks `Cube.002` (4.1 × 2.9 × 2.1 m, 12 tris) and `Plane`/`Plane.003` walls/ground (cream wall; 4,400-tri plane — replace with a quad); `PESU` (FONT object, 2 m wide 3D text "PESU", 1,060 tris — keep as a sign, convert to mesh, emissive), `Pot`/`leaves`/`Object_2_Object_0` (8,064-tri unknown prop, probably a statue or planter, decimate or drop). Target total ≤ 15,000 tris. Scale: unresolved (§2) — decide after looking at the trees against Part 3's building; they are backdrop so a rough size is acceptable.
Missing textures: 20 of 21 images (grass, leaves PBR, a WhatsApp image). Use flat colours and the supplied `green-grass-texture.jpg`.

### 3.6 `washrooms.blend` — optional (P1 priority 5)

Authored at ~1/12 scale (whole room 0.48 × 0.44 × 0.18 m). 665,988 tris: 6 hand basins × 62,336, 12 door cylinders (`Cylinder`..`Cylinder.018`) × 20,176, 6 urinal/dividers × ~6,925, 7 toilets × 4,111, 6 tap fittings (`Circle.00x`) × 8,796, plus mirror layers and a ground plane. A stray zero-size object `URINAL_AND_DIVIDER_Cube.338` sits at z = −1.3 (delete). If used: scale ×12, replace doors with boxes, decimate basins to ~1,500, drop taps, total ≤ 8,000 tris, place as a small building at the edge of the backdrop with an openable door purely as decor. Stretch goal §6.7(1) would reuse one basin.
Missing: `map.png` from another student's machine (irrelevant; do not use).

### 3.7 `chair_blue_flower_pot.blend`

3 objects: two `Plane` meshes with Skin + Subsurf modifiers (chair frame / plant) and a `Torus`. 10,768 tris evaluated. Probably the chair + pot test models that became Part 2's chairs. Optional; skip unless a prop is needed.

---

## 4. Textures and materials strategy

- **Rule:** flat-colour URP/Lit materials for everything first (colours copied from each Blender material's base colour via script), then add the supplied tiling textures where they clearly belong, then CC0 textures for gaps. Result must look intentional, not broken.
- **Supplied textures → use:** `tile.jpeg` (interior floor), `floor.png` (canteen floor brick/tile), `seating_granite.jpeg` and `stone-texture.jpg` (patio ground, counters, terrace seats), `green-grass-texture.jpg` (grass strips), `PES_text.jpeg` (signage, 69 KB), `pot_yellow.jpeg` (pots), `throne.png` (4.2 MB — probably a decorative picture; may be used as a framed poster or ignored; resize to ≤ 1024 if used).
- **CC0 gap-fillers:** ambientCG.com and polyhaven.com (CC0). Record every file in `docs/CREDITS.md`.
- **Texture import:** max 1024 px (2048 only for the donut/shake/cake label atlas if used), sRGB for colour, normal-map flagged, compression normal quality (ASTC 6×6 Android / DXT5-BC7 PC), mipmaps on, no Read/Write.
- **Atlas the small props:** donut, cola, shake, cake, cup, tray share one 1024 atlas + one material → one SRP Batcher batch. Build the atlas in Blender (bake or hand-assign flat colours + one label region).
- **Procedural shaders** (brick, noise marble, etc.) do not export. For the four hero surfaces needing a pattern (floor brick, marble) bake to a 1024 texture or substitute a supplied tile.
- **Glass:** URP Lit, Surface Type Transparent, alpha 0.25, only on the display case fronts (2 quads). Fridge door glass: opaque.

## 5. How to reduce the heavy meshes

### 5.1 General recipe (Blender, scripted)
1. `bpy.ops.object.convert(target='MESH')` (applies modifiers incl. Geometry Nodes). Use evaluated mesh to be safe.
2. Delete loose geometry, doubles (`merge_by_distance` 0.0001), zero-area faces, hidden interior faces on boxes.
3. Decimate (Collapse) with `ratio = targetTris / currentTris`, `use_symmetry=False`, keep `Triangulate` off; recalc normals; smooth shading with auto-smooth/sharp edges by angle 35°.
4. Apply scale; set origin as required (hinge for doors, base-centre for items).
5. Re-check tris via evaluated mesh; the script FAILS (exit 1) if any module exceeds its budget.

### 5.2 The donut specifically
`Dough` is 176,864 triangles because of sprinkles + pearls + sculpted glaze. Order of attempts: (a) Collapse-decimate the glaze+dough to ~1,500 tris, delete sprinkles, add 3 low-poly sprinkle-colour decals via vertex colour; (b) if that looks bad, build a torus (24×12 = 576 tris) for dough + a slightly larger glaze torus (576 tris) with a baked normal map from the high-poly (Cycles bake, selected-to-active), plus 4 small low-poly pearls (spheres 32 tris); (c) fallback: vertex-colour torus. The donut is ~0.3 m wide and 1 m from the player's eyes, so (b) is plenty. Export ONE donut; Unity spawns 12.

## 6. New props P1 must model (none exist in the source)

`Prop_Tray` (0.45×0.03×0.30 m with a 0.01 m raised lip, ≤ 300 tris, flat colour); `Prop_TrayRack` (≤ 200); `Item_Cup` (paper/plastic cup 0.08×0.10 m, ≤ 300; plus a `Water` visual state = cup with inner water disc, can be a child mesh toggled by script); `Prop_CupDispenser` (≤ 200); `Prop_TicketRail` (order rail, ≤ 150 — UI is placed by P2); `Marker`s are Unity-side. Style: low-poly, flat shaded with matching palette to the canteen.

## 7. Export modules (one FBX each; names are contractual)

All exports: Blender FBX, `apply_scale_options='FBX_SCALE_ALL'`, `axis_forward='-Z'`, `axis_up='Y'`, `use_mesh_modifiers=True`, `bake_space_transform=True`, `object_types={'MESH'}`, `mesh_smooth_type='FACE'` or `'OFF'` (consistent), `add_leaf_bones=False`, textures not embedded (Unity materials rebuilt). Origin conventions in the last column.

| FBX | Contents | Budget (tris) | Origin / pivot |
|---|---|---|---|
| `Env_CanteenShell.fbx` | cafeteria shell, display cases, counters, plates, fan, lights, fridge body, shelves, hinge/handles (without doors) | ≤ 6,000 | world-aligned, shell centre at (0,0,0), floor at y=0 |
| `Prop_FridgeDoorUpper.fbx`, `Prop_FridgeDoorLower.fbx` | each door + its handle | ≤ 300 each | pivot on the hinge edge, door closed = identity rotation, axis = local Y |
| `Prop_WaterCooler.fbx` | stand + bottle + pump head as separate child `PumpHead` | ≤ 3,200 | base centre; `PumpHead` pivot at its press axis |
| `Item_Donut.fbx` | one donut | ≤ 2,000 | centre of mass, y-up |
| `Item_Cola.fbx` | one can | ≤ 800 | base centre |
| `Item_Shake.fbx` | one glass with cream | ≤ 1,500 | base centre |
| `Item_Cake.fbx` | one cake | ≤ 3,000 | base centre |
| `Item_Cup.fbx` | cup (+ water child) | ≤ 400 | base centre |
| `Prop_Tray.fbx`, `Prop_TrayRack.fbx`, `Prop_CupDispenser.fbx` | new props | ≤ 300/200/200 | base centre |
| `Env_Patio.fbx` | ground, fence quad(s), service building, doors, panels, columns (no tables/chairs) | ≤ 8,000 | patio centre (0,0,0), floor y=0 |
| `Prop_TablePatio.fbx` | one table | ≤ 250 | floor-centre |
| `Prop_ChairBlue.fbx` | one chair | ≤ 1,200 | floor-centre, facing +Z |
| `Env_PodsBackdrop.fbx`, `Env_Terrace.fbx`, `Env_Skyline.fbx` | Parts 2, 4, 1 reduced | ≤ 25k / 30k / 15k | each module origin at its ground-centre |
| `Env_Washroom.fbx` (optional) | washroom building | ≤ 8,000 | ground-centre |
| `Prop_PlantPot.fbx` | one pot+plant | ≤ 1,500 | base centre |

**Total static budget:** canteen ≈ 20k + patio ≈ 8k + 24 chairs × 1.2k (≈ 29k) + 4 tables ≈ 1k + backdrop ≈ 70k ≈ **135k** tris. Comfortably inside §8 of the master spec; the remainder of the budget is for items, trays, customers (≤ 8 × 300 tris) and UI.

## 8. Unity import settings (P1)

Model tab: Scale Factor 1, Convert Units on, Import BlendShapes off, Import Cameras/Lights off, Mesh Compression Medium, Read/Write off, Generate Colliders off (colliders are P2's/P1's primitives; mesh colliders only for the canteen shell and patio ground, and only if convex-decomposable is impractical), Optimize Mesh on, Normals Import, Tangents Calculate, **Generate Lightmap UVs on for every `Env_*`**, Swap UVs off. Rig: None. Animation: off. Materials: location "Use External Materials (Legacy)" then remap by name to `Assets/Art/Materials/M_*.mat`. Static flags on environment: Contribute GI, Batching Static, Occluder/Occludee, Navigation Static (only walkable floors).

## 9. Pipeline script spec: `tools/blender/build_world.py` (P1 writes)

Inputs: `--source <dir with Outdoor/>`, `--out <dir for FBX/PNG>`, `--config tools/blender/config.json` (per-file scale, per-object keep/cut/target-tris/pivot map). Behaviour: for each module, open the source `.blend` (`--disable-autoexec`), build the module from the config (select by object name, with regex for numbered duplicates), apply modifiers, scale, clean, decimate, set pivots, write `.fbx` and a `report.json` (tris, dims, bounding box, missing textures). Exit code non-zero if any budget or dimension check (e.g. chair height not in 0.85–1.0 m) fails. Idempotent: rerunning overwrites outputs. Prints one summary table. Keep it a single file of < 400 lines; no external Python dependencies beyond Blender's bundled Python. Run: `blender -b --factory-startup --python tools/blender/build_world.py -- --source source --out unity/CanteenRush/Assets/Art/_export`.

## 10. Known gotchas for the pipeline

- Blender 5.2's Python API changed `bpy.data.objects.remove()` keyword arguments (no `remove_data`). Use `bpy.data.objects.remove(obj)`.
- FBX export in Blender 5.x: verify the operator signature in the Blender 5.2 docs (`bpy.ops.export_scene.fbx`) — option names above are the long-standing ones.
- Object names contain `+` and `.NNN` suffixes (e.g. `tilet.001_Gintare+mazas+wc+kambarys.001`); sanitise to `[A-Za-z0-9_]` for FBX.
- Geometry-Nodes modifiers need the node group available; if one fails to evaluate, bake the evaluated mesh from the viewport and note it in the report.
- `Map #4` and `Map #4.001` are the only packed images (4096²). They are label textures for the shake and cake; downsample to 1024 before shipping.
- The canteen `cup` object has Subsurf lvl 2 on an empty mesh — do not try to rescue it.
- All source files reference absolute or relative paths on the authors' computers. Never "fix" paths by editing the sources; rebuild materials in the export step.
- The .blend files are untrusted: always `--disable-autoexec`; never commit them (they are git-ignored).

---

## 11. Build reality (supersedes §7 where they differ) — 2026-10-30

What `tools/blender/build_world.py` actually produces today (numbers in `docs/PIPELINE_STATUS.md`):

- **Multi-node modules.** `Env_Canteen.fbx` is ONE FBX with 11 mesh nodes (`Shell`, `DisplayCase_A/B`, `Counter_A/B`, `Plates`, `FridgeBody`, `Door_Upper`, `Door_Lower`, `Fixtures`, `WaterCooler`) that all keep their positions in the canteen's source frame, plus 18 marker empties (`Spawn_Donut_00..11`, `Spawn_Cola_00..01`, `Spawn_Shake_00..01`, `Spawn_Cake_00`, `Spawn_Cup_00`). Place the FBX at the Unity origin; spawners and doors are placed by reading the imported nodes/markers. The fridge doors are separate nodes whose **pivot is the hinge axis**, closed = identity rotation.
- **`Env_Patio.fbx`** (7 mesh nodes + `Table_00..03` + `Chair_00..23` markers, base-centre anchors, no orientation) at ×0.1. The 618,336-triangle fence is a 12-triangle box (dot pattern lost; texture it).
- **Single-node props** (`Item_*`, `Prop_*`) are centred (`Item_Donut`) or base-centred at the origin; `Item_Cup.fbx` has two nodes (`Item_Cup`, `Cup_Water`).
- **Water cooler:** bottle and pump are one 10k-triangle mesh and cannot be split. P2's pump interactable must be an invisible collider over the pump head, not a mesh node (the earlier `PumpHead` child in §7 does not exist).
- **Canteen shell is an open-front kiosk** (open toward −X; back wall at +X). See `docs/LAYOUT.md`.
- **Generated props** (tray, rack, cup + water disc, dispenser) are built procedurally by the script (not from any source file) and are intentionally crude flat-colour meshes.
- **Materials:** each module has `Assets/Art/Materials/source/<module>.materials.json` with Blender base colours/textures. The FBX also carries base colours; Unity's URP importer converts them. Missing label textures mean the shake is plain brown and the cake is dark grey until recoloured.
- **Not built yet:** `Env_PodsBackdrop`, `Env_Terrace`, `Env_Skyline`, `Env_Washroom`, `Prop_PlantPot`.
