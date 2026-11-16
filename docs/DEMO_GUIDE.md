# DEMO GUIDE AND SCRIPT — Canteen Rush (4 minutes)

Two parts: **A** the operator's guide (setup, keys, what to do if it breaks) and **B** the word-for-word script. The timing table in `docs/06_VIVA_PACK.md` §A is the source of truth for beats; this file turns it into something you can read aloud. Numbers come from `docs/PERF_REPORT.md`.

Roles: **Driver** holds the keyboard and mouse. **Narrator** speaks and watches the clock. With two people this works best; one person can do both once rehearsed. The third person fields questions at 3:50.

---

## A. Operator's guide

### A1. Before the room fills (15 minutes)
1. Laptop on power. Close everything else. Windows: set Power mode to Best performance, mute notifications (Focus assist on).
2. Pull `main`, then either:
   - **Build (preferred):** run `Builds/CanteenRush/CanteenRush.exe` (rebuild with the batch command in the README if the build is older than the latest merge), or
   - **Editor:** open `unity/CanteenRush/`, open `Assets/Scenes/Main.unity`, set the Game view to **Maximize on Play**.
3. Headphones on the Narrator's side or plugged into the room speakers: the spatial audio beat depends on hearing left/right.
4. Launch once, play the tutorial for 20 seconds, quit. This warms the shader cache so the demo does not hitch.
5. Open the **Stats** overlay (editor) or have `docs/PERF_REPORT.md` ready as a fallback slide for the numbers.
6. Start a screen recording of a full practice round now. It is the backup if the live demo fails (see A4).
7. Reset: restart the app so the first thing on screen is the **menu board**.

### A2. Controls cheat sheet (XR Device Simulator, keyboard and mouse)
Tape this beside the keyboard.

| To do | Keys |
|---|---|
| Look around | hold **right mouse button** and move the mouse |
| Walk | **W A S D** |
| Select a hand | **Y** = right controller, **T** = left, **Tab** cycles (see the on-screen help) |
| Grab / release | tap **G** to grab, tap **G** again to release (toggle grip) |
| Trigger (pump, UI click) | **left mouse button** with a controller selected |
| Start or replay | point at Play or Replay and trigger. Fallback: **Enter** |
| Back to menu | **Menu** on the results board. Fallback: **F8** |
| Seated / teleport | **F6** / **F7** (or the menu buttons) |
| Haptics HUD | **F9** |

Rules that save you on stage:
- **Click the window first** so it has keyboard focus; otherwise keys go to the editor.
- A grab only works when the hand's ray or sphere is on the item. Aim, then tap **G**.
- Do not mash G: it is a toggle, two taps in a second puts the item straight back.

### A3. Where things are
The player starts **behind the display cases**, between the cases and the fridge, facing the open side. Donuts are in the display cases, the fridge is beside you, the water cooler is on the prep counter, trays are on the rack. The exit to the patio is the opening **immediately right of the donut cases**. Customers sit on the patio, 9 to 18 m out. Full coordinates: `docs/LAYOUT.md`.

### A4. If something goes wrong
| Problem | Fix, and what to say |
|---|---|
| Keys do nothing | Click the Game window. Still nothing: Enter starts the round, F8 returns to menu. |
| Item will not grab | Move the hand closer, aim at it, tap G. Say: "The simulator gives me one hand at a time." |
| Dropped an item | Fine. It is the Spill haptic and sound: "That is the spill feedback." Use it as a feature. |
| No haptics visible | Press **F9**. Say: "The simulator cannot vibrate, so the HUD shows every pulse." |
| No sound | Check output device before the demo, not during. Fall back to narrating the cues. |
| Frame rate drops | Lower resolution; say the Quest-class quality level is the real target (3.5 to 3.8 ms). |
| Crash or hang | Switch to the backup recording. Narrate over it as if live; say it is a recording. Do not apologise at length. |
| Ran out of time | Cut the tray-tilt demo (1:20) first, then the expiry demo (2:50). Never cut the bell, the door crossing or the sun. |

### A5. Five graded features, and where each is seen
| Feature | Beat |
|---|---|
| Interaction | grab donut, fridge door, cup and pump, tray |
| Spatial audio | fridge latch, order bell by ear, indoor to outdoor crossing |
| Haptics | HUD on every grab, door latch, water ramp, tilt ramp, serve pulses |
| UI | menu board, wrist HUD, order tickets, results board |
| Lighting | warm indoors, sun moving to golden hour, baked GI |

---

## B. The script

Read the **Say** lines as written, in your own rhythm. **Do** is what the Driver does at the same time. Keep each Say to about 15 seconds; silence while something plays is good, especially for the bell and the door.

