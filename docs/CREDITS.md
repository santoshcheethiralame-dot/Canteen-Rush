# Credits: third-party, generated and AI-assisted assets

Per MASTER_SPEC §15. Append a row whenever something not written from scratch by the team enters the repo.

## Source material
| What | Source | License / terms | Used for |
|---|---|---|---|
| "Outdoor" floor Blender and texture files (`Outdoor-20261009T052754Z-1-001.zip`) | Course drive (PES University AR/VR mini-project) | Course-provided for this assignment | Canteen, patio and backdrop geometry (via P1's pipeline) |

## Packages (Unity Package Manager, see `docs/VERSIONS.md`)
| Package | Use | License |
|---|---|---|
| XR Interaction Toolkit 3.1.2 + Starter Assets and XR Device Simulator samples | Rig, interaction, simulator, haptic impulse player | Unity Companion License |
| URP 17.0.1, Input System 1.8.1, OpenXR 1.14.0, AI Navigation 2.0.0, uGUI 2.0.0, Test Framework 1.4.2 | Engine features | Unity Companion License |

## Audio (P3)
All 35 clips are procedurally synthesised by `tools/p3/gen_p3_assets.py`, team-authored and CC0. Per-file table: `unity/CanteenRush/Assets/Audio/CREDITS.md`. No third-party or AI-generated audio.

## Fonts
| Font | Source | Use |
|---|---|---|
| Unity built-in `LegacyRuntime.ttf` | Ships with the Unity Editor | All world-space UI text (order UI and P3's shell): one font family |

## AI assistance (course permits it; understanding is tested in the viva)
| Person | Tool | What it helped with | Where the human record is |
|---|---|---|---|
| P1 | AI coding assistant | Blender pipeline, scene builder, lighting tooling | `docs/LOG_P1.md` |
| P2 | AI coding assistant | Core, gameplay, order UI, EditMode tests | `docs/LOG_P2.md` |
| P3 | Claude (Anthropic) via Claude Code | Audio/haptics/UI-shell code, PlayMode tests, audio synthesis script, QA checklist, viva pack | `docs/LOG_P3.md` |

No AI-generated images, audio or 3D models are in the repository.
