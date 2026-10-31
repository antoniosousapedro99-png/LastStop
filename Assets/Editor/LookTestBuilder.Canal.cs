using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Station 2: Canal St Junction, a big interchange gone dead. Built in the same scene as Hollis St but far
// away (beyond z 300), reached by the train ride (StationTransfer).
// - Platform level (y 0): an 80 m platform hall with an island platform between two tracks, columns,
//   benches. Every tunnel runs on 40 m further than it used to: the arrival train stalls at the far end of
//   the north track's west tunnel, 80 m out; staff steps at the west end lead down to both track beds.
//   A rusted maintenance door in the south track's west tunnel leads down to the lower level
//   (LookTestBuilder.CanalLower.cs): service corridors, a machinery hall, and the emergency latch.
//   Fuel cans: one in the cab of the train parked in the north-east tunnel (CanalTrain.cs), one in a cage in the lower level's machinery hall.
// - Stairs 1 climb from the island's east end to the mezzanine (y 6): ticket gates, a ticket booth
//   (the office key lies on its back counter), ticket machines, the generator room (north) and the locked
//   station master's office (south: the spark plug and a save PC).
// - The backup generator (GeneratorPuzzle) brings Canal St's own power back: lights come on in a wave and the
//   emergency latch on the lower level opens: the way on (the end card for now).
//   The street shutter at the top of stairs 2 stays jammed.
public static partial class LookTestBuilder
{
    const float CanalZ = 300f;              // the station's north wall; everything here is offset from Hollis St
    static PowerGrid canalGrid;

    static void BuildCanal()
    {
        var canal = Group("CanalSt");
        var wallTile = Mat("WallTile", Tex("tile_tile.jpg"), Color.white, 1.6f, 0.35f);
        var blueTile = Mat("WallTileBlue", Tex("tile_tile.jpg"), new Color(0.13f, 0.22f, 0.45f), 1.6f, 0.35f);
        var floorTile = Mat("FloorTile", Tex("Tiles.png"), Color.white, 1.2f, 0.25f);
        var tactile = Mat("TactileStrip", Tex("TactilePaving.png"), Color.white, 0.6f, 0.2f);
        var concrete = Mat("Concrete", Tex("ConcreteBare.png"), new Color(0.6f, 0.6f, 0.58f), 2f, 0.1f);
        var tunnelConcrete = Mat("TunnelConcrete", Tex("ConcreteBare.png"), new Color(0.55f, 0.53f, 0.5f), 2f, 0.1f);
        var ceiling = Mat("Ceiling", Tex("ConcreteBare.png"), new Color(0.4f, 0.4f, 0.38f), 2.5f, 0.05f);
        var gravel = PitGravel();
        var trackBed = Mat("TrackBed", Tex("road_rails.jpg"), new Color(0.8f, 0.78f, 0.74f), 2.6f, 0.1f);
        var housing = Mat("LampHousing", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.3f, 0.5f);
        var tubeLit = EmissiveMat("TubeLit", Fluorescent * 2.2f);

        // ---- Canal St's own power, off until the generator runs
        var power = new GameObject("CanalPowerGrid");
        power.transform.SetParent(canal);
        canalGrid = power.AddComponent<PowerGrid>();
        canalGrid.ownsUnassignedLamps = false;
        canalGrid.clunk = Load<AudioClip>(SfxDir + "/Hammer2.wav");
        canalGrid.musicSting = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Powerbox.ogg");
        var hum = power.AddComponent<AudioSource>();
        hum.clip = Load<AudioClip>(AmbienceDir + "/DSGNDron_Noise-Empty Engine Room_HY_SD.wav");
        hum.loop = true; hum.playOnAwake = false; hum.spatialBlend = 0f;
        canalGrid.hum = hum;

        CanalPlatformLevel(canal, wallTile, blueTile, floorTile, tactile, concrete, tunnelConcrete, ceiling, gravel, trackBed, housing, tubeLit);
        CanalStairs(canal, wallTile, concrete, ceiling, floorTile, housing, tubeLit);
        CanalMezzanine(canal, wallTile, blueTile, floorTile, concrete, ceiling, housing, tubeLit);
        CanalGeneratorRoom(canal, concrete, ceiling, housing, tubeLit);
        CanalOffice(canal, wallTile, floorTile, ceiling);
        CanalArrivalTrain(canal);
        CanalDressing(canal);     // rubbish, cones, posters, blood (LookTestBuilder.CanalDressing.cs)
        CanalLowerLevel(canal);   // the service level under the south tunnel (LookTestBuilder.CanalLower.cs)
        CanalScares(canal);       // one-time scares (LookTestBuilder.CanalScares.cs)
        CanalParkedTrain(canal);  // three intact cars in the north-east tunnel, the fuel can in the cab (CanalTrain.cs)
        CanalSupplies(canal);     // flares, batteries and painkillers, two of each (LookTestBuilder.CanalSupplies.cs)
        BakeCanal(canal);
    }

    // A lamp on Canal St's power.
    static void CanalLamp(Transform parent, string name, Vector3 pos, Material tube, Material housing, bool flicker = false)
    {
        var lamp = Lamp(name, parent, pos, 90f, tube, housing, flicker);
        lamp.GetComponent<PoweredLight>().grid = canalGrid;
    }

    // A dim red emergency light that works without power: a caged lamp with red glass on the nearest wall,
    // and a point light just in front of it.
    static void EmergencyLight(Transform parent, Vector3 pos, float intensity = 2.5f, float range = 7f)
    {
        var lamp = WallLamp(parent, "EmergencyLamp", pos, Emergency * 2.5f, out Vector3 outward);
        var light = AddLight(lamp, LightType.Point, Emergency, intensity, range);
        light.transform.position = Bounds(lamp).center + outward * 0.5f;
    }

