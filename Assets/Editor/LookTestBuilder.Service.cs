using System.Linq;
using UnityEngine;

// The service level under the west tunnel: industrial, run down, a little "backrooms". The first time the
// player reaches the bottom of the stairs, Frankie wakes upstairs and comes searching; he can follow
// down the stairs while the maintenance door is open (LookTestBuilder.Monster.cs bakes this level too).
// - A rusted maintenance door in the west tunnel's outer wall (x -55) opens onto a landing and square steel
//   stairs running down the shaft walls (four flights, corner landings, railings), 8 m below the tracks.
// - A short corridor leads to the machinery hall (generator, turbine, transformers, pumps, a battery work
//   light, a pipe you can knock on). West of it a storage room; east of it a squatter camp (mattresses,
//   candles still burning, a radio hissing static, notes) where the west fuse now lies.
// - Sounds with no source (footsteps overhead, a cough from the empty camp, a whisper on the stairs) and
//   things that change when you look away (the camp chair, a candle).
// - Industrial dressing (BuildServiceIndustrial): pipes along the ceilings and walls, breaker boards and
//   cabinets, chain-link cages round the transformers and the pumps, dim caged bulbs; three hiding lockers.
public static partial class LookTestBuilder
{
    const float ServiceDoorX = -55f;        // west edge of the maintenance doorway in the west tunnel
    const float SvcFloor = -9.3f;           // the service level's floor
    static readonly Vector3 StairCentre = new Vector3(-54.5f, 0f, 31.3f);
    const string SewerTex = "Environment/Sewer/";

    static Material svcWall, svcFloor, svcCeiling, svcBrick, svcMetal;

    static void BuildServiceLevel()
    {
        var level = Group("ServiceLevel");
        // Tints chosen so the surfaces end up about as bright as the tunnels' concrete (darkness comes from missing light).
        svcWall = Mat("SvcConcrete", Tex2(SewerTex + "concrete_dirty.jpg"), new Color(1f, 0.97f, 0.92f), 2.5f, 0.1f);
        svcFloor = Mat("SvcFloor", Tex2(SewerTex + "concrete_dirty_2.jpg"), new Color(0.78f, 0.78f, 0.76f), 3f, 0.15f);   // grimy bare concrete
        svcCeiling = Mat("SvcCeiling", Tex2(SewerTex + "concrete_bare_01.jpg"), new Color(0.8f, 0.78f, 0.75f), 2.5f, 0.05f);
        svcBrick = Mat("SvcBrick", Tex2(SewerTex + "brick_modern.jpg"), new Color(0.72f, 0.66f, 0.62f), 2f, 0.1f);
        svcMetal = Mat("SvcMetal", Tex2(SewerTex + "Metal.jpg"), new Color(0.7f, 0.64f, 0.58f), 1f, 0.3f, 0.5f);

        IndustrialMaterials();
        var door = BuildMaintenanceDoor(level, Vector3.zero);
        BuildStairwell(level, Vector3.zero, door);
        BuildServiceRooms(level);
        BuildMachineryHall(level);
        BuildStorage(level);
        BuildSquatterCamp(level);
        BuildServiceScares(level);
        BuildServiceIndustrial(level);
    }

    // ---------------------------------------------------------------- the door in the tunnel wall
    // 'o' shifts the whole door, landing and staircase (Canal St reuses them: see LookTestBuilder.CanalLower.cs).
    static HingedDoor BuildMaintenanceDoor(Transform parent, Vector3 o)
    {
        var hinge = new GameObject("MaintenanceDoor");
        hinge.transform.SetParent(parent);
        hinge.transform.position = o + new Vector3(ServiceDoorX, -1.3f, 25.65f);
        Box("Door", hinge.transform, o + new Vector3(ServiceDoorX + 0.5f, -1.3f + 1.09f, 25.65f), new Vector3(0.96f, 2.18f, 0.05f),
            Mat("SvcDoorMetal", Tex2(SewerTex + "door_metal.jpg"), Color.white, 2.2f, 0.25f, 0.4f));
        var door = hinge.AddComponent<HingedDoor>();
        door.openAngle = -95f;                     // swings in, away from the tracks
        door.openTime = 1.8f;
        door.doorName = "maintenance door";
        door.openSound = Load<AudioClip>(SfxDir + "/OldDoorOpen.wav");
        return door;
    }

