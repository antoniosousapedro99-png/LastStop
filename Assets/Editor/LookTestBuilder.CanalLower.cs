using UnityEngine;

// Canal St's lower level, under the south track's west tunnel: bigger and more "backrooms" than Hollis St's.
// - A rusted maintenance door in the tunnel (x -80) and the same square steel stairs as Hollis St, 8 m down
//   (BuildMaintenanceDoor / BuildStairwell, shifted by CanalLowerOffset).
// - A maze of 2 m service corridors with three small rooms off it: a pump room, an electrical room with a
//   caged transformer, and a storeroom (LowerMap, built by GridLevel).
// - The machinery hall (32 x 26 m, 5.5 m high): generators and a turbine, a fenced pump bay, a caged
//   transformer bay, and the stores cage in the far corner with the second fuel can.
// - The latch room off the hall: the emergency latch in the floor (EmergencyLatch), sealed until the
//   generator restores the power; climbing down ends Station 2.
// Pipes along every corridor ceiling and the hall, control boxs, cabinets, dim caged bulbs, three lockers.
// Frankie walks all of it (BakeCanal), following the stairs' walking routes while the door is open.
public static partial class LookTestBuilder
{
    static readonly Vector3 CanalLowerOffset = new Vector3(-25f, 0f, 295.5f);   // Hollis St's stairwell shifted here
    static float CanalServiceDoorX => ServiceDoorX + CanalLowerOffset.x;          // x -80
    static readonly Vector3 LowerOrigin = new Vector3(-100f, SvcFloor, 332f);    // the map's corner (floor height)
    const float LowerCell = 2f;

    // One character per 2 m square; row 0 is the south edge (z 332), column 0 the west (x -100).
    // '#' solid, '.' corridor or small room, 'H' machinery hall, 'd' doorway. Column 10 of row 0 meets the stairs.
    static readonly string[] LowerMap =
    {
        "##########.###################################",  // 0
        "##########.###################################",  // 1
        "##.................###########################",  // 2  corridor A
        "##.###.###.#####.##HHHHHHHHHHHHHHHH###########",  // 3
        "##.#...###.#...#.##HHHHHHHHHHHHHHHH###########",  // 4  pump room (cols 4-6), electrical room (12-14)
        "##.#...d...d...#.##HHHHHHHHHHHHHHHH###########",  // 5
        "##.#####.#######.ddHHHHHHHHHHHHHHHH###########",  // 6  doorway into the hall
        "##.......#.......##HHHHHHHHHHHHHHHH###########",  // 7  corridor B
        "######.###.#####.##HHHHHHHHHHHHHHHHd.......###",  // 8  doorway to the latch room
        "##...#.#.....#...##HHHHHHHHHHHHHHHH#.......###",  // 9
        "##...d.#.###.#.####HHHHHHHHHHHHHHHH#.......###",  // 10 storeroom (cols 2-4)
        "##...#.#.#...#.####HHHHHHHHHHHHHHHH#.......###",  // 11
        "######...#.###.####HHHHHHHHHHHHHHHH###########",  // 12
        "##.......#.....ddddHHHHHHHHHHHHHHHH###########",  // 13 corridor C, a low passage into the hall
        "##.#####.#.###.####HHHHHHHHHHHHHHHH###########",  // 14
        "##.......#.......##HHHHHHHHHHHHHHHH###########",  // 15
        "##############################################",  // 16
    };

    // The centre of a map cell, on the floor.
    static Vector3 Cell(int col, int row) => LowerOrigin + new Vector3((col + 0.5f) * LowerCell, 0f, (row + 0.5f) * LowerCell);

