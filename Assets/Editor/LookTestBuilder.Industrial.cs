using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Building blocks for the industrial, "backrooms" look of the lower levels (both stations):
// - GridLevel: a whole level drawn as a character map (one character per square cell), turned into
//   walls, floors and ceilings that meet edge to edge;
// - ChainFence: chain-link fencing (the Industrial Exterior pack's see-through fence image) on steel posts;
// - Pipe / PipeRun: round pipes along walls and ceilings (8-sided, PS1 style), joints filled in;
// - CagedBulb: a dim bulb in a wire cage, the only light down there;
// - ControlPanel and ElectricCabinet: control boxes (with cable bundles up to the ceiling) and cabinets on
//   the walls (not the circuit-breaker model, which is kept for the real fuse box, the objective);
// - WalkLink: a walking route for Frankie down stairs or through a doorway (see LinkPath).
public static partial class LookTestBuilder
{
    const string IndustrialTex = "Assets/Art/Textures/Industrial/";
    static Material chainLink, fencePost, pipeRust, pipeSteel, pipeGreen, conduit, cabinetGrey, cabinetVent, cabinetBody, bulbCage, bulbGlow, wireBundle;
    // Pipe images that are sheets of several metal pieces: only this column (left and right edge, 0..1) is
    // wrapped round the pipe, so every pipe shows the same piece, with its seams as rings along it.
    static readonly Dictionary<Material, Vector2> pipeStrip = new Dictionary<Material, Vector2>();

