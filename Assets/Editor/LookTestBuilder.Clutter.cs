using System.Collections.Generic;
using Fusion.Fluid;
using UnityEngine;

// Kickable clutter (the director's choices, 2026-10-31): once the whole level is built and the litter has
// settled, the loose small and medium props get a physics body and a Kickable (see Kickable.cs), and Sousa
// and every Frankie get a ClutterKicker, so walking into a can sends it skittering and a crate is shoved along.
// Small: cans, cups, food boxes, chip bags, bin bags, glass bottles, cardboard boxes, single bricks, traffic cones. Medium: crates, barrels,
// bins, tyres, office chairs, brick piles, the shopping trolley. Left alone: anything you pick up or use,
// the climbing crates (into both trains) and their bricks, the train wreck's rubble, the scare that drops rubble, and anything
// with something standing on it (a crate with candles on top), so nothing is left floating.
public static partial class LookTestBuilder
{
    // What each kind of clutter is: its sound, small (walked through) or medium (solid), its weight in kg
    // and how fast it leaves the foot compared to the kicker's speed.
    static (Kickable.Sound sound, bool small, float mass, float kick)? ClutterKind(string name)
    {
        name = name.ToLowerInvariant();
        if (name.Contains("trash_can")) return (Kickable.Sound.Metal, false, 8f, 0.85f);
        if (name.Contains("shoppingcart")) return (Kickable.Sound.Metal, false, 14f, 1.1f);
        if (name.Contains("barrel")) return (Kickable.Sound.Metal, false, 18f, 0.85f);
        if (name.Contains("crate")) return (Kickable.Sound.Wood, false, 12f, 0.85f);
        if (name.Contains("chair")) return (Kickable.Sound.Plastic, false, 10f, 1f);
        if (name.Contains("tire")) return (Kickable.Sound.Rubber, false, 9f, 0.8f);
        if (name == "bricks") return (Kickable.Sound.Stone, false, 20f, 0.5f);
        if (name.Contains("glassbottle")) return (Kickable.Sound.Glass, true, 0.35f, 1.2f);
        if (name.Contains("cardboardbox")) return (Kickable.Sound.Soft, true, 0.5f, 0.9f);
        if (name.Contains("cone")) return (Kickable.Sound.Plastic, true, 2.5f, 1f);
        if (name.Contains("chip_bag")) return (Kickable.Sound.Plastic, true, 0.05f, 1.2f);
        if (name.Contains("bag")) return (Kickable.Sound.Soft, true, 2.5f, 0.45f);
        if (name.Contains("brick")) return (Kickable.Sound.Stone, true, 2.5f, 0.6f);
        if (name.Contains("cup") || name.Contains("fry_container")) return (Kickable.Sound.Plastic, true, 0.05f, 1.2f);
        if (name.StartsWith("can_")) return (Kickable.Sound.Tin, true, 0.25f, 1.3f);
        return null;
    }

    // Groups whose props stay fixed (puzzle pieces, the wreck, scripted scares, things already afloat).
    static readonly string[] FixedClutterGroups = { "ClimbCrates", "CabClimbCrate", "Pickups", "TrainWreck", "CanalScares", "FloatingRubbish", "StillFloaters" };

    static void KickableClutter()
    {
        var player = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player) player.gameObject.AddComponent<ClutterKicker>().isPlayer = true;
        foreach (var frankie in Object.FindObjectsByType<FrankieAI>(FindObjectsInactive.Include))
            frankie.gameObject.AddComponent<ClutterKicker>().passThroughAll = true;

