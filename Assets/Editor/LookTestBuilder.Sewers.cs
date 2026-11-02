using System.Collections.Generic;
using UnityEngine;

// Station 3, the sewers: a city storm-relief system, laid out from the director's plan (2026-10-30). Reached by
// climbing down Canal St's emergency latch (EmergencyLatch); built beyond z 600, 20 m below the stations.
// The tunnels are the Sewer pack's round brick pieces (LookTestBuilder.SewerKit.cs): a water channel down the
// middle between two walkways. In metres from the arrival (x to the right of the plan, z down it):
//   MAINTENANCE GALLERY along the top: the access shaft (the ladder up to the hatch) -> a corridor -> the
//   CONTROL ROOM -> a corridor -> the ELECTRICAL ROOM.
//   The INTERCEPTOR RING: a square loop of tunnel round the cistern, a VALVE CHAMBER at each corner:
//   valve 1 top-left (under the access shaft), valve 2 top-right (a flooded pit), valve 3 bottom-left (past a
//   flooded SIPHON: a shaft down, a culvert, a shaft up), valve 4 bottom-right.
//   The CISTERN SHAFT in the middle (centre x 40, z 70): a round shaft 28 m across and 16 m deep; four spokes
//   from the ring arrive on a gallery round its top, stairs spiral down the wall to the bottom, where the
//   FINAL VALVE stands.
//   The PUMP ROOM below the ring, a fast spillway across it, and the overflow tunnel out to the RIVER.
// Water and what the valves do: LookTestBuilder.SewerWater.cs.
public static partial class LookTestBuilder
{
    static readonly Vector3 SewerOrigin = new Vector3(0f, -20f, 600f);
    public static Vector3 SewerSpawn => SewerOrigin + new Vector3(0f, 0.05f, 2.6f);
    static readonly Vector3 CisternCentre = new Vector3(40f, 0f, 70f);   // relative to the sewer origin
    const float CisternRadius = 14f;      // the shaft's inner face
    const float CisternFloor = -32f;      // its bottom, 32 m below the tunnels
    const float TunnelDoorHalf = 3f, CorridorDoorHalf = 2.2f;   // half a doorway's width: onto a tunnel, onto a corridor
    static readonly List<Vector3> corridorEnds = new List<Vector3>();   // where each corridor meets a room (sewer-relative)
    static readonly List<Bounds> culvertAreas = new List<Bounds>();     // the water inside each culvert (world space)

    static Material sewerBrick, sewerFloor, sewerMud, sewerWater, sewerGrate, sewerCeiling, sewerConcrete;
    const string FoxTexDir = "Assets/Art/Textures/FoxTex/";

