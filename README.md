# Canteen Rush

A VR food-service game built in Unity 6 (URP, XR Interaction Toolkit 3.1) from the PES University "Outdoor" floor assets. You are a student cook in the rooftop canteen on the 13th floor: grab donuts, colas, shakes and cakes, fill cups at the water cooler, load a tray and carry it out to customers on the patio before their patience runs out. An order bell rings from the table that ordered, every action has a spatial sound and a haptic pulse, and the light slides from midday to golden hour over the 3-minute round.

AR/VR course mini-project: interaction, spatial audio, haptics, UI and lighting, budgeted for Quest-class hardware and demonstrated through the XR Device Simulator.

## Run it

**Editor (development and the viva demo)**
1. Install Unity **6000.0.43f1** with Unity Hub (Windows Build Support is enough). Clone with Git LFS (`git lfs install` first).
2. Open `unity/CanteenRush/` in Unity Hub and wait for the first import.
3. Open `Assets/Scenes/Main.unity` and press Play. The XR Device Simulator starts automatically; the menu board appears in front of you.
4. Sandboxes: `Sandbox_P2.unity` (gameplay) and `Sandbox_P3.unity` (audio, haptics and UI with a debug panel, no gameplay needed).

**Windows build:** menu **File > Build Profiles > Windows > Build**, or batch:
```bash
Unity.exe -batchmode -nographics -quit -projectPath unity/CanteenRush -executeMethod CanteenRush.World.Editor.LightingBuilder.BuildWindowsBatch
```
Output: `Builds/CanteenRush/CanteenRush.exe` (git-ignored, about 107 MB).

**Tests:** Window > General > Test Runner. EditMode = P2's logic and gameplay tests; PlayMode = P3's smoke tests (scene loads, scripted round, cue/preset coverage, leaks, subscriptions).

## Controls

**Desktop mode (default on PC, no VR needed):** first-person keyboard and mouse. One invisible hand follows the camera and grabs through the same XR interactables, so audio, haptics and scoring behave exactly as in VR. Press **F5** to switch to the XR Device Simulator.

| Action | Keys |
|---|---|
| Walk / sprint | W A S D / Shift |
| Look | mouse |
| Grab or drop what the crosshair is on | left click or **E** (drop "smart-places" on the tray, under the tap, or on the customer table) |
| Open / close the fridge | click or **E** on the door |
| Pump water | set a cup under the tap, hold **F** |
| Tilt the tray | hold right mouse while carrying, then move the mouse |
| Start / replay the round | **Enter** |
| Pause / menu | **Esc** (then **M** for the menu) |
| Controls overlay / haptics HUD | **F1** / **F9** |
| Switch to the VR simulator | **F5** |

**XR Device Simulator (F5):** hold right mouse to look, W A S D to move, Y/T/Tab to pick a controller, **G** toggles grab, left click is the trigger, F6/F7 seated/teleport, F8 menu. The simulator cannot vibrate a controller; the haptics HUD stands in for it. Headphones recommended for the spatial audio.

## How it is built

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

- `Cr.Core`: enums, the static `GameEvents` bus, data ScriptableObjects, pure logic (`OrderMatcher`, `ScoreCalculator`, `WaveDirector`).
- `Cr.Gameplay` (P2): XR rig, stations, trays, customers, round flow. `Cr.World` (P1): scene builder, sun cycle, markers.
- `Cr.Audio`, `Cr.Haptics`, `Cr.UI` (P3): listeners only. They reference `Cr.Core` and never each other or `Cr.Gameplay`; the menu talks back through `GameEvents.PlayRequested / MenuRequested / ComfortChanged`.

| Document | What |
|---|---|
| `docs/00_MASTER_SPEC.md`, `docs/DECISIONS.md` | Design, budgets, decisions |
| `docs/04_CONTRACTS.md` | Event bus, types, layers, folders, assemblies |
| `docs/LAYOUT.md`, `docs/PERF_REPORT.md` | World coordinates; measured performance |
| `docs/QA_CHECKLIST.md` | Manual and automated QA |
| `docs/06_VIVA_PACK.md` | Demo script, questions and answers, the numbers |
| `docs/LOG_P1.md`, `LOG_P2.md`, `LOG_P3.md` | What each person built and how AI was used |

## Credits
- Source floor models and textures: PES University AR/VR course drive (`Outdoor` folder), used for this assignment.
- Unity packages (URP, XR Interaction Toolkit with Starter Assets and XR Device Simulator samples, Input System, OpenXR, AI Navigation, uGUI): Unity Companion License.
- Audio: all clips synthesised by `tools/p3/gen_p3_assets.py`, team-authored, CC0. Per-file list: `unity/CanteenRush/Assets/Audio/CREDITS.md`.
- AI assistance (permitted by the course) is disclosed per person in `docs/CREDITS.md` and the `LOG_P*.md` files.

Code: MIT (`LICENSE`). The source drop (`Outdoor-*.zip`, 517 MB) is not in this repository; extract it to `source/` (git-ignored) to rerun the Blender pipeline.