    // ---------------------------------------------------------------- lower level: platform hall and tunnels
    static void CanalPlatformLevel(Transform canal, Material wallTile, Material blueTile, Material floorTile, Material tactile, Material concrete,
                                   Material tunnelConcrete, Material ceiling, Material gravel, Material trackBed, Material housing, Material tubeLit)
    {
        var g = new GameObject("PlatformLevel").transform;
        g.SetParent(canal);
        float z = CanalZ;
        // Track pits (north: a long west tunnel where the train stalled; south: a short dead end).
        Span("PitN", g, new Vector3(-140f, -1.5f, z), new Vector3(145f, -1.3f, z + 5.5f), gravel);   // runs on 50 m further east, where the parked train stands
        Span("PitS", g, new Vector3(-100f, -1.5f, z + 15.5f), new Vector3(95f, -1.3f, z + 21f), gravel);
        CanalTrack(g, "TrackN", -140f, 145f, z + 1.6f);
        CanalTrack(g, "TrackS", -100f, 95f, z + 17.2f);
        // Island platform between them.
        Span("IslandBase", g, new Vector3(-40f, -1.5f, z + 5.5f), new Vector3(40f, -0.05f, z + 15.5f), concrete);
        Span("IslandTop", g, new Vector3(-40f, -0.05f, z + 5.5f), new Vector3(40f, 0f, z + 15.5f), floorTile);
        Span("EdgeStripN", g, new Vector3(-40f, 0f, z + 5.6f), new Vector3(40f, 0.012f, z + 6.2f), tactile, false);
        Span("EdgeStripS", g, new Vector3(-40f, 0f, z + 14.8f), new Vector3(40f, 0.012f, z + 15.4f), tactile, false);

        // Hall: x -40..40, ceiling 5 m; tiled walls with a blue band.
        Span("HallWallN", g, new Vector3(-40.3f, -1.5f, z - 0.3f), new Vector3(40.3f, 5.2f, z), wallTile);
        Span("HallWallS", g, new Vector3(-40.3f, -1.5f, z + 21f), new Vector3(40.3f, 5.2f, z + 21.3f), wallTile);
        Span("BandN", g, new Vector3(-40f, 2.19f, z), new Vector3(40f, 2.41f, z + 0.02f), blueTile, false);
        Span("BandS", g, new Vector3(-40f, 2.19f, z + 20.98f), new Vector3(40f, 2.41f, z + 21f), blueTile, false);
        Span("HallCeiling", g, new Vector3(-40f, 5f, z), new Vector3(40f, 5.2f, z + 21f), ceiling);
        Span("WestEnd", g, new Vector3(-40.3f, -1.5f, z + 5.5f), new Vector3(-40f, 5.2f, z + 15.5f), wallTile);
        Span("WestLintelN", g, new Vector3(-40.3f, 3.2f, z), new Vector3(-40f, 5.2f, z + 5.5f), concrete);
        Span("WestLintelS", g, new Vector3(-40.3f, 3.2f, z + 15.5f), new Vector3(-40f, 5.2f, z + 21f), concrete);
        Span("EastEndA", g, new Vector3(40f, -1.5f, z + 5.5f), new Vector3(40.3f, 5.2f, z + 8f), wallTile);
        Span("EastEndB", g, new Vector3(40f, -1.5f, z + 12f), new Vector3(40.3f, 5.2f, z + 15.5f), wallTile);
        Span("EastLintelN", g, new Vector3(40f, 3.2f, z), new Vector3(40.3f, 5.2f, z + 5.5f), concrete);
        Span("EastLintelS", g, new Vector3(40f, 3.2f, z + 15.5f), new Vector3(40.3f, 5.2f, z + 21f), concrete);
        Span("EastLintelStairs", g, new Vector3(40f, 5f, z + 8f), new Vector3(40.3f, 9.4f, z + 12f), wallTile);
        Span("EastHeaderN", g, new Vector3(40f, 5.2f, z + 7.7f), new Vector3(40.3f, 9.4f, z + 8f), wallTile);
        Span("EastHeaderS", g, new Vector3(40f, 5.2f, z + 12f), new Vector3(40.3f, 9.4f, z + 12.3f), wallTile);

        // Tunnels beyond the hall (concrete, ceiling 3 m above the platform).
        CanalTunnel(g, "TunnelNW", -140f, -40.3f, z, z + 5.5f, tunnelConcrete, true, "TrackN", z + 1.6f);
        CanalTunnel(g, "TunnelSW", -100f, -40.3f, z + 15.5f, z + 21f, tunnelConcrete, true, "TrackS", z + 17.2f, CanalServiceDoorX);
        CanalTunnel(g, "TunnelNE", 40.3f, 145f, z, z + 5.5f, tunnelConcrete, false, "TrackN", z + 1.6f);
        CanalTunnel(g, "TunnelSE", 40.3f, 95f, z + 15.5f, z + 21f, tunnelConcrete, false, "TrackS", z + 17.2f);
        // Rusty mesh barriers along both island edges at each end, where trains don't stop (the west ones
        // start past the staff steps). Frankie's jump links skip them (BakeCanal).
        foreach (float ez in new[] { z + 5.75f, z + 15.25f })
        {
            FenceLine(g, "TrackBarrier_W", new Vector3(-36f, 0f, ez), new Vector3(-28f, 0f, ez));
            FenceLine(g, "TrackBarrier_E", new Vector3(32f, 0f, ez), new Vector3(40f, 0f, ez));
        }

        // Staff steps at the west end, down from the island to each track bed.
        for (int k = 0; k < 5; k++)
        {
            float top = -0.217f * (k + 1);
            Span("StaffStepN" + k, g, new Vector3(-40f + 0.65f * k, -1.3f, z + 4.4f), new Vector3(-39.35f + 0.65f * k, top, z + 5.5f), concrete);
            Span("StaffStepS" + k, g, new Vector3(-40f + 0.65f * k, -1.3f, z + 15.5f), new Vector3(-39.35f + 0.65f * k, top, z + 16.6f), concrete);
        }

        // Columns along the island and lamps between them.
        for (int i = 0; i < 8; i++)
        {
            float cx = -35f + i * 10f;
            Span("Column" + i, g, new Vector3(cx - 0.3f, 0f, z + 10.2f), new Vector3(cx + 0.3f, 5f, z + 10.8f), wallTile);
        }
        for (int i = 0; i < 7; i++) CanalLamp(g, "CanalHallLamp_" + i, new Vector3(-30f + i * 10f, 4.98f, z + 10.5f), tubeLit, housing);
        EmergencyLight(g, new Vector3(-39.8f, 3.6f, z + 10.5f));
        EmergencyLight(g, new Vector3(-100f, 2.7f, z + 0.25f), 2f, 6f);  // in the arrival tunnel, red markers to walk towards
        EmergencyLight(g, new Vector3(-60f, 2.7f, z + 0.25f), 2f, 6f);
        EmergencyLight(g, new Vector3(CanalServiceDoorX + 0.5f, 1.4f, z + 20.75f), 1.5f, 5f);   // over the maintenance door

        // Benches, bins, a vending machine, and the notice at the stairs.
        foreach (float bx in new[] { -27f, -7f, 13f })
        {
            Prop("seats", g, new Vector3(bx, 0f, z + 7.2f), 90f);
            Prop("seats", g, new Vector3(bx + 4f, 0f, z + 13.8f), -90f);
        }
        Prop("trash_can", g, new Vector3(-15f, 0f, z + 9.6f), 0f);
        Prop("trash_can", g, new Vector3(25f, 0f, z + 11.4f), 0f);
        BackToWall(Prop("Vending_machine", g, new Vector3(-38.8f, 0f, z + 13.5f), -90f), Vector3.left, -40f);
        Note(g, "CanalNotice", new Vector3(37.5f, 0.01f, z + 9.2f), 80f,
             "NOTICE - Canal St Junction\nStation power failure.\nBackup generator: mezzanine service room.\n- MTA Line 4");
        HidingLockerAt(g, "HidingLocker_CanalWest", new Vector3(-39.68f, 0f, z + 7.5f), 90f);
        // (The fuel cans are in the parked train's cab and on the lower level.)
    }

