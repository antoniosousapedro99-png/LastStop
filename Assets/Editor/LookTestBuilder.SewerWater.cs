using System.Collections.Generic;
using Fusion.Fluid;
using UnityEngine;

// The sewers' water, and what the valves do (the mechanics: FloodZone, SluiceGate, SewerValve, WaterCurrent,
// RiverExit, PlayerSwim, BreathHold, Waterfall, RubbishDrift, and FusionWater's floating).
// Every tunnel's canal is 2 m of water, over your head, flowing gently toward the cistern (out to the river
// past the pump room), on under the rooms in culverts, and over the cistern's edge in four waterfalls.
// Why it's flooded: the main outfall gate to the river is shut, so the cistern (a storm tank) and every low
// place have backed up. The water stands only where the floor is lowest, so nothing has to hold it back:
//   the cistern's tank: up to just below its doors (deep: the walkway ring and catwalks are under it), with
//     a whirlpool round the central platform while it's deep;
//   each valve room's sunken pit: valve 1 knee-deep, valves 2 and 4 deep, valve 3's inner chamber full;
//   the outfall chamber's spillway: deep and fast, always.
// Each valve room's valve lifts its relief sluice gate: the cistern's water roars out of the relief pipe
// and down the drain shaft beside it, the tank drops a step, and the pit drains with it. After four, the
// tank's walkway and catwalks are clear; the final valve on the platform opens the main outfall gate,
// purging the rest to the river, and the river grate lifts. Heights are relative to the tunnels' walkways.
public static partial class LookTestBuilder
{
    static readonly Dictionary<string, GameObject> valveWheels = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, SluiceGate> reliefGates = new Dictionary<string, SluiceGate>();
    // The cistern: 6 m down at the start; each of the four valves lowers it a quarter of the way to an ankle-deep
    // puddle on its floor (about 6.5 m a time), and the final valve purges the rest.
    const float CisternStart = -6f, CisternPuddle = CisternFloor + 0.15f, CisternPurged = CisternFloor - 0.4f;
    const float CisternStep = (CisternStart - CisternPuddle) / 4f;
    const float DrainSeconds = 20f;

