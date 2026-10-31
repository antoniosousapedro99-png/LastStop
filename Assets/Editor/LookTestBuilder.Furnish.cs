using UnityEngine;

// Furniture for rooms that felt empty (the director's look-through, 2026-10-31), from packs already in the
// project: a couch and a fridge in Hollis St's staff room; a shelf of file binders, a couch and stacked boxes
// in Canal St's station master's office; fuel drums, a crate and a shelf in the generator room; boxes by
// the ticket booth and an extra bin on the mezzanine. Built before each station's walkable area is baked,
// so Frankie walks round them. Crates, drums, boxes and bins can be kicked (LookTestBuilder.Clutter.cs).
public static partial class LookTestBuilder
{
    const string Derelict = "Props/DerelictFurniture/";

    static void FurnishHollis()
    {
        var g = Group("Furnishing");
        Physics.SyncTransforms();
        const float f = -1.3f;   // the staff room's floor
        // The break room's couch against the west wall (past the locker), the fridge in the north-east corner.
        AgainstWall(Kit(Derelict + "Couch.fbx", null, Derelict + "couch256x256.png", g, new Vector3(40.8f, f, 29.4f), 180f, 0.85f, true), Vector3.left);
        AgainstWall(Kit(Derelict + "Fridge.fbx", null, Derelict + "fridge256x256.png", g, new Vector3(45.4f, f, 26.5f), 0f, 1.6f, true), Vector3.right);
    }

    static void FurnishCanal(Transform canal)
    {
        var g = new GameObject("Furnishing").transform;
        g.SetParent(canal);
        Physics.SyncTransforms();
        const float f = 6f;   // the mezzanine level
        float z = CanalZ;

        // ---- the station master's office (x 54..62, z +21.3..+28): a shelf of binders, a couch, boxes
        var shelf = AgainstWall(Kit(Derelict + "Shelf.fbx", null, Derelict + "shelf256x256.png", g, new Vector3(61.4f, f, z + 24.2f), 0f, 1.8f, true), Vector3.right);
        Binders(g, shelf, 4);
        AgainstWall(Kit(Derelict + "Couch.fbx", null, Derelict + "couch256x256.png", g, new Vector3(54.7f, f, z + 24.4f), 180f, 0.85f, true), Vector3.left);
        Boxes(g, new Vector3(61.4f, f, z + 22f), 2, 1);

        // ---- the generator room (x 64..72, z -7..0): fuel drums and a crate in the corners, a shelf
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(71.3f, f, z - 6.4f), 20f, 0.9f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(70.4f, f, z - 6.5f), 75f, 0.9f, true);
        var crate = IndustrialPiece(g, "CrateWood", new Vector3(0.7f, 0.6f, 0.7f));
        crate.transform.rotation = Quaternion.Euler(0f, 12f, 0f) * crate.transform.rotation;
        StandOn(crate, new Vector3(64.8f, f, z - 6.3f));
        var genShelf = AgainstWall(Kit(Derelict + "Shelf.fbx", null, Derelict + "shelf256x256.png", g, new Vector3(71.4f, f, z - 3f), 0f, 1.8f, true), Vector3.right);
        Binders(g, genShelf, 2);

        // ---- the mezzanine: spare boxes stacked by the ticket booth, a bin on the north wall's east end
        Boxes(g, new Vector3(59f, f, z + 0.6f), 2, 2);
        BackToWall(Prop("trash_can", g, new Vector3(78.6f, f, z + 0.7f), 0f), Vector3.back, z);
    }

    // Slides a piece of furniture until its back touches the nearest wall in 'towards' (found by a ray from its middle).
    static GameObject AgainstWall(GameObject go, Vector3 towards)
    {
        var b = Bounds(go);
        Vector3 from = new Vector3(b.center.x, b.min.y + 0.5f, b.center.z);
        var own = go.GetComponentsInChildren<Collider>();
        foreach (var c in own) c.enabled = false;
        if (Physics.Raycast(from, towards, out var wall, 3f, ~0, QueryTriggerInteraction.Ignore))
            BackToWall(go, towards, towards.x != 0f ? wall.point.x : wall.point.z);
        else Debug.LogWarning("Furnishing: no wall behind " + go.name + " at " + from);
        foreach (var c in own) c.enabled = true;
        return go;
    }

    // A few file binders standing in a row on a shelf's boards (found by rays down through the shelf).
    static void Binders(Transform parent, GameObject shelf, int count)
    {
        Physics.SyncTransforms();
        var b = Bounds(shelf);
        bool alongZ = b.size.z > b.size.x;
        // Rays down from just under the shelf's top find the board below it.
        Vector3 from = new Vector3(b.center.x, b.max.y - 0.35f, b.center.z);
        if (!Physics.Raycast(from, Vector3.down, out var board, 1.5f, ~0, QueryTriggerInteraction.Ignore)) return;
        for (int i = 0; i < count; i++)
        {
            float along = (i - (count - 1) * 0.5f) * 0.09f;
            var binder = Kit("Props/Office/file_binder.fbx", null, "Props/Office/file_binder.jpg", parent,
                             board.point + (alongZ ? Vector3.forward : Vector3.right) * along, alongZ ? 0f : 90f, 0.3f);
            binder.name = "FileBinder";
        }
    }

    // Cardboard boxes stacked 'high' deep in 'stacks' side by side (along x), a little crooked.
    static void Boxes(Transform parent, Vector3 floorPoint, int high, int stacks)
    {
        var rng = new System.Random((int)(floorPoint.x * 13f + floorPoint.z));
        for (int s = 0; s < stacks; s++)
        {
            Vector3 at = floorPoint + Vector3.left * (s * 0.55f);
            for (int h = 0; h < high; h++)
            {
                var box = ModelPiece(TrashFbx, h == 0 ? "CardboardBox_1" : "CardboardBox_3", parent, Tex2("Props/Trash/CardboardPaper.png"), 1f);
                var bb = Bounds(box);
                box.transform.localScale *= (h == 0 ? 0.5f : 0.4f) / Mathf.Max(bb.size.x, bb.size.z, 0.01f);
                box.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 20f - 10f, 0f) * box.transform.rotation;
                StandOn(box, at);
                Physics.SyncTransforms();
                at.y = Bounds(box).max.y;   // the next one sits on top
            }
        }
    }
}
