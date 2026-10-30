# VIVA PACK — be able to defend every line

Owner: P3 (personalise with real numbers and screenshots at M5). Everyone reads all of it. The brief says AI tools are allowed but **understanding will be tested**. The questions below are what an examiner would plausibly ask for each graded feature. Answers are written for THIS design; edit them to match what is actually built. Anything in `[brackets]` is a number to fill in from the finished project.

## A. 4-minute demo script (rehearse twice, identical every time)

| Time | Action | What to say (one sentence) | Feature shown |
|---|---|---|---|
| 0:00 | Menu board in the simulator; point and click Play | "VR on a Quest-class budget, shown here through the XR Device Simulator." | UI, platform |
| 0:20 | Tutorial step 1: grab a donut from the display case | "Items are grab-interactables with colliders and a pooled respawn; note the haptic flash on the left controller." | Interaction, haptics HUD |
| 0:40 | Walk to the fridge, pull the door open; grab a cola | "The door is a hinge joint with limits; the click you hear and feel is the latch detent at 8°." | Interaction, spatial audio, haptics |
| 1:00 | Fill a cup at the cooler (hold under tap, press pump) | "Fill level drives both the pitch of the water loop and a rising haptic ramp." | Audio + haptics coupling |
| 1:20 | Load donut + cola on the tray; show the tilt warning by tipping it | "Tilt past 20° gives a growing buzz; past 35° the items slide off." | Haptics ramp, physics |
| 1:40 | Order bell rings from a table; turn toward the sound; carry tray out | "The bell is emitted at the customer's seat with a pitch per table, so you find who ordered by ear." | **Spatial audio** |
| 2:20 | Pass through the door | "Snapshot cross-fades from the indoor mix to the outdoor mix; reverb zone only inside." | Spatial audio, lighting |
| 2:30 | Serve; show score popup, perfect-combo double pulse | "Score = items × combo + speed bonus − penalties; all tunable in a ScriptableObject." | Game logic |
| 2:50 | Let one order expire (angry bubble + long buzz) | "Failure has its own sound and haptic." | Feedback |
| 3:10 | Point at the sky / long shot of patio | "The sun lerps from midday to golden hour over the round; GI is baked, only the sun and ambient change." | **Lighting** |
| 3:30 | Results board, stars; show Stats window | "[X] k triangles, [Y] draw calls, [Z] ms on this machine; Quest budget is 250k / 150 / 11 ms." | **Optimisation** |
| 3:50 | Questions | | |

## B. Questions and model answers

### Concept, scope, platform
1. **Why VR and why a simulator?** The brief allows any of mobile/headset/PC and AR/VR/MR plus emulators. VR gives native hands-on interaction, spatial audio and controller haptics; the XR Device Simulator lets us demo without owning a headset. We budgeted for Quest-class hardware so the optimisation claim is meaningful.
2. **Why canteen service?** Uses the richest asset in our floor (canteen) in a way that needs interaction, audio cues and haptics naturally, and a short walk to the patio gives the other parts a purpose. Other teams share the floor, so ours differs in mechanics (the spatial order-bell, haptic language, day-to-golden-hour arc).
3. **What did you cut and why?** Washroom gameplay (asset at 1/12 scale, 666k triangles), character animation (no rigs supplied), save/load, multiplayer. The cut list is in the master spec §17.
4. **Why only part of the supplied floor?** Parts 1, 2 and 4 are used as backdrop only because they are mostly chairs and decoration; Part 3 has the open layout and 4 tables.

