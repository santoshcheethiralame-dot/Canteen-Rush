"""Generate every Unity asset P3 owns, deterministically, without opening Unity.

Why a script: the team's Unity Editor was not usable on P3's machine, and hand-made assets in Unity produce random
GUIDs that conflict between clones. Everything here is text (YAML) or synthesised audio, so it is reviewable,
rerunnable and identical on every machine. Re-run after changing a table below; commit the outputs with their .meta.

Outputs (all under unity/CanteenRush/Assets):
  Audio/Clips/**.wav (+ .meta, Vorbis q0.5, mono) .. one clip per cue (2-3 variants for repeated sounds), synthesised
  Audio/Mixer/CanteenMixer.mixer ................ groups Master/SFX/Ambience/Music/UI/Voice, snapshots Indoor/Outdoor
  Data/Haptics/*.asset .......................... 13 HapticPreset assets (MASTER_SPEC §11 table)
  Data/AudioCues/*.asset ........................ 22 AudioCueSet assets (MASTER_SPEC §10 table)
  Prefabs/Systems/Audio.prefab, Haptics.prefab .. picked up by P1's MainSceneBuilder
  Prefabs/UI/UIShell.prefab ..................... the UI shell root
  Prefabs/UI/Resources/P3/*.prefab .............. runtime fallback copies (UiShell fills empty _Audio/_Haptics/UI roots)
  Scenes/Sandbox_P3.unity ....................... P3's sandbox
  .meta for every new folder/script/asmdef ...... deterministic GUIDs, same scheme as tools/gen_folder_metas.py

Usage:  python tools/p3/gen_p3_assets.py            (from the repo root; needs numpy)
        python tools/p3/gen_p3_assets.py --no-audio (skip re-synthesising WAVs: keeps LFS history small)
"""
import hashlib
import os
import sys
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ASSETS_REL = "unity/CanteenRush/Assets"
RATE = 44100
NL = "\n"
NO_AUDIO = "--no-audio" in sys.argv


def L(rel):
    """Absolute path with the Windows long-path prefix (the repo may live in a deep folder)."""
    p = os.path.abspath(os.path.join(ROOT, rel))
    return "\\\\?\\" + p.replace("/", "\\") if os.name == "nt" else p


def guid_for(rel_asset_path):
    # Same scheme as tools/gen_folder_metas.py: md5("canteenrush:" + "unity/CanteenRush/Assets/...").
    return hashlib.md5(("canteenrush:" + rel_asset_path).encode()).hexdigest()


def write(rel, text):
    os.makedirs(os.path.dirname(L(rel)), exist_ok=True)
    with open(L(rel), "w", newline="\n", encoding="utf-8") as f:
        f.write(text)


def read_guid(rel):
    with open(L(rel + ".meta"), encoding="utf-8") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split()[1]
    raise SystemExit("no guid in " + rel + ".meta")


def meta(rel, importer_block, force=False):
    """Write rel.meta (only if missing, unless force). Returns the GUID actually in the file."""
    path = rel + ".meta"
    if os.path.exists(L(path)) and not force:
        return read_guid(rel)
    g = guid_for(rel)
    write(path, "fileFormatVersion: 2" + NL + "guid: " + g + NL + importer_block)
    return g


FOLDER = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
SCRIPT = ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n"
          "  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
ASMDEF = "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
NATIVE = "NativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {main}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
PREFAB = "PrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
SCENE = "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
TEXT = "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def audio_meta(stream):
    # Vorbis quality 0.5 (= "quality 50", spec asks for OGG q5 ~ 0.5), forced mono; long loops stream.
    load_type = 2 if stream else 0
    return ("AudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n    serializedVersion: 2\n"
            "    loadType: %d\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n"
            "    quality: 0.5\n    conversionMode: 0\n    preloadAudioData: %d\n  platformSettingOverrides: {}\n"
            "  forceToMono: 1\n  normalize: 0\n  loadInBackground: %d\n  ambisonic: 0\n  3D: 1\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n") % (load_type, 0 if stream else 1, 1 if stream else 0)


# --------------------------------------------------------------------------------------------------------------------
# Audio synthesis. Every clip is original, generated here from noise and sine partials (team-authored, CC0).
# --------------------------------------------------------------------------------------------------------------------
rng = np.random.default_rng(20261113)


def t_axis(sec):
    return np.arange(int(sec * RATE)) / RATE


def onepole_lp(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):  # small clips only; loops use the FFT filter below
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def band(x, lo, hi):
    """Brick-wall band-pass via FFT (fine for noise beds; zero phase)."""
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    X[(f < lo) | (f > hi)] = 0
    return np.fft.irfft(X, len(x))


def env_ad(n, attack, decay_tau):
    t = np.arange(n) / RATE
    e = np.exp(-t / decay_tau)
    a = int(attack * RATE)
    if a > 0:
        e[:a] *= np.linspace(0, 1, a)
    return e


def fades(x, ms=5):
    n = int(ms / 1000 * RATE)
    if n and len(x) > 2 * n:
        x[:n] *= np.linspace(0, 1, n)
        x[-n:] *= np.linspace(1, 0, n)
    return x