    // Pushes an object along 'towards' until its back is 1 cm from the wall plane at 'wall' (x or z).
    static void BackToWall(GameObject go, Vector3 towards, float wall)
    {
        var b = Bounds(go);
        if (towards.x != 0f) go.transform.position += Vector3.right * (wall - (towards.x < 0 ? b.min.x - 0.01f : b.max.x + 0.01f));
        else go.transform.position += Vector3.forward * (wall - (towards.z < 0 ? b.min.z - 0.01f : b.max.z + 0.01f));
    }

    // A 2.6 m track bed starting at 'bedZ', its texture lined up with its edge (each bed gets its own copy of
    // the material for that), and steel rails sitting on the painted ones, like Hollis St's.
    static void CanalTrack(Transform g, string name, float x0, float x1, float bedZ)
    {
        const float width = 2.6f;
        var bed = Mat("TrackBed_" + name, Tex("road_rails.jpg"), new Color(0.8f, 0.78f, 0.74f), width, 0.1f);
        tileOrigin[bed] = new Vector3(0f, 0f, bedZ);
        Span(name, g, new Vector3(x0, -1.3f, bedZ), new Vector3(x1, -1.29f, bedZ + width), bed, false);
        var rail = Mat("RailMetal", Tex("metal_02.png"), new Color(0.55f, 0.52f, 0.5f), 1f, 0.6f, 0.8f);
        foreach (float at in new[] { 0.25f, 0.6875f })
        {
            float rz = bedZ + width * at;
            Span(name + "_Rail" + at, g, new Vector3(x0, -1.29f, rz - 0.035f), new Vector3(x1, -1.18f, rz + 0.035f), rail);
        }
    }

    // A tunnel x0..x1 between walls at z0 and z1; 'doorX' (if given) leaves a 1 m doorway in the south wall.
    // Its far end is invisible, with the tunnel and its track ('track', bed at 'bedZ') running on beyond.
    static void CanalTunnel(Transform g, string name, float x0, float x1, float z0, float z1, Material mat, bool capWest, string track, float bedZ, float doorX = float.NaN)
    {
        Span(name + "_WallN", g, new Vector3(x0, -1.5f, z0 - 0.3f), new Vector3(x1, 3.2f, z0), mat);
        if (float.IsNaN(doorX)) Span(name + "_WallS", g, new Vector3(x0, -1.5f, z1), new Vector3(x1, 3.2f, z1 + 0.3f), mat);
        else
        {
            Span(name + "_WallS_W", g, new Vector3(x0, -1.5f, z1), new Vector3(doorX, 3.2f, z1 + 0.3f), mat);
            Span(name + "_WallS_E", g, new Vector3(doorX + 1f, -1.5f, z1), new Vector3(x1, 3.2f, z1 + 0.3f), mat);
            Span(name + "_DoorLintel", g, new Vector3(doorX, 0.9f, z1), new Vector3(doorX + 1f, 3.2f, z1 + 0.3f), mat);
            Span(name + "_DoorThreshold", g, new Vector3(doorX, -1.5f, z1), new Vector3(doorX + 1f, -1.3f, z1 + 0.3f), mat);
        }
        Span(name + "_Ceiling", g, new Vector3(x0, 3f, z0), new Vector3(x1, 3.2f, z1), mat);
        var bed = Mat("TrackBed_" + track, Tex("road_rails.jpg"), new Color(0.8f, 0.78f, 0.74f), 2.6f, 0.1f);   // the same as CanalTrack's
        EndlessTunnel(g, name, capWest ? x0 : x1, capWest ? -1 : 1, z0, z1, 3f, bedZ, bed);
    }

    static void PlaceFuelCan(Transform parent, Vector3 floorPoint, float yaw)
    {
        var can = ModelPiece(ArtDir + "/Props/Mine/Mines.fbx", "Petrol_Can", parent, Tex2("Props/Mine/Petrol_Can.png"), 1f);
        can.transform.localScale *= 0.45f / Mathf.Max(0.01f, Bounds(can).size.y);
        MakePickup(can, global::Pickup.Kind.FuelCan, floorPoint, yaw, "PlasticBox1");
    }