    // Still water in the low places (the cistern, valve 2's pit, the siphon, the spillway), the canals, and the
    // valves: each valve room's valve lifts its relief gate and lowers the cistern a step (valve 2 drains its
    // pit, valve 3 the siphon); the final valve on the cistern's floor purges it and opens the river grate.
    static void Flooding(Transform parent, Transform grate)
    {
        var g = new GameObject("Flooding").transform;
        g.SetParent(parent);
        Vector3 o = SewerOrigin;
        // The cistern starts 6 m down, so the canals pour a long way into it; the valves lower it further.
        var cistern = Zone(g, "Cistern", CisternStart, CisternCentre.x - CisternRadius, CisternCentre.x + CisternRadius, CisternCentre.z - CisternRadius, CisternCentre.z + CisternRadius, CisternPurged, true);
        var siphon = Zone(g, "Siphon", -0.4f, -3f, 3f, 80f, 98f, -6.9f);   // drains to below its shafts' floors
        var pit2 = Zone(g, "Pit2", -0.4f, 71f, 89f, 21f, 39f, -2.4f);
        var spillway = Zone(g, "Spillway", -0.6f, 43f, 67f, 129f, 132f, -0.6f);
        // The spillway drags a swimmer along to the culvert grate and under.
        var current = new GameObject("SpillwayCurrent").AddComponent<WaterCurrent>();
        current.transform.SetParent(g);
        current.area = new Bounds(o + new Vector3(55f, 0f, 130.5f), new Vector3(24f, 20f, 3f));
        current.flow = new Vector3(1.8f, 0f, 0f); current.down = 0.8f;
        current.zone = spillway;

        Canals(g, cistern);

        // Each valve room's relief gate, on a wall clear of its doorways and its valve.
        foreach (var (name, wallFloor, inward) in new[] { ("Gate1", new Vector3(-7f, 0f, 25f), Vector3.right), ("Gate2", new Vector3(89f, -2.2f, 24.5f), Vector3.left),
                                                         ("Gate3", new Vector3(0f, 0f, 119f), Vector3.back), ("Gate4", new Vector3(80f, 0f, 117f), Vector3.back) })
        {
            ClearPillars(o + wallFloor + inward * 0.2f + Vector3.up * 1.5f, 1.3f);
            ReliefGate(g, name, o + wallFloor, inward);
        }

        // The valves, and the way out they lead to.
        var squealClip = Load<AudioClip>(SfxDir + "/OldDoorCreak.wav");
        var clunk = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        var groan = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");
        var exit = grate.gameObject.AddComponent<RiverExit>();
        exit.grate = grate;
        exit.lift = SewerWalkwayRise + 2.4f;   // its bars' feet come up clear of your head
        exit.exitArea = new Bounds(o + new Vector3(98.6f, 1f, 137f), new Vector3(2f, 4f, 8.6f));
        exit.grind = squealClip;
        exit.endCard = Object.FindAnyObjectByType<EndCard>(FindObjectsInactive.Include);
        foreach (var (name, gate, zone) in new[] { ("Valve1", "Gate1", (FloodZone)null), ("Valve2", "Gate2", pit2), ("Valve3", "Gate3", siphon),
                                                   ("Valve4", "Gate4", (FloodZone)null), ("FinalValve", null, (FloodZone)null) })
        {
            var wheel = valveWheels[name];
            var valve = wheel.AddComponent<SewerValve>();
            valve.id = name; valve.wheel = wheel.transform;
            valve.zone = zone; valve.drainTo = zone ? zone.surfaceRest[0] : 0f;
            valve.gate = gate != null ? reliefGates[gate] : null;
            valve.cistern = cistern; valve.cisternStart = o.y + CisternStart; valve.cisternStep = CisternStep; valve.cisternPurged = o.y + CisternPurged;
            valve.drainSeconds = DrainSeconds;
            valve.final = name == "FinalValve";
            if (valve.final) valve.exit = exit;
            valve.squeal = wheel.AddComponent<AudioSource>();
            valve.squeal.clip = squealClip; valve.squeal.loop = true; valve.squeal.playOnAwake = false; valve.squeal.pitch = 0.55f;
            valve.squeal.spatialBlend = 1f; valve.squeal.minDistance = 2f; valve.squeal.maxDistance = 25f; valve.squeal.volume = 0.8f;
            valve.clunk = clunk; valve.groan = groan;
        }

        // The player: swimming, and holding his breath.
        var player = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include).gameObject;
        var splashes = new[] { Load<AudioClip>(SfxDir + "/WaterSplash1.wav"), Load<AudioClip>(SfxDir + "/WaterSplash2.wav") };
        var swim = player.AddComponent<PlayerSwim>();
        swim.source = player.AddComponent<AudioSource>();
        swim.source.spatialBlend = 0f; swim.source.playOnAwake = false;
        swim.strokes = splashes;
        var breath = player.AddComponent<BreathHold>();
        breath.eyes = player.transform.Find("Main Camera");
        breath.health = player.GetComponent<PlayerHealth>();
        breath.voice = player.AddComponent<AudioSource>();
        breath.voice.spatialBlend = 0f; breath.voice.playOnAwake = false;
        breath.heartbeat = Load<AudioClip>(CharacterSfx + "/PlayerHeartbeat.ogg");
        breath.bubbles = new[] { Load<AudioClip>(SfxDir + "/FoodPackagingWaterBottle1.wav"), Load<AudioClip>(SfxDir + "/FoodPackagingWaterBottle2.wav"), Load<AudioClip>(SfxDir + "/FoodPackagingWaterBottle3.wav") };
        breath.gasps = new[] { Load<AudioClip>(SfxDir + "/HumanBreathingOut1.wav"), Load<AudioClip>(SfxDir + "/HumanBreathingOut3.wav") };
        breath.cough = Load<AudioClip>(SfxDir + "/HumanCough1.wav");
    }

    // The canals: one body of water (never drained) filling every tunnel's channel, the culverts under the rooms
    // and the slot into the pump room's spillway, each stretch flowing toward the cistern (the river tunnel's
    // out to the river); the waterfalls into the cistern; floating rubbish carried along.
    static void Canals(Transform g, FloodZone cistern)
    {
        Vector3 o = SewerOrigin;
        float surface = o.y + ChannelWater;
        var zone = new GameObject("Flood_Canals").AddComponent<FloodZone>();
        zone.transform.SetParent(g);
        zone.id = "Canals";
        zone.level = surface;
        zone.surfaces = new Transform[0]; zone.surfaceRest = new float[0];   // the channel water quads never move
        var areas = new List<Bounds>();
        var fluid = new GameObject("CanalFluid");   // FusionWater: the same boxes as triggers, their tops the surface
        fluid.transform.SetParent(g);
        fluid.layer = 2;   // Ignore Raycast: the crosshair looks through it
        void Area(Bounds b, Vector3 flow)
        {
            areas.Add(b);
            var box = fluid.AddComponent<BoxCollider>();
            box.isTrigger = true; box.center = b.center; box.size = b.size;
            if (flow == Vector3.zero) return;
            var current = new GameObject("CanalCurrent").AddComponent<WaterCurrent>();
            current.transform.SetParent(zone.transform);
            current.area = b; current.flow = flow; current.down = 0.15f; current.zone = zone;
        }
        Vector3 toCistern = o + CisternCentre;
        foreach (var (a, b) in kitChannels)
        {
            Vector3 along = b - a; along.y = 0f;
            if (along.magnitude < 0.1f) continue;
            Vector3 dir = along.normalized, mid = (a + b) * 0.5f;
            bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
            float bottom = a.y;
            var box = new Bounds(new Vector3(mid.x, (bottom + surface) * 0.5f, mid.z),
                                 new Vector3(alongX ? Mathf.Abs(along.x) : ChannelHalf * 2f, surface - bottom, alongX ? ChannelHalf * 2f : Mathf.Abs(along.z)));
            Vector3 flow = Vector3.Dot(dir, toCistern - mid) >= 0f ? dir : -dir;
            if (mid.z - o.z > 130f) flow = Vector3.right;   // the overflow tunnel runs out to the river
            Area(box, flow * 0.7f);
        }
        foreach (var b in culvertAreas) Area(b, Vector3.zero);
        Area(new Bounds(o + new Vector3(55f, (ChannelWater - SewerWalkwayRise) * 0.5f, 127f), new Vector3(ChannelHalf * 2f, SewerWalkwayRise + ChannelWater, 4f)), Vector3.forward * 0.7f);   // the pump room's slot
        zone.areas = areas.ToArray();
        fluid.AddComponent<Fluid>();

        // (Rubbish that goes over the falls just drops into the cistern and turns up upstream again: RubbishDrift.)

        // The waterfalls: each canal pouring off its lip under the gallery, down to the cistern's water.
        var fallMat = FallMat();
        var sprayMat = DustMat();
        int n = 0;
        foreach (var (lip, inward) in cisternSpouts)
        {
            var root = new GameObject("CisternFall_" + n++).transform;
            root.SetParent(g);
            root.position = lip;
            var sheet = Prim(PrimitiveType.Quad, "FallSheet", root, lip + inward * 0.05f, Vector3.one, fallMat);
            sheet.transform.rotation = Quaternion.LookRotation(-inward);   // a quad shows on the side facing its -Z: into the shaft
            Object.DestroyImmediate(sheet.GetComponent<Collider>());
            var splash = new GameObject("FallSplash").transform;
            splash.SetParent(root);
            splash.position = lip + inward * 0.6f;
            var roar = splash.gameObject.AddComponent<AudioSource>();
            roar.clip = Load<AudioClip>(SfxDir + "/FastWaterfallStreamLoop.wav");
            roar.loop = true; roar.playOnAwake = true; roar.volume = 0.7f; roar.spatialBlend = 1f;
            roar.rolloffMode = AudioRolloffMode.Linear; roar.minDistance = 4f; roar.maxDistance = 35f;
            var spray = splash.gameObject.AddComponent<ParticleSystem>();
            var main = spray.main;
            main.startLifetime = 1.4f; main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f); main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.gravityModifier = 0.3f; main.maxParticles = 120; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = spray.emission; emission.rateOverTime = 50f;
            var shape = spray.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 40f; shape.radius = 1.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // spraying up off the water
            splash.GetComponent<ParticleSystemRenderer>().sharedMaterial = sprayMat;
            var fall = root.gameObject.AddComponent<Waterfall>();
            fall.zone = cistern; fall.top = lip.y; fall.width = ChannelHalf * 2f;
            fall.sheet = sheet.GetComponent<Renderer>(); fall.splash = splash;
            // Sized to the cistern's starting water already, so it shows in the Scene view too.
            sheet.transform.position = new Vector3(sheet.transform.position.x, (lip.y + cistern.level) * 0.5f, sheet.transform.position.z);
            sheet.transform.localScale = new Vector3(fall.width, lip.y - cistern.level, 1f);
            splash.position = new Vector3(splash.position.x, cistern.level + 0.05f, splash.position.z);
        }

        // Rubbish afloat in the canals, carried toward the falls (it turns up again upstream once gone over).
        var floating = new GameObject("FloatingRubbish").transform;
        floating.SetParent(g);
        var rng = new System.Random(77);
        string[] brands = { "generic_cola", "pepso", "sprit", "fantom_orange", "dr_piper", "red_bullet" };
        for (int i = 0; i < 16 && kitChannels.Count > 0; i++)
        {
            var (a, b) = kitChannels[rng.Next(kitChannels.Count)];
            if ((b - a).magnitude < 4f) { i--; continue; }
            Vector3 at = Vector3.Lerp(a, b, 0.2f + 0.6f * (float)rng.NextDouble());
            at.y = surface + 0.1f;
            GameObject item;
            if (i % 4 == 0)
            {
                item = Model(SewerKitFbx, "Garbage_bag", floating, null, 1f, (float)rng.NextDouble() * 360f, sewerKitTextures);
                item.transform.localScale *= 0.55f / Mathf.Max(0.001f, Bounds(item).size.y);
            }
            else
            {
                item = Model(ArtDir + "/Props/SodaCans/Can_Standard330.fbx", null, floating, Tex2("Props/SodaCans/can_" + brands[rng.Next(brands.Length)] + ".png"), 1f, (float)rng.NextDouble() * 360f);
                item.transform.localScale *= 0.12f / Mathf.Max(0.001f, Bounds(item).size.y);
                item.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * item.transform.rotation;   // floating on its side
            }
            item.name = "FloatingRubbish";
            item.transform.position += at - Bounds(item).center;
            var shapeBox = item.AddComponent<BoxCollider>();
            var body = item.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            var floater = item.AddComponent<BasicFluidInteractor>();
            floater.customVolume = 3f;            // with its float strength: it rides about 5 cm deep
            floater.simulateWaterTurbulence = true; floater.turbulenceStrength = 0.3f;
            item.AddComponent<RubbishDrift>();
        }
    }

    // The falling water: unlit and see-through (pale, so it reads in the dark shaft), the rippling water picture
    // on it (Waterfall scrolls it downward).
    static Material FallMat()
    {
        string path = MatDir + "/M_Waterfall.mat";
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null && mat.shader != unlit) { UnityEditor.AssetDatabase.DeleteAsset(path); mat = null; }
        if (mat == null) { mat = new Material(unlit); UnityEditor.AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", new Color(0.95f, 1f, 0.9f, 0.75f));   // the picture is mostly dark grey: lifted to read as white water
        mat.SetTexture("_BaseMap", Load<Texture2D>(FoxTexDir + "Water/WaterSurface_01.png"));
        mat.SetFloat("_Surface", 1f);   // transparent, alpha blended
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        UnityEditor.EditorUtility.SetDirty(mat);
        return mat;
    }

    // A body of water at 'start' over x0..x1, z0..z1, settling at 'rest' once drained; with 'roars', a deep
    // draining roar from its middle while its level drops (the cistern).
    static FloodZone Zone(Transform g, string id, float start, float x0, float x1, float z0, float z1, float rest, bool roars = false)
    {
        Vector3 o = SewerOrigin, mid = o + new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
        var zone = new GameObject("Flood_" + id).AddComponent<FloodZone>();
        zone.transform.SetParent(g);
        zone.transform.position = mid;
        zone.id = id;
        zone.level = o.y + start;
        float low = o.y + Mathf.Min(start, rest) - 2f, high = o.y + 12f;   // from below its lowest water to 12 m up
        zone.areas = new[] { new Bounds(new Vector3(mid.x, (low + high) * 0.5f, mid.z), new Vector3(x1 - x0, high - low, z1 - z0)) };
        var water = Prim(PrimitiveType.Quad, "Water", zone.transform, mid + Vector3.up * start, new Vector3(x1 - x0, z1 - z0, 1f), sewerWater);
        water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // facing up
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.AddComponent<WaterSurface>().size = new Vector2(x1 - x0, z1 - z0);   // deep enough, it breaks a fall
        zone.surfaces = new[] { water.transform };
        zone.surfaceRest = new[] { o.y + rest };
        if (roars)
        {
            zone.roar = zone.gameObject.AddComponent<AudioSource>();
            zone.roar.clip = Load<AudioClip>(SfxDir + "/FastWaterfallStreamLoop.wav");
            zone.roar.loop = true; zone.roar.playOnAwake = false; zone.roar.pitch = 0.5f; zone.roar.volume = 0f;
            zone.roar.spatialBlend = 1f; zone.roar.rolloffMode = AudioRolloffMode.Linear; zone.roar.minDistance = 10f; zone.roar.maxDistance = 60f;
        }
        return zone;
    }

    // A relief gate on a valve room's wall: a big rusty pipe mouth (the relief line from the cistern) behind
    // a steel gate in guide rails, a grated drain shaft in the floor below it, and the falling water shown
    // while it pours. 'wallFloor' is on the floor at the wall, 'inward' points into the room.
    static void ReliefGate(Transform g, string name, Vector3 wallFloor, Vector3 inward)
    {
        var root = new GameObject(name).transform;
        root.SetParent(g);
        Vector3 across = Vector3.Cross(Vector3.up, inward);
        Vector3 mouth = wallFloor + Vector3.up * 2f;
        root.position = mouth;   // the roar comes from the pipe (the parts below are placed in world space)
        Pipe(root, name + "_PipeMouth", mouth, mouth + inward * 0.5f, 0.55f, pipeRust);
        foreach (float s in new[] { -0.75f, 0.75f })   // guide rails up the wall
            Turned(Box(name + "_Guide", root, wallFloor + inward * 0.6f + across * s + Vector3.up * 2.6f, new Vector3(0.12f, 2.6f, 0.12f), pipeSteel), Quaternion.LookRotation(inward));
        var plate = Turned(Box(name + "_Gate", root, mouth + inward * 0.6f, new Vector3(1.4f, 1.4f, 0.08f), Mat("FloodBarrier", Tex2(SewerTex + "Metal.jpg"), new Color(0.5f, 0.38f, 0.3f), 1f, 0.3f, 0.6f)), Quaternion.LookRotation(inward));
        Turned(Box(name + "_Shaft", root, wallFloor + inward * 1.4f + Vector3.up * 0.03f, new Vector3(1.8f, 0.06f, 1.6f), sewerGrate), Quaternion.LookRotation(inward));   // the drain shaft's grating
        var torrent = Prim(PrimitiveType.Quad, name + "_Torrent", root, mouth + inward * 0.75f + Vector3.down * 0.95f, new Vector3(0.9f, 2f, 1f),
                           TransparentMat("Torrent", new Color(0.45f, 0.5f, 0.45f, 0.55f), 0.9f));
        torrent.transform.rotation = Quaternion.LookRotation(-inward);   // a quad shows on the side facing its -Z: into the room
        Object.DestroyImmediate(torrent.GetComponent<Collider>());
        torrent.SetActive(false);
        var gate = root.gameObject.AddComponent<SluiceGate>();
        gate.gate = plate.transform;
        gate.torrent = torrent;
        gate.roar = root.gameObject.AddComponent<AudioSource>();
        gate.roar.clip = Load<AudioClip>(SfxDir + "/FastWaterfallStreamLoop.wav");
        gate.roar.loop = true; gate.roar.playOnAwake = false; gate.roar.pitch = 0.8f;
        gate.roar.spatialBlend = 1f; gate.roar.rolloffMode = AudioRolloffMode.Linear; gate.roar.minDistance = 3f; gate.roar.maxDistance = 35f;
        reliefGates[name] = gate;
    }
}
