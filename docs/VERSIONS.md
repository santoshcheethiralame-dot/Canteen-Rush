# Versions (pinned at M0, 2026-10-30)

| Component | Version | Note |
|---|---|---|
| Unity Editor | 6000.0.43f1 | installed at `C:\Program Files\Unity\Hub\Editor\6000.0.43f1`; Windows standalone module only (no Android module) |
| Project template | Universal 3D (`com.unity.template.3d-cross-platform` 17.0.11) | trimmed: tutorial assets, sample scene removed |
| URP | 17.0.1 | `Assets/Settings/Mobile_RPAsset` = Quest-class (Android/iOS quality level), `PC_RPAsset` = standalone |
| XR Interaction Toolkit | 3.1.2 | pinned; registry latest is 3.7.0 — do not bump without a PR |
| XR Plugin Management | 4.5.1 | |
| OpenXR Plugin | 1.14.0 | |
| Input System | 1.8.1 | |
| AI Navigation | 2.0.0 | |
| Test Framework | 1.4.2 | |
| uGUI (incl. TextMeshPro) | 2.0.0 | |
| Blender (pipeline) | 5.2.0 LTS | source files written by 3.3.6 |

Status: manifest written by hand against the Unity package registry (versions confirmed to exist and to list compatible dependencies). **Not yet opened in the editor** — the editor on the author's machine has no activated license (see `docs/DECISIONS.md` 2026-10-30). First open will generate `packages-lock.json`; commit it.
