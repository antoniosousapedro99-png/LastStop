using UnityEditor;
using UnityEngine;

// Station 1's fear is death. Grounded traces of it (the haunting itself stays unexplained):
// - a small memorial on the platform's back wall for Eddie Russo, a track worker killed last winter by a
//   faulty third rail nobody fixed: a photo, a card, burnt-down candles and two wilted bouquets;
// - Ray's work jacket and shoes left at the top of the service stairs (the squatters' notes say he went up
//   to look and never came back).
// The flowers, jacket and shoes are simple shapes until models are found. All text is a draft.
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

        // Two wilted bouquets lying on the floor, their paper gone limp, heads drooping brown and dark red.
        var paper = Mat("BouquetPaper", null, new Color(0.55f, 0.5f, 0.42f), 1f, 0.2f);   // limp brown paper
        var stem = Mat("WiltedStem", null, new Color(0.25f, 0.27f, 0.15f), 1f, 0.1f);
        Color[] heads = { new Color(0.35f, 0.08f, 0.08f), new Color(0.4f, 0.28f, 0.16f), new Color(0.3f, 0.12f, 0.14f) };
        var rng = new System.Random(4);
        foreach (var (side, yaw) in new[] { (-0.05f, 70f), (0.55f, 115f) })
        {
            var bouquet = new GameObject("WiltedBouquet").transform;
            bouquet.SetParent(g);
            bouquet.SetPositionAndRotation(Floor(side, 0.3f), Quaternion.Euler(0f, yaw + Vector3.SignedAngle(Vector3.forward, n, Vector3.up), 0f));
            var wrap = Prim(PrimitiveType.Cylinder, "Wrap", bouquet, Vector3.zero, new Vector3(0.06f, 0.1f, 0.035f), paper);
            wrap.transform.localPosition = new Vector3(0f, 0.03f, -0.05f); wrap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(wrap.GetComponent<Collider>());
            for (int i = 0; i < 5; i++)
            {
                float spread = (i - 2) * 0.03f;
                var s = Prim(PrimitiveType.Cylinder, "Stem", bouquet, Vector3.zero, new Vector3(0.008f, 0.12f, 0.008f), stem);
                s.transform.localPosition = new Vector3(spread, 0.02f, 0.12f); s.transform.localRotation = Quaternion.Euler(90f, 0f, (float)rng.NextDouble() * 20f - 10f);
                Object.DestroyImmediate(s.GetComponent<Collider>());
                var head = Prim(PrimitiveType.Sphere, "Head", bouquet, Vector3.zero, new Vector3(0.075f, 0.04f, 0.065f), Mat("WiltedHead" + (i % 3), null, heads[i % 3], 1f, 0.1f));
                head.transform.localPosition = new Vector3(spread * 1.6f, 0.02f, 0.25f + (float)rng.NextDouble() * 0.03f);   // drooping on the floor
                Object.DestroyImmediate(head.GetComponent<Collider>());
            }
        }

        // ---- Ray's jacket and shoes at the top of the service stairs, by the landing's east wall
        var cloth = Mat("RayJacket", null, new Color(0.22f, 0.24f, 0.2f), 1f, 0.1f);      // a dark olive work jacket
        var leather = Mat("RayShoes", null, new Color(0.13f, 0.1f, 0.08f), 1f, 0.3f);
        var jacket = new GameObject("RaysJacket").transform;
        jacket.SetParent(g);
        jacket.SetPositionAndRotation(new Vector3(-52.9f, -1.3f, 27.5f), Quaternion.Euler(0f, 200f, 0f));
        var body = Prim(PrimitiveType.Cube, "Body", jacket, Vector3.zero, new Vector3(0.5f, 0.07f, 0.42f), cloth);
        body.transform.localPosition = new Vector3(0f, 0.035f, 0f); body.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        body.GetComponent<BoxCollider>().size = new Vector3(1.4f, 4f, 1.4f);   // easy to look at
        body.AddComponent<ReadableNote>().text = "A worn work jacket, damp and cold.\n\"RAY\" is written on the collar in marker.\nHis shoes are beside it, laces still tied.";
        foreach (float side in new[] { -1f, 1f })   // sleeves, folded in
        {
            var sleeve = Prim(PrimitiveType.Capsule, "Sleeve", jacket, Vector3.zero, new Vector3(0.1f, 0.22f, 0.07f), cloth);
            sleeve.transform.localPosition = new Vector3(side * 0.18f, 0.06f, 0.05f); sleeve.transform.localRotation = Quaternion.Euler(90f, side * 35f, 90f);
            Object.DestroyImmediate(sleeve.GetComponent<Collider>());
        }
        var collar = Prim(PrimitiveType.Cube, "Collar", jacket, Vector3.zero, new Vector3(0.3f, 0.05f, 0.08f), cloth);
        collar.transform.localPosition = new Vector3(0f, 0.08f, -0.2f); collar.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        Object.DestroyImmediate(collar.GetComponent<Collider>());
        foreach (float side in new[] { -0.07f, 0.07f })   // the shoes, side by side, as if stepped out of
        {
            var shoe = new GameObject("RaysShoe").transform;
            shoe.SetParent(g);
            shoe.SetPositionAndRotation(new Vector3(-52.75f + side, -1.3f, 28.2f), Quaternion.Euler(0f, 175f + side * 60f, 0f));
            var sole = Prim(PrimitiveType.Cube, "Shoe", shoe, Vector3.zero, new Vector3(0.1f, 0.09f, 0.27f), leather);
            sole.transform.localPosition = new Vector3(0f, 0.045f, 0f); sole.transform.localRotation = Quaternion.identity;
            Object.DestroyImmediate(sole.GetComponent<Collider>());
            var toe = Prim(PrimitiveType.Sphere, "Toe", shoe, Vector3.zero, new Vector3(0.1f, 0.08f, 0.1f), leather);
            toe.transform.localPosition = new Vector3(0f, 0.04f, 0.13f);
            Object.DestroyImmediate(toe.GetComponent<Collider>());
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