    // ---------------------------------------------------------------- landing and square steel stairs
    static void BuildStairwell(Transform parent, Vector3 o, HingedDoor door)
    {
        var g = new GameObject("Stairwell").transform;
        g.SetParent(parent);
        // Landing behind the door (floor at the tracks' level), opening straight onto the stair shaft.
        Span("LandingFloor", g, o + new Vector3(-57f, -1.5f, 25.8f), o + new Vector3(-52f, -1.3f, 28.8f), svcFloor);
        Span("ShaftWall_W", g, o + new Vector3(-57.3f, -9.5f, 25.8f), o + new Vector3(-57f, 1.9f, 33.8f), svcWall);
        Span("ShaftWall_E", g, o + new Vector3(-52f, -9.5f, 25.8f), o + new Vector3(-51.7f, 1.9f, 33.8f), svcWall);
        Span("ShaftCeiling", g, o + new Vector3(-57f, 1.7f, 25.8f), o + new Vector3(-52f, 1.9f, 33.8f), svcCeiling);
        Span("ShaftWall_S", g, o + new Vector3(-57f, -9.5f, 28.5f), o + new Vector3(-52f, -1.5f, 28.8f), svcWall);   // under the landing
        Span("ShaftWall_N", g, o + new Vector3(-57.3f, -6.3f, 33.8f), o + new Vector3(-51.7f, 1.9f, 34.1f), svcWall);
        Span("ShaftWall_NW", g, o + new Vector3(-57.3f, -9.5f, 33.8f), o + new Vector3(-55.5f, -6.3f, 34.1f), svcWall);
        Span("ShaftWall_NE", g, o + new Vector3(-52.5f, -9.5f, 33.8f), o + new Vector3(-51.7f, -6.3f, 34.1f), svcWall);
        Span("ShaftFloor", g, o + new Vector3(-57f, -9.5f, 28.8f), o + new Vector3(-52f, SvcFloor, 33.8f), svcFloor);
        Span("ShaftExitSill", g, o + new Vector3(-55.5f, -9.5f, 33.8f), o + new Vector3(-52.5f, SvcFloor, 34.1f), svcFloor);

        // Square steel stairs round the shaft: four flights running down the walls (east, north, west, south),
        // each 2 m down over 3 m (nine 0.2 m steps and the landing), a 1 m square landing in each corner, and a
        // railing all round the open well in the middle. Start: the south-east corner, level with the landing.
        var steel = Mat("StairSteel", null, new Color(0.36f, 0.37f, 0.39f), 1f, 0.45f, 0.75f);
        var rail = Mat("RailSteel", null, new Color(0.2f, 0.2f, 0.21f), 1f, 0.4f, 0.8f);
        CornerLanding(g, "Landing_SE", o.x + -53f, o.z + 28.8f, -1.3f, steel);
        CornerLanding(g, "Landing_NE", o.x + -53f, o.z + 32.8f, -3.3f, steel);
        CornerLanding(g, "Landing_NW", o.x + -57f, o.z + 32.8f, -5.3f, steel);
        CornerLanding(g, "Landing_SW", o.x + -57f, o.z + 28.8f, -7.3f, steel);
        Flight(g, "Flight_E", o + new Vector3(-52.5f, -1.3f, 29.8f), Vector3.forward, Vector3.left, steel, rail);
        Flight(g, "Flight_N", o + new Vector3(-53f, -3.3f, 33.3f), Vector3.left, Vector3.back, steel, rail);
        Flight(g, "Flight_W", o + new Vector3(-56.5f, -5.3f, 32.8f), Vector3.back, Vector3.right, steel, rail);
        Flight(g, "Flight_S", o + new Vector3(-56f, -7.3f, 29.3f), Vector3.right, Vector3.forward, steel, rail);
        // Railing along the top landing's edge (and the corner landing beside it), where it drops into the shaft.
        RailRun(g, "TopRail", o + new Vector3(-57f, -1.3f, 28.8f), o + new Vector3(-53f, -1.3f, 28.8f), rail);
        RailRun(g, "TopRail_SE", o + new Vector3(-53f, -1.3f, 28.8f), o + new Vector3(-53f, -1.3f, 29.8f), rail);

        // One dying bulb hanging on a cable in the middle of the well.
        var cable = Prim(PrimitiveType.Cylinder, "BulbCable", g, new Vector3((StairCentre.x + o.x), (1.7f - 4.1f) * 0.5f, (StairCentre.z + o.z)), new Vector3(0.015f, (1.7f + 4.1f) * 0.5f, 0.015f), rail);
        Object.DestroyImmediate(cable.GetComponent<Collider>());
        var bulb = Prim(PrimitiveType.Sphere, "StairBulb", g, new Vector3((StairCentre.x + o.x), -4.2f, (StairCentre.z + o.z)), Vector3.one * 0.12f, Mat("BulbYellow", null, new Color(1f, 0.85f, 0.5f), 1f, 0.6f));
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        var light = new GameObject("StairLight").AddComponent<Light>();
        light.transform.SetParent(g);
        light.transform.position = new Vector3((StairCentre.x + o.x), -4.35f, (StairCentre.z + o.z));
        light.type = LightType.Point; light.color = new Color(1f, 0.8f, 0.5f); light.intensity = 2f; light.range = 7f; light.shadows = LightShadows.Soft;
        var flicker = light.gameObject.AddComponent<FlickeringLight>();
        flicker.lamp = light; flicker.minCalm = 2f; flicker.maxCalm = 7f;

        // Frankie's way down: through the maintenance door (only while it's open), then down the flights.
        // His walkable area can't fit through the 1 m doorway or along the narrow flights by itself.
        WalkLink(g, "DoorRoute", door, o + new Vector3(ServiceDoorX + 0.5f, -1.3f, 24.5f), o + new Vector3(ServiceDoorX + 0.5f, -1.3f, 27.2f));
        WalkLink(g, "StairRoute", null,
                 o + new Vector3(-53.6f, -1.3f, 28.1f), o + new Vector3(-52.5f, -1.3f, 29.3f), o + new Vector3(-52.5f, -3.3f, 32.8f), o + new Vector3(-52.5f, -3.3f, 33.3f),
                 o + new Vector3(-56f, -5.3f, 33.3f), o + new Vector3(-56.5f, -5.3f, 33.3f), o + new Vector3(-56.5f, -7.3f, 29.8f), o + new Vector3(-56.5f, -7.3f, 29.3f),
                 o + new Vector3(-53f, -9.3f, 29.3f), o + new Vector3(-54.5f, -9.3f, 31.6f));
    }

