using System.Collections.Generic;
using UnityEngine;

// The sewers' water (the mechanics: FloodZone, WaterCurrent, SewerValve, RiverExit, PlayerSwim, BreathHold).
// Five flood zones, kept apart by rusty steel flood barriers across the tunnels (1.35 m high: vaultable,
// and holding back water up to 1.3 m over the walkways):
//   arrival line + valve 1 room: knee-deep          -> drained by valve 1
//   valve 2 line + the side room: waist-deep        -> drained by valve 2
//   west hall, NW room, valve 3's sunken sump: deep (the sump is under water to its ceiling) -> valve 3
//   south junction, valve 4, outfall: deep, fast currents in the channels toward the river -> valve 4
//   the cistern: deep, a whirlpool round the central platform; a step lower with each valve, dry after
//   four; the final valve purges it and opens the river grate.
// Heights are relative to the walkways (the tunnels' channels rest 0.45 m below them).
public static partial class LookTestBuilder
{
    static readonly Dictionary<string, GameObject> valveWheels = new Dictionary<string, GameObject>();
    const float CisternStart = 1.3f, CisternStep = 0.575f, CisternPurged = -5.5f;   // four steps: 1.3 -> -1.0

    static void Flooding(Transform parent, Transform grate)
    {
        var g = new GameObject("Flooding").transform;
        g.SetParent(parent);
        Vector3 o = SewerOrigin;
        // (x0, x1, z0, z1, rest): boxes that tile each zone without overlapping (so their see-through water
        // never doubles up); 'rest' is where each box's water settles once drained (channels -0.45; under a
        // room's floor; the cistern's pool -1).
        const float ch = -0.45f;
        var n1 = Zone(g, "Arrival", 0.45f, new[] { (-3.6f, 3.6f, 0f, 12.5f, ch), (-30f, 3.6f, 12.5f, 20.4f, ch), (-40.9f, -30f, 11f, 21f, ch) });
        var n2 = Zone(g, "ValveTwo", 1.0f, new[] { (-81f, -41.15f, 12.6f, 19.4f, ch), (-59.4f, -50.6f, 19.4f, 20.9f, ch), (-91f, -81f, 11f, 21f, ch),
                                                    (-89.6f, -82.4f, 21f, 61.4f, ch), (-105f, -89.6f, 40f, 50f, ch), (-82.4f, -77.15f, 53.7f, 60.3f, ch) });
        var cistern = Zone(g, "Cistern", CisternStart, new[] { (-58.6f, -51.4f, 21.15f, 36.8f, ch), (-75.4f, -34.6f, 36.8f, 77.2f, -1f), (-76.9f, -75.4f, 53.7f, 60.3f, ch),
                                                                (-34.6f, -21.15f, 53.7f, 60.3f, ch), (-58.6f, -51.4f, 77.2f, 82.4f, ch) });
        var west = Zone(g, "West", 1.3f, new[] { (-20.9f, -19f, 53.7f, 60.3f, ch), (-19f, -5f, 50f, 64f, ch), (-15.6f, -8.4f, 35f, 50f, ch), (-17f, -7f, 25f, 35f, ch),
                                                 (-15.6f, -8.4f, 64f, 77f, ch), (-16.4f, -7.6f, 77f, 85.4f, ch), (-23f, -16.4f, 77.7f, 84.3f, ch), (-33f, -23f, 76f, 86f, -2.6f) });
        var south = Zone(g, "South", 1.3f, new[] { (-59.4f, -50.6f, 82.65f, 92.4f, ch), (-77f, -59.4f, 84.6f, 91.4f, ch), (-87f, -77f, 83f, 93f, ch),
                                                   (-58.6f, -51.4f, 92.4f, 101f, ch), (-63f, -47f, 101f, 117f, ch), (-95f, -63f, 105.6f, 112.4f, ch) });

        // Flood barriers where the zones meet (x0, x1, z0, z1): rusty steel plates, the channel's bottom to 1.35 m.
        var barrierSteel = Mat("FloodBarrier", Tex2(SewerTex + "Metal.jpg"), new Color(0.5f, 0.38f, 0.3f), 1f, 0.3f, 0.6f);
        foreach (var (x0, x1, z0, z1) in new[] { (-41.15f, -40.9f, 12.4f, 19.6f), (-58.8f, -51.2f, 20.9f, 21.15f), (-77.15f, -76.9f, 53.4f, 60.6f),
                                                 (-21.15f, -20.9f, 53.4f, 60.6f), (-58.8f, -51.2f, 82.4f, 82.65f) })
            Span("FloodBarrier", g, o + new Vector3(x0, -SewerWalkwayRise - 0.05f, z0), o + new Vector3(x1, 1.35f, z1), barrierSteel);

        // Currents toward the river in the south: down the outfall tunnel's channel, across the outfall chamber,
        // and fastest in the overflow tunnel to the grate. A whirlpool round the cistern's platform while it's deep.
        foreach (var (x0, x1, z0, z1, flow, down) in new[] { (-56.3f, -53.7f, 92.4f, 101f, new Vector3(0f, 0f, 1.5f), 0.6f), (-63f, -53f, 107f, 111.5f, new Vector3(-1.3f, 0f, 0f), 0.5f),
                                                             (-95f, -63f, 107.8f, 110.2f, new Vector3(-1.7f, 0f, 0f), 0.7f) })
        {
            var current = new GameObject("Current").AddComponent<WaterCurrent>();
            current.transform.SetParent(g);
            current.area = new Bounds(o + new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 20f, z1 - z0));
            current.flow = flow; current.down = down;
            current.zone = south; current.activeAbove = o.y;   // only while the water is over the walkways
        }
        var whirl = new GameObject("CisternWhirlpool").AddComponent<WaterCurrent>();
        whirl.transform.SetParent(g);
        whirl.transform.position = o + CisternCentre;
        whirl.radius = 9f; whirl.pull = 1.3f; whirl.down = 0.9f;
        whirl.zone = cistern; whirl.activeAbove = o.y - 0.3f;

