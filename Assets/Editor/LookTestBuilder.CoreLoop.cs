using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The gameplay half of the look-test builder: the power system, fuse box, fuses and batteries,
// tunnel work lamps, the staff room (door, furniture, desk clutter, locker with shotgun and shells)
// and the player's gear (inventory, interaction, flashlight battery, shotgun with WRAD ARMS).
public static partial class LookTestBuilder
{
    const float TunnelEnd = 68.3f;     // each tunnel runs from x = 18.3 to here (about 50 m)
    const float StaffDoorX = 42.5f;    // west edge of the staff room doorway in the east tunnel

    const string ArtDir = "Assets/Art/Models";
    const string BreakerFbx = ArtDir + "/Props/Plumbing/CircuitBreaker.fbx";
    const string SurvivalFbx = ArtDir + "/Props/Survival/Survival.fbx";

    static PowerGrid grid;

    static void BuildCoreLoop(Material tubeLit, Material housing, Material concrete, Material ceiling,
                              Material floorTile, Material doorMetal)
    {
        var crate = Mat("Crate", Tex("metal_01.jpg"), new Color(0.55f, 0.5f, 0.42f), 1f, 0.2f, 0.3f);
        var bulb = EmissiveMat("WorkBulb", Fluorescent * 3f);

        // ---- power for the whole area
        var power = new GameObject("PowerGrid");
        power.transform.SetParent(root);
        grid = power.AddComponent<PowerGrid>();
        grid.clunk = Load<AudioClip>(SfxDir + "/Hammer2.wav");
        grid.musicSting = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Powerbox.ogg");
        var hum = power.AddComponent<AudioSource>();
        hum.clip = Load<AudioClip>(AmbienceDir + "/DSGNDron_Noise-Empty Engine Room_HY_SD.wav");
        hum.loop = true; hum.playOnAwake = false; hum.spatialBlend = 0f;
        grid.hum = hum;

        // ---- fuse box on the platform's west end wall, in the flickering lamp's light
        // At eye level (centre 1.5 m) and a good size, so it's easy to look at and hard to miss.
        BuildFuseBox(new Vector3(-18f, 1.5f, 17f));

        // ---- tunnel work lamps: caged bulbs on the inner wall, dark until the power returns
        var lamps = Group("TunnelLamps");
        foreach (int side in new[] { -1, 1 })
            foreach (float x in new[] { 24f, 36f, 48f, 60f })
                WorkLamp(lamps, new Vector3(x * side, 1.4f, 20f), housing, bulb);

        // ---- fuses (one per tunnel) and spare batteries, on crates and the floor
        var items = Group("Pickups");
        var fuseCrateW = Span("Crate_FuseW", items, new Vector3(-60.5f, -1.3f, 24.7f), new Vector3(-59.9f, -0.7f, 25.3f), crate);
        MakePickup(ModelPiece(BreakerFbx, "Fuse", items, Tex2("Props/Plumbing/FuseBox.png"), 1.6f), global::Pickup.Kind.Fuse,
               Top(fuseCrateW, -60.2f, 25.0f), 30f, "PlasticBox1");
        var fuseCrateE = Span("Crate_FuseE", items, new Vector3(61.7f, -1.3f, 20.2f), new Vector3(62.3f, -0.7f, 20.8f), crate);
        MakePickup(ModelPiece(BreakerFbx, "Fuse", items, Tex2("Props/Plumbing/FuseBox.png"), 1.6f), global::Pickup.Kind.Fuse,
               Top(fuseCrateE, 62f, 20.5f), -60f, "PlasticBox1");
        MakePickup(ModelPiece(SurvivalFbx, "Battery", items, Tex2("Props/Survival/Battery.png"), 1.5f), global::Pickup.Kind.Battery,
               new Vector3(-13.2f, 0f, 14.8f), 70f, "PlasticBox1", lieDown: true);   // clear of the bench, so it can be looked at
        var batteryCrate = Span("Crate_Battery", items, new Vector3(-36.3f, -1.3f, 20.2f), new Vector3(-35.7f, -0.7f, 20.8f), crate);
        MakePickup(ModelPiece(SurvivalFbx, "Battery", items, Tex2("Props/Survival/Battery.png"), 1.5f), global::Pickup.Kind.Battery,
               Top(batteryCrate, -36f, 20.5f), 15f, "PlasticBox1", lieDown: true);

        BuildStaffRoom(tubeLit, housing, concrete, ceiling, floorTile, doorMetal);
    }

