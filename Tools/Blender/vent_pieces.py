# Splits the "PSX Style Modular Vents" pack (Sketchfab, GLB) into the three pieces the level's ducts use,
# each 1 m across, with a pivot that makes them easy to chain in Unity:
#   DuctStraight - 1 m of square duct, flanged both ends; pivot at one end's centre, running up (Unity +Y).
#   DuctElbow    - a 90 degree bend; pivot at its inlet's centre (air comes in going up, Unity +Y) and its
#                  outlet 1 m up and 1 m along Unity -Z (air leaves going -Z).
#   VentGrille   - a flat louvred grille, 1 m square, centred, facing Unity +-Z.
# Also saves the pack's texture (the GLB's own copy, which is the right way up). Run:
#   blender -b --factory-startup --python vent_pieces.py -- <psx_style_modular_vents.glb> <out folder>
import bpy, sys, os
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
PIECES = {"Object_12": ("DuctStraight", "bottom"), "Object_16": ("DuctElbow", "inlet"), "Object_10": ("VentGrille", "centre")}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
img = bpy.data.images[0]
img.filepath_raw = os.path.join(out, "Vents.png"); img.file_format = "PNG"; img.save()

for obj_name, (name, pivot) in PIECES.items():
    o = bpy.data.objects[obj_name]
    world = o.matrix_world.copy(); o.parent = None; o.matrix_world = world
    for other in list(bpy.context.scene.objects):
        if other != o: other.hide_set(True)
    bpy.ops.object.select_all(action="DESELECT"); o.hide_set(False); o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pts = [v.co for v in o.data.vertices]
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    c = (lo + hi) / 2
    # Blender is Z-up; the inlet of the bend is its bottom (z = 0) at the middle of the vertical leg (y = 0).
    if pivot == "bottom": p = Vector((c.x, c.y, lo.z))
    elif pivot == "inlet": p = Vector((c.x, 0.0, lo.z))
    else: p = c
    o.data.transform(Matrix.Translation(-p))
    o.name = o.data.name = name
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=True, object_types={"MESH"},
                             path_mode="STRIP", apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)
    print("exported", name, "size", tuple(round(x, 2) for x in hi - lo))
    o.hide_set(True)