### 0:00 — Menu board
**Do:** the menu board is on screen. Point the ray at **Play** and trigger. Tutorial hint ring pulses on the donut.
**Say:** "Canteen Rush is a VR food-service game on a Quest-class budget, set on the roof of a campus building. We are showing it through the XR Device Simulator, so my keyboard and mouse stand in for the controllers. Five things are graded and I will point out each: interaction, spatial audio, haptics, UI and lighting."

### 0:20 — Grab (interaction, haptics HUD)
**Do:** press **F9** to show the haptics HUD. Aim at a donut in the display case, tap **G**.
**Say:** "This is a grab interactable with a collider. The simulator cannot vibrate, so every haptic pulse shows on this HUD: colour is the preset, size is strength, fade is duration. A short pulse means contact."

### 0:40 — Fridge door (interaction, audio, haptics)
**Do:** walk to the fridge, grab the door handle and pull it open slowly. Grab a cola.
**Say:** "The door is a hinge joint with limits. Listen: that click, and the pulse on the HUD, is a latch detent at 8 degrees. When it hits the open limit you get a duller thunk and a stronger pulse. The sound comes from the fridge's position, so it pans as I turn."

### 1:00 — Cup and pump (audio and haptics coupling)
**Do:** grab a cup, hold it under the cooler tap, trigger the pump. Let it fill.
**Say:** "Fill level drives two things at once: the pitch of the water loop rises, and the haptic ramps up from light to strong. A continuous process gets a ramp; a contact gets a short pulse."

### 1:20 — Tray and tilt (haptics, physics) — *cut first if short on time*
**Do:** grab a tray from the rack, place the donut and cola on it. Tilt it past the warning.
**Say:** "Past 20 degrees the tray buzzes, growing with the angle. Past 35 degrees the items slide off. The warning is felt before it is seen."

### 1:40 — The order bell (spatial audio)
**Do:** stay still and let the bell ring. Turn the view toward the sound before moving. Carry the tray toward the patio.
**Say:** "The bell is emitted at the customer's seat, and its pitch goes up one semitone per table, so higher means farther. I find who ordered by ear: the side from the panning, the distance from loudness and pitch."

### 2:20 — Through the door (spatial audio, lighting)
**Do:** walk through the opening right of the donut cases onto the patio.
**Say:** "Crossing the door switches the audio snapshot from the indoor mix to the outdoor mix with a short cross-fade. The reverb zone only exists inside the kiosk, and the light changes from warm interior to open sky."

### 2:30 — Serve (game logic, UI)
**Do:** place the tray on the customer's table socket. Point at the score popup and the wrist HUD.
**Say:** "A perfect serve gives a double pulse and a happy sound. The score is items times combo plus a speed bonus minus penalties, all in one ScriptableObject we can tune without code."

### 2:50 — A failure — *cut second if short on time*
**Do:** let one order's patience run out; show the angry bubble.
**Say:** "Failure has its own sound and its own haptic: a long buzz. Every haptic is paired with a sound, so the feedback never depends on one sense."

### 3:10 — Light (lighting)
**Do:** stop moving. Slowly look at the sky and across the patio.
**Say:** "Over the three-minute round the sun moves from midday to golden hour. Global illumination is baked, so only the sun and the ambient colour change at runtime. That is why it is cheap."

### 3:30 — Results and performance (optimisation)
**Do:** results board is up. Show the stars and the tally. Show the Stats overlay or the perf slide.
**Say:** "The results board shows score, stars, and how many we served, got wrong, spilled or lost. On performance, measured on a Windows build on an integrated Intel Iris Xe GPU: 58 to 100 thousand visible triangles, at most 88 visible renderers, and 3.5 to 7.8 milliseconds a frame. The Quest budget is 250 thousand triangles and 11 milliseconds. One honest gap: we did not measure draw calls directly, only the proxies for them."

### 3:50 — Questions
**Say:** "That is the demo. We are happy to take questions on any system."
Hand off to the person who owns the system asked about. See `docs/06_VIVA_PACK.md` §B and §H for answers.

---

## C. Rehearsal and honesty notes
- Rehearse twice, with a timer, identical each time. Record the second run as the backup.
- State plainly that the simulator is not a headset and cannot vibrate; the HUD is the stand-in. Examiners reward this and punish bluffing.
- Everyone must be able to name the three numbers (triangles, draw calls, frame time) and the event bus (`GameEvents`). Pairings for the cross-explain drill are in the viva pack §C.
- Not yet verified by a human on the final build: a full manual pass of every interaction, and five human rounds for balance (see `docs/LOG_P2.md` and `docs/QA_CHECKLIST.md`). Do this on the demo machine before relying on this script's exact beats; adjust any step that does not behave as written.