    // ---------------------------------------------------------------- fuse box
    static void BuildFuseBox(Vector3 wallPoint)
    {
        var box = new GameObject("FuseBox");
        box.transform.SetParent(root);
        // The breaker model's front faces -X; turn it around so it faces into the platform (+X).
        var model = Model(BreakerFbx, null, box.transform, Tex2("Props/Plumbing/FuseBox.png"), 2.2f, 180f);
        var b = Bounds(model);
        model.transform.position += wallPoint + new Vector3(0.002f, 0, 0) - new Vector3(b.min.x, b.center.y, b.center.z);

        // The model's own fuse becomes slot 1; a copy beside it is slot 2. Both stay hidden until filled.
        var fuse1 = model.transform.Find("Fuse").gameObject;
        var fuse2 = Object.Instantiate(fuse1, fuse1.transform.parent);
        fuse2.name = "Fuse2";
        fuse2.transform.localPosition += new Vector3(0, 0, -0.214f);   // mirrored across the box's centre

        b = Bounds(model);
        model.transform.SetParent(null, true);          // move the box's origin to the model's centre
        box.transform.position = b.center;
        model.transform.SetParent(box.transform, true);
        var col = box.AddComponent<BoxCollider>();
        col.center = box.transform.InverseTransformPoint(b.center);
        col.size = b.size + new Vector3(0.05f, 0.05f, 0.05f);

        var fuseBox = box.AddComponent<FuseBox>();
        fuseBox.grid = grid;
        fuseBox.fuseSlots = new[] { fuse1, fuse2 };
        fuseBox.insertSound = Load<AudioClip>(SfxDir + "/SwitchButton2_On.wav");
    }

