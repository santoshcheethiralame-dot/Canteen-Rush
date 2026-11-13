# P3 log: audio, haptics, UI shell, QA, viva

What I built, what the AI assistant suggested, what I changed or rejected and why. Viva material.

## 2026-11-13 — everything in the P3 brief, first pass (branch `p3/audio-haptics-ui-qa`)

### Built
- **Haptics (`Cr.Haptics`)**: `IHapticsService`; `HapticsServiceBase` (preset lookup, 30 ms per-hand rate limit, ramp coalescing at 50 ms, double pulses); `XRHapticsService` (XRI 3.1.2 `HapticImpulsePlayer.SendHapticImpulse(amplitude, duration)`, found by "Left"/"Right" in the controller names); `NullHapticsService`; `HapticsEventBinder` (§11 table); `HapticsDebugHud` (marker above each controller, corner flash, 8-line log, F9, auto-hides when a real haptic controller is detected); 13 `HapticPreset` assets.
- **Audio (`Cr.Audio`)**: `AudioService` (cue library, 12 pooled one-shots with quietest-voice stealing, custom log rolloff that reaches zero at max distance, Indoor/Outdoor snapshots with a 0.4 s fade); `AudioEventBinder` (every §10 row); `AmbienceEmitter` (loops with indoor/outdoor gating); `AudioOcclusion` (0.1 s linecast on `Environment`, 22 kHz → 1.2 kHz, ×0.7); `AudioZoneTrigger` (polled; adds the canteen `AudioReverbZone`, preset Room); `AudioSceneInstaller` (fridge hum, room bed, crowd, 4 wind/bird emitters, zone components, all found by name/tag); `PlaceholderClips`; 22 `AudioCueSet` assets; `CanteenMixer` (Master → SFX, Ambience, Music, UI, Voice; snapshots Indoor/Outdoor). Project audio: DSP buffer 512 ("Good latency"), 16 real voices.
- **Order bell pointer**: `order_bell` plays at `Order.SeatPosition`, pitch `2^(seat/12)`: seat 0 (nearest) ×1.00 up to seat 7 (farthest) ×1.50, so higher means farther.
- **UI shell (`Cr.UI`)**: `MenuBoard` (Play, Quit, Seated, Teleport), `ResultsBoard` (score, stars, served/correct/wrong/spills/expired, Replay, Menu), `WristHud` (left controller; time, score, wave), `TutorialHint` (3 steps, pulsing ring + card), `UiSoundHooks` (UiHover/UiClick), `UiFactory`, `UiShell` (EventSystem + XR UI Input Module; fallback install into Main).
- **Sandbox (`Cr.P3.Sandbox`)**: `Sandbox_P3.unity`, `P3Sandbox` (floor, head camera you can turn with keys/mouse, stand-in controllers, synthetic round as a stand-in GameManager), `P3DebugPanel` (F10: a button for every event at 8 selectable positions; "every cue" and "every preset" sweeps), `SyntheticRound`.
- **Tests (`Cr.Tests.PlayMode`)**: 8 PlayMode tests for brief items (a)–(e), see `QA_CHECKLIST.md` §0.
- **Assets by script**: `tools/p3/gen_p3_assets.py` synthesises all 35 clips and writes every P3 asset and `.meta` with deterministic GUIDs.
- **Contract change**: `GameEvents.PlayRequested/MenuRequested/ComfortChanged` plus one subscribe line each in P2's `GameManager` and `ComfortSettings` (P2 to review).
- **Docs**: `QA_CHECKLIST.md`, `CREDITS.md` (+ `Assets/Audio/CREDITS.md`), `DECISIONS.md` entry, contracts, viva pack, README.

### AI suggestions I changed or rejected
- *Use `OnTriggerEnter` on the audio zone boxes.* Rejected: the XR rig's camera has no collider or Rigidbody, so triggers would never fire for the listener. Zones are polled against the AudioListener position every 0.1 s.
- *Call `GameManager.Play()` from the menu (P2's own log suggested it).* Rejected: Cr.UI must not reference Cr.Gameplay (contracts §1). Added request events to the bus instead.
- *Unity's built-in logarithmic rolloff.* Changed to a custom curve: the built-in one never reaches silence, so far sources still eat voices. Ours is 1/distance with a fade to zero over the last 15 % of the range.
- *Ramps as one pulse per frame.* Changed: at 90 fps that is 90 impulses/s and breaks the 30 ms rule. The service keeps only the latest amplitude per hand and sends one 60 ms impulse every 50 ms, so the buzz is continuous.
- *Download Kenney/freesound clips.* Not done in this session (downloads needed separate approval); synthesised instead, CC0 by construction. Recorded clips can still replace files 1:1.
- *TextMeshPro for the boards.* Changed to `UnityEngine.UI.Text` with the built-in font: TMP Essential Resources are not in the project and P2's order UI already uses that font (one font family rule).
- *A bug the compiler caught that the first draft missed:* `UnityEngine.XR.Hand` clashes with our `Hand` enum in `XRHapticsService` (fixed with an alias). Another one I caught in review: a method named `RoundState` taking a `RoundState` parameter.

### Haptic + sound pairs (brief task 8: every haptic pairs with a sound)
| Haptic preset | Paired sound (same event) |
|---|---|
| Grab | grab_soft at the item |
| Release | release_soft |
| SocketSnap | tray_clink |
| DoorLatch | fridge_latch (open or close variant) |
| DoorStop | fridge_latch, lower and quieter (a thunk) |
| PumpPress | pump_press |
| WaterFill (ramp) | water_fill_loop, pitch rising with the level |
| TrayTiltWarn (ramp) | tray rattle (tray_clink, faster and louder with the angle) |
| ServeOk | customer_happy + ui_confirm |
| ServePartial | customer_happy (lower) |
| Spill | item_drop / item_drop_glass |
| OrderExpired | customer_angry + ui_error |
| UiTick | ui_click (soft on hover) |
Checked automatically by `C3_EventsSelectTheRightPresetAndPlayASound` (asserts a cue played for every preset).

### Not done / limits (be honest in the viva)
- **Nothing has run in Unity yet.** No working editor on my machine (pinned version not installed, disk full, the other installed version broken). The code is type-checked against the real Unity 6 DLLs and each assembly against only its allowed references; the assets were written as YAML by a script. First Unity run: import, run the PlayMode tests, open Sandbox_P3.
- Manual QA (`QA_CHECKLIST.md`) is written but not executed. Haptics tuning against the HUD (task 8) and the mix balance by ear still need a session in the editor with headphones.
- Meta XR Audio SDK (HRTF) not installed (decision logged). Tutorial "pulsing outline" is a pulsing ring plus a card, not an emission toggle on the target's material (URP Lit only shows emission if the material already has it enabled).
- Cross-explain sessions (task 11) and the release candidate tag `v1.0-rc` on a second machine (task 13) need the team. Record them below when done.
- Stretch (hand-wash step) not started.

## Cross-explain sessions (to fill in)
| Date | Explainer → system | Asker's hard question | Gap found | Fixed by |
|---|---|---|---|---|
| | P1 → event bus (`GameEvents`) | | | |
| | P2 → lighting bake | | | |
| | P3 → XR grab/socket flow | | | |
