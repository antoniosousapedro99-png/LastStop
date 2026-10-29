# One side of the Soviet metro pack's A-frame stepladder (SM_Ladder), to lean against a wall: the faces on
# the other side of the frame's middle (along X, where its two sides splay apart) are deleted; the half kept
# leans in toward +X at the top. Stood on
# the origin (bottom centre) and exported as FBX. Run:
#   blender -b --factory-startup --python ladder_half.py -- <FrozenCortexSketchfab.fbx> <out.fbx>
import bpy, sys, bmesh
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
lad = bpy.data.objects["SM_Ladder"]
for o in list(bpy.data.objects):
    if o != lad: bpy.data.objects.remove(o, do_unlink=True)
world = lad.matrix_world.copy(); lad.parent = None; lad.matrix_world = world
bpy.context.view_layer.objects.active = lad; lad.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bm = bmesh.new(); bm.from_mesh(lad.data)
xs = [v.co.x for v in bm.verts]
mid = (min(xs) + max(xs)) / 2
gone = [f for f in bm.faces if f.calc_center_median().x > mid]
bmesh.ops.delete(bm, geom=gone, context="FACES")
loose = [v for v in bm.verts if not v.link_faces]
bmesh.ops.delete(bm, geom=loose, context="VERTS")
bm.to_mesh(lad.data); bm.free()

pts = [v.co for v in lad.data.vertices]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
lad.data.transform(Matrix.Translation(Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))))
lad.name = lad.data.name = "Ladder"
print("faces removed", len(gone), "size", tuple(round(c, 3) for c in hi - lo))
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="STRIP",
                         apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