        var everything = root.GetComponentsInChildren<Renderer>(true);
        int made = 0;
        var kept = new List<string>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var filter = t.GetComponentInChildren<MeshFilter>(true);
            // Washed-up junk is all named alike: tell what it is by its model.
            string name = t.name == "WashedUpJunk" && filter && filter.sharedMesh ? filter.sharedMesh.name : t.name;
            var kind = ClutterKind(name);
            if (kind == null || !filter) continue;
            if (t.GetComponentInParent<Rigidbody>(true) || t.GetComponentInParent<IInteractable>(true) != null) continue;
            if (InFixedGroup(t)) continue;
            var k = kind.Value;
            if (!k.small && HoldsSomething(t, everything)) { kept.Add(t.name); continue; }
            MakeKickable(t.gameObject, k.sound, k.small, k.mass, k.kick);
            made++;
        }
        Debug.Log("Kickable clutter: " + made + " props" + (kept.Count > 0 ? "; left fixed (something stands on them): " + string.Join(", ", kept) : ""));
    }

    static bool InFixedGroup(Transform t)
    {
        for (var p = t; p; p = p.parent)
            if (System.Array.IndexOf(FixedClutterGroups, p.name) >= 0) return true;
        return false;
    }

    // Something else resting on its top (candles on a crate, a box on a barrel)?
    static bool HoldsSomething(Transform t, Renderer[] everything)
    {
        var b = Bounds(t.gameObject);
        foreach (var r in everything)
        {
            if (r.transform.IsChildOf(t)) continue;
            var o = r.bounds;
            if (Mathf.Max(o.size.x, o.size.y, o.size.z) > 2.5f) continue;   // walls, floors, ceilings
            if (Mathf.Abs(o.min.y - b.max.y) > 0.06f) continue;
            if (o.center.x < b.min.x || o.center.x > b.max.x || o.center.z < b.min.z || o.center.z > b.max.z) continue;
            return true;
        }
        return false;
    }

    static void MakeKickable(GameObject go, Kickable.Sound sound, bool small, float mass, float kick)
    {
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        // One model piece: a convex hull of it (cans and cones roll). Several: a box round them all.
        var filters = go.GetComponentsInChildren<MeshFilter>(true);
        var own = go.GetComponent<MeshFilter>();
        if (filters.Length == 1 && own && own.sharedMesh)
        {
            var hull = go.AddComponent<MeshCollider>();
            hull.sharedMesh = own.sharedMesh;
            hull.convex = true;
        }
        else LocalBoxCollider(go, filters);

        var body = go.AddComponent<Rigidbody>();
        body.mass = mass;
        body.linearDamping = small ? 0.1f : 0.5f;
        body.angularDamping = small ? 0.3f : 1f;
        body.interpolation = RigidbodyInterpolation.Interpolate;   // smooth to the eye between physics steps
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // fast cans don't pass through walls

        var clutter = go.AddComponent<Kickable>();
        clutter.sound = sound; clutter.small = small; clutter.kickScale = kick;
        (clutter.clips, clutter.pitch, clutter.volume) = sound switch
        {
            Kickable.Sound.Tin => (Clips("Hammer1", "Hammer2"), 1.9f, 0.35f),
            Kickable.Sound.Plastic => (Clips("FoodPackagingWaterBottle1", "FoodPackagingWaterBottle2", "FoodPackagingWaterBottle3", "PlasticBox1"), 1.1f, 0.5f),
            Kickable.Sound.Soft => (Clips("PaperDocument1", "PaperDocument2"), 0.6f, 0.5f),
            Kickable.Sound.Stone => (Clips("Hammer1", "Hammer2"), 0.5f, 0.6f),
            Kickable.Sound.Wood => (Clips("DesignedPunch1", "DesignedPunch2"), 0.65f, 0.8f),
            Kickable.Sound.Metal => (Clips("MetalCabinet1", "Hammer2"), 0.7f, 0.7f),
            Kickable.Sound.Glass => (Clips("SmallGlassBottles1"), 1f, 0.45f),
            _ => (Clips("DesignedPunch1", "DesignedPunch2"), 0.45f, 0.6f),
        };

        // In the sewers, light things kicked into the water float (FusionWater); stone and steel sink.
        if (IsUnder(go.transform, "Sewers") && sound != Kickable.Sound.Stone && sound != Kickable.Sound.Metal)
        {
            var floater = go.AddComponent<BasicFluidInteractor>();
            floater.customVolume = mass / (2f * 0.05f);   // rides about 5 cm deep (as the floating rubbish)
        }

        // Small things on the Ignore Raycast layer: they never block the crosshair from something you can use.
        if (small) foreach (var part in go.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 2;
    }

    static bool IsUnder(Transform t, string group)
    {
        for (var p = t; p; p = p.parent) if (p.name == group) return true;
        return false;
    }

    // A box round all the model's pieces, in the object's own axes (so a tipped-over bin gets a tipped box).
    static void LocalBoxCollider(GameObject go, MeshFilter[] filters)
    {
        var t = go.transform;
        bool any = false;
        var local = new Bounds();
        foreach (var f in filters)
        {
            if (!f.sharedMesh) continue;
            var mb = f.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                var p = t.InverseTransformPoint(f.transform.TransformPoint(corner));
                if (!any) { local = new Bounds(p, Vector3.zero); any = true; } else local.Encapsulate(p);
            }
        }
        var box = go.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;
    }
}
