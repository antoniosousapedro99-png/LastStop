using System.Collections.Generic;
using UnityEngine;

// Dressing and wayfinding for the sewers (positions relative to the sewer origin, walkways at height 0):
// - painted signs on the tunnel wall before every junction (on your right as you walk in), arrows to what's
//   ahead, left and right; signs beside every room doorway naming where it leads;
// - each valve chamber has its colour (1 yellow, 2 blue, 3 green, 4 red), a painted stripe and a big number
//   on its valve wall, and the signs name the valves in those colours;
// - the control room's wall schematic of the whole system ("you are here");
// - things in the rooms (pipe manifolds and gauges by the valves, cabinets and conduits in the electrical
//   room, a desk in the control room) and the Sewer pack's own rubbish along the tunnels.
public static partial class LookTestBuilder
{
    static readonly Dictionary<string, Color> sewerColours = new Dictionary<string, Color>
    {
        { "VALVE 1", new Color(0.85f, 0.7f, 0.23f) }, { "VALVE 2", new Color(0.33f, 0.5f, 0.82f) },
        { "VALVE 3", new Color(0.33f, 0.62f, 0.38f) }, { "VALVE 4", new Color(0.75f, 0.27f, 0.22f) },
    };
    static readonly Color SignPaint = new Color(0.9f, 0.87f, 0.78f);   // cream stencil paint

