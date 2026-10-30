using System.Collections.Generic;
using UnityEngine;

// Station 3, the sewers. Reached by climbing down Canal St's emergency latch (EmergencyLatch). Built far from
// the stations, beyond z 600, 20 m below them. The tunnels are the Sewer pack's round brick-and-concrete
// pieces (LookTestBuilder.SewerKit.cs): a water channel down the middle between two walkways. The layout
// follows the director's map (turned round, so it runs north from the arrival); in metres from the arrival
// (x across, z forward):
//   arrival chamber (the ladder up to the hatch) -> north 12 m, a bend west ->
//   VALVE 1 room -> a junction (a spoke south into the cistern) -> VALVE 2 room -> a junction (a side room
//   to the west) -> a bend -> the cistern's east door.
//   The CISTERN (centre x -55, z 57): a round chamber 40 m across, a walkway ring round a deep pool, four
//   catwalks to the central platform with the FINAL VALVE. Doors on its four sides.
//   West door -> the west hall -> (north) a side room / (south) a bend -> VALVE 3 room.
//   South door -> a junction (west: VALVE 4 room) -> the outfall chamber -> the overflow tunnel to the river.
// For now everything is walkable and the water stands still: the valves are placeholders (turning them,
// the water dropping, swimming and drowning come next).
public static partial class LookTestBuilder
{
    static readonly Vector3 SewerOrigin = new Vector3(0f, -20f, 600f);
    public static Vector3 SewerSpawn => SewerOrigin + new Vector3(0f, 0.05f, 2.6f);
    static readonly Vector3 CisternCentre = new Vector3(-55f, 0f, 57f);   // relative to the sewer origin
    const float CisternRadius = 20f;

    static Material sewerBrick, sewerFloor, sewerMud, sewerWater, sewerGrate;

