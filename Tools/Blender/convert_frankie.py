# Converts Big Frankie (assets/characters/monsters/big frankie (4).rar, "big frankie.fbx") for Last Stop:
# - removes his top hat (one loose piece sitting on the head),
# - gives the three materials clear names (Frankie_Body, Frankie_Bones, Frankie_Face),
# - keeps only the animations the game uses, with plain names: Idle, Walk, Run, Scream (the roar) and
#   Attack (frames 60-150 of the "door" animation: a crouched lunge with a claw reach),
# - exports one FBX for Unity.
# Run: blender -b --python convert_frankie.py -- "<big frankie.fbx>" "<out Frankie.fbx>"
import bpy, sys, bmesh

src, out = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = [o for o in bpy.data.objects if o.type == "ARMATURE"][0]
mesh = [o for o in bpy.data.objects if o.type == "MESH"][0]
arm.name = "Armature"
mesh.name = "Frankie"

# ---- the hat: the loose piece whose lowest point is above 350 (the head's top is at 401, in the mesh's units)
bm = bmesh.new(); bm.from_mesh(mesh.data)
seen, doomed = set(), []
for f in bm.faces:
    if f.index in seen: continue
    stack, island = [f], []
    seen.add(f.index)
    while stack:
        cur = stack.pop(); island.append(cur)
        for e in cur.edges:
            for nf in e.link_faces:
                if nf.index not in seen:
                    seen.add(nf.index); stack.append(nf)
    if min(v.co.y for fc in island for v in fc.verts) > 350:
        doomed.extend(island)
print("HAT FACES", len(doomed))
verts = {v for fc in doomed for v in fc.verts}
bmesh.ops.delete(bm, geom=list(verts), context="VERTS")
bm.to_mesh(mesh.data); bm.free()

# ---- material names
names = {"Material.007": "Frankie_Body", "foot": "Frankie_Bones", "Material.008": "Frankie_Face"}
for slot in mesh.material_slots:
    if slot.material and slot.material.name in names: slot.material.name = names[slot.material.name]

# ---- animations
def find(name):
    return next(a for a in bpy.data.actions if a.name.endswith("|" + name))
keep = {
    "Idle": find("scary idle frankie"),
    "Walk": find("Walking frankie"),
    "Run": find("run frankie"),
    "Scream": find("roar frankie"),
}
attack = find("Door anim frankie").copy()
attack.use_frame_range = True
attack.frame_start, attack.frame_end = 60, 150
keep["Attack"] = attack
for a in list(bpy.data.actions):
    if a not in keep.values(): bpy.data.actions.remove(a)
for name, a in keep.items():
    a.name = name
    a.use_fake_user = True
arm.animation_data_create()
arm.animation_data.action = keep["Idle"]
print("ACTIONS", [(a.name, tuple(a.frame_range)) for a in bpy.data.actions])

bpy.ops.export_scene.fbx(filepath=out, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="STRIP", apply_scale_options="FBX_SCALE_ALL")
print("EXPORTED", out)
