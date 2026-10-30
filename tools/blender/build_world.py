"""Build clean, correctly scaled FBX modules from the supplied (untrusted) .blend files.

Run (always with --disable-autoexec; the sources are third-party data):
  blender -b --factory-startup --disable-autoexec --python tools/blender/build_world.py -- \
      --source source --out unity/CanteenRush/Assets/Art [--config tools/blender/config.json] [--only Item_Donut ...]

A module = one FBX = one or more GROUPS (each becomes one mesh node) + optional MARKER empties.
Per group: keep the matched objects, apply modifiers (Geometry Nodes included), join, scale, clean,
decimate to the triangle budget, set the pivot, check dimensions. Modules with "generate" build new
props procedurally instead of reading a source file. Everything is written to
  <out>/<category>/<module>.fbx   and   <out>/Materials/source/<module>.materials.json
plus <out>/build_report.json. Exit code 1 if any budget or dimension check fails. Safe to re-run.

Group keys: name, objects (regexes, fullmatch on object names), take (keep first N matches), offset ([x,y,z] source metres, moves the group),
  target_tris, pivot (keep|center|base|point), pivot_point [x,y,z] in SOURCE world metres (before module scale),
  place (zero: node at world origin | keep: node stays where the pivot is in the source frame),
  proxy ("bbox": replace the matched objects by one box of their bounds), height_m / max_extent_m ([lo,hi]).
Marker keys: name_prefix, objects (regexes), anchor (center|base), or name + point [x,y,z] (source world).
"""
import bpy, bmesh, json, math, os, re, sys, time
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))


def parse_args():
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    o = {"source": "source", "out": "unity/CanteenRush/Assets/Art", "config": os.path.join(HERE, "config.json"), "only": []}
    i = 0
    while i < len(a):
        if a[i] in ("--source", "--out", "--config"):
            o[a[i][2:]] = a[i + 1]; i += 2
        elif a[i] == "--only":
            i += 1
            while i < len(a) and not a[i].startswith("--"):
                o["only"].append(a[i]); i += 1
        else:
            raise SystemExit("unknown arg " + a[i])
    return o


# ---------- helpers ----------
def tri_count(obj):
    eo = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    me = eo.to_mesh(); me.calc_loop_triangles()
    n = len(me.loop_triangles); eo.to_mesh_clear()
    return n


def select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def world_bounds(obj):
    ws = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return Vector(map(min, *ws)), Vector(map(max, *ws))


def match(patterns, take=None):
    out, seen = [], set()
    for pat in patterns:
        for o in sorted((o for o in bpy.data.objects if re.fullmatch(pat, o.name)), key=lambda o: o.name):
            if o.name not in seen:
                seen.add(o.name); out.append(o)
    return out[:take] if take else out


def anchor_point(obj, anchor):
    mn, mx = world_bounds(obj)
    c = (mn + mx) / 2
    return Vector((c.x, c.y, mn.z)) if anchor == "base" else c


def new_material(name, rgba, rough=0.6, metal=0.0, alpha=None):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = rgba
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    if alpha is not None:
        p.inputs["Alpha"].default_value = alpha
    m.diffuse_color = rgba
    return m


def make_obj(name, builder, material):
    bm = bmesh.new(); builder(bm)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    me.materials.append(material)
    return o


def box(center, size):
    def b(bm):
        v = bmesh.ops.create_cube(bm, size=1.0)["verts"]
        bmesh.ops.scale(bm, vec=Vector(size), verts=v); bmesh.ops.translate(bm, vec=Vector(center), verts=v)
    return b


def cone(center, r_bottom, r_top, h, seg=12):
    def b(bm):
        v = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r_bottom, radius2=r_top, depth=h)["verts"]
        bmesh.ops.translate(bm, vec=Vector(center), verts=v)
    return b


def combine(*builders):
    def b(bm):
        for f in builders:
            f(bm)
    return b


# ---------- procedural props (new models; none exist in the source files) ----------
def gen_tray():
    m = new_material("M_TrayBody", (0.16, 0.22, 0.30, 1), 0.5)
    L, W, H, T = 0.45, 0.30, 0.012, 0.010
    parts = [box((0, 0, H / 2), (L, W, H)),
             box((0, W / 2 - T / 2, H + 0.009), (L, T, 0.018)), box((0, -W / 2 + T / 2, H + 0.009), (L, T, 0.018)),
             box((L / 2 - T / 2, 0, H + 0.009), (T, W - 2 * T, 0.018)), box((-L / 2 + T / 2, 0, H + 0.009), (T, W - 2 * T, 0.018))]
    make_obj("Prop_Tray", combine(*parts), m)


