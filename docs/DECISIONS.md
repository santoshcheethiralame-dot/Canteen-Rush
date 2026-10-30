# Decisions log

Append-only. Newest at the bottom. Format: `## YYYY-MM-DD — title`, then decision, reason, who agreed.

## 2026-10-30 — Initial decisions D1–D10
See `docs/00_MASTER_SPEC.md` §2. Summary: VR (Quest-class budget) shown via XR Device Simulator; Unity 6000.0.43f1 + URP + XRI 3.x; concept "Canteen Rush"; world = canteen + Part 3 patio, other parts backdrop; menu = Donut/Cola/Shake/Cake/Water (the source "Dough" objects are donuts); continuous move + snap turn; primitive customers; single scene with single-writer ownership; washroom out of gameplay; tunables in ScriptableObjects.

## 2026-10-30 — Corrections found during recon
- Source `Dough` meshes are donuts, not pizzas (close-up render `docs/recon/previews/canteen_prop_Dough.png`).
- Part 3 has 24 chairs + 4 tables (not 28 chairs).
- The canteen `cup` object has zero triangles; a cup must be modelled.

## Open (needs the team lead)
1. Viva / submission date. 2. Does anyone own a Quest headset? 3. GPU of the viva laptop. 4. Other teams' concepts. 5. Android build module installed?