def make_loop(x, xfade=1.0):
    """Seamless loop: crossfade the tail into the head (equal power) and drop the tail."""
    n = int(xfade * RATE)
    head, body, tail = x[:n], x[n:-n] if n else x, x[-n:]
    w = np.linspace(0, np.pi / 2, n)
    blended = tail * np.cos(w) + head * np.sin(w)
    return np.concatenate([blended, x[n:len(x) - n]])


def partials(sec, f0, ratios, amps, tau):
    t = t_axis(sec)
    y = np.zeros_like(t)
    for r, a in zip(ratios, amps):
        y += a * np.sin(2 * np.pi * f0 * r * t) * np.exp(-t / (tau / max(1.0, r ** 0.5)))
    return y


def normalise(x, target_db, peak_db=-1.0):
    """Approximate loudness normalisation: RMS of the non-silent part to target dBFS, peak limited.
    (True LUFS needs K-weighting and gating; for short synthetic clips RMS is within ~1-2 dB.)"""
    active = x[np.abs(x) > 1e-4]
    rms = np.sqrt(np.mean(active ** 2)) if len(active) else 1e-9
    y = x * (10 ** (target_db / 20) / max(rms, 1e-9))
    peak = np.max(np.abs(y)) if len(y) else 0
    lim = 10 ** (peak_db / 20)
    if peak > lim:
        y *= lim / peak
    return y


def noise(sec):
    return rng.standard_normal(int(sec * RATE))


def sweep(sec, f_start, f_end, curve=1.0):
    t = t_axis(sec)
    k = (t / sec) ** curve
    f = f_start + (f_end - f_start) * k
    return np.sin(2 * np.pi * np.cumsum(f) / RATE)


def bubble(sec=0.06, f=600):
    t = t_axis(sec)
    fr = f * (1 + 2.5 * t / sec)
    return np.sin(2 * np.pi * np.cumsum(fr) / RATE) * np.exp(-t / (sec / 3))


def place(dst, src, at):
    i = int(at * RATE)
    j = min(len(dst), i + len(src))
    dst[i:j] += src[:j - i]


def voice_babble(sec, voices, lo=250, hi=2600):
    """Murmur: several band-limited noise 'voices' with syllable-rate (3-6 Hz) amplitude modulation."""
    t = t_axis(sec)
    out = np.zeros_like(t)
    for v in range(voices):
        rate = rng.uniform(3, 6)
        phase = rng.uniform(0, 2 * np.pi)
        talk = np.clip(np.sin(2 * np.pi * rate * t + phase), 0, None) ** 1.5
        turn = (np.sin(2 * np.pi * rng.uniform(0.05, 0.15) * t + rng.uniform(0, 6)) > -0.2).astype(float)
        out += band(noise(sec), lo * rng.uniform(0.8, 1.3), hi * rng.uniform(0.7, 1.1)) * talk * turn
    return out


def vocal(sec, f_start, f_end, formants, harsh=0.0):
    """Wordless vocal bark: a glottal-ish pulse train shaped by a few formant bands."""
    t = t_axis(sec)
    f = np.linspace(f_start, f_end, len(t))
    phase = np.cumsum(f) / RATE
    saw = 2 * (phase % 1) - 1
    src = saw + harsh * np.sign(np.sin(2 * np.pi * phase))
    y = np.zeros_like(t)
    for (fc, bw, g) in formants:
        y += g * band(src, fc - bw, fc + bw)
    e = env_ad(len(t), 0.02, sec / 2.5)
    return y * e