    // A 1 m square steel landing plate with its top at 'top' (x0, z0 = its corner with the smaller x and z).
    static void CornerLanding(Transform parent, string name, float x0, float z0, float top, Material steel)
    {
        Span(name, parent, new Vector3(x0, top - 0.05f, z0), new Vector3(x0 + 1f, top, z0 + 1f), steel);
    }

    // One straight flight: nine steel steps, 1 m wide, from 'start' (the middle of its top edge, at the landing's
    // height) along 'along' for 3 m, dropping 0.2 m each; 'inner' points from the flight towards the open well,
    // where it gets a stringer under the steps and a railing.
    static void Flight(Transform parent, string name, Vector3 start, Vector3 along, Vector3 inner, Material steel, Material rail)
    {
        const int steps = 9;
        const float run = 3f, drop = 2f, width = 1f, plate = 0.05f;
        float tread = run / steps;
        Vector3 across = new Vector3(Mathf.Abs(inner.x), 0f, Mathf.Abs(inner.z));
        Vector3 lengthwise = new Vector3(Mathf.Abs(along.x), 0f, Mathf.Abs(along.z));
        for (int k = 0; k < steps; k++)
        {
            float top = start.y - drop / (steps + 1) * (k + 1);
            Vector3 c = new Vector3(start.x, 0f, start.z) + along * (tread * (k + 0.5f));
            Vector3 half = across * (width * 0.5f) + lengthwise * (tread * 0.5f);
            Span(name + "_Step" + k, parent, new Vector3(c.x - half.x, top - plate, c.z - half.z), new Vector3(c.x + half.x, top, c.z + half.z), steel);
        }
        // Stringer: a steel side plate under the step ends on the well side.
        Vector3 a = start + inner * (width * 0.5f), b = a + along * run + Vector3.down * drop;
        var stringer = Prim(PrimitiveType.Cube, name + "_Stringer", parent, (a + b) * 0.5f + Vector3.down * 0.15f, new Vector3(0.04f, 0.25f, Vector3.Distance(a, b)), steel);
        stringer.transform.rotation = Quaternion.LookRotation(b - a);
        Object.DestroyImmediate(stringer.GetComponent<Collider>());
        RailRun(parent, name + "_Rail", a, b, rail, endPost: name == "Flight_S");   // the bottom flight closes the railing
    }

    // A railing from a to b (points on the walking surface): posts at the start and the middle (the next run's
    // start post closes this one; 'endPost' adds one for the last run), a handrail at 0.95 m and a middle rail at
    // 0.5 m running between the posts. Solid, so nobody falls into the well.
    static void RailRun(Transform parent, string name, Vector3 a, Vector3 b, Material rail, bool endPost = false)
    {
        foreach (float t in endPost ? new[] { 0f, 0.5f, 1f } : new[] { 0f, 0.5f })
        {
            Vector3 foot = Vector3.Lerp(a, b, t);
            Prim(PrimitiveType.Cube, name + "_Post", parent, foot + Vector3.up * 0.5f, new Vector3(0.05f, 1f, 0.05f), rail);
        }
        foreach (float h in new[] { 0.95f, 0.5f })
        {
            var bar = Prim(PrimitiveType.Cube, name + "_Bar", parent, (a + b) * 0.5f + Vector3.up * h, new Vector3(0.04f, 0.04f, Vector3.Distance(a, b) - 0.05f), rail);
            bar.transform.rotation = Quaternion.LookRotation(b - a);
        }
    }

