using UnityEngine;

// The sewers' power (stage 3): the main breaker in the electrical room (its handle lies on a crate in Valve 1's
// room), the sewers' own PowerGrid, sodium floodlights in the cistern and the rooms (dark until the breaker
// is thrown; the tunnels' weak caged bulbs and the red emergency lamps stay on), the motorised flood gate
// across Valve 4's north door, and the barred gate in its west door, bolted on the room side (the shortcut back).
public static partial class LookTestBuilder
{
    static void SewerPower(Transform parent)
    {
        Vector3 o = SewerOrigin;
        var g = new GameObject("Power").transform;
        g.SetParent(parent);

        // ---- the grid: off until the breaker is thrown
        var grid = new GameObject("SewerPowerGrid").AddComponent<PowerGrid>();
        grid.transform.SetParent(g);
        grid.ownsUnassignedLamps = false;
        grid.clunk = Load<AudioClip>(SfxDir + "/Hammer2.wav");
        grid.musicSting = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Powerbox.ogg");
        grid.delayBetweenLights = 0.25f;
        var hum = grid.gameObject.AddComponent<AudioSource>();
        hum.clip = Load<AudioClip>(AmbienceDir + "/DSGNDron_Noise-Empty Engine Room_HY_SD.wav");
        hum.loop = true; hum.playOnAwake = false; hum.spatialBlend = 0f;
        grid.hum = hum; grid.humVolume = 0.15f;

        // ---- the rooms' own bulbs now run on the grid (the tunnels', the arrival's and the control room's don't)
        foreach (string room in new[] { "ValveChamber1", "ValveChamber2", "ValveChamber3", "ValveChamber4", "ElectricalRoom" })
            OnGrid(parent.Find(room + "_Bulb"), grid);
        OnGrid(parent.Find("Cistern/CisternBulb_Bottom"), grid);

        // ---- the main breaker: a lever on the front of the breaker cabinet (SewerDressing), its handle missing
        var breaker = new GameObject("MainBreakerSwitch").AddComponent<SewerBreaker>();
        breaker.transform.SetParent(g);
        breaker.transform.position = o + new Vector3(47f, 1.2f, 0.76f);
        var face = breaker.gameObject.AddComponent<BoxCollider>();   // the crosshair finds it anywhere on the cabinet's door
        face.size = new Vector3(1.6f, 2.4f, 0.1f);
        breaker.grid = grid;
        var dark = Mat("BreakerMount", null, new Color(0.12f, 0.12f, 0.11f), 1f, 0.3f, 0.6f);
        Object.DestroyImmediate(Box("BreakerMount", breaker.transform, o + new Vector3(47.45f, 1.45f, 0.74f), new Vector3(0.22f, 0.4f, 0.06f), dark).GetComponent<Collider>());
        var lever = new GameObject("BreakerLever").transform;
        lever.SetParent(breaker.transform);
        lever.position = o + new Vector3(47.45f, 1.5f, 0.8f);
        var handle = BreakerHandleModel(lever);
        handle.transform.position = lever.position;   // its pivot end on the hinge, pointing up
        breaker.lever = lever;
        breaker.fitSound = Load<AudioClip>(SfxDir + "/SwitchButton2_On.wav");
        breaker.throwSound = Load<AudioClip>(SfxDir + "/SwitchButton3_On.wav");

        // ---- the handle, on a crate in Valve 1's room (north-west corner, out of the way of the doors)
        var crate = IndustrialPiece(g, "CrateWood", new Vector3(0.9f, 0.8f, 0.7f));
        crate.name = "HandleCrate";
        crate.transform.rotation = Quaternion.Euler(0f, 12f, 0f) * crate.transform.rotation;
        StandOn(crate, o + new Vector3(-4.6f, 0f, 35.1f));
        crate.AddComponent<BoxCollider>();
        var loose = BreakerHandleModel(g);
        loose.name = "BreakerHandle";
        MakePickup(loose, global::Pickup.Kind.BreakerHandle, o + new Vector3(-4.6f, Bounds(crate).max.y - o.y, 35.1f), 70f, "MetalCabinet1", lieDown: true);

        // ---- sodium floodlights: eight round the cistern above the gallery, aimed down across the shaft
        Vector3 c = o + CisternCentre;
        for (int i = 0; i < 8; i++)
        {
            Vector3 d = Quaternion.Euler(0f, 22.5f + i * 45f, 0f) * Vector3.forward;
            SodiumFlood(g, "CisternFlood_" + i, c + d * (CisternRadius - 0.5f) + Vector3.up * 5.5f, -d,
                        c - d * 5f + Vector3.up * (CisternFloor * 0.55f), 1500f, 60f, 70f, grid);
        }
        // ...two in each valve room (on the wall facing the valve), one in the electrical room.
        foreach (var (name, a, b, valve) in new[] {
                     ("ValveFlood1", new Vector3(6.5f, 3.9f, 25f), new Vector3(6.5f, 3.9f, 35f), new Vector3(-7f, 0f, 30f)),
                     ("ValveFlood2", new Vector3(71.5f, 3.6f, 23f), new Vector3(71.5f, 3.6f, 37f), new Vector3(89f, -2.2f, 30f)),
                     ("ValveFlood3", new Vector3(8.5f, 3.9f, 103.5f), new Vector3(8.5f, 3.9f, 116.5f), new Vector3(-9f, 0f, 110f)),
                     ("ValveFlood4", new Vector3(73.5f, 3.9f, 105f), new Vector3(73.5f, 3.9f, 115f), new Vector3(87f, 0f, 110f)) })
        {
            Vector3 normal = (valve - a).x > 0f ? Vector3.right : Vector3.left;   // out of the wall, toward the valve
            SodiumFlood(g, name + "A", o + a, normal, o + valve + Vector3.up * 0.5f, 45f, 20f, 75f, grid);
            SodiumFlood(g, name + "B", o + b, normal, o + valve + Vector3.up * 0.5f, 45f, 20f, 75f, grid);
        }
        SodiumFlood(g, "ElectricalFlood", o + new Vector3(47f, 3.2f, 9.5f), Vector3.back, o + new Vector3(47f, 0.8f, 0.5f), 35f, 14f, 80f, grid);

        FloodGate(g, grid);
        ShortcutGate(g);
    }

