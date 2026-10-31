using System.Collections.Generic;
using UnityEngine;

// Air-conditioning ducts ("PSX Style Modular Vents", Sketchfab, split by Tools/Blender/vent_pieces.py).
// Big 1 m mains in the machinery halls, slimmer 50 cm ducts on the platforms and in rooms. Each duct hangs
// under the ceiling and runs from wall to wall, so it seems to come from and go on to other rooms; one
// with no wall in reach turns up into the ceiling instead. Grilles under the ducts and on small rooms'
// walls blow air (VentHum) once their station's power is back. No colliders: they're out of reach, and
// Frankie's walkable area ignores them.
public static partial class LookTestBuilder
{
    const string VentDir = ArtDir + "/Props/Vents/";
    static Material hangerSteel;

    static void BuildVents()
    {
        var g = Group("Vents");
        Physics.SyncTransforms();   // the rays below must see everything built so far
        hangerSteel = Mat("DuctHanger", null, new Color(0.3f, 0.29f, 0.27f), 1f, 0.3f, 0.7f);
        float cz = CanalZ;

        // ---- Hollis St (the first station's grid)
        CeilingDuct(g, "Duct_HollisPlatform", new Vector3(0f, 2f, 16.2f), Vector3.right, 0.5f, grid);
        WallVent(g, "Vent_StaffRoom", new Vector3(43f, 0f, 28.5f), Vector3.forward, 0.5f, grid);
        CeilingDuct(g, "Duct_ServiceHall", new Vector3(-56f, -7.5f, 45.5f), Vector3.right, 1f, grid);
        WallVent(g, "Vent_ServiceStorage", new Vector3(-76f, -8f, 47f), Vector3.left, 0.5f, grid);
        WallVent(g, "Vent_SquatterCamp", new Vector3(-37f, -8f, 47f), Vector3.right, 0.5f, grid);

        // ---- Canal St (its own grid, from the generator)
        CeilingDuct(g, "Duct_CanalPlatform", new Vector3(0f, 2f, cz + 6.75f), Vector3.right, 0.5f, canalGrid);   // centred on the east wall's section beside the escalator opening (z+5.5..8)
        CeilingDuct(g, "Duct_CanalMezzanine", new Vector3(64f, 8f, cz + 3f), Vector3.right, 0.5f, canalGrid);
        CeilingDuct(g, "Duct_CanalGenerator", new Vector3(68f, 7.5f, cz - 5.5f), Vector3.right, 0.5f, canalGrid);
        WallVent(g, "Vent_CanalOffice", new Vector3(58f, 7.5f, cz + 25f), Vector3.right, 0.5f, canalGrid);
        // The hall's mains hang lower, under the pipe that crosses the hall north-south.
        CeilingDuct(g, "Duct_CanalLowerHall_S", new Vector3(-46f, -7f, 342.5f), Vector3.right, 1f, canalGrid, 0.95f);
        CeilingDuct(g, "Duct_CanalLowerHall_N", new Vector3(-46f, -7f, 362f), Vector3.right, 1f, canalGrid, 0.95f);
        WallVent(g, "Vent_CanalLatchRoom", new Vector3(-21f, -8f, 352f), Vector3.right, 0.5f, canalGrid);
        WallVent(g, "Vent_CanalPumps", new Vector3(-88f, -8f, 344f), Vector3.left, 0.5f, canalGrid);
        WallVent(g, "Vent_CanalStore", new Vector3(-88f, -8f, 353f), Vector3.left, 0.5f, canalGrid);
    }

