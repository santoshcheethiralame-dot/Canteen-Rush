# P2 gameplay log

## 2026-11-03 — core foundation (in progress)

- Added the core enums, order/result types, event bus and ScriptableObject types from the contract. Static subscribers are cleared on subsystem registration and in test teardown, never at round start.
- Added initial EditMode tests before the matching, scoring and wave implementations. Added order and score services; these need scene wiring and play testing.
- The wave director uses a private serialized template list in GameConfig. The published constructor has no other way to receive its template pool; this is an implementation detail, not a change to the shared public API. Wave 3 filters out single-item orders as required by the master spec.
- Checked core and services against the installed Unity 6000.2.2f1 managed assemblies with `tools/unity_compile_check.py`: compilation succeeded, with expected warnings for serialized fields awaiting prefab wiring. This is not a Unity import or test run.
- Project targets Unity 6000.0.43f1 and XRI 3.1.2. The target editor, package cache and Starter Assets samples were not found on this machine. No XR interaction APIs or serialized rig were guessed. The EditMode suite has not been run.
- Remaining: finish and run the required test matrix; create configured data assets and metadata via Unity; XR rig, interactions, prefabs, customers, round flow, order UI and sandbox; integrate with P1's world and P3's listeners; verify simulator, repeated rounds, profiling and balance. P1 must place prefabs in Main; Main is not P2-owned.

## 2026-11-03 — gameplay components (still in progress)

- Inspected the official XRI 3.1.2 package and implemented initial grab, socket, spill, water, fridge, customer, serving, round, motion, comfort and order UI components. Added config, item and order data assets. These components have not been wired into prefabs or a scene and are not a playable milestone.
- Imported an isolated copy using Unity 6000.2.2f1 and an embedded copy of XRI 3.1.2. The updated scripts compiled without C# errors. The newer editor upgraded other packages in the isolated copy only; the repository package settings were not changed.
- Attempted the EditMode test run in that copy. Unity reported no valid Editor license and exited before executing tests. The project-target editor 6000.0.43f1 remains unavailable. Data assets were authored as YAML and need inspection in the target editor, including item prefab and icon references.
- Still required: finish tests and execute them; build and configure all prefabs, the XRI Starter Assets rig and simulator, and Sandbox_P2; complete scene wiring, UI and reset handling; validate the specified interactions and three successive rounds; profile allocations and rigidbodies; balance five rounds. None of the new XR, customer or UI components has had a PlayMode or simulator check. P1 integration and P3 listeners remain dependencies.

## 2026-11-03 — independent logic review

- Added EditMode cases for speed bonuses, surplus duplicates, wave 2 and 3 cadence and capacity, spill score floors, serving, timeouts, and listeners that reset orders from callbacks.
- OrderService now iterates a reusable snapshot, so a listener can reset or replace orders during patience and expiration events without invalidating the active iteration. Spawn cadence now consumes an interval even if no seat is free or the wave is at capacity, matching the periodic spawn rule.
- The core and updated services compile against the available Unity 6000.2.2f1 managed assemblies; serialized fields remain unwired. The new tests have not run because the target editor/license setup is in progress.

## 2026-11-03 — target editor and prefab foundation

- Unity 6000.0.43f1 is installed and licensed. In an isolated copy using the project package versions, the EditMode suite passes 33/33 tests, including checks for item, tray and systems prefab references. The first run identified an incorrectly serialized float array in GameConfig, now corrected.
- Added the basic item, tray, customer, fridge, cooler, item-spawner and serve-socket prefabs, plus a systems prefab with score, order, customer and round service references. Item and tray visuals use the supplied FBX assets when available. The editor builder lives in the P2 gameplay folder.
- This is not a playable milestone: the XR Starter Assets rig and simulator, gameplay UI prefabs, station/seat wiring, rack, sandbox scene and complete round/reset flow still need implementation and PlayMode verification. The station prefabs contain scene-specific references that must be wired; the systems prefab alone does not populate the world. P1 owns integration into Main, and P3 supplies the shell and event listeners.
- The EditMode checks do not establish that grabbing, hinges, sockets, walking, water, serving or repeated rounds work in the simulator. No profiling or balance pass has been performed.

