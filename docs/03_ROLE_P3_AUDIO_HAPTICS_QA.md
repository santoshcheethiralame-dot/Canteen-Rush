# ROLE P3 — AUDIO, HAPTICS, UI SHELL, QA & VIVA (0.5 person, ≈ 6 person-days)

You are the AI assistant working for **Person 3**, who has half the capacity of the others. Your scope is deliberately all *listeners* plus quality: you react to `GameEvents`, you do not drive gameplay. That keeps you unblocked and your work parallel to everyone else's. Read, in order: `docs/00_MASTER_SPEC.md` (all; §10, §11, §12, §13 are your bible), `docs/04_CONTRACTS.md` (all), then this file, then `docs/06_VIVA_PACK.md`. You do not have the conversation behind these documents; if something is ambiguous ask your human and record the answer in `docs/DECISIONS.md`.

## Paste this at the start of your session

> You are the AI pair for Person 3 on the Canteen Rush VR mini-project (Unity 6000.0.43f1, URP, XR Interaction Toolkit 3.x). Read docs/00_MASTER_SPEC.md, docs/04_CONTRACTS.md and docs/03_ROLE_P3_AUDIO_HAPTICS_QA.md first. You own only the paths under "Owns". Audio, Haptics and UI are listeners: subscribe to GameEvents in OnEnable, unsubscribe in OnDisable, never call gameplay code, never reference Cr.Gameplay. Verify any XR Interaction Toolkit haptics API against the installed version before using it. Every audio file needs a license entry in Assets/Audio/CREDITS.md.

## Mission

Make the game *feel* right and prove it works: spatial audio, a coherent haptic language, the menu/results/wrist UI, the automated + manual QA, and the viva pack so all three people can defend the project. Work against placeholders until real events/art arrive.

## Owns (only you edit)

`Assets/Scripts/Audio/**` (+ `Cr.Audio.asmdef`), `Assets/Scripts/Haptics/**` (+ `Cr.Haptics.asmdef`), `Assets/Scripts/UI/**` except `UI/Gameplay/`, `Assets/Audio/**`, `Assets/Data/Haptics/**`, `Assets/Data/AudioCues/**`, `Assets/Prefabs/UI/**`, `Assets/Prefabs/Systems/Audio.prefab` and `Haptics.prefab`, `Assets/Scenes/Sandbox_P3.unity`, `Assets/Tests/PlayMode/**`, `docs/06_VIVA_PACK.md`, `docs/QA_CHECKLIST.md`, `docs/CREDITS.md`, `docs/LOG_P3.md`, `README.md`.

You consume: `Cr.Core` events (from P2, M0), the Unity project (P1, M0), world prefabs/zone positions (P1). You provide: `Audio.prefab`, `Haptics.prefab`, UI prefabs, QA reports, viva pack.

## Task list with acceptance criteria

### M0–M1 — Skeleton against stubs — ≈ 1.5 days

1. **Haptics service** (`Cr.Haptics`): `IHapticsService`, `XRHapticsService`, `NullHapticsService`, `HapticsEventBinder` (maps `GameEvents` → presets per master spec §11), `HapticPreset` assets under `Assets/Data/Haptics/` generated from the §11 table. First inspect the installed XRI version: in XRI 3.x haptic output is done with a `HapticImpulsePlayer` component on each controller; in older versions via the controller/interactor `SendHapticImpulse`. Implement the thinnest possible adapter that resolves a `Hand` to the right sender. Rate-limit per master spec §11.
   *Done when:* in `Sandbox_P3` a debug panel can raise any `GameEvents` event with buttons and the right preset is selected and logged.
2. **`HapticsDebugHud`:** world-space icon above each controller that flashes (colour = preset, size = amplitude, fade = duration) and a scrolling 8-line log; toggled with a key or menu switch; **enabled by default in the simulator**, auto-disabled when a real device with haptics is detected. This is what the audience sees in the viva.
3. **Audio manager** (`Cr.Audio`): `IAudioService` implementation (`AudioService`), pooled one-shot `AudioSource`s (12), `AudioMixer` asset with groups `Master/SFX/Ambience/Music/UI/Voice` and snapshots `Indoor`, `Outdoor`; `AudioCueSet` assets under `Assets/Data/AudioCues/` (fields in contracts §5); `AudioEventBinder` mapping events → cues per master spec §10 (cue table is exhaustive — implement every row). Placeholder clips: generate beeps/noise bursts in code or use CC0 placeholders, one per cue, so the pipeline works before final sounds exist.
   *Done when:* Sandbox_P3's debug panel triggers every cue at a chosen world position and you can hear its direction when moving the simulated head.
4. **Spatial settings:** every 3D source: `spatialBlend 1`, `dopplerLevel 0`, custom log rolloff, `minDistance/maxDistance` per cue table; project Audio settings: Spatializer plugin — try Meta XR Audio SDK (if installable from the Package Manager/Meta registry); if not feasible in ≤ 0.5 day, ship without and document in `docs/DECISIONS.md`; set DSP buffer to "Good latency"; voice limit 16.

### M2–M3 — Real content and UI shell — ≈ 3 days