    static void BuildSewers()
    {
        var g = Group("Sewers");
        valveWheels.Clear();
        sewerBrick = Mat("SewerBrick", Tex2(SewerTex + "bricks.jpg"), new Color(0.72f, 0.66f, 0.58f), 2f, 0.15f);
        sewerFloor = Mat("SewerFloor", Tex2(SewerTex + "concrete_dirty_2.jpg"), new Color(0.7f, 0.7f, 0.66f), 3f, 0.2f);
        sewerMud = Mat("SewerMud", Tex2(SewerTex + "soil_mud.jpg"), new Color(0.5f, 0.46f, 0.38f), 2f, 0.3f);
        sewerWater = TransparentMat("SewerWater", new Color(0.04f, 0.06f, 0.045f, 0.82f), 0.95f);
        sewerGrate = Mat("SewerGrate", Tex2(SewerTex + "Metal.jpg"), new Color(0.45f, 0.43f, 0.4f), 1f, 0.35f, 0.6f);
        Vector3 o = SewerOrigin;

        // ---- arrival: a brick chamber with a ladder up the south wall to the latch's hatch (shut overhead)
        SewerRoom(g, "Arrival", -2f, 2f, 0f, 4f, 0f, 3f, (2, -2f, 2f, 2.6f));   // open on to both walkways
        Span("ArrivalHatch", g, o + new Vector3(-0.6f, 2.9f, 0f), o + new Vector3(0.6f, 2.98f, 1.1f), Mat("LatchSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.55f, 0.53f, 0.5f), 1f, 0.35f, 0.6f));
        // A ladder leaning on the south wall up to the hatch: one side of the Soviet metro pack's stepladder
        // (Tools/Blender/ladder_half.py), dark steel. Turned so its top leans toward the wall (-Z).
        var ladder = Model(ArtDir + "/Props/SovietMetro/Ladder.fbx", null, g, null, 1f, 0f);
        ladder.name = "Ladder";
        var ladderSteel = Mat("LadderSteel", null, new Color(0.35f, 0.33f, 0.3f), 1f, 0.4f, 0.7f);
        foreach (var lr in ladder.GetComponentsInChildren<Renderer>()) { var ms = lr.sharedMaterials; for (int m = 0; m < ms.Length; m++) ms[m] = ladderSteel; lr.sharedMaterials = ms; }
        ladder.transform.localScale *= 2.85f / Bounds(ladder).size.y;
        var mf = ladder.GetComponentInChildren<MeshFilter>();
        Vector3 topSum = Vector3.zero, footSum = Vector3.zero; int tops = 0, feet = 0;
        float lowY = Bounds(ladder).min.y, highY = Bounds(ladder).max.y;
        foreach (var v in mf.sharedMesh.vertices)
        {
            var w = mf.transform.TransformPoint(v);
            if (w.y > highY - 0.2f) { topSum += w; tops++; } else if (w.y < lowY + 0.2f) { footSum += w; feet++; }
        }
        Vector3 lean = topSum / tops - footSum / feet; lean.y = 0f;
        ladder.transform.rotation = Quaternion.FromToRotation(lean.normalized, Vector3.back) * ladder.transform.rotation;
        StandOn(ladder, o + new Vector3(0f, 0f, 0.5f));
        ladder.transform.position += Vector3.forward * (o.z + 0.03f - Bounds(ladder).min.z);   // its top against the wall
        EmergencyLight(g, o + new Vector3(1.6f, 2.5f, 3.6f), 1.6f, 6f);

        // ---- the tunnels (positions relative to the sewer origin; a port's height is the channel's bottom)
        var kit = new GameObject("Tunnels").transform;
        kit.SetParent(g);
        Vector3 P(float x, float z) => o + new Vector3(x, -SewerWalkwayRise, z);
        Port Door(float x, float z, Vector3 way) => new Port(P(x, z), way);

        // The arrival tunnel: north from the arrival, a bend west, on to valve 1.
        var k1 = KitNode(kit, "Serwers", P(0f, 16f), Vector3.back, Vector3.left);
        KitLink(kit, Door(0f, 4f, Vector3.forward), k1[Vector3.back]);
        KitTunnel(kit, k1[Vector3.left], P(-30f, 16f));
        // Valve 1 -> a junction (a spoke south to the cistern) -> valve 2.
        var t1 = KitNode(kit, "Serwers_001", P(-55f, 16f), Vector3.right, Vector3.left, Vector3.forward);
        KitLink(kit, Door(-40f, 16f, Vector3.left), t1[Vector3.right]);
        KitTunnel(kit, t1[Vector3.left], P(-81f, 16f));
        KitTunnel(kit, t1[Vector3.forward], P(-55f, CisternCentre.z - CisternRadius));
        // Valve 2 -> a junction (a side room west) -> a bend -> the cistern's east door.
        var t2 = KitNode(kit, "Serwers_001", P(-86f, 45f), Vector3.back, Vector3.forward, Vector3.left);
        KitLink(kit, Door(-86f, 21f, Vector3.forward), t2[Vector3.back]);
        KitTunnel(kit, t2[Vector3.left], P(-95f, 45f));
        var k2 = KitNode(kit, "Serwers", P(-86f, 57f), Vector3.back, Vector3.right);
        KitLink(kit, t2[Vector3.forward], k2[Vector3.back]);
        KitTunnel(kit, k2[Vector3.right], P(CisternCentre.x - CisternRadius, 57f));
        // The cistern's west door -> the west hall -> (north) a side room / (south) a bend -> valve 3.
        KitTunnel(kit, Door(CisternCentre.x + CisternRadius, 57f, Vector3.right), P(-19f, 57f));
        KitTunnel(kit, Door(-12f, 50f, Vector3.back), P(-12f, 35f));
        var k3 = KitNode(kit, "Serwers", P(-12f, 81f), Vector3.back, Vector3.left);
        KitLink(kit, Door(-12f, 64f, Vector3.forward), k3[Vector3.back]);
        KitTunnel(kit, k3[Vector3.left], P(-23f, 81f));
        // The cistern's south door -> a junction (valve 4 to the west) -> the outfall chamber.
        var t3 = KitNode(kit, "Serwers_001", P(-55f, 88f), Vector3.back, Vector3.forward, Vector3.left);
        KitLink(kit, Door(-55f, CisternCentre.z + CisternRadius, Vector3.forward), t3[Vector3.back]);
        KitTunnel(kit, t3[Vector3.left], P(-77f, 88f));
        KitTunnel(kit, t3[Vector3.forward], P(-55f, 101f));
        // The overflow tunnel from the outfall chamber west to the river.
        KitTunnel(kit, Door(-63f, 109f, Vector3.left), P(-95f, 109f));
        TunnelExtras(g, kit);
        // Grated footbridges at each junction: one over the side branch's channel where it meets the main
        // tunnel (so the walkway past a junction doesn't drop into the water), and one across the main
        // channel (to cross to the other walkway).
        foreach (var (at, way) in new[] { (new Vector3(-55f, 0f, 16f), Vector3.forward), (new Vector3(-86f, 0f, 45f), Vector3.left), (new Vector3(-55f, 0f, 88f), Vector3.left) })
        {
            var across = Vector3.Cross(Vector3.up, way);
            ChannelBridge(kit.parent, o + at + way * 2.15f, way * 1.2f + across * 1.3f);   // the walkway is 1.0..3.3 m out from the main channel
            ChannelBridge(kit.parent, o + at - way * 0.25f, way * 1.2f + across * 0.7f);   // meets the first one edge to edge
        }

        // ---- the rooms (floors at the walkways' height)
        KitRoom(g, "ValveRoom1", -40f, -30f, 11f, 21f, 4.5f, (1, 16f), (3, 16f));
        Valve(g, "Valve1", o + new Vector3(-35f, 0f, 21f), Vector3.back);
        KitRoom(g, "ValveRoom2", -91f, -81f, 11f, 21f, 4.5f, (1, 16f), (2, -86f));
        Valve(g, "Valve2", o + new Vector3(-91f, 0f, 16f), Vector3.right);
        KitRoom(g, "SideRoomEast", -105f, -95f, 40f, 50f, 4f, (1, 45f));
        KitRoom(g, "WestHall", -19f, -5f, 50f, 64f, 5.5f, (3, 57f), (0, -12f), (2, -12f));
        KitRoom(g, "SideRoomNorthWest", -17f, -7f, 25f, 35f, 4f, (2, -12f));
        // Valve 3's room is a sump: steps down 2.5 m from the tunnel, its ceiling below the flood's surface,
        // so the valve has to be reached and turned under water.
        KitRoomAt(g, "ValveRoom3", -2.5f, -33f, -23f, 76f, 86f, 3.6f, (1, 81f));
        Span("ValveRoom3_Threshold", g, o + new Vector3(-23f, -2.5f, 78.8f), o + new Vector3(-22.7f, 0f, 83.2f), sewerFloor);
        for (int k = 0; k < 7; k++)   // 31 cm steps (the floor is the eighth): low enough to walk back up once it's drained
            Span("ValveRoom3_Step" + k, g, o + new Vector3(-23.4f - 0.4f * k, -2.5f, 78.8f), o + new Vector3(-23f - 0.4f * k, -0.3125f * (k + 1), 83.2f), sewerFloor);
        Valve(g, "Valve3", o + new Vector3(-33f, -2.5f, 81f), Vector3.right);
        KitRoom(g, "ValveRoom4", -87f, -77f, 83f, 93f, 4.5f, (1, 88f));
        Valve(g, "Valve4", o + new Vector3(-87f, 0f, 88f), Vector3.right);
        KitRoom(g, "OutfallChamber", -63f, -47f, 101f, 117f, 6f, (0, -55f), (3, 109f));
        Cistern(g);
        var grate = RiverOutlet(g, o + new Vector3(-95f, 0f, 109f), ladderSteel);
        Flooding(g, grate);

        // ---- sound: running water in the cistern and down the tunnels
        foreach (var (name, at, range) in new[] { ("CisternFlow", CisternCentre, 45f), ("TunnelFlowNorth", new Vector3(-55f, 0f, 16f), 30f), ("OutfallFlow", new Vector3(-55f, 0f, 109f), 35f) })
        {
            var flow = new GameObject(name).AddComponent<AudioSource>();
            flow.transform.SetParent(g);
            flow.transform.position = o + at;
            flow.clip = Load<AudioClip>(SfxDir + "/OverflowingWaterPipeLoop.wav");
            flow.loop = true; flow.playOnAwake = true; flow.volume = 0.5f; flow.spatialBlend = 1f;
            flow.rolloffMode = AudioRolloffMode.Linear; flow.minDistance = 4f; flow.maxDistance = range;
        }

        // ---- where Sousa lands after climbing down Canal St's latch
        var arrival = new GameObject("SewerArrival").transform;
        arrival.SetParent(g);
        arrival.SetPositionAndRotation(SewerSpawn, Quaternion.identity);   // facing north, up the arrival tunnel
        var latch = Object.FindAnyObjectByType<EmergencyLatch>();
        if (latch) { latch.sewerArrival = arrival; latch.climb = Load<AudioClip>(SfxDir + "/FootstepsConcrete2.wav"); }
    }

    // A caged bulb hanging from the tunnel ceiling every 12 m or so (the water is laid by Flooding).
    static void TunnelExtras(Transform g, Transform kit)
    {
        int n = 0;
        foreach (Transform piece in kit)
        {
            var b = Bounds(piece.gameObject);
            if (n++ % 2 == 0)
                CagedBulb(g, "SewerBulb_" + n, new Vector3(b.center.x, SewerOrigin.y + 4.3f, b.center.z), 1.4f, 7f, flicker: n % 5 == 0, cable: 0.9f);
        }
    }

    // A brick room x0..x1, z0..z1 (relative to the sewer origin), its floor at the walkways' height,
    // 'height' tall, with a 4.4 m wide, 3.6 m tall doorway for each tunnel that meets it: (side: 0 south,
    // 1 east, 2 north, 3 west; the door's middle, along x for south/north and along z for east/west).
    static void KitRoom(Transform g, string name, float x0, float x1, float z0, float z1, float height, params (int side, float at)[] doors)
        => KitRoomAt(g, name, 0f, x0, x1, z0, z1, height, doors);

    // The same with its floor at 'floor' (relative to the walkways).
    static void KitRoomAt(Transform g, string name, float floor, float x0, float x1, float z0, float z1, float height, params (int side, float at)[] doors)
    {
        var gaps = new List<(int, float, float, float)>();
        foreach (var d in doors) gaps.Add((d.side, d.at - 2.2f, d.at + 2.2f, 3.6f));
        SewerRoom(g, name, x0, x1, z0, z1, floor, floor + height, gaps.ToArray());
        CagedBulb(g, name + "_Bulb", SewerOrigin + new Vector3((x0 + x1) * 0.5f, floor + height - 0.3f, (z0 + z1) * 0.5f), 1.3f, 6f, cable: 0.4f);
    }

    // A valve on a wall: a rusty pipe up the wall with a big red wheel (a placeholder; turning it comes
    // next). 'wallFloor' is on the floor at the wall, 'inward' points into the room.
    static GameObject Valve(Transform g, string name, Vector3 wallFloor, Vector3 inward)
    {
        Vector3 pipeAt = wallFloor + inward * 0.35f;
        Pipe(g, name + "_Pipe", pipeAt, pipeAt + Vector3.up * 3.5f, 0.2f, pipeRust);
        var wheel = Prim(PrimitiveType.Cylinder, name + "_Wheel", g, pipeAt + Vector3.up * 1.3f + inward * 0.3f, new Vector3(0.7f, 0.03f, 0.7f),
                         Mat("ValveRed", null, new Color(0.5f, 0.12f, 0.08f), 1f, 0.4f, 0.4f));
        wheel.transform.rotation = Quaternion.FromToRotation(Vector3.up, inward);
        var stem = Prim(PrimitiveType.Cylinder, name + "_Stem", g, pipeAt + Vector3.up * 1.3f + inward * 0.15f, new Vector3(0.08f, 0.15f, 0.08f), pipeSteel);
        stem.transform.rotation = wheel.transform.rotation;
        Object.DestroyImmediate(stem.GetComponent<Collider>());
        valveWheels[name] = wheel;
        return wheel;
    }

    // The cistern: a round brick chamber (24 flat wall sections, 40 m across, 9 m high) with a 4 m walkway
    // round a 6 m deep pool, a doorway on each side where the tunnels arrive, four grated catwalks from the
    // walkway to the round central platform, and the final valve standing on it.
    static void Cistern(Transform parent)
    {
        var g = new GameObject("Cistern").transform;
        g.SetParent(parent);
        Vector3 c = SewerOrigin + CisternCentre;
        const float R = CisternRadius, inner = 16f, top = 9f, deep = -6f;
        float side = 2f * (R + 0.4f) * Mathf.Tan(7.5f * Mathf.Deg2Rad) + 0.05f;   // one wall section's width
        for (int i = 0; i < 24; i++)
        {
            float a = i * 15f;
            var rot = Quaternion.Euler(0f, a, 0f);
            Vector3 d = rot * Vector3.forward, across = rot * Vector3.right;
            // Outer wall (a doorway in the sections facing the four tunnels).
            Vector3 wallMid = c + d * (R + 0.2f);
            if (i % 6 == 0)
            {
                float post = (side - 4.4f) * 0.5f;
                foreach (float s in new[] { -1f, 1f })
                    Turned(Box("CisternWall_" + i, g, wallMid + across * s * (2.2f + post * 0.5f) + Vector3.up * (top - 0.5f) * 0.5f, new Vector3(post, top + 0.5f, 0.4f), sewerBrick), rot);
                Turned(Box("CisternLintel_" + i, g, wallMid + Vector3.up * (3.6f + (top - 3.6f) * 0.5f), new Vector3(4.4f, top - 3.6f, 0.4f), sewerBrick), rot);
                Turned(Box("CisternSill_" + i, g, wallMid + Vector3.down * 1.105f, new Vector3(4.4f, 2.19f, 0.4f), sewerFloor), rot);   // closes the tunnel's channel (its top 1 cm under the walkway)
            }
            else Turned(Box("CisternWall_" + i, g, wallMid + Vector3.up * (top - 0.5f) * 0.5f, new Vector3(side, top + 0.5f, 0.4f), sewerBrick), rot);
            // The walkway ring and the pool's wall below its edge.
            Turned(Box("CisternWalkway_" + i, g, c + d * ((R + inner) * 0.5f) + Vector3.down * (0.15f + (i % 2) * 0.004f), new Vector3(side, 0.3f, R - inner), sewerFloor), rot);
            float poolSide = 2f * inner * Mathf.Tan(7.5f * Mathf.Deg2Rad) + 0.05f;
            Turned(Box("CisternPoolWall_" + i, g, c + d * (inner - 0.15f) + Vector3.up * (deep - 0.3f) * 0.5f, new Vector3(poolSide, 0.3f - deep, 0.3f), sewerBrick), rot);
        }
        Span("CisternPoolBed", g, c + new Vector3(-inner, deep - 0.2f, -inner), c + new Vector3(inner, deep, inner), sewerMud);
        Span("CisternCeiling", g, c + new Vector3(-R - 1f, top, -R - 1f), c + new Vector3(R + 1f, top + 0.3f, R + 1f), sewerBrick);

        // The central platform: a round concrete pier up from the pool's bed, its top level with the walkway.
        var pier = Prim(PrimitiveType.Cylinder, "CentralPlatform", g, c + Vector3.up * deep * 0.5f, new Vector3(9f, -deep * 0.5f, 9f), sewerFloor);
        Object.DestroyImmediate(pier.GetComponent<Collider>());
        pier.AddComponent<MeshCollider>();
        // Four catwalks, on the diagonals, from the walkway to the platform, with a rail along each side.
        for (int i = 0; i < 4; i++)
        {
            var rot = Quaternion.Euler(0f, 45f + i * 90f, 0f);
            Vector3 d = rot * Vector3.forward, across = rot * Vector3.right;
            float from = 4.3f, to = inner + 0.3f, mid = (from + to) * 0.5f, length = to - from;
            Turned(Box("Catwalk_" + i, g, c + d * mid + Vector3.down * 0.05f, new Vector3(1.8f, 0.12f, length), sewerGrate), rot);   // 1 cm proud, never level with what it rests on
            foreach (float s in new[] { -0.9f, 0.9f })
            {
                var rail = Box("CatwalkRail_" + i, g, c + d * mid + across * s + Vector3.up * 1f, new Vector3(0.05f, 0.05f, length), pipeSteel);
                Turned(rail, rot);
                for (float p = -length * 0.5f; p <= length * 0.5f + 0.01f; p += length / 4f)
                {
                    var post = Box("CatwalkPost_" + i, g, c + d * (mid + p) + across * s + Vector3.up * 0.5f, new Vector3(0.05f, 1f, 0.05f), pipeSteel);
                    Turned(post, rot);
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }
            }
        }
        // The final valve on the platform: a thick riser out of the pier with a big wheel on top.
        Pipe(g, "FinalValve_Riser", c + Vector3.down * 0.5f, c + Vector3.up * 1.1f, 0.3f, pipeRust);
        valveWheels["FinalValve"] = Prim(PrimitiveType.Cylinder, "FinalValve_Wheel", g, c + Vector3.up * 1.15f, new Vector3(1.4f, 0.04f, 1.4f), Mat("ValveRed", null, new Color(0.5f, 0.12f, 0.08f), 1f, 0.4f, 0.4f));
        // (The pool's water is one of the flood zones: LookTestBuilder.SewerWater.cs.)
        // Light: bulbs on the wall between the doors, one over the platform.
        for (int i = 0; i < 4; i++)
        {
            var d = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward;
            CagedBulb(g, "CisternBulb_" + i, c + d * (R - 0.6f) + Vector3.up * 6f, 2.2f, 14f, flicker: i == 2);
        }
        CagedBulb(g, "CisternBulb_Centre", c + Vector3.up * 6.5f, 2.5f, 12f, cable: 2.5f);
    }

    static GameObject Turned(GameObject go, Quaternion rot) { go.transform.rotation = rot; return go; }

    // A grated steel plate 10 cm thick, its top level with the walkways, 'half' metres out from 'mid' each way.
    static void ChannelBridge(Transform g, Vector3 mid, Vector3 half)
    {
        half = new Vector3(Mathf.Abs(half.x), 0f, Mathf.Abs(half.z));
        Span("ChannelBridge", g, mid - half - new Vector3(0f, 0.1f, 0f), mid + half, sewerGrate);
    }

    // The end of the overflow tunnel: a wall with a barred opening onto the night and the river (the way
    // out once the water is purged).
    static Transform RiverOutlet(Transform g, Vector3 at, Material bars)
    {
        Span("RiverOutletWall", g, at + new Vector3(-0.3f, -1.5f, -4f), at + new Vector3(0f, 5.8f, 4f), sewerBrick);
        var night = Prim(PrimitiveType.Quad, "RiverNight", g, at + new Vector3(0.02f, 1.6f, 0f), new Vector3(3.6f, 3.2f, 1f), EmissiveMat("RiverNight", new Color(0.12f, 0.16f, 0.24f)));
        night.transform.rotation = Quaternion.LookRotation(Vector3.left);
        Object.DestroyImmediate(night.GetComponent<Collider>());
        var grate = new GameObject("RiverGrate").transform;   // lifted as one by RiverExit
        grate.SetParent(g);
        for (float z = -1.7f; z <= 1.71f; z += 0.3f)
            Prim(PrimitiveType.Cylinder, "GrateBar", grate, at + new Vector3(0.4f, 1.6f, z), new Vector3(0.06f, 1.6f, 0.06f), bars);
        Span("GrateRail", grate, at + new Vector3(0.35f, 3.1f, -1.8f), at + new Vector3(0.45f, 3.25f, 1.8f), bars);
        var moon = new GameObject("RiverMoonlight").AddComponent<Light>();
        moon.transform.SetParent(g);
        moon.transform.position = at + new Vector3(1.5f, 2f, 0f);
        moon.type = LightType.Point; moon.color = new Color(0.55f, 0.65f, 0.9f); moon.intensity = 1.2f; moon.range = 9f;
        return grate;
    }

    // A brick room x0..x1, z0..z1 (relative to the sewer origin), floor at 'y0' and ceiling at 'y1', with 0.3 m
    // walls outside its edges. Each gap is (side: 0 south, 1 east, 2 north, 3 west; from; to; top): a doorway
    // in that wall, along x for south/north and along z for east/west, 'top' high above the floor.
    static void SewerRoom(Transform g, string name, float x0, float x1, float z0, float z1, float y0, float y1,
                          params (int side, float from, float to, float top)[] gaps) => SewerRoom(g, name, x0, x1, z0, z1, y0, y1, true, gaps);

    static void SewerRoom(Transform g, string name, float x0, float x1, float z0, float z1, float y0, float y1, bool floor,
                          params (int side, float from, float to, float top)[] gaps)
    {
        Vector3 o = SewerOrigin;
        const float t = 0.3f;
        if (floor) Span(name + "_Floor", g, o + new Vector3(x0, y0 - 0.2f, z0), o + new Vector3(x1, y0, z1), sewerFloor);
        Span(name + "_Ceiling", g, o + new Vector3(x0, y1, z0), o + new Vector3(x1, y1 + 0.2f, z1), sewerBrick);
        for (int side = 0; side < 4; side++)
        {
            bool alongX = side == 0 || side == 2;
            float a0 = alongX ? x0 - (side == 0 ? t : 0f) : z0, a1 = alongX ? x1 + (side == 0 ? t : 0f) : z1;   // south wall covers the corners
            if (alongX) { a0 = x0 - t; a1 = x1 + t; }
            var cuts = new List<(float from, float to, float top)>();
            foreach (var gp in gaps) if (gp.side == side) cuts.Add((gp.from, gp.to, gp.top));
            cuts.Sort((p, q) => p.from.CompareTo(q.from));
            float at = a0; int n = 0;
            foreach (var c in cuts)
            {
                if (c.from > at) WallPiece(g, name + "_Wall" + side + "_" + n++, side, at, c.from, x0, x1, z0, z1, y0 - 2.2f, y1 + 0.2f);
                if (c.top < y1 - y0) WallPiece(g, name + "_Lintel" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0 + c.top, y1 + 0.2f);
                // A sill level with the floor: closes the end of a tunnel's channel below the doorway.
                WallPiece(g, name + "_Sill" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0 - 2.2f, y0);
                Headwall(g, name + "_Headwall" + side + "_" + n++, side, c.from, c.to, c.top, x0, x1, z0, z1, y0);
                at = c.to;
            }
            if (at < a1) WallPiece(g, name + "_Wall" + side + "_" + n, side, at, a1, x0, x1, z0, z1, y0 - 2.2f, y1 + 0.2f);
        }
    }

    // A brick end-wall just outside a doorway, across the whole round tunnel that meets it (8 m wide, up to
    // 6.5 m above the floor), with the doorway left open: it closes the tunnel's end where the room is lower
    // or narrower than the tunnel.
    static void Headwall(Transform g, string name, int side, float from, float to, float top, float x0, float x1, float z0, float z1, float y0)
    {
        Vector3 o = SewerOrigin;
        const float inner = 0.3f, outer = 0.55f, reach = 2f;   // 25 cm thick, just beyond the room's own wall
        float lo = y0 - 2.2f, hi = Mathf.Max(y0, 0f) + 6.5f;   // up past the tunnel's crown, even for a sunken room
        foreach (var (a, b, y1, y2) in new[] { (from - reach, from, lo, hi), (to, to + reach, lo, hi), (from, to, y0 + top, hi) })
        {
            Vector3 min, max;
            switch (side)
            {
                case 0: min = new Vector3(a, y1, z0 - outer); max = new Vector3(b, y2, z0 - inner); break;
                case 2: min = new Vector3(a, y1, z1 + inner); max = new Vector3(b, y2, z1 + outer); break;
                case 1: min = new Vector3(x1 + inner, y1, a); max = new Vector3(x1 + outer, y2, b); break;
                default: min = new Vector3(x0 - outer, y1, a); max = new Vector3(x0 - inner, y2, b); break;
            }
            Span(name, g, o + min, o + max, sewerBrick);
        }
    }

    // One straight piece of a SewerRoom's wall, from 'a' to 'b' along it, between heights 'lo' and 'hi'.
    static void WallPiece(Transform g, string name, int side, float a, float b, float x0, float x1, float z0, float z1, float lo, float hi)
    {
        Vector3 o = SewerOrigin;
        const float t = 0.3f;
        Vector3 min, max;
        switch (side)
        {
            case 0: min = new Vector3(a, lo, z0 - t); max = new Vector3(b, hi, z0); break;
            case 2: min = new Vector3(a, lo, z1); max = new Vector3(b, hi, z1 + t); break;
            case 1: min = new Vector3(x1, lo, a); max = new Vector3(x1 + t, hi, b); break;
            default: min = new Vector3(x0 - t, lo, a); max = new Vector3(x0, hi, b); break;
        }
        Span(name, g, o + min, o + max, sewerBrick);
    }

    // A still, murky water surface over x0..x1, z0..z1 at height 'y' (relative to the sewer origin).
    static void SewerWaterPlane(Transform g, string name, float x0, float x1, float z0, float z1, float y)
    {
        var water = Prim(PrimitiveType.Quad, name, g, SewerOrigin + new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), new Vector3(x1 - x0, z1 - z0, 1f), sewerWater);
        water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // facing up
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.AddComponent<WaterSurface>().size = new Vector2(x1 - x0, z1 - z0);   // deep enough, it breaks a fall
    }
}