    // ---------------------------------------------------------------- stairs up to the mezzanine and the street exit
    static void CanalStairs(Transform canal, Material wallTile, Material concrete, Material ceiling, Material floorTile, Material housing, Material tubeLit)
    {
        var g = new GameObject("Stairs").transform;
        g.SetParent(canal);
        float z = CanalZ;
        // Stairs 1: island (x 40, y 0) up to the mezzanine (x 49.5, y 6), in a passage z 308..312.
        // (Escalators until 2026-10-31: the director swapped both for plain stairs.)
        Span("Passage1_WallN", g, new Vector3(40.3f, -1.5f, z + 7.7f), new Vector3(49.2f, 9.4f, z + 8f), wallTile);
        Span("Passage1_WallS", g, new Vector3(40.3f, -1.5f, z + 12f), new Vector3(49.2f, 9.4f, z + 12.3f), wallTile);
        Span("Passage1_Ceiling", g, new Vector3(40.3f, 9.2f, z + 8f), new Vector3(49.2f, 9.4f, z + 12f), ceiling);
        StairFlight(g, "Stairs1", new Vector3(40f, 0f, z + 10f), new Vector3(49.5f, 6f, z + 10f), concrete);
        CanalLamp(g, "CanalStairsLamp1", new Vector3(45f, 9.18f, z + 10f), tubeLit, housing);

        // Stairs 2: mezzanine (x 79.8, y 6) up to the street exit (x 89.3, y 12), then the shutter at x 95.
        Span("Passage2_WallN", g, new Vector3(79.8f, 5.8f, z + 7.7f), new Vector3(100f, 15.2f, z + 8f), wallTile);
        Span("Passage2_WallS", g, new Vector3(79.8f, 5.8f, z + 12f), new Vector3(100f, 15.2f, z + 12.3f), wallTile);
        Span("Passage2_Ceiling", g, new Vector3(79.8f, 15f, z + 8f), new Vector3(100f, 15.2f, z + 12f), ceiling);
        Span("ExitLanding", g, new Vector3(89.3f, 11.8f, z + 8f), new Vector3(100f, 12f, z + 12f), floorTile);
        Span("ExitEnd", g, new Vector3(100f, 11.8f, z + 7.7f), new Vector3(100.3f, 15.2f, z + 12.3f), concrete);
        StairFlight(g, "Stairs2", new Vector3(79.8f, 6f, z + 10f), new Vector3(89.3f, 12f, z + 10f), concrete);
        CanalLamp(g, "CanalStairsLamp2", new Vector3(85f, 14.98f, z + 10f), tubeLit, housing);
        CanalLamp(g, "CanalExitLamp", new Vector3(92f, 14.98f, z + 10f), tubeLit, housing);

        // The roller shutter over the street exit: jammed for good (the way on is the emergency latch below).
        var shutter = Span("ExitShutter", g, new Vector3(95f, 12f, z + 8f), new Vector3(95.1f, 15f, z + 12f), Mat("ShutterMetal", Tex("Door_01.jpg"), new Color(0.55f, 0.55f, 0.55f), 1f, 0.3f, 0.5f));
        shutter.AddComponent<JammedShutter>().rattle = Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav");
        EmergencyLight(g, new Vector3(94.6f, 14.6f, z + 10f), 1.5f, 5f);
    }