5. **Real audio assets:** source CC0/free-licensed clips for every cue (Kenney, freesound CC0, Pixabay, Sonniss). 2–3 variants for repeated sounds (grab, clink, footsteps), loops ≥ 8 s seamless for ambience. Normalise (−16 LUFS), trim, mono for 3D, 44.1 kHz, WAV or OGG Vorbis quality 5. Fill `Assets/Audio/CREDITS.md` (file, URL, author, license, date) — required, graded by honesty in viva.
   *Done when:* every row of the master spec §10 cue table has a real clip assigned and the mix is balanced (no cue clips; SFX −6 dB under UI; ambience −18 dB under SFX).
6. **Spatial extras:**
   - `AudioOcclusion` (raycast listener→source on layer `Environment`, 0.1 s tick, low-pass + volume as in §10).
   - `AudioZoneTrigger` (called by P1's trigger volumes): `SetSnapshot(Indoor/Outdoor)`; also pause/resume indoor-only ambience; cross-fade 0.4 s.
   - `AudioReverbZone` over the canteen (preset `Room`), tuned; none outside.
   - **The order-bell pointer:** on `OrderPlaced`, play `order_bell` at the customer's seat position (use `Order.SeatPosition`, already in the contract). Unique pitch per seat (8 semitone steps) so players learn "high = far table".
   - `amb_patio_crowd` volume tracks `CustomerCount`.
7. **UI shell** (`Cr.UI`, prefabs under `Assets/Prefabs/UI`): `MenuBoard` (Play, Quit, Seated toggle, Teleport toggle — writes to a small `PlayerSettings` ScriptableObject that P2's rig reads; agree its fields with P2 by PR), `ResultsBoard` (total score, correct/wrong/spill/expired counts, stars, Replay, Menu), `WristHUD` (round time, score, wave number; attached to the left controller or toggled by gaze), `TutorialHint` (3 pulsing outline steps; step advance via `GameEvents.OnTutorialStep`, raised by P2's `GameManager`), UI buttons raise `UiHover`/`UiClick`. World-space Canvas with `TrackedDeviceGraphicRaycaster`, an XR UI input module in the scene, text sizes per master spec §12, one font, high contrast, nothing under 24 pt at canvas scale.
   *Done when:* in the simulator you can point and click every button; Replay returns to Playing with a clean state.
8. **Haptics tuning:** with the debug HUD, review every preset against the table; fix timing; verify "every haptic pairs with a sound" (list in `docs/LOG_P3.md`).

### M4–M5 — QA, viva pack, release — ≈ 1.5 days

9. **PlayMode tests** (`Cr.Tests.PlayMode`): (a) Main/Sandbox scene loads with no errors logged; (b) scripted full round via synthetic `GameEvents` calls reaches `Results`; (c) every audio cue ID in the table resolves to a clip; every haptic preset resolves; (d) 3 consecutive rounds: managed memory delta < 5 MB, no `NullReferenceException`; (e) event subscription counts return to baseline after a round (use reflection on `GameEvents` delegates).
10. **Manual QA:** write and execute `docs/QA_CHECKLIST.md`: each station in §6.2; each cue in §10; each haptic in §11; menu/results; comfort (vignette, snap turn); restart ×3; low-framerate behaviour; seated mode. File bugs as GitHub issues with `[P1]/[P2]` prefixes and repro steps.
11. **Viva pack:** complete and personalise `docs/06_VIVA_PACK.md` (Q&A and 4-minute demo script already drafted; add screenshots, actual numbers from `docs/PERF_REPORT.md`, actual cue list and haptic list). Run two cross-explain sessions (each person explains another person's system in 3 minutes); record gaps in `docs/LOG_P3.md`.
12. **README.md** at repo root: what it is, how to run (Windows build + Editor), controls with the simulator, credits, architecture diagram (copy §7.2 of the master spec).
13. **Release candidate:** with P1, verify the Windows build on a second machine; tag `v1.0-rc`.

## Stretch (only if everything above is done)

- Hand-wash step at a washroom basin (master spec §6.7): needs P1's basin prop and P2's item gating — open a `DECISIONS.md` entry first.

## Constraints and traps

- **Never** reference `Cr.Gameplay`/other listeners from your assemblies. If you need extra data (seat position, tutorial step), request an additive change to `GameEvents`/`Order` via a PR on `docs/04_CONTRACTS.md` and `Cr.Core`.
- Never call `GameEvents.ResetAll()` during a session.
- Audio files and licensing: only clips with clear licenses; AI-generated sounds are allowed only if the generator's terms permit and are listed in `CREDITS.md`.
- LFS: keep total audio under ~30 MB (OGG Vorbis, mono for 3D, short loops). Do not recommit re-encoded versions of the same file repeatedly.
- The simulator cannot vibrate hardware or reproduce HRTF perfectly: demonstrate with the Debug HUD and headphones, and say so honestly in the viva.
- You have half capacity: if time runs short, cut in this order — Meta XR Audio SDK, occlusion, reverb zone, tutorial hints, QA PlayMode tests (b)(d)(e). Never cut: all cues in the table with at least placeholder-quality audio, all haptic presets, menu/results UI, the viva pack.

## Definition of done for you

Every cue and preset in the spec implemented and demonstrable; Debug HUD working in the simulator; menu/results/wrist HUD usable with the ray interactor; PlayMode tests green; QA checklist executed with results logged; `CREDITS.md` complete; viva pack personalised; `docs/LOG_P3.md` written; README done.
