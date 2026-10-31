using Unity.AI.Navigation;
using UnityEngine;

// Canal St's parked train: three intact carriages standing in the north-east tunnel (east of the ceiling
// collapse), every doorway shut except one pair of side doors forced half open on the north side of the
// middle car. Sousa vaults up into it from the track bed (Frankie leaps in after him). Dim orange emergency
// lights still burn in each car; at the far end, behind the driver's cab partition (door open), the fuel can
// lies on the cab floor by the controls.
public static partial class LookTestBuilder
{
    const float ParkedTrainWestX = 78f;   // the first car's west end

    static void CanalParkedTrain(Transform canal)
    {
        var g = new GameObject("ParkedTrain").transform;
        g.SetParent(canal);
        float z = CanalZ + 2.9f;   // the north track's middle
        var cars = TrainOfCars(g, ParkedTrainWestX, z, 3, 1, -1f, out float floorY);   // the middle car's north doors forced half open
        float wallN = Bounds(cars[0]).min.z + 0.07f;   // the middle of the north side wall

        // The train's two outer end doorways: shut.
        var first = Bounds(cars[0]); var last = Bounds(cars[2]);
        EndDoor(g, first.min.x + 0.08f, first.center.z, floorY);
        EndDoor(g, last.max.x - 0.08f, last.center.z, floorY);

        // The driver's cab at the far (east) end of the last car: its partition (door left open), the controls,
        // and the fuel can on the floor.
        const float cabDepth = 2.6f;
        CabPartition(g, cars[2], last.max.x - cabDepth, floorY, 1, doorOpen: true);
        var desk = Mat("ConsoleGrey", null, new Color(0.3f, 0.32f, 0.35f), 1f, 0.35f, 0.3f);
        float cx = last.max.x - 1.3f;
        Span("CabDesk", g, new Vector3(cx - 0.45f, floorY, wallN + 0.13f), new Vector3(cx + 0.45f, floorY + 1f, wallN + 0.58f), desk);
        var lever = Prim(PrimitiveType.Cube, "CabHandle", g, new Vector3(cx, floorY + 1.12f, wallN + 0.4f), new Vector3(0.05f, 0.22f, 0.05f), Mat("HandleBlack", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.5f));
        lever.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
        Object.DestroyImmediate(lever.GetComponent<Collider>());
        PlaceFuelCan(g, new Vector3(cx, floorY, last.center.z + 0.5f), 35f);

        // The doorway into the cab is too narrow for his walkable area: a walking route through it (WalkLink; the
        // gaps between the cars have theirs from TrainOfCars), so he can follow Sousa all the way down the train.
        float cabWall = last.max.x - cabDepth;
        WalkLink(g, "TrainCabRoute", null, new Vector3(cabWall - 1.2f, floorY, z), new Vector3(cabWall, floorY, z), new Vector3(cabWall + 1f, floorY, z));

        // Frankie's way in: a leap from the track bed through the forced door.
        var mid = Bounds(cars[1]);
        // The car's skirt below the door isn't solid either: an invisible step there gives Sousa a ledge to vault.
        var sill = new GameObject("ForcedDoorSill").AddComponent<BoxCollider>();
        sill.transform.SetParent(g, true);
        sill.transform.position = new Vector3(mid.center.x, (-1.3f + floorY) * 0.5f, mid.min.z + 0.2f);
        sill.size = new Vector3(1.2f, floorY + 1.3f, 0.4f);
        var link = new GameObject("ParkedTrainDoorLink").AddComponent<NavMeshLink>();
        link.transform.SetParent(g);
        link.transform.position = Vector3.zero;
        link.startPoint = new Vector3(mid.center.x, -1.3f, mid.min.z - 0.7f);
        link.endPoint = new Vector3(mid.center.x, floorY, mid.min.z + 0.9f);
        link.width = 0f;
        link.bidirectional = true;
    }