def gen_clips():
    C = {}  # cue -> list of (folder, arrays, loop)
    # ---- Ambience (loops >= 8 s, seamless) ----
    t = t_axis(11)
    hum = (0.5 * np.sin(2 * np.pi * 100 * t) + 0.25 * np.sin(2 * np.pi * 200 * t) + 0.12 * np.sin(2 * np.pi * 300 * t)
           + 0.06 * np.sin(2 * np.pi * 50 * t)) * (1 + 0.08 * np.sin(2 * np.pi * 0.5 * t))
    hum += 0.15 * band(noise(11), 80, 900)
    C["amb_fridge_hum"] = [("Ambience", make_loop(hum), True)]

    room = voice_babble(13, 7) + 0.35 * band(noise(13), 60, 400)  # murmur + extractor fan rumble
    C["amb_canteen_room"] = [("Ambience", make_loop(room), True)]

    # Rooftop bed: wind with slow gusts + sparse distant birds (P1: "it is a rooftop, wind fits better than birds").
    sec = 17
    t = t_axis(sec)
    gust = 0.55 + 0.45 * np.sin(2 * np.pi * 0.07 * t) * np.sin(2 * np.pi * 0.023 * t + 1)
    wind = band(noise(sec), 70, 1100) * gust + 0.25 * band(noise(sec), 1500, 3500) * gust ** 3
    for at in rng.uniform(0.5, sec - 2.5, 7):
        chirp = sum(sweep(0.09, rng.uniform(2800, 3400), rng.uniform(4200, 5200), 0.6) * env_ad(int(0.09 * RATE), 0.005, 0.03)
                    for _ in range(1))
        for k in range(int(rng.integers(2, 4))):
            place(wind, 0.35 * chirp, at + k * 0.13)
    C["amb_patio_birds"] = [("Ambience", make_loop(wind), True)]

    crowd = voice_babble(13, 12, 220, 2400)
    for at in rng.uniform(1, 11, 4):  # the odd cutlery clink
        place(crowd, 0.25 * partials(0.3, rng.uniform(2200, 3200), [1, 2.7, 4.1], [1, .5, .3], 0.05), at)
    C["amb_patio_crowd"] = [("Ambience", make_loop(crowd), True)]

    # ---- Stations ----
    click = lambda f, tau: band(noise(0.06), f * 0.6, f * 1.6) * env_ad(int(0.06 * RATE), 0.001, tau)
    latch_open = np.zeros(int(0.25 * RATE)); place(latch_open, click(3000, 0.006), 0); place(latch_open, 0.5 * click(1800, 0.01), 0.035)
    latch_open += 0.3 * band(noise(0.25), 200, 900) * env_ad(len(latch_open), 0.0, 0.03)
    latch_close = np.zeros(int(0.3 * RATE))
    place(latch_close, partials(0.3, 95, [1, 2.1, 3.3], [1, .4, .2], 0.05), 0)
    place(latch_close, click(2600, 0.008), 0.01)
    C["fridge_latch"] = [("Sfx", latch_open, False), ("Sfx", latch_close, False)]  # variant 0 = open, 1 = close

    clank = partials(0.5, 420, [1, 2.69, 5.26, 8.1], [1, .7, .45, .25], 0.12) + 0.4 * band(noise(0.5), 300, 2500) * env_ad(int(0.5 * RATE), 0, 0.03)
    C["pump_press"] = [("Sfx", clank, False)]

    sec = 9
    water = 0.35 * band(noise(sec), 400, 4000) * (0.7 + 0.3 * np.sin(2 * np.pi * 1.3 * t_axis(sec)))
    for at in rng.uniform(0, sec - 0.1, 140):
        place(water, 0.5 * bubble(rng.uniform(0.03, 0.08), rng.uniform(400, 1200)), at)
    C["water_fill_loop"] = [("Sfx", make_loop(water, 0.5), True)]

    # ---- Items ----
    def rustle(f_lo, f_hi, sec, tau):
        return band(noise(sec), f_lo, f_hi) * env_ad(int(sec * RATE), 0.008, tau)
    C["grab_soft"] = [("Sfx", rustle(600 + 150 * i, 4000 + 400 * i, 0.12, 0.03), False) for i in range(3)]
    C["release_soft"] = [("Sfx", rustle(300 + 100 * i, 2200 + 300 * i, 0.12, 0.025), False) for i in range(3)]
    def clink(f):
        y = partials(0.35, f, [1, 1.59, 2.31, 3.1], [1, .6, .4, .25], 0.06)
        place(y, 0.3 * click(5000, 0.003), 0)  # the contact tick before the ring
        return y
    C["tray_clink"] = [("Sfx", clink(f), False) for f in (2300, 2650, 3050)]

    def thud(f, sec=0.45):
        tt = t_axis(sec)
        body = np.sin(2 * np.pi * np.cumsum(f * (1 - 0.5 * tt / sec)) / RATE) * np.exp(-tt / 0.07)
        return body + 0.5 * band(noise(sec), 100, 1500) * np.exp(-tt / 0.03)
    C["item_drop"] = [("Sfx", thud(85), False), ("Sfx", thud(110), False)]

    def glass(sec=0.8):
        y = np.zeros(int(sec * RATE))
        for at in np.sort(rng.exponential(0.08, 40)):
            if at < sec - 0.12:
                place(y, 0.6 * partials(0.12, rng.uniform(2500, 7000), [1, 1.7, 2.9], [1, .5, .3], 0.02), at)
        y += 0.5 * band(noise(sec), 2000, 9000) * env_ad(len(y), 0.0, 0.12)
        return y
    C["item_drop_glass"] = [("Sfx", glass(), False), ("Sfx", glass(), False)]

    # ---- Orders / customers ----
    C["order_bell"] = [("Sfx", partials(1.6, 1320, [1, 2.76, 5.40, 8.93], [1, .55, .3, .15], 0.55), False)]
    C["customer_happy"] = [("Sfx", np.concatenate([vocal(0.18, 210, 230, [(700, 150, 1), (1200, 200, .6)]), np.zeros(int(0.04 * RATE)),
                                                   vocal(0.28, 250, 330, [(600, 150, 1), (1100, 200, .7), (2500, 300, .2)])]), False)]
    C["customer_angry"] = [("Sfx", vocal(0.45, 190, 120, [(650, 180, 1), (1100, 220, .7), (2600, 400, .4)], harsh=0.5), False)]

    # ---- Player ----
    def step(lo, hi, tau, thump):
        y = band(noise(0.18), lo, hi) * env_ad(int(0.18 * RATE), 0.002, tau)
        y += thump * partials(0.18, 70, [1, 1.9], [1, .3], 0.03)
        return y
    C["footsteps_tile"] = [("Sfx", step(1500 + 200 * i, 7000, 0.012, 0.6), False) for i in range(3)]
    C["footsteps_stone"] = [("Sfx", step(300 + 80 * i, 3500, 0.03, 0.4), False) for i in range(3)]

    # ---- UI (2D) ----
    C["ui_click"] = [("Ui", partials(0.05, 2000, [1, 2.0], [1, .3], 0.008), False)]
    conf = np.zeros(int(0.45 * RATE)); place(conf, partials(0.25, 1318.5, [1, 2, 3], [1, .3, .1], 0.09), 0); place(conf, partials(0.3, 1760, [1, 2, 3], [1, .3, .1], 0.1), 0.11)
    C["ui_confirm"] = [("Ui", conf, False)]
    tt = t_axis(0.4)
    err = np.sign(np.sin(2 * np.pi * np.where(tt < 0.18, 220, 175) * tt)) * 0.5 * np.where((tt > 0.15) & (tt < 0.2), 0, 1) * np.exp(-tt / 0.25)
    C["ui_error"] = [("Ui", band(err, 80, 3000), False)]

    # ---- Music: 16 s, 120 BPM, 8 bars, I-vi-IV-V in C; electric-piano-ish chords, bass, soft hats ----
    sec = 16.0
    music = np.zeros(int(sec * RATE))
    chords = [(261.63, 329.63, 392.0), (220.0, 261.63, 329.63), (174.61, 220.0, 261.63), (196.0, 246.94, 293.66)] * 2
    beat = 0.5
    for bar, ch in enumerate(chords):
        start = bar * 2.0
        for b in range(4):
            for f in ch:
                place(music, 0.22 * partials(0.9, f, [1, 2, 3], [1, .25, .08], 0.35), start + b * beat + (0.25 if b % 2 else 0))
            place(music, 0.45 * partials(0.45, ch[0] / 2, [1, 2], [1, .2], 0.2), start + b * beat)
            place(music, 0.08 * band(noise(0.05), 6000, 12000) * env_ad(int(0.05 * RATE), 0.001, 0.01), start + b * beat + 0.25)
    C["music_loop"] = [("Music", make_loop(np.concatenate([music, music[:int(1.0 * RATE)]]), 1.0), True)]

    sting = np.zeros(int(1.8 * RATE))
    for i, f in enumerate((523.25, 659.25, 783.99, 1046.5)):
        place(sting, partials(1.2 if i == 3 else 0.5, f, [1, 2, 3, 4.2], [1, .4, .2, .1], 0.5 if i == 3 else 0.2), i * 0.12)
    C["round_end_sting"] = [("Music", sting, False)]
    return C


