using UnityEngine;

// Station 1's way out: the east train (an open car like the west one, its open end facing the platform)
// with a small driver's cab, the controls (TrainConsole) and a note; the staff room desk drawer that
// holds the spare reverser key; and the key itself. Placeholder controls and drawer are simple shapes.
public static partial class LookTestBuilder
{
    // ---------------------------------------------------------------- the departure train (east tunnel)
    static void BuildDepartureTrain(float tunnelEnd, Material crate)
    {
        var tunnel = root.Find("Tunnel_E");
        var car = Place("Assets/Art/Models/Environment/MetroPSX/SubwayCarOpen.fbx", tunnel, Vector3.zero, 0f);
        car.name = "DepartureTrain";
        foreach (var r in car.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = PropMat(mats[i].name);
            r.sharedMaterials = mats;
        }
        var b = Bounds(car);
        if (b.size.z > b.size.x) car.transform.rotation = Quaternion.Euler(0, 90f, 0) * car.transform.rotation;
        car.transform.rotation = Quaternion.Euler(0, 180f, 0) * car.transform.rotation;   // open end toward the platform (west)
        b = Bounds(car);
        car.transform.localScale *= CarLength / b.size.x;
        b = Bounds(car);
        float centreX = tunnelEnd - 1f - b.size.x * 0.5f;
        StandOn(car, new Vector3(centreX, -1.3f, 22.75f));
        b = Bounds(car);
        float faceX = b.min.x;                                        // the open end, facing the player
        Physics.SyncTransforms();

        float floorY = -0.25f;
        if (Physics.Raycast(new Vector3(faceX + 3f, b.min.y + b.size.y * 0.6f, 22.75f), Vector3.down, out var fh, 4f)) floorY = fh.point.y;
        float wallN = b.min.z + 0.13f, wallS = b.max.z - 0.13f;
        const float cabDepth = 2.6f;                                  // the cab: the first 2.6 m inside the door

        // Pieces made in world space under the tunnel, then moved onto the car so they ride along with it.
        var onCar = new System.Collections.Generic.List<Transform>();
        // The cab's back wall: the carriage's own end wall with a closed door (LookTestBuilder.TrainCab.cs).
        onCar.Add(CabPartition(tunnel, car, faceX + cabDepth, floorY, -1));
        foreach (var w in new[] { wallN - 0.05f, wallS + 0.05f })
        {
            var inner = new GameObject("CabInnerWall").AddComponent<BoxCollider>();   // the thin car sides don't stop the player
            inner.transform.SetParent(tunnel, true);
            inner.transform.position = new Vector3(faceX + cabDepth * 0.5f, floorY + 1.2f, w);
            inner.size = new Vector3(cabDepth, 2.4f, 0.1f);
            onCar.Add(inner.transform);
        }

        // The driver's controls against the north wall: a grey desk with a lever (the dead man's handle).
        var console = new GameObject("TrainConsole").transform;
        console.SetParent(tunnel, true);
        float cx = faceX + 1.4f;
        var desk = Mat("ConsoleGrey", null, new Color(0.3f, 0.32f, 0.35f), 1f, 0.35f, 0.3f);
        Span("Desk", console, new Vector3(cx - 0.45f, floorY, wallN), new Vector3(cx + 0.45f, floorY + 1.0f, wallN + 0.45f), desk);
        var lever = Prim(PrimitiveType.Cube, "Handle", console, new Vector3(cx, floorY + 1.12f, wallN + 0.28f), new Vector3(0.05f, 0.22f, 0.05f), Mat("HandleBlack", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.5f));
        lever.transform.rotation = Quaternion.Euler(-20f, 0, 0);
        var knob = Prim(PrimitiveType.Sphere, "Knob", console, new Vector3(cx, floorY + 1.23f, wallN + 0.31f), Vector3.one * 0.07f, Mat("KnobRed", null, new Color(0.62f, 0.1f, 0.08f), 1f, 0.5f));
        Object.DestroyImmediate(knob.GetComponent<Collider>());
        var slot = Prim(PrimitiveType.Cube, "KeySlot", console, new Vector3(cx - 0.25f, floorY + 1.005f, wallN + 0.25f), new Vector3(0.08f, 0.01f, 0.05f), Mat("SlotDark", null, new Color(0.05f, 0.05f, 0.05f), 1f, 0.2f));
        Object.DestroyImmediate(slot.GetComponent<Collider>());
        onCar.Add(console);

        // A note taped to the wall above the controls.
        var note = Prim(PrimitiveType.Quad, "CabNote", tunnel, new Vector3(cx + 0.1f, floorY + 1.6f, wallN + 0.015f), new Vector3(0.21f, 0.27f, 1f), Mat("NotePaper", null, new Color(0.9f, 0.88f, 0.8f), 1f, 0.1f));
        note.transform.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, 3f);   // faces into the cab
        Object.DestroyImmediate(note.GetComponent<Collider>());
        note.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
        note.AddComponent<ReadableNote>().text = "CAR 4471 - DO NOT MOVE\nReverser key went home with the day driver again.\nSpare is in the staff room, desk drawer.\n- Night supervisor";
        onCar.Add(note.transform);