    // A duct 'size' across hanging just under the ceiling above 'inside' ('drop' metres lower still, to pass
    // under pipes), running along 'axis' from wall to wall (each end 15 cm into its wall). A way with no wall within 40 m turns up into the ceiling there.
    // Grilles underneath every 6 m or so; the middle one hums once 'power' is on.
    static void CeilingDuct(Transform parent, string name, Vector3 inside, Vector3 axis, float size, PowerGrid power, float drop = 0f)
    {
        if (!Surface(inside, Vector3.up, 20f, out var ceiling)) { Debug.LogWarning(name + ": no ceiling found"); return; }
        float y = ceiling.point.y - 0.12f - drop - size * 0.5f;   // a hand's width under the ceiling, room for the hangers
        var mid = new Vector3(inside.x, y, inside.z);
        bool wallA = Surface(mid, -axis, 40f, out var hitA), wallB = Surface(mid, axis, 40f, out var hitB);
        Vector3 a = wallA ? hitA.point - axis * 0.15f : mid - axis * 40f;
        Vector3 b = wallB ? hitB.point + axis * 0.15f : mid + axis * 40f;
        float up = ceiling.point.y + 0.3f - y;   // from the duct's middle to just inside the ceiling
        var points = new List<Vector3>();
        if (!wallA) points.Add(a + Vector3.up * up);
        points.Add(a); points.Add(b);
        if (!wallB) points.Add(b + Vector3.up * up);

        var run = Duct(parent, name, size, points.ToArray());
        float length = Vector3.Distance(a, b);
        Debug.Log(name + ": " + length.ToString("F1") + " m, y " + y.ToString("F2") + ", ends " + (wallA ? hitA.collider.name : "ceiling") + " / " + (wallB ? hitB.collider.name : "ceiling"));

        // Hangers up to the ceiling every 2.5 m.
        for (float d = 1.2f; d < length - 1f; d += 2.5f)
        {
            var p = a + axis * d;
            float top = y + size * 0.5f;
            var rod = Prim(PrimitiveType.Cylinder, "Hanger", run, new Vector3(p.x, (top + ceiling.point.y) * 0.5f, p.z), new Vector3(0.03f, (ceiling.point.y - top) * 0.5f + 0.02f, 0.03f), hangerSteel);
            Object.DestroyImmediate(rod.GetComponent<Collider>());
        }
        // Grilles underneath, spread evenly, at least 1.5 m from the walls.
        int grilles = Mathf.Max(1, Mathf.RoundToInt((length - 3f) / 6f));
        for (int i = 0; i < grilles; i++)
        {
            var p = Vector3.Lerp(a, b, (i + 0.5f) / grilles) + Vector3.down * (size * 0.5f + 0.016f);
            var grille = Grille(run, "Grille", p, Vector3.down, size * 0.65f);
            if (i == grilles / 2) Airflow(grille, power, size);
        }
        CheckClear(name, run);
    }

    // A grille high on the nearest wall along 'toWall' from 'inside', 25 cm under the ceiling: the air
    // comes through the wall from a duct out of sight.
    static void WallVent(Transform parent, string name, Vector3 inside, Vector3 toWall, float size, PowerGrid power)
    {
        if (!Surface(inside, Vector3.up, 20f, out var ceiling) || !Surface(inside, toWall, 20f, out var wall))
        { Debug.LogWarning(name + ": no wall or ceiling found"); return; }
        var p = new Vector3(wall.point.x, ceiling.point.y - 0.25f - size * 0.5f, wall.point.z) + wall.normal * 0.016f;
        var grille = Grille(parent, name, p, wall.normal, size);
        Airflow(grille, power, size);
        Debug.Log(name + ": on " + wall.collider.name + " at " + p.ToString("F2"));
    }