# Loudness targets (dBFS RMS). Mix balance itself is done in the mixer (UI 0, SFX -6, Ambience -24, Music -20 dB).
TARGET = {"Ambience": -20.0, "Music": -18.0}
DEFAULT_TARGET = -16.0


def write_wav(rel, x):
    data = np.clip(x, -1, 1)
    pcm = (data * 32767).astype("<i2")
    os.makedirs(os.path.dirname(L(rel)), exist_ok=True)
    with wave.open(L(rel), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())


def clip_paths():
    """cue -> list of (rel path, loop). Stable even with --no-audio."""
    out = {}
    for cue, folder, n, loop in CLIP_LAYOUT:
        out[cue] = [("%s/Audio/Clips/%s/%s_%02d.wav" % (ASSETS_REL, folder, cue, i + 1), loop) for i in range(n)]
    return out


CLIP_LAYOUT = [  # cue, folder, variants, loop
    ("amb_fridge_hum", "Ambience", 1, True), ("amb_canteen_room", "Ambience", 1, True), ("amb_patio_birds", "Ambience", 1, True),
    ("amb_patio_crowd", "Ambience", 1, True), ("fridge_latch", "Sfx", 2, False), ("pump_press", "Sfx", 1, False),
    ("water_fill_loop", "Sfx", 1, True), ("grab_soft", "Sfx", 3, False), ("release_soft", "Sfx", 3, False),
    ("tray_clink", "Sfx", 3, False), ("item_drop", "Sfx", 2, False), ("item_drop_glass", "Sfx", 2, False),
    ("order_bell", "Sfx", 1, False), ("customer_happy", "Sfx", 1, False), ("customer_angry", "Sfx", 1, False),
    ("footsteps_tile", "Sfx", 3, False), ("footsteps_stone", "Sfx", 3, False), ("ui_click", "Ui", 1, False),
    ("ui_confirm", "Ui", 1, False), ("ui_error", "Ui", 1, False), ("music_loop", "Music", 1, True), ("round_end_sting", "Music", 1, False),
]


