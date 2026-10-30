"""Regenerate docs/PIPELINE_STATUS.md from Assets/Art/build_report.json (written by tools/blender/build_world.py)."""
import json

REPORT = "unity/CanteenRush/Assets/Art/build_report.json"
OUT = "docs/PIPELINE_STATUS.md"

mods = json.load(open(REPORT))
lines = [
    "# Pipeline status (generated from `Assets/Art/build_report.json` by `tools/gen_pipeline_status.py`)",
    "",
    "Run `blender -b --factory-startup --disable-autoexec --python tools/blender/build_world.py -- --source source --out unity/CanteenRush/Assets/Art` to regenerate. Source tris are evaluated (modifiers applied).",
    "",
    "| FBX | node | source tris | built tris | budget | dims (m) | node pivot in source frame |",
    "|---|---|---|---|---|---|---|",
]
total_env = 0
for m in mods:
    for g in m["groups"]:
        d = g.get("dims_m", [])
        lines.append("| %s | %s | %s | %s | %s | %s | %s |" % (
            m["fbx"], g["group"], format(g.get("tris_source", 0), ","), format(g.get("tris", 0), ","),
            g.get("target_tris", ""), " x ".join("%.2f" % v for v in d), g.get("pivot_world", "")))
        if m["fbx"].startswith("Environment/"):
            total_env += g.get("tris", 0)
    if m.get("markers"):
        names = sorted({mk.rsplit("_", 1)[0] if mk[-1].isdigit() else mk for mk in (x if isinstance(x, str) else x.get("name", "") for x in m["markers"])})
        lines.append("| %s | markers | | | | | %d empties: %s |" % (m["fbx"], len(m["markers"]), ", ".join(names)))
lines += ["", "Built environment triangles (canteen + patio + three backdrops, before instancing chairs/tables/plants): %s." % format(total_env, ","),
          "", "Not built (cut, see `docs/DECISIONS.md`): `Env_Washroom`."]
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
print("wrote", OUT, "modules", len(mods))