def gen_rack():
    m = new_material("M_RackMetal", (0.55, 0.57, 0.60, 1), 0.35, 0.6)
    parts = [box((0, 0, 0.01), (0.54, 0.38, 0.02)), box((0, 0, 0.21), (0.54, 0.38, 0.02)), box((0, 0.18, 0.11), (0.54, 0.02, 0.22))]
    for sx in (-0.26, 0.26):
        parts.append(box((sx, -0.17, 0.11), (0.02, 0.02, 0.22)))
    make_obj("Prop_TrayRack", combine(*parts), m)


def gen_cup():
    make_obj("Item_Cup", cone((0, 0, 0.05), 0.030, 0.040, 0.10, 12), new_material("M_CupPaper", (0.93, 0.93, 0.90, 1), 0.8))
    make_obj("Cup_Water", cone((0, 0, 0.078), 0.0355, 0.0365, 0.006, 12), new_material("M_Water", (0.20, 0.50, 0.90, 1), 0.1, 0.0, 0.7))


def gen_dispenser():
    m = new_material("M_DispenserBody", (0.85, 0.85, 0.88, 1), 0.5)
    parts = [cone((0, 0, 0.01), 0.07, 0.07, 0.02, 16), cone((0, 0, 0.16), 0.05, 0.05, 0.28, 16), cone((0, 0, 0.31), 0.052, 0.040, 0.03, 16)]
    make_obj("Prop_CupDispenser", combine(*parts), m)


GENERATORS = {"tray": gen_tray, "rack": gen_rack, "cup": gen_cup, "dispenser": gen_dispenser}


# ---------- group processing ----------
def process_group(g, scale):
    objs = match(g["objects"], g.get("take"))
    if not objs:
        raise RuntimeError("group %s: nothing matched %s" % (g["name"], g["objects"]))
    res = {"group": g["name"], "errors": [], "source_objects": len(objs)}
    for o in objs:
        o.hide_set(False); o.hide_viewport = False; o.hide_render = False
    if g.get("proxy") == "bbox":
        mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
        for o in objs:
            a, b = world_bounds(o); mn = Vector(map(min, mn, a)); mx = Vector(map(max, mx, b))
        mat = objs[0].data.materials[0] if objs[0].data.materials else new_material("M_Proxy", (0.8, 0.8, 0.8, 1))
        res["tris_source"] = sum(tri_count(o) for o in objs)
        o = make_obj(g["name"], box((mn + mx) / 2, mx - mn), mat)
        for old in objs:
            bpy.data.objects.remove(old)
        objs = [o]
    select_only(objs)
    bpy.ops.object.convert(target='MESH')
    meshes = [o for o in objs if o.type == 'MESH']
    select_only(meshes)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = g["name"]; obj.data.name = g["name"]
    res.setdefault("tris_source", tri_count(obj))

    # bake world transform into the mesh, then scale about the world origin
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if scale != 1.0:
        obj.data.transform(Matrix.Scale(scale, 4))
    if g.get("offset"):
        obj.data.transform(Matrix.Translation(Vector(g["offset"]) * scale))

    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_edges], context='VERTS')
    bm.to_mesh(obj.data); bm.free()

    target = int(g["target_tris"])
    cur = tri_count(obj)
    if cur > target:
        mod = obj.modifiers.new("Decimate", 'DECIMATE')
        mod.decimate_type = 'COLLAPSE'; mod.ratio = max(target / cur, 0.001)
        select_only([obj]); bpy.ops.object.modifier_apply(modifier=mod.name)
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(g.get("smooth_angle", 35)))
    except Exception:
        bpy.ops.object.shade_smooth()

    # pivot: shift mesh so the node origin sits at the pivot; place at world origin or keep at the pivot
    mn, mx = world_bounds(obj); c = (mn + mx) / 2
    mode = g.get("pivot", "keep")
    if mode == "center":
        pivot = c
    elif mode == "base":
        pivot = Vector((c.x, c.y, mn.z))
    elif mode == "point":
        pivot = (Vector(g["pivot_point"]) + Vector(g.get("offset", (0, 0, 0)))) * scale
    else:
        pivot = Vector((0, 0, 0))
    obj.data.transform(Matrix.Translation(-pivot))
    obj.location = pivot if g.get("place", "keep") == "keep" else Vector((0, 0, 0))
    bpy.context.view_layer.update()

    tris = tri_count(obj); mn, mx = world_bounds(obj); dim = mx - mn
    res.update(obj=obj, tris=tris, target_tris=target, dims_m=[round(v, 3) for v in dim],
               pivot_world=[round(v, 3) for v in obj.location])
    if tris > target * 1.1:
        res["errors"].append("tris %d exceed budget %d" % (tris, target))
    for key, val in (("height_m", dim.z), ("max_extent_m", max(dim))):
        if g.get(key) and not (g[key][0] <= val <= g[key][1]):
            res["errors"].append("%s %.3f outside %s (check scale)" % (key, val, g[key]))
    return res


