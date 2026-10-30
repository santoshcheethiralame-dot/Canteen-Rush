"""Render single named objects close-up (flat shaded).
Run: blender -b --disable-autoexec FILE.blend --python render_isolated.py -- OUT_DIR TAG name1 name2 ...
"""
import bpy, sys, os
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]
out, tag, names = a[0], a[1], a[2:]
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o)
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
sc.render.resolution_x, sc.render.resolution_y = 640, 480
sc.world = sc.world or bpy.data.worlds.new("w"); sc.world.color = (0.6, 0.65, 0.75)
for n in names:
    for o in bpy.data.objects:
        o.hide_render = (o.name != n)
    o = bpy.data.objects.get(n)
    if o is None:
        continue
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector(map(min, *ws)); mx = Vector(map(max, *ws)); c = (mn + mx) / 2; r = max((mx - mn).length, 1e-3)
    cam = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    cam.clip_start = 0.001; cam.clip_end = 1000
    co.location = c + Vector((r * 0.9, -r * 0.9, r * 0.6))
    co.rotation_euler = (c - co.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(out, "%s_%s.png" % (tag, n.replace(' ', '_')))
    bpy.ops.render.render(write_still=True)