    // ---------------------------------------------------------------- lamps
    static void WorkLamp(Transform parent, Vector3 wallPoint, Material housing, Material bulb)
    {
        var lamp = new GameObject("WorkLamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = wallPoint + new Vector3(0, 0, 0.08f);
        var cage = Box("Cage", lamp.transform, lamp.transform.position + new Vector3(0, 0.12f, -0.02f), new Vector3(0.2f, 0.06f, 0.12f), housing);
        Object.DestroyImmediate(cage.GetComponent<Collider>());
        var glass = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glass.name = "Bulb";
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        glass.transform.SetParent(lamp.transform, false);
        glass.transform.localScale = Vector3.one * 0.11f;
        glass.GetComponent<Renderer>().sharedMaterial = bulb;
        var light = AddLight(lamp, LightType.Point, Fluorescent, 10f, 8f);
        light.transform.localPosition = new Vector3(0, -0.05f, 0.25f);
        var power = lamp.AddComponent<PoweredLight>();
        power.lights = new[] { light };
        power.glowParts = new[] { glass.GetComponent<Renderer>() };
        power.glowColour = Fluorescent * 3f;
    }

    // ---------------------------------------------------------------- staff room
    // A small room behind the east tunnel's outer wall (x 40..46, z 25.8..30.3, floor at pit level).
    static void BuildStaffRoom(Material tubeLit, Material housing, Material concrete, Material ceiling,
                               Material floorTile, Material doorMetal)
    {
        var room = Group("StaffRoom");
        const float floorY = -1.3f;
        Span("Floor", room, new Vector3(40, -1.5f, 25.8f), new Vector3(46, floorY, 30.3f), floorTile);
        Span("Ceiling", room, new Vector3(40, 1.2f, 25.8f), new Vector3(46, 1.4f, 30.3f), ceiling);
        Span("WallWest", room, new Vector3(39.7f, -1.5f, 25.8f), new Vector3(40, 1.4f, 30.6f), concrete);
        Span("WallEast", room, new Vector3(46, -1.5f, 25.8f), new Vector3(46.3f, 1.4f, 30.6f), concrete);
        Span("WallBack", room, new Vector3(40, -1.5f, 30.3f), new Vector3(46, 1.4f, 30.6f), concrete);
        var staffLamp = Lamp("StaffLamp", room, new Vector3(43, 1.18f, 28.2f), 90f, tubeLit, housing);
        // The tube's spot light only shines down; a soft fill light shows the walls and the locker.
        var roomFill = AddLight(staffLamp, LightType.Point, Fluorescent, 3f, 7f);
        roomFill.transform.localPosition = new Vector3(0, -0.4f, 0);
        var lampPower = staffLamp.GetComponent<PoweredLight>();
        lampPower.lights = new[] { lampPower.lights[0], roomFill };

        // Door: swings into the room once the power unlocks it.
        var hinge = new GameObject("StaffDoor");
        hinge.transform.SetParent(room);
        hinge.transform.position = new Vector3(StaffDoorX, floorY, 25.65f);
        var door = Box("Door", hinge.transform, new Vector3(StaffDoorX + 0.5f, floorY + 1.09f, 25.65f), new Vector3(0.96f, 2.18f, 0.05f), doorMetal);
        var staffDoor = hinge.AddComponent<HingedDoor>();
        staffDoor.openAngle = -95f;
        staffDoor.openTime = 2.5f;
        staffDoor.needsPower = grid;
        staffDoor.doorName = "staff room";
        staffDoor.unlockSound = Load<AudioClip>(SfxDir + "/SwitchButton3_On.wav");
        staffDoor.openSound = Load<AudioClip>(SfxDir + "/OldDoorOpen.wav");

        // Desk against the back wall, chair in front of it.
        var desk = Model(ArtDir + "/Props/Office/desk1_weathered.fbx", null, room, Tex2("Props/Office/desk_1_old.jpg"), 1f, 0f);
        StandOn(desk, new Vector3(43, floorY, 29.7f));
        AddMeshColliders(desk);
        var chair = Model(ArtDir + "/Props/Office/office_chair_black.fbx", null, room, Tex2("Props/Office/office_chair_black.jpg"), 1f, 160f);
        StandOn(chair, new Vector3(43.2f, floorY, 28.7f));
        AddMeshColliders(chair);

        // Desk clutter: logbook (the future save point), half-eaten pizza, soda can, cigarettes and a lighter.
        float top = Bounds(desk).max.y;
        var logbook = Model(ArtDir + "/Props/Office/file_binder.fbx", null, room, Tex2("Props/Office/file_binder.jpg"), 1f, 0f);
        logbook.transform.rotation = Quaternion.Euler(0, 15f, 90f) * logbook.transform.rotation;   // lying flat
        StandOn(logbook, new Vector3(42.3f, top, 29.7f));
        AddMeshColliders(logbook);
        logbook.AddComponent<Logbook>().pageSound = Load<AudioClip>(SfxDir + "/PaperDocument1.wav");

        var pizza = Model(ArtDir + "/Props/Kitchen/pizza_quarter_eaten.fbx", null, room, null, 1f, 10f, new Dictionary<string, string>
        {
            { "pizza", "Props/Kitchen/pizza.png" }, { "pizza box", "Props/Kitchen/Pizza box.png" },
        });
        StandOn(pizza, new Vector3(43.6f, top, 29.75f));
        var can = Model(ArtDir + "/Props/Kitchen/can_cola_cola.fbx", null, room, Tex2("Props/Kitchen/cola can.png"), 1f, 40f);
        StandOn(can, new Vector3(42.75f, top, 29.35f));

        var cigarettes = CigarettePack(room);
        StandOn(cigarettes, new Vector3(43.0f, top, 29.3f));
        cigarettes.transform.rotation = Quaternion.Euler(0, 25f, 0);
        var lighter = ModelPiece(SurvivalFbx, "zippo_01", room, Tex2("Props/Survival/zippo.png"), 0.55f);
        lighter.transform.rotation = Quaternion.Euler(90f, 0, 0) * lighter.transform.rotation;      // lying on its side
        StandOn(lighter, new Vector3(43.13f, top, 29.22f));

        BuildLocker(room, floorY);
    }

    // A placeholder cigarette pack (no model in the asset library yet): red box with a white lid.
    static GameObject CigarettePack(Transform parent)
    {
        var pack = new GameObject("CigarettePack");
        pack.transform.SetParent(parent);
        var red = Mat("CigarettesRed", null, new Color(0.62f, 0.08f, 0.07f), 1f, 0.35f);
        var white = Mat("CigarettesWhite", null, new Color(0.85f, 0.83f, 0.78f), 1f, 0.35f);
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body"; body.transform.SetParent(pack.transform, false);
        body.transform.localScale = new Vector3(0.055f, 0.022f, 0.062f);
        body.transform.localPosition = new Vector3(0, 0.011f, -0.012f);
        body.GetComponent<Renderer>().sharedMaterial = red;
        var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid"; lid.transform.SetParent(pack.transform, false);
        lid.transform.localScale = new Vector3(0.055f, 0.022f, 0.024f);
        lid.transform.localPosition = new Vector3(0, 0.011f, 0.031f);
        lid.GetComponent<Renderer>().sharedMaterial = white;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        Object.DestroyImmediate(lid.GetComponent<Collider>());
        return pack;
    }

    // ---------------------------------------------------------------- locker, shotgun and shells
    // A tall steel staff locker against the room's west wall, opening toward the room (+X).
    static void BuildLocker(Transform room, float floorY)
    {
        var steel = Mat("LockerSteel", Tex("metal_04.png"), new Color(0.5f, 0.55f, 0.58f), 1f, 0.45f, 0.6f);
        var locker = new GameObject("Locker").transform;
        locker.SetParent(room);
        float x0 = 40.02f, x1 = 40.55f, z0 = 26.6f, z1 = 27.2f, yTop = floorY + 1.9f, t = 0.03f;
        Span("Back", locker, new Vector3(x0, floorY, z0), new Vector3(x0 + t, yTop, z1), steel);
        Span("SideA", locker, new Vector3(x0 + t, floorY, z0), new Vector3(x1, yTop, z0 + t), steel);
        Span("SideB", locker, new Vector3(x0 + t, floorY, z1 - t), new Vector3(x1, yTop, z1), steel);
        Span("Bottom", locker, new Vector3(x0 + t, floorY, z0 + t), new Vector3(x1, floorY + 0.05f, z1 - t), steel);
        Span("Top", locker, new Vector3(x0 + t, yTop - t, z0 + t), new Vector3(x1, yTop, z1 - t), steel);
        float shelfY = floorY + 1.5f;
        Span("Shelf", locker, new Vector3(x0 + t, shelfY - 0.02f, z0 + t), new Vector3(x1, shelfY, z1 - t), steel);

        // Door hinged on the near-tunnel side; swings out into the room.
        var hinge = new GameObject("LockerDoor");
        hinge.transform.SetParent(locker);
        hinge.transform.position = new Vector3(x1 + 0.015f, floorY, z0);
        Box("Door", hinge.transform, new Vector3(x1 + 0.015f, floorY + 0.95f, (z0 + z1) * 0.5f), new Vector3(0.025f, 1.88f, z1 - z0 - 0.01f), steel);
        var lockerDoor = hinge.AddComponent<HingedDoor>();
        lockerDoor.openAngle = 110f;
        lockerDoor.openTime = 0.8f;
        lockerDoor.doorName = "locker";
        lockerDoor.openSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");

        // The shotgun stands upright inside, barrels up, leaning on the back wall.
        var gun = ShotgunModel(locker);
        gun.transform.rotation = Quaternion.Euler(-82f, 90f, 0f) * gun.transform.rotation;   // barrels up
        var gb = Bounds(gun);
        gun.transform.position += new Vector3((x0 + x1) * 0.5f, floorY + 0.06f, (z0 + z1) * 0.5f) - new Vector3(gb.center.x, gb.min.y, gb.center.z);
        MakePickup(gun, global::Pickup.Kind.Shotgun, gun.transform.position, null, "DesignedGunSoundHandling1", keepPlace: true);

        // Three red shells lying on the shelf.
        for (int i = 0; i < 3; i++)
        {
            var shell = Shell(locker);
            shell.transform.position = new Vector3(40.25f + i * 0.02f, shelfY + 0.012f, 26.78f + i * 0.09f);
            shell.transform.rotation = Quaternion.Euler(0, 20f + i * 35f, 90f);
            MakePickup(shell, global::Pickup.Kind.Shell, shell.transform.position, null, "DesignedGunSoundReload1", keepPlace: true);
        }
    }

    // One 12-gauge shell: red plastic hull and a brass base (simple shapes; no shell model exists yet).
    static GameObject Shell(Transform parent)
    {
        var shell = new GameObject("ShotgunShell");
        shell.transform.SetParent(parent);
        var hull = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hull.name = "Hull"; hull.transform.SetParent(shell.transform, false);
        hull.transform.localScale = new Vector3(0.022f, 0.027f, 0.022f);
        hull.GetComponent<Renderer>().sharedMaterial = Mat("ShellHull", null, new Color(0.65f, 0.07f, 0.06f), 1f, 0.5f);
        var brass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        brass.name = "Base"; brass.transform.SetParent(shell.transform, false);
        brass.transform.localScale = new Vector3(0.0235f, 0.007f, 0.0235f);
        brass.transform.localPosition = new Vector3(0, -0.027f, 0);
        brass.GetComponent<Renderer>().sharedMaterial = Mat("ShellBrass", null, new Color(0.75f, 0.58f, 0.25f), 1f, 0.7f, 0.9f);
        Object.DestroyImmediate(hull.GetComponent<Collider>());
        Object.DestroyImmediate(brass.GetComponent<Collider>());
        return shell;
    }

    // The PS1 shotgun model, turned so its barrels point along +Z and scaled to about 0.95 m.
    static GameObject ShotgunModel(Transform parent)
    {
        var gun = Model(ArtDir + "/Weapons/ShotgunPS1/ShotgunPS1.fbx", null, parent, Tex2("Weapons/ShotgunPS1/ShotgunPS1.png"), 0.0935f, 180f);
        gun.name = "Shotgun";
        return gun;
    }

    // ---------------------------------------------------------------- player gear
    static void BuildPlayerGear(GameObject player)
    {
        var cam = player.transform.Find("Main Camera");
        var inventory = player.AddComponent<PlayerInventory>();
        player.AddComponent<Hud>();
        var interactor = cam.gameObject.AddComponent<Interactor>();
        interactor.inventory = inventory;

        var torch = cam.Find("Flashlight");
        var battery = torch.gameObject.AddComponent<FlashlightBattery>();
        battery.inventory = inventory;
        battery.swapSound = Load<AudioClip>(SfxDir + "/PlasticBox1.wav");
        torch.GetComponent<Flashlight>().battery = battery;

        // ---- shotgun held in first person
        var rig = new GameObject("ShotgunRig").transform;
        rig.SetParent(cam, false);
        var pivot = new GameObject("Gun").transform;         // grip point; moved by Shotgun for raise/kick
        pivot.SetParent(rig, false);
        var gun = ShotgunModel(pivot);
        gun.transform.localPosition = Vector3.zero;

        // Put the trigger at the pivot, so the right hand (and the raise motion) is centred on the grip.
        Transform trigger = gun.transform.Find("Trigger"), forend = gun.transform.Find("Forend"), body = gun.transform.Find("Body");
        Vector3 triggerLocal = pivot.InverseTransformPoint(trigger.GetComponent<Renderer>().bounds.center);
        gun.transform.localPosition -= triggerLocal;
        var bodyBounds = body.GetComponent<Renderer>().bounds;
        foreach (var r in gun.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(pivot, false);
        Vector3 bodyCentre = pivot.InverseTransformPoint(bodyBounds.center);
        float frontZ = 0f;
        foreach (var c in Corners(bodyBounds)) frontZ = Mathf.Max(frontZ, pivot.InverseTransformPoint(c).z);
        muzzle.localPosition = new Vector3(bodyCentre.x, pivot.InverseTransformPoint(forend.GetComponent<Renderer>().bounds.center).y + 0.02f, frontZ + 0.02f);
        var flashLight = muzzle.gameObject.AddComponent<Light>();
        flashLight.type = LightType.Point; flashLight.color = new Color(1f, 0.72f, 0.38f);
        flashLight.intensity = 60f; flashLight.range = 14f; flashLight.shadows = LightShadows.None;
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MuzzleFlash";
        Object.DestroyImmediate(flash.GetComponent<Collider>());
        flash.transform.SetParent(muzzle, false);
        flash.GetComponent<Renderer>().sharedMaterial = EmissiveMat("MuzzleFlash", new Color(1f, 0.65f, 0.25f) * 8f);
        flash.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        var gripRight = new GameObject("GripRight").transform;
        gripRight.SetParent(pivot, false);
        gripRight.localPosition = new Vector3(0, -0.02f, -0.05f);
        var gripLeft = new GameObject("GripLeft").transform;
        gripLeft.SetParent(pivot, false);
        Vector3 forendLocal = pivot.InverseTransformPoint(forend.GetComponent<Renderer>().bounds.center);
        // The left wrist sits below and to the left of the forend: the palm cups its underside,
        // the thumb runs along its left side and the fingers wrap up the right side.
        gripLeft.localPosition = forendLocal + LeftGripOffset;


        var shotgun = rig.gameObject.AddComponent<Shotgun>();
        var arms = BuildArms(rig, gripRight, gripLeft, shotgun);

        // Light layers: the torch skips the held gun and arms (it would blast them white at 30 cm);
        // station lamps still light them, plus a faint fill light that follows the torch.
        EnableRenderingLayers();
        foreach (var r in gun.GetComponentsInChildren<Renderer>()) r.renderingLayerMask = ViewmodelLayer;
        foreach (var r in arms.GetComponentsInChildren<Renderer>()) r.renderingLayerMask = ViewmodelLayer;
        flash.GetComponent<Renderer>().renderingLayerMask = ViewmodelLayer;
        var flashlightScript = torch.GetComponent<Flashlight>();
        foreach (var l in torch.GetComponentsInChildren<Light>()) SetLightLayers(l, ~ViewmodelLayer);
        var fill = new GameObject("ViewmodelFill").AddComponent<Light>();
        fill.transform.SetParent(rig, false);
        fill.transform.localPosition = new Vector3(0.1f, 0.15f, 0.05f);
        fill.type = LightType.Point; fill.color = new Color(1f, 0.94f, 0.82f);
        fill.intensity = 0.25f; fill.range = 1.5f; fill.shadows = LightShadows.None;
        SetLightLayers(fill, ViewmodelLayer);
        flashlightScript.viewmodelFill = fill;

        shotgun.raisedPosition = new Vector3(0.12f, -0.16f, 0.22f);
        shotgun.inventory = inventory;
        shotgun.look = player.GetComponent<PlayerLook>();
        shotgun.gun = pivot;
        shotgun.muzzle = muzzle;
        shotgun.muzzleLight = flashLight;
        shotgun.muzzleFlash = flash.GetComponent<Renderer>();
        shotgun.showWhenRaised = new[] { gun };
        shotgun.impactMaterial = ImpactMaterial();
        shotgun.raiseSound = Load<AudioClip>(SfxDir + "/DesignedGunSoundHandling1.wav");
        shotgun.lowerSound = Load<AudioClip>(SfxDir + "/DesignedGunSoundHandling2.wav");
        shotgun.reloadSound = Load<AudioClip>(SfxDir + "/DesignedGunSoundReload1.wav");
        shotgun.dryFireSound = Load<AudioClip>(SfxDir + "/SwitchButton1_Off.wav");
    }

    // PSX first-person arms (bare skin). The rig has a "camera" bone the animations were
    // made around, so the model is placed with that bone exactly on the player's camera.
    // ArmsRig bends the arms around the gun; ArmsController shows them and plays the grab animation.
    static GameObject BuildArms(Transform rig, Transform gripRight, Transform gripLeft, Shotgun shotgun)
    {
        var arms = Model(ArtDir + "/Characters/PsxArms/PsxArms.fbx", null, rig, Tex2("Characters/PsxArms/PsxArms_Bare.png"), 1f, 0f);
        arms.name = "Arms";
        arms.transform.localRotation = Quaternion.identity;
        Transform cameraBone = null;
        foreach (var t in arms.GetComponentsInChildren<Transform>()) if (t.name == "camera") cameraBone = t;
        arms.transform.localPosition = -rig.InverseTransformPoint(cameraBone.position);
        // Shoulders sit a bit forward of the camera, so the hands reach the gun with the elbows
        // bent down (a fully stretched arm looks stiff and robotic).
        arms.transform.localPosition += ShoulderOffset;
        foreach (var r in arms.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            r.updateWhenOffscreen = true;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        var hintRight = new GameObject("ElbowHintRight").transform;
        hintRight.SetParent(rig, false);
        hintRight.localPosition = new Vector3(0.55f, -0.7f, 0.0f);
        var hintLeft = new GameObject("ElbowHintLeft").transform;
        hintLeft.SetParent(rig, false);
        hintLeft.localPosition = LeftElbowHint;                        // elbow angled out to the left

        var ik = arms.AddComponent<ArmsRig>();
        ik.right = ArmFor(arms.transform, "R", gripRight, hintRight);
        ik.left = ArmFor(arms.transform, "L", gripLeft, hintLeft);

        var controller = rig.gameObject.AddComponent<ArmsController>();
        controller.shotgun = shotgun;
        controller.rig = ik;
        controller.clips = arms.GetComponent<Animation>();
        controller.visuals = arms;
        return arms;
    }

    static Vector3 ShoulderOffset = new Vector3(0f, -0.04f, 0.16f);
    static float LeftCurl = 70f;
    static Vector3 LeftGripOffset = new Vector3(-0.08f, -0.07f, 0f);
    static Vector3 LeftElbowHint = new Vector3(-0.8f, -0.5f, 0.1f);
    static Vector3 LeftFingers = new Vector3(1f, 0.1f, 0.25f);
    static Vector3 LeftThumb = new Vector3(-0.2f, 0.3f, 1f);    // degrees per joint the left fingers curl up around the forend

    static ArmsRig.Arm ArmFor(Transform arms, string side, Transform target, Transform hint)
    {
        var bones = new Dictionary<string, Transform>();
        foreach (var t in arms.GetComponentsInChildren<Transform>()) bones[t.name] = t;
        var fingers = new List<Transform>();
        var thumb = new List<Transform>();
        foreach (var name in new[] { "index", "middle", "ring", "pinky" })
            for (int i = 1; i <= 3; i++) fingers.Add(bones["f_" + name + ".0" + i + "." + side]);
        for (int i = 1; i <= 3; i++) thumb.Add(bones["thumb.0" + i + "." + side]);
        return new ArmsRig.Arm
        {
            upper = bones["upper_arm." + side], lower = bones["forearm." + side], hand = bones["hand." + side],
            target = target, elbowHint = hint,
            fingers = fingers.ToArray(), thumb = thumb.ToArray(),
            // Right hand wraps the stock's grip (knuckles forward-down, thumb on top);
            // left hand: palm up under the forend, fingers pointing across it (to the right) so they can
            // wrap up its right side; with the palm up and fingers to the right, the thumb points forward,
            // lying along the left side of the forend.
            fingersDirection = side == "R" ? new Vector3(0f, -0.6f, 0.8f) : LeftFingers,
            thumbDirection = side == "R" ? new Vector3(-0.3f, 0.9f, 0.3f) : LeftThumb,
            fingerCurl = side == "R" ? -60f : LeftCurl,
            thumbCurl = side == "R" ? -25f : 0f,
        };
    }
    // Dark, ragged dent mark for shotgun hits: a soft-edged spot drawn once by code and saved as a texture.
    static Material ImpactMaterial()
    {
        const string texPath = "Assets/Art/Textures/Generated/ImpactMark.png";
        EnsureFolder("Assets/Art/Textures/Generated");
        if (!System.IO.File.Exists(texPath))
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var rng = new System.Random(7);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = new Vector2(x - 15.5f, y - 15.5f).magnitude / 16f;
                    float a = Mathf.Clamp01((1f - d) * 2.2f + (float)rng.NextDouble() * 0.5f - 0.35f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, d > 0.95f ? 0 : a));
                }
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
        }
        var mat = TransparentMat("ImpactMark", new Color(0.04f, 0.035f, 0.03f, 0.95f), 0.1f);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_BlendModePreserveSpecular", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return mat;
    }

