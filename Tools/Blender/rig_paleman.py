# Gives the unrigged Pale Man mesh the Mixamo skeleton of another character from the same pack
# (Character_Killer), stretched to his body, with automatic skin weights, and exports one FBX.
import bpy, sys, mathutils
args = sys.argv[sys.argv.index("--") + 1:]
monster_path, donor_path, out_path = args
bpy.ops.wm.read_factory_settings(use_empty=True)

bpy.ops.import_scene.fbx(filepath=donor_path)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
for o in list(bpy.data.objects):
    if o.type == "MESH": bpy.data.objects.remove(o, do_unlink=True)
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
arm.animation_data_clear()

bpy.ops.import_scene.fbx(filepath=monster_path)
mesh = next(o for o in bpy.data.objects if o.type == "MESH")

def apply_all(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

apply_all(arm); apply_all(mesh)

# Turn the skeleton so its arms lie along X with the left hand at +X: the character then faces -Y.
def bone_pos(part):
    b = next(b for b in arm.data.bones if b.name.endswith(part))
    return arm.matrix_world @ b.head_local
for _ in range(4):
    l, r = bone_pos("LeftHand"), bone_pos("RightHand")
    if abs(l.x - r.x) > abs(l.y - r.y) and l.x > r.x: break
    arm.rotation_euler = (0, 0, 1.5708); apply_all(arm)
print("hands", bone_pos("LeftHand"), bone_pos("RightHand"))

def bounds(points):
    xs, ys, zs = zip(*points)
    return mathutils.Vector((min(xs), min(ys), min(zs))), mathutils.Vector((max(xs), max(ys), max(zs)))

def mesh_pts(): return [mesh.matrix_world @ v.co for v in mesh.data.vertices]
lo, hi = bounds(mesh_pts())
print("monster bounds", lo, hi)
# Arms of the T-pose lie along the wider horizontal axis; turn him so they lie along X like the donor.
if (hi.y - lo.y) > (hi.x - lo.x):
    mesh.rotation_euler = (0, 0, 1.5708); apply_all(mesh)
    lo, hi = bounds(mesh_pts())
# Which way does the face point? At head height the face/jaw sticks out further on the front side.
height = hi.z - lo.z
head = [p for p in mesh_pts() if p.z > lo.z + 0.88 * height]
cy = sum(p.y for p in head) / len(head)
front_neg = (cy - min(p.y for p in head)) > (max(p.y for p in head) - cy)
print("head centre y", cy, "front is -Y:", front_neg)
if not front_neg:
    mesh.rotation_euler = (0, 0, 3.14159); apply_all(mesh)
    lo, hi = bounds(mesh_pts())

# Fit the skeleton: scale it evenly to his height and stand it where he stands.
def skel_bounds():
    return bounds([arm.matrix_world @ b.head_local for b in arm.data.bones] + [arm.matrix_world @ b.tail_local for b in arm.data.bones])
blo, bhi = skel_bounds()
s = (hi.z - lo.z) / (bhi.z - blo.z)
arm.scale = (s, s, s); apply_all(arm)
blo, bhi = skel_bounds()
arm.location = ((lo.x + hi.x) / 2 - (blo.x + bhi.x) / 2, (lo.y + hi.y) / 2 - (blo.y + bhi.y) / 2, lo.z - blo.z); apply_all(arm)
print("uniform scale", s)
# Line the spine up with the middle of his chest and belly (front to back).
torso = [p for p in mesh_pts() if abs(p.x) < 0.12 and lo.z + 0.45 * (hi.z - lo.z) < p.z < lo.z + 0.7 * (hi.z - lo.z)]
torso_y = (min(p.y for p in torso) + max(p.y for p in torso)) / 2
spine = next(b for b in arm.data.bones if b.name.endswith("Spine1"))
shift = torso_y - (arm.matrix_world @ spine.head_local).y
arm.location.y += shift; apply_all(arm)
print("torso y", torso_y, "shifted skeleton by", shift)

# His arms angle out and down; the skeleton's hang straight down. Aim each arm chain from its shoulder
# at the middle of his hand (found from the outermost vertices) and stretch it to reach.
pts = mesh_pts()
half = max(abs(p.x) for p in pts)
to_local = arm.matrix_world.inverted()
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm.data.edit_bones
for side, sign in (("Left", 1), ("Right", -1)):
    hand_pts = [p for p in pts if p.x * sign > half - 0.18]
    hand_mid = sum(hand_pts, mathutils.Vector()) / len(hand_pts)
    wrist_target = to_local @ (hand_mid - mathutils.Vector((sign * 0.05, 0, 0)))
    upper = next(b for b in eb if b.name.endswith(side + "Arm"))
    hand = next(b for b in eb if b.name.endswith(side + "Hand"))
    chain = [upper] + list(upper.children_recursive)
    pivot = upper.head.copy()
    rot = (hand.head - pivot).normalized().rotation_difference((wrist_target - pivot).normalized())
    k = (wrist_target - pivot).length / (hand.head - pivot).length
    # Joined bones share end points, so work out every new position first, then set them all.
    new = {b.name: (pivot + (rot @ (b.head - pivot)) * k, pivot + (rot @ (b.tail - pivot)) * k) for b in chain}
    for b in chain: b.use_connect = False
    for b in chain: b.head, b.tail = new[b.name]
    print(side, "shoulder", arm.matrix_world @ pivot, "wrist", arm.matrix_world @ hand.head, "hand mid", hand_mid, "stretch", round(k, 2))
bpy.ops.object.mode_set(mode="OBJECT")

# Skin it.
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
empty = [g.name for g in mesh.vertex_groups if not any(g.index in [x.group for x in v.groups] for v in mesh.data.vertices)]
print("groups", len(mesh.vertex_groups), "unused", len(empty))
unweighted = sum(1 for v in mesh.data.vertices if not v.groups)
print("unweighted verts", unweighted)

mesh.name = "PaleMan"; arm.name = "Armature"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=out_path, use_selection=True, add_leaf_bones=False, bake_anim=False,
                         object_types={"ARMATURE", "MESH"}, apply_scale_options="FBX_SCALE_ALL")
print("EXPORTED", out_path)
