"""Flat-shaded (Workbench) preview renders of one .blend: iso, iso2, top.
Run: blender -b --disable-autoexec FILE.blend --python render_preview.py -- OUT_DIR TAG [hide_big_metres]
hide_big_metres: objects with longest bounding extent above this are hidden from the render
(use ~9 for canteen.blend so its 13 m placeholder floor/wall do not swallow the frame).
Workbench shows material colours only, never image textures or baked lighting.
"""
import bpy, sys, os
from mathutils import Vector

a = sys.argv[sys.argv.index("--") + 1:]
out, tag = a[0], a[1]
hide_big = float(a[2]) if len(a) > 2 else None
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o)

rows = []
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector(map(min, *ws)); mx = Vector(map(max, *ws))
    if hide_big and (mx - mn).length > hide_big:
        o.hide_render = True
        continue
    rows.append((mn, mx))
mn = Vector(map(min, *[r[0] for r in rows])); mx = Vector(map(max, *[r[1] for r in rows]))
ctr = (mn + mx) / 2; size = mx - mn; R = max(size) * 0.9 + 1e-3

sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'MATERIAL'
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.world = sc.world or bpy.data.worlds.new("w")
sc.world.color = (0.55, 0.65, 0.78)

def shot(name, loc, ortho=False, scale=None):
    cam = bpy.data.cameras.new(name); co = bpy.data.objects.new(name, cam)
    sc.collection.objects.link(co); sc.camera = co
    cam.clip_end = 5000; cam.clip_start = 0.01
    co.location = loc
    co.rotation_euler = (ctr - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    if ortho:
        cam.type = 'ORTHO'; cam.ortho_scale = scale
    sc.render.filepath = os.path.join(out, "%s_%s.png" % (tag, name))
    bpy.ops.render.render(write_still=True)

shot("iso", ctr + Vector((R * 1.1, -R * 1.1, R * 0.8)))
shot("iso2", ctr + Vector((-R * 1.1, R * 1.0, R * 0.7)))
shot("top", ctr + Vector((0.01, 0, R * 2.5)), ortho=True, scale=max(size.x, size.y) * 1.1)
