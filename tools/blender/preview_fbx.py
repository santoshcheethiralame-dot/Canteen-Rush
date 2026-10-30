"""Render exported FBX modules (flat shaded, material colours) to check decimation quality.
Run: blender -b --factory-startup --python preview_fbx.py -- OUT_DIR a.fbx b.fbx ...
Each FBX gets its own PNG (<name>_fbx.png). Imported materials show base colours only.
"""
import bpy, sys, os
from mathutils import Vector

a = sys.argv[sys.argv.index("--") + 1:]
out, files = a[0], a[1:]
os.makedirs(out, exist_ok=True)
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f)
    sc = bpy.context.scene
    meshes = [o for o in sc.objects if o.type == 'MESH']
    ws = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    mn = Vector(map(min, *ws)); mx = Vector(map(max, *ws)); c = (mn + mx) / 2; r = max((mx - mn).length, 1e-3)
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
    sc.render.resolution_x, sc.render.resolution_y = 640, 480
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.6, 0.65, 0.75)
    cam = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    cam.clip_start = 0.001
    co.location = c + Vector((r * 0.9, -r * 0.9, r * 0.6))
    co.rotation_euler = (c - co.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(out, os.path.splitext(os.path.basename(f))[0] + "_fbx.png")
    bpy.ops.render.render(write_still=True)
    tris = sum(len(o.data.polygons) for o in meshes)
    print("PREVIEW", os.path.basename(f), "polys", tris, "bounds", [round(v, 3) for v in (mx - mn)])
