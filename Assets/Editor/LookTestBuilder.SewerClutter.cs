using UnityEditor;
using UnityEngine;

// The sewers' clutter pass (2026-11-02, the director's choices: moderate, with dark empty stretches left between):
// more pipes along the tunnel walls and across every other tunnel's ceiling, wall junction boxes, utility
// equipment in the dry rooms (cabinets, a transformer, pumps, a generator, conduits), junk washed up on the
// walkways (planks, blocks, rebar, sheet metal, bottles, bags, crushed cans) and the sewer crew's leftovers
// (shelving, crates, a mining helmet, a toolbox, a shovel, a cable coil, cones and a barrier). Things only go
// on dry floor at the walkways' height, against a wall, clear of doorways, valves, ladders and pickups.
// Crates, barrels, bags, cans, bottles and cones become kickable in the clutter pass, like everywhere else.
public static partial class LookTestBuilder
{
    const string MineFbx = ArtDir + "/Props/Mine/Mines.fbx";

    // ---- tunnels: 1-2 more pipes low on the wall at 'toWall' (under TunnelPipe's), each bending into the brick at both ends
    static void PipeBundle(Transform g, Vector3 start, Vector3 dir, Vector3 toWall, float length, System.Random rng)
    {
        int count = 1 + rng.Next(2);
        Material[] mats = { pipeRust, pipeSteel, pipeGreen };
        for (int i = 0; i < count; i++)
        {
            float height = 2.15f + i * 0.28f, radius = i == 0 ? 0.1f : 0.065f;
            if (!Physics.Raycast(start + dir * (length * 0.5f) + Vector3.up * height, toWall, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) continue;
            float wall = hit.distance, off = wall - 0.05f - radius;
            float s0 = 0.8f + i * 0.3f, s1 = length - 0.8f - i * 0.3f;
            Vector3 P(float s, float d) => start + dir * s + toWall * d + Vector3.up * height;
            PipeRun(g, "WallPipeLow", radius, mats[(i + rng.Next(3)) % 3], P(s0, wall + 0.15f), P(s0, off), P(s1, off), P(s1, wall + 0.15f));
        }
    }

    // ---- tunnels: a fat pipe under the vault, a little off the middle, turning up into the ceiling at both ends
    static void CeilingPipe(Transform g, Vector3 start, Vector3 dir, Vector3 across, float length)
    {
        Vector3 Line(float s) => start + dir * s + across * 0.75f;
        if (!Physics.Raycast(Line(length * 0.5f) + Vector3.up, Vector3.up, out var top, 8f, ~0, QueryTriggerInteraction.Ignore)) return;
        float y = top.point.y - 0.34f, into = top.point.y + 0.25f;
        Vector3 P(float s, float h) { var p = Line(s); p.y = h; return p; }
        PipeRun(g, "CeilingPipe", 0.13f, pipeRust, P(1.4f, into), P(1.4f, y), P(length - 1.4f, y), P(length - 1.4f, into));
    }

    // ---- tunnels: a junction box with its cables on the wall at 'toWall', somewhere along the middle of the stretch
    static void TunnelWallBox(Transform g, Vector3 start, Vector3 dir, Vector3 toWall, float length, System.Random rng)
    {
        Vector3 from = start + dir * (length * (0.35f + (float)rng.NextDouble() * 0.3f)) + Vector3.up * 1.5f;
        if (!Physics.Raycast(from, toWall, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) return;
        if (Busy(hit.point, 1.2f)) return;
        Vector3 normal = hit.normal; normal.y = 0f;
        ControlPanel(g, hit.point, normal.normalized);
    }

    // ---- tunnels: odd bits of junk along the walkways, one every ~9 m with gaps (about a third left empty)
    static readonly (string piece, float size)[] walkwayJunk =
    {
        ("Wooden_Plank", 1.1f), ("Cinder_Block", 0.4f), ("Concrete_Debris", 0.6f), ("Rebar", 1f), ("Sheet_Metal", 0.9f),
        ("GlassBottle", 0.28f), ("TrashBag", 0.6f), ("CardboardBox", 0.45f), ("Can_1_Crushed", 0.13f), ("Can_5_Crushed", 0.13f),
    };

    static void WalkwayJunk(Transform g, Vector3 start, Vector3 dir, float length, System.Random rng)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(length / 9f));
        for (int i = 0; i < n; i++)
        {
            if (rng.NextDouble() < 0.35) continue;   // a dark, empty stretch
            float s = length * ((i + 0.2f + (float)rng.NextDouble() * 0.6f) / n);
            Vector3 across = Vector3.Cross(Vector3.up, dir) * (rng.Next(2) == 0 ? 1f : -1f);
            Vector3 spot = start + dir * s + across * (2.5f + (float)rng.NextDouble() * 0.35f);
            if (!DryFloor(spot, out var floor) || Busy(floor, 1f)) continue;
            var k = walkwayJunk[rng.Next(walkwayJunk.Length)];
            var item = TrashPiece(g, k.piece, k.size);
            if (!item) continue;
            item.transform.rotation = Quaternion.Euler(0f, rng.Next(360), 0f) * item.transform.rotation;
            StandOn(item, floor);
        }
    }

