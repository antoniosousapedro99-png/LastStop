# Two pieces from the Metro PSX subway car:
#  - SubwayCarOpen.fbx: the whole car with both end doorways widened (frame and windows slide outward a
#    little, so the original look stays and the player fits through: 0.9 m clear in game, where the
#    builders scale the car to 18.7 m long, which also makes the doorway 1.95 m tall. The player is 0.7 m
#    wide and 1.8 m tall plus 8 cm collision padding on every side).
#  - SubwayCarEnd.fbx: just one end wall (doorway and two windows), used as the driver's cab partition.
import bpy, sys, bmesh
args = sys.argv[sys.argv.index("--")+1:]
src, out_car, out_end = args[0], args[1], args[2]

def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=src)
    car = bpy.data.objects["subway_car"]
    for o in list(bpy.data.objects):
        if o != car: bpy.data.objects.remove(o, do_unlink=True)
    car.parent = None
    return car

# Piecewise-linear map of |y| (across the car's width): door frame 0.24 -> 0.327, window frames shift a
# little, the car's sides stay put.
pts = [(0.0, 0.0), (0.24, 0.327), (0.41, 0.44), (0.86, 0.875), (1.03, 1.03), (9.0, 9.0)]
def widen(a):
    s = 1 if a >= 0 else -1; a = abs(a)
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        if a <= x1: return s * (y0 + (a - x0) / (x1 - x0) * (y1 - y0))
    return s * a

def export(path):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, object_types={'MESH'}, path_mode='STRIP',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True)

car = load()
bm = bmesh.new(); bm.from_mesh(car.data)
end = max(abs(v.co.x) for v in bm.verts)
moved = 0
for v in bm.verts:
    if abs(v.co.x) > end - 0.4:
        v.co.y = widen(v.co.y); moved += 1
bm.to_mesh(car.data); bm.free()
print("widened verts", moved)
export(out_car)

# The end wall alone (the +x end, widened the same way).
car = load()
bm = bmesh.new(); bm.from_mesh(car.data)
end = max(v.co.x for v in bm.verts)
for v in bm.verts:
    if v.co.x > end - 0.4: v.co.y = widen(v.co.y)
# Keep the wall itself: faces at the very end, above the floor and below the roof edge.
keep = lambda c: c.x > end - 0.16 and -0.80 < c.z < 0.97
bmesh.ops.delete(bm, geom=[f for f in bm.faces if not keep(f.calc_center_median())], context='FACES')
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
print("end faces", len(bm.faces))
bm.to_mesh(car.data); bm.free()
export(out_end)
print("EXPORTED")
