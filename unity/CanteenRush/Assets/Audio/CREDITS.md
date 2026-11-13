# Audio credits

Every audio file in `Assets/Audio/Clips/` is listed here. Graded on honesty in the viva.

**Source of all 35 clips:** procedurally synthesised by `tools/p3/gen_p3_assets.py` (written for this project by P3 with AI assistance, 2026-11-13). The script builds each sound from white noise, FFT band-pass filters, decaying sine partials and simple formant filters. No recordings, samples, sound libraries, AI audio generators or third-party clips were used, so there is nothing to attribute. The clips are released under **CC0 1.0** like any other team-authored asset (the repository code is MIT).

How to regenerate: `python tools/p3/gen_p3_assets.py` from the repo root (needs numpy). The random seed is fixed, so the output is identical on every machine. `--no-audio` regenerates only the metadata and data assets.

Processing (same for every file): mono, 44.1 kHz, 16-bit WAV in the repo; Unity imports it as Vorbis quality 0.5, forced mono. Loudness is normalised to −16 dBFS RMS for SFX/UI/voice, −18 for music and −20 for ambience, with peaks limited to −1 dBFS. This is an RMS approximation of −16 LUFS: true LUFS needs K-weighting, and on these short synthetic clips the two agree within about 2 dB. Loops (≥ 8 s) are made seamless with a 1 s equal-power crossfade of tail into head. Mix balance is set in `Audio/Mixer/CanteenMixer.mixer`: UI 0 dB, SFX −6 dB, Voice −6 dB, Ambience −24 dB (18 dB under SFX), Music −20 dB.

| File | Cue | Made from | Author | License | Date |
|---|---|---|---|---|---|
| Ambience/amb_fridge_hum_01.wav | amb_fridge_hum | 100/200/300 Hz compressor hum + band noise, slow AM, 10 s loop | P3 (script) | CC0 | 2026-11-13 |
| Ambience/amb_canteen_room_01.wav | amb_canteen_room | 7 syllable-modulated noise "voices" + extractor-fan rumble, 12 s loop | P3 (script) | CC0 | 2026-11-13 |
| Ambience/amb_patio_birds_01.wav | amb_patio_birds | rooftop wind with gusts + sparse bird chirps (FM sweeps), 16 s loop | P3 (script) | CC0 | 2026-11-13 |
| Ambience/amb_patio_crowd_01.wav | amb_patio_crowd | 12-voice murmur + cutlery clinks, 12 s loop | P3 (script) | CC0 | 2026-11-13 |
| Sfx/fridge_latch_01.wav | fridge_latch (open) | two band-noise clicks + rattle | P3 (script) | CC0 | 2026-11-13 |
| Sfx/fridge_latch_02.wav | fridge_latch (close) | 95 Hz thunk + click | P3 (script) | CC0 | 2026-11-13 |
| Sfx/pump_press_01.wav | pump_press | inharmonic metal partials (420 Hz base) + air burst | P3 (script) | CC0 | 2026-11-13 |
| Sfx/water_fill_loop_01.wav | water_fill_loop | 140 rising-pitch bubbles over band noise, 8.5 s loop | P3 (script) | CC0 | 2026-11-13 |
| Sfx/grab_soft_01..03.wav | grab_soft | band-noise rustles, 3 variants | P3 (script) | CC0 | 2026-11-13 |
| Sfx/release_soft_01..03.wav | release_soft | softer, lower rustles, 3 variants | P3 (script) | CC0 | 2026-11-13 |
| Sfx/tray_clink_01..03.wav | tray_clink | contact tick + inharmonic ring (2.3/2.65/3.05 kHz) | P3 (script) | CC0 | 2026-11-13 |
| Sfx/item_drop_01..02.wav | item_drop | falling-pitch 85/110 Hz thud + noise | P3 (script) | CC0 | 2026-11-13 |
| Sfx/item_drop_glass_01..02.wav | item_drop_glass | 40 random glass pings + crackle | P3 (script) | CC0 | 2026-11-13 |
| Sfx/order_bell_01.wav | order_bell | counter-bell partials 1 : 2.76 : 5.40 : 8.93 at 1320 Hz | P3 (script) | CC0 | 2026-11-13 |
| Sfx/customer_happy_01.wav | customer_happy | two rising wordless "mm-hm" notes (pulse train + formants) | P3 (script) | CC0 | 2026-11-13 |
| Sfx/customer_angry_01.wav | customer_angry | falling, harsher "hmph" | P3 (script) | CC0 | 2026-11-13 |
| Sfx/footsteps_tile_01..03.wav | footsteps_tile | short bright click + thump | P3 (script) | CC0 | 2026-11-13 |
| Sfx/footsteps_stone_01..03.wav | footsteps_stone | longer gritty noise + thump | P3 (script) | CC0 | 2026-11-13 |
| Ui/ui_click_01.wav | ui_click | 2 kHz tick, 50 ms | P3 (script) | CC0 | 2026-11-13 |
| Ui/ui_confirm_01.wav | ui_confirm | E6 → A6 chime | P3 (script) | CC0 | 2026-11-13 |
| Ui/ui_error_01.wav | ui_error | 220 → 175 Hz square buzz | P3 (script) | CC0 | 2026-11-13 |
| Music/music_loop_01.wav | music_loop | 120 BPM, 8 bars, I–vi–IV–V in C (sine e-piano, bass, hats), 16 s loop | P3 (script) | CC0 | 2026-11-13 |
| Music/round_end_sting_01.wav | round_end_sting | C–E–G–C arpeggio chime | P3 (script) | CC0 | 2026-11-13 |

Total: 35 files, 7.6 MB as WAV in Git LFS (well under the 30 MB audio budget).

If recorded CC0 clips (e.g. Kenney.nl audio packs, freesound.org filtered to CC0) replace any of these later, add a row with file, URL, author, license and download date, and keep the cue IDs and file names (`<cueId>_<nn>.wav`) so no asset reference changes.