def build_audio():
    paths = clip_paths()
    guids = {}
    if not NO_AUDIO:
        clips = gen_clips()
        for cue, entries in clips.items():
            for (rel, loop), (folder, x, _) in zip(paths[cue], entries):
                x = normalise(np.asarray(x, dtype=np.float64), TARGET.get(folder, DEFAULT_TARGET))
                if not loop:
                    x = fades(x, 3)
                write_wav(rel, x)
    for cue, entries in paths.items():
        guids[cue] = []
        for rel, loop in entries:
            if not os.path.exists(L(rel)):
                raise SystemExit("missing clip (run without --no-audio): " + rel)
            guids[cue].append(meta(rel, audio_meta(stream=loop and ("music" in rel or "amb_" in rel)), force=True))
    return guids


# --------------------------------------------------------------------------------------------------------------------
# YAML helpers
# --------------------------------------------------------------------------------------------------------------------
HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"


def ref(file_id=0, guid=None, typ=2):
    return "{fileID: %d}" % file_id if guid is None else "{fileID: %d, guid: %s, type: %d}" % (file_id, guid, typ)


def so_asset(script_guid, name, fields):
    return (HEADER + "--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n"
            "  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: %s\n  m_EditorClassIdentifier: \n%s"
            % (script_guid, name, fields))


def color(c):
    return "{r: %g, g: %g, b: %g, a: %g}" % c


# ---- Haptic presets: MASTER_SPEC §11 ----
PRESETS = [  # name, amp, dur, ramp, pulses, gap, colour, rampIn, rampAmp
    ("Grab", .25, .04, False, 1, 0, (0.3, 0.8, 1, 1), (0, 1), (.25, .25)),
    ("Release", .10, .03, False, 1, 0, (0.6, 0.6, 0.6, 1), (0, 1), (.1, .1)),
    ("SocketSnap", .35, .06, False, 1, 0, (0.2, 1, 0.6, 1), (0, 1), (.35, .35)),
    ("DoorLatch", .45, .05, False, 1, 0, (1, 0.85, 0.3, 1), (0, 1), (.45, .45)),
    ("DoorStop", .60, .08, False, 1, 0, (1, 0.6, 0.2, 1), (0, 1), (.6, .6)),
    ("PumpPress", .50, .07, False, 1, 0, (0.3, 0.6, 1, 1), (0, 1), (.5, .5)),
    ("WaterFill", .10, .05, True, 1, 0, (0.2, 0.7, 1, 1), (0, 1), (.10, .45)),
    ("TrayTiltWarn", .15, .05, True, 1, 0, (1, 0.5, 0.1, 1), (20, 35), (.15, .70)),
    ("ServeOk", .50, .06, False, 2, .05, (0.2, 1, 0.3, 1), (0, 1), (.5, .5)),
    ("ServePartial", .35, .12, False, 1, 0, (0.8, 1, 0.3, 1), (0, 1), (.35, .35)),
    ("Spill", .85, .10, False, 1, 0, (1, 0.25, 0.2, 1), (0, 1), (.85, .85)),
    ("OrderExpired", .70, .30, False, 1, 0, (0.9, 0.1, 0.1, 1), (0, 1), (.7, .7)),
    ("UiTick", .15, .02, False, 1, 0, (1, 1, 1, 1), (0, 1), (.15, .15)),
]

# ---- Cue sets: MASTER_SPEC §10 (min/max metres; bus; spatial; occludable; loop; pitch; volume) ----
CUES = [
    ("amb_fridge_hum", 1, 9, "Ambience", True, True, True, (1, 1), (.7, .7)),
    ("amb_canteen_room", 2, 20, "Ambience", False, False, True, (1, 1), (.8, .8)),
    ("amb_patio_birds", 3, 25, "Ambience", True, True, True, (1, 1), (.6, .6)),
    ("amb_patio_crowd", 3, 20, "Ambience", True, True, True, (1, 1), (.9, .9)),
    ("fridge_latch", 1, 8, "SFX", True, False, False, (.97, 1.03), (.9, 1)),
    ("pump_press", 1, 6, "SFX", True, False, False, (.95, 1.05), (.9, 1)),
    ("water_fill_loop", 1, 6, "SFX", True, False, True, (1, 1), (.8, .8)),
    ("grab_soft", 1, 5, "SFX", True, False, False, (.95, 1.05), (.7, .9)),
    ("release_soft", 1, 5, "SFX", True, False, False, (.95, 1.05), (.6, .8)),
    ("tray_clink", 1, 8, "SFX", True, False, False, (.95, 1.05), (1, 1)),
    ("item_drop", 1, 12, "SFX", True, False, False, (.93, 1.05), (1, 1)),
    ("item_drop_glass", 1, 12, "SFX", True, False, False, (.95, 1.05), (1, 1)),
    ("order_bell", 2, 30, "SFX", True, False, False, (1, 1), (1, 1)),
    ("customer_happy", 2, 20, "Voice", True, True, False, (.95, 1.08), (.9, 1)),
    ("customer_angry", 2, 20, "Voice", True, True, False, (.95, 1.05), (.9, 1)),
    ("footsteps_tile", 1, 5, "SFX", False, False, False, (.92, 1.08), (.35, .45)),
    ("footsteps_stone", 1, 5, "SFX", False, False, False, (.92, 1.08), (.35, .45)),
    ("ui_click", 1, 5, "UI", False, False, False, (.98, 1.02), (.8, .8)),
    ("ui_confirm", 1, 5, "UI", False, False, False, (1, 1), (.9, .9)),
    ("ui_error", 1, 5, "UI", False, False, False, (1, 1), (.9, .9)),
    ("music_loop", 1, 5, "Music", False, False, True, (1, 1), (.8, .8)),
    ("round_end_sting", 1, 5, "Music", False, False, False, (1, 1), (1, 1)),
]
BUS_INDEX = {"SFX": 0, "Ambience": 1, "Music": 2, "UI": 3, "Voice": 4}