    static void CanalLowerLevel(Transform canal)
    {
        var level = new GameObject("LowerLevel").transform;
        level.SetParent(canal);
        var o = CanalLowerOffset;
        float f = SvcFloor;

        // ---- the door in the south tunnel and the stairs down (shared with Hollis St)
        var door = BuildMaintenanceDoor(level, o);
        door.name = "CanalMaintenanceDoor";
        BuildStairwell(level, o, door);
        // A short corridor from the bottom of the stairs (exit x -80.5..-77.5 at z 329.6) to the map (z 332).
        Span("StairCorridorFloor", level, new Vector3(-80.5f, f - 0.2f, 329.6f), new Vector3(-77.5f, f, 332f), svcFloor);
        Span("StairCorridorCeiling", level, new Vector3(-80.5f, f + 3f, 329.6f), new Vector3(-77.5f, f + 3.2f, 332f), svcCeiling);
        Span("StairCorridorWallW", level, new Vector3(-80.8f, f - 0.2f, 329.6f), new Vector3(-80.5f, f + 3.2f, 332f), svcWall);
        Span("StairCorridorWallE", level, new Vector3(-77.5f, f - 0.2f, 329.6f), new Vector3(-77.2f, f + 3.2f, 332f), svcWall);

        // ---- corridors, rooms and the hall
        GridLevel(level, LowerMap, LowerOrigin, LowerCell, svcWall, svcFloor, svcCeiling);
        LowerCorridorPipes(level);
        LowerRooms(level);
        LowerHall(level);
        LowerLatchRoom(level);
    }