## 2026-11-07 — P2 gameplay completion and regression pass

Implemented:
- Tutorial is a separate, untimed practice order that always requests a donut. It cannot consume wave time or patience. Failed practice serves restart practice; successful practice clears its score and items before the timed round. Startup no longer depends on the order of the two Start callbacks.
- Round boundaries reset customers, orders, score, spawners, trays, socket selections, pump state and fridge doors. Static event subscribers survive replay. Score stops accepting spill penalties in Menu and Results; the clock reports zero at Results.
- Each tray owns its return timer and rack home. Returning one tray cannot eject a replacement tray from the serving socket. Replay cancels old timers and clears held/socketed contents.
- Item pools prewarm at station setup, with four objects per spawner and a global limit of 34 active items. With two trays and two fridge doors this leaves the gameplay rigidbody count below 40. At capacity, spawners wait for an item to be recycled; they do not reclaim items the player is using. Item colliders now fit the actual supplied mesh bounds instead of treating a flat donut as a tall capsule.
- Fridge spawners guard each new cola while the upper door is closed, including pooled replacements. Items already removed remain usable. Doors use velocity tracking, a handle-only grab collider, a single selector and no throw impulse. Sandbox doors and shelf have physical clearance.
- Cooler filling deduplicates cups with compound colliders, requires pump activation and overlap, stops on pump release/selection exit, and does not complete a full cup twice. Added visible sandbox pump geometry and a supported cup dispenser.
- Customers reuse a four-object pool, preserve the original body height while bobbing, use shared-material colour variants, face the player, and carry an order bubble with icons and a patience ring. The order rail has matching item icons, readable labels, seat numbers and a low-patience flash.
- Added Station_TrayRack, Station_CupDispenser, UI_OrderTicket, UI_CustomerBubble and a P2 simulator wrapper. The wrapper omits only the sample keyboard-help overlay in batch mode, where no physical keyboard bindings exist; interactive use retains the overlay.

Main integration:
- P1's saved Main.unity, world assets and build settings were not edited. WorldGameplayInstaller loads Assets/Data/Resources/P2Runtime.prefab when Main has no systems, or uses the installer on Systems.prefab after P1 rebuilds Main.
- The installer supplies the rig and simulator, fills 18 SpawnPoint markers, wires all eight serving seats, places the rack, pump overlay and order rail, and enables optional teleport surfaces on tagged floors. Main fridge doors reuse the supplied door meshes and fitted colliders, preserving the source hinge pivots rather than the placeholder dimensions. Hollow display-case and fridge bounding boxes are replaced with source-mesh colliders at runtime so their openings remain usable.
- Enter starts/replays; F8 returns to Menu; F6 toggles seated height; F7 toggles optional teleport. These keys avoid the XR simulator bindings on M, C and T. These are simulator fallback controls. P3's shell should call GameManager.Play/Menu and ComfortSettings.SetSeated/SetTeleport and listen to GameEvents. Audio, haptics, menu/results boards and tutorial highlight presentation remain P3-owned.
- Rebuild P2 assets with CanteenRush.Gameplay.Editor.SandboxBuilder.BuildAll (or the Canteen Rush > Gameplay menus). The gameplay UI remains in its existing independent Cr.UIGameplay assembly; no gameplay-to-UI runtime dependency was added.

Validation:
- Unity 6000.0.43f1 generated the prefabs and Sandbox_P2 with no C# errors.
- Expanded EditMode coverage includes Play-mode entry, an actual XRI socket serve, untimed practice, navigation to an order, cup filling through real trigger overlap, door access, swapping a tray during return, frozen Results scoring, Main station population and three replay cycles.
- Final rendered suite result: **39/39 passed** in Unity 6000.0.43f1 with D3D rendering enabled. Main retained all displayed stock after settling, and repeatedly taking stock reached the full 34-item cap without exceeding 40 active Rigidbodies. Test XML/logs are in the parent Project folder as p2-tests.xml and p2-tests.log. A rendered Main customer preview is p2-customer.png there.

