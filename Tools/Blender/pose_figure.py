# Poses a rigged Mixamo character (elbolilloduro's Characters PSX: T-pose, facing -Y, which is Unity's forward
# once exported) into a still figure for the shadow glimpses: arms hanging at his sides, head tilted as if
# studying you. The pose is baked into the mesh, the skeleton removed, and he stands on the origin. Also renders
# a black preview from the front. Run:
#   blender -b --factory-startup --python pose_figure.py -- <rigged.fbx> <out.fbx> <preview.png>
import bpy, math, sys
from mathutils import Matrix, Vector

src, out, preview = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
body = next(o for o in bpy.data.objects if o.type == "MESH")
forward = Vector((0, -1, 0))   # the way he faces in the file (his arms reach along X)
side = Vector((1, 0, 0))

def turn(bone, axis, degrees):
    """Turns a pose bone (and so everything below it) about a world axis through its joint."""
    pb = rig.pose.bones[bone]
    bpy.context.view_layer.update()
    world = rig.matrix_world @ pb.matrix
    joint = world.to_translation()
    rot = Matrix.Translation(joint) @ Matrix.Rotation(math.radians(degrees), 4, axis) @ Matrix.Translation(-joint)
    pb.matrix = rig.matrix_world.inverted() @ rot @ world

# Arms down: the T-pose arms point sideways (+X his left, -X his right); swing each down about the facing
# axis, leaving them a hand's width off the hips.
turn("mixamorig:LeftArm", forward, -78)
turn("mixamorig:RightArm", forward, 78)
# Head tilted to one side, the neck taking a little of it; chin a touch down.
turn("mixamorig:Neck", forward, 9)
turn("mixamorig:Head", forward, 17)
turn("mixamorig:Head", side, -5)

# Bake the pose into the mesh and drop the skeleton.
bpy.context.view_layer.objects.active = body
for m in list(body.modifiers):
    if m.type == "ARMATURE":
        bpy.ops.object.modifier_apply(modifier=m.name)
world = body.matrix_world.copy()
body.parent = None
body.matrix_world = world
bpy.data.objects.remove(rig, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Stand on the origin, centred.
mesh = body.data
pts = [v.co for v in mesh.vertices]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
mesh.transform(Matrix.Translation(Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))))
body.name = "ShadowPerson"
size = tuple(round(c, 2) for c in hi - lo)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={"MESH"}, path_mode="STRIP",
                         apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True)

# Preview: pure black against a pale wall, from the front (and a three-quarter view beside it).
black = bpy.data.materials.new("Black"); black.diffuse_color = (0, 0, 0, 1)
black.use_nodes = True
black.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0, 0, 0, 1)
black.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1
mesh.materials.clear(); mesh.materials.append(black)
twin = body.copy(); twin.data = mesh; bpy.context.scene.collection.objects.link(twin)
twin.rotation_euler = (0, 0, math.radians(35)); twin.location = (1.2, 0, 0)
scene = bpy.context.scene
scene.render.engine = "CYCLES"; scene.cycles.samples = 16
scene.render.resolution_x, scene.render.resolution_y = 800, 800
world_bg = bpy.data.worlds.new("w"); scene.world = world_bg; world_bg.use_nodes = True
world_bg.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.73, 0.68, 1)
cam_data = bpy.data.cameras.new("cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = 2.4
cam = bpy.data.objects.new("cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam
cam.location = (0.6, -6, 0.95); cam.rotation_euler = (math.radians(90), 0, 0)
scene.render.filepath = preview
bpy.ops.render.render(write_still=True)
print("POSED", out, "size", size)