    // ---- the rooms
    static void SewerClutter(Transform parent)
    {
        var g = new GameObject("Clutter").transform;
        g.SetParent(parent);
        Physics.SyncTransforms();
        Vector3 o = SewerOrigin;
        var rng = new System.Random(2611);

        // Pipes high on the valve rooms' long walls, above the water and the doorways, into the walls at both ends.
        RoomPipes(g, -7f, 7f, 23f, 37f, 0f, 5f);
        RoomPipes(g, -9f, 9f, 101f, 119f, 0f, 5f);
        RoomPipes(g, 73f, 87f, 103f, 117f, 0f, 5f);
        RoomPipes(g, 71f, 89f, 21f, 39f, -2.2f, 6.7f);
        RoomPipes(g, 43f, 67f, 125f, 141f, 0f, 6f);

        // The electrical room: a transformer, cabinets, conduits along the north wall, wall boxes on the east.
        AgainstWalls(g, 40f, 54f, 0f, 10f, rng,
            () => Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, o, 0f, 1.5f, true),
            () => Cabinet(g), () => Cabinet(g),
            () => Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, o, 0f, 0.9f, true));
        Conduits(g, o + new Vector3(41f, 2.6f, 10f), o + new Vector3(53f, 2.6f, 10f), Vector3.back, 3);
        foreach (float z in new[] { 3f, 7f })
            if (!Busy(o + new Vector3(54f, 1.5f, z), 1f)) ControlPanel(g, o + new Vector3(54f, 1.5f, z), Vector3.left);

        // The pump room: two pumps, a generator, barrels.
        AgainstWalls(g, 43f, 67f, 125f, 141f, rng,
            () => Kit("Props/Plumbing/Pump.fbx", null, "Props/Plumbing/Pump.png", g, o, 0f, 0.6f, true),
            () => Kit("Props/Plumbing/Pump.fbx", null, "Props/Plumbing/Pump.png", g, o, 0f, 0.6f, true),
            () => Kit("Props/Plumbing/Generator.fbx", null, "Props/Plumbing/Generator.png", g, o, 0f, 1.6f, true),
            () => Mine(g, "Barrel_01", 1f), () => Mine(g, "Barrel_01", 1f));

        // The control room: the crew's leftovers.
        AgainstWalls(g, 18f, 32f, 0f, 10f, rng,
            () => Mine(g, "Shelving", 2f, true),
            () => CrateWithHelmet(g),
            () => Mine(g, "Medical_box", 0.45f),
            () => Leaning(Mine(g, "Shovel", 1.3f)),
            () => Mine(g, "Cables", 0.12f),
            () => Kit("Props/Barriers/TrafficCone.fbx", null, "Props/Barriers/Cones.png", g, o, 0f, 0.7f),
            () => Kit("Props/Barriers/BarrierType2.fbx", null, "Props/Barriers/TypeBarriers.png", g, o, 0f, 1f, true));

        // The arrival: a toolbox and a hand lamp someone left at the foot of the ladder.
        AgainstWalls(g, -3f, 3f, 0f, 8f, rng, () => Mine(g, "Medical_box", 0.42f), () => Mine(g, "Lamp", 0.36f));

        // The valve rooms' dry landings: cones and a crate where the crew worked.
        foreach (var (x0, x1, z0, z1) in new[] { (-7f, 7f, 23f, 37f), (-9f, 9f, 101f, 119f), (73f, 87f, 103f, 117f) })
            AgainstWalls(g, x0, x1, z0, z1, rng,
                () => Kit("Props/Barriers/ConstructionCone.fbx", null, "Props/Barriers/Cones.png", g, o, 0f, 0.7f),
                () => IndustrialPiece(g, "CrateWood", new Vector3(0.8f, 0.7f, 0.6f)));
    }

    // A pipe along each long wall of a room, near the top (45 cm out, clear of the 25 cm brick pillars), entering the end walls; and one across its middle.
    static void RoomPipes(Transform g, float x0, float x1, float z0, float z1, float floor, float height)
    {
        Vector3 o = SewerOrigin;
        float y = floor + height - 0.6f;
        bool alongX = x1 - x0 >= z1 - z0;
        if (alongX)
        {
            PipeRun(g, "RoomPipe", 0.1f, pipeRust, o + new Vector3(x0 - 0.2f, y, z0 + 0.45f), o + new Vector3(x1 + 0.2f, y, z0 + 0.45f));
            PipeRun(g, "RoomPipe", 0.075f, pipeSteel, o + new Vector3(x0 - 0.2f, y - 0.3f, z1 - 0.45f), o + new Vector3(x1 + 0.2f, y - 0.3f, z1 - 0.45f));
            PipeRun(g, "RoomPipe", 0.12f, pipeGreen, o + new Vector3((x0 + x1) * 0.5f + 1.3f, y + 0.25f, z0 - 0.2f), o + new Vector3((x0 + x1) * 0.5f + 1.3f, y + 0.25f, z1 + 0.2f));
        }
        else
        {
            PipeRun(g, "RoomPipe", 0.1f, pipeRust, o + new Vector3(x0 + 0.45f, y, z0 - 0.2f), o + new Vector3(x0 + 0.45f, y, z1 + 0.2f));
            PipeRun(g, "RoomPipe", 0.075f, pipeSteel, o + new Vector3(x1 - 0.45f, y - 0.3f, z0 - 0.2f), o + new Vector3(x1 - 0.45f, y - 0.3f, z1 + 0.2f));
            PipeRun(g, "RoomPipe", 0.12f, pipeGreen, o + new Vector3(x0 - 0.2f, y + 0.25f, (z0 + z1) * 0.5f + 1.3f), o + new Vector3(x1 + 0.2f, y + 0.25f, (z0 + z1) * 0.5f + 1.3f));
        }
    }

    // Each item stood against one of the room's walls at a free spot (dry floor at the walkways' height, a wall
    // right behind it, nothing in the way, no doorway, valve, ladder or pickup near), its back to the wall.
    // An item that finds no spot is left out.
    static void AgainstWalls(Transform g, float x0, float x1, float z0, float z1, System.Random rng, params System.Func<GameObject>[] items)
    {
        Vector3 o = SewerOrigin;
        foreach (var make in items)
        {
            var item = make();
            if (!item) continue;
            var own = item.transform.rotation;
            bool placed = false;
            for (int tries = 0; tries < 40 && !placed; tries++)
            {
                int side = rng.Next(4);
                Vector3 inward = side switch { 0 => Vector3.forward, 1 => Vector3.left, 2 => Vector3.back, _ => Vector3.right };
                float t = (float)rng.NextDouble();
                Vector3 wall = side switch
                {
                    0 => new Vector3(Mathf.Lerp(x0 + 0.9f, x1 - 0.9f, t), 0f, z0),
                    1 => new Vector3(x1, 0f, Mathf.Lerp(z0 + 0.9f, z1 - 0.9f, t)),
                    2 => new Vector3(Mathf.Lerp(x0 + 0.9f, x1 - 0.9f, t), 0f, z1),
                    _ => new Vector3(x0, 0f, Mathf.Lerp(z0 + 0.9f, z1 - 0.9f, t)),
                };
                item.transform.rotation = Quaternion.LookRotation(inward) * own;
                var b = Bounds(item);
                float depth = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(inward.x), 0f, Mathf.Abs(inward.z))));
                Vector3 spot = o + wall + inward * (depth * 0.5f + 0.15f);   // just past the 12.5 cm base course
                if (!DryFloor(spot, out var floor)) continue;
                if (!Physics.Raycast(floor + Vector3.up, -inward, depth * 0.5f + 0.4f, ~0, QueryTriggerInteraction.Ignore)) continue;   // no wall: a doorway
                StandOn(item, floor);
                Physics.SyncTransforms();
                var nb = Bounds(item);
                bool blocked = false;
                foreach (var h in Physics.OverlapBox(nb.center + Vector3.up * 0.05f, nb.extents * 0.92f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(item.transform)) { blocked = true; break; }
                if (blocked || Busy(floor, 1.4f, item.transform)) continue;
                placed = true;
            }
            if (!placed) Object.DestroyImmediate(item);
        }
    }

    // Dry floor at the walkways' height right under 'spot' (not a pit, step, channel or water).
    static bool DryFloor(Vector3 spot, out Vector3 floor)
    {
        floor = spot;
        if (!Physics.Raycast(new Vector3(spot.x, SewerOrigin.y + 1.5f, spot.z), Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (Mathf.Abs(hit.point.y - SewerOrigin.y) > 0.08f) return false;
        floor = hit.point;
        return true;
    }

    // Something the player uses or needs near 'point' (valves, the breaker, pickups, doors: all interactable;
    // ladders and gates)?
    static bool Busy(Vector3 point, float radius, Transform except = null)
    {
        foreach (var c in Physics.OverlapSphere(point + Vector3.up * 0.8f, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (except && c.transform.IsChildOf(except)) continue;
            if (c.GetComponentInParent<IInteractable>() != null || c.GetComponentInParent<Pickup>()) return true;
            for (var p = c.transform; p; p = p.parent)
                if (p.name.Contains("Ladder") || p.name.EndsWith("Gate")) return true;
        }
        return false;
    }

    // One piece of the trash pack, its longest side 'size' metres, with its own texture; no collider.
    static GameObject TrashPiece(Transform g, string prefix, float size)
    {
        var asset = Load<GameObject>(TrashFbx);
        Transform source = null;
        foreach (var t in asset.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith(prefix) && t.GetComponent<Renderer>()) { source = t; break; }
        if (!source) { Debug.LogWarning("Sewer clutter: no trash piece " + prefix); return null; }
        string mat = source.GetComponent<Renderer>().sharedMaterial.name;
        string tex = mat == "RustyCans" ? "CansRusty" : mat;   // the one material named differently from its picture
        var go = ModelPiece(TrashFbx, source.name, g, AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Props/Trash/" + tex + ".png"), 1f);
        go.name = prefix;
        var b = Bounds(go);
        go.transform.localScale *= size / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y, b.size.z));
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return go;
    }

    // One piece of the Mine pack 'height' tall (its texture is named after its material).
    static GameObject Mine(Transform g, string piece, float height, bool solid = false)
    {
        var asset = Load<GameObject>(MineFbx);
        Renderer source = null;
        foreach (var r in asset.GetComponentsInChildren<Renderer>()) if (r.name == piece) { source = r; break; }
        if (!source) { Debug.LogWarning("Sewer clutter: no Mine piece " + piece); return null; }
        string mat = source.sharedMaterial.name;
        string tex = System.IO.File.Exists(ArtDir + "/Props/Mine/" + mat + ".jpg") ? mat + ".jpg" : mat + ".png";
        var go = Kit("Props/Mine/Mines.fbx", piece, "Props/Mine/" + tex, g, SewerOrigin, 0f, height, solid);
        if (!solid) foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return go;
    }

    // A grey steel cabinet (the industrial pack's TransformerBox), solid.
    static GameObject Cabinet(Transform g)
    {
        var go = IndustrialPiece(g, "TransformerBox", new Vector3(1f, 1.8f, 0.5f));
        go.name = "SewerCabinet";
        AddMeshColliders(go);
        return go;
    }

    // A crate with a mining helmet left on top (the crate stays put: something stands on it).
    static GameObject CrateWithHelmet(Transform g)
    {
        var holder = new GameObject("CrewLeftovers").transform;
        holder.SetParent(g);
        holder.position = SewerOrigin;
        var crate = IndustrialPiece(holder, "CrateWood", new Vector3(0.9f, 0.75f, 0.7f));
        StandOn(crate, SewerOrigin);
        var helmet = Mine(holder, "Mining_Helmet", 0.16f);
        var top = Bounds(crate);
        if (helmet) StandOn(helmet, new Vector3(top.center.x + 0.1f, top.max.y, top.center.z + 0.05f));
        return holder.gameObject;
    }

    // Leant against the wall behind it (tipped back a little).
    static GameObject Leaning(GameObject go)
    {
        if (go) go.transform.rotation = Quaternion.Euler(-14f, 0f, 0f) * go.transform.rotation;
        return go;
    }
}
