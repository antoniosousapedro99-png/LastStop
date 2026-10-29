# The hiding locker from the "Dirty Locker" Sketchfab model (the closed one of its two lockers), made ready
# for HidingLocker:
#  - LockerBody: the cabinet and its three hinges.
#  - LockerDoor: the door and its vent slats, with its origin on the hinge line so it can swing. Behind the
#    upper vent slats a small window is cut through the door, so the player hiding inside can peek out
#    between the slats (the original door is solid there).
# Scaled to 2 m tall (the vent window then sits at the player's eye height, 1.65 m), standing on the origin. Run:
#   blender -b --factory-startup --python locker_door.py -- <source.fbx> <out.fbx>
import bpy, sys, bmesh
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
HEIGHT = 2.0

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
keep = bpy.data.objects["Locker.003"]          # the closed locker
for o in list(bpy.data.objects):
    if o != keep: bpy.data.objects.remove(o, do_unlink=True)
keep.parent = None
bpy.context.view_layer.objects.active = keep
keep.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.separate(type="LOOSE"); bpy.ops.object.mode_set(mode="OBJECT")

def box(o):
    pts = [v.co for v in o.data.vertices]
    return (Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
            Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))))

parts = [o for o in bpy.data.objects if o.type == "MESH"]
body = max(parts, key=lambda o: (box(o)[1] - box(o)[0]).length if len(o.data.polygons) < 100 else 0)
door = max(parts, key=lambda o: len(o.data.polygons))
slats = [o for o in parts if len(o.data.polygons) == 8]
hinges = [o for o in parts if o not in slats and o not in (body, door)]
print("body", body.name, "door", door.name, "slats", len(slats), "hinges", len(hinges))

# The window behind the upper slats: the slats' outline, slightly inset.
top = [s for s in slats if box(s)[0].z > 0.9]
lo = Vector((min(box(s)[0].x for s in top), 0, min(box(s)[0].z for s in top)))
hi = Vector((max(box(s)[1].x for s in top), 0, max(box(s)[1].z for s in top)))
x0, x1, z0, z1 = lo.x + 0.01, hi.x - 0.01, lo.z + 0.004, hi.z - 0.004
bm = bmesh.new(); bm.from_mesh(door.data)
for co, no in [((0, 0, z0), (0, 0, 1)), ((0, 0, z1), (0, 0, 1)), ((x0, 0, 0), (1, 0, 0)), ((x1, 0, 0), (1, 0, 0))]:
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no)
cut = [f for f in bm.faces if x0 < f.calc_center_median().x < x1 and z0 < f.calc_center_median().z < z1 and abs(f.normal.y) > 0.5]
print("window faces removed", len(cut))
bmesh.ops.delete(bm, geom=cut, context="FACES")
bm.to_mesh(door.data); bm.free()

def join(target, others, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in [target] + others: o.select_set(True)
    bpy.context.view_layer.objects.active = target
    if others: bpy.ops.object.join()
    target.name = name; target.data.name = name
    return target

hinge_side = sum((box(h)[0].x + box(h)[1].x) / 2 for h in hinges) / len(hinges)
body = join(body, hinges, "LockerBody")
door = join(door, slats, "LockerDoor")

# Stand the locker on the origin (bottom centre), scale it, and put the door's origin on its hinge line
# (the side the hinges are on).
blo, bhi = box(body)
shift = Vector((-(blo.x + bhi.x) / 2, -(blo.y + bhi.y) / 2, -blo.z))
k = HEIGHT / (bhi.z - blo.z)
for o in (body, door):
    o.data.transform(Matrix.Translation(shift))
    o.data.transform(Matrix.Scale(k, 4))
dlo, dhi = box(door)
# No shelf: the cabinet has one at chest height, where the hiding player stands. Its vertices are pushed back
# flat against the back wall (the strip it leaves in the wall becomes part of the back).
bl, bh = box(body)
inner = [v for v in body.data.vertices if abs(v.co.x) < bh.x - 0.025 and bl.y + 0.02 < v.co.y and 0.6 * HEIGHT < v.co.z < 0.9 * HEIGHT]
back_y = max(v.co.y for v in inner)
for v in inner: v.co.y = back_y
print("shelf vertices flattened", len(inner))
hinge_x = dhi.x if hinge_side > (blo.x + bhi.x) / 2 else dlo.x   # the door edge on the hinges' side
pivot = Vector((hinge_x, (dlo.y + dhi.y) / 2, 0))
door.data.transform(Matrix.Translation(-pivot))
door.location = pivot
print("door pivot", tuple(round(c, 3) for c in pivot), "body size", tuple(round(c, 3) for c in (box(body)[1] - box(body)[0])))

for o in (body, door):
    for m in o.data.materials: m.name = "Locker"
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="STRIP",
                         apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