    static void BuildSewers()
    {
        var g = Group("Sewers");
        valveWheels.Clear();
        reliefGates.Clear();
        corridorEnds.Clear();
        culvertAreas.Clear();
        kitChannels.Clear();
        deepened.Clear();
        // The rooms match the tunnels: the tunnels' own brick (at the size the tunnels show it),
        // cracked old stone slabs underfoot (FoxTex), plain concrete for ceilings, beams and base courses.
        sewerBrick = Mat("SewerBrick", Tex2(SewerTex + "brick_modern_01.jpg"), Color.white, 4f, 0.15f);
        sewerFloor = Mat("SewerFloor", Load<Texture2D>(FoxTexDir + "StoneSlabsCracked.png"), new Color(0.85f, 0.83f, 0.8f), 3f, 0.25f);
        sewerCeiling = Mat("SewerCeiling", Tex2(SewerTex + "concrete_base_03.jpg"), new Color(0.75f, 0.73f, 0.7f), 3f, 0.1f);
        sewerConcrete = Mat("SewerConcrete", Tex2(SewerTex + "concrete_dirty.jpg"), new Color(0.8f, 0.78f, 0.74f), 2f, 0.15f);
        sewerMud = Mat("SewerMud", Tex2(SewerTex + "soil_mud.jpg"), new Color(0.5f, 0.46f, 0.38f), 2f, 0.3f);
        // Murky green water whose rippling surface pictures play as a loop (WaterFrames, set up below).
        var waterFrames = new Texture2D[20];
        for (int i = 0; i < 20; i++) waterFrames[i] = Load<Texture2D>(FoxTexDir + "Water/WaterSurface_" + (i + 1).ToString("00") + ".png");
        sewerWater = TransparentMat("SewerWater", new Color(0.26f, 0.32f, 0.22f, 0.86f), 0.9f);
        sewerWater.SetTexture("_BaseMap", waterFrames[0]);
        sewerGrate = Mat("SewerGrate", Tex2(SewerTex + "Metal.jpg"), new Color(0.45f, 0.43f, 0.4f), 1f, 0.35f, 0.6f);
        Vector3 o = SewerOrigin;

        // ---- the maintenance gallery's corridors first (the rooms they meet keep their doorways corridor-wide)
        Corridor(g, "GalleryCorridorA", 3f, 18f, 5f);
        Corridor(g, "GalleryCorridorB", 32f, 40f, 5f);

        // ---- arrival: a brick chamber with a ladder up the south wall to the latch's hatch (shut overhead)
        KitRoom(g, "Arrival", -3f, 3f, 0f, 8f, 4f, (2, 0f), (1, 5f));   // the access shaft's foot: down the plan to valve 1, right along the gallery
        Span("ArrivalHatch", g, o + new Vector3(-0.6f, 3.9f, 0f), o + new Vector3(0.6f, 3.98f, 1.1f), Mat("LatchSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.55f, 0.53f, 0.5f), 1f, 0.35f, 0.6f));
        // A ladder leaning on the south wall up to the hatch: one side of the Soviet metro pack's stepladder
        // (Tools/Blender/ladder_half.py), dark steel. Turned so its top leans toward the wall (-Z).
        var ladder = Model(ArtDir + "/Props/SovietMetro/Ladder.fbx", null, g, null, 1f, 0f);
        ladder.name = "Ladder";
        var ladderSteel = Mat("LadderSteel", null, new Color(0.35f, 0.33f, 0.3f), 1f, 0.4f, 0.7f);
        foreach (var lr in ladder.GetComponentsInChildren<Renderer>()) { var ms = lr.sharedMaterials; for (int m = 0; m < ms.Length; m++) ms[m] = ladderSteel; lr.sharedMaterials = ms; }
        ladder.transform.localScale *= 3.85f / Bounds(ladder).size.y;   // up to the hatch in the 4 m ceiling
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
        ClearPillars(o + new Vector3(0f, 2f, 0.3f), 1.2f);   // the ladder and the hatch need that bit of wall
        EmergencyLight(g, o + new Vector3(1.6f, 2.5f, 3.6f), 1.6f, 6f);

        // ---- the tunnels (positions relative to the sewer origin; a port's height is the channel's bottom)
        var kit = new GameObject("Tunnels").transform;
        kit.SetParent(g);
        Vector3 P(float x, float z) => o + new Vector3(x, -SewerWalkwayRise, z);
        Port Door(float x, float z, Vector3 way) => new Port(P(x, z), way);
        Vector3 down = Vector3.forward, up = Vector3.back, right = Vector3.right, left = Vector3.left;   // on the plan

        // Access shaft -> valve 1's chamber (the ring's top-left corner).
        KitTunnel(kit, Door(0f, 8f, down), P(0f, 23f));
        // The ring's top: valve 1 -> a junction up to the control room -> a junction down the north spoke -> valve 2.
        var tc = KitNode(kit, "Serwers_001", P(25f, 30f), up, left, right);
        KitLink(kit, Door(7f, 30f, right), tc[left]);
        KitTunnel(kit, tc[up], P(25f, 10f));
        var tn = KitNode(kit, "Serwers_001", P(40f, 30f), left, right, down);
        KitLink(kit, tc[right], tn[left]);
        KitTunnel(kit, tn[down], P(40f, CisternCentre.z - CisternRadius));
        KitTunnel(kit, tn[right], P(71f, 30f));
        // The ring's left: valve 1 -> a junction (the west spoke) -> the siphon -> valve 3.
        var tw = KitNode(kit, "Serwers_001", P(0f, 70f), up, down, right);
        KitLink(kit, Door(0f, 37f, down), tw[up]);
        KitTunnel(kit, tw[right], P(CisternCentre.x - CisternRadius, 70f));
        KitTunnel(kit, tw[down], P(0f, 80f));
        KitTunnel(kit, Door(0f, 98f, down), P(0f, 101f));
        // The ring's right: valve 2 -> a junction (the east spoke) -> valve 4.
        var te = KitNode(kit, "Serwers_001", P(80f, 70f), up, down, left);
        KitLink(kit, Door(80f, 39f, down), te[up]);
        KitTunnel(kit, te[left], P(CisternCentre.x + CisternRadius, 70f));
        KitTunnel(kit, te[down], P(80f, 103f));
        // The ring's bottom: valve 3 -> a junction up the south spoke -> a junction down to the pump room -> valve 4.
        var ts = KitNode(kit, "Serwers_001", P(40f, 110f), up, left, right);
        KitLink(kit, Door(40f, CisternCentre.z + CisternRadius, down), ts[up]);
        KitLink(kit, Door(9f, 110f, right), ts[left]);
        var tp = KitNode(kit, "Serwers_001", P(55f, 110f), left, right, down);
        KitLink(kit, ts[right], tp[left]);
        KitTunnel(kit, tp[right], P(73f, 110f));
        KitTunnel(kit, tp[down], P(55f, 125f));
        // The overflow tunnel from the pump room out to the river.
        KitTunnel(kit, Door(67f, 137f, right), P(100f, 137f));
        TunnelExtras(g, kit);
        // Grated footbridges at each junction: one over the side branch's channel where it meets the main
        // tunnel (so the walkway past a junction doesn't drop into the water), and one across the main
        // channel (to cross to the other walkway).
        foreach (var (at, way) in new[] { (new Vector3(25f, 0f, 30f), up), (new Vector3(40f, 0f, 30f), down), (new Vector3(0f, 0f, 70f), right),
                                          (new Vector3(80f, 0f, 70f), left), (new Vector3(40f, 0f, 110f), up), (new Vector3(55f, 0f, 110f), down) })
        {
            var across = Vector3.Cross(Vector3.up, way);
            ChannelBridge(kit.parent, o + at + way * (2.15f * KitWide), way * (1.2f * KitWide) + across * (ChannelHalf + 0.3f));   // the walkway is 1.3..4.3 m out from the main channel
            ChannelBridge(kit.parent, o + at - way * (0.25f * KitWide), way * (1.2f * KitWide) + across * ChannelHalf);   // meets the first one edge to edge
        }

        // ---- the maintenance gallery: access shaft -> corridor -> control room -> corridor -> electrical room
        KitRoom(g, "ControlRoom", 18f, 32f, 0f, 10f, 4.5f, (3, 5f), (1, 5f), (2, 25f));
        KitRoom(g, "ElectricalRoom", 40f, 54f, 0f, 10f, 4f, (3, 5f));

        // ---- the valve chambers at the ring's corners
        KitRoom(g, "ValveChamber1", -7f, 7f, 23f, 37f, 5f, (0, 0f), (1, 30f), (2, 0f));
        Valve(g, "Valve1", o + new Vector3(-7f, 0f, 30f), right);
        // Valve 2's chamber: a dry landing by its doors, a flooded pit round the far corner (the wheel under water).
        KitRoomAt(g, "ValveChamber2", -2.2f, 71f, 89f, 21f, 39f, 6.7f, (3, 30f), (2, 80f));
        Landing(g, "ValveChamber2", -2.2f, 71f, 83f, 27f, 39f);
        Steps(g, "ValveChamber2", -2.2f, new Vector3(83f, 0f, 34f), right, 2f);
        Valve(g, "Valve2", o + new Vector3(89f, -2.2f, 30f), left);
        KitRoom(g, "ValveChamber3", -9f, 9f, 101f, 119f, 5f, (0, 0f), (1, 110f));
        Valve(g, "Valve3", o + new Vector3(-9f, 0f, 110f), right);
        KitRoom(g, "ValveChamber4", 73f, 87f, 103f, 117f, 5f, (0, 80f), (3, 110f));
        Valve(g, "Valve4", o + new Vector3(87f, 0f, 110f), left);

        // ---- the siphon on the ring's left: a flooded shaft down, a culvert under, a shaft back up
        Siphon(g);
        PumpRoom(g);
        Cistern(g);
        var grate = RiverOutlet(g, o + new Vector3(100f, 0f, 137f), ladderSteel, -1f);
        Flooding(g, grate);
        SewerDressing(g);   // signs, colours, the schematic, props and rubbish (LookTestBuilder.SewerDressing.cs)
        SewerPower(g);      // the breaker, floodlights and Valve 4's gates (LookTestBuilder.SewerPower.cs)
        SewerDetail(g);     // depth gauges, tide lines, ladders, cables, pipes, junk (LookTestBuilder.SewerDetail.cs)
        SewerSupplies(g);   // flares, batteries and painkillers, two of each (LookTestBuilder.SewerSupplies.cs)

        // ---- sound: running water in the cistern and down the tunnels
        foreach (var (name, at, range) in new[] { ("CisternFlow", CisternCentre + Vector3.down * 12f, 45f), ("RingFlowTop", new Vector3(40f, 0f, 30f), 30f), ("SpillwayFlow", new Vector3(55f, 0f, 130.5f), 35f) })
        {
            var flow = new GameObject(name).AddComponent<AudioSource>();
            flow.transform.SetParent(g);
            flow.transform.position = o + at;
            flow.clip = Load<AudioClip>(SfxDir + "/OverflowingWaterPipeLoop.wav");
            flow.loop = true; flow.playOnAwake = true; flow.volume = 0.5f; flow.spatialBlend = 1f;
            flow.rolloffMode = AudioRolloffMode.Linear; flow.minDistance = 4f; flow.maxDistance = range;
        }

        // ---- the water surfaces ripple
        var ripples = g.gameObject.AddComponent<WaterFrames>();
        ripples.material = sewerWater; ripples.frames = waterFrames;

        // ---- where Sousa lands after climbing down Canal St's latch
        var arrival = new GameObject("SewerArrival").transform;
        arrival.SetParent(g);
        arrival.SetPositionAndRotation(SewerSpawn, Quaternion.identity);   // facing north, up the arrival tunnel
        var latch = Object.FindAnyObjectByType<EmergencyLatch>();
        if (latch) { latch.sewerArrival = arrival; latch.climb = Load<AudioClip>(SfxDir + "/FootstepsConcrete2.wav"); }
    }

    // The water in every tunnel's channel (it stands below the walkways, so only the channel shows it: 2 m deep,
    // over your head) and a caged bulb hanging from the ceiling every 12 m or so. The walkways never flood.
    static void TunnelExtras(Transform g, Transform kit)
    {
        float waterY = SewerOrigin.y + ChannelWater;
        var channels = new GameObject("ChannelWater").transform;
        channels.SetParent(g);
        int n = 0;
        foreach (Transform piece in kit)
        {
            var b = Bounds(piece.gameObject);
            var water = Prim(PrimitiveType.Quad, "ChannelWater", channels, new Vector3(b.center.x, waterY, b.center.z), new Vector3(b.size.x - 0.1f, b.size.z - 0.1f, 1f), sewerWater);
            water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.AddComponent<WaterSurface>().size = new Vector2(b.size.x, b.size.z);
            if (n++ % 2 == 0)
                CagedBulb(g, "SewerBulb_" + n, new Vector3(b.center.x, SewerOrigin.y + 5f, b.center.z), 1.4f, 8f, flicker: n % 5 == 0, cable: 0.9f);
        }
    }

    // A brick room x0..x1, z0..z1 (relative to the sewer origin), its floor at the walkways' height,
    // 'height' tall, with a 3.6 m tall doorway for each tunnel or corridor that meets it: (side: 0 south,
    // 1 east, 2 north, 3 west; the door's middle, along x for south/north and along z for east/west).
    // A tunnel's doorway is 6 m wide (both walkways come through) and its canal carries on under the floor
    // in a culvert; a corridor's is 4.4 m.
    static void KitRoom(Transform g, string name, float x0, float x1, float z0, float z1, float height, params (int side, float at)[] doors)
        => KitRoomAt(g, name, 0f, x0, x1, z0, z1, height, doors);

    // The same with its floor at 'floor' (relative to the walkways).
    static void KitRoomAt(Transform g, string name, float floor, float x0, float x1, float z0, float z1, float height, params (int side, float at)[] doors)
    {
        var gaps = new List<(int, float, float, float)>();
        foreach (var d in doors)
        {
            float half = DoorHalf(d.side, d.at, x0, x1, z0, z1);
            gaps.Add((d.side, d.at - half, d.at + half, 3.6f - floor));   // 3.6 m above the walkways, even in a sunken room
        }
        SewerRoom(g, name, x0, x1, z0, z1, floor, floor + height, gaps.ToArray());
        RoomStructure(g, name, x0, x1, z0, z1, floor, floor + height, doors);
        if (floor < 0f)   // a sunken room: each doorway's threshold rises to the walkways' height
            foreach (var d in doors) { float half = DoorHalf(d.side, d.at, x0, x1, z0, z1); WallPiece(g, name + "_Threshold", d.side, d.at - half, d.at + half, x0, x1, z0, z1, floor, 0f); }
        CagedBulb(g, name + "_Bulb", SewerOrigin + new Vector3((x0 + x1) * 0.5f, floor + height - 0.3f, (z0 + z1) * 0.5f), 1.3f, 6f, cable: 0.4f);
    }

    // Half the width of the doorway in a room's wall: corridor-wide where a corridor ends there, else tunnel-wide.
    static float DoorHalf(int side, float at, float x0, float x1, float z0, float z1)
    {
        var mid = side switch { 0 => new Vector3(at, 0f, z0), 1 => new Vector3(x1, 0f, at), 2 => new Vector3(at, 0f, z1), _ => new Vector3(x0, 0f, at) };
        return corridorEnds.Exists(e => (e - mid).sqrMagnitude < 1f) ? CorridorDoorHalf : TunnelDoorHalf;
    }

    // What makes a room part of the same brick system as the tunnels instead of a plain box: brick pillars
    // standing out from the walls about every 3.5 m (clear of the doorways), a concrete base course along the
    // foot of each wall (broken at the doorways), and concrete beams across the ceiling's shorter span.
    static void RoomStructure(Transform g, string name, float x0, float x1, float z0, float z1, float floor, float top, (int side, float at)[] doors)
    {
        Vector3 o = SewerOrigin;
        for (int side = 0; side < 4; side++)
        {
            bool alongX = side == 0 || side == 2;
            float a0 = alongX ? x0 : z0, a1 = alongX ? x1 : z1, length = a1 - a0;
            // 'in' is how far out from the wall; this gives a box between a0..a1 along the wall, 'from'..'to' out.
            Vector3 Lo(float a, float from, float y) => side switch
            {
                0 => new Vector3(a, y, z0 + from), 2 => new Vector3(a, y, z1 - from), 1 => new Vector3(x1 - from, y, a), _ => new Vector3(x0 + from, y, a),
            };
            void Piece(string part, float a, float b, float from, float to, float yLo, float yHi, Material mat)
            {
                Vector3 p = Lo(a, from, yLo), q = Lo(b, to, yHi);
                Span(name + part, g, o + Vector3.Min(p, q), o + Vector3.Max(p, q), mat);
            }
            var gaps = new List<float>();
            var halves = new List<float>();
            foreach (var d in doors) if (d.side == side) { gaps.Add(d.at); halves.Add(DoorHalf(d.side, d.at, x0, x1, z0, z1)); }
            int count = Mathf.Max(1, Mathf.RoundToInt(length / 3.5f));
            for (int k = 1; k < count; k++)
            {
                float at = a0 + length * k / count;
                bool nearDoor = false;
                for (int i = 0; i < gaps.Count; i++) nearDoor |= Mathf.Abs(gaps[i] - at) < halves[i] + 0.7f;
                if (nearDoor) continue;
                Piece("_Pillar", at - 0.3f, at + 0.3f, 0f, 0.25f, floor, top, sewerBrick);
            }
            // The base course, in runs between the doorways, kept 5 mm off the wall and the floor (never sharing
            // their faces); along z it stops just short of the corners, where the runs along x already are.
            var order = new List<int>();
            for (int i = 0; i < gaps.Count; i++) order.Add(i);
            order.Sort((p, q) => gaps[p].CompareTo(gaps[q]));
            float run = alongX ? a0 : a0 + 0.13f, end = alongX ? a1 : a1 - 0.13f;
            foreach (int i in order) { float d = gaps[i], h = halves[i]; if (d - h > run) Piece("_BaseCourse", run, d - h, 0.005f, 0.125f, floor + 0.005f, floor + 0.5f, sewerConcrete); run = d + h; }
            if (end > run) Piece("_BaseCourse", run, end, 0.005f, 0.125f, floor + 0.005f, floor + 0.5f, sewerConcrete);
        }
        // Beams across the ceiling's shorter span.
        bool acrossZ = x1 - x0 >= z1 - z0;
        float span0 = acrossZ ? x0 : z0, span1 = acrossZ ? x1 : z1;
        int beams = Mathf.Max(1, Mathf.RoundToInt((span1 - span0) / 3.5f));
        for (int k = 1; k < beams; k++)
        {
            float at = span0 + (span1 - span0) * k / beams;
            // (5 mm short of the walls and the ceiling, never sharing their faces)
            if (acrossZ) Span(name + "_Beam", g, o + new Vector3(at - 0.2f, top - 0.4f, z0 + 0.005f), o + new Vector3(at + 0.2f, top - 0.005f, z1 - 0.005f), sewerConcrete);
            else Span(name + "_Beam", g, o + new Vector3(x0 + 0.005f, top - 0.4f, at - 0.2f), o + new Vector3(x1 - 0.005f, top - 0.005f, at + 0.2f), sewerConcrete);
        }
    }

    // Takes away any wall pillar within 'radius' of 'at' (where a valve, a gate or a pump stands against the wall).
    static void ClearPillars(Vector3 at, float radius)
    {
        Physics.SyncTransforms();
        foreach (var hit in Physics.OverlapSphere(at, radius, ~0, QueryTriggerInteraction.Ignore))
            if (hit && hit.name.EndsWith("_Pillar")) Object.DestroyImmediate(hit.gameObject);
    }

    // A sunken room's dry landing: solid from its floor up to the walkways' height over x0..x1, z0..z1.
    // (Any of the room's pillars or base course that would stand inside it come out.)
    static void Landing(Transform g, string name, float floor, float x0, float x1, float z0, float z1)
    {
        Vector3 lo = SewerOrigin + new Vector3(x0, floor, z0), hi = SewerOrigin + new Vector3(x1, 0f, z1);
        Physics.SyncTransforms();
        foreach (var hit in Physics.OverlapBox((lo + hi) * 0.5f, (hi - lo) * 0.5f + Vector3.one * 0.01f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (hit && (hit.name.EndsWith("_Pillar") || hit.name.EndsWith("_BaseCourse"))) Object.DestroyImmediate(hit.gameObject);
        Span(name + "_Landing", g, lo, hi, sewerFloor);
    }

    // Steps from a landing's edge down to a sunken room's floor: 'from' is the middle of the top step's
    // edge on the landing, 'down' the way they descend, 'width' across, 'tread' deep (0.25 m makes a steep
    // service stair). Each step rises no more than a stride can (Sousa steps up 35 cm), so the pit can be
    // walked out of once it's drained.
    static void Steps(Transform g, string name, float floor, Vector3 from, Vector3 down, float width, float tread = 0.32f)
    {
        int count = Mathf.CeilToInt(-floor / 0.33f) - 1;   // the floor itself is the last step
        float rise = -floor / (count + 1);
        Vector3 across = Vector3.Cross(Vector3.up, down) * (width * 0.5f);
        for (int k = 0; k < count; k++)
        {
            Vector3 a = from + down * (tread * k) - across, b = from + down * (tread * (k + 1)) + across;
            Span(name + "_Step" + k, g, SewerOrigin + new Vector3(Mathf.Min(a.x, b.x), floor, Mathf.Min(a.z, b.z)),
                 SewerOrigin + new Vector3(Mathf.Max(a.x, b.x), -rise * (k + 1), Mathf.Max(a.z, b.z)), sewerFloor);
        }
    }

    // A dry maintenance corridor along x from one room's doorway (its wall at x0) to the next (its wall at x1),
    // 4.4 m wide (the rooms' doorways) and 3.6 m tall, centred on z.
    static void Corridor(Transform g, string name, float x0, float x1, float z)
    {
        Vector3 o = SewerOrigin;
        float z0 = z - 2.2f, z1 = z + 2.2f;
        corridorEnds.Add(new Vector3(x0, 0f, z)); corridorEnds.Add(new Vector3(x1, 0f, z));
        Span(name + "_Floor", g, o + new Vector3(x0 + 0.3f, -0.2f, z0), o + new Vector3(x1 - 0.3f, 0f, z1), sewerFloor);
        Span(name + "_Ceiling", g, o + new Vector3(x0 + 0.55f, 3.6f, z0), o + new Vector3(x1 - 0.55f, 3.8f, z1), sewerCeiling);
        Span(name + "_WallL", g, o + new Vector3(x0 + 0.55f, -2.2f, z0 - 0.3f), o + new Vector3(x1 - 0.55f, 3.8f, z0), sewerBrick);
        Span(name + "_WallR", g, o + new Vector3(x0 + 0.55f, -2.2f, z1), o + new Vector3(x1 - 0.55f, 3.8f, z1 + 0.3f), sewerBrick);
        CagedBulb(g, name + "_Bulb", o + new Vector3((x0 + x1) * 0.5f, 3.3f, z), 1f, 5f, flicker: true, cable: 0.2f);
    }

    // An inverted siphon on the ring's left side (x -3..3): the tunnel arrives at the lip of a flooded shaft
    // (6.5 m deep); a low culvert runs under to a second shaft, whose lip leads on to valve 3. The water stands
    // just under the lips, so the culvert is full to its roof: dive down, swim through, come up.
    static void Siphon(Transform g)
    {
        Vector3 o = SewerOrigin;
        const float floor = -6.5f, culvertTop = -4.3f;
        // Shaft A: the tunnel's doorway on its top side, the culvert's opening at the foot of its bottom side.
        // (Each shaft's tunnel doorway is its whole width, so both of the tunnel's walkways lead onto its lip.)
        SewerRoom(g, "SiphonShaftA", -3f, 3f, 80f, 86f, floor, 3.6f, (0, -3f, 3f, 3.6f - floor), (2, -1.5f, 1.5f, culvertTop - floor));
        WallPiece(g, "SiphonShaftA_Threshold", 0, -3f, 3f, -3f, 3f, 80f, 86f, floor, 0f);
        Span("SiphonShaftA_Lip", g, o + new Vector3(-3f, floor, 80f), o + new Vector3(3f, 0f, 81.2f), sewerFloor);
        // The culvert between the shafts' feet.
        Span("SiphonCulvert_Floor", g, o + new Vector3(-1.5f, floor - 0.2f, 86.3f), o + new Vector3(1.5f, floor, 91.7f), sewerFloor);   // between the shafts' sills
        Span("SiphonCulvert_Roof", g, o + new Vector3(-1.5f, culvertTop, 86.55f), o + new Vector3(1.5f, culvertTop + 0.2f, 91.45f), sewerBrick);
        Span("SiphonCulvert_WallL", g, o + new Vector3(-1.8f, floor - 0.2f, 86.55f), o + new Vector3(-1.5f, culvertTop + 0.2f, 91.45f), sewerBrick);
        Span("SiphonCulvert_WallR", g, o + new Vector3(1.5f, floor - 0.2f, 86.55f), o + new Vector3(1.8f, culvertTop + 0.2f, 91.45f), sewerBrick);
        // Shaft B: the culvert's opening at the foot of its top side, the doorway on to valve 3 on its bottom side.
        SewerRoom(g, "SiphonShaftB", -3f, 3f, 92f, 98f, floor, 3.6f, (0, -1.5f, 1.5f, culvertTop - floor), (2, -3f, 3f, 3.6f - floor));
        WallPiece(g, "SiphonShaftB_Threshold", 2, -3f, 3f, -3f, 3f, 92f, 98f, floor, 0f);
        Span("SiphonShaftB_Lip", g, o + new Vector3(-3f, floor, 96.8f), o + new Vector3(3f, 0f, 98f), sewerFloor);
        // Steep service stairs in each shaft, along the lip's edge from the east wall down toward the west, ending
        // on open floor: once valve 3 has drained the siphon it can be walked through (full, they're under water,
        // and the dive is still the way).
        Steps(g, "SiphonShaftA", floor, new Vector3(3f, 0f, 81.8f), Vector3.left, 1.2f, 0.25f);
        Steps(g, "SiphonShaftB", floor, new Vector3(3f, 0f, 96.2f), Vector3.left, 1.2f, 0.25f);
        CagedBulb(g, "SiphonBulbA", o + new Vector3(0f, 3.3f, 83.5f), 1.2f, 7f, flicker: true, cable: 0.2f);
        CagedBulb(g, "SiphonBulbB", o + new Vector3(0f, 3.3f, 94.5f), 1.2f, 7f, cable: 0.2f);
    }

    // The pump room below the ring: the overflow comes in down the plan from the ring and leaves to the right
    // for the river. A deep, fast spillway trench runs right across it, crossed on one narrow grated beam
    // (the way out is beyond it).
    static void PumpRoom(Transform g)
    {
        Vector3 o = SewerOrigin;
        SewerRoom(g, "PumpRoom", 43f, 67f, 125f, 141f, 0f, 6f, false, (0, 52f, 58f, 3.6f), (1, 134f, 140f, 3.6f));
        // The canal from the ring comes in under the south doorway and runs in an open slot into the spillway.
        var oldSills = new List<GameObject>();
        foreach (Transform t in g) if (t.name.StartsWith("PumpRoom_Sill0_")) oldSills.Add(t.gameObject);
        foreach (var s in oldSills) Object.DestroyImmediate(s);
        ChannelSill(g, "PumpRoom_ChannelSill", 0, 52f, 58f, 43f, 67f, 125f, 141f, 0f);
        Span("PumpRoom_SlotFloor", g, o + new Vector3(55f - ChannelHalf, -SewerWalkwayRise - 0.2f, 125f), o + new Vector3(55f + ChannelHalf, -SewerWalkwayRise, 129f), sewerConcrete);
        SewerWaterPlane(g, "PumpRoom_SlotWater", 55f - ChannelHalf, 55f + ChannelHalf, 125f, 129f, ChannelWater);
        ChannelBridge(g, o + new Vector3(55f, 0f, 126.6f), new Vector3(ChannelHalf, 0f, 0.8f));   // a grate over the slot by the doorway
        RoomStructure(g, "PumpRoom", 43f, 67f, 125f, 141f, 0f, 6f, new[] { (0, 55f), (1, 137f) });
        CagedBulb(g, "PumpRoom_BulbA", o + new Vector3(55f, 5.7f, 127f), 1.2f, 8f, cable: 0.4f);
        CagedBulb(g, "PumpRoom_BulbB", o + new Vector3(55f, 5.7f, 137f), 1.2f, 8f, flicker: true, cable: 0.4f);
        Span("PumpRoom_FloorTop", g, o + new Vector3(43f, -3f, 125f), o + new Vector3(55f - ChannelHalf, 0f, 129f), sewerFloor);
        Span("PumpRoom_FloorTop", g, o + new Vector3(55f + ChannelHalf, -3f, 125f), o + new Vector3(67f, 0f, 129f), sewerFloor);
        Span("PumpRoom_FloorBottom", g, o + new Vector3(43f, -3f, 132f), o + new Vector3(67f, 0f, 141f), sewerFloor);
        Span("SpillwayBed", g, o + new Vector3(43f, -3.2f, 129f), o + new Vector3(67f, -3f, 132f), sewerMud);
        Span("SpillwayEndL", g, o + new Vector3(42.7f, -3.2f, 129f), o + new Vector3(43f, -2.8f, 132f), sewerBrick);   // the room's walls stop 2.8 m down
        Span("SpillwayEndR", g, o + new Vector3(67f, -3.2f, 129f), o + new Vector3(67.3f, -2.8f, 132f), sewerBrick);
        for (float z = 129.2f; z < 131.9f; z += 0.3f)   // the culvert grate the spillway drains through (and pins you against)
            Prim(PrimitiveType.Cylinder, "CulvertBar", g, o + new Vector3(66.85f, -1.8f, z), new Vector3(0.06f, 1.2f, 0.06f), pipeRust);
        Span("SpillwayBeam", g, o + new Vector3(49.8f, -0.15f, 129f), o + new Vector3(50.2f, 0f, 132f), sewerGrate);   // no rails
        // Two dead pumps on plinths along the far wall (placeholders).
        foreach (float x in new[] { 47f, 59f })
        {
            ClearPillars(o + new Vector3(x, 1.5f, 140.3f), 2f);
            Span("PumpPlinth", g, o + new Vector3(x - 1.5f, 0f, 138f), o + new Vector3(x + 1.5f, 0.4f, 140.6f), sewerFloor);
            var body = Prim(PrimitiveType.Cylinder, "Pump", g, o + new Vector3(x, 1.4f, 139.3f), new Vector3(1.8f, 1f, 1.8f), pipeSteel);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.AddComponent<MeshCollider>();
            Pipe(g, "PumpRiser", o + new Vector3(x, 2.4f, 139.3f), o + new Vector3(x, 5.8f, 139.3f), 0.25f, pipeRust);
        }
    }

    // A valve on a wall: a rusty pipe up the wall with a big red wheel (a placeholder; turning it comes
    // next). 'wallFloor' is on the floor at the wall, 'inward' points into the room.
    static GameObject Valve(Transform g, string name, Vector3 wallFloor, Vector3 inward)
    {
        Vector3 pipeAt = wallFloor + inward * 0.35f;
        ClearPillars(pipeAt + Vector3.up * 1.3f, 1.1f);
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

    // The cistern: a round shaft 28 m across (16 flat wall sections), from 9 m above the tunnels down to its
    // floor 32 m below them. The four spokes arrive at doorways on a 2 m gallery round its top, a mesh guard
    // along its edge; each spoke's canal carries on under the gallery and pours out of the wall as a waterfall
    // (the falls themselves: LookTestBuilder.SewerWater.cs). A dark steel catwalk stair spirals down inside
    // the gallery, hung off the wall on brackets, with landings every 13 steps. At the bottom, a low round
    // plinth with the final valve, and the closed outfall gate in the wall (the purge goes out through it).
    static readonly List<(Vector3 lip, Vector3 inward)> cisternSpouts = new List<(Vector3, Vector3)>();

    static void Cistern(Transform parent)
    {
        var g = new GameObject("Cistern").transform;
        g.SetParent(parent);
        cisternSpouts.Clear();
        Vector3 c = SewerOrigin + CisternCentre;
        const float R = CisternRadius, top = 9f, floor = CisternFloor, bottom = CisternFloor - 0.5f, galleryIn = 12f, stairIn = 10.25f;
        float half = Mathf.Tan(11.25f * Mathf.Deg2Rad);
        float side = 2f * (R + 0.4f) * half + 0.05f;   // one wall section's width
        float doorHalf = side * 0.5f - 0.12f;          // a spoke's doorway takes its whole section but for slim posts
        const float stairStart = 196f;                 // where the stairs leave the gallery (just past the top doorway)
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            var rot = Quaternion.Euler(0f, a, 0f);
            Vector3 d = rot * Vector3.forward, across = rot * Vector3.right;
            Vector3 wallMid = c + d * (R + 0.2f);
            if (i % 4 == 0)
            {
                foreach (float s in new[] { -1f, 1f })
                    Turned(Box("CisternWall_" + i, g, wallMid + across * s * (doorHalf + 0.06f) + Vector3.up * (top + bottom) * 0.5f, new Vector3(0.12f, top - bottom, 0.4f), sewerBrick), rot);
                Turned(Box("CisternLintel_" + i, g, wallMid + Vector3.up * (3.6f + (top - 3.6f) * 0.5f), new Vector3(doorHalf * 2f, top - 3.6f, 0.4f), sewerBrick), rot);
                // The sill below the doorway, with the canal's mouth in it (1 cm under the tunnel's walkway).
                float mouth = -SewerWalkwayRise;
                Turned(Box("CisternSill_" + i, g, wallMid + Vector3.up * (bottom + mouth) * 0.5f, new Vector3(doorHalf * 2f, mouth - bottom, 0.4f), sewerBrick), rot);
                float cheek = doorHalf - ChannelHalf;
                foreach (float s in new[] { -1f, 1f })
                    Turned(Box("CisternSill_" + i, g, wallMid + across * s * (ChannelHalf + cheek * 0.5f) + Vector3.up * (mouth - 0.01f) * 0.5f, new Vector3(cheek, -0.01f - mouth, 0.4f), sewerBrick), rot);
                // A short concrete lip under the gallery that the canal spills off.
                Turned(Box("CisternSpout_" + i, g, c + d * (R - 0.3f) + Vector3.up * (mouth - 0.1f), new Vector3(ChannelHalf * 2f, 0.2f, 0.6f), sewerConcrete), rot);
                cisternSpouts.Add((c + d * (R - 0.6f) + Vector3.up * ChannelWater, -d));
            }
            else Turned(Box("CisternWall_" + i, g, wallMid + Vector3.up * (top + bottom) * 0.5f, new Vector3(side, top - bottom, 0.4f), sewerBrick), rot);
            // The gallery (every other slab 4 mm lower: no flicker).
            Turned(Box("CisternGallery_" + i, g, c + d * ((R + galleryIn) * 0.5f) + Vector3.up * (-0.15f - (i % 2) * 0.004f), new Vector3(2f * R * half + 0.05f, 0.3f, R - galleryIn), sewerFloor), rot);
        }
        // The gallery's mesh guard all the way round, open only at the head of the stairs.
        const float headWidth = 4f;   // degrees: about 0.8 m at the guard
        GuardArc(g, c, galleryIn + 0.05f, 0f, stairStart + headWidth, stairStart + 360f);
        CatwalkStairs(g, c, stairStart, stairIn, galleryIn, floor, R);
        Span("CisternFloor", g, c + new Vector3(-R - 0.5f, floor - 0.2f, -R - 0.5f), c + new Vector3(R + 0.5f, floor, R + 0.5f), sewerFloor);
        Span("CisternCeiling", g, c + new Vector3(-R - 1f, top, -R - 1f), c + new Vector3(R + 1f, top + 0.3f, R + 1f), sewerBrick);
        // The plinth and the final valve on it.
        var plinth = Prim(PrimitiveType.Cylinder, "CentralPlinth", g, c + Vector3.up * (floor + 0.25f), new Vector3(6f, 0.25f, 6f), sewerFloor);
        Object.DestroyImmediate(plinth.GetComponent<Collider>());
        plinth.AddComponent<MeshCollider>();
        Pipe(g, "FinalValve_Riser", c + Vector3.up * floor, c + Vector3.up * (floor + 1.6f), 0.3f, pipeRust);
        valveWheels["FinalValve"] = Prim(PrimitiveType.Cylinder, "FinalValve_Wheel", g, c + Vector3.up * (floor + 1.65f), new Vector3(1.4f, 0.04f, 1.4f), Mat("ValveRed", null, new Color(0.5f, 0.12f, 0.08f), 1f, 0.4f, 0.4f));
        // The outfall gate low in the wall on the pump room's side (shut until the final valve).
        Turned(Box("CisternOutfallGate", g, c + Vector3.forward * (R - 0.05f) + Vector3.up * (floor + 1.6f), new Vector3(3.6f, 3.2f, 0.1f), Mat("FloodBarrier", Tex2(SewerTex + "Metal.jpg"), new Color(0.5f, 0.38f, 0.3f), 1f, 0.3f, 0.6f)), Quaternion.identity);
        // Light: dim emergency lamps on the gallery and at the bottom (floodlights come with the breaker).
        for (int i = 0; i < 4; i++)
        {
            var dd = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward;
            EmergencyLight(g, c + dd * (R - 0.3f) + Vector3.up * 2.6f, 1.6f, 14f);
        }
        CagedBulb(g, "CisternBulb_Bottom", c + Vector3.up * (floor + 4f), 1.8f, 14f, cable: 0f);
    }

    // The cistern's stair: a dark steel catwalk spiralling down the inside of the gallery from 'startAngle'
    // (degrees, 0 = +z, turning toward +x) to the floor, between radius 'rIn' and 'rOut'. 31 cm steps of open
    // steel tread on two stringers, a landing every 13 steps, a handrail and mid rail along the open shaft side,
    // and brackets back to the wall every few steps (none where a waterfall comes down). The wall side has no
    // rail: a swimmer in the cistern climbs out onto the steps from there, whatever the water's height.
    static void CatwalkStairs(Transform g, Vector3 c, float startAngle, float rIn, float rOut, float floor, float wallR)
    {
        var steel = Mat("CatwalkSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.2f, 0.2f, 0.22f), 1f, 0.35f, 0.7f);
        var tread = Mat("CatwalkTread", Tex2(SewerTex + "Metal.jpg"), new Color(0.27f, 0.27f, 0.28f), 1f, 0.3f, 0.7f);
        const int steps = 105;                        // the floor is the last one
        const float stepAngle = 1.96f, landingAngle = 8f;
        float rise = -floor / steps, angle = startAngle;
        float[] rails = { rIn + 0.04f, rOut - 0.04f };
        var last = new Vector3[2, 3];                 // the previous step's points: [side, stringer/mid rail/handrail]
        bool first = true;
        for (int k = 0; k < steps - 1; k++)
        {
            float stepTop = -rise * (k + 1);
            bool landing = k % 13 == 12;
            float span = landing ? landingAngle : stepAngle;
            var rot = Quaternion.Euler(0f, angle + span * 0.5f, 0f);
            Vector3 fwd = rot * Vector3.forward;
            float midR = (rIn + rOut) * 0.5f, width = 2f * midR * Mathf.Tan(span * 0.5f * Mathf.Deg2Rad) + 0.04f;
            Turned(Box(landing ? "CatwalkLanding" : "CatwalkTread", g, c + fwd * midR + Vector3.up * (stepTop - 0.025f), new Vector3(width, 0.05f, rOut - rIn), tread), rot);
            // Stringers under both edges, and the rails above them, joined from the last step to this one.
            for (int sd = 0; sd < 2; sd++)
            {
                Vector3 edge = c + fwd * rails[sd];
                var now = new[] { edge + Vector3.up * (stepTop - 0.13f), edge + Vector3.up * (stepTop + 0.5f), edge + Vector3.up * (stepTop + 1.0f) };
                if (!first)
                    for (int r = 0; r < (sd == 0 ? 3 : 1); r++)
                        Beam(g, r == 0 ? "CatwalkStringer" : "CatwalkRail", last[sd, r], now[r], r == 0 ? new Vector2(0.06f, 0.2f) : new Vector2(0.045f, 0.045f), steel);
                for (int r = 0; r < 3; r++) last[sd, r] = now[r];
                if (sd == 0 && (k % 3 == 0 || landing))   // a post
                    Box("CatwalkPost", g, edge + Vector3.up * (stepTop + 0.5f), new Vector3(0.05f, 1f, 0.05f), steel).transform.rotation = rot;
            }
            first = false;
            // A bracket back to the wall under this step: an arm under the tread and a strut below it.
            float spoutGap = Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 90f) * 90f));
            if (k % 4 == 1 && spoutGap > 12f)
            {
                Vector3 wall = c + fwd * (wallR - 0.01f);
                Beam(g, "CatwalkBracket", c + fwd * rIn + Vector3.up * (stepTop - 0.3f), wall + Vector3.up * (stepTop - 0.3f), new Vector2(0.08f, 0.12f), steel);
                Beam(g, "CatwalkStrut", c + fwd * (rIn + 0.6f) + Vector3.up * (stepTop - 0.34f), wall + Vector3.up * (stepTop - 1.8f), new Vector2(0.07f, 0.07f), steel);
            }
            if (landing && k % 26 == 12)   // a dim lamp on the wall by every other landing
                EmergencyLight(g, c + fwd * (wallR - 0.3f) + Vector3.up * (stepTop + 2.2f), 1.4f, 10f);
            angle += span;
        }
    }

    // A straight steel member from 'a' to 'b' with a 'section' (width, height) cross-section.
    static void Beam(Transform g, string name, Vector3 a, Vector3 b, Vector2 section, Material mat)
    {
        var go = Box(name, g, (a + b) * 0.5f, new Vector3(section.x, section.y, (b - a).magnitude + 0.02f), mat);
        go.transform.rotation = Quaternion.LookRotation(b - a);
    }

    static GameObject Turned(GameObject go, Quaternion rot) { go.transform.rotation = rot; return go; }

    // A 1.1 m mesh guard on a circle of radius 'r' round 'c', standing on 'floor', from angle a0 to a1 (degrees,
    // 0 = +z, turning toward +x), in short straight pieces so it follows the curve.
    static void GuardArc(Transform g, Vector3 c, float r, float floor, float a0, float a1)
    {
        int pieces = Mathf.CeilToInt((a1 - a0) / 4f);
        float step = (a1 - a0) / pieces, width = 2f * r * Mathf.Tan(step * 0.5f * Mathf.Deg2Rad) + 0.02f;
        for (int k = 0; k < pieces; k++)
        {
            var rot = Quaternion.Euler(0f, a0 + step * (k + 0.5f), 0f);
            Turned(Box("GuardArc", g, c + rot * Vector3.forward * r + Vector3.up * (floor + 0.55f), new Vector3(width, 1.1f, 0.06f), sewerGrate), rot);
        }
    }

    // A grated steel plate 10 cm thick, its top level with the walkways, 'half' metres out from 'mid' each way.
    static void ChannelBridge(Transform g, Vector3 mid, Vector3 half)
    {
        half = new Vector3(Mathf.Abs(half.x), 0f, Mathf.Abs(half.z));
        Span("ChannelBridge", g, mid - half - new Vector3(0f, 0.1f, 0f), mid + half, sewerGrate);
    }

    // The end of the overflow tunnel: a wall with a barred opening onto the night and the river (the way
    // out once the water is purged). The grate spans the whole tunnel and reaches down to the canal's floor,
    // so there's no slipping round or under it.
    static Transform RiverOutlet(Transform g, Vector3 at, Material bars, float side = 1f)
    {
        // 'side': +1 when the tunnel comes from +x (the outlet faces -x), -1 when it comes from -x.
        Span("RiverOutletWall", g, at + new Vector3(Mathf.Min(-0.3f * side, 0f), -SewerWalkwayRise - 0.3f, -4.8f), at + new Vector3(Mathf.Max(-0.3f * side, 0f), 7.5f, 4.8f), sewerBrick);
        var night = Prim(PrimitiveType.Quad, "RiverNight", g, at + new Vector3(0.02f * side, 1.9f, 0f), new Vector3(8f, 3.8f, 1f), EmissiveMat("RiverNight", new Color(0.12f, 0.16f, 0.24f)));
        night.transform.rotation = Quaternion.LookRotation(Vector3.left * side);   // its picture faces back up the tunnel
        Object.DestroyImmediate(night.GetComponent<Collider>());
        var grate = new GameObject("RiverGrate").transform;   // lifted as one by RiverExit
        grate.SetParent(g);
        float barLow = -SewerWalkwayRise, barHigh = 3.8f;   // from the canal's floor to above head height
        for (float z = -4.2f; z <= 4.21f; z += 0.3f)
            Prim(PrimitiveType.Cylinder, "GrateBar", grate, at + new Vector3(0.4f * side, (barLow + barHigh) * 0.5f, z), new Vector3(0.06f, (barHigh - barLow) * 0.5f, 0.06f), bars);
        Span("GrateRail", grate, at + new Vector3(Mathf.Min(0.35f * side, 0.45f * side), 3.6f, -4.3f), at + new Vector3(Mathf.Max(0.35f * side, 0.45f * side), 3.75f, 4.3f), bars);
        var moon = new GameObject("RiverMoonlight").AddComponent<Light>();
        moon.transform.SetParent(g);
        moon.transform.position = at + new Vector3(1.5f * side, 2f, 0f);
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
        var headwalls = new List<Bounds>();   // headwall pieces placed so far, so two doorways near a corner don't both fill it
        if (floor) Span(name + "_Floor", g, o + new Vector3(x0, y0 - 0.2f, z0), o + new Vector3(x1, y0, z1), sewerFloor);
        Span(name + "_Ceiling", g, o + new Vector3(x0, y1, z0), o + new Vector3(x1, y1 + 0.2f, z1), sewerCeiling);
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
                if (c.from > at) WallPiece(g, name + "_Wall" + side + "_" + n++, side, at, c.from, x0, x1, z0, z1, y0 - 2.8f, y1 + 0.2f);
                if (c.top < y1 - y0) WallPiece(g, name + "_Lintel" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0 + c.top, y1 + 0.2f);
                // A sill level with the floor below the doorway. Under a tunnel's doorway (6 m wide) in a room at the
                // walkways' height, it has a hole where the tunnel's canal carries on under the floor, in a culvert.
                if (c.to - c.from > 5f && floor && Mathf.Approximately(y0, 0f))
                {
                    ChannelSill(g, name + "_Sill" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0);
                    Culvert(g, name + "_Culvert" + side + "_" + n++, side, (c.from + c.to) * 0.5f, x0, x1, z0, z1);
                }
                else WallPiece(g, name + "_Sill" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0 - 2.8f, y0);
                Headwall(g, name + "_Headwall" + side + "_" + n++, side, c.from, c.to, c.top, x0, x1, z0, z1, y0, headwalls);
                at = c.to;
            }
            if (at < a1) WallPiece(g, name + "_Wall" + side + "_" + n, side, at, a1, x0, x1, z0, z1, y0 - 2.8f, y1 + 0.2f);
        }
    }