    static void SewerDressing(Transform parent)
    {
        var g = new GameObject("Dressing").transform;
        g.SetParent(parent);
        Vector3 up = Vector3.back, down = Vector3.forward, left = Vector3.left, right = Vector3.right;   // on the plan

        // ---- junction signs
        JunctionSigns(g, new Vector3(25f, 0f, 30f), (up, "CONTROL ROOM"), (left, "ACCESS SHAFT · VALVE 1"), (right, "CISTERN · VALVE 2"));
        JunctionSigns(g, new Vector3(40f, 0f, 30f), (down, "CISTERN"), (left, "CONTROL ROOM · VALVE 1"), (right, "VALVE 2"));
        JunctionSigns(g, new Vector3(0f, 0f, 70f), (right, "CISTERN"), (up, "VALVE 1 · ACCESS SHAFT"), (down, "SIPHON · VALVE 3"));
        JunctionSigns(g, new Vector3(80f, 0f, 70f), (left, "CISTERN"), (up, "VALVE 2"), (down, "VALVE 4"));
        JunctionSigns(g, new Vector3(40f, 0f, 110f), (up, "CISTERN"), (left, "VALVE 3"), (right, "PUMP ROOM · VALVE 4"));
        JunctionSigns(g, new Vector3(55f, 0f, 110f), (down, "PUMP ROOM · RIVER"), (left, "CISTERN · VALVE 3"), (right, "VALVE 4"));

        // ---- door signs (room x0, x1, z0, z1; the door's side and middle; where it leads)
        DoorSign(g, -3f, 3f, 0f, 8f, 2, 0f, "VALVE 1\nTHE RING");
        DoorSign(g, -3f, 3f, 0f, 8f, 1, 5f, "CONTROL\nROOM");
        DoorSign(g, 18f, 32f, 0f, 10f, 3, 5f, "ACCESS\nSHAFT");
        DoorSign(g, 18f, 32f, 0f, 10f, 1, 5f, "ELECTRICAL\nROOM");
        DoorSign(g, 18f, 32f, 0f, 10f, 2, 25f, "THE RING\nCISTERN");
        DoorSign(g, 40f, 54f, 0f, 10f, 3, 5f, "CONTROL\nROOM");
        DoorSign(g, -7f, 7f, 23f, 37f, 0, 0f, "ACCESS\nSHAFT");
        DoorSign(g, -7f, 7f, 23f, 37f, 1, 30f, "CISTERN\nVALVE 2");
        DoorSign(g, -7f, 7f, 23f, 37f, 2, 0f, "SIPHON\nVALVE 3");
        DoorSign(g, 71f, 89f, 21f, 39f, 3, 30f, "CISTERN\nVALVE 1");
        DoorSign(g, 71f, 89f, 21f, 39f, 2, 80f, "CISTERN\nVALVE 4");
        DoorSign(g, -9f, 9f, 101f, 119f, 0, 0f, "SIPHON\nVALVE 1");
        DoorSign(g, -9f, 9f, 101f, 119f, 1, 110f, "CISTERN\nPUMP ROOM");
        DoorSign(g, 73f, 87f, 103f, 117f, 0, 80f, "CISTERN\nVALVE 2");
        DoorSign(g, 73f, 87f, 103f, 117f, 3, 110f, "PUMP ROOM\nVALVE 3");
        DoorSign(g, 43f, 67f, 125f, 141f, 0, 55f, "THE RING\nCISTERN");
        DoorSign(g, 43f, 67f, 125f, 141f, 1, 137f, "RIVER\nOUTFALL");

        // ---- the valve chambers' colours and numbers (on the valve's wall)
        ChamberMark(g, "VALVE 1", "1", new Vector3(-7f, 0f, 30f), right, 14f, 0f);
        ChamberMark(g, "VALVE 2", "2", new Vector3(89f, -2.2f, 30f), left, 18f, -2.2f);
        ChamberMark(g, "VALVE 3", "3", new Vector3(-9f, 0f, 110f), right, 18f, 0f);
        ChamberMark(g, "VALVE 4", "4", new Vector3(87f, 0f, 110f), left, 14f, 0f);

        // ---- the control room: the schematic on its top wall, a desk and chair facing it
        Schematic(g, SewerOrigin + new Vector3(25f, 1.9f, 0.05f));
        var desk = Model(ArtDir + "/Props/Office/desk1_weathered.fbx", null, g, Tex2("Props/Office/desk_1_old.jpg"), 1f, 0f);
        StandOn(desk, SewerOrigin + new Vector3(25f, 0f, 3.6f));
        desk.AddComponent<MeshCollider>();
        var chair = Model(ArtDir + "/Props/Office/office_chair_black.fbx", null, g, Tex2("Props/Office/office_chair_black.jpg"), 1f, 160f);
        StandOn(chair, SewerOrigin + new Vector3(25.6f, 0f, 5.3f));
        RustyCabinet(g, "ControlConsole", SewerOrigin + new Vector3(19.4f, 0.75f, 8.6f), new Vector3(2.2f, 1.5f, 0.6f), Vector3.right);

        // ---- the electrical room: a row of cabinets with conduits to the ceiling, the main breaker cabinet
        foreach (float z in new[] { 1.6f, 3.4f, 6.6f, 8.4f })
        {
            ClearPillars(SewerOrigin + new Vector3(53.7f, 1f, z), 1f);
            ElectricCabinet(g, SewerOrigin + new Vector3(54f, 0f, z), Vector3.left, 1.4f, 1.9f, 0.5f);
            Pipe(g, "Conduit", SewerOrigin + new Vector3(53.7f, 1.9f, z), SewerOrigin + new Vector3(53.7f, 4f, z), 0.05f, conduit);
        }
        ClearPillars(SewerOrigin + new Vector3(47f, 1.2f, 0.3f), 1.4f);
        RustyCabinet(g, "MainBreaker", SewerOrigin + new Vector3(47f, 1.2f, 0.36f), new Vector3(1.6f, 2.4f, 0.7f), Vector3.forward);
        WallSign(g, SewerOrigin + new Vector3(47f, 2.8f, 3f), Vector3.back, "MAIN BREAKER", SignPaint, 1.8f);

        // ---- pipe manifolds and a gauge by each valve
        foreach (var (at, inward) in new[] { (new Vector3(-7f, 0f, 30f), right), (new Vector3(89f, -2.2f, 30f), left), (new Vector3(-9f, 0f, 110f), right), (new Vector3(87f, 0f, 110f), left) })
        {
            Vector3 along = Vector3.Cross(Vector3.up, inward), p = SewerOrigin + at + inward * 0.35f + Vector3.up * 2.9f;
            PipeRun(g, "Manifold", 0.14f, pipeRust, p, p + along * 2.4f, p + along * 2.4f - inward * 0.6f);
            var gauge = Prim(PrimitiveType.Cylinder, "Gauge", g, p + Vector3.down * 1.1f + inward * 0.25f + along * 0.45f, new Vector3(0.22f, 0.03f, 0.22f), Mat("GaugeFace", null, new Color(0.85f, 0.83f, 0.76f), 1f, 0.3f));
            gauge.transform.rotation = Quaternion.FromToRotation(Vector3.up, inward);
            Object.DestroyImmediate(gauge.GetComponent<Collider>());
        }

        // ---- the Sewer pack's own rubbish and rubble: piles in the rooms' corners, odd things along the tunnels
        foreach (var (piece, x, z, y, yaw, size) in new[]
        {
            ("Bricks", 5.5f, 25f, 0f, 30f, 0.7f), ("debris_02", -5f, 35f, 0f, 70f, 2.2f), ("Garbage_bag", 5.8f, 35.6f, 0f, 0f, 0.6f),
            ("debris_03", 87f, 23f, -2.2f, 10f, 2.4f), ("Box_T_01", 72f, 37.6f, 0f, 20f, 0.7f),
            ("debris_04", -7f, 117f, 0f, 200f, 2.6f), ("Bricks", 7.5f, 103f, 0f, 120f, 0.7f), ("Tire", -7.6f, 103.5f, 0f, 0f, 0.95f),
            ("Garbage_bag", 85.5f, 115.5f, 0f, 60f, 0.6f), ("debris_05", 75f, 105f, 0f, 300f, 2.2f),
            ("Trash_M", 44f, 7.5f, 0f, 90f, 1.6f), ("Box_T_02", 21f, 1.2f, 0f, 10f, 0.9f), ("Garbage_bag", -2.3f, 6.8f, 0f, 0f, 0.55f),
            ("Bricks", 15f, 32.7f, 0f, 0f, 0.7f), ("Garbage_bag", 58f, 27.3f, 0f, 0f, 0.6f), ("Bricks", 82.7f, 52f, 0f, 90f, 0.7f),
            ("Box_T_03", 25f, 112.7f, 0f, 0f, 0.8f), ("Garbage_bag", 65f, 107.3f, 0f, 0f, 0.6f), ("Bricks", -2.7f, 50f, 0f, 45f, 0.7f),
            ("Tire", 0f, 58f, -0.77f, 0f, 0.95f), ("Tire", 80f, 90f, -0.77f, 0f, 0.95f), ("debris_02", 63f, 139f, 0f, 30f, 2f),
        })
        {
            var prop = Model(SewerKitFbx, piece, g, null, 1f, yaw, sewerKitTextures);
            var b = Bounds(prop);
            prop.transform.localScale *= size / Mathf.Max(b.size.x, b.size.z, 0.01f);
            StandOn(prop, SewerOrigin + new Vector3(x, y, z));
        }
    }

