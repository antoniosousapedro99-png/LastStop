using UnityEngine;

// Canal St's supplies and extra platform clutter (the director's choices, 2026-10-31).
// Supplies, lean (2 of each), left where people would have left them:
//   flares: under the red emergency lamp in the arrival tunnel (20 m out from the platform), and by the
//           electrical cabinets in the lower level's machinery hall;
//   batteries: on the station master's desk, and on the floor of the lower level's electrical room;
//   painkillers: on the ticket booth's back counter, and on a storeroom shelf on the lower level.
// Clutter: about a dozen more cans, glass bottles, cardboard boxes and a bin bag over the platform floor,
// knocked about like the rest; they settle with the other litter and can be kicked (LookTestBuilder.Clutter.cs).
public static partial class LookTestBuilder
{
    static void CanalSupplies(Transform canal)
    {
        var g = new GameObject("Supplies").transform;
        g.SetParent(canal);
        Physics.SyncTransforms();   // the rays below must see the rooms just built
        float z = CanalZ;

        if (Surface(new Vector3(-60f, 1f, z + 0.7f), "flare, arrival tunnel", out var p)) PlaceFlare(g, p, 75f);
        if (Surface(new Vector3(-61.25f, -8f, z + 50.7f), "flare, machinery hall", out p)) PlaceFlare(g, p, -20f);
        if (Surface(new Vector3(58.6f, 7.5f, z + 27.15f), "battery, office desk", out p)) Battery(g, p, 35f);
        if (Surface(new Vector3(-75.3f, -8f, z + 42.6f), "battery, electrical room", out p)) Battery(g, p, -60f);
        if (Surface(new Vector3(55.7f, 7.5f, z + 0.3f), "painkillers, ticket booth", out p)) PlacePainkillers(g, p, 15f);
        if (Surface(new Vector3(-90.7f, -8.3f, z + 55.78f), "painkillers, storeroom shelf", out p)) PlacePainkillers(g, p, 40f);
    }

    static void Battery(Transform parent, Vector3 floorPoint, float yaw)
    {
        MakePickup(ModelPiece(SurvivalFbx, "Battery", parent, Tex2("Props/Survival/Battery.png"), 1.5f), global::Pickup.Kind.Battery,
                   floorPoint, yaw, "PlasticBox1", lieDown: true);
    }

    // The first solid surface straight below 'from' (logged, so a misplaced item shows in the Console).
    static bool Surface(Vector3 from, string what, out Vector3 point)
    {
        point = default;
        if (!Physics.Raycast(from, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("Supplies: nothing under the " + what + " at " + from);
            return false;
        }
        Debug.Log("Supplies: " + what + " on " + hit.collider.name + " at " + hit.point.ToString("F2"));
        point = hit.point + Vector3.up * 0.003f;
        return true;
    }

    // A dozen heavier bits of litter over the platform floor: cans, glass bottles, cardboard boxes, a bin bag.
    static void PlatformClutter(Transform parent, float z)
    {
        (string piece, string tex, float size)[] kinds =
        {
            ("Can_1", "Cans", 0.12f), ("Can_5", "Cans", 0.12f), ("Can_6_Crushed", "Cans", 0.11f), ("Can_8", "Cans", 0.12f),
            ("GlassBottle_1", "GlassBottles", 0.28f), ("GlassBottle_2", "GlassBottles", 0.26f), ("GlassBottle_3", "GlassBottles", 0.3f),
            ("CardboardBox_1", "CardboardPaper", 0.4f), ("CardboardBox_3", "CardboardPaper", 0.35f), ("CardboardBox_4", "CardboardPaper", 0.45f),
            ("TrashBag_1", "TrashBag", 0.55f), ("Can_3", "Cans", 0.12f),
        };
        var rng = new System.Random(23);
        for (int i = 0, tries = 0; i < kinds.Length && tries < 60; tries++)
        {
            var k = kinds[i];
            // Anywhere along the platform (x -37..37), clear of its edges.
            Vector3 spot = new Vector3(-37f + 74f * (float)rng.NextDouble(), 0f, z + 10.5f + ((float)rng.NextDouble() * 2f - 1f) * 4f);
            if (!Physics.Raycast(spot + Vector3.up, Vector3.down, out var floor, 3f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(floor.point.y) > 0.05f) continue;   // on a bench or a column base
            var go = ModelPiece(TrashFbx, k.piece, parent, Tex2("Props/Trash/" + k.tex + ".png"), 1f);
            var b = Bounds(go);
            go.transform.localScale *= k.size / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y, b.size.z));
            // Boxes and the bag sit roughly upright, battered; cans and bottles lie on their side.
            bool upright = k.piece.StartsWith("Cardboard") || k.piece.StartsWith("TrashBag");
            go.transform.rotation = (upright ? Messy(rng, 10f) : OnItsSide(rng)) * go.transform.rotation;
            StandOn(go, floor.point + Vector3.up * 0.01f);
            looseLitter.Add(go);   // dropped for real with the rest of the litter (SettleLitter)
            i++;
        }
    }
}