def material_json(objs):
    mats, seen = [], set()
    for obj in objs:
        for m in obj.data.materials:
            if not m or m.name in seen:
                continue
            seen.add(m.name)
            d = {"name": m.name, "base_color": list(m.diffuse_color), "metallic": 0.0, "roughness": 0.5,
                 "emission": [0, 0, 0, 1], "emission_strength": 0.0, "alpha": 1.0, "textures": {}}
            if m.node_tree:
                for n in m.node_tree.nodes:
                    if n.type != 'BSDF_PRINCIPLED':
                        continue
                    for k, key in (("Base Color", "base_color"), ("Emission Color", "emission")):
                        i = n.inputs.get(k)
                        if i is not None:
                            d[key] = list(i.default_value)
                            if i.is_linked and i.links[0].from_node.type == 'TEX_IMAGE' and i.links[0].from_node.image:
                                d["textures"][key] = i.links[0].from_node.image.name
                    for k, key in (("Metallic", "metallic"), ("Roughness", "roughness"), ("Alpha", "alpha"), ("Emission Strength", "emission_strength")):
                        i = n.inputs.get(k)
                        if i is not None:
                            d[key] = round(float(i.default_value), 4)
                    break
            mats.append(d)
    return mats


def build(name, cfg, source_dir, out_dir):
    t0 = time.time()
    scale = float(cfg.get("scale", 1.0))
    out = {"module": name, "errors": [], "groups": []}
    markers = []
    if "generate" in cfg:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        GENERATORS[cfg["generate"]]()
    else:
        bpy.ops.wm.open_mainfile(filepath=os.path.join(source_dir, cfg["file"]))
        # record marker anchors from the source before anything is deleted
        for mk in cfg.get("markers", []):
            if "point" in mk:
                markers.append((mk["name"], Vector(mk["point"]) * scale))
            else:
                for i, o in enumerate(match(mk["objects"], mk.get("take"))):
                    markers.append(("%s_%02d" % (mk["name_prefix"], i), anchor_point(o, mk.get("anchor", "center")) * scale))
        keep = []
        for g in cfg["groups"]:
            keep += match(g["objects"], g.get("take"))
        keepset = set(keep)
        for o in keep:
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
        for o in [o for o in bpy.data.objects if o not in keepset]:
            bpy.data.objects.remove(o)
    groups = cfg["groups"]
    nodes = []
    for g in groups:
        r = process_group(g, scale if "generate" not in cfg else 1.0)
        nodes.append(r.pop("obj")); out["groups"].append(r)
        out["errors"] += ["%s: %s" % (g["name"], e) for e in r["errors"]]
    empties = []
    for mname, pos in markers:
        e = bpy.data.objects.new(mname, None); e.empty_display_size = 0.05
        bpy.context.scene.collection.objects.link(e); e.location = pos; empties.append(e)
    out["markers"] = [m for m, _ in markers]

    mdir = os.path.join(out_dir, "Materials", "source"); os.makedirs(mdir, exist_ok=True)
    with open(os.path.join(mdir, name + ".materials.json"), "w") as f:
        json.dump(material_json(nodes), f, indent=1)
    cdir = os.path.join(out_dir, cfg.get("category", "Props")); os.makedirs(cdir, exist_ok=True)
    select_only(nodes + empties)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(cdir, name + ".fbx"), use_selection=True, object_types={'MESH', 'EMPTY'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', global_scale=1.0,
        apply_unit_scale=True, bake_space_transform=True, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, path_mode='STRIP', embed_textures=False)
    out["fbx"] = "%s/%s.fbx" % (cfg.get("category", "Props"), name)
    out["seconds"] = round(time.time() - t0, 1)
    return out


def main():
    a = parse_args()
    cfg = json.load(open(a["config"]))
    mods = {k: v for k, v in cfg["modules"].items() if not k.startswith("_") and (not a["only"] or k in a["only"])}
    missing = set(a["only"]) - set(mods)
    if missing:
        raise SystemExit("unknown modules: %s" % sorted(missing))
    results = []
    for name, mcfg in mods.items():
        try:
            results.append(build(name, mcfg, a["source"], a["out"]))
        except Exception as e:
            import traceback; traceback.print_exc()
            results.append({"module": name, "errors": ["exception: %r" % e], "groups": []})
    print("\nMODULE SUMMARY")
    bad = 0
    for r in results:
        bad += bool(r["errors"])
        print("%-4s %s (%d markers)" % ("FAIL" if r["errors"] else "ok", r["module"], len(r.get("markers", []))))
        for g in r["groups"]:
            print("       %-18s src %8s -> %6s tris (budget %5s) dims %s pivot %s" % (
                g["group"], g.get("tris_source", "-"), g.get("tris", "-"), g.get("target_tris", "-"), g.get("dims_m"), g.get("pivot_world")))
        for e in r["errors"]:
            print("       ERROR", e)
    os.makedirs(a["out"], exist_ok=True)
    with open(os.path.join(a["out"], "build_report.json"), "w") as f:
        json.dump(results, f, indent=1)
    sys.exit(1 if bad else 0)


main()