    // ---------------------------------------------------------------- shells: corridor, hall, storage, camp
    static void BuildServiceRooms(Transform parent)
    {
        var g = new GameObject("ServiceRooms").transform;
        g.SetParent(parent);
        // Corridor from the stairs north to the hall.
        Span("CorridorFloor", g, new Vector3(-55.5f, -9.5f, 34.1f), new Vector3(-52.5f, SvcFloor, 39.7f), svcFloor);
        Span("CorridorCeiling", g, new Vector3(-55.5f, -6.3f, 34.1f), new Vector3(-52.5f, -6.1f, 39.7f), svcCeiling);
        Span("CorridorWall_W", g, new Vector3(-55.8f, -9.5f, 34.1f), new Vector3(-55.5f, -6.1f, 39.7f), svcWall);
        Span("CorridorWall_E", g, new Vector3(-52.5f, -9.5f, 34.1f), new Vector3(-52.2f, -6.1f, 39.7f), svcWall);

        // Machinery hall: x -68..-44, z 40..54, ceiling 4.8 m.
        Span("HallFloor", g, new Vector3(-68f, -9.5f, 40f), new Vector3(-44f, SvcFloor, 54f), svcFloor);
        Span("HallCeiling", g, new Vector3(-68f, -4.5f, 40f), new Vector3(-44f, -4.3f, 54f), svcCeiling);
        Span("HallWall_S_W", g, new Vector3(-68.3f, -9.5f, 39.7f), new Vector3(-55.5f, -4.3f, 40f), svcWall);
        Span("HallWall_S_E", g, new Vector3(-52.5f, -9.5f, 39.7f), new Vector3(-43.7f, -4.3f, 40f), svcWall);
        Span("HallWall_S_Lintel", g, new Vector3(-55.5f, -6.3f, 39.7f), new Vector3(-52.5f, -4.3f, 40f), svcWall);
        Span("HallSill_S", g, new Vector3(-55.5f, -9.5f, 39.7f), new Vector3(-52.5f, SvcFloor, 40f), svcFloor);
        Span("HallWall_N", g, new Vector3(-68.3f, -9.5f, 54f), new Vector3(-43.7f, -4.3f, 54.3f), svcWall);
        Span("HallWall_W_S", g, new Vector3(-68.3f, -9.5f, 40f), new Vector3(-68f, -4.3f, 44f), svcWall);
        Span("HallWall_W_N", g, new Vector3(-68.3f, -9.5f, 46.5f), new Vector3(-68f, -4.3f, 54f), svcWall);
        Span("HallWall_W_Lintel", g, new Vector3(-68.3f, -7f, 44f), new Vector3(-68f, -4.3f, 46.5f), svcWall);
        Span("HallSill_W", g, new Vector3(-68.3f, -9.5f, 44f), new Vector3(-68f, SvcFloor, 46.5f), svcFloor);
        Span("HallWall_E_S", g, new Vector3(-44f, -9.5f, 40f), new Vector3(-43.7f, -4.3f, 45f), svcWall);
        Span("HallWall_E_N", g, new Vector3(-44f, -9.5f, 47.5f), new Vector3(-43.7f, -4.3f, 54f), svcWall);
        Span("HallWall_E_Lintel", g, new Vector3(-44f, -7f, 45f), new Vector3(-43.7f, -4.3f, 47.5f), svcWall);
        Span("HallSill_E", g, new Vector3(-44f, -9.5f, 45f), new Vector3(-43.7f, SvcFloor, 47.5f), svcFloor);

        // Storage room, west: x -82..-68.3, z 42..52, ceiling 3 m.
        Span("StorageFloor", g, new Vector3(-82f, -9.5f, 42f), new Vector3(-68.3f, SvcFloor, 52f), svcFloor);
        Span("StorageCeiling", g, new Vector3(-82f, -6.3f, 42f), new Vector3(-68.3f, -6.1f, 52f), svcCeiling);
        Span("StorageWall_S", g, new Vector3(-82.3f, -9.5f, 41.7f), new Vector3(-68.3f, -6.1f, 42f), svcBrick);
        Span("StorageWall_N", g, new Vector3(-82.3f, -9.5f, 52f), new Vector3(-68.3f, -6.1f, 52.3f), svcBrick);
        Span("StorageWall_W", g, new Vector3(-82.3f, -9.5f, 42f), new Vector3(-82f, -6.1f, 52f), svcBrick);

        // Squatter camp, east: x -43.7..-32, z 42..52, ceiling 3 m.
        Span("CampFloor", g, new Vector3(-43.7f, -9.5f, 42f), new Vector3(-32f, SvcFloor, 52f), svcFloor);
        Span("CampCeiling", g, new Vector3(-43.7f, -6.3f, 42f), new Vector3(-32f, -6.1f, 52f), svcCeiling);
        Span("CampWall_S", g, new Vector3(-43.7f, -9.5f, 41.7f), new Vector3(-31.7f, -6.1f, 42f), svcBrick);
        Span("CampWall_N", g, new Vector3(-43.7f, -9.5f, 52f), new Vector3(-31.7f, -6.1f, 52.3f), svcBrick);
        Span("CampWall_E", g, new Vector3(-32f, -9.5f, 42f), new Vector3(-31.7f, -6.1f, 52f), svcBrick);
    }

    // A model scaled to a height, turned and stood on a floor point (optionally given colliders).
    static GameObject Kit(string fbx, string piece, string tex, Transform parent, Vector3 floorPoint, float yaw, float height, bool solid = false)
    {
        var go = piece == null ? Model(ArtDir + "/" + fbx, null, parent, tex == null ? null : Tex2(tex), 1f, yaw)
                               : ModelPiece(ArtDir + "/" + fbx, piece, parent, tex == null ? null : Tex2(tex), 1f);
        if (piece != null) go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;
        go.transform.localScale *= height / Mathf.Max(0.001f, Bounds(go).size.y);
        StandOn(go, floorPoint);
        if (solid) AddMeshColliders(go);
        return go;
    }