### Assets and pipeline
5. **What was wrong with the supplied files?** ~7M triangles; five different scales; missing textures (they pointed to files on the authors' machines); procedural Geometry-Nodes modifiers and node shaders that do not export; no colliders. See Asset Bible §1–2.
6. **How did you combine files authored at different scales?** Measured reference objects (chair 9.43 m tall in Part 3 → ×0.1; washroom door 0.17 m → ×12; etc.), applied scale in Blender, verified against a 1.8 m reference figure, recorded factors in `config.json`.
7. **How did you cut triangles?** Replaced 12 donuts of 176,864 triangles with one ~2,000-triangle mesh instanced 12×; replaced a 618k-triangle perforated fence with an alpha-cut quad; replaced 24 chairs of 24,080 triangles with one ~1,200-triangle chair instanced; applied modifiers then Collapse-decimated hero props. The rebuild is a script, so it is repeatable.
8. **What is decimation and what are its limits?** Collapsing edges to reduce faces while preserving shape (quadric error); loses small details such as sprinkles; mitigated with baked normals/vertex colour.
9. **Why FBX and why those export settings?** Unity imports FBX natively; forward −Z, up Y converts Blender's Z-up to Unity's Y-up; apply transforms so scale is 1.0 in Unity.
10. **How did you handle missing textures?** Flat-colour URP materials from the Blender base colours, the supplied tile/stone textures where they fit, CC0 textures (ambientCG/Poly Haven) for gaps, atlas for small props. Listed in `CREDITS.md`.

### Interaction
11. **How do grabbing and the tray work?** XR Interaction Toolkit: items have a Rigidbody and an XRGrabInteractable; the tray has four socket interactors that snap items into slots; only socketed items count for an order.
12. **How does the fridge door work?** A HingeJoint with 0–110° limits on the door; the handle is the grab point; angle drives `FridgeDoorMoved` events and a detent at 8°.
13. **Why sockets rather than parenting?** Sockets give snapping, haptic hooks and clean enter/exit events and keep physics consistent.
14. **How is an order evaluated?** Multiset comparison of requested vs tray items (`OrderMatcher`), then `ScoreCalculator` applies formulas from `GameConfig`. Both are plain C# classes covered by unit tests.
15. **How do you avoid motion sickness?** Snap turn, comfort vignette while moving, no acceleration, seated mode; locomotion speed 1.8 m/s.

### Spatial audio
16. **What makes audio "spatial"?** Every in-world source is a 3D AudioSource with distance rolloff, direction panning (HRTF with the spatializer plugin if installed), per-cue min/max distances, occlusion low-pass when walls block line of sight, indoor/outdoor mixer snapshots, and a reverb zone in the canteen.
17. **What is the signature spatial mechanic?** The order bell plays at the customer's seat with a pitch per table, so you locate the customer by ear.
18. **How do you keep audio cheap?** 12 pooled one-shot sources, ≤ 16 voices, mono 3D clips, compressed Vorbis, short loops.
19. **What are HRTF and attenuation curves?** HRTF filters sound per ear using direction to emulate head/ear shape; attenuation curves define volume vs distance (logarithmic here to mimic physical falloff).
20. **How did you implement occlusion?** Raycast listener→source against the Environment layer every 0.1 s; if blocked, ease the source's low-pass cutoff to 1.2 kHz and drop volume 30%.

### Haptics
21. **What is your haptic design principle?** Short = contact, ramp = continuous process, long = failure; every haptic is paired with a sound. Table of presets in master spec §11.
22. **How do you demo haptics without a headset?** The haptic debug HUD flashes and logs each pulse with its amplitude and duration; the same service sends real impulses on a device.
23. **How are haptics decoupled from gameplay?** Gameplay raises events; `HapticsEventBinder` maps them to presets; the service talks to the XR input system. Gameplay never knows haptics exists.

### UI
24. **Why world-space, diegetic UI?** In VR screen-space overlays cause discomfort and break presence. Order tickets live on a rail, bubbles above customers, score on the wrist. Large icons, high contrast, minimum text size.
25. **How do VR UI clicks work?** A `TrackedDeviceGraphicRaycaster` on each canvas and the XR UI Input Module let the ray interactor click Unity UI.

### Lighting
26. **Baked vs realtime?** Architecture is baked into lightmaps (cheap at runtime); one realtime sun gives shadows on moving objects; light probes light dynamic items; a reflection probe handles shiny props.
27. **How does the lighting change over the round?** `SunCycle` lerps the realtime sun's elevation and colour temperature, ambient and fog, because baked GI is fixed.
28. **Why area lights in the canteen?** They exist in the source and give soft counter lighting; Unity bakes area lights (no realtime area lights in URP).

### Optimisation
29. **What are your performance numbers and targets?** [X] triangles visible, [Y] draw calls, [Z] ms; targets 250k / 150 / 11 ms (Quest 2, 72 Hz).
30. **What did you do for performance?** URP forward, MSAA 4×, SRP Batcher, static batching, GPU instancing for chairs, LODs, occlusion culling, compressed textures ≤ 1024, one realtime light, baked lightmaps, pooling, no per-frame allocations, fixed timestep 1/72.
31. **What is SRP Batcher / draw call / overdraw?** SRP Batcher caches material constants to cut per-draw CPU cost; a draw call is a CPU→GPU render command; overdraw is shading the same pixel multiple times (bad on mobile tiled GPUs, which is why glass is limited).
32. **How would it run on a phone?** Same URP asset reduced; the project is already within Quest-2 budgets which exceed mid-range phones.

### Engineering
33. **How is the code structured?** Assembly definitions; `Cr.Core` has data and pure logic; Gameplay, Audio, Haptics and UI depend only on Core and communicate through a static event bus. Data-driven via ScriptableObjects.
34. **Why events instead of direct calls?** Decoupling lets three people work in parallel; audio/haptics can be removed without touching gameplay; easy to test.
35. **How did you test?** EditMode unit tests for the logic (TDD), PlayMode smoke test for a full round, manual QA checklist, profiler checks.
36. **How did three people avoid conflicts?** Single-writer ownership of scenes/prefabs, art-swap rule (`Visual` child), small PRs, Git LFS for binaries.
37. **What did AI do and what did you do?** (Everyone: personal answer from your `LOG_Pn.md`: what you asked, what you changed or rejected, what bug you found that the AI missed.)

## C. Cross-explain drill (M3 and M5)
Each person explains another's system in 3 minutes with the other asking one hard question. Suggested pairings: P1 explains the event bus; P2 explains the lighting bake; P3 explains the XR grab/socket flow.

## D. Demo-day checklist
Charged laptop on power; build tested on the viva machine; simulator controls memorised; headphones; backup screen recording of a full round on the desktop; `Stats` window visible; QA checklist printed; one-page architecture diagram; each person knows the three numbers (triangles, draw calls, ms).
