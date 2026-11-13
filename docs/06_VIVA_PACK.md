# VIVA PACK — be able to defend every line

Owner: P3. Everyone reads all of it. The brief says AI tools are allowed but **understanding will be tested**. The questions below are what an examiner would plausibly ask for each graded feature, answered for what is actually built (status 2026-11-13). Numbers come from `docs/PERF_REPORT.md` (P1, Windows player build, Intel Iris Xe). Re-measure after the full gameplay integration and update the three numbers in section E.

Screenshots for slides or the printout: `docs/screenshots/player_counter.png`, `player_door.png`, `player_patio.png` (the three measurement positions), `editor_interior_warm.png` (warm inside, cool outside), `editor_patio_day.png` vs `editor_patio_golden_hour.png` (the round's lighting arc).

## A. 4-minute demo script (rehearse twice, identical every time)

| Time | Action | What to say (one sentence) | Feature shown |
|---|---|---|---|
| 0:00 | Menu board in the simulator; point and click Play | "VR on a Quest-class budget, shown here through the XR Device Simulator." | UI, platform |
| 0:20 | Tutorial step 1: grab a donut from the display case | "Items are grab-interactables with colliders and a pooled respawn; the simulator cannot vibrate, so every haptic pulse shows on this HUD: colour = preset, size = strength, fade = duration." | Interaction, haptics HUD |
| 0:40 | Walk to the fridge, pull the door open; grab a cola | "The door is a hinge joint with limits; the click you hear and feel is the latch detent at 8°." | Interaction, spatial audio, haptics |
| 1:00 | Fill a cup at the cooler (hold under tap, press pump) | "Fill level drives both the pitch of the water loop and a rising haptic ramp." | Audio + haptics coupling |
| 1:20 | Load donut + cola on the tray; show the tilt warning by tipping it | "Tilt past 20° gives a growing buzz; past 35° the items slide off." | Haptics ramp, physics |
| 1:40 | Order bell rings from a table; turn toward the sound; carry tray out | "The bell is emitted at the customer's seat with a pitch per table, so you find who ordered by ear." | **Spatial audio** |
| 2:20 | Pass through the door | "Snapshot cross-fades from the indoor mix to the outdoor mix; reverb zone only inside." | Spatial audio, lighting |
| 2:30 | Serve; show score popup, perfect-combo double pulse | "Score = items × combo + speed bonus − penalties; all tunable in a ScriptableObject." | Game logic |
| 2:50 | Let one order expire (angry bubble + long buzz) | "Failure has its own sound and haptic." | Feedback |
| 3:10 | Point at the sky / long shot of patio | "The sun lerps from midday to golden hour over the round; GI is baked, only the sun and ambient change." | **Lighting** |
| 3:30 | Results board, stars; show Stats window | "58k to 100k visible triangles, at most 88 visible renderers and 3.5 to 7.8 ms a frame on an integrated GPU; the Quest budget is 250k / 150 draw calls / 11 ms. Draw calls themselves were not measured; that is the honest gap." | **Optimisation** |
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
16. **What makes audio "spatial"?** Every in-world source is a 3D AudioSource (`spatialBlend 1`, doppler 0) with a custom distance rolloff, direction panning, per-cue min/max distances from the cue table, occlusion low-pass when walls block line of sight, Indoor/Outdoor mixer snapshots (0.4 s cross-fade at the door), and an `AudioReverbZone` (Room) inside the kiosk only. We did **not** install an HRTF spatializer (Meta XR Audio SDK): it needs a package registry change and nobody has a headset to hear it; sources are flagged `spatialize`, so it is a one-setting upgrade. Say this honestly.
17. **What is the signature spatial mechanic?** The order bell plays at the customer's seat with a pitch per table, so you locate the customer by ear.
18. **How do you keep audio cheap?** 12 pooled one-shot sources (no Instantiate per sound; when all are busy we steal the one that is quietest at the listener), 16 real voices in Project Settings, mono 3D clips, Vorbis compression, short loops (8–17 s), loops pause when gated to silence, DSP buffer 512 (Good latency).
19. **What are HRTF and attenuation curves?** HRTF filters sound per ear using direction to emulate head/ear shape; attenuation curves define volume vs distance (logarithmic here to mimic physical falloff).
20. **How did you implement occlusion?** `AudioOcclusion`: a linecast listener→source against the Environment layer every 0.1 s; if blocked, ease the source's low-pass cutoff to 1.2 kHz (interpolated in log-frequency, because pitch perception is logarithmic) and drop volume 30 %, over 0.25 s so it never clicks. It runs on ambience emitters and customer barks, not on the order bell (the bell must stay findable).

### Haptics
21. **What is your haptic design principle?** Short = contact, ramp = continuous process, long = failure; every haptic is paired with a sound. Table of presets in master spec §11.
22. **How do you demo haptics without a headset?** `HapticsDebugHud` (F9): a marker above each controller and a corner square that flash in the preset's colour, sized by amplitude and fading over the duration, plus an 8-line log (`R Grab 0.25 amp 0.040s (HUD only)`). The same service calls XRI's `HapticImpulsePlayer.SendHapticImpulse(amplitude, duration)` on real controllers, and the HUD hides itself when a controller that supports impulses is detected.
23. **How are haptics decoupled from gameplay?** Gameplay raises events; `HapticsEventBinder` maps them to presets; the service talks to the XR input system. Gameplay never knows haptics exists.

### UI
24. **Why world-space, diegetic UI?** In VR screen-space overlays cause discomfort and break presence. Order tickets live on a rail, bubbles above customers, score on the wrist. Large icons, high contrast, minimum text size.
25. **How do VR UI clicks work?** A `TrackedDeviceGraphicRaycaster` on each canvas and the XR UI Input Module let the ray interactor click Unity UI.

### Lighting
26. **Baked vs realtime?** Architecture is baked into lightmaps (cheap at runtime); one realtime sun gives shadows on moving objects; light probes light dynamic items; a reflection probe handles shiny props.
27. **How does the lighting change over the round?** `SunCycle` lerps the realtime sun's elevation and colour temperature, ambient and fog, because baked GI is fixed.
28. **Why area lights in the canteen?** They exist in the source and give soft counter lighting; Unity bakes area lights (no realtime area lights in URP).

### Optimisation
29. **What are your performance numbers and targets?** 58k–100k triangles visible per view, 109k in the whole scene; ≤ 88 visible renderers, ≤ 60 materials, 1 shader (draw calls not measured, SRP Batcher on); 3.5–3.8 ms (Quest-class quality level) and 7.6–7.8 ms (PC level) per frame on Intel Iris Xe at 1600×900; build 107 MB. Targets: 250k / 150 / 11 ms (Quest 2, 72 Hz). Caveat from the report: mono, not stereo, and not a Quest GPU; at Quest 2 resolution the same GPU would need roughly 12 ms.
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

## E. The three numbers everyone must know
- **Triangles:** 58k–100k visible per view (budget 250k); 109k in the whole scene (budget 600k).
- **Frame time:** 3.5–3.8 ms on the Quest-class quality level, 7.6–7.8 ms on the PC level (Iris Xe, 1600×900, mono). Budget 11 ms.
- **Draw calls:** not measured (headless build); proxies are ≤ 88 visible renderers, ≤ 60 materials, 1 shader, SRP Batcher on, static batching on 98 % of triangles. Budget 150.

## F. Spatial audio as built (cue list)
| Cue | Plays on | Where | Range (m) | Notes |
|---|---|---|---|---|
| amb_fridge_hum | always | fridge | 1–9 | louder as the door opens; occluded |
| amb_canteen_room | inside the kiosk | 2D bed | – | fades in/out with the Indoor zone |
| amb_patio_birds | always | 4 emitters around the patio | 3–25 | rooftop wind + birds; random start offsets |
| amb_patio_crowd | CustomerCount | patio centre | 3–20 | volume = customers / 4 |
| fridge_latch | door passes 8° / hits the open limit | fridge | 1–8 | open and close variants; duller thunk at the limit |
| pump_press | PumpActivated(true) | pump | 1–6 | |
| water_fill_loop | CupFilling | under the spout | 1–6 | pitch 0.9 → 1.2 with the level; stops if events stop for 0.2 s |
| grab_soft / release_soft | ItemGrabbed / Released | item | 1–5 | 3 variants; pitch by ItemDef.AudioMaterial |
| tray_clink | socketed, impacts, tray placed, tilt rattle | item/tray | 1–8 | volume ∝ impact speed (0–4 m/s), ±5 % pitch |
| item_drop / item_drop_glass | ItemDropped | floor | 1–12 | glass for the Shake |
| **order_bell** | OrderPlaced | **the customer's seat** | 2–30 | **pitch +1 semitone per seat, farther = higher** |
| customer_happy / customer_angry | OrderServed / OrderExpired | the seat | 2–20 | Voice bus; occluded |
| footsteps_tile / footsteps_stone | Footstep | 2D | – | surface from P2's raycast tag |
| ui_click / ui_confirm / ui_error | UI hover/click, perfect serve, expiry or empty tray | 2D | – | UI bus |
| music_loop | Wave 1–3 | 2D | – | fades over 1.5 s; low-passed indoors by the snapshot |
| round_end_sting | Results | 2D | – | |

Mixer: UI 0 dB, SFX −6, Voice −6, Ambience −24 (18 under SFX), Music −20 (−23 indoors). Indoor snapshot: Master low-pass 12 kHz; Outdoor: open (22 kHz).

## G. Haptic language as built
| Preset | When | Strength / length | Hand |
|---|---|---|---|
| Grab | pick up | 0.25 / 40 ms | grabbing hand |
| Release | let go | 0.10 / 30 ms | releasing hand |
| SocketSnap | item snaps onto the tray | 0.35 / 60 ms | last holder |
| DoorLatch | fridge door passes 8° | 0.45 / 50 ms | last holder |
| DoorStop | door hits its open limit | 0.60 / 80 ms | last holder |
| PumpPress | pump pressed | 0.50 / 70 ms | the hand not holding the cup |
| WaterFill | while filling | ramp 0.10 → 0.45 with the level | cup hand |
| TrayTiltWarn | tray tilted > 20° | ramp 0.15 → 0.70 from 20° to 35° | tray hand |
| ServeOk | perfect serve | 2 × (0.50 / 60 ms), 50 ms gap | both |
| ServePartial | imperfect serve | 0.35 / 120 ms | both |
| Spill | item hits the floor | 0.85 / 100 ms | last holder |
| OrderExpired | customer gives up | 0.70 / 300 ms | both |
| UiTick | hover or click a button | 0.15 / 20 ms | right (pointing) hand |

Rules: short = contact, ramp = a continuous process, long = failure; never two pulses within 30 ms on one hand; ramps are coalesced to one impulse every 50 ms; every preset has a paired sound (`docs/LOG_P3.md`).

## H. P3 deep-dive questions
38. **Why is audio a "listener"?** `AudioEventBinder` only subscribes to `GameEvents` in `OnEnable` and unsubscribes in `OnDisable`; `Cr.Audio` references only `Cr.Core`, so it cannot call gameplay even by accident. Delete the Audio prefab and the game still plays, silently.
39. **How does the order bell help the player?** It plays at `Order.SeatPosition`, 1.2 m up, with `pitch = 2^(seat/12)`. Seats are numbered nearest table first, so you hear which side (panning) and how far (loudness and pitch).
40. **Why a custom rolloff curve?** Unity's logarithmic curve never reaches zero, so distant sources keep using voices. Ours follows 1/distance (−6 dB per doubling) and fades to silence over the last 15 % of each cue's max distance.
41. **How does voice stealing work?** If all 12 pooled sources are busy, we estimate each one's loudness at the listener (volume × rolloff gain at its distance) and reuse the quietest, so a far clink is cut before a near bell.
42. **How do you know which zone the player is in?** Every 0.1 s, `AudioService` asks each `AudioZoneTrigger` whether its box contains the AudioListener (`Collider.ClosestPoint` equals the point). Indoor wins because the outdoor box overlaps the kiosk. We poll because the XR camera has no collider, so trigger callbacks would never fire.
43. **How did you test listeners without the gameplay?** `Sandbox_P3` + `SyntheticRound`: a scripted round made only of `GameEvents` calls. The PlayMode tests reuse it: Results reached, tallies right, every cue has a clip, every preset matches the table, every haptic plays a sound, three rounds leak < 5 MB and leave subscriber counts unchanged (read by reflection from the event backing fields).
44. **Where do the sounds come from?** Synthesised by our own script (`tools/p3/gen_p3_assets.py`): filtered noise, decaying sine partials (the bell uses the 1 : 2.76 : 5.40 : 8.93 partial ratios of a struck bell) and formant filters for the wordless customer barks. All CC0, listed in `Assets/Audio/CREDITS.md`.