        // The same faint, flickering cab light, steadying when the train wakes.
        var cab = new GameObject("CabLight");
        cab.transform.SetParent(tunnel, true);
        cab.transform.position = new Vector3(faceX + 1.2f, floorY + 2f, 22.75f);
        var light = cab.AddComponent<Light>();
        light.type = LightType.Point; light.color = new Color(1f, 0.55f, 0.3f);
        light.intensity = 2.5f; light.range = 6f; light.shadows = LightShadows.Soft;
        var flicker = cab.AddComponent<FlickeringLight>();
        flicker.lamp = light; flicker.minCalm = 3f; flicker.maxCalm = 9f;
        onCar.Add(cab.transform);

        foreach (var t in onCar) t.SetParent(car.transform, true);

        // A crate pushed against the door to climb in by (two short hops, as at the west train).
        var climb = new GameObject("CabClimbCrate").transform;
        climb.SetParent(tunnel);
        var step = Place("Assets/Art/Models/Props/Crates/WideCrate.fbx", climb, Vector3.zero, 0f);
        var sb = Bounds(step);
        if (sb.size.y > sb.size.z) step.transform.rotation = Quaternion.Euler(90f, 0, 0) * step.transform.rotation;
        sb = Bounds(step);
        step.transform.localScale *= 0.57f / sb.size.y;
        sb = Bounds(step);
        if (sb.size.x < sb.size.z) { step.transform.rotation = Quaternion.Euler(0, 90f, 0) * step.transform.rotation; sb = Bounds(step); }
        StandOn(step, new Vector3(faceX - sb.size.x * 0.5f - 0.02f, -1.3f, 22.75f));

        // Invisible walls in the gaps beside the car, so the only way on is through the door.
        foreach (var gap in new[] { (20f, b.min.z), (b.max.z, 25.5f) })
        {
            var g = new GameObject("TrainSideBlocker").AddComponent<BoxCollider>();
            g.transform.SetParent(tunnel, true);
            g.transform.position = new Vector3(faceX - 0.1f, 0.85f, (gap.Item1 + gap.Item2) * 0.5f);
            g.size = new Vector3(0.2f, 4.3f, Mathf.Max(0.05f, gap.Item2 - gap.Item1));
        }

        // (The cab is too small for Frankie to walk into; in the finale he lunges in from the crate.)