    // A row of 'count' intact carriages on the track at 'z', the first one's west end at 'westX', 0.2 m apart:
    // every side door shut (except, if 'forcedCar' >= 0, the middle door of that car on side 'forcedSide' (-1
    // north, +1 south), forced half open), thick invisible walls along both sides (the cars' thin sides aren't
    // solid), floor plates and walking routes for Frankie across the gaps, and a dim orange emergency strip in
    // each car. Returns the cars; 'floorY' is their floor. (Hollis St's departure train uses it.)
    static GameObject[] TrainOfCars(Transform g, float westX, float z, int count, int forcedCar, float forcedSide, out float floorY)
    {
        var doorMat = Mat("TrainDoor", null, new Color(0.55f, 0.56f, 0.58f), 1f, 0.35f, 0.5f);
        var glass = Mat("TrainDoorWindow", null, new Color(0.04f, 0.045f, 0.05f), 1f, 0.9f, 0.2f);
        var glow = EmissiveMat("EmergencyStrip", new Color(1f, 0.45f, 0.15f) * 1.5f);
        var cars = new GameObject[count];
        float x = westX;
        floorY = -0.25f;
        for (int i = 0; i < count; i++)
        {
            var car = Place("Assets/Art/Models/Environment/MetroPSX/SubwayCarOpen.fbx", g, Vector3.zero, 0f);
            car.name = "TrainCar_" + (i + 1);
            foreach (var r in car.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) if (mats[m]) mats[m] = PropMat(mats[m].name);
                r.sharedMaterials = mats;
            }
            var b = Bounds(car);
            if (b.size.z > b.size.x) { car.transform.rotation = Quaternion.Euler(0, 90f, 0) * car.transform.rotation; b = Bounds(car); }
            car.transform.localScale *= CarLength / b.size.x;
            b = Bounds(car);
            StandOn(car, new Vector3(x + b.size.x * 0.5f, -1.3f, z));
            cars[i] = car;
            x += b.size.x + 0.2f;
        }
        Physics.SyncTransforms();
        var fb = Bounds(cars[0]);
        if (Physics.Raycast(new Vector3(fb.center.x, fb.min.y + fb.size.y * 0.6f, fb.center.z), Vector3.down, out var fh, 4f)) floorY = fh.point.y;
        float doorBottom = fb.min.y + 0.2645f * fb.size.y, doorTop = fb.min.y + 0.7274f * fb.size.y, doorH = doorTop - doorBottom;
        float wallN = fb.min.z + 0.07f, wallS = fb.max.z - 0.07f;
        for (int i = 0; i < count; i++)
        {
            var cb = Bounds(cars[i]);
            // Doors: the three wide doorways each side slide open and shut (Use); the narrow cab door at one end of
            // the model stays shut (its leaves sit hidden inside the wall at the other end).
            foreach (var (frac, width) in new[] { (-0.2997f, 1.41f), (0f, 1.41f), (0.2997f, 1.41f), (-0.4384f, 0.66f), (0.4384f, 0.66f) })
            {
                float dx = cb.center.x + frac * cb.size.x;
                foreach (float side in new[] { -1f, 1f })
                {
                    bool forced = i == forcedCar && frac == 0f && side == forcedSide;
                    TrainDoorLeaves(g, new Vector3(dx, doorBottom, side < 0 ? wallN : wallS), width, doorH, forced ? 0.48f : 0f, doorMat, glass, width > 1f);
                }
            }
            // Invisible walls along both sides, with a gap at each wide doorway (its leaves block it when shut).
            foreach (float side in new[] { -1f, 1f })
            {
                float wz = side < 0 ? wallN : wallS, from = cb.min.x;
                foreach (float frac in new[] { -0.2997f, 0f, 0.2997f })
                {
                    float dx = cb.center.x + frac * cb.size.x;
                    InnerWall(g, from, dx - 0.5f, wz, floorY);
                    from = dx + 0.5f;
                }
                InnerWall(g, from, cb.max.x, wz, floorY);
                // Benches along the wall between the doors, facing across the aisle, short enough to leave
                // 25 cm clear of the doorway on each side.
                foreach (float off in new[] { -0.15f, 0.15f })
                {
                    float bx = cb.center.x + off * cb.size.x;
                    var seat = Prop("seats", g, new Vector3(bx, floorY, wz - side * 0.4f), side < 0 ? -90f : 90f);
                    if (!seat) continue;
                    FitAlongX(seat, 0.2997f * cb.size.x - 1.41f - 0.5f);
                    StandOn(seat, new Vector3(bx, floorY, wz - side * (0.08f + Bounds(seat).extents.z)));
                }
            }
            if (i < count - 1)
            {
                Span("CarGapPlate_" + i, g, new Vector3(cb.max.x - 0.05f, floorY - 0.03f, cb.center.z - 0.5f), new Vector3(cb.max.x + 0.25f, floorY, cb.center.z + 0.5f), doorMat);
                float gapX = cb.max.x + 0.1f;
                WalkLink(g, "TrainGapRoute_" + i, null, new Vector3(gapX - 1.2f, floorY, z), new Vector3(gapX, floorY, z), new Vector3(gapX + 1.2f, floorY, z));
            }
            // Three emergency strips along each car's ceiling (one in the middle left most of the car pitch dark;
            // the director asked for every car to be lit, still dim, 2026-10-31).
            for (int k = -1; k <= 1; k++)
            {
                float sx = cb.center.x + k * cb.size.x / 3.2f;
                var strip = Prim(PrimitiveType.Cube, "EmergencyStrip", g, new Vector3(sx, floorY + 2.15f, cb.center.z), new Vector3(2f, 0.03f, 0.08f), glow);
                Object.DestroyImmediate(strip.GetComponent<Collider>());
                var lamp = new GameObject("EmergencyLight").AddComponent<Light>();
                lamp.transform.SetParent(g);
                lamp.transform.position = new Vector3(sx, floorY + 2f, cb.center.z);
                lamp.type = LightType.Point; lamp.color = new Color(1f, 0.5f, 0.2f); lamp.intensity = 0.8f; lamp.range = 6.5f; lamp.shadows = LightShadows.None;
            }
        }
        return cars;
    }

    // True if a horizontal ray from outside the car at 'from' (toward the car, direction 'dirZ') finds no wall
    // within 1.2 m: a doorway.
    static bool IsHole(Vector3 from, float dirZ) => !Physics.Raycast(from, new Vector3(0f, 0f, dirZ), 1.2f, ~0, QueryTriggerInteraction.Ignore);

    // A pair of sliding door leaves filling a side doorway (bottom middle at 'sill'), each with a dark window;
    // 'apart' slides each leaf that far into the wall, leaving a gap between them.
    static void TrainDoorLeaves(Transform parent, Vector3 sill, float width, float height, float apart, Material metal, Material glass, bool slides = false)
    {
        float leaf = width * 0.5f;
        var pair = new GameObject("TrainDoors").transform;
        pair.SetParent(parent);
        pair.position = sill + Vector3.up * height * 0.5f;
        Transform[] leaves = new Transform[2];
        int n = 0;
        foreach (float s in new[] { -1f, 1f })
        {
            float cx = sill.x + s * (leaf * 0.5f + apart);
            var door = Box("TrainDoorLeaf", pair, new Vector3(cx, sill.y + height * 0.5f, sill.z), new Vector3(leaf, height, 0.04f), metal);
            leaves[n++] = door.transform;
            if (apart > 0f) door.transform.rotation = Quaternion.Euler(0f, s * 4f, s * 1.5f);   // wrenched a little crooked
            var pane = Box("TrainDoorWindow", door.transform, new Vector3(cx, sill.y + height * 0.68f, sill.z), new Vector3(leaf * 0.55f, height * 0.3f, 0.05f), glass);
            pane.transform.rotation = door.transform.rotation;
            Object.DestroyImmediate(pane.GetComponent<Collider>());
        }
        if (!slides) return;
        var slide = pair.gameObject.AddComponent<SlidingTrainDoor>();
        slide.leafA = leaves[0]; slide.leafB = leaves[1];
        slide.along = Vector3.right; slide.apart = 0.48f; slide.startOpen = apart > 0f;
        slide.slideSound = Load<AudioClip>("Assets/Art/Audio/SFX/Train/MetroDoor.ogg");   // the director's metro door sound
    }

    // A closed steel door in a car's end doorway at 'x', with an invisible wall across the car's width.
    static void EndDoor(Transform parent, float x, float z, float floorY)
    {
        Box("TrainEndDoor", parent, new Vector3(x, floorY + 0.97f, z), new Vector3(0.04f, 1.94f, 0.95f), Mat("DoorMetal", Tex("Door_01.jpg"), new Color(0.7f, 0.7f, 0.7f), 1f, 0.3f, 0.3f));
        var block = new GameObject("TrainEndBlock").AddComponent<BoxCollider>();
        block.transform.SetParent(parent, true);
        block.transform.position = new Vector3(x, floorY + 1.2f, z);
        block.size = new Vector3(0.1f, 2.4f, 3f);
    }

    static void InnerWall(Transform parent, float x0, float x1, float z, float floorY)
    {
        var wall = new GameObject("TrainInnerWall").AddComponent<BoxCollider>();
        wall.transform.SetParent(parent, true);
        wall.transform.position = new Vector3((x0 + x1) * 0.5f, floorY + 1.2f, z);
        wall.size = new Vector3(x1 - x0, 2.4f, 0.35f);   // thick enough to stop the player from either side
    }
}
