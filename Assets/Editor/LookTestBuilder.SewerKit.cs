using System.Collections.Generic;
using UnityEngine;

// Fitting the Sewer pack's round tunnel pieces together (Models/Environment/Sewer/Sewers.fbx, the "Serwers"
// set: 6.6 m wide, a 2 m water channel between two walkways 0.77 m higher, 5.5 m of headroom).
// Each piece's open ends ("ports") are found from its mesh: a face of its bounds that the tunnel's whole
// cross-section touches. A port is the bottom-middle of the channel at that face, with the way out of the
// piece. Bends and junctions are placed at a point (where their channels' centre lines cross), turned so
// their ports face the ways asked for; straights fill the gap between two ports, stretched a little to fit.
public static partial class LookTestBuilder
{
    const string SewerKitFbx = ArtDir + "/Environment/Sewer/Sewers.fbx";
    public const float SewerWalkwayRise = 0.77f;   // the walkways stand this much above the channel's bottom

    struct Port { public Vector3 pos, dir; public Port(Vector3 p, Vector3 d) { pos = p; dir = d; } }

    static readonly Dictionary<string, string> sewerKitTextures = new Dictionary<string, string>
    {
        { "concrete_base", "Environment/Sewer/concrete_base.png" }, { "brick_modern_01", "Environment/Sewer/brick_modern_01.jpg" },
        { "brick_modern", "Environment/Sewer/brick_modern.jpg" }, { "bricks", "Environment/Sewer/bricks.jpg" },
        { "concrete_base_02", "Environment/Sewer/concrete_base_02.jpg" }, { "concrete_base_03", "Environment/Sewer/concrete_base_03.jpg" },
        { "concrete_dirty", "Environment/Sewer/concrete_dirty.jpg" }, { "soil_mud", "Environment/Sewer/soil_mud.jpg" },
        { "concrete_bare_01", "Environment/Sewer/concrete_bare_01.jpg" }, { "brick_pavement", "Environment/Sewer/brick_pavement.jpg" },
        { "Metal", "Environment/Sewer/Metal.jpg" },
        // The pack's rubbish and rubble.
        { "Trash_01", "Environment/Sewer/Trash_01.png" }, { "Trash", "Environment/Sewer/Trash.png" }, { "Garbage_bag", "Environment/Sewer/Garbage_bag.png" },
        { "debris", "Environment/Sewer/debris.jpg" }, { "debris_01", "Environment/Sewer/debris_01.jpg" }, { "debris_02", "Environment/Sewer/debris_02.jpg" },
        { "debris_03", "Environment/Sewer/debris_03.png" },
    };

    // One piece of the kit at the world origin (turned by 'yaw'), solid, textured.
    static GameObject KitPiece(Transform parent, string piece, float yaw)
    {
        var go = Model(SewerKitFbx, piece, parent, null, 1f, yaw, sewerKitTextures);
        go.transform.position = Vector3.zero;
        go.AddComponent<MeshCollider>();
        return go;
    }

