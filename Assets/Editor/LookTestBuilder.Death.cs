using UnityEditor;
using UnityEngine;

// Station 1's fear is death. Grounded traces of it (the haunting itself stays unexplained):
// a small memorial on the platform's back wall for Eddie Russo, a track worker killed last winter by a faulty
// third rail nobody fixed: a photo, a card, burnt-down candles and two wilted bouquets. (Ray's jacket and
// shoes at the top of the service stairs were removed on 2026-10-28.)
// The bouquets are the "Dry Flower" scan (Sketchfab), simplified to 12,000 triangles. All text is a draft.
public static partial class LookTestBuilder
{
    static void BuildDeathTraces()
    {
        var g = Group("DeathTraces");
        Physics.SyncTransforms();

        // ---- the memorial, between the two platform benches against the back wall
        const float x = -3f;
        if (!Physics.Raycast(new Vector3(x, 1.4f, 17f), Vector3.back, out var wall, 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("Memorial: no wall found");
            return;
        }
        Vector3 n = wall.normal, along = Vector3.Cross(Vector3.up, n);
        Vector3 Floor(float side, float out_) => new Vector3(wall.point.x, 0f, wall.point.z) + along * side + n * out_;

        var photo = Prim(PrimitiveType.Quad, "MemorialPhoto", g, Vector3.zero, new Vector3(0.18f, 0.24f, 1f), Mat("MemorialPhoto", MemorialPhotoTexture(), Color.white, 1f, 0.2f));
        Object.DestroyImmediate(photo.GetComponent<Collider>());
        Stick(photo, new Vector3(x - 0.1f, 1.45f, 17f), Vector3.back);
        photo.transform.rotation *= Quaternion.Euler(0f, 0f, 3f);   // taped up a little crooked
        var card = Prim(PrimitiveType.Quad, "MemorialCard", g, Vector3.zero, new Vector3(0.2f, 0.14f, 1f), Mat("NotePaper", null, new Color(0.9f, 0.88f, 0.8f), 1f, 0.1f));
        Object.DestroyImmediate(card.GetComponent<Collider>());
        Stick(card, new Vector3(x + 0.12f, 1.22f, 17f), Vector3.back);
        card.transform.rotation *= Quaternion.Euler(0f, 0f, -4f);
        card.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
        card.AddComponent<ReadableNote>().text = "IN MEMORY OF\nEDDIE RUSSO\n1951 - 1995\n31 years on Line 4.\nKilled by a faulty third rail they never fixed.\nWe won't forget you, Eddie. - The night crew";

        // Burnt-down candles along the foot of the wall.
        float[] candleSides = { -0.35f, -0.15f, 0.2f, 0.38f };
        for (int i = 0; i < candleSides.Length; i++) Candle(g, Floor(candleSides[i], 0.12f + (i % 2) * 0.08f), false);

        // Two bouquets of dried flowers lying on the floor, heads away from the wall.
        foreach (var (side, yaw, roll) in new[] { (-0.05f, 70f, 15f), (0.55f, 115f, -25f) })
        {
            var bouquet = new GameObject("WiltedBouquet").transform;
            bouquet.SetParent(g);
            bouquet.SetPositionAndRotation(Floor(side, 0.3f), Quaternion.Euler(0f, yaw + Vector3.SignedAngle(Vector3.forward, n, Vector3.up), 0f));
            var flowers = Model(ArtDir + "/Props/DryBouquet/DryBouquet.fbx", null, bouquet, Tex2("Props/DryBouquet/DryBouquet.png"), 1.4f, 0f);
            // Laid down: its stems toward the wall, heads along the bouquet's forward, turned a little on its side.
            flowers.transform.rotation = bouquet.rotation * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, roll, 0f);
            StandOn(flowers, bouquet.position + bouquet.forward * 0.1f);
        }
    }

    // A faded little photo drawn by code: a sepia snapshot of a man in a work jacket against a dark background.
    static Texture2D MemorialPhotoTexture()
    {
        const string path = "Assets/Art/Textures/Generated/MemorialPhoto.png";
        EnsureFolder("Assets/Art/Textures/Generated");
        if (!System.IO.File.Exists(path))
        {
            const int w = 36, h = 48;
            var tex = new Texture2D(w, h);
            var rng = new System.Random(9);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0.86f, 0.82f, 0.72f);                                    // white border
                    if (x > 2 && x < w - 3 && y > 6 && y < h - 3)
                    {
                        c = Color.Lerp(new Color(0.32f, 0.26f, 0.2f), new Color(0.55f, 0.47f, 0.36f), y / (float)h);   // background
                        float dx = x - w * 0.5f, dyHead = y - 33f;
                        if (dx * dx + dyHead * dyHead * 0.8f < 30f) c = new Color(0.7f, 0.58f, 0.45f);              // face
                        else if (y < 28 && Mathf.Abs(dx) < 9f + (28 - y) * 0.25f) c = new Color(0.24f, 0.26f, 0.2f);  // jacket
                    }
                    c *= 0.85f + (float)rng.NextDouble() * 0.15f;                                // faded, grainy
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