    static void IndustrialMaterials()
    {
        chainLink = Mat("ChainLink", Load<Texture2D>(IndustrialTex + "Fence.png"), new Color(0.85f, 0.85f, 0.85f), 1f, 0.35f, 0.6f);
        chainLink.SetFloat("_AlphaClip", 1f);
        chainLink.SetFloat("_Cutoff", 0.5f);
        chainLink.EnableKeyword("_ALPHATEST_ON");
        chainLink.SetFloat("_Cull", 2f);                 // normal culling: the mesh has its own face for each side
        chainLink.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        fencePost = Mat("FencePost", null, new Color(0.5f, 0.52f, 0.53f), 1f, 0.4f, 0.7f);
        pipeRust = Mat("PipeRust", Load<Texture2D>(IndustrialTex + "Pipes.png"), new Color(0.9f, 0.86f, 0.8f), 3f, 0.25f, 0.4f);
        pipeStrip[pipeRust] = new Vector2(0.835f, 0.985f);   // the rusted plate on the right of the sheet
        pipeSteel = Mat("PipeSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.62f, 0.62f, 0.6f), 1f, 0.35f, 0.6f);
        pipeGreen = Mat("PipeGreen", null, new Color(0.33f, 0.42f, 0.36f), 1f, 0.3f, 0.3f);
        conduit = Mat("Conduit", null, new Color(0.18f, 0.18f, 0.19f), 1f, 0.3f, 0.3f);
        cabinetGrey = Mat("ElecCabinet", Load<Texture2D>(IndustrialTex + "Metal_cabinet_2.png"), Color.white, 1f, 0.3f, 0.4f);
        cabinetVent = Mat("ElecCabinetVent", Load<Texture2D>(IndustrialTex + "Metal_cabinet_1.png"), Color.white, 1f, 0.3f, 0.4f);
        cabinetBody = Mat("ElecCabinetBody", null, new Color(0.66f, 0.67f, 0.66f), 1f, 0.3f, 0.4f);   // plain sides and top
        bulbCage = Mat("BulbCage", null, new Color(0.15f, 0.15f, 0.15f), 1f, 0.3f, 0.6f);
        bulbGlow = EmissiveMat("BulbGlow", new Color(1f, 0.75f, 0.45f) * 2f);
        wireBundle = Mat("WireBundle", Tex2("Props/Plumbing/Wires.png"), Color.white, 4f, 0.3f);   // coloured stripes round the bundle
    }

    // ---------------------------------------------------------------- a level from a character map
    // Each character is a 'cell' x 'cell' square; row i runs along x at z = origin.z + i * cell.
    // '#' solid, '.' corridor or small room (3 m ceiling), 'H' hall (5.5 m), 'd' doorway (2.3 m lintel).
    // Solid cells next to open ones become wall blocks; each open cell gets floor and a ceiling block
    // reaching up to one common top, so low ceilings meet high ones with a clean step.
    static void GridLevel(Transform parent, string[] rows, Vector3 origin, float cell, Material wall, Material floor, Material ceil)
    {
        float f = origin.y, top = f + 5.7f;
        bool Open(int x, int y) => y >= 0 && y < rows.Length && x >= 0 && x < rows[y].Length && rows[y][x] != '#';
        float Height(char c) => c == 'H' ? 5.5f : c == 'd' ? 2.3f : 3f;
        for (int y = 0; y < rows.Length; y++)
        {
            float z0 = origin.z + y * cell, z1 = z0 + cell;
            int x = 0;
            while (x < rows[y].Length)
            {
                char c = rows[y][x];
                bool needed = c != '#' || NextToOpen(x, y);
                int end = x;
                while (end + 1 < rows[y].Length && rows[y][end + 1] == c && (c != '#' || NextToOpen(end + 1, y) == needed)) end++;
                float x0 = origin.x + x * cell, x1 = origin.x + (end + 1) * cell;
                string id = "_" + y + "_" + x;
                if (c == '#')
                {
                    if (needed) Span("Wall" + id, parent, new Vector3(x0, f - 0.2f, z0), new Vector3(x1, top, z1), wall);
                }
                else
                {
                    Span("Floor" + id, parent, new Vector3(x0, f - 0.2f, z0), new Vector3(x1, f, z1), floor);
                    Span("Ceiling" + id, parent, new Vector3(x0, f + Height(c), z0), new Vector3(x1, top, z1), ceil);
                }
                x = end + 1;
            }
        }

        bool NextToOpen(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (Open(x + dx, y + dy)) return true;
            return false;
        }
    }

    // ---------------------------------------------------------------- chain-link fencing
    // A straight run of fence from a to b (points on the floor), 'height' tall: posts at most 2.5 m apart,
    // a top rail, and the see-through mesh between. Solid for the player, but Frankie (and the light) can
    // see through it; it is cut out of his walkable area so he walks round it.
    static GameObject ChainFence(Transform parent, string name, Vector3 a, Vector3 b, float height = 2.4f)
    {
        var fence = new GameObject(name);
        fence.transform.SetParent(parent);
        Vector3 along = b - a; along.y = 0f;
        float length = along.magnitude;
        Vector3 dir = along / length;
        fence.transform.SetPositionAndRotation(a, Quaternion.LookRotation(dir));

        int spans = Mathf.Max(1, Mathf.CeilToInt(length / 2.5f));
        for (int i = 0; i <= spans; i++)
        {
            var post = Prim(PrimitiveType.Cylinder, "Post", fence.transform, a + dir * (length * i / spans) + Vector3.up * (height * 0.5f), new Vector3(0.06f, height * 0.5f, 0.06f), fencePost);
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }
        foreach (float h in new[] { height - 0.03f, 0.08f })
        {
            var rail = Prim(PrimitiveType.Cylinder, "Rail", fence.transform, a + dir * (length * 0.5f) + Vector3.up * h, new Vector3(0.04f, length * 0.5f, 0.04f), fencePost);
            rail.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(rail.GetComponent<Collider>());
        }

        // The mesh: a quad along the run, its image repeating every half metre. It has two faces, back to
        // back, each facing (and lit from) its own side, so the fence looks the same from either side.
        var mesh = new GameObject("Mesh");
        mesh.transform.SetParent(fence.transform, false);
        const float tile = 0.5f;
        var m = new Mesh { name = name + "_Mesh" };
        var corners = new[] { new Vector3(0f, 0.08f, 0f), new Vector3(0f, height - 0.03f, 0f), new Vector3(0f, height - 0.03f, length), new Vector3(0f, 0.08f, length) };
        var cornerUvs = new[] { new Vector2(0f, 0.08f / tile), new Vector2(0f, height / tile), new Vector2(length / tile, height / tile), new Vector2(length / tile, 0.08f / tile) };
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var normals = new List<Vector3>();
        foreach (var side in new[] { Vector3.right, Vector3.left })   // the first face (wound one way) faces +x, the second -x
            for (int i = 0; i < 4; i++) { verts.Add(corners[i]); uvs.Add(cornerUvs[i]); normals.Add(side); }
        m.SetVertices(verts); m.SetUVs(0, uvs); m.SetNormals(normals);
        m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 };
        m.RecalculateBounds();
        mesh.AddComponent<MeshFilter>().sharedMesh = m;
        mesh.AddComponent<MeshRenderer>().sharedMaterial = chainLink;

        var block = new GameObject("FenceBlock");
        block.transform.SetParent(fence.transform, false);
        block.layer = 2;                                   // rays (sight, the crosshair, the torch) pass through
        var box = block.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, height * 0.5f, length * 0.5f);
        box.size = new Vector3(0.08f, height, length);
        var carve = block.AddComponent<NavMeshObstacle>();
        carve.shape = NavMeshObstacleShape.Box;
        carve.center = box.center; carve.size = new Vector3(0.3f, height, length);
        carve.carving = true; carve.carveOnlyStationary = true;
        return fence;
    }

    // A closed fence around a rectangle (x0..x1, z0..z1 on the floor at y), with an opening 'gateWidth' wide
    // in the middle of one side ('gateSide': 0 = -z side, 1 = +x, 2 = +z, 3 = -x). Sides listed in 'skip'
    // aren't fenced (a wall is already there).
    static void FencedCage(Transform parent, string name, float x0, float x1, float z0, float z1, float y, int gateSide, float gateWidth = 1.4f, params int[] skip)
    {
        var corners = new[] { new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1) };
        for (int side = 0; side < 4; side++)
        {
            if (System.Array.IndexOf(skip, side) >= 0) continue;
            Vector3 a = corners[side], b = corners[(side + 1) % 4];
            if (side != gateSide) { ChainFence(parent, name + "_Side" + side, a, b); continue; }
            Vector3 mid = (a + b) * 0.5f, dir = (b - a).normalized;
            ChainFence(parent, name + "_Side" + side + "A", a, mid - dir * (gateWidth * 0.5f));
            ChainFence(parent, name + "_Side" + side + "B", mid + dir * (gateWidth * 0.5f), b);
        }
    }

    // ---------------------------------------------------------------- pipes
    // One straight pipe from a to b, 'radius' thick (8 sides, PS1 style), lengthened by its radius at both
    // ends so joints in a run look filled. No collider (they're up on the walls and ceilings).
    static GameObject Pipe(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material mat)
    {
        Vector3 dir = (b - a).normalized;
        a -= dir * radius; b += dir * radius;
        float length = Vector3.Distance(a, b);
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(a, Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up));
        float tile = tileSize.TryGetValue(mat, out var t) ? t : 1f;
        Vector2 strip = pipeStrip.TryGetValue(mat, out var st) ? st : new Vector2(0f, 1f);
        const int sides = 8;
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        // The image goes round the pipe (u) and repeats along it every 'tile' metres (v).
        for (int i = 0; i <= sides; i++)
        {
            float ang = i * Mathf.PI * 2f / sides, u = Mathf.Lerp(strip.x, strip.y, (float)i / sides);
            var ring = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
            verts.Add(ring); uvs.Add(new Vector2(u, 0f));
            verts.Add(ring + Vector3.forward * length); uvs.Add(new Vector2(u, length / tile));
        }
        for (int i = 0; i < sides; i++)
        {
            int k = i * 2;
            tris.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 });
        }
        // End caps.
        foreach (float z in new[] { 0f, length })
        {
            int centre = verts.Count;
            verts.Add(new Vector3(0f, 0f, z)); uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                int v = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, z)); uvs.Add(new Vector2(0.5f, 0.5f));
                verts.Add(new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, z)); uvs.Add(new Vector2(0.5f, 0.5f));
                if (z == 0f) tris.AddRange(new[] { centre, v + 1, v }); else tris.AddRange(new[] { centre, v, v + 1 });
            }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // A pipe through several points (bends at each), with a bracket every few metres if 'brackets'.
    static void PipeRun(Transform parent, string name, float radius, Material mat, params Vector3[] points)
    {
        for (int i = 1; i < points.Length; i++)
            Pipe(parent, name + "_" + i, points[i - 1], points[i], radius, mat);
    }

    // ---------------------------------------------------------------- lights
    // A dim bulb in a wire cage hanging from the ceiling at 'pos' (the bulb's centre). Not on any grid:
    // these run off the emergency circuit, so they're lit before the power comes back.
    // A bare bulb is weak: lights fade with distance squared, so the given intensity is multiplied by 20 (and
    // the reach by 1.6) to give a real pool of light under each one, a little brighter than the station lamps, since the concrete down there is dark. 'cable' hangs it that far below its mount.
    static Light CagedBulb(Transform parent, string name, Vector3 pos, float intensity = 2.2f, float range = 6f, bool flicker = false, float cable = 0f)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent);
        var bulb = Prim(PrimitiveType.Sphere, "Bulb", g, pos, Vector3.one * 0.09f, bulbGlow);
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        // Cage: four thin bars round the bulb and a base plate it hangs from.
        foreach (var o in new[] { new Vector3(0.07f, 0f, 0.07f), new Vector3(-0.07f, 0f, 0.07f), new Vector3(0.07f, 0f, -0.07f), new Vector3(-0.07f, 0f, -0.07f) })
            Object.DestroyImmediate(Prim(PrimitiveType.Cube, "Bar", g, pos + o, new Vector3(0.012f, 0.16f, 0.012f), bulbCage).GetComponent<Collider>());
        Object.DestroyImmediate(Prim(PrimitiveType.Cube, "Base", g, pos + Vector3.up * 0.09f, new Vector3(0.17f, 0.025f, 0.17f), bulbCage).GetComponent<Collider>());
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(g);
        light.transform.position = pos + Vector3.down * 0.12f;
        light.type = LightType.Point; light.color = new Color(1f, 0.86f, 0.66f);
        light.intensity = intensity * 20f; light.range = range * 1.6f; light.shadows = LightShadows.None;
        if (cable > 0f)
            Object.DestroyImmediate(Prim(PrimitiveType.Cylinder, "Cable", g, pos + Vector3.up * (0.1f + cable * 0.5f), new Vector3(0.012f, cable * 0.5f, 0.012f), bulbCage).GetComponent<Collider>());
        if (flicker)
        {
            var f = light.gameObject.AddComponent<FlickeringLight>();
            f.lamp = light; f.tube = bulb.GetComponent<Renderer>(); f.tubeGlow = new Color(1f, 0.75f, 0.45f) * 2f;
            f.minCalm = 3f; f.maxCalm = 9f;
        }
        return light;
    }

    // ---------------------------------------------------------------- electrics on the walls
    // Facing 'normal' (out of the wall), yaw for a model whose front faces -X.
    static float YawFacing(Vector3 normal) => Mathf.Atan2(normal.z, -normal.x) * Mathf.Rad2Deg;

    // A row of three control boxes on a wall (the Plumbing & Wiring pack's switch box: red and green lamps),
    // with two bundles of coloured cables running from them up to the ceiling. 'wallPoint' is on the wall
    // surface at the boxes' centre height.
    static GameObject ControlPanel(Transform parent, Vector3 wallPoint, Vector3 normal, float height = 0.45f)
    {
        var panel = Model(ArtDir + "/Props/Plumbing/Switch.fbx", null, parent, Tex2("Props/Plumbing/Switch.png"), 1f, YawFacing(normal));
        panel.name = "ControlPanel";
        panel.transform.localScale *= height / Mathf.Max(0.01f, Bounds(panel).size.y);
        var b = Bounds(panel);
        float depth = Mathf.Abs(Vector3.Dot(b.size, normal));
        panel.transform.position += wallPoint + normal * (depth * 0.5f + 0.01f) - b.center;
        b = Bounds(panel);
        // Cable bundles from the top of the outer boxes up to whatever is above (the ceiling).
        Physics.SyncTransforms();
        Vector3 across = Vector3.Cross(Vector3.up, normal);
        float halfWidth = Mathf.Abs(Vector3.Dot(b.size, across)) * 0.5f;
        foreach (float side in new[] { -0.7f, 0.7f })
        {
            Vector3 foot = new Vector3(b.center.x, b.max.y, b.center.z) + across * (halfWidth * side) - normal * (depth * 0.15f);
            float top = Physics.Raycast(foot + Vector3.up * 0.05f, Vector3.up, out var hit, 8f) ? hit.point.y : foot.y + 1.5f;
            Pipe(parent, "CableBundle", foot, new Vector3(foot.x, top, foot.z), 0.035f, wireBundle);
        }
        return panel;
    }

    // A grey electrical cabinet standing on the floor against a wall (its back 1 cm off the wall).
    // 'floorPoint' is on the floor at the wall, the middle of the cabinet's width. Plain grey all round, with
    // its door picture (and warning sign) on the front only, the right way round.
    static void ElectricCabinet(Transform parent, Vector3 floorPoint, Vector3 normal, float width = 1f, float height = 1.9f, float depth = 0.45f, bool vents = false)
    {
        Vector3 across = Vector3.Cross(Vector3.up, normal);
        Vector3 c = floorPoint + normal * (depth * 0.5f + 0.01f) + Vector3.up * (height * 0.5f);
        Vector3 size = new Vector3(Mathf.Abs(across.x) * width + Mathf.Abs(normal.x) * depth, height, Mathf.Abs(across.z) * width + Mathf.Abs(normal.z) * depth);
        var body = Box("ElectricCabinet", parent, c, size, cabinetBody);
        // A quad shows its picture on the side facing its -Z, so it is turned to look into the wall.
        var front = Prim(PrimitiveType.Quad, "Front", body.transform, c + normal * (depth * 0.5f + 0.002f), new Vector3(width, height, 1f), vents ? cabinetVent : cabinetGrey);
        front.transform.rotation = Quaternion.LookRotation(-normal);
        Object.DestroyImmediate(front.GetComponent<Collider>());
    }

    // A bundle of black cable conduits running along a wall between two points (on the wall surface).
    // Where a run stops in the middle of a wall (at a doorway's edge) rather than in a corner, 'boxAtA' /
    // 'boxAtB' end it in a junction box, from which the cables carry on straight up into the ceiling.
    static void Conduits(Transform parent, Vector3 a, Vector3 b, Vector3 normal, int count = 3, bool boxAtA = false, bool boxAtB = false)
    {
        Vector3 along = (b - a).normalized;
        Vector3 aa = boxAtA ? a + along * 0.2f : a, bb = boxAtB ? b - along * 0.2f : b;   // the box stands just in from the end
        for (int i = 0; i < count; i++)
        {
            Vector3 off = normal * 0.05f + Vector3.up * (i * 0.07f);
            Pipe(parent, "Conduit", aa + off, bb + off, 0.025f, conduit);
        }
        if (boxAtA) JunctionBox(parent, aa, normal, along, count);
        if (boxAtB) JunctionBox(parent, bb, normal, along, count);
    }

    // A small grey junction box on the wall where a bundle of 'count' conduits ends ('point' on the wall at
    // the lowest conduit), with the cables rising from its top to the ceiling.
    static void JunctionBox(Transform parent, Vector3 point, Vector3 normal, Vector3 along, int count)
    {
        float spread = (count - 1) * 0.07f, h = spread + 0.26f, d = 0.13f, w = spread + 0.2f;
        Vector3 c = point + normal * (d * 0.5f) + Vector3.up * (spread * 0.5f);
        Vector3 size = new Vector3(Mathf.Abs(along.x) * w + Mathf.Abs(normal.x) * d, h, Mathf.Abs(along.z) * w + Mathf.Abs(normal.z) * d);
        Object.DestroyImmediate(Box("JunctionBox", parent, c, size, cabinetBody).GetComponent<Collider>());
        Physics.SyncTransforms();
        for (int i = 0; i < count; i++)
        {
            Vector3 foot = c + Vector3.up * (h * 0.5f) + along * ((i - (count - 1) * 0.5f) * 0.07f);
            float top = Physics.Raycast(foot + Vector3.up * 0.02f, Vector3.up, out var hit, 6f) ? hit.point.y : foot.y + 1f;
            Pipe(parent, "Conduit", foot, new Vector3(foot.x, top, foot.z), 0.025f, conduit);
        }
    }

    // ---------------------------------------------------------------- Frankie's walking routes
    // A jump link from points[0] to the last point that Frankie walks along (stairs, doorways); with a
    // 'gate', it only exists while that door is open.
    static void WalkLink(Transform parent, string name, HingedDoor gate, params Vector3[] points)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = Vector3.zero;
        var link = go.AddComponent<NavMeshLink>();
        link.startPoint = points[0];
        link.endPoint = points[points.Length - 1];
        link.width = 0f;
        link.bidirectional = true;
        var route = go.AddComponent<LinkPath>();
        route.points = points;
        route.gate = gate;
    }
}