# ---- Mixer ----
MIXER_REL = ASSETS_REL + "/Audio/Mixer/CanteenMixer.mixer"
GROUPS = ["Master", "SFX", "Ambience", "Music", "UI", "Voice"]
GROUP_DB = {"Master": 0, "SFX": -6, "Ambience": -24, "Music": -20, "UI": 0, "Voice": -6}
GROUP_ID = {g: 24300002 + 10 * i for i, g in enumerate(GROUPS)}
ATTEN_ID = {g: 24400004 + 10 * i for i, g in enumerate(GROUPS)}
LOWPASS_ID = 24400099
SNAP_ID = {"Outdoor": 24500006, "Indoor": 24500016}


def h(label):
    return hashlib.md5(("canteenrush-mixer:" + label).encode()).hexdigest()


def build_mixer():
    g = meta(MIXER_REL, NATIVE.format(main=24100000))
    o = [HEADER]
    o.append("--- !u!241 &24100000\nAudioMixerController:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
             "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: CanteenMixer\n  m_OutputGroup: {fileID: 0}\n"
             "  m_MasterGroup: {fileID: %d}\n  m_Snapshots:\n  - {fileID: %d}\n  - {fileID: %d}\n  m_StartSnapshot: {fileID: %d}\n"
             "  m_SuspendThreshold: -80\n  m_EnableSuspend: 1\n  m_UpdateMode: 0\n  m_ExposedParameters: []\n"
             "  m_AudioMixerGroupViews:\n  - guids:\n%s    name: View\n  m_CurrentViewIndex: 0\n  m_TargetSnapshot: {fileID: %d}\n"
             % (GROUP_ID["Master"], SNAP_ID["Outdoor"], SNAP_ID["Indoor"], SNAP_ID["Outdoor"],
                "".join("    - %s\n" % h("group:" + x) for x in GROUPS), SNAP_ID["Outdoor"]))
    for grp in GROUPS:
        children = "".join("  - {fileID: %d}\n" % GROUP_ID[c] for c in GROUPS[1:]) if grp == "Master" else ""
        effects = "  - {fileID: %d}\n" % ATTEN_ID[grp] + ("  - {fileID: %d}\n" % LOWPASS_ID if grp == "Master" else "")
        o.append("--- !u!243 &%d\nAudioMixerGroupController:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                 "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: %s\n  m_AudioMixer: {fileID: 24100000}\n"
                 "  m_GroupID: %s\n  m_Children:%s\n%s  m_Volume: %s\n  m_Pitch: %s\n  m_Send: 00000000000000000000000000000000\n"
                 "  m_Effects:\n%s  m_UserColorIndex: 0\n  m_Mute: 0\n  m_Solo: 0\n  m_BypassEffects: 0\n"
                 % (GROUP_ID[grp], grp, h("group:" + grp), "" if children else " []", children, h("vol:" + grp), h("pitch:" + grp), effects))
        o.append("--- !u!244 &%d\nAudioMixerEffectController:\n  m_ObjectHideFlags: 3\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                 "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: \n  m_EffectID: %s\n  m_EffectName: Attenuation\n"
                 "  m_MixLevel: %s\n  m_Parameters: []\n  m_SendTarget: {fileID: 0}\n  m_EnableWetMix: 0\n  m_Bypass: 0\n"
                 % (ATTEN_ID[grp], h("atten:" + grp), h("attenmix:" + grp)))
    o.append("--- !u!244 &%d\nAudioMixerEffectController:\n  m_ObjectHideFlags: 3\n  m_CorrespondingSourceObject: {fileID: 0}\n"
             "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: \n  m_EffectID: %s\n  m_EffectName: Lowpass Simple\n"
             "  m_MixLevel: %s\n  m_Parameters:\n  - m_ParameterName: Cutoff freq\n    m_GUID: %s\n  m_SendTarget: {fileID: 0}\n"
             "  m_EnableWetMix: 0\n  m_Bypass: 0\n" % (LOWPASS_ID, h("lp"), h("lpmix"), h("lp:cutoff")))
    for snap, cutoff, music_db in (("Outdoor", 22000, -20), ("Indoor", 12000, -23)):
        vals = "".join("    %s: %g\n" % (h("vol:" + grp), (music_db if grp == "Music" else GROUP_DB[grp])) for grp in GROUPS)
        vals += "    %s: %g\n" % (h("lp:cutoff"), cutoff)
        o.append("--- !u!245 &%d\nAudioMixerSnapshotController:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                 "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: %s\n  m_AudioMixer: {fileID: 24100000}\n"
                 "  m_SnapshotID: %s\n  m_FloatValues:\n%s  m_TransitionOverrides: {}\n" % (SNAP_ID[snap], snap, h("snap:" + snap), vals))
    write(MIXER_REL, "".join(o))
    return g