Remaining validation outside the automated checks:
- A human keyboard/mouse simulator pass of every grab, pump and hinge interaction; five human rounds for balance; a gameplay Profiler capture for GC allocations and frame time. The automated replays advance the clock and exercise reset behaviour; they are not five manually played/balanced rounds or a Quest performance claim.
- P3 listener/shell integration and P1's next standalone build are separate deliverables. Existing built executables were not regenerated. This work leaves the existing working-tree changes in place and does not stage, commit or push them.


## 2026-11-07 — real hand detection regression

- User playtesting exposed a gap in the earlier tests: scripted XRInteractionManager.SelectEnter bypassed the Starter Assets rig's physics queries. Both near-hand sphere queries and far rays were filtering out our Items, Tray and Stations layers.
- ComfortSettings now configures those masks on rig startup; the rig builder serializes the same masks. RigInteractionUpgrade repairs an existing Rig.prefab on editor reload without rebuilding or editing Main or the imported XRI samples.
- Remapped fallback controls to F6 (seated), F7 (teleport), F8 (menu). The old T/C/M mappings conflicted with simulator hand selection, movement constraint and controller menu inputs.
- Added GrabInputTests.SpaceAndGGrabADonutThroughTheRealRig: places the real rig hand near an item, checks actual detected targets, then injects Space + G through an Input System keyboard and checks selection/release, without calling SelectEnter directly. Verified in a clean isolated copy using Unity 6000.0.43f1: focused grab/release test passed; full rendered suite **40/40 passed** (p2-grab-suite.xml and p2-grab-suite.log in the parent Project folder). The test freezes controller poses and temporarily routes input to the Game view on the batch worker; it restores those settings afterward. The actual simulator input actions and XRI detector perform selection/release. Live UI testing was attempted through the computer-use skill, but the helper could not restore the minimized Unity window after user input.

- SimulatorSupport now runs before XRI’s simulator Awake, so the optional keyboard-help overlay is correctly omitted on batch workers. Imported XRI samples and project package versions remain unchanged.


## 2026-11-07 — native editor playtest and keyboard bridge

- Reproduced the live editor issue with computer-use: Y/Tab did not reach the simulator while editor menu shortcuts worked. Added an editor-only IMGUI-to-Input-System fallback in SimulatorSupport. It creates a fallback keyboard only after a missed native key, releases its keys on focus loss and removes that device on destruction. Short G taps remain active for at least 0.15 seconds so they cannot disappear between simulator updates; a normal held grip still releases on key-up.
- Native UI verification after the bridge: clicked the Game view, started with Enter, selected Right Controller with Y, aimed using mouse movement and pressed G. Observed the ray turn blue and the middle donut move/attach. Captured the actual Game-view screenshot as p2-live-keyboard-grab.png in the parent Project folder.
- Also ran an instrumented live Main-scene check, without moving the food: the hand's actual target and first ray collider were Item_Donut, keyboard grip selected it, and release released it. Logs and a capture are p2-live-grab.txt and p2-live-grab.png. LiveGrabDiagnostics provides explicit editor-menu actions for this check and restores its temporary input settings and tracking changes afterward; it does not run during normal play.
- Final bridge regression suite: **40/40 passed**, p2-input-bridge-suite.xml/log in the parent Project folder. An intermittent XRI attach-transform assertion at test teardown was resolved by resetting to Menu and letting two player frames finish before exiting Play mode.
- The XRI Project Validation window still reports the pre-existing pinned-package/teleport-layer/background configuration warnings. No package upgrades or P1 settings edits were performed. Close that floating editor window to use the Game view. Code recompilation during a live round requires a fresh Play session; tests and source edits were completed before the final live restart.
