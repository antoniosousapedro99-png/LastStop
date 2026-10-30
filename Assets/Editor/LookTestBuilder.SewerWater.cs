using System.Collections.Generic;
using UnityEngine;

// The sewers' water, and what the valves do (the mechanics: FloodZone, SluiceGate, SewerValve, WaterCurrent,
// RiverExit, PlayerSwim, BreathHold).
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
    const float CisternStart = -0.5f, CisternStep = 0.75f, CisternPurged = -8.6f;   // four steps: -0.5 -> -3.5 (the walkway, at -3, is clear)

    // STAGE 1 (the layout): still water in the low places only, so the deep parts are swum, not fallen into.
    // The valves are placeholders until stage 2 wires them (relief gates, the shaft draining, the purge, the exit).
    static void Flooding(Transform parent, Transform grate)
    {
        var g = new GameObject("Flooding").transform;
        g.SetParent(parent);
        Vector3 o = SewerOrigin;
        var cistern = Zone(g, "Cistern", -1f, CisternCentre.x - CisternRadius, CisternCentre.x + CisternRadius, CisternCentre.z - CisternRadius, CisternCentre.z + CisternRadius, CisternFloor - 0.4f, true);
        Zone(g, "Siphon", -0.4f, -3f, 3f, 80f, 98f, -0.4f);
        Zone(g, "Pit2", -0.4f, 71f, 89f, 21f, 39f, -2.4f);
        var spillway = Zone(g, "Spillway", -0.6f, 43f, 67f, 129f, 132f, -0.6f);
        // The spillway drags a swimmer along to the culvert grate and under.
        var current = new GameObject("SpillwayCurrent").AddComponent<WaterCurrent>();
        current.transform.SetParent(g);
        current.area = new Bounds(o + new Vector3(55f, 0f, 130.5f), new Vector3(24f, 20f, 3f));
        current.flow = new Vector3(1.8f, 0f, 0f); current.down = 0.8f;
        current.zone = spillway;

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
        zone.areas = new[] { new Bounds(mid, new Vector3(x1 - x0, 24f, z1 - z0)) };   // 12 m down to 12 m up
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