# --------------------------------------------------------------------------------------------------------------------
# Prefabs and scene
# --------------------------------------------------------------------------------------------------------------------
def go_block(gid, tid, comps, name):
    return ("--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n%s  m_Layer: 0\n  m_Name: %s\n  m_TagString: Untagged\n"
            "  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"
            % (gid, "".join("  - component: {fileID: %d}\n" % c for c in [tid] + comps), name))


def transform_block(tid, gid):
    return ("--- !u!4 &%d\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
            "  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n"
            "  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n" % (tid, gid))


def mb_block(cid, gid, script_guid, fields=""):
    return ("--- !u!114 &%d\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n%s" % (cid, gid, script_guid, fields))


def single_object_prefab(name, components, seed):
    """components: list of (script_guid, fields). IDs derived from the prefab name so they are stable."""
    base = int(hashlib.md5(("p3prefab:" + seed).encode()).hexdigest()[:12], 16)
    gid, tid = base, base + 1
    cids = [base + 10 + i for i in range(len(components))]
    out = HEADER + go_block(gid, tid, cids, name) + transform_block(tid, gid)
    for cid, (sg, fields) in zip(cids, components):
        out += mb_block(cid, gid, sg, fields)
    return out


def ref_list(field, refs):
    if not refs:
        return "  %s: []\n" % field
    return "  %s:\n%s" % (field, "".join("  - %s\n" % r for r in refs))