        // The engine and the departure itself.
        var engine = car.AddComponent<AudioSource>();
        engine.clip = Load<AudioClip>("Assets/Art/Audio/Ambience/HeltonYan/DSGNDron_Noise-Empty Engine Room_HY_SD.wav");
        engine.loop = true; engine.playOnAwake = false; engine.spatialBlend = 1f;
        engine.rolloffMode = AudioRolloffMode.Linear; engine.minDistance = 3f; engine.maxDistance = 45f;
        var departure = car.AddComponent<TrainDeparture>();
        departure.cabLight = light;
        departure.cabFlicker = flicker;
        departure.engine = engine;
        departure.startClunk = Load<AudioClip>(SfxDir + "/GateWoodChain1.wav");
        departure.screech = Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav");
        var controls = console.gameObject.AddComponent<TrainConsole>();
        controls.grid = grid;
        controls.departure = departure;
        controls.lockedSound = Load<AudioClip>(SfxDir + "/SwitchButton3_Off.wav");
    }

    // ---------------------------------------------------------------- the key drawer (staff room)
    // A small steel drawer unit beside the desk; the spare reverser key lies in its drawer.
    static void BuildKeyDrawer(Transform room, float floorY, float deskMaxX)
    {
        var steel = Mat("DrawerSteel", Tex("metal_04.png"), new Color(0.46f, 0.5f, 0.52f), 1f, 0.4f, 0.5f);
        var unit = new GameObject("DrawerUnit").transform;
        unit.SetParent(room);
        float x0 = deskMaxX + 0.08f, x1 = x0 + 0.5f, z0 = 29.7f, z1 = 30.25f, top = floorY + 0.7f, t = 0.03f;
        float slotBottom = top - 0.2f;                                  // the drawer fills the top 17 cm
        Span("Left", unit, new Vector3(x0, floorY, z0), new Vector3(x0 + t, top, z1), steel);
        Span("Right", unit, new Vector3(x1 - t, floorY, z0), new Vector3(x1, top, z1), steel);
        Span("Back", unit, new Vector3(x0 + t, floorY, z1 - t), new Vector3(x1 - t, top, z1), steel);
        Span("Top", unit, new Vector3(x0 + t, top - t, z0), new Vector3(x1 - t, top, z1 - t), steel);
        Span("LowerFront", unit, new Vector3(x0 + t, floorY, z0), new Vector3(x1 - t, slotBottom, z0 + t), steel);
        Span("Shelf", unit, new Vector3(x0 + t, slotBottom - t, z0 + t), new Vector3(x1 - t, slotBottom, z1 - t), steel);

        var drawer = new GameObject("Drawer").transform;
        drawer.SetParent(unit);
        float dx0 = x0 + t + 0.005f, dx1 = x1 - t - 0.005f, dy0 = slotBottom + 0.005f, dy1 = top - t - 0.005f;
        Span("Front", drawer, new Vector3(dx0, dy0, z0 - 0.01f), new Vector3(dx1, dy1, z0 + 0.01f), steel);
        Span("TrayBottom", drawer, new Vector3(dx0, dy0, z0 + 0.01f), new Vector3(dx1, dy0 + 0.01f, z1 - t - 0.02f), steel);
        Span("TraySideL", drawer, new Vector3(dx0, dy0 + 0.01f, z0 + 0.01f), new Vector3(dx0 + 0.01f, dy0 + 0.08f, z1 - t - 0.02f), steel);
        Span("TraySideR", drawer, new Vector3(dx1 - 0.01f, dy0 + 0.01f, z0 + 0.01f), new Vector3(dx1, dy0 + 0.08f, z1 - t - 0.02f), steel);
        var handle = Prim(PrimitiveType.Cube, "Handle", drawer, new Vector3((x0 + x1) * 0.5f, (dy0 + dy1) * 0.5f, z0 - 0.025f), new Vector3(0.14f, 0.02f, 0.02f), Mat("HandleBlack", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.5f));
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        var slide = drawer.gameObject.AddComponent<SlidingDrawer>();
        slide.slide = new Vector3(0f, 0f, -0.35f);                     // out toward the room
        slide.openSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");

        // The key, lying in the tray. Its pickup box is shrunk to the key itself, so it can't be taken
        // through the closed drawer.
        var key = ReverserKeyModel(drawer);
        MakePickup(key, global::Pickup.Kind.ReverserKey, new Vector3((x0 + x1) * 0.5f, dy0 + 0.01f, (z0 + z1) * 0.5f - 0.05f), 15f, "GateWoodChain1");
        var kb = Bounds(key);
        var box = key.GetComponent<BoxCollider>();
        var s = key.transform.lossyScale;
        box.center = key.transform.InverseTransformPoint(kb.center);
        box.size = new Vector3(kb.size.x / s.x, Mathf.Min(kb.size.y, 0.06f) / s.y, kb.size.z / s.z);
    }

    // A train driver's reverser key: a steel shaft with a T handle and a red tag (simple shapes).
    static GameObject ReverserKeyModel(Transform parent)
    {
        var key = new GameObject("ReverserKey");
        key.transform.SetParent(parent);
        var metal = Mat("KeySteel", null, new Color(0.62f, 0.64f, 0.66f), 1f, 0.7f, 0.9f);
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.transform.SetParent(key.transform, false);
        shaft.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        shaft.transform.localScale = new Vector3(0.014f, 0.06f, 0.014f);   // 12 cm long, lying along X
        shaft.GetComponent<Renderer>().sharedMaterial = metal;
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.transform.SetParent(key.transform, false);
        handle.transform.localPosition = new Vector3(-0.065f, 0, 0);
        handle.transform.localScale = new Vector3(0.018f, 0.018f, 0.075f);
        handle.GetComponent<Renderer>().sharedMaterial = Mat("HandleBlack", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.5f);
        var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bit.transform.SetParent(key.transform, false);
        bit.transform.localPosition = new Vector3(0.055f, 0, 0.008f);
        bit.transform.localScale = new Vector3(0.015f, 0.01f, 0.012f);
        bit.GetComponent<Renderer>().sharedMaterial = metal;
        var tag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tag.transform.SetParent(key.transform, false);
        tag.transform.localPosition = new Vector3(-0.1f, 0, 0.015f);
        tag.transform.localScale = new Vector3(0.04f, 0.003f, 0.028f);
        tag.GetComponent<Renderer>().sharedMaterial = Mat("TagRed", null, new Color(0.62f, 0.1f, 0.08f), 1f, 0.3f);
        foreach (var col in key.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        key.transform.localScale = Vector3.one * 1.3f;
        return key;
    }
}
