using UnityEditor;
using UnityEngine;

// Canal St Junction, dressed as a busy station everyone fled in a panic: newspapers, cups and cans
// strewn thinly across the platform and mezzanine (a little more by the ticket gates), snacks and
// unopened soda cans left behind on the benches, bins knocked over, "out of service" cones and barriers
// kicked aside (never blocking the way), old ads and Line 4 notices on the walls, a few graffiti tags,
// and a little blood: a spatter by the west staff steps and a drag trail along the south track to the
// maintenance door.
public static partial class LookTestBuilder
{
    const string TrashFbx = ArtDir + "/Props/Trash/TrashAndDebris.fbx";
    const string BarrierDir = "Props/Barriers";

    static void CanalDressing(Transform canal)
    {
        var g = new GameObject("Dressing").transform;
        g.SetParent(canal);
        Physics.SyncTransforms();   // the wall rays must see everything just built
        float z = CanalZ, mezz = 6f;

        // ---- rubbish, thinly spread (half what it was, over a wider area)
        Scatter(g, new Vector3(-19f, 0f, z + 10.5f), new Vector2(19f, 4.2f), 20, 11);
        Scatter(g, new Vector3(19f, 0f, z + 10.5f), new Vector2(19f, 4.2f), 23, 12);
        Scatter(g, new Vector3(56.5f, mezz, z + 10.5f), new Vector2(6.5f, 9f), 15, 14);  // mezzanine, platform side
        Scatter(g, new Vector3(64f, mezz, z + 10.5f), new Vector2(1.2f, 1.6f), 5, 15);   // a few in the gate aisle
        Scatter(g, new Vector3(72f, mezz, z + 10.5f), new Vector2(6.5f, 9f), 15, 16);    // street side
        Scatter(g, new Vector3(-68f, -1.3f, z + 1.2f), new Vector2(8f, 0.4f), 3, 17);    // blown down the arrival tunnel
        Scatter(g, new Vector3(-105f, -1.3f, z + 1.2f), new Vector2(8f, 0.4f), 3, 18);

        // ---- snacks and drinks left behind on the benches
        int bench = 0;
        foreach (var t in canal.GetComponentsInChildren<Transform>())
            if (t.name == "seats") BenchSnacks(g, t.gameObject, bench++);

        // Knocked-over bins spilling their bags.
        TippedBin(g, new Vector3(8f, 0f, z + 12.9f), 30f);
        TippedBin(g, new Vector3(69f, mezz, z + 13.5f), 200f);
        Kit("Props/Trash/TrashBagA.fbx", null, "Props/Trash/TrashBagBlack.png", g, new Vector3(9.1f, 0f, z + 13.4f), 40f, 0.5f);
        Kit("Props/Trash/TrashBagA.fbx", null, "Props/Trash/TrashBagBlack.png", g, new Vector3(70.2f, mezz, z + 14.2f), 120f, 0.45f);
        Kit("Props/Trash/TrashBagA.fbx", null, "Props/Trash/TrashBagBlack.png", g, new Vector3(-16.2f, 0f, z + 9.3f), 75f, 0.5f);

        // ---- "out of service" cones and barriers, kicked aside
        // Escalator 1 foot (the escalator runs x 40+, z 308.3..311.7).
        Kit(BarrierDir + "/BarrierType3.fbx", null, BarrierDir + "/TypeBarriers.png", g, new Vector3(36.6f, 0f, z + 7.1f), 35f, 1.2f, true);   // shoved crooked against the wall
        Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, new Vector3(38.3f, 0f, z + 12.5f), 0f, 0.7f);
        Toppled(Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, Vector3.zero, 0f, 0.7f), new Vector3(36.4f, 0f, z + 9.6f), 120f);
        Toppled(Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, Vector3.zero, 0f, 0.7f), new Vector3(37.6f, 0f, z + 11.2f), 250f);
        // Escalator 2 foot (mezzanine, x 79.8+).
        Toppled(Kit(BarrierDir + "/BarrierType2.fbx", null, BarrierDir + "/TypeBarriers.png", g, Vector3.zero, 80f, 1f), new Vector3(77.4f, mezz, z + 13f), 80f);
        Toppled(Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, Vector3.zero, 0f, 0.7f), new Vector3(78.1f, mezz, z + 8.4f), 30f);
        Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, new Vector3(76.8f, mezz, z + 7.9f), 0f, 0.7f);
        // Ticket gates.
        Toppled(Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, Vector3.zero, 0f, 0.7f), new Vector3(61.8f, mezz, z + 10.9f), 200f);
        Kit(BarrierDir + "/TrafficCone.fbx", null, BarrierDir + "/Cones.png", g, new Vector3(64.4f, mezz, z + 8.9f), 0f, 0.7f);

        // ---- old ads across the tracks and on the mezzanine, the line map on the west end wall
        string[] ads = { "Poster_01", "Poster_02", "Poster_03", "Poster_04", "Poster_05", "Poster_06", "Poster_07", "Poster_08" };
        float[] northAds = { -30f, -10f, 10f, 30f }, southAds = { -20f, 0f, 20f };
        for (int i = 0; i < northAds.Length; i++) WallProp(ads[i], g, new Vector3(northAds[i], 1f, z + 10.5f), Vector3.back, 0.4f, 1.3f);
        for (int i = 0; i < southAds.Length; i++) WallProp(ads[4 + i], g, new Vector3(southAds[i], 1f, z + 10.5f), Vector3.forward, 0.4f, 1.3f);
        WallProp(ads[7], g, new Vector3(52f, mezz + 1.5f, z + 5f), Vector3.back, mezz + 0.9f, 1.3f);
        WallProp(ads[2], g, new Vector3(66f, mezz + 1.5f, z + 16f), Vector3.forward, mezz + 0.9f, 1.3f);
        WallProp("metro_map", g, new Vector3(-37f, 1.5f, z + 10.5f), Vector3.left, 1.05f, 0.9f);

        // ---- Line 4 notices (readable by looking at them)
        Notice(g, "Notice_Suspended", new Vector3(-5f, 1.5f, z + 9f), Vector3.forward,
               "LINE 4 - SERVICE SUSPENDED\nDue to a power failure, all Line 4 services are suspended until further notice.\nPassengers must leave the station by the nearest exit.\n- MTA");
        Notice(g, "Notice_Escalators", new Vector3(38f, 1.6f, z + 6.8f), Vector3.right,
               "ESCALATORS OUT OF SERVICE\nPlease use the stairs.\nWe apologise for any inconvenience.");
        Notice(g, "Notice_NightClosure", new Vector3(61f, mezz + 1.6f, z + 5f), Vector3.back,
               "NIGHT CLOSURE\nCanal St Junction closes at 1:00 AM.\nStaff will clear the platforms. Do not remain in the station after closing.\n- Line 4 Station Management");
        Notice(g, "Notice_Unattended", new Vector3(63.5f, mezz + 1.6f, z + 16f), Vector3.forward,
               "SEE SOMETHING? SAY SOMETHING.\nReport unattended items, or anyone acting strangely, to station staff.");

        // ---- graffiti
        Spray(g, "Tag_Goru", 1.4f, new Vector3(-62f, 0.6f, z + 2.75f), Vector3.back);       // arrival tunnel, by the red lamp
        Spray(g, "Tag_Drips", 2f, new Vector3(-22f, 1f, z + 16f), Vector3.forward);        // across the south track
        Spray(g, "Tag_Scrawl", 1.2f, new Vector3(44f, 4.2f, z + 10f), Vector3.back);        // escalator 1's side wall
        Spray(g, "Tag_Peso", 1.2f, new Vector3(70f, mezz + 1.6f, z + 15f), Vector3.forward); // mezzanine, above the seats

        // ---- a little blood: a spatter by the staff steps, a drag trail into the south tunnel, drops by the booth
        Decal("Blood_CanalSpatter", g, "BloodSpatter02", new Vector3(-38.8f, 0.006f, z + 14.8f), 0.6f, 20f);
        float pit = -1.294f;
        Decal("Blood_CanalDrag1", g, "BloodFabric11", new Vector3(-41.5f, pit, z + 16.4f), 0.9f, 90f);
        Decal("Blood_CanalDrag2", g, "BloodFabric11", new Vector3(-44.6f, pit, z + 16.45f), 0.8f, 95f);
        Decal("Blood_CanalDrag3", g, "BloodFabric07", new Vector3(-48.2f, pit, z + 16.35f), 0.7f, 80f);
        Decal("Blood_CanalDrag4", g, "BloodFabric11", new Vector3(-52f, pit, z + 16.6f), 0.55f, 100f);
        Decal("Blood_CanalDrop1", g, "BloodDrop01", new Vector3(-55.2f, pit, z + 16.5f), 0.22f, 40f);
        Decal("Blood_CanalDrop2", g, "BloodDrop02", new Vector3(-57.3f, -1.284f, z + 18.4f), 0.18f, 160f);   // on the track bed
        // ...on down the tunnel to the maintenance door.
        Decal("Blood_CanalDrag5", g, "BloodFabric11", new Vector3(-63f, pit, z + 16.5f), 0.6f, 92f);
        Decal("Blood_CanalDrop3", g, "BloodDrop01", new Vector3(-68.5f, pit, z + 16.4f), 0.2f, 15f);
        Decal("Blood_CanalDrag6", g, "BloodFabric07", new Vector3(-73f, pit, z + 16.6f), 0.55f, 85f);
        Decal("Blood_CanalDrop4", g, "BloodDrop02", new Vector3(-77.2f, -1.284f, z + 18.3f), 0.18f, 60f);
        Decal("Blood_CanalDrag7", g, "BloodFabric11", new Vector3(-79.4f, pit, z + 20.45f), 0.5f, 5f);   // at the door
        Decal("Blood_CanalBoothDrop", g, "BloodDrop20", new Vector3(58.7f, mezz + 0.006f, z + 2.2f), 0.2f, 70f);
        foreach (var r in g.GetComponentsInChildren<Renderer>())
            if (r.name.StartsWith("Blood_Canal")) Matte(r.sharedMaterial);
    }

    // A few things left on a bench: unopened soda cans standing up and chip bags lying flat, on every other
    // bench (and the odd one more), found by casting rays down onto its seats.
    static void BenchSnacks(Transform parent, GameObject seats, int index)
    {
        var rng = new System.Random(40 + index);
        if (index % 2 == 1 && rng.NextDouble() < 0.6) return;
        string[] brands = { "generic_cola", "pepso", "sprit", "fantom_orange", "dr_piper", "red_bullet" };
        string[] cans = { "Can_Standard330", "Can_Standard330", "Can_Slim250", "Can_Fat500" };
        var b = Bounds(seats);
        bool alongX = b.size.x > b.size.z;
        int count = 1 + rng.Next(3);
        for (int i = 0; i < count; i++)
        {
            float t = 0.1f + 0.8f * (float)rng.NextDouble();
            float across = (float)rng.NextDouble() * 0.2f - 0.1f;
            Vector3 p = alongX ? new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.max.y + 0.5f, b.center.z + across)
                               : new Vector3(b.center.x + across, b.max.y + 0.5f, Mathf.Lerp(b.min.z, b.max.z, t));
            if (!Physics.Raycast(p, Vector3.down, out var hit, 2f) || hit.point.y > b.min.y + 0.65f) continue;   // missed, or hit a backrest
            GameObject item;
            if (rng.NextDouble() < 0.6)
            {
                item = Model(ArtDir + "/Props/SodaCans/" + cans[rng.Next(cans.Length)] + ".fbx", null, parent,
                             Tex2("Props/SodaCans/can_" + brands[rng.Next(brands.Length)] + ".png"), 1f, (float)rng.NextDouble() * 360f);
                item.transform.localScale *= 0.12f / Mathf.Max(0.001f, Bounds(item).size.y);
            }
            else
            {
                item = ModelPiece(TrashFbx, rng.NextDouble() < 0.5 ? "Chip_Bag_1" : "Chip_Bag_3", parent, Tex2("Props/Trash/Bags.png"), 1f);
                var ib = Bounds(item);
                item.transform.localScale *= 0.22f / Mathf.Max(0.001f, Mathf.Max(ib.size.x, ib.size.y, ib.size.z));
                item.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f) * item.transform.rotation;
            }
            StandOn(item, hit.point + Vector3.up * 0.003f);
        }
    }

    // Litter dropped at random (but always the same: 'seed') over an oval area around 'centre'.
    static void Scatter(Transform parent, Vector3 centre, Vector2 radius, int count, int seed)
    {
        // Piece, texture, size in metres (its longest side).
        (string piece, string tex, float size)[] kinds =
        {
            ("News_1", "NewsPapers", 0.45f), ("News_2", "NewsPapers", 0.45f), ("News_3", "NewsPapers", 0.45f), ("Paper_1", "NewsPapers", 0.3f),
            ("Paper_2", "NewsPapers", 0.3f), ("Coffee_Cup_Crushed", "FastFoodTrash", 0.12f), ("Soda_Cup", "FastFoodTrash", 0.16f),
            ("Fry_Container", "FastFoodTrash", 0.14f), ("Chip_Bag_2", "Bags", 0.24f), ("Can_2_Crushed", "Cans", 0.11f),
            ("Can_4", "Cans", 0.12f), ("Paper_Bag_1", "Bags", 0.28f), ("News_Stack", "NewsPapers", 0.4f), ("Coffee_Cup_WithLid", "FastFoodTrash", 0.13f),
        };
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            var k = kinds[rng.Next(kinds.Length)];
            Vector2 p = Random2(rng) * radius;
            var go = ModelPiece(TrashFbx, k.piece, parent, Tex2("Props/Trash/" + k.tex + ".png"), 1f);
            var b = Bounds(go);
            go.transform.localScale *= k.size / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y, b.size.z));
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f) * go.transform.rotation;
            // Cups and cans sometimes lie on their side.
            if (k.size < 0.2f && rng.NextDouble() < 0.5) go.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * go.transform.rotation;
            StandOn(go, centre + new Vector3(p.x, 0.008f, p.y));   // a hair above the floor, so flat paper doesn't flicker
        }
    }

    static Vector2 Random2(System.Random rng)
    {
        float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
    }

    // Lays an object on its side (as if knocked over), facing 'yaw', resting on 'floorPoint'.
    static GameObject Toppled(GameObject go, Vector3 floorPoint, float yaw)
    {
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, 90f) * go.transform.rotation;
        StandOn(go, floorPoint);
        return go;
    }

    // A station bin knocked over, with rubbish spilling out of its mouth.
    static void TippedBin(Transform parent, Vector3 floorPoint, float yaw)
    {
        var bin = Prop("trash_can", parent, floorPoint, 0f);
        Object.DestroyImmediate(bin.GetComponent<MeshCollider>());
        Toppled(bin, floorPoint, yaw);
        bin.AddComponent<MeshCollider>();
        Vector3 mouth = floorPoint + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 0.7f;
        Scatter(parent, mouth, new Vector2(0.5f, 0.5f), 6, (int)(floorPoint.x * 10f));
    }

    // A Metro kit piece (poster, map) hung flat on the first wall along 'direction' from 'from',
    // its bottom edge at height 'bottom' and 'height' metres tall.
    static void WallProp(string piece, Transform parent, Vector3 from, Vector3 direction, float bottom, float height)
    {
        if (!Physics.Raycast(from, direction, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("Dressing: no wall found for " + piece);
            return;
        }
        Vector3 n = hit.normal;
        // In the kit the posters face -Z at yaw 0, the line map +Z.
        float face = piece.StartsWith("Poster") ? -1f : 1f;
        var go = Prop(piece, parent, Vector3.zero, Mathf.Atan2(face * n.x, face * n.z) * Mathf.Rad2Deg);
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());
        go.transform.localScale *= height / Mathf.Max(0.001f, Bounds(go).size.y);
        var b = Bounds(go);
        // Its back 1 cm off the wall, centred on the ray, bottom at 'bottom'.
        float back = Vector3.Dot(b.center, n) - Mathf.Abs(Vector3.Dot(b.extents, n));
        Vector3 shift = n * (Vector3.Dot(hit.point, n) + 0.01f - back);
        Vector3 along = new Vector3(hit.point.x - b.center.x, 0f, hit.point.z - b.center.z);
        along -= n * Vector3.Dot(along, n);
        go.transform.position += shift + along + Vector3.up * (bottom - b.min.y);
    }

    // A printed Line 4 notice stuck on a wall: readable when looked at.
    static void Notice(Transform parent, string name, Vector3 from, Vector3 direction, string text)
    {
        var quad = Prim(PrimitiveType.Quad, name, parent, from, new Vector3(0.42f, 0.6f, 1f), Mat("Line4Notice", NoticeTexture(), Color.white, 1f, 0.1f));
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Stick(quad, from, direction);
        quad.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.02f);
        quad.AddComponent<ReadableNote>().text = text;
    }

    // An official notice drawn by code: a dark blue Line 4 header band, a white "4" disc and lines of "text".
    static Texture2D NoticeTexture()
    {
        const string path = "Assets/Art/Textures/Generated/Line4Notice.png";
        EnsureFolder("Assets/Art/Textures/Generated");
        if (!System.IO.File.Exists(path))
        {
            const int w = 56, h = 80;
            var tex = new Texture2D(w, h);
            var rng = new System.Random(7);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0.9f, 0.89f, 0.84f);                                       // paper
                    bool band = y > 64 && y < 77 && x > 2 && x < 53;
                    float dx = x - 10f, dy = y - 70.5f;
                    if (band) c = dx * dx + dy * dy < 20f ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.12f, 0.2f, 0.42f);
                    else if (y > 50 && y < 58 && x > 6 && x < 49) c = new Color(0.12f, 0.12f, 0.14f); // heading
                    else if (y < 44 && y > 8 && y % 5 < 2 && x > 6 && x < 50 - (y * 5 % 11)) c = new Color(0.25f, 0.24f, 0.24f); // text lines
                    c *= 0.88f + (float)rng.NextDouble() * 0.12f;                                  // grubby
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