    // ---------------------------------------------------------------- machinery hall
    static void BuildMachineryHall(Transform parent)
    {
        var g = new GameObject("MachineryHall").transform;
        g.SetParent(parent);
        float f = SvcFloor;
        Kit("Props/Plumbing/Generator.fbx", null, "Props/Plumbing/Generator.png", g, new Vector3(-62f, f, 51.6f), 0f, 1.7f, true);
        Kit("Props/Plumbing/Turbine.fbx", null, "Props/Plumbing/Turbine.png", g, new Vector3(-50f, f, 51f), 0f, 1.8f, true);
        Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, new Vector3(-67.2f, f, 49f), 90f, 1.7f, true);
        Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, new Vector3(-67.2f, f, 51.5f), 90f, 1.7f, true);
        foreach (float x in new[] { -61f, -58.5f, -48f })
            Kit("Props/Plumbing/Pump.fbx", null, "Props/Plumbing/Pump.png", g, new Vector3(x, f, 41f), 0f, 0.6f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-46f, f, 41.2f), 20f, 0.9f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-45.3f, f, 42.1f), 70f, 0.9f, true);
        var crate = Place("Assets/Art/Models/Props/Crates/Crate.fbx", g, Vector3.zero, 15f, 0.8f);
        StandOn(crate, new Vector3(-64f, f, 43f));
        MakePickup(ModelPiece(SurvivalFbx, "Battery", g, Tex2("Props/Survival/Battery.png"), 1.5f), global::Pickup.Kind.Battery,
                   new Vector3(-64f, Bounds(crate).max.y, 43f), 30f, "PlasticBox1", lieDown: true);
        var wide = Place("Assets/Art/Models/Props/Crates/WideCrate.fbx", g, Vector3.zero, -10f, 0.8f);
        StandOn(wide, new Vector3(-55f, f, 49f));

        // The machines are still running: a deep hum from the generator and turbine, a buzz from the transformers.
        MachineLoop(g, "GeneratorHum", new Vector3(-62f, f + 1f, 51.6f), "MachineHum", 0.75f, 24f, 1f);
        MachineLoop(g, "TurbineHum", new Vector3(-50f, f + 1f, 51f), "MachineHum", 0.6f, 22f, 0.85f);
        MachineLoop(g, "TransformerBuzz", new Vector3(-67.2f, f + 1.2f, 50.2f), "TransformerBuzz", 0.35f, 12f, 1f);

        // Pipe runs high along the north wall.
        for (int i = 0; i < 6; i++)
        {
            var pipe = Prim(PrimitiveType.Cylinder, "WallPipe", g, new Vector3(-66f + i * 4f, -5.4f, 53.6f), new Vector3(0.35f, 2f, 0.35f), svcMetal);
            pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(pipe.GetComponent<Collider>());
        }

        // The pipe you can knock on: a fat vertical pipe in the north-east corner.
        var knockPipe = Prim(PrimitiveType.Cylinder, "KnockPipe", g, new Vector3(-45f, (f - 4.5f) * 0.5f, 53.2f), new Vector3(0.5f, (-4.5f - f) * 0.5f, 0.5f), svcMetal);
        var here = knockPipe.AddComponent<AudioSource>();
        here.playOnAwake = false; here.spatialBlend = 1f; here.minDistance = 2f; here.maxDistance = 20f;
        var farObject = new GameObject("PipeEcho");
        farObject.transform.SetParent(g);
        farObject.transform.position = new Vector3(-72f, -13f, 62f);   // somewhere deep in the pipework
        var far = farObject.AddComponent<AudioSource>();
        far.playOnAwake = false; far.spatialBlend = 1f; far.rolloffMode = AudioRolloffMode.Linear; far.minDistance = 10f; far.maxDistance = 60f;
        farObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 1400f;
        farObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.SewerPipe;
        var knocking = knockPipe.AddComponent<KnockingPipe>();
        knocking.here = here; knocking.far = far;
        knocking.knock = Load<AudioClip>(SfxDir + "/Hammer2.wav");

        // A battery work light on a tripod, the only light in the hall, aimed at the generator.
        var tripod = new GameObject("WorkLightTripod").transform;
        tripod.SetParent(g);
        Vector3 foot = new Vector3(-58f, f, 47f);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var leg = Prim(PrimitiveType.Cylinder, "Leg", tripod, foot + new Vector3(Mathf.Cos(a) * 0.3f, 0.8f, Mathf.Sin(a) * 0.3f), new Vector3(0.03f, 0.82f, 0.03f), svcMetal);
            leg.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(-Mathf.Cos(a) * 0.3f, 1.6f, -Mathf.Sin(a) * 0.3f));
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }
        var head = Prim(PrimitiveType.Cube, "LampHead", tripod, foot + new Vector3(0f, 1.75f, 0f), new Vector3(0.35f, 0.25f, 0.15f), Mat("WorkLightYellow", null, new Color(0.75f, 0.6f, 0.15f), 1f, 0.4f));
        Object.DestroyImmediate(head.GetComponent<Collider>());
        var spot = new GameObject("WorkLight").AddComponent<Light>();
        spot.transform.SetParent(tripod);
        spot.transform.position = foot + new Vector3(0f, 1.75f, 0f);
        spot.transform.LookAt(new Vector3(-62f, f + 0.8f, 51.6f));
        head.transform.rotation = spot.transform.rotation;
        spot.type = LightType.Spot; spot.color = new Color(1f, 0.93f, 0.8f); spot.intensity = 35f; spot.range = 18f; spot.spotAngle = 80f;
        spot.shadows = LightShadows.Soft;
    }

    // A looping machine sound at a point, fading out by 'reach' metres.
    static void MachineLoop(Transform parent, string name, Vector3 at, string clip, float volume, float reach, float pitch)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = at;
        var source = go.AddComponent<AudioSource>();
        source.clip = Load<AudioClip>(SfxDir + "/Scares/" + clip + ".wav");
        source.loop = true; source.playOnAwake = true; source.volume = volume; source.pitch = pitch;
        source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2.5f; source.maxDistance = reach;
    }

    // ---------------------------------------------------------------- storage room
    static void BuildStorage(Transform parent)
    {
        var g = new GameObject("Storage").transform;
        g.SetParent(parent);
        float f = SvcFloor;
        foreach (float x in new[] { -80.5f, -78f, -75.5f, -73f })
        {
            Kit("Props/DerelictFurniture/Shelf.fbx", null, "Props/DerelictFurniture/shelf256x256.png", g, new Vector3(x, f, 51.78f), 270f, 1.6f, true);
            Kit("Props/DerelictFurniture/Shelf.fbx", null, "Props/DerelictFurniture/shelf256x256.png", g, new Vector3(x, f, 42.22f), 90f, 1.6f, true);
        }
        var rng = new System.Random(7);
        for (int i = 0; i < 5; i++)
        {
            var c = Place("Assets/Art/Models/Props/Crates/Crate.fbx", g, Vector3.zero, (float)rng.NextDouble() * 90f, 0.75f);
            StandOn(c, new Vector3(-80f + i * 1.6f, f, 46.5f + (float)rng.NextDouble() * 1.5f));
        }
        Kit("Props/Trash/TrashAndDebris.fbx", "CardboardBox_1", "Props/Trash/CardboardPaper.png", g, new Vector3(-71.5f, f, 49.5f), 30f, 0.5f, true);
        Kit("Props/Trash/TrashAndDebris.fbx", "CardboardBox_2", "Props/Trash/CardboardPaper.png", g, new Vector3(-70.8f, f, 44.5f), -15f, 0.6f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-81.2f, f, 48.5f), 0f, 0.9f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-81.3f, f, 45.5f), 140f, 0.9f, true);
        MakePickup(ModelPiece(SurvivalFbx, "Battery", g, Tex2("Props/Survival/Battery.png"), 1.5f), global::Pickup.Kind.Battery,
                   new Vector3(-76.5f, f, 43.3f), 60f, "PlasticBox1", lieDown: true);
        PlaceFlare(g, new Vector3(-80.4f, f, 47f), 35f);
    }

    // ---------------------------------------------------------------- squatter camp
    static void BuildSquatterCamp(Transform parent)
    {
        var g = new GameObject("SquatterCamp").transform;
        g.SetParent(parent);
        float f = SvcFloor;
        var mattressA = Kit("Props/DerelictFurniture/Mattress.fbx", null, "Props/DerelictFurniture/mattress256x256.png", g, new Vector3(-35.5f, f, 50.6f), 0f, 0.15f, true);
        Kit("Props/DerelictFurniture/Mattress.fbx", null, "Props/DerelictFurniture/mattress256x256.png", g, new Vector3(-33.6f, f, 44.5f), 80f, 0.15f, true);
        Kit("Props/DerelictFurniture/Couch.fbx", null, "Props/DerelictFurniture/couch256x256.png", g, new Vector3(-40.5f, f, 51.5f), 270f, 0.85f, true);
        Kit("Props/DerelictFurniture/Fridge.fbx", null, "Props/DerelictFurniture/fridge256x256.png", g, new Vector3(-32.5f, f, 48.6f), -90f, 1.9f, true);
        var chair = Kit("Props/DerelictFurniture/Chair.fbx", null, "Props/DerelictFurniture/chair128x128.png", g, new Vector3(-38.6f, f, 46.2f), 200f, 1f, true);   // facing the crate table

        // A crate used as a table: a candle, the missing fuse and a note.
        var table = Place("Assets/Art/Models/Props/Crates/Crate.fbx", g, Vector3.zero, 10f, 0.7f);
        StandOn(table, new Vector3(-37.2f, f, 47.8f));
        float tableTop = Bounds(table).max.y;
        Candle(g, new Vector3(-37.4f, tableTop, 47.6f), true);
        MakePickup(ModelPiece(BreakerFbx, "Fuse", g, Tex2("Props/Plumbing/FuseBox.png"), 1.6f), global::Pickup.Kind.Fuse,
                   new Vector3(-37f, tableTop, 48f), 40f, "SmallGlassBottles1");
        Note(g, "CampNote_Fuse", new Vector3(-36.35f, f + 0.008f, 48.55f), 25f,   // on the floor beside the crate
             "Took the fuse so the maintenance guys can't turn the lights back on.\nIf the power comes back they'll clear us out.\nWe've got nowhere else to go.");
        Note(g, "CampNote_Ray", Bounds(mattressA).center + Vector3.up * (Bounds(mattressA).extents.y + 0.003f), -15f,
             "Day 12 down here.\nSo cold the candles barely burn.\nRay went up the stairs to look on Tuesday.\nHe never came back.");

        PlacePainkillers(g, new Vector3(-37f, f, 50.4f), 30f);

        // Candles burning on the floor by the mattress, and one by the couch that won't stay put.
        Candle(g, new Vector3(-34.2f, f, 50.1f), true);
        Candle(g, new Vector3(-34.6f, f, 49.8f), false);
        var wandering = Candle(g, new Vector3(-41.8f, f, 50.2f), true);
        var moves = wandering.AddComponent<LookAwayChange>();
        moves.newPosition = new Vector3(-43.1f, f, 46.3f);   // right inside the doorway you'll come back through
        moves.faceTowards = moves.newPosition + Vector3.forward;
        moves.frontYaw = 0f;
        moves.seenDistance = 9f; moves.awayDistance = 12f;

        // The chair turns to face the doorway once you've left.
        var turns = chair.AddComponent<LookAwayChange>();
        turns.newPosition = chair.transform.position;
        turns.faceTowards = new Vector3(-44f, f, 46.25f);
        turns.frontYaw = 110f;                                // the model faces west at yaw 0, so at 200 it faces 110 degrees
        turns.seenDistance = 9f; turns.awayDistance = 12f;

        // A radio hissing static on the floor.
        var radio = Prim(PrimitiveType.Cube, "Radio", g, new Vector3(-33.6f, f + 0.12f, 51.4f), new Vector3(0.42f, 0.24f, 0.14f), Mat("RadioBrown", null, new Color(0.3f, 0.2f, 0.14f), 1f, 0.3f));
        radio.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
        var grille = Prim(PrimitiveType.Quad, "Grille", radio.transform, Vector3.zero, Vector3.one, Mat("RadioGrille", null, new Color(0.08f, 0.08f, 0.08f), 1f, 0.2f));
        grille.transform.localPosition = new Vector3(-0.15f, 0f, -0.51f); grille.transform.localScale = new Vector3(0.5f, 0.7f, 1f); grille.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(grille.GetComponent<Collider>());
        var hiss = radio.AddComponent<AudioSource>();
        hiss.clip = Load<AudioClip>(SfxDir + "/Scares/RadioStatic.wav");
        hiss.loop = true; hiss.playOnAwake = true; hiss.volume = 0.45f; hiss.spatialBlend = 1f;
        hiss.rolloffMode = AudioRolloffMode.Linear; hiss.minDistance = 1.5f; hiss.maxDistance = 14f;

        // Rubbish: bags, cans, flattened cardboard for bedding (no chip bags: those are only on Canal St's benches).
        Kit("Props/Trash/TrashBagA.fbx", null, "Props/Trash/TrashBagBlack.png", g, new Vector3(-32.8f, f, 42.8f), 10f, 0.5f);
        Kit("Props/Trash/TrashBagA.fbx", null, "Props/Trash/TrashBagBlack.png", g, new Vector3(-33.4f, f, 42.6f), 70f, 0.45f);
        Kit("Props/Trash/TrashAndDebris.fbx", "Cardboard_Cutout_1", "Props/Trash/CardboardPaper.png", g, new Vector3(-38.5f, f + 0.015f, 50.6f), 90f, 0.02f);   // lifted off the floor so they don't flicker
        string[] litter = { "Can_1_Crushed", "Can_2", "Can_3_Crushed", "Can_5_Crushed", "Can_7" };
        string[] litterTex = { "CansRusty", "Cans", "Cans", "Cans", "Cans" };
        var rng = new System.Random(3);
        for (int i = 0; i < litter.Length; i++)
            Kit("Props/Trash/TrashAndDebris.fbx", litter[i], "Props/Trash/" + litterTex[i] + ".png", g,
                new Vector3(-41f + (float)rng.NextDouble() * 8f, f, 43f + (float)rng.NextDouble() * 7f), (float)rng.NextDouble() * 360f, i % 3 == 0 ? 0.06f : 0.1f);

        // Words scrawled on the wall, in black paint.
        Spray(g, "Tag_Scrawl", 1.6f, new Vector3(-33f, -7.6f, 46.5f), Vector3.right);
        var scrawl = new GameObject("CampScrawl");
        scrawl.transform.SetParent(g);
        scrawl.transform.position = new Vector3(-32.05f, -7.6f, 46.5f);
        scrawl.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1f, 1.6f);
        scrawl.AddComponent<ReadableNote>().text = "Scrawled in black paint:\nDONT LET THEM TURN THE LIGHTS ON";
    }

    // A candle (lit or burnt out) standing on 'point'; lit ones carry a small wavering flame light.
    static GameObject Candle(Transform parent, Vector3 point, bool lit)
    {
        var candle = Kit(lit ? "Props/OldStuff/Candle.fbx" : "Props/OldStuff/UsedCandle.fbx", null,
                         lit ? "Props/OldStuff/Candle.png" : "Props/OldStuff/UsedCandle.png", parent, point, 0f, lit ? 0.14f : 0.09f);
        if (!lit) return candle;
        var flame = new GameObject("Flame").AddComponent<Light>();
        flame.transform.SetParent(candle.transform, true);
        flame.transform.position = point + Vector3.up * 0.2f;
        flame.type = LightType.Point; flame.color = new Color(1f, 0.6f, 0.3f); flame.intensity = 1.3f; flame.range = 3.2f;
        flame.shadows = LightShadows.None;
        flame.gameObject.AddComponent<CandleFlicker>();
        return candle;
    }

    // A sheet of paper lying flat, readable when looked at.
    static void Note(Transform parent, string name, Vector3 point, float yaw, string text)
    {
        var note = Prim(PrimitiveType.Quad, name, parent, point, new Vector3(0.21f, 0.28f, 1f), Mat("NotePaper", null, new Color(0.9f, 0.88f, 0.8f), 1f, 0.1f));
        note.transform.rotation = Quaternion.Euler(90f, yaw, 0f);   // lying face up
        Object.DestroyImmediate(note.GetComponent<Collider>());
        note.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
        note.AddComponent<ReadableNote>().text = text;
    }

    // ---------------------------------------------------------------- sounds with no source
    static void BuildServiceScares(Transform parent)
    {
        var g = new GameObject("ServiceScares").transform;
        g.SetParent(parent);
        var steps = new[] { "FootstepsConcrete1", "FootstepsConcrete2", "FootstepsConcrete3", "FootstepsConcrete4" }.Select(n => Load<AudioClip>(SfxDir + "/" + n + ".wav")).ToArray();

        // Entering the hall: slow footsteps cross the floor above.
        var overhead = Ghost(g, "Overhead", new Vector3(-62f, -4.1f, 46f), 900f);
        var hall = ScareSpot(g, "Scare_FootstepsAbove", new Vector3(-54f, -7.8f, 39f), new Vector3(3f, 3f, 1.2f), ScareTrigger.PowerRule.Any);
        var walk = hall.AddComponent<SoundSequence>();
        walk.source = overhead; walk.clips = steps; walk.count = 7; walk.delay = 2f; walk.gap = 0.85f;
        walk.pitch = new Vector2(0.75f, 0.85f); walk.volume = 1f; walk.drift = new Vector3(12f, 0f, 0f);

        // Stepping into the storage room: a cough from the (empty) camp across the hall.
        var cough = Ghost(g, "CampCough", new Vector3(-36f, -8f, 47f), 4000f);
        var store = ScareSpot(g, "Scare_Cough", new Vector3(-69f, -7.8f, 45.25f), new Vector3(1.4f, 3f, 2.5f), ScareTrigger.PowerRule.Any);
        var coughing = store.AddComponent<SoundSequence>();
        coughing.source = cough; coughing.clips = new[] { Load<AudioClip>(SfxDir + "/HumanCough1.wav") }; coughing.delay = 1.2f; coughing.volume = 0.8f;
        coughing.pitch = new Vector2(0.9f, 0.95f);

        // Halfway down the stairs: a whisper from the landing above, behind you.
        var whisper = Ghost(g, "StairWhisper", new Vector3(-54.5f, -2.3f, 29.6f), 3000f);
        var stairs = ScareSpot(g, "Scare_Whisper", new Vector3(-54.5f, -5.3f, 31.3f), new Vector3(5f, 1f, 5f), ScareTrigger.PowerRule.Any);
        var whispering = stairs.AddComponent<SoundSequence>();
        whispering.source = whisper; whispering.clips = new[] { Load<AudioClip>(SfxDir + "/Scares/ShadowWhisper.wav") }; whispering.delay = 0.3f; whispering.volume = 0.9f;
    }

    // An invisible 3D sound source, muffled through 'cutoff' (Hz) as if behind a wall or floor.
    static AudioSource Ghost(Transform parent, string name, Vector3 position, float cutoff)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 3f; source.maxDistance = 30f;
        go.AddComponent<AudioLowPassFilter>().cutoffFrequency = cutoff;
        go.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.StoneCorridor;
        return go.GetComponent<AudioSource>();
    }
}