def main():
    A = ASSETS_REL
    # 1) metas for folders, scripts, asmdefs that P3 added
    for d in ["Scripts/Audio", "Scripts/Haptics", "Scripts/UI", "Scripts/UI/Shell", "Scripts/UI/Sandbox", "Tests/PlayMode",
              "Data/Haptics", "Data/AudioCues", "Audio", "Audio/Mixer", "Audio/Clips", "Audio/Clips/Sfx", "Audio/Clips/Ambience",
              "Audio/Clips/Music", "Audio/Clips/Ui", "Prefabs/UI", "Prefabs/UI/Resources", "Prefabs/UI/Resources/P3"]:
        os.makedirs(L(A + "/" + d), exist_ok=True)
        meta(A + "/" + d, FOLDER)
    for d in ["Scripts/Audio", "Scripts/Haptics", "Scripts/UI", "Scripts/UI/Shell", "Scripts/UI/Sandbox", "Tests/PlayMode"]:
        for fn in sorted(os.listdir(L(A + "/" + d))):
            rel = A + "/" + d + "/" + fn
            if fn.endswith(".cs"):
                meta(rel, SCRIPT)
            elif fn.endswith(".asmdef"):
                meta(rel, ASMDEF)
    S = lambda rel: read_guid(A + "/Scripts/" + rel)

    # 2) audio clips + mixer
    clip_guids = build_audio()
    mixer_guid = build_mixer()

    # 3) haptic presets
    preset_refs = []
    for (name, amp, dur, ramp, pulses, gap, col, rin, ramp_amp) in PRESETS:
        rel = A + "/Data/Haptics/Haptic_%s.asset" % name
        fields = ("  Name: %s\n  Amplitude: %g\n  Duration: %g\n  IsRamp: %d\n  Pulses: %d\n  Gap: %g\n  DebugColor: %s\n"
                  "  RampInput: {x: %g, y: %g}\n  RampAmplitude: {x: %g, y: %g}\n"
                  % (name, amp, dur, 1 if ramp else 0, pulses, gap, color(col), rin[0], rin[1], ramp_amp[0], ramp_amp[1]))
        write(rel, so_asset(S("Haptics/HapticPreset.cs"), "Haptic_" + name, fields))
        preset_refs.append(ref(11400000, meta(rel, NATIVE.format(main=11400000))))

    # 4) cue sets
    cue_refs = []
    for (cue, mn, mx, bus, spatial, occ, loop, pitch, vol) in CUES:
        rel = A + "/Data/AudioCues/Cue_%s.asset" % cue
        variants = [ref(8300000, gd, 3) for gd in clip_guids[cue]]
        group_bus = "SFX" if bus not in GROUP_ID else bus
        fields = ("  CueId: %s\n%s  PitchRange: {x: %g, y: %g}\n  VolumeRange: {x: %g, y: %g}\n  MinDistance: %g\n  MaxDistance: %g\n"
                  "  Loop: %d\n  Group: %s\n  Spatial: %d\n  Bus: %d\n  Occludable: %d\n"
                  % (cue, ref_list("Variants", variants), pitch[0], pitch[1], vol[0], vol[1], mn, mx, 1 if loop else 0,
                     ref(GROUP_ID[group_bus], mixer_guid, 2), 1 if spatial else 0, BUS_INDEX[bus], 1 if occ else 0))
        write(rel, so_asset(S("Audio/AudioCueSet.cs"), "Cue_" + cue, fields))
        cue_refs.append(ref(11400000, meta(rel, NATIVE.format(main=11400000))))

    # 5) prefabs
    items = [ref(11400000, read_guid(A + "/Data/Items/Item_%s.asset" % n)) for n in ("Donut", "Cola", "Shake", "Cake", "Water")]
    config = ref(11400000, read_guid(A + "/Data/GameConfig.asset"))
    audio = single_object_prefab("Audio", [
        (S("Audio/AudioService.cs"), ref_list("_cues", cue_refs) + "  _mixer: %s\n  _poolSize: 12\n  _spread: 15\n" % ref(24100000, mixer_guid)),
        (S("Audio/AudioEventBinder.cs"), "  _audio: {fileID: 0}\n" + ref_list("_items", items) +
         "  _maxImpactSpeed: 4\n  _minImpactSpeed: 0.25\n  _waterPitchEmpty: 0.9\n  _waterPitchFull: 1.2\n  _crowdFullAt: 4\n"
         "  _fridgeHumOpenBoost: 0.6\n  _tiltRattleInterval: 0.25\n"),
        (S("Audio/AudioSceneInstaller.cs"), "  _birdRadius: 9\n  _birdHeight: 3\n  _fallbackFridge: {x: -0.5, y: 1.2, z: 1.5}\n  _fallbackPatio: {x: 0, y: 1.2, z: 14}\n"),
    ], "Audio")
    haptics = single_object_prefab("Haptics", [
        (S("Haptics/XRHapticsService.cs"), ref_list("_presets", preset_refs) + "  _left: {fileID: 0}\n  _right: {fileID: 0}\n  _rescanSeconds: 2\n"),
        (S("Haptics/HapticsEventBinder.cs"), "  _service: {fileID: 0}\n  _trayWarnDeg: 20\n"),
        (S("Haptics/HapticsDebugHud.cs"), "  _service: {fileID: 0}\n  _toggleKey: 102\n  _markerHeight: 0.12\n  _maxMarkerSize: 0.06\n"
         "  _minFlashSeconds: 0.15\n  _fallbackLeft: {fileID: 0}\n  _fallbackRight: {fileID: 0}\n"),
    ], "Haptics")
    shell = single_object_prefab("UIShell", [(S("UI/Shell/UiShell.cs"), "  _config: %s\n" % config)], "UIShell")
    prefab_guids = {}
    for rel, text in [(A + "/Prefabs/Systems/Audio.prefab", audio), (A + "/Prefabs/Systems/Haptics.prefab", haptics),
                      (A + "/Prefabs/UI/UIShell.prefab", shell), (A + "/Prefabs/UI/Resources/P3/Audio.prefab", audio),
                      (A + "/Prefabs/UI/Resources/P3/Haptics.prefab", haptics), (A + "/Prefabs/UI/Resources/P3/UIShell.prefab", shell)]:
        write(rel, text)
        prefab_guids[rel] = meta(rel, PREFAB)

    # 6) Sandbox_P3 scene: scene settings copied from Sandbox_P2 + one object with P3Sandbox and P3DebugPanel
    with open(L(A + "/Scenes/Sandbox_P2.unity"), encoding="utf-8") as f:
        p2 = f.read()
    settings = p2[:p2.index("--- !u!1 &", p2.index("NavMeshSettings"))]
    pg = lambda rel: ref(int(hashlib.md5(("p3prefab:" + os.path.basename(rel)[:-7]).encode()).hexdigest()[:12], 16), prefab_guids[rel], 3)
    sandbox_fields = ("  _audioPrefab: %s\n  _hapticsPrefab: %s\n  _uiPrefab: %s\n  _beat: 0.35\n  _turnSpeed: 90\n  _walkSpeed: 1.8\n"
                      "  _mouseLook: 0.15\n" % (pg(A + "/Prefabs/Systems/Audio.prefab"), pg(A + "/Prefabs/Systems/Haptics.prefab"),
                                               pg(A + "/Prefabs/UI/UIShell.prefab")))
    gid, tid, c1, c2 = 710000001, 710000002, 710000003, 710000004
    scene = settings + go_block(gid, tid, [c1, c2], "Sandbox_P3")
    scene += transform_block(tid, gid) + mb_block(c1, gid, S("UI/Sandbox/P3Sandbox.cs"), sandbox_fields)
    scene += mb_block(c2, gid, S("UI/Sandbox/P3DebugPanel.cs"), "  _visible: 1\n")
    scene += ("--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: %d}\n" % tid)
    write(A + "/Scenes/Sandbox_P3.unity", scene)
    meta(A + "/Scenes/Sandbox_P3.unity", SCENE)

    # 7) CREDITS.md meta (text asset inside Assets/)
    if os.path.exists(L(A + "/Audio/CREDITS.md")):
        meta(A + "/Audio/CREDITS.md", TEXT)
    total = sum(os.path.getsize(L(r)) for es in clip_paths().values() for r, _ in es)
    print("P3 assets generated. Audio on disk: %.1f MB (WAV; Unity stores Vorbis in the build)." % (total / 1e6))


if __name__ == "__main__":
    main()