    // ---------------------------------------------------------------- light layers
    const uint ViewmodelLayer = 1u << 1;   // "Light Layer 1": only the held gun and arms

    // Turns on URP's rendering layers (light layers) in the project's PC pipeline asset.
    static void EnableRenderingLayers()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var so = new SerializedObject(asset);
        var prop = so.FindProperty("m_SupportsLightLayers");
        if (prop != null && !prop.boolValue) { prop.boolValue = true; so.ApplyModifiedProperties(); EditorUtility.SetDirty(asset); }
        if (prop == null) Debug.LogWarning("Could not find the rendering layers setting on the URP asset.");
    }

    static void SetLightLayers(Light light, uint mask)
    {
        var data = light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        if (data == null) data = light.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        data.renderingLayers = mask;
        light.renderingLayerMask = unchecked((int)mask);
    }

    // ---------------------------------------------------------------- model helpers
    static Texture2D Tex2(string pathUnderModels) => Load<Texture2D>(ArtDir + "/" + pathUnderModels);

    // Instantiates a whole model (piece == null) or one named piece of it, scaled and turned by yaw.
    // Every material gets 'texture', or a texture looked up by material name in 'texturesByMaterial'.
    static GameObject Model(string fbxPath, string piece, Transform parent, Texture2D texture, float scale, float yaw,
                            Dictionary<string, string> texturesByMaterial = null)
    {
        var asset = Load<GameObject>(fbxPath);
        GameObject go;
        if (piece == null)
        {
            go = Object.Instantiate(asset, parent);
            go.name = asset.name;
            go.transform.localScale = asset.transform.localScale * scale;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0) * asset.transform.rotation;
            go.transform.position = parent.position;
        }
        else
        {
            Transform source = null;
            foreach (var t in asset.GetComponentsInChildren<Transform>()) if (t.name == piece) { source = t; break; }
            go = Object.Instantiate(source.gameObject, parent);
            go.name = piece;
            foreach (Transform child in go.transform) Object.DestroyImmediate(child.gameObject);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0) * source.rotation;
            go.transform.localScale = source.lossyScale * scale;
            go.transform.position = parent.position;
        }

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string matName = mats[i].name;
                Texture2D tex = texture;
                if (texturesByMaterial != null && texturesByMaterial.TryGetValue(matName, out var texPath)) tex = Tex2(texPath);
                string key = (tex ? tex.name : matName).Replace(" ", "_");
                mats[i] = Mat("Model_" + key, tex, Color.white, 1f, 0.25f);
            }
            r.sharedMaterials = mats;
        }
        return go;
    }

    static GameObject ModelPiece(string fbxPath, string piece, Transform parent, Texture2D texture, float scale)
        => Model(fbxPath, piece, parent, texture, scale, 0f);

    // Moves an object so the bottom-centre of its bounds sits on 'point'.
    static void StandOn(GameObject go, Vector3 point)
    {
        var b = Bounds(go);
        go.transform.position += point - new Vector3(b.center.x, b.min.y, b.center.z);
    }

    static void AddMeshColliders(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>();
    }

    static Vector3 Top(GameObject box, float x, float z) => new Vector3(x, Bounds(box).max.y, z);

    // Makes 'go' a pickup: places it (unless keepPlace), adds an easy-to-aim trigger box and the Pickup script.
    static void MakePickup(GameObject go, global::Pickup.Kind kind, Vector3 floorPoint, float? yaw, string soundName,
                       bool lieDown = false, bool keepPlace = false)
    {
        if (!keepPlace)
        {
            if (lieDown) go.transform.rotation = Quaternion.Euler(0, 0, 90f) * go.transform.rotation;
            if (yaw.HasValue) go.transform.rotation = Quaternion.Euler(0, yaw.Value, 0) * go.transform.rotation;
            StandOn(go, floorPoint);
        }
        var b = Bounds(go);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = Vector3.Max(b.size, Vector3.one * 0.25f);    // at least 25 cm, so it's easy to look at
        var s = go.transform.lossyScale;
        col.size = new Vector3(size.x / Mathf.Abs(s.x), size.y / Mathf.Abs(s.y), size.z / Mathf.Abs(s.z));
        var pickup = go.AddComponent<global::Pickup>();
        pickup.kind = kind;
        pickup.pickupSound = Load<AudioClip>(SfxDir + "/" + soundName + ".wav");
    }

    static IEnumerable<Vector3> Corners(Bounds b)
    {
        for (int i = 0; i < 8; i++)
            yield return new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z);
    }
}