    // Signs on the tunnel wall before a junction, one for each way in: on the right-hand wall as you walk
    // toward the junction, 'what's that way' for each other way out (ahead, left or right).
    static void JunctionSigns(Transform g, Vector3 at, params (Vector3 way, string to)[] ways)
    {
        foreach (var from in ways)
        {
            Vector3 heading = -from.way, rightHand = Vector3.Cross(Vector3.up, heading);
            var lines = new List<string>();
            foreach (var other in ways)
            {
                if (other.way == from.way) continue;
                string arrow = other.way == heading ? "▲" : other.way == rightHand ? "►" : "◄";
                lines.Add(Coloured(arrow + " " + other.to));
            }
            Vector3 stand = SewerOrigin + at + from.way * 6f + Vector3.up * 1.8f;
            WallSign(g, stand, rightHand, string.Join("\n", lines), SignPaint, 2.6f);
        }
    }

    // A sign beside a room's doorway (on its inside wall, just right of the door as you face it), naming
    // where the doorway leads, with an arrow toward the door.
    static void DoorSign(Transform g, float x0, float x1, float z0, float z1, int side, float at, string text)
    {
        Vector3 outward = side switch { 0 => Vector3.back, 1 => Vector3.right, 2 => Vector3.forward, _ => Vector3.left };
        Vector3 doorMid = side switch { 0 => new Vector3(at, 0f, z0), 1 => new Vector3(x1, 0f, at), 2 => new Vector3(at, 0f, z1), _ => new Vector3(x0, 0f, at) };
        Vector3 rightHand = Vector3.Cross(Vector3.up, outward);
        // 2.75 m to the side of the door, or less in a narrow room so the plate stays clear of the corner.
        float room = side switch { 0 => at - x0, 2 => x1 - at, 1 => at - z0, _ => z1 - at };   // room left on the right-hand side
        Vector3 stand = SewerOrigin + doorMid - outward * 1f + rightHand * Mathf.Min(2.75f, room - 0.6f) + Vector3.up * 1.9f;
        WallSign(g, stand, outward, Coloured("◄ " + text.Replace("\n", "\n   ")), SignPaint, 1.1f);
    }

