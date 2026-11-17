# DEMO GUIDE — Canteen Rush

Everything you need to run, present and defend the 4-minute demo. It is written so anyone on the team can pick it up cold.

**How this file is laid out**

| Part | What it is for |
|---|---|
| [1. The game in one minute](#1-the-game-in-one-minute) | What the game is, so you can explain it in two sentences |
| [2. Before the demo](#2-before-the-demo-15-minutes) | Setup checklist |
| [3. Controls](#3-controls) | Every key, and exactly how each action works |
| [4. Where things are](#4-where-things-are) | The map of the play area |
| [5. How a round works](#5-how-a-round-works) | Tutorial, waves, patience, scoring, stars |
| [6. The 4-minute script](#6-the-4-minute-script) | What to do and say, beat by beat |
| [7. If something goes wrong](#7-if-something-goes-wrong) | Quick fixes during the demo |
| [8. What we used and why](#8-what-we-used-and-why) | Every tool and technique, with the reason |
| [9. Glossary](#9-glossary) | Plain-English meaning of every technical word |
| [10. Honesty notes](#10-honesty-notes) | What is and is not proven |

Other files: the viva questions are in `docs/06_VIVA_PACK.md`, the P1 (world and lighting) script is `docs/DEMO_SCRIPT_P1.md`, the performance numbers are `docs/PERF_REPORT.md`.

**Roles on the day.** The **Driver** holds the mouse and keyboard. The **Narrator** speaks and watches the clock. The **third person** answers questions at the end. One person can do both Driver and Narrator after two rehearsals.

---

## 1. The game in one minute

Canteen Rush is a VR food-service game. You run a small canteen kiosk on the roof of the 13th floor of our campus. Customers sit at tables on the open patio and order food. You pick up the food, put it on a tray, carry the tray out, and serve the right customer before they lose patience.

It was built for a VR headset (Quest-class), but we have no headset in the room. So the game has two ways to play on a laptop:

- **Desktop mode (the default).** Play it like a normal PC game: mouse to look, WASD to walk, click to grab. This is what we demo.
- **VR simulator mode (press F5).** Unity's XR Device Simulator pretends the keyboard and mouse are a headset and two controllers. This shows the real VR menus and wrist display.

Both modes drive the same VR rig, so every sound, haptic pulse and score is exactly the same in both.

---

## 2. Before the demo (15 minutes)

1. Plug the laptop into power. Close every other app. Windows: Power mode **Best performance**, notifications off (Focus assist).
2. Get the latest `main` from GitHub.
3. Run `Builds/CanteenRush/CanteenRush.exe`. If the build is older than the last merge, rebuild it with the batch command in `README.md` (takes about a minute).
   - Fallback: open `unity/CanteenRush/` in Unity 6000.0.43f1, open `Assets/Scenes/Main.unity`, tick **Maximize on Play**, press Play.
4. Sound: headphones or room speakers. The order bell beat only works if the room can hear left and right.
5. **Warm-up run.** Play the tutorial for 20 seconds, then quit. This loads the shaders so the real demo does not stutter.
6. **Record a backup.** Screen-record one full practice round (Win+G opens the Xbox Game Bar recorder). If the live demo fails, you play this instead.
7. Have `docs/PERF_REPORT.md` open in another window for the performance beat.
8. Restart the game so the first thing on screen is the **title card** ("CANTEEN RUSH, Press ENTER to play").
9. Click once inside the game window so it captures the mouse.

---

## 3. Controls

### 3.1 Desktop mode (the default)

Print this table and tape it next to the keyboard. The game also shows it in the bottom-left corner; **F1** shows or hides it.

| What you want to do | Key | Notes |
|---|---|---|
| Walk | **W A S D** | |
| Run | hold **Shift** while walking | |
| Look around | move the **mouse** | Click the window first so it captures the cursor |
| Grab the thing under the crosshair | **Left click** or **E** | Works up to 3 m away. The prompt under the crosshair says "Grab donut", "Grab tray" and so on |
| Put down / drop what you hold | **Left click** or **E** again | It snaps to where you are looking: a tray slot, the tap, a table |
| Put food on a tray | hold the food, look at the tray, **E** | Prompt: "Put donut on the tray". The food snaps into a free slot |
| Pick up a tray (with food on it) | look at the tray, **E** | Clicking food that is already on a tray picks up the whole tray |
| Serve a customer | carry the tray, look at the customer, **Left click** or **E** | Prompt: "Serve donut". The tray goes onto that customer's table |
| Open or close the fridge | look at the fridge door, **E** | The door swings open by itself; the cola inside can then be grabbed |
| Fill a cup with water | grab a cup, look at the tap, **E** to place it, then **hold F** | About 4 seconds to fill. Let go of F to stop |
| Tilt the tray (to show the spill warning) | hold **Right mouse** and move the mouse sideways | Past 20° it warns, past 35° the food slides off |
| Start the round / play again | **Enter** | Works on the title card and the results screen |
| Pause | **Esc** | Click or Esc again to resume |
| Back to the title card | **Esc** then **M** (or **F8** any time) | |
| Show / hide the controls list | **F1** | |
| Show / hide the haptics display | **F9** | The laptop cannot vibrate, so this shows every vibration pulse on screen |
| Switch to the VR simulator | **F5** | Press F5 again to come back |

**The crosshair.** The small square in the middle of the screen is where you are pointing. Below it, a label with a key cap (for example **E** "Grab donut") always tells you what the next click will do. If there is no label, nothing in reach can be used.

**What is on screen (desktop mode)**

- **Top bar:** the phase (PRACTICE, WAVE 1-3), the round timer (turns red and blinks in the last 20 seconds), and the score.
- **Order tickets (right side):** one card per waiting customer: table number, what they ordered, and a patience bar that goes green, then yellow, then red. The ticket of the customer you are looking at is highlighted.
- **Pop-ups:** "+120", "PERFECT!", "Table 3 gave up", "WAVE 2".
- **Results card:** score, 1-3 stars, orders served, perfect serves, customers who walked out.

### 3.2 VR simulator mode (F5)

F5 hands control to Unity's **XR Device Simulator**, the tool that fakes a headset and two controllers with the keyboard and mouse. In this mode you see the real VR interface: the menu board, the wrist display, the order rail and the results board. Only use it for one beat in the demo ("this is the VR view"). It is harder to drive, so switch back with **F5** straight after. The simulator shows its own key help on screen.

---

## 4. Where things are

You start **behind the display cases**, between the cases and the fridge, facing the open side of the kiosk.

| Thing | Where |
|---|---|
| Donuts (also cake and shakes) | In the glass display cases in front of you |
| Fridge with colas | Right beside your start point |
| Cups | Cup dispenser on the prep counter |
| Water cooler (tap) | On the prep counter |
| Trays | Tray rack (two trays) |
| Exit to the patio | The opening **just right of the donut cases** |
| Customers | Seated at tables on the patio, 9 to 18 m out |

Exact coordinates are in `docs/LAYOUT.md`.

---

## 5. How a round works

**Tutorial (PRACTICE).** The first time you press Enter you get a 3-step practice with one customer at table 1 who wants a donut:
1. Grab a donut.
2. Put it on a tray.
3. Serve table 1.

There is no timer, and getting it wrong just lets you try again.

**The round.** Then the real round starts: **3 waves of 60 seconds each (3 minutes total)**.

| Wave | Customers at once (max) | A new customer every |
|---|---|---|
| 1 | 2 | 14 s |
| 2 | 3 | 11 s |
| 3 | 4 | 9 s |

**Ordering.** A customer sits down and an **order bell** rings from their seat. Their ticket appears on the right.

**Patience.** Each customer waits **25 seconds plus 15 seconds per item** they ordered (a 2-item order waits 55 s). If the bar runs out, they leave angry.

**Scoring**

| Event | Points |
|---|---|
| Each correct item | +100 |
| Perfect order with 2+ items (combo) | items × 1.25, × 1.5 ... (+25 % per extra item) |
| Speed bonus | up to +50, more the faster you serve |
| Wrong item on the tray | −25 |
| Food dropped on the floor | −10 |
| Customer gives up | −50 |

**Stars:** 1 star at 300 points, 2 at 700, 3 at 1200.

After a serve, the customer eats and the tray **returns to the rack by itself after 3 seconds**. All of these numbers live in one settings file (`GameConfig`), so they can be tuned without changing code.

---

## 6. The 4-minute script

**Do** is what the Driver does. **Say** is what the Narrator says. Keep each Say to about 15 seconds. Silence while a sound plays is fine, especially for the bell and the door.

### 0:00 — Title card
**Do:** the title card is on screen. Press **Enter**. The practice starts.
**Say:** "Canteen Rush is a VR food-service game on a Quest-class budget, set on the roof of our campus building. We have no headset here, so you are seeing desktop mode, which drives the same VR rig with a mouse and keyboard. Five things are graded and I will point out each one: interaction, spatial audio, haptics, UI and lighting."

### 0:20 — Grab (interaction, haptics display)
**Do:** press **F9** to show the haptics display. Put the crosshair on a donut ("Grab donut"), press **E**.
**Say:** "Every item is a physics object the VR hand can grab. The laptop cannot vibrate, so every vibration pulse is drawn here: colour is the type of pulse, size is the strength, fade is the length. A short pulse means you touched something."

### 0:40 — Onto the tray (interaction, sockets)
**Do:** look at the tray on the rack ("Put donut on the tray"), press **E**. The donut snaps into a slot. The practice moves to step 3.
**Say:** "The tray has four slots. Food snaps in, and only food in a slot counts for an order."

### 0:55 — Fridge door (interaction, audio, haptics)
**Do:** look at the fridge door, press **E**. It swings open. (Optional: grab a cola and put it on the tray too.)
**Say:** "The door is a hinge with limits. That click, and the pulse on screen, is a latch at 8 degrees. At the open limit you get a duller thunk and a stronger pulse. The sound comes from the fridge, so it moves as I turn."

### 1:10 — Cup and pump (audio and haptics together)
**Do:** grab a cup, look at the tap ("Place cup under the tap"), **E**, then **hold F** for 4 seconds.
**Say:** "As the cup fills, two things rise together: the pitch of the water sound and the strength of the vibration. A continuous action gets a ramp; a single touch gets a short pulse."

### 1:25 — Tilt the tray (haptics, physics) — *cut first if short on time*
**Do:** pick up the tray (**E** on it). Hold **right mouse** and move the mouse sideways slowly.
**Say:** "Past 20 degrees the tray starts buzzing, stronger as it tips. Past 35 degrees the food slides off. You feel the warning before you see the spill." *(Level it again before 35° if you want to keep the food.)*

### 1:40 — The order bell (spatial audio)
**Do:** stop moving and let the bell ring. Turn toward the sound, then walk out with the tray.
**Say:** "The bell plays at the customer's seat, and its pitch goes up one note per table, so higher means farther away. You find who ordered by ear: the direction from left-right panning, the distance from loudness and pitch."

### 2:15 — Through the door (spatial audio, lighting)
**Do:** walk through the opening right of the donut cases onto the patio.
**Say:** "Crossing the door switches the sound mix from indoor to outdoor, with a short cross-fade. The echo only exists inside the kiosk, and the light changes from warm inside to open sky."

### 2:30 — Serve (game logic, UI)
**Do:** look at the customer ("Serve donut"), **left click**. Point at the "+points" pop-up and the ticket disappearing.
**Say:** "Clicking the customer puts the tray on their table. A perfect serve gives a double pulse and a happy sound. The score is points per item, times a combo, plus a speed bonus, minus penalties. Every number is in one settings file we can tune without code."

### 2:45 — The VR view (UI) — *one quick look*
**Do:** press **F5**. Point out the menu board, wrist display and order rail. Press **F5** again.
**Say:** "This is the same moment in the VR simulator. In a headset the interface lives in the world: tickets on a rail, a score on your wrist, because flat screen overlays are uncomfortable in VR. Desktop mode draws the same information as a normal game HUD."

### 2:55 — A failure — *cut second if short on time*
**Do:** let one ticket's patience bar run out.
**Say:** "When a customer gives up there is an angry sound and a long buzz. Every vibration is paired with a sound, so the feedback never relies on one sense."

### 3:10 — Light (lighting)
**Do:** stand still on the patio. Look slowly across the tables, then up at the sky.
**Say:** "Over the three-minute round the sun moves from midday to golden hour. The rest of the light is baked in advance, so only the sun and the sky colour change while you play. That is why it is cheap."

### 3:30 — Results and performance (optimisation)
**Do:** the results card is up (**Enter** replays). Show the stars and the tally, then the `PERF_REPORT.md` table.
**Say:** "Results show score, stars, served, perfect and walk-outs. On performance, on a Windows build with an integrated Intel Iris Xe GPU: 58 to 100 thousand triangles in view, at most 88 objects drawn, and 3.5 to 3.8 milliseconds a frame at the Quest-class quality level. The Quest budget is 250 thousand triangles and 11 milliseconds. One honest gap: we did not measure draw calls directly, only the things that cause them."

### 3:50 — Questions
**Say:** "That is the demo. We are happy to take questions on any part."
Hand each question to the person who owns that part (see `docs/06_VIVA_PACK.md` §B and §H).

---

## 7. If something goes wrong

| Problem | Fix, and what to say |
|---|---|
| Keys or mouse do nothing | Click inside the game window. Still stuck: **Esc** twice, then **Enter**. |
| No prompt under the crosshair | You are too far (more than 3 m) or not pointing at it. Walk closer. |
| Wrong thing grabbed | Press **E** to drop it and aim again. |
| Food fell on the floor | Turn it into a feature: "That is the spill sound and the spill vibration." It costs 10 points. |
| Tray went missing | After a serve it returns to the rack by itself in 3 seconds. |
| Cup will not fill | It must be placed under the tap (prompt "Place cup under the tap"), and you must **hold** F. |
| No haptics shown | Press **F9**. |
| Ended up in the VR simulator by accident | Press **F5**. |
| No sound | Check the output device before the demo, not during. Narrate the sounds instead. |
| Frame rate drops | Close other apps. Say the Quest-class quality level is the real target (3.5 to 3.8 ms). |
| Crash or freeze | Switch to the backup recording, say it is a recording, and narrate as if live. Do not apologise at length. |
| Running out of time | Cut the tray tilt (1:25) first, then the failure (2:55). Never cut the bell, the door crossing or the sun. |

**The five graded features, and where each one shows up**

| Feature | Beats |
|---|---|
| Interaction | grab, tray slots, fridge door, cup and pump, tray tilt, serve |
| Spatial audio | fridge latch, order bell by ear, indoor to outdoor crossing |
| Haptics | display on every grab, door latch, water ramp, tilt ramp, serve pulses, failure buzz |
| UI | title card, HUD and tickets, VR boards in the simulator (F5), results |
| Lighting | warm inside, sun moving to golden hour, baked light |

---

## 8. What we used and why

### 8.1 Tools

| What | What it does | Why we chose it |
|---|---|---|
| **Unity 6** (6000.0.43f1) | The game engine | Free for students, strong VR support, and the course allows it. |
| **URP** (Universal Render Pipeline) | Unity's lightweight renderer | Built for phones and standalone headsets like the Quest, so the same project hits a mobile budget. |
| **XR Interaction Toolkit 3** (XRI) | Ready-made VR grabbing, sockets, rays and UI clicking | Grabbing, snapping and VR menus work without writing low-level controller code, and every interaction fires events we hook sound and haptics to. |
| **XR Device Simulator** | Fakes a headset and controllers with keyboard and mouse | We have no headset; this is the official way to test VR without one. |
| **Desktop mode** (our own) | First-person mouse-and-keyboard controls over the same VR rig | The simulator is awkward to drive in front of examiners. Desktop mode is easy to play, and because it goes through the same XRI grab system, every event, sound, pulse and score is identical. |
| **Input System** | Unity's modern keyboard, mouse and controller input | Required by XRI; one system for VR controllers and the keyboard. |
| **Blender + Python script** | Cleaned the supplied 3D files | The source was about 7 million triangles at five different scales. A script rebuilds each piece to a triangle budget, so the whole world can be rebuilt with one command (`tools/run_world_pipeline.sh`). |
| **FBX** | 3D file format from Blender into Unity | Unity imports it natively; fixed export settings keep scale at 1 and the axes correct. |
| **Unity Test Framework** | Automated tests (56 of them) | Catches breakage after every change: scoring, order matching, audio and haptic wiring, and desktop controls are all checked by code, not by hand. |
| **Git + GitHub (with LFS)** | Version control | Three people working in parallel; LFS stores big binary files like models and sounds. |

### 8.2 Techniques, by graded feature

**Interaction**
- **Grab interactables:** every food item is a physics object with a collider, so it can be picked up, dropped and knocked over.
- **Sockets:** the four tray slots and each customer's table spot use XRI sockets. Sockets give clean snapping and clear "item placed / removed" events, which we use for scoring, sound and haptics.
- **Hinge joint:** the fridge door is a real physics hinge with limits (0 to 110°) and a latch click at 8°.
- **Pooling:** items are recycled instead of created and destroyed, so there is no stutter from memory clean-up.

**Spatial audio**
- **3D sound sources:** each sound plays from its object, with distance fall-off and left-right panning.
- **The order bell:** plays at the customer's seat, one note higher per table, so you can find a customer by ear.
- **Occlusion:** a wall between you and a sound muffles it (a low-pass filter).
- **Mixer snapshots:** separate indoor and outdoor mixes that cross-fade at the door, with echo only inside.

**Haptics**
- **One rule for the whole game:** a short pulse means contact, a ramp means something continuous, a long buzz means failure. Every pulse is paired with a sound.
- **The on-screen display (F9):** shows every pulse, because a laptop cannot vibrate.

**UI**
- **In VR:** the interface lives in the world (menu board, wrist display, ticket rail), because flat overlays cause discomfort in a headset.
- **In desktop mode:** a normal game HUD shows the same information.

**Lighting**
- **Baked lighting:** light on everything that never moves is calculated once in the editor and stored in a lightmap, so it costs almost nothing while playing.
- **Moving sun:** the only light calculated live is the sun, which moves from midday to golden hour over the round.
- **Light probes:** moving objects (food, trays) pick up the baked light around them, so they match the room.

**Optimisation**
- Triangle budgets per model, LODs (simpler models far away), occlusion culling (do not draw what is hidden), static batching and the SRP Batcher (fewer draw commands), GPU instancing for chairs, and textures at most 1024 pixels.
- Two quality levels: Quest-class (one shadow cascade at 25 m) and PC (two cascades at 40 m).
- Desktop mode adds a light colour-grading pass (tone mapping, soft bloom, vignette, anti-aliasing) so the laptop view looks better. The VR quality level does not use it, to stay inside the headset budget.

**Code structure**
- **Event bus (`GameEvents`):** gameplay announces things ("item grabbed", "order served"). Audio, haptics and UI listen. Gameplay never calls them directly, so three people could work in parallel and any one system can be removed without breaking the others.
- **Assemblies:** the code is split into separate modules (Core, Gameplay, Audio, Haptics, UI, World) with strict rules about which may use which.
- **ScriptableObject settings (`GameConfig`):** every gameplay number lives in one asset, so balancing needs no code changes.

---

## 9. Glossary

| Word | Plain meaning |
|---|---|
| **VR** | Virtual reality: you wear a headset and are "inside" the game. |
| **XR** | Umbrella term for VR, AR (augmented reality) and MR (mixed reality). |
| **Quest-class** | Standalone headsets like the Meta Quest: a phone-level chip, so the game must be light. |
| **Rig (XR rig)** | The player object: the head camera plus two hands. Desktop mode moves the same rig. |
| **Interactor / interactable** | XRI words. An interactor does the grabbing (a hand, a tray slot); an interactable is the thing grabbed (a donut). |
| **Socket** | An interactor that holds one item in a fixed spot, like a tray slot. |
| **Collider** | An invisible shape that tells physics where an object's surface is. |
| **Rigidbody** | Makes an object obey physics: gravity, falling, being pushed. |
| **Kinematic** | A rigidbody moved by code instead of by physics, for example food snapped into a slot. |
| **Hinge joint** | A physics link that lets something swing around one axis, like a door. |
| **Haptics** | Vibration in the controllers. |
| **Haptic preset** | One named pulse pattern (strength and length), for example "Grab" or "DoorLatch". |
| **Spatial (3D) audio** | Sound that seems to come from a place in the world: louder when near, panned left or right. |
| **Attenuation / roll-off** | How sound gets quieter with distance. |
| **Occlusion** | Muffling a sound when a wall is in the way. |
| **Mixer snapshot** | A saved set of volume and effect levels (indoor, outdoor) that the game blends between. |
| **HRTF** | A filter that imitates how your ears and head shape sound, so direction feels real in headphones. |
| **HUD** | Heads-up display: the score, timer and tickets drawn over the game view. |
| **Diegetic UI** | Interface that exists inside the world (a board, a wrist display) instead of on a flat overlay. |
| **Crosshair** | The square in the middle of the screen showing what you are pointing at. |
| **Baked lighting / lightmap** | Lighting worked out once in advance and saved as a texture, so it is free while playing. |
| **Realtime light** | Light calculated every frame; expensive, so we use only one (the sun). |
| **GI (global illumination)** | Light bouncing off surfaces onto other surfaces. Ours is baked. |
| **Light probe** | A sample point of baked light that moving objects use to light themselves. |
| **Reflection probe** | A captured picture of the surroundings used for shiny surfaces. |
| **Post-processing** | Effects applied to the finished image: colour grading, bloom (glow), vignette (dark edges). |
| **Tone mapping (ACES)** | Squeezes very bright and dark values into what a screen can show, for a film-like look. |
| **Anti-aliasing (SMAA, MSAA)** | Smooths jagged edges. |
| **Triangle** | The smallest piece of a 3D model. Fewer triangles means faster drawing. |
| **Decimation** | Automatically reducing a model's triangles while keeping its shape. |
| **LOD (level of detail)** | Swapping in a simpler model when it is far away. |
| **Draw call** | One "draw this" command from the CPU to the GPU. Too many slow the game down. |
| **Batching / SRP Batcher / instancing** | Ways to draw many objects with fewer or cheaper draw calls. |
| **Occlusion culling** | Not drawing objects hidden behind walls. |
| **Shadow cascade** | Shadow detail split by distance; fewer cascades are cheaper. |
| **Frame time (ms)** | How long one frame takes to draw. 11 ms is roughly 90 frames a second. |
| **Pooling** | Reusing objects instead of creating new ones, to avoid stutters. |
| **Event bus** | A central noticeboard where code posts "this happened" and other code listens. |
| **ScriptableObject** | A Unity data file; we keep all game settings in one. |
| **Assembly (asmdef)** | A separately compiled code module with rules about what it may use. |
| **FBX** | A common 3D model file format. |
| **Combo** | Bonus multiplier for serving a multi-item order perfectly. |
| **Patience** | How long a customer waits before giving up. |

---

## 10. Honesty notes

- Say plainly that the laptop is not a headset and cannot vibrate, and that the F9 display stands in for vibration. Examiners reward this and punish bluffing.
- Desktop mode is our own addition for the demo. It is not a replacement for VR; it drives the same VR rig, which is why all events match.
- Everyone must know the three numbers (triangles, frame time, the draw-call gap) and the event bus (`GameEvents`). Pairings for the cross-explain drill are in the viva pack §C.
- **Checked by code:** 56 automated tests pass, and a scripted run of the real Windows build plays the tutorial, a full round and the results.
- **Not yet checked by a human on the final build:** a full hand-played pass of every interaction, and five human rounds for balance (see `docs/LOG_P2.md` and `docs/QA_CHECKLIST.md`). Do this on the demo laptop before relying on the exact timings above, and change any step that does not behave as written.
- Rehearse twice with a timer, the same way each time. Record the second run as the backup.
