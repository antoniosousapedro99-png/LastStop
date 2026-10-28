using UnityEngine;

// The driver's cab partition in a stalled train: the carriage's own end wall (SubwayCarEnd.fbx, cut from
// the same model by Tools/Blender/widen_car.py) standing across the car, its plain inner side facing the
// cab, with a closed steel door in its doorway. Through its two windows you see the dark carriage behind.
public static partial class LookTestBuilder
{
    // Every stalled carriage is scaled to this length. At 18.7 m its widened end doorway is 0.9 m wide and
    // 1.95 m tall, just enough for the player, and its roof still clears the 3 m tunnel ceilings.
    const float CarLength = 18.7f;

    // 'car' is the placed carriage; the partition stands with its cab side at 'atX', facing 'towardCab'
    // (+1 or -1 along x). Returns the partition so callers can move it with the car.
    static Transform CabPartition(Transform parent, GameObject car, float atX, float floorY, int towardCab, bool doorOpen = false)
    {
        var wall = Place("Assets/Art/Models/Environment/MetroPSX/SubwayCarEnd.fbx", parent, Vector3.zero, 0f);
        wall.name = "CabPartition";
        foreach (var r in wall.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = PropMat(mats[i].name);
            r.sharedMaterials = mats;
        }
        // Put it exactly where it sits on the carriage (same position, turn and size): it is then one of
        // the car's ends, so its outward side points away from the car's middle.
        wall.transform.SetPositionAndRotation(car.transform.position, car.transform.rotation);
        wall.transform.localScale = car.transform.localScale;
        var carBounds = Bounds(car);
        float outward = Mathf.Sign(Bounds(wall).center.x - carBounds.center.x);
        // The plain inner side must face the cab, so the outward side points away from it.
        if (outward == towardCab) wall.transform.RotateAround(carBounds.center, Vector3.up, 180f);
        var b = Bounds(wall);
        float shiftX = towardCab > 0 ? atX - b.max.x : atX - b.min.x;
        wall.transform.position += new Vector3(shiftX, floorY - 0.03f - b.min.y, carBounds.center.z - b.center.z);
        b = Bounds(wall);

        if (doorOpen)
        {
            // Doorway left open: invisible walls either side of it, so nobody climbs through the windows.
            foreach (float s in new[] { -1f, 1f })
            {
                var half = new GameObject("CabPartitionCollider").AddComponent<BoxCollider>();
                half.transform.SetParent(wall.transform, true);
                float inner = 0.5f, outer = carBounds.extents.z;
                half.transform.position = new Vector3(b.center.x, floorY + 1.2f, carBounds.center.z + s * (inner + outer) * 0.5f);
                half.size = new Vector3(b.size.x, 2.4f, outer - inner);
            }
            return wall.transform;
        }
        // A closed steel door filling the doorway (0.9 m by 1.94 m), in the middle of the wall's thickness.
        var door = Box("CabDoor", wall.transform, new Vector3(b.center.x, floorY + 0.97f, carBounds.center.z), new Vector3(0.04f, 1.94f, 0.92f),
                       Mat("DoorMetal", Tex("Door_01.jpg"), new Color(0.7f, 0.7f, 0.7f), 1f, 0.3f, 0.3f));
        // One invisible wall across the whole car, so nobody climbs through the windows.
        var block = new GameObject("CabPartitionCollider").AddComponent<BoxCollider>();
        block.transform.SetParent(wall.transform, true);
        block.transform.position = new Vector3(b.center.x, floorY + 1.2f, carBounds.center.z);
        block.size = new Vector3(b.size.x, 2.4f, carBounds.size.z);
        return wall.transform;
    }
}