    // A painted sign plate on the first wall along 'toward' from 'stand' (the tunnels' curved walls too),
    // its words in 'colour' (rich text colours win), 'width' metres wide.
    static void WallSign(Transform g, Vector3 stand, Vector3 toward, string text, Color colour, float width)
    {
        Physics.SyncTransforms();
        if (!Physics.Raycast(stand, toward, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) { Debug.LogWarning("Sewer sign found no wall: " + text); return; }
        int lines = text.Split('\n').Length;
        var plate = Box("SewerSign", g, hit.point + hit.normal * 0.03f, new Vector3(width, 0.22f + 0.2f * lines, 0.04f), Mat("SignPaintedPlate", null, new Color(0.13f, 0.14f, 0.13f), 1f, 0.2f));
        plate.transform.rotation = Quaternion.LookRotation(-hit.normal);   // its back to the wall; text faces out
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        Label(plate.transform, text, new Vector3(0f, 0f, -0.03f), 0.022f, colour, 0f);
    }

    // Wraps each valve name in a sign's text in that valve's colour.
    static string Coloured(string text)
    {
        foreach (var kv in sewerColours)
            text = text.Replace(kv.Key, "<color=#" + ColorUtility.ToHtmlStringRGB(kv.Value) + ">" + kv.Key + "</color>");
        return text;
    }

    // A valve chamber's colour stripe across its valve wall and its big painted number above the valve.
    static void ChamberMark(Transform g, string valve, string number, Vector3 wallFloor, Vector3 inward, float wallLength, float floor)
    {
        var colour = sewerColours[valve];
        Vector3 along = Vector3.Cross(Vector3.up, inward);
        Vector3 c = SewerOrigin + wallFloor + inward * 0.008f + Vector3.up * (1.75f);
        var stripe = Box("ChamberStripe", g, c, new Vector3(wallLength - 0.4f, 0.25f, 0.006f), Mat("ChamberPaint_" + number, null, colour, 1f, 0.2f));
        stripe.transform.rotation = Quaternion.LookRotation(-inward);
        Object.DestroyImmediate(stripe.GetComponent<Collider>());
        var holder = new GameObject("ChamberNumber").transform;
        holder.SetParent(g);
        holder.SetPositionAndRotation(SewerOrigin + wallFloor + inward * 0.03f + along * 1.6f + Vector3.up * 3.2f, Quaternion.LookRotation(-inward));
        Label(holder, number, Vector3.zero, 0.12f, colour, 0f);
    }

    // The control room's wall schematic: the ring, the spokes, the shaft, the valve chambers in their
    // colours, the gallery rooms, the pump room and the river, with "you are here". 'centre' is on the wall.
    static void Schematic(Transform g, Vector3 centre)
    {
        const float s = 0.017f;   // metres of panel per metre of sewer
        var panel = Box("Schematic", g, centre + Vector3.forward * 0.02f, new Vector3(2.3f, 2.7f, 0.04f), Mat("SchematicBoard", null, new Color(0.16f, 0.2f, 0.19f), 1f, 0.2f));
        panel.transform.rotation = Quaternion.LookRotation(Vector3.back);   // faces the room (+z)
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        var ink = Mat("SchematicInk", null, new Color(0.85f, 0.82f, 0.72f), 1f, 0.2f);
        // A sewer point (plan x, z) on the panel: plan x to the viewer's right, plan z downward.
        Vector3 P(float x, float z) => panel.transform.TransformPoint(new Vector3((x - 45f) * s, -(z - 68f) * s, -0.03f));
        void Bar(float xa, float za, float xb, float zb, float thick, Material m)
        {
            var a = P(xa, za); var b = P(xb, zb);
            var bar = Prim(PrimitiveType.Cube, "Ink", panel.transform, (a + b) * 0.5f, Vector3.one, m);
            bar.transform.rotation = Quaternion.LookRotation(-Vector3.back, b - a);
            bar.transform.localScale = new Vector3(thick, Vector3.Distance(a, b) / 1f + thick, 0.005f);
            Object.DestroyImmediate(bar.GetComponent<Collider>());
        }
        void Room(float x0, float x1, float z0, float z1, Material m)
        {
            Bar(x0, z0, x1, z0, 0.02f, m); Bar(x0, z1, x1, z1, 0.02f, m); Bar(x0, z0, x0, z1, 0.02f, m); Bar(x1, z0, x1, z1, 0.02f, m);
        }
        // Tunnels.
        Bar(0f, 8f, 0f, 23f, 0.05f, ink); Bar(7f, 30f, 71f, 30f, 0.05f, ink); Bar(25f, 10f, 25f, 30f, 0.05f, ink);
        Bar(0f, 37f, 0f, 101f, 0.05f, ink); Bar(80f, 39f, 80f, 103f, 0.05f, ink); Bar(9f, 110f, 73f, 110f, 0.05f, ink);
        Bar(40f, 30f, 40f, 56f, 0.05f, ink); Bar(40f, 84f, 40f, 110f, 0.05f, ink); Bar(0f, 70f, 26f, 70f, 0.05f, ink); Bar(54f, 70f, 80f, 70f, 0.05f, ink);
        Bar(55f, 110f, 55f, 125f, 0.05f, ink); Bar(67f, 137f, 100f, 137f, 0.05f, ink);
        // The shaft: a ring of short bars.
        for (int i = 0; i < 16; i++)
        {
            float a0 = i * 22.5f * Mathf.Deg2Rad, a1 = (i + 1) * 22.5f * Mathf.Deg2Rad;
            Bar(40f + 14f * Mathf.Sin(a0), 70f + 14f * Mathf.Cos(a0), 40f + 14f * Mathf.Sin(a1), 70f + 14f * Mathf.Cos(a1), 0.03f, ink);
        }
        // Rooms: the gallery, the chambers in their colours, the pump room.
        Room(-3f, 3f, 0f, 8f, ink); Room(18f, 32f, 0f, 10f, ink); Room(40f, 54f, 0f, 10f, ink); Room(43f, 67f, 125f, 141f, ink);
        foreach (var (v, x0, x1, z0, z1) in new[] { ("1", -7f, 7f, 23f, 37f), ("2", 71f, 89f, 21f, 39f), ("3", -9f, 9f, 101f, 119f), ("4", 73f, 87f, 103f, 117f) })
        {
            var m = Mat("ChamberPaint_" + v, null, sewerColours["VALVE " + v], 1f, 0.2f);
            Room(x0, x1, z0, z1, m);
            Label(panel.transform, "<color=#" + ColorUtility.ToHtmlStringRGB(sewerColours["VALVE " + v]) + ">" + v + "</color>", panel.transform.InverseTransformPoint(P((x0 + x1) * 0.5f, (z0 + z1) * 0.5f)), 0.03f, Color.white, 0f);
        }
        Label(panel.transform, "5", panel.transform.InverseTransformPoint(P(40f, 70f)), 0.03f, SignPaint, 0f);
        Label(panel.transform, "STORM RELIEF SYSTEM · LINE 4", new Vector3(0f, 1.22f, -0.03f), 0.012f, SignPaint, 0f);
        Label(panel.transform, "RIVER ►", panel.transform.InverseTransformPoint(P(92f, 132f)), 0.01f, SignPaint, 0f);
        Label(panel.transform, "<color=#C0392B>●</color> YOU ARE HERE", panel.transform.InverseTransformPoint(P(25f, 4f)) + new Vector3(0.05f, 0f, 0f), 0.01f, SignPaint, 0f);
        Label(panel.transform, "OPEN RELIEF VALVES 1-4, THEN MAIN VALVE 5 (CISTERN FLOOR)", new Vector3(0f, -1.22f, -0.03f), 0.009f, SignPaint, 0f);
    }
}
