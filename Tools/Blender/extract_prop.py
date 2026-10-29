# Copies chosen objects out of a model file (FBX or GLB) into a clean FBX for Unity: transforms applied,
# standing on the origin (bottom centre). Materials keep their names; textures are copied beside the FBX
# separately. Run:
#   blender -b --factory-startup --python extract_prop.py -- <source> <out.fbx> <object> [<object> ...]
import bpy, sys
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out, names = args[0], args[1], args[2:]

bpy.ops.wm.read_factory_settings(use_empty=True)
if src.lower().endswith(".glb"): bpy.ops.import_scene.gltf(filepath=src)
else: bpy.ops.import_scene.fbx(filepath=src)
keep = [bpy.data.objects[n] for n in names]
for o in list(bpy.data.objects):
    if o not in keep: bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for o in keep:
    world = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = world
    o.select_set(True)
bpy.context.view_layer.objects.active = keep[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

pts = [v.co for o in keep for v in o.data.vertices]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
shift = Matrix.Translation(Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z)))
for o in keep: o.data.transform(shift)
print("size", tuple(round(c, 3) for c in hi - lo))
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="STRIP",
                         apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