    // Lays a square duct 'size' across along a path of straight, axis-aligned steps, with a bend at every
    // corner. Straight lengths are made of whole duct sections (with their flanged joints), stretched a little to fit.
    static Transform Duct(Transform parent, string name, float size, params Vector3[] points)
    {
        var run = new GameObject(name).transform;
        run.SetParent(parent);
        int last = points.Length - 2;
        for (int i = 0; i <= last; i++)
        {
            Vector3 dir = (points[i + 1] - points[i]).normalized;
            Vector3 start = points[i] + (i > 0 ? dir * size : Vector3.zero);           // a bend takes 'size' at each corner
            Vector3 end = points[i + 1] - (i < last ? dir * size : Vector3.zero);
            float length = Vector3.Distance(start, end);
            int sections = Mathf.Max(1, Mathf.RoundToInt(length / (size * 2f)));
            // A section runs up its own +Y from its pivot; turn that onto 'dir', seams kept level.
            var turn = Quaternion.LookRotation(Mathf.Abs(dir.y) > 0.5f ? Vector3.forward : Vector3.up, dir);
            for (int s = 0; s < sections; s++)
            {
                DuctPiece(run, "DuctStraight", start + dir * (length * s / sections), turn, new Vector3(size, length / sections, size));
            }
            if (i < last)
            {
                // The bend's inlet is its pivot (air going up its +Y); its outlet is 1 m up and 1 m along its -Z.
                Vector3 next = (points[i + 2] - points[i + 1]).normalized;
                DuctPiece(run, "DuctElbow", points[i + 1] - dir * size, Quaternion.LookRotation(-next, dir), Vector3.one * size);
            }
        }
        return run;
    }

    static GameObject DuctPiece(Transform parent, string model, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var go = Model(VentDir + model + ".fbx", null, parent, Tex2("Props/Vents/Vents.png"), 1f, 0f);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        return go;
    }

    // A flat louvred grille 'size' across, centred on 'position', facing 'outward'.
    static GameObject Grille(Transform parent, string name, Vector3 position, Vector3 outward, float size)
    {
        var go = DuctPiece(parent, "VentGrille", position, Quaternion.LookRotation(outward, Mathf.Abs(outward.y) > 0.5f ? Vector3.forward : Vector3.up), Vector3.one * size);
        go.name = name;
        return go;
    }

    // The air-conditioner loop on a grille, silent until 'power' comes back (VentHum).
    static void Airflow(GameObject grille, PowerGrid power, float size)
    {
        var source = grille.AddComponent<AudioSource>();
        source.clip = Load<AudioClip>("Assets/Art/Audio/SFX/Vents/AirConditionerLoop.ogg");
        source.loop = true; source.playOnAwake = false; source.volume = 0f;
        source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f; source.maxDistance = size > 0.7f ? 14f : 9f;
        var hum = grille.AddComponent<VentHum>();
        hum.grid = power; hum.source = source;
        hum.volume = size > 0.7f ? 0.35f : 0.25f;
    }

    // The nearest wall, floor or ceiling along 'dir': a big solid surface, not a pipe, lamp or prop.
    static bool Surface(Vector3 from, Vector3 dir, float reach, out RaycastHit surface)
    {
        surface = default;
        float best = float.MaxValue;
        foreach (var hit in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
        {
            var s = hit.collider.bounds.size;
            float[] sides = { s.x, s.y, s.z };
            System.Array.Sort(sides);
            if (sides[1] < 1.2f || hit.collider.GetComponentInParent<HingedDoor>() || hit.distance >= best) continue;
            best = hit.distance; surface = hit;
        }
        return best < float.MaxValue;
    }

    // Warns in the Console about anything already built that a duct runs through (pipes, lamps, beams).
    static void CheckClear(string name, Transform run)
    {
        var mine = new HashSet<Renderer>(run.GetComponentsInChildren<Renderer>());
        var parts = new List<Bounds>();
        foreach (var r in mine) if (r.name != "Hanger") { var b = r.bounds; b.Expand(-0.04f); parts.Add(b); }
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (mine.Contains(r) || r.transform.IsChildOf(run.parent)) continue;
            var s = r.bounds.size;
            if ((s.x > 3f ? 1 : 0) + (s.y > 3f ? 1 : 0) + (s.z > 3f ? 1 : 0) >= 2) continue;   // walls, floors, ceilings
            foreach (var p in parts)
                if (p.Intersects(r.bounds)) { Debug.LogWarning(name + " runs through " + r.name + " at " + r.bounds.center.ToString("F1")); break; }
        }
    }
}
