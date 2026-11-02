# Copies trees out of a model file into clean FBXs for the street: transforms applied, scaled to a chosen
# height, standing on the origin at the foot of the trunk (bark material). With "bare", everything but the bark
# is removed (the leaf cards), leaving the trunk and limbs: a dead tree. Textures are copied beside the FBX
# separately. Run:
#   blender -b --factory-startup --python street_trees.py -- <source.fbx> <out folder> <object>:<out name>:<height m>[:bare] ...
import bpy, bmesh, os, sys
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, specs = args[0], args[1], args[2:]

for spec in specs:
    parts = spec.split(":")
    name, out_name, height, bare = parts[0], parts[1], float(parts[2]), len(parts) > 3 and parts[3] == "bare"
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=src)
    tree = bpy.data.objects[name]
    for o in list(bpy.data.objects):
        if o != tree: bpy.data.objects.remove(o, do_unlink=True)
    world = tree.matrix_world.copy()
    tree.parent = None
    tree.matrix_world = world
    bpy.context.view_layer.objects.active = tree
    tree.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    mesh = tree.data
    bark = {i for i, s in enumerate(tree.material_slots) if s.material and "bark" in s.material.name.lower()}
    if bare:
        bm = bmesh.new(); bm.from_mesh(mesh)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index not in bark], context="FACES")
        bm.to_mesh(mesh); bm.free()
        for i in reversed(range(len(tree.material_slots))):   # drop the leaf materials
            if i not in bark:
                tree.active_material_index = i
                bpy.ops.object.material_slot_remove()

    # Stand it on the origin at the foot of its trunk, then scale it to 'height'.
    pts = [v.co for v in mesh.vertices]
    low = min(p.z for p in pts)
    foot = [mesh.vertices[i].co for poly in mesh.polygons if poly.material_index in bark
            for i in poly.vertices if mesh.vertices[i].co.z < low + 1.0] or pts
    cx = (min(p.x for p in foot) + max(p.x for p in foot)) / 2
    cy = (min(p.y for p in foot) + max(p.y for p in foot)) / 2
    top = max(p.z for p in pts)
    k = height / (top - low)
    mesh.transform(Matrix.Scale(k, 4) @ Matrix.Translation(Vector((-cx, -cy, -low))))

    pts = [v.co for v in mesh.vertices]
    size = tuple(round(max(p[a] for p in pts) - min(p[a] for p in pts), 2) for a in range(3))
    tree.name = out_name
    out = os.path.join(out_dir, out_name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={"MESH"}, path_mode="STRIP",
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
    print("EXPORTED", out_name, "size", size, "materials", [s.material.name for s in tree.material_slots])