    // A caged room bulb (CagedBulb) made to wait for the grid: dark until the power comes back.
    static void OnGrid(Transform bulb, PowerGrid grid)
    {
        if (!bulb) { Debug.LogWarning("Sewer power: bulb not found"); return; }
        var power = bulb.gameObject.AddComponent<PoweredLight>();
        power.lights = bulb.GetComponentsInChildren<Light>();
        var glass = bulb.Find("Bulb").GetComponent<Renderer>();
        power.glowParts = new[] { glass };
        power.glowColour = glass.sharedMaterial.GetColor("_BaseColor");
        power.offColour = new Color(0.15f, 0.13f, 0.11f);
        power.grid = grid;
        power.poweredReach = 1f; power.poweredWiden = 0f;
    }

    // A big caged lamp on a wall ('wallPoint' about half a metre out from it, 'normal' out of the wall), its glass
    // glowing sodium orange, throwing a wide spot at 'aim' and a little light round itself. Warms up slowly.
    static void SodiumFlood(Transform g, string name, Vector3 wallPoint, Vector3 normal, Vector3 aim, float intensity, float range, float angle, PowerGrid grid)
    {
        Physics.SyncTransforms();
        if (Physics.Raycast(wallPoint, -normal, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore)) wallPoint = hit.point;
        var lamp = CagedLamp(g, name, Sodium * 3f);
        lamp.transform.localScale *= 1.7f;
        lamp.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
        var b = Bounds(lamp);
        float half = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z))));
        lamp.transform.position += wallPoint + normal * half - b.center;
        Vector3 at = Bounds(lamp).center + normal * (half + 0.15f);
        var spot = AddLight(lamp, LightType.Spot, Sodium, intensity, range);
        spot.transform.SetPositionAndRotation(at, Quaternion.LookRotation(aim - at));
        spot.spotAngle = angle; spot.innerSpotAngle = angle * 0.55f;
        var fill = AddLight(lamp, LightType.Point, Sodium, 3f, 6f);
        fill.transform.position = at;
        var power = lamp.AddComponent<PoweredLight>();
        power.lights = new[] { spot, fill };
        power.glowParts = lamp.GetComponentsInChildren<Renderer>();
        power.glowProperty = "_EmissionColor";
        power.glowColour = Sodium * 3f; power.offColour = Color.black;
        power.grid = grid;
        power.warmUp = 5f;
        power.poweredReach = 1f; power.poweredWiden = 0f;
    }

    // The breaker's lever handle: a steel bar with a red rubber grip, its square boss at the origin, pointing up.
    static GameObject BreakerHandleModel(Transform parent)
    {
        var handle = new GameObject("BreakerHandle");
        handle.transform.SetParent(parent);
        var steel = Mat("KeySteel", null, new Color(0.62f, 0.64f, 0.66f), 1f, 0.7f, 0.9f);
        var rubber = Mat("GripRed", null, new Color(0.55f, 0.08f, 0.06f), 1f, 0.25f);
        void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var p = GameObject.CreatePrimitive(type);
            p.transform.SetParent(handle.transform, false);
            p.transform.localPosition = pos; p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(p.GetComponent<Collider>());
        }
        Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.07f, 0.07f, 0.07f), steel);
        Part(PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.035f, 0.17f, 0.035f), steel);
        Part(PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.05f, 0.07f, 0.05f), rubber);
        return handle;
    }

    // Valve 4's north doorway (x 77..83 at z 103): a steel flood gate set in the wall, dead until the power's
    // back, then it grinds up into the wall above. It reaches down into the canal's mouth below the walkways.
    static void FloodGate(Transform g, PowerGrid grid)
    {
        Vector3 o = SewerOrigin;
        var root = new GameObject("Valve4_FloodGate").AddComponent<PoweredGate>();
        root.transform.SetParent(g);
        root.transform.position = o + new Vector3(80f, 0f, 102.85f);   // in the middle of the wall's thickness
        var gate = new GameObject("Gate").transform;
        gate.SetParent(root.transform);
        gate.position = root.transform.position;
        var steel = Mat("FloodGateSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.66f, 0.62f, 0.55f), 1f, 0.3f, 0.6f);
        Box("FloodGatePlate", gate, o + new Vector3(80f, 1.55f, 102.85f), new Vector3(6.2f, 4.3f, 0.12f), steel);
        foreach (float y in new[] { 0.8f, 1.8f, 2.8f })
            foreach (float z in new[] { -0.085f, 0.085f })
                Object.DestroyImmediate(Box("FloodGateRib", gate, o + new Vector3(80f, y, 102.85f + z), new Vector3(6.2f, 0.14f, 0.05f), steel).GetComponent<Collider>());
        // Yellow and black warning stripes along its foot, on both faces.
        var yellow = Mat("HazardYellow", null, new Color(0.8f, 0.62f, 0.12f), 1f, 0.3f);
        var black = Mat("HazardBlack", null, new Color(0.08f, 0.08f, 0.07f), 1f, 0.3f);
        for (int k = 0; k < 15; k++)
            foreach (float z in new[] { -0.071f, 0.071f })
                Object.DestroyImmediate(Box("FloodGateStripe", gate, o + new Vector3(76.9f + 0.2f + k * 0.4f, 0.28f, 102.85f + z), new Vector3(0.4f, 0.45f, 0.018f), k % 2 == 0 ? yellow : black).GetComponent<Collider>());
        var motor = gate.gameObject.AddComponent<AudioSource>();
        motor.clip = Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav");
        motor.loop = true; motor.playOnAwake = false; motor.pitch = 0.45f; motor.spatialBlend = 1f;
        motor.rolloffMode = AudioRolloffMode.Linear; motor.minDistance = 3f; motor.maxDistance = 30f;
        root.grid = grid; root.gate = gate; root.motor = motor;
        root.clank = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
    }

    // Valve 4's west doorway (z 107..113 at x 73): a barred double gate, bolted on the room's side.
    static void ShortcutGate(Transform g)
    {
        Vector3 o = SewerOrigin;
        const float x = 72.85f, height = 3.45f;
        var root = new GameObject("Valve4_BoltedGate").AddComponent<BoltedGate>();
        root.transform.SetParent(g);
        root.transform.position = o + new Vector3(x, 0f, 110f);
        var rust = pipeRust;
        Transform Leaf(string name, float hinge, float dir)
        {
            var leaf = new GameObject(name).transform;
            leaf.SetParent(root.transform);
            leaf.position = o + new Vector3(x, 0f, hinge);
            const float length = 2.95f;
            for (float s = 0.1f; s < length; s += 0.2f)
            {
                var bar = Prim(PrimitiveType.Cylinder, "Bar", leaf, o + new Vector3(x, height * 0.5f, hinge + dir * s), new Vector3(0.045f, height * 0.5f, 0.045f), rust);
                Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
            foreach (float y in new[] { 0.12f, 1.7f, height - 0.08f })
                Object.DestroyImmediate(Box("Rail", leaf, o + new Vector3(x, y, hinge + dir * length * 0.5f), new Vector3(0.07f, 0.09f, length), rust).GetComponent<Collider>());
            var block = leaf.gameObject.AddComponent<BoxCollider>();   // one solid box: bars you can see through but not squeeze between
            block.center = new Vector3(0f, height * 0.5f, dir * length * 0.5f);
            block.size = new Vector3(0.12f, height, length);
            return leaf;
        }
        root.leafA = Leaf("LeafA", 107.05f, 1f);
        root.leafB = Leaf("LeafB", 112.95f, -1f);
        root.swingA = 90f; root.swingB = -90f;   // both into the room
        var steel = Mat("KeySteel", null, new Color(0.62f, 0.64f, 0.66f), 1f, 0.7f, 0.9f);
        // The bolt on the room side, across the leaves' meeting point (it rides on leaf B and slides into it).
        var bolt = new GameObject("Bolt").transform;
        bolt.SetParent(root.leafB);
        bolt.position = o + new Vector3(x + 0.09f, 1.1f, 110f);
        Object.DestroyImmediate(Box("BoltBar", bolt, o + new Vector3(x + 0.09f, 1.1f, 110f), new Vector3(0.05f, 0.05f, 0.8f), steel).GetComponent<Collider>());
        Object.DestroyImmediate(Box("BoltKnob", bolt, o + new Vector3(x + 0.14f, 1.1f, 110.25f), new Vector3(0.08f, 0.04f, 0.04f), steel).GetComponent<Collider>());
        root.bolt = bolt;
        root.boltSlide = new Vector3(0f, 0f, 0.45f);
        root.boltSide = Vector3.right;
        root.boltSound = Load<AudioClip>(SfxDir + "/SwitchButton3_Off.wav");
        root.swingSound = Load<AudioClip>(SfxDir + "/OldDoorCreak.wav");
        root.rattleSound = Load<AudioClip>(SfxDir + "/GateWoodChain1.wav");
    }
}