    // The ports of a piece as it stands now, in world space.
    static List<Port> PortsOf(GameObject go)
    {
        var mf = go.GetComponent<MeshFilter>();
        var verts = mf.sharedMesh.vertices;
        var world = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++) world[i] = mf.transform.TransformPoint(verts[i]);
        var b = Bounds(go);
        var ports = new List<Port>();
        foreach (var (axis, sign) in new[] { (0, -1), (0, 1), (2, -1), (2, 1) })
        {
            float plane = sign < 0 ? b.min[axis] : b.max[axis];
            int perp = 2 - axis;
            float pMin = float.MaxValue, pMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            int count = 0;
            foreach (var v in world)
            {
                if (Mathf.Abs(v[axis] - plane) > 0.03f) continue;
                count++;
                pMin = Mathf.Min(pMin, v[perp]); pMax = Mathf.Max(pMax, v[perp]);
                yMin = Mathf.Min(yMin, v.y); yMax = Mathf.Max(yMax, v.y);
            }
            if (count < 6 || pMax - pMin < 3f || yMax - yMin < 2.5f) continue;
            var pos = Vector3.zero;
            pos[axis] = plane; pos[perp] = (pMin + pMax) * 0.5f; pos.y = yMin;
            var dir = Vector3.zero; dir[axis] = sign;
            ports.Add(new Port(pos, dir));
        }
        return ports;
    }

    // A bend or junction whose channel centre lines cross at 'at' (the channel's bottom there), with its
    // ports facing exactly the ways in 'ways'. Returns its ports, keyed by the way they face.
    static Dictionary<Vector3, Port> KitNode(Transform parent, string piece, Vector3 at, params Vector3[] ways)
    {
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            var go = KitPiece(parent, piece, yaw);
            var ports = PortsOf(go);
            bool fits = ports.Count == ways.Length;
            foreach (var w in ways) fits &= ports.Exists(p => Vector3.Dot(p.dir, w) > 0.99f);
            if (!fits) { Object.DestroyImmediate(go); continue; }
            // Where its centre lines cross: x from a port facing along z, z from a port facing along x.
            Port alongX = ports.Find(p => Mathf.Abs(p.dir.x) > 0.5f), alongZ = ports.Find(p => Mathf.Abs(p.dir.z) > 0.5f);
            var cross = new Vector3(alongZ.pos.x, ports[0].pos.y, alongX.pos.z);
            go.transform.position += at - cross;
            var result = new Dictionary<Vector3, Port>();
            foreach (var p in ports) result[p.dir] = new Port(p.pos + at - cross, p.dir);
            return result;
        }
        Debug.LogWarning("Sewer kit: " + piece + " has no turn with ports facing all the ways asked at " + at);
        return new Dictionary<Vector3, Port>();
    }

    // Straight tunnel sections from port 'a' (facing the way to go) to the point 'to' on its axis, as whole
    // 6.77 m sections stretched a little to fit. Returns the port at the far end (facing on, the same way).
    static Port KitTunnel(Transform parent, Port a, Vector3 to, string piece = "Serwers_009")
    {
        Vector3 along = to - a.pos; along.y = 0f;
        float length = Vector3.Dot(along, a.dir);
        if (length < 0.05f) return a;
        if (length < 5f && piece == "Serwers_009") piece = "Serwers_008";   // a short gap: the 3.46 m section
        // A section, turned to run along a.dir: find its ports and its natural length.
        var probe = KitPiece(parent, piece, 0f);
        var ports = PortsOf(probe);
        float yaw = Vector3.SignedAngle(ports[1].pos - ports[0].pos, a.dir, Vector3.up);
        Object.DestroyImmediate(probe);
        int sections = Mathf.Max(1, Mathf.RoundToInt(length / 6.77f));
        float each = length / sections;
        for (int i = 0; i < sections; i++)
        {
            var go = KitPiece(parent, piece, Mathf.Round(yaw / 90f) * 90f);
            var ps = PortsOf(go);
            float natural = Vector3.Distance(ps[0].pos, ps[1].pos);
            // Stretch it along its length: the local axis that lies along the tunnel.
            var local = go.transform.InverseTransformDirection(a.dir);
            var s = go.transform.localScale;
            int k = Mathf.Abs(local.x) > Mathf.Abs(local.y) ? (Mathf.Abs(local.x) > Mathf.Abs(local.z) ? 0 : 2) : (Mathf.Abs(local.y) > Mathf.Abs(local.z) ? 1 : 2);
            s[k] *= each / natural;
            go.transform.localScale = s;
            ps = PortsOf(go);
            var back = ps.Find(p => Vector3.Dot(p.dir, a.dir) < -0.99f);
            go.transform.position += a.pos + a.dir * (each * i) - back.pos;
        }
        return new Port(a.pos + a.dir * length, a.dir);
    }

    // A tunnel from one port to another facing it (their axes must line up).
    static void KitLink(Transform parent, Port from, Port to) => KitTunnel(parent, from, to.pos);
}