    // A brick end-wall just outside a doorway, across the whole round tunnel that meets it (out to 2.6 m past
    // the doorway each side, up to 7.5 m above the floor), with the doorway left open: it closes the tunnel's
    // end where the room is lower or narrower than the tunnel. Stretches already filled by an earlier headwall
    // ('placed', e.g. the other doorway's near a corner) are left out, so the two meet edge to edge.
    static void Headwall(Transform g, string name, int side, float from, float to, float top, float x0, float x1, float z0, float z1, float y0, List<Bounds> placed)
    {
        Vector3 o = SewerOrigin;
        const float inner = 0.3f, outer = 0.55f, reach = 2.6f;   // 25 cm thick, just beyond the room's own wall
        float lo = y0 - 2.8f, hi = Mathf.Max(y0, 0f) + 7.5f;   // up past the tunnel's crown, even for a sunken room
        int run = side == 0 || side == 2 ? 0 : 2;   // the axis the headwall runs along (x or z)
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
            // Cut out of [min, max] along the run axis every earlier piece that fills its whole cross-section there.
            var cuts = new List<(float from, float to)>();
            foreach (var p in placed)
            {
                bool covers = true;
                for (int k = 0; k < 3; k++)
                    if (k != run && (p.min[k] > min[k] + 0.001f || p.max[k] < max[k] - 0.001f)) covers = false;
                if (covers && p.max[run] > min[run] && p.min[run] < max[run]) cuts.Add((p.min[run], p.max[run]));
            }
            cuts.Sort((p, q) => p.from.CompareTo(q.from));
            float at = min[run];
            foreach (var c in cuts)
            {
                if (c.from > at) SpanAlong(name, g, o, min, max, run, at, c.from);
                at = Mathf.Max(at, c.to);
            }
            if (at < max[run]) SpanAlong(name, g, o, min, max, run, at, max[run]);
            placed.Add(new Bounds((min + max) * 0.5f, max - min));
        }
    }

    // The part of the box [min, max] between 'from' and 'to' along axis 'run'.
    static void SpanAlong(string name, Transform g, Vector3 o, Vector3 min, Vector3 max, int run, float from, float to)
    {
        min[run] = from; max[run] = to;
        Span(name, g, o + min, o + max, sewerBrick);
    }

    // A doorway's sill with a hole in its middle where the tunnel's canal (2.6 m wide, its floor SewerWalkwayRise
    // down) goes on under the room.
    static void ChannelSill(Transform g, string name, int side, float from, float to, float x0, float x1, float z0, float z1, float y0)
    {
        float mid = (from + to) * 0.5f;
        WallPiece(g, name, side, from, mid - ChannelHalf, x0, x1, z0, z1, y0 - 2.8f, y0);
        WallPiece(g, name, side, mid + ChannelHalf, to, x0, x1, z0, z1, y0 - 2.8f, y0);
        WallPiece(g, name, side, mid - ChannelHalf, mid + ChannelHalf, x0, x1, z0, z1, y0 - 2.8f, y0 - SewerWalkwayRise);
    }

    // The canal's culvert under a room's floor, from the doorway at 'at' on 'side' 4 m in: a concrete trough
    // (the room's floor is its roof), the water in it, and a rusty grate at its far end where it drops away.
    static void Culvert(Transform g, string name, int side, float at, float x0, float x1, float z0, float z1)
    {
        Vector3 o = SewerOrigin;
        Vector3 inward = side switch { 0 => Vector3.forward, 1 => Vector3.left, 2 => Vector3.back, _ => Vector3.right };
        Vector3 across = Vector3.Cross(Vector3.up, inward);
        Vector3 door = side switch { 0 => new Vector3(at, 0f, z0), 1 => new Vector3(x1, 0f, at), 2 => new Vector3(at, 0f, z1), _ => new Vector3(x0, 0f, at) };
        const float length = 4f;
        float bottom = -SewerWalkwayRise, roof = -0.205f;   // 5 mm under the room's floor slab
        void Block(string part, float a0, float a1, float s0, float s1, float lo, float hi, Material mat)
        {
            Vector3 p = door + inward * a0 + across * s0, q = door + inward * a1 + across * s1;
            Span(name + part, g, o + new Vector3(Mathf.Min(p.x, q.x), lo, Mathf.Min(p.z, q.z)), o + new Vector3(Mathf.Max(p.x, q.x), hi, Mathf.Max(p.z, q.z)), mat);
        }
        Block("_Floor", 0f, length, -ChannelHalf, ChannelHalf, bottom - 0.2f, bottom, sewerConcrete);
        Vector3 inside = Abs(inward) * length + Abs(across) * (ChannelHalf * 2f) + Vector3.up * (ChannelWater - bottom);
        culvertAreas.Add(new Bounds(o + door + inward * (length * 0.5f) + Vector3.up * ((bottom + ChannelWater) * 0.5f), inside));
        Block("_WallL", 0f, length, -ChannelHalf - 0.3f, -ChannelHalf, bottom - 0.2f, roof, sewerConcrete);
        Block("_WallR", 0f, length, ChannelHalf, ChannelHalf + 0.3f, bottom - 0.2f, roof, sewerConcrete);
        Block("_End", length, length + 0.3f, -ChannelHalf - 0.3f, ChannelHalf + 0.3f, bottom - 0.2f, roof, sewerBrick);
        for (float s = -ChannelHalf + 0.15f; s < ChannelHalf; s += 0.3f)   // the grate the water drops away through
        {
            var bar = Prim(PrimitiveType.Cylinder, name + "_Bar", g, o + door + inward * (length - 0.4f) + across * s + Vector3.up * ((bottom + roof) * 0.5f),
                           new Vector3(0.05f, (roof - bottom) * 0.5f, 0.05f), pipeRust);
            Object.DestroyImmediate(bar.GetComponent<Collider>());
        }
        var water = Prim(PrimitiveType.Quad, name + "_Water", g, o + door + inward * (length * 0.5f) + Vector3.up * ChannelWater, new Vector3(ChannelHalf * 2f, length, 1f), sewerWater);
        water.transform.rotation = Quaternion.LookRotation(Vector3.down, inward);   // lying flat, facing up
        Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

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
