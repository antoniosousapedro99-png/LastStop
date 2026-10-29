# Makes a heavy scanned model (OBJ, FBX or GLB) light enough for a PS1-style game: every mesh is simplified
# (Decimate, collapse) down to about <triangles> in total, transforms applied, stood on the origin (bottom
# centre), and exported as FBX. The texture is shrunk separately (ffmpeg). Run:
#   blender -b --factory-startup --python decimate_prop.py -- <source> <out.fbx> <triangles>
import bpy, sys
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out, target = args[0], args[1], int(args[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
if src.lower().endswith(".obj"): bpy.ops.wm.obj_import(filepath=src)
elif src.lower().endswith(".glb"): bpy.ops.import_scene.gltf(filepath=src)
else: bpy.ops.import_scene.fbx(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
total = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
for o in meshes:
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    mod = o.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = min(1.0, target / total)
    bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
pts = [v.co for o in meshes for v in o.data.vertices]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
for o in meshes: o.data.transform(Matrix.Translation(Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))))
print("triangles", total, "->", sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons), "size", tuple(round(c, 3) for c in hi - lo))
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="STRIP",
                         apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