        // The valves.
        var squealClip = Load<AudioClip>(SfxDir + "/OldDoorCreak.wav");
        var clunk = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        var groan = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");
        var exit = grate.gameObject.AddComponent<RiverExit>();
        exit.grate = grate;
        exit.exitArea = new Bounds(o + new Vector3(-93.5f, 1f, 109f), new Vector3(3f, 4f, 4.4f));
        exit.grind = squealClip;
        exit.endCard = Object.FindAnyObjectByType<EndCard>(FindObjectsInactive.Include);
        foreach (var (name, zone, drainTo) in new[] { ("Valve1", n1, ch), ("Valve2", n2, ch), ("Valve3", west, -2.6f), ("Valve4", south, ch), ("FinalValve", (FloodZone)null, 0f) })
        {
            var wheel = valveWheels[name];
            var valve = wheel.AddComponent<SewerValve>();
            valve.id = name; valve.wheel = wheel.transform;
            valve.zone = zone; valve.drainTo = o.y + drainTo;
            valve.cistern = cistern; valve.cisternStart = o.y + CisternStart; valve.cisternStep = CisternStep; valve.cisternPurged = o.y + CisternPurged;
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

    // A flood zone at 'start' over its boxes, each with its own water surface.
    static FloodZone Zone(Transform g, string id, float start, (float x0, float x1, float z0, float z1, float rest)[] boxes)
    {
        Vector3 o = SewerOrigin;
        var zone = new GameObject("Flood_" + id).AddComponent<FloodZone>();
        zone.transform.SetParent(g);
        zone.id = id;
        zone.level = o.y + start;
        zone.areas = new Bounds[boxes.Length];
        zone.surfaces = new Transform[boxes.Length];
        zone.surfaceRest = new float[boxes.Length];
        for (int i = 0; i < boxes.Length; i++)
        {
            var (x0, x1, z0, z1, rest) = boxes[i];
            zone.areas[i] = new Bounds(o + new Vector3((x0 + x1) * 0.5f, -1f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 12f, z1 - z0));
            var water = Prim(PrimitiveType.Quad, "Water", zone.transform, o + new Vector3((x0 + x1) * 0.5f, start, (z0 + z1) * 0.5f), new Vector3(x1 - x0, z1 - z0, 1f), sewerWater);
            water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // facing up
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.AddComponent<WaterSurface>().size = new Vector2(x1 - x0, z1 - z0);   // deep enough, it breaks a fall
            zone.surfaces[i] = water.transform;
            zone.surfaceRest[i] = o.y + rest;
        }
        // The draining roar: the waterfall loop slowed right down, heard everywhere (it's the whole sewer).
        zone.roar = zone.gameObject.AddComponent<AudioSource>();
        zone.roar.clip = Load<AudioClip>(SfxDir + "/FastWaterfallStreamLoop.wav");
        zone.roar.loop = true; zone.roar.playOnAwake = false; zone.roar.pitch = 0.5f; zone.roar.spatialBlend = 0f; zone.roar.volume = 0f;
        return zone;
    }
}