    // A straight flight of concrete stairs from 'bottom' to 'top' (both on the walking surface, along x), 4 m wide
    // between the passage walls: 20 cm steps as at Hollis St's street stairs, a yellow safety strip along each
    // step's edge, and one steel handrail down the middle (the director's choices, 2026-10-31).
    static void StairFlight(Transform parent, string name, Vector3 bottom, Vector3 top, Material concrete)
    {
        int steps = Mathf.RoundToInt((top.y - bottom.y) / 0.2f);
        float rise = (top.y - bottom.y) / steps, run = (top.x - bottom.x) / steps;
        float z0 = bottom.z - 2f, z1 = bottom.z + 2f;
        var nosing = Mat("StairNosing", null, new Color(0.9f, 0.72f, 0.12f), 1f, 0.3f);
        for (int i = 1; i <= steps; i++)
        {
            float x0 = bottom.x + run * (i - 1), x1 = bottom.x + run * i, y = bottom.y + rise * i;
            Span(name + "_Step" + i, parent, new Vector3(x0, bottom.y, z0), new Vector3(x1, y, z1), concrete);
            Span(name + "_Nosing" + i, parent, new Vector3(x0, y, z0 + 0.05f), new Vector3(x0 + 0.06f, y + 0.005f, z1 - 0.05f), nosing, false);
        }

        // The handrail: posts on every fifth tread, a bar along the slope 0.9 m above the step edges.
        var steel = Mat("StairRailSteel", null, new Color(0.3f, 0.31f, 0.33f), 1f, 0.5f, 0.7f);
        const float railHeight = 0.9f;
        for (int i = 1; i <= steps; i += 5)
        {
            float x = bottom.x + run * (i - 0.5f), foot = bottom.y + rise * i, head = bottom.y + rise * (i - 0.5f) + railHeight;
            Prim(PrimitiveType.Cylinder, name + "_RailPost", parent, new Vector3(x, (foot + head) * 0.5f, bottom.z), new Vector3(0.05f, (head - foot) * 0.5f, 0.05f), steel);
        }
        Vector3 along = top - bottom;
        float length = along.magnitude - run, angle = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;
        var rail = Prim(PrimitiveType.Cube, name + "_Rail", parent, (bottom + top) * 0.5f + Vector3.up * railHeight, new Vector3(length, 0.05f, 0.05f), steel);
        rail.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---------------------------------------------------------------- mezzanine
    static void CanalMezzanine(Transform canal, Material wallTile, Material blueTile, Material floorTile, Material concrete, Material ceiling, Material housing, Material tubeLit)
    {
        var g = new GameObject("Mezzanine").transform;
        g.SetParent(canal);
        float z = CanalZ, f = 6f;
        Span("MezzFloor", g, new Vector3(49.5f, 5.8f, z), new Vector3(79.5f, f, z + 21f), floorTile);
        Span("MezzCeiling", g, new Vector3(49.5f, 10.5f, z), new Vector3(79.5f, 10.7f, z + 21f), ceiling);
        Span("MezzWallW_N", g, new Vector3(49.2f, 5.8f, z), new Vector3(49.5f, 10.7f, z + 8f), wallTile);
        Span("MezzWallW_S", g, new Vector3(49.2f, 5.8f, z + 12f), new Vector3(49.5f, 10.7f, z + 21f), wallTile);
        Span("MezzWallW_Lintel", g, new Vector3(49.2f, 9.4f, z + 8f), new Vector3(49.5f, 10.7f, z + 12f), wallTile);
        Span("MezzWallE_N", g, new Vector3(79.5f, 5.8f, z), new Vector3(79.8f, 10.7f, z + 8f), wallTile);
        Span("MezzWallE_S", g, new Vector3(79.5f, 5.8f, z + 12f), new Vector3(79.8f, 10.7f, z + 21f), wallTile);
        Span("MezzWallE_Lintel", g, new Vector3(79.5f, 9.4f, z + 8f), new Vector3(79.8f, 10.7f, z + 12f), wallTile);
        Span("MezzWallE_Header", g, new Vector3(79.5f, 10.7f, z + 7.7f), new Vector3(79.8f, 15.2f, z + 12.3f), wallTile);   // above the ceiling, up to stairs 2's roof
        // North wall with the generator room door (x 67..68.2), south wall with the office door (x 57..58.2).
        Span("MezzWallN_W", g, new Vector3(49.2f, 5.8f, z - 0.3f), new Vector3(67f, 10.7f, z), wallTile);
        Span("MezzWallN_E", g, new Vector3(68.2f, 5.8f, z - 0.3f), new Vector3(79.8f, 10.7f, z), wallTile);
        Span("MezzWallN_Lintel", g, new Vector3(67f, 8.2f, z - 0.3f), new Vector3(68.2f, 10.7f, z), wallTile);
        Span("MezzWallS_W", g, new Vector3(49.2f, 5.8f, z + 21f), new Vector3(57f, 10.7f, z + 21.3f), wallTile);
        Span("MezzWallS_E", g, new Vector3(58.2f, 5.8f, z + 21f), new Vector3(79.8f, 10.7f, z + 21.3f), wallTile);
        Span("MezzWallS_Lintel", g, new Vector3(57f, 8.2f, z + 21f), new Vector3(58.2f, 10.7f, z + 21.3f), wallTile);
        Span("MezzBandN_W", g, new Vector3(49.5f, 8.69f, z), new Vector3(67f, 8.91f, z + 0.02f), blueTile, false);
        Span("MezzBandN_E", g, new Vector3(68.2f, 8.69f, z), new Vector3(79.5f, 8.91f, z + 0.02f), blueTile, false);
        Span("MezzBandS_W", g, new Vector3(49.5f, 8.69f, z + 20.98f), new Vector3(57f, 8.91f, z + 21f), blueTile, false);
        Span("MezzBandS_E", g, new Vector3(58.2f, 8.69f, z + 20.98f), new Vector3(79.5f, 8.91f, z + 21f), blueTile, false);
        foreach (float lx in new[] { 55f, 65f, 75f })
            foreach (float lz in new[] { 5f, 16f })
                CanalLamp(g, "CanalMezzLamp_" + lx + "_" + lz, new Vector3(lx, 10.48f, z + lz), tubeLit, housing);
        EmergencyLight(g, new Vector3(67.6f, 9.6f, z + 0.25f), 2f, 7f);   // over the generator room door

        // Ticket gate line at x 63: low barriers with two gates and an open aisle (the gates are dead).
        // Rusty mesh fence panels from each wall right up to its gate.
        var gateN = Prop("automatic_ticket_gate", g, new Vector3(63f, f, z + 8.6f), 90f);
        var gateS = Prop("automatic_ticket_gate", g, new Vector3(63f, f, z + 12.4f), 90f);
        FenceLine(g, "GateBarrierN", new Vector3(63f, f, z), new Vector3(63f, f, gateN ? Bounds(gateN).min.z : z + 7.6f));
        FenceLine(g, "GateBarrierS", new Vector3(63f, f, gateS ? Bounds(gateS).max.z : z + 13.4f), new Vector3(63f, f, z + 21f));

        // The ticket booth against the north wall on the platform side; the office key on its back counter.
        var booth = new GameObject("TicketBooth").transform;
        booth.SetParent(g);
        // Worn grey-green painted steel panels (cut from a cabinet door picture: Textures/Generated/BoothPanel.png).
        var boothMat = Mat("BoothPanel", Load<Texture2D>("Assets/Art/Textures/Generated/BoothPanel.png"), new Color(0.72f, 0.8f, 0.72f), 1f, 0.3f, 0.35f);
        Span("BoothFront", booth, new Vector3(55f, f, z + 3.3f), new Vector3(58.2f, f + 1.1f, z + 3.5f), boothMat);
        Span("BoothGlass", booth, new Vector3(55f, f + 1.1f, z + 3.38f), new Vector3(58.2f, f + 2.4f, z + 3.42f), TransparentMat("ShopGlass", new Color(0.35f, 0.45f, 0.5f, 0.22f), 0.95f));
        Span("BoothWallW", booth, new Vector3(55f, f, z), new Vector3(55.2f, f + 2.4f, z + 3.3f), boothMat);
        Span("BoothWallE", booth, new Vector3(58f, f, z), new Vector3(58.2f, f + 2.4f, z + 1.4f), boothMat);   // the gap is its door
        Span("BoothRoof", booth, new Vector3(55f, f + 2.4f, z), new Vector3(58.2f, f + 2.6f, z + 3.5f), boothMat);
        Span("BoothCounter", booth, new Vector3(55.2f, f, z), new Vector3(58f, f + 0.95f, z + 0.6f), boothMat);
        var key = BuildKey(booth);
        MakePickup(key, global::Pickup.Kind.OfficeKey, new Vector3(56.4f, f + 0.95f, z + 0.3f), 25f, "GateWoodChain1");

        // Ticket machines, seats, a vending machine, a bin; a hiding locker by the exit side.
        Prop("ticket_machine", g, new Vector3(52.5f, f, z + 20.4f), 0f);   // screens facing out, into the hall
        Prop("ticket_machine", g, new Vector3(54.3f, f, z + 20.4f), 0f);
        BackToWall(Prop("Vending_machine", g, new Vector3(77.5f, f, z + 20.4f), 0f), Vector3.forward, z + 21f);
        Prop("seats", g, new Vector3(71f, f, z + 20.2f), 180f);
        Prop("trash_can", g, new Vector3(60.5f, f, z + 20.5f), 0f);
        HidingLockerAt(g, "HidingLocker_CanalMezz", new Vector3(74.5f, f, z + 20.68f), 180f);
    }

    // ---------------------------------------------------------------- generator room (off the mezzanine, north)
    static void CanalGeneratorRoom(Transform canal, Material concrete, Material ceiling, Material housing, Material tubeLit)
    {
        var g = new GameObject("GeneratorRoom").transform;
        g.SetParent(canal);
        float z = CanalZ, f = 6f;
        Span("GenFloor", g, new Vector3(64f, 5.8f, z - 7f), new Vector3(72f, f, z - 0.3f), concrete);
        Span("GenCeiling", g, new Vector3(64f, 9.3f, z - 7f), new Vector3(72f, 9.5f, z - 0.3f), ceiling);
        Span("GenWallW", g, new Vector3(63.7f, 5.8f, z - 7.3f), new Vector3(64f, 9.5f, z - 0.3f), concrete);
        Span("GenWallE", g, new Vector3(72f, 5.8f, z - 7.3f), new Vector3(72.3f, 9.5f, z - 0.3f), concrete);
        Span("GenWallN", g, new Vector3(64f, 5.8f, z - 7.3f), new Vector3(72f, 9.5f, z - 7f), concrete);
        Span("GenSill", g, new Vector3(67f, 5.8f, z - 0.3f), new Vector3(68.2f, f, z), concrete);

        var hinge = new GameObject("GeneratorRoomDoor");
        hinge.transform.SetParent(g);
        // The Soviet metro pack's door with a DANGER HIGH VOLTAGE sign, the sign toward the hall. It's hinged on
        // its east side (the sign's side of the model needs it that way round, handle to the west).
        hinge.transform.position = new Vector3(68.2f, f, z - 0.15f);
        var leaf = Model(ArtDir + "/Props/SovietMetro/DangerDoor.fbx", null, hinge.transform, null, 1f, 0f);
        leaf.name = "Door";
        foreach (var r in leaf.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
                mats[m] = mats[m].name.Contains("dangersign") ? Mat("DangerSign", Tex2("Props/SovietMetro/DangerHighVoltage.jpg"), Color.white, 1f, 0.2f)
                        : mats[m].name.Contains("kapimaterial2") ? Mat("DoorHandleGrey", null, new Color(0.57f, 0.57f, 0.57f), 1f, 0.5f, 0.7f)
                        : Mat("DoorDarkBrown", null, new Color(0.37f, 0.15f, 0.1f), 1f, 0.25f, 0.3f);
            r.sharedMaterials = mats;
        }
        var ds = Bounds(leaf).size;   // thin along X, wide along Z at first
        leaf.transform.localScale = Vector3.Scale(leaf.transform.localScale, new Vector3(1f, 2.18f / ds.y, 1.16f / ds.z));
        leaf.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * leaf.transform.rotation;   // across the doorway, sign to the north
        leaf.transform.position += new Vector3(67.6f, f + 1.09f, z - 0.15f) - Bounds(leaf).center;
        FitBoxCollider(leaf);
        var door = hinge.AddComponent<HingedDoor>();
        door.openAngle = -95f;   // swings into the room
        door.doorName = "service room";
        door.openSound = Load<AudioClip>(SfxDir + "/OldDoorOpen.wav");
        door.bangSound = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");
        WalkLink(g, "GeneratorDoorRoute", door, new Vector3(67.6f, f, z + 1.2f), new Vector3(67.6f, f, z - 1.8f));   // Frankie's way in

        // The generator, a flickering work lamp that runs off its battery, and the maintenance log.
        var generator = Kit("Props/Plumbing/Generator.fbx", null, "Props/Plumbing/Generator.png", g, new Vector3(68f, f, z - 4f), 90f, 1.7f, true);
        var puzzle = generator.AddComponent<GeneratorPuzzle>();
        puzzle.grid = canalGrid;
        var engine = generator.AddComponent<AudioSource>();
        engine.clip = Load<AudioClip>("Assets/Art/Audio/SFX/Generator/GeneratorWorkingDroneLoop.ogg");   // the director's running loop
        engine.loop = true; engine.playOnAwake = false; engine.volume = 1f;
        engine.spatialBlend = 1f; engine.rolloffMode = AudioRolloffMode.Linear; engine.minDistance = 3f; engine.maxDistance = 45f;
        puzzle.engine = engine;
        puzzle.pour = Load<AudioClip>(SfxDir + "/WaterSplash1.wav");
        puzzle.fitPlug = Load<AudioClip>(SfxDir + "/SwitchButton3_On.wav");
        puzzle.pull = Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav");
        puzzle.sputter = Load<AudioClip>(SfxDir + "/Rockfall2.wav");
        puzzle.failStart = Load<AudioClip>("Assets/Art/Audio/SFX/Generator/GeneratorFailStart1.ogg");
        CanalLamp(g, "GeneratorRoomLamp_Flicker", new Vector3(68f, 9.28f, z - 3.5f), tubeLit, housing, flicker: true);
        var log = Prim(PrimitiveType.Quad, "GeneratorLog", g, new Vector3(64.02f, f + 1.55f, z - 2.2f), new Vector3(0.21f, 0.28f, 1f), Mat("NotePaper", null, new Color(0.9f, 0.88f, 0.8f), 1f, 0.1f));
        log.transform.rotation = Quaternion.LookRotation(Vector3.left);   // faces into the room
        Object.DestroyImmediate(log.GetComponent<Collider>());
        log.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
        log.AddComponent<ReadableNote>().text = "BACKUP GENERATOR - CANAL ST\nTank: EMPTY. Two cans left: one in the cab of the train parked in the north-east tunnel, one in the stores cage on the lower level (maintenance door, south tunnel).\nSpark plug: shot. Spare is in the station master's office.\nOffice key is on the ticket booth's back counter.\nNB: backup power also releases the emergency latch on the lower level.\n- Line 4 Maintenance";
    }

    // ---------------------------------------------------------------- station master's office (south, locked)
    static void CanalOffice(Transform canal, Material wallTile, Material floorTile, Material ceiling)
    {
        var g = new GameObject("StationOffice").transform;
        g.SetParent(canal);
        float z = CanalZ + 21.3f, f = 6f;
        var plaster = Mat("OfficePlaster", Tex("ConcreteBare.png"), new Color(0.62f, 0.6f, 0.55f), 2f, 0.1f);
        Span("OfficeFloor", g, new Vector3(54f, 5.8f, z), new Vector3(62f, f, z + 6.7f), floorTile);
        Span("OfficeCeiling", g, new Vector3(54f, 9.3f, z), new Vector3(62f, 9.5f, z + 6.7f), ceiling);
        Span("OfficeWallW", g, new Vector3(53.7f, 5.8f, z), new Vector3(54f, 9.5f, z + 7f), plaster);
        Span("OfficeWallE", g, new Vector3(62f, 5.8f, z), new Vector3(62.3f, 9.5f, z + 7f), plaster);
        Span("OfficeWallS", g, new Vector3(54f, 5.8f, z + 6.7f), new Vector3(62f, 9.5f, z + 7f), plaster);
        Span("OfficeSill", g, new Vector3(57f, 5.8f, z - 0.3f), new Vector3(58.2f, f, z), floorTile);

        var hinge = new GameObject("StationOfficeDoor");
        hinge.transform.SetParent(g);
        hinge.transform.position = new Vector3(57f, f, z - 0.15f);
        Box("Door", hinge.transform, new Vector3(57.6f, f + 1.09f, z - 0.15f), new Vector3(1.16f, 2.18f, 0.05f), Mat("DoorMetal", Tex("Door_01.jpg"), new Color(0.7f, 0.7f, 0.7f), 1f, 0.3f, 0.3f));
        var door = hinge.AddComponent<HingedDoor>();
        door.openAngle = -95f;   // swings into the office
        door.doorName = "station master's office";
        door.needsOfficeKey = true;
        door.openSound = Load<AudioClip>(SfxDir + "/OldDoorOpen.wav");
        door.bangSound = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");
        WalkLink(g, "OfficeDoorRoute", door, new Vector3(57.6f, f, z - 1.2f), new Vector3(57.6f, f, z + 1.6f));   // Frankie's way in, once unlocked

        // Desk with a PC (the save point) and the spare spark plug; a chair; a battery lantern for light.
        var desk = Model(ArtDir + "/Props/Office/desk1_weathered.fbx", null, g, Tex2("Props/Office/desk_1_old.jpg"), 1f, 0f);
        StandOn(desk, new Vector3(58f, f, z + 6.1f));
        AddMeshColliders(desk);
        float top = Bounds(desk).max.y;
        var pcTex = Tex2("Props/Office/computer.jpg");
        var pc = new GameObject("CanalPC");
        pc.transform.SetParent(g);
        var pcCase = Model(ArtDir + "/Props/Office/horizontal_desktop.fbx", null, pc.transform, pcTex, 0.75f, PcYaw);
        StandOn(pcCase, new Vector3(57.4f, top, z + 6.2f));
        var monitor = Model(ArtDir + "/Props/Office/computer_monitor.fbx", null, pc.transform, pcTex, 0.75f, PcYaw);
        StandOn(monitor, new Vector3(57.4f, Bounds(pcCase).max.y, z + 6.22f));
        var keyboard = Model(ArtDir + "/Props/Office/computer_keyboard.fbx", null, pc.transform, pcTex, 0.75f, PcYaw);
        StandOn(keyboard, new Vector3(57.4f, top, z + 5.7f));
        AddMeshColliders(pc);
        var terminal = pc.AddComponent<Logbook>();
        terminal.pageSound = Load<AudioClip>(SfxDir + "/SwitchButton2_On.wav");
        terminal.terminalName = "CANAL ST STATION TERMINAL";
        MakePickup(SparkPlugModel(g), global::Pickup.Kind.SparkPlug, new Vector3(58.9f, top, z + 5.8f), 70f, "SmallGlassBottles1");
        var chair = Model(ArtDir + "/Props/Office/office_chair_black.fbx", null, g, Tex2("Props/Office/office_chair_black.jpg"), 1f, 200f);
        StandOn(chair, new Vector3(58.1f, f, z + 5f));
        AddMeshColliders(chair);
        // A candle burning on the desk (left by the station master?) lights the room a little.
        var candle = Candle(g, new Vector3(58.45f, top, z + 6.35f), true);
        // (Fuel can 1 used to lie here; since 2026-10-28 it's in the parked train's cab, LookTestBuilder.CanalTrain.cs.)
        var flame = candle.GetComponentInChildren<Light>();
        flame.intensity = 1.6f; flame.range = 4.5f;
    }

    // A spark plug made of simple shapes: a white ceramic body, a steel hex nut and electrode.
    static GameObject SparkPlugModel(Transform parent)
    {
        var plug = new GameObject("SparkPlug");
        plug.transform.SetParent(parent);
        var ceramic = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ceramic.transform.SetParent(plug.transform, false);
        ceramic.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        ceramic.transform.localScale = new Vector3(0.016f, 0.03f, 0.016f);
        ceramic.transform.localPosition = new Vector3(-0.025f, 0f, 0f);
        ceramic.GetComponent<Renderer>().sharedMaterial = Mat("PlugCeramic", null, new Color(0.9f, 0.88f, 0.84f), 1f, 0.6f);
        var nut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nut.transform.SetParent(plug.transform, false);
        nut.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        nut.transform.localScale = new Vector3(0.024f, 0.008f, 0.024f);
        nut.transform.localPosition = new Vector3(0.01f, 0f, 0f);
        var metal = Mat("KeySteel", null, new Color(0.62f, 0.64f, 0.66f), 1f, 0.7f, 0.9f);
        nut.GetComponent<Renderer>().sharedMaterial = metal;
        var thread = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        thread.transform.SetParent(plug.transform, false);
        thread.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        thread.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);
        thread.transform.localPosition = new Vector3(0.03f, 0f, 0f);
        thread.GetComponent<Renderer>().sharedMaterial = metal;
        foreach (var col in plug.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        plug.transform.localScale = Vector3.one * 1.6f;
        return plug;
    }

    // ---------------------------------------------------------------- the arrival train (stalled in the north-west tunnel)
    static void CanalArrivalTrain(Transform canal)
    {
        var tunnel = canal.Find("PlatformLevel");
        var car = Place("Assets/Art/Models/Environment/MetroPSX/SubwayCarOpen.fbx", tunnel, Vector3.zero, 0f);
        car.name = "StalledTrain";
        foreach (var r in car.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = PropMat(mats[i].name);
            r.sharedMaterials = mats;
        }
        var b = Bounds(car);
        if (b.size.z > b.size.x) { car.transform.rotation = Quaternion.Euler(0, 90f, 0) * car.transform.rotation; b = Bounds(car); }
        car.transform.localScale *= CarLength / b.size.x;
        b = Bounds(car);
        StandOn(car, new Vector3(-121.2f - b.size.x * 0.5f, -1.3f, CanalZ + 2.75f));  // open end (east) at x -121.2, back end just inside the tunnel's end wall
        b = Bounds(car);
        float faceX = b.max.x;
        Physics.SyncTransforms();
        float floorY = -0.25f;
        if (Physics.Raycast(new Vector3(faceX - 3f, b.min.y + b.size.y * 0.6f, CanalZ + 2.75f), Vector3.down, out var hit, 4f)) floorY = hit.point.y;
        float wallN = b.min.z + 0.13f, wallS = b.max.z - 0.13f;
        // Keep the player in the front part of the car and the tunnel beyond the car out of reach.
        foreach (var w in new[] { wallN - 0.05f, wallS + 0.05f })
        {
            var inner = new GameObject("CarInnerWall").AddComponent<BoxCollider>();
            inner.transform.SetParent(tunnel, true);
            inner.transform.position = new Vector3(faceX - 2.5f, floorY + 1.2f, w);
            inner.size = new Vector3(5f, 2.4f, 0.1f);
        }
        CabPartition(tunnel, car, faceX - 5f, floorY, 1);   // the carriage's end wall, door shut, keeps the player at the front
        foreach (var gap in new[] { (CanalZ, b.min.z), (b.max.z, CanalZ + 5.5f) })
        {
            var side = new GameObject("CarSideBlocker").AddComponent<BoxCollider>();
            side.transform.SetParent(tunnel, true);
            side.transform.position = new Vector3(faceX + 0.1f, 0.85f, (gap.Item1 + gap.Item2) * 0.5f);
            side.size = new Vector3(0.2f, 4.3f, Mathf.Max(0.05f, gap.Item2 - gap.Item1));
        }

        // Where the player wakes up, the dying cab light, the ride's rumble and the roof thud.
        var spawn = new GameObject("CanalSpawn").transform;
        spawn.SetParent(tunnel);
        spawn.SetPositionAndRotation(new Vector3(faceX - 2f, floorY + 0.05f, CanalZ + 2.75f), Quaternion.Euler(0f, 90f, 0f));
        var cab = new GameObject("CarLight").AddComponent<Light>();
        cab.transform.SetParent(tunnel);
        cab.transform.position = new Vector3(faceX - 1.5f, floorY + 2f, CanalZ + 2.75f);
        cab.type = LightType.Point; cab.color = new Color(1f, 0.85f, 0.65f); cab.intensity = 3f; cab.range = 7f; cab.shadows = LightShadows.Soft;
        var ride = canal.gameObject.AddComponent<AudioSource>();
        ride.clip = Load<AudioClip>(SfxDir + "/Scares/TrainRumble.wav");
        ride.loop = true; ride.playOnAwake = false; ride.spatialBlend = 0f; ride.volume = 0.9f; ride.pitch = 0.85f;
        var roof = new GameObject("RoofThud");
        roof.transform.SetParent(tunnel);
        roof.transform.position = new Vector3(faceX - 4f, b.max.y, CanalZ + 2.75f);
        var thud = roof.AddComponent<AudioSource>();
        thud.clip = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");
        thud.playOnAwake = false; thud.spatialBlend = 1f; thud.pitch = 0.5f; thud.volume = 1f;
        thud.rolloffMode = AudioRolloffMode.Linear; thud.minDistance = 4f; thud.maxDistance = 60f;
        roof.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.SewerPipe;

        var arrival = canal.gameObject.AddComponent<CanalArrival>();
        arrival.monster = Object.FindAnyObjectByType<FrankieAI>(FindObjectsInactive.Include);
        arrival.dropPoint = new Vector3(faceX + 2f, -1.3f, CanalZ + 2.75f);
        arrival.dropYaw = 90f;
        arrival.thud = thud;
        // His far-away scream from back down the tunnel, a few seconds before he lands on the roof.
        var far = MonsterAudio(tunnel, "CanalFarScream", Vector3.zero, 200f);
        far.transform.position = new Vector3(-139f, 0.5f, CanalZ + 2.75f);
        far.clip = Load<AudioClip>(CharacterSfx + "/FrankieScreamFar.ogg");
        far.minDistance = 15f; far.rolloffMode = AudioRolloffMode.Logarithmic;
        arrival.farScream = far;
        var transfer = canal.gameObject.AddComponent<StationTransfer>();
        transfer.spawn = spawn;
        transfer.ride = ride;
        transfer.halt = Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav");
        transfer.carLight = cab;
        transfer.arrival = arrival;
    }

    // ---------------------------------------------------------------- Frankie's walkable area at Canal St
    static void BakeCanal(Transform canal)
    {
        var go = new GameObject("CanalNavMesh");
        go.transform.SetParent(canal);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, 2.5f, CanalZ + 30f);
        surface.size = new Vector3(292f, 26f, 80f);       // x -146..146, y -10.5..15.5, z 290..370
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~((1 << PreviewLayer) | (1 << 2));
        surface.BuildNavMesh();
        const string path = "Assets/Scenes/LookTest_CanalNavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);
        // Links so he can jump between the island and the track beds anywhere, not only use the staff steps.
        // Not where the barriers stand at the island's ends (x -36..-28 and 32..40).
        foreach (var (from, to) in new[] { (-40f, -36f), (-28f, 32f) })
        {
            EdgeLinks(go.transform, "IslandEdgeLink", from, to, CanalZ + 5.5f, -1);
            // The south track runs right against the edge, so he lands on the raised bed between its rails.
            EdgeLinks(go.transform, "IslandEdgeLink", from, to, CanalZ + 15.5f, 1, 1.9f, 1.15f);
        }
    }
}