    // A pipe along the ceiling of every straight corridor 3+ cells long (along x a little off-centre, along z
    // a little lower so crossings pass under each other), ending where the corridor does.
    static void LowerCorridorPipes(Transform parent)
    {
        var g = new GameObject("CorridorPipes").transform;
        g.SetParent(parent);
        bool Corridor(char c) => c == '.' || c == 'd';
        int rows = LowerMap.Length, cols = LowerMap[0].Length;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols;)
            {
                if (!Corridor(LowerMap[r][c])) { c++; continue; }
                int e = c;
                while (e + 1 < cols && Corridor(LowerMap[r][e + 1])) e++;
                if (e - c >= 2)
                {
                    float z = LowerOrigin.z + (r + 0.5f) * LowerCell + 0.45f;
                    Pipe(g, "PipeX", new Vector3(LowerOrigin.x + c * LowerCell, SvcFloor + 2.72f, z), new Vector3(LowerOrigin.x + (e + 1) * LowerCell, SvcFloor + 2.72f, z), 0.09f, (r % 2 == 0) ? pipeRust : pipeSteel);
                }
                c = e + 1;
            }
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows;)
            {
                if (!Corridor(LowerMap[r][c])) { r++; continue; }
                int e = r;
                while (e + 1 < rows && Corridor(LowerMap[e + 1][c])) e++;
                if (e - r >= 2)
                {
                    float x = LowerOrigin.x + (c + 0.5f) * LowerCell - 0.45f;
                    float z0 = LowerOrigin.z + r * LowerCell, z1 = LowerOrigin.z + (e + 1) * LowerCell;
                    if (r == 0) z0 = 329.6f;   // the first corridor carries on to the stairs
                    Pipe(g, "PipeZ", new Vector3(x, SvcFloor + 2.5f, z0), new Vector3(x, SvcFloor + 2.5f, z1), 0.07f, pipeGreen);
                }
                r = e + 1;
            }

        // Conduits along a few corridor walls, and dim bulbs here and there (big dark stretches between).
        Conduits(g, new Vector3(-78f, SvcFloor + 2.2f, 338f), new Vector3(-68f, SvcFloor + 2.2f, 338f), Vector3.back);           // corridor A, north wall
        Conduits(g, new Vector3(-94f, SvcFloor + 2.2f, 346f), new Vector3(-84f, SvcFloor + 2.2f, 346f), Vector3.forward);        // corridor B, south wall
        Conduits(g, new Vector3(-96f, SvcFloor + 2.2f, 358f), new Vector3(-88f, SvcFloor + 2.2f, 358f), Vector3.forward);        // corridor C, south wall
        CagedBulb(g, "LowerBulb_Stairs", Cell(10, 1) + Vector3.up * 2.75f, 1.6f, 5f);
        CagedBulb(g, "LowerBulb_A", Cell(16, 2) + Vector3.up * 2.75f, 1.4f, 5f, flicker: true);
        CagedBulb(g, "LowerBulb_B", Cell(5, 7) + Vector3.up * 2.75f, 1.4f, 5f);
        CagedBulb(g, "LowerBulb_C", Cell(12, 13) + Vector3.up * 2.75f, 1.5f, 5f, flicker: true);
        CagedBulb(g, "LowerBulb_D", Cell(3, 15) + Vector3.up * 2.75f, 1.2f, 4.5f);
        CagedBulb(g, "LowerBulb_E", Cell(10, 9) + Vector3.up * 2.75f, 1.3f, 5f);
        CagedBulb(g, "LowerBulb_F", Cell(2, 5) + Vector3.up * 2.75f, 1.2f, 5f, flicker: true);
        CagedBulb(g, "LowerBulb_G", Cell(13, 7) + Vector3.up * 2.75f, 1.3f, 5f);
        CagedBulb(g, "LowerBulb_H", Cell(6, 13) + Vector3.up * 2.75f, 1.3f, 5f);
    }

    // ---------------------------------------------------------------- the three rooms in the maze
    static void LowerRooms(Transform parent)
    {
        var g = new GameObject("Rooms").transform;
        g.SetParent(parent);
        float f = SvcFloor;

        // Pump room (x -92..-86, z 340..344): pumps fed from a pipe along the north wall, a locker.
        foreach (float x in new[] { -89.6f, -87.6f })
        {
            Kit("Props/Plumbing/Pump.fbx", null, "Props/Plumbing/Pump.png", g, new Vector3(x, f, 343f), 0f, 0.6f, true);
            Pipe(g, "PumpDrop", new Vector3(x, f + 2f, 343.78f), new Vector3(x, f + 0.55f, 343.78f), 0.06f, pipeRust);
        }
        Pipe(g, "PumpHeader", new Vector3(-92f, f + 2f, 343.78f), new Vector3(-86f, f + 2f, 343.78f), 0.1f, pipeRust);
        ControlPanel(g, new Vector3(-86f, f + 1.5f, 340.9f), Vector3.left);
        HidingLockerAt(g, "HidingLocker_CanalLowerPumps", new Vector3(-91.68f, f, 341.2f), 90f);

        // Electrical room (x -76..-70, z 340..344): cabinets, control boxs and a caged transformer.
        ElectricCabinet(g, new Vector3(-75.3f, f, 344f), Vector3.back, 1f, 1.9f, 0.45f);
        ElectricCabinet(g, new Vector3(-76f, f, 341.2f), Vector3.right, 1f, 1.9f, 0.45f, vents: true);
        ControlPanel(g, new Vector3(-73.8f, f + 1.5f, 344f), Vector3.back);
        ControlPanel(g, new Vector3(-72.6f, f + 1.5f, 344f), Vector3.back);
        Conduits(g, new Vector3(-76f, f + 2.4f, 344f), new Vector3(-70f, f + 2.4f, 344f), Vector3.back, 4);
        Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, new Vector3(-71f, f, 342.6f), 90f, 1.6f, true);
        FencedCage(g, "TransformerCage_Small", -72.4f, -70f, 341f, 344f, f, 3, 1f, 1, 2);
        MachineLoop(g, "SmallTransformerBuzz", new Vector3(-71f, f + 1.2f, 342.6f), "TransformerBuzz", 0.3f, 10f, 1.1f);

        // Storeroom (x -96..-90, z 350..356): shelves, crates, a locker.
        Kit("Props/DerelictFurniture/Shelf.fbx", null, "Props/DerelictFurniture/shelf256x256.png", g, new Vector3(-93f, f, 355.78f), 270f, 1.6f, true);
        Kit("Props/DerelictFurniture/Shelf.fbx", null, "Props/DerelictFurniture/shelf256x256.png", g, new Vector3(-90.7f, f, 355.78f), 270f, 1.6f, true);
        var crate = Place("Assets/Art/Models/Props/Crates/Crate.fbx", g, Vector3.zero, 20f, 0.75f);
        StandOn(crate, new Vector3(-92.5f, f, 350.9f));
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-90.8f, f, 351f), 40f, 0.9f, true);
        HidingLockerAt(g, "HidingLocker_CanalLowerStore", new Vector3(-95.68f, f, 353.4f), 90f);
    }

    // ---------------------------------------------------------------- machinery hall (x -62..-30, z 338..364)
    static void LowerHall(Transform parent)
    {
        var g = new GameObject("MachineryHall").transform;
        g.SetParent(parent);
        float f = SvcFloor, ceil = f + 5.5f;

        // Machines along the north half (the walkway to the latch room runs east-west through the middle).
        Kit("Props/Plumbing/Generator.fbx", null, "Props/Plumbing/Generator.png", g, new Vector3(-55f, f, 353.5f), 0f, 1.7f, true);
        Kit("Props/Plumbing/Turbine.fbx", null, "Props/Plumbing/Turbine.png", g, new Vector3(-47f, f, 353.5f), 0f, 1.8f, true);
        Kit("Props/Plumbing/Generator.fbx", null, "Props/Plumbing/Generator.png", g, new Vector3(-39f, f, 353.5f), 180f, 1.7f, true);
        MachineLoop(g, "LowerGeneratorHum", new Vector3(-55f, f + 1f, 353.5f), "MachineHum", 0.6f, 22f, 0.9f);
        MachineLoop(g, "LowerTurbineHum", new Vector3(-47f, f + 1f, 353.5f), "MachineHum", 0.55f, 20f, 0.8f);
        foreach (float x in new[] { -55f, -47f, -39f })
            PipeRun(g, "MachineFeed", 0.12f, pipeSteel, new Vector3(x, ceil - 0.5f, 349.5f), new Vector3(x, ceil - 0.5f, 353f), new Vector3(x, f + 1.6f, 353f));

        // Fenced pump bay along the south wall, its pumps fed from the wall pipes.
        foreach (float x in new[] { -58f, -55.5f, -52.5f, -50f })
        {
            Kit("Props/Plumbing/Pump.fbx", null, "Props/Plumbing/Pump.png", g, new Vector3(x, f, 339.2f), 0f, 0.6f, true);
            Pipe(g, "PumpDrop", new Vector3(x, f + 1.7f, 338.25f), new Vector3(x, f + 0.5f, 338.25f), 0.06f, pipeRust);
        }
        FencedCage(g, "PumpBay", -60f, -48f, 338f, 342.4f, f, 2, 1.6f, 0);
        Pipe(g, "SouthWallPipe", new Vector3(-62f, f + 1.7f, 338.25f), new Vector3(-36f, f + 1.7f, 338.25f), 0.09f, pipeRust);
        Pipe(g, "SouthWallPipe2", new Vector3(-62f, f + 2.1f, 338.22f), new Vector3(-36f, f + 2.1f, 338.22f), 0.06f, pipeGreen);

        // Transformer bay against the north wall, caged, with control boxs inside.
        Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, new Vector3(-48f, f, 362.8f), 0f, 1.7f, true);
        Kit("Props/Plumbing/Transformer.fbx", null, "Props/Plumbing/Transformer.png", g, new Vector3(-44.5f, f, 362.8f), 0f, 1.7f, true);
        FencedCage(g, "TransformerBay", -50f, -42.5f, 359.4f, 364f, f, 0, 1.4f, 2);
        MachineLoop(g, "LowerTransformerBuzz", new Vector3(-46.2f, f + 1.2f, 362.8f), "TransformerBuzz", 0.35f, 12f, 0.95f);
        ControlPanel(g, new Vector3(-46.2f, f + 2.4f, 364f), Vector3.back);
        Conduits(g, new Vector3(-62f, f + 3.2f, 364f), new Vector3(-30f, f + 3.2f, 364f), Vector3.back, 4);

        // The stores cage in the south-east corner: the second fuel can, crates, barrels.
        FencedCage(g, "StoresCage", -36f, -30f, 338f, 344f, f, 3, 1.4f, 0, 1);
        PlaceFuelCan(g, new Vector3(-31.4f, f, 339.4f), 20f);
        var crate = Place("Assets/Art/Models/Props/Crates/Crate.fbx", g, Vector3.zero, 10f, 0.8f);
        StandOn(crate, new Vector3(-33.2f, f, 339f));
        var wide = Place("Assets/Art/Models/Props/Crates/WideCrate.fbx", g, Vector3.zero, 90f, 0.8f);
        StandOn(wide, new Vector3(-30.9f, f, 342.6f));
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-34.9f, f, 343.1f), 0f, 0.9f, true);

        // Odd bits along the walkway: barrels, a cabinet row on the west wall between the doorways.
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-44f, f, 343.4f), 30f, 0.9f, true);
        Kit("Props/OldStuff/Barrel.fbx", null, "Props/OldStuff/Barrel.png", g, new Vector3(-43.2f, f, 344.1f), 110f, 0.9f, true);
        ElectricCabinet(g, new Vector3(-62f, f, 349.6f), Vector3.right, 1f, 1.9f, 0.45f);
        ElectricCabinet(g, new Vector3(-62f, f, 351.8f), Vector3.right, 1f, 1.9f, 0.45f, vents: true);
        ControlPanel(g, new Vector3(-62f, f + 1.5f, 354f), Vector3.right);
        ControlPanel(g, new Vector3(-30f, f + 1.5f, 352.4f), Vector3.left);

        // Ceiling: a big main across the hall, a second line beside it, one crossing north-south.
        Pipe(g, "HallMain", new Vector3(-62f, ceil - 0.5f, 349.5f), new Vector3(-30f, ceil - 0.5f, 349.5f), 0.17f, pipeSteel);
        Pipe(g, "HallLine", new Vector3(-62f, ceil - 0.4f, 350.2f), new Vector3(-30f, ceil - 0.4f, 350.2f), 0.08f, pipeGreen);
        Pipe(g, "HallCross", new Vector3(-40f, ceil - 0.8f, 338f), new Vector3(-40f, ceil - 0.8f, 364f), 0.12f, pipeRust);

        // Dim caged bulbs, one flickering by the latch room door, and a locker on the east wall.
        CagedBulb(g, "HallBulb_W", new Vector3(-57f, ceil - 2.2f, 346.5f), 2.4f, 8f, cable: 2.1f);
        CagedBulb(g, "HallBulb_Pumps", new Vector3(-53f, ceil - 2.2f, 340.5f), 1.8f, 6f, cable: 2.1f);
        CagedBulb(g, "HallBulb_N", new Vector3(-46f, ceil - 2.2f, 358f), 2f, 7f, cable: 2.1f);
        CagedBulb(g, "HallBulb_E", new Vector3(-33f, ceil - 2.2f, 348.5f), 2f, 7f, flicker: true, cable: 2.1f);
        HidingLockerAt(g, "HidingLocker_CanalLowerHall", new Vector3(-30.32f, f, 356.5f), -90f);
    }

    // ---------------------------------------------------------------- the latch room (x -28..-14, z 348..356)
    static void LowerLatchRoom(Transform parent)
    {
        var g = new GameObject("LatchRoom").transform;
        g.SetParent(parent);
        float f = SvcFloor;
        Vector3 c = new Vector3(-20f, f, 352f);
        const float hole = 1.2f, curb = 0.2f, rim = 0.2f;
        var steel = Mat("LatchSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.55f, 0.53f, 0.5f), 1f, 0.35f, 0.6f);

        // A raised steel frame round a dark shaft opening.
        float h = hole * 0.5f, o = h + rim;
        Span("LatchFrameS", g, c + new Vector3(-o, 0f, -o), c + new Vector3(o, curb, -h), steel);
        Span("LatchFrameN", g, c + new Vector3(-o, 0f, h), c + new Vector3(o, curb, o), steel);
        Span("LatchFrameW", g, c + new Vector3(-o, 0f, -h), c + new Vector3(-h, curb, h), steel);
        Span("LatchFrameE", g, c + new Vector3(h, 0f, -h), c + new Vector3(o, curb, h), steel);
        var dark = Prim(PrimitiveType.Quad, "LatchShaft", g, c + Vector3.up * 0.01f, new Vector3(hole, hole, 1f), EmissiveMat("ShaftBlack", Color.black));
        dark.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Object.DestroyImmediate(dark.GetComponent<Collider>());

        // The lid on a hinge along the north edge; it swings up and back over the north side.
        var hinge = new GameObject("LatchLid").transform;
        hinge.SetParent(g);
        hinge.position = c + new Vector3(0f, curb, h);
        var lid = Box("Lid", hinge, c + new Vector3(0f, curb + 0.03f, 0f), new Vector3(hole + 0.1f, 0.06f, hole + 0.1f), steel);
        Object.DestroyImmediate(lid.GetComponent<Collider>());
        var wheel = Prim(PrimitiveType.Cylinder, "LidWheel", hinge, c + new Vector3(0f, curb + 0.09f, 0f), new Vector3(0.35f, 0.02f, 0.35f), pipeSteel);
        Object.DestroyImmediate(wheel.GetComponent<Collider>());

        // What the player looks at and uses (it also keeps people from walking onto the hatch).
        var latchObject = new GameObject("EmergencyLatch");
        latchObject.transform.SetParent(g);
        latchObject.transform.position = c + Vector3.up * 0.25f;
        latchObject.AddComponent<BoxCollider>().size = new Vector3(hole + 2f * rim, 0.5f, hole + 2f * rim);
        var latch = latchObject.AddComponent<EmergencyLatch>();
        latch.grid = canalGrid;
        latch.lid = hinge;
        latch.openAngle = 110f;
        latch.unlock = Load<AudioClip>(SfxDir + "/Hammer2.wav");
        latch.creak = Load<AudioClip>(SfxDir + "/OldDoorOpen.wav");

        // A red emergency lamp over it, the stencilled notice, pipes and conduits.
        EmergencyLight(g, new Vector3(-20f, f + 2.85f, 355.8f), 2f, 6f);
        Notice(g, "Notice_Latch", new Vector3(-20f, f + 1.6f, 354f), Vector3.forward,
               "EMERGENCY EXIT - SEWER ACCESS\nLatch is released by BACKUP POWER only.\nIn an emergency, follow the overflow tunnel to the river outfall.\n- Line 4 Maintenance");
        ControlPanel(g, new Vector3(-14f, f + 1.5f, 352f), Vector3.left);
        Conduits(g, new Vector3(-14f, f + 2.2f, 348f), new Vector3(-14f, f + 2.2f, 356f), Vector3.left);
        Pipe(g, "LatchPipe", new Vector3(-28f, f + 2.7f, 349f), new Vector3(-14f, f + 2.7f, 349f), 0.1f, pipeRust);
        Pipe(g, "LatchPipe2", new Vector3(-28f, f + 2.6f, 355.3f), new Vector3(-14f, f + 2.6f, 355.3f), 0.07f, pipeGreen);
        CagedBulb(g, "LatchBulb", new Vector3(-24f, f + 2.75f, 352f), 1.2f, 5f, flicker: true);
    }
}
