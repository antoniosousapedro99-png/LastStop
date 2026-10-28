using UnityEngine;

// Hollis St's service level made more industrial: pipes along the ceilings (rising through the corridor
// into the machinery hall, dropping into the generator and turbine), pipes and cable conduits on the
// walls, control boxs and electrical cabinets, chain-link cages round the transformers and the pump
// bay, a few dim caged bulbs, and three hiding lockers (corridor, hall, camp).
// Rooms (see BuildServiceRooms): corridor x -55.5..-52.5, z 34.1..39.7 (ceiling -6.3); hall x -68..-44,
// z 40..54 (ceiling -4.5); storage x -82..-68.3, z 42..52; camp x -43.7..-32, z 42..52 (ceilings -6.3).
public static partial class LookTestBuilder
{
    static void BuildServiceIndustrial(Transform parent)
    {
        var g = new GameObject("Industrial").transform;
        g.SetParent(parent);
        float f = SvcFloor;

        // ---- corridor: a pipe along the ceiling that rises into the hall, conduits, a control box, a bulb
        // (It comes down out of the corridor ceiling by the stairs, and rises into the hall clear of the wall pipes.)
        PipeRun(g, "CorridorPipe", 0.09f, pipeRust, new Vector3(-54.6f, -6.3f, 34.6f), new Vector3(-54.6f, -6.55f, 34.6f), new Vector3(-54.6f, -6.55f, 40.6f), new Vector3(-54.6f, -4.85f, 40.6f), new Vector3(-54.6f, -4.85f, 42.5f));
        Conduits(g, new Vector3(-55.5f, -6.65f, 34.1f), new Vector3(-55.5f, -6.65f, 39.7f), Vector3.right);
        ControlPanel(g, new Vector3(-52.5f, -7.8f, 35.6f), Vector3.left);
        CagedBulb(g, "CorridorBulb", new Vector3(-53.6f, -6.55f, 37.2f), 1.6f, 5f, flicker: true);
        HidingLockerAt(g, "HidingLocker_SvcCorridor", new Vector3(-55.18f, f, 38.6f), 90f);

        // ---- machinery hall
        // Ceiling: two pipes across the whole hall by the south wall, a big one further in that drops into
        // the generator and the turbine.
        Pipe(g, "HallPipeA", new Vector3(-68f, -4.85f, 42.5f), new Vector3(-44f, -4.85f, 42.5f), 0.12f, pipeRust);
        Pipe(g, "HallPipeB", new Vector3(-68f, -4.8f, 43.1f), new Vector3(-44f, -4.8f, 43.1f), 0.07f, pipeGreen);
        Pipe(g, "HallMain", new Vector3(-68f, -4.9f, 49.4f), new Vector3(-44f, -4.9f, 49.4f), 0.16f, pipeSteel);
        PipeRun(g, "GeneratorFeed", 0.1f, pipeSteel, new Vector3(-62f, -4.9f, 49.4f), new Vector3(-62f, -4.9f, 51.3f), new Vector3(-62f, -7.75f, 51.3f));
        PipeRun(g, "TurbineFeed", 0.1f, pipeSteel, new Vector3(-50f, -4.9f, 49.4f), new Vector3(-50f, -4.9f, 50.8f), new Vector3(-50f, -7.65f, 50.8f));
        // South wall: two pipes the whole width of the wall, stepping up over the doorway (one inside the
        // other), and a feed from the lower one into each pump.
        PipeRun(g, "SouthWallPipe", 0.08f, pipeRust, new Vector3(-68f, -7f, 40.25f), new Vector3(-55.8f, -7f, 40.25f), new Vector3(-55.8f, -5.9f, 40.25f),
                new Vector3(-52.2f, -5.9f, 40.25f), new Vector3(-52.2f, -7f, 40.25f), new Vector3(-44f, -7f, 40.25f));
        PipeRun(g, "SouthWallPipe2", 0.05f, pipeGreen, new Vector3(-68f, -6.6f, 40.22f), new Vector3(-56.1f, -6.6f, 40.22f), new Vector3(-56.1f, -5.6f, 40.22f),
                new Vector3(-51.9f, -5.6f, 40.22f), new Vector3(-51.9f, -6.6f, 40.22f), new Vector3(-44f, -6.6f, 40.22f));
        foreach (float x in new[] { -61f, -58.5f, -48f })
            PumpFeed(g, x, 41f, 40.25f, -7f, f);
        // East wall: risers floor to ceiling.
        foreach (float z in new[] { 41.4f, 42.1f })
            Pipe(g, "EastRiser", new Vector3(-44.22f, f, z), new Vector3(-44.22f, -4.5f, z), 0.08f, pipeGreen);
        // Electrics: cabinets on the south and north walls, control boxs above them.
        ElectricCabinet(g, new Vector3(-51.2f, f, 40f), Vector3.forward, 1f, 1.9f, 0.45f);
        ElectricCabinet(g, new Vector3(-66.4f, f, 40f), Vector3.forward, 1f, 1.7f, 0.5f, vents: true);
        ElectricCabinet(g, new Vector3(-57.2f, f, 54f), Vector3.back, 1f, 1.9f, 0.45f);
        ElectricCabinet(g, new Vector3(-56f, f, 54f), Vector3.back, 1f, 1.9f, 0.45f, vents: true);
        ControlPanel(g, new Vector3(-57.6f, -7.6f, 40f), Vector3.forward);
        ControlPanel(g, new Vector3(-44f, -7.8f, 49.6f), Vector3.left);
        Conduits(g, new Vector3(-58.5f, -7.1f, 54f), new Vector3(-44f, -7.1f, 54f), Vector3.back, 4);
        // Chain-link: a cage round the two transformers (walls on two sides) and a fenced pump bay.
        FencedCage(g, "TransformerCage", -68f, -65.4f, 47.6f, 54f, f, 1, 1.2f, 2, 3);
        FencedCage(g, "PumpBay", -62.6f, -57.2f, 40f, 42.4f, f, 2, 1.2f, 0);
        // Dim caged bulbs (the work light is still the brightest thing here).
        CagedBulb(g, "HallBulbE", new Vector3(-48f, -6.1f, 46.5f), 2.2f, 7f, cable: 1.5f);
        CagedBulb(g, "HallBulbW", new Vector3(-63.5f, -6.1f, 44.5f), 1.8f, 6f, flicker: true, cable: 1.5f);
        HidingLockerAt(g, "HidingLocker_SvcHall", new Vector3(-44.32f, f, 52.2f), -90f);

        // ---- storage: a pipe across the ceiling and a control box by the barrels
        Pipe(g, "StoragePipe", new Vector3(-82f, -6.55f, 47f), new Vector3(-68.3f, -6.55f, 47f), 0.1f, pipeRust);
        Pipe(g, "StoragePipe2", new Vector3(-82f, -6.5f, 47.4f), new Vector3(-68.3f, -6.5f, 47.4f), 0.05f, pipeSteel);
        ControlPanel(g, new Vector3(-82f, -7.8f, 44f), Vector3.right);

        // ---- camp: pipes overhead, a locker by the door
        Pipe(g, "CampPipe", new Vector3(-43.7f, -6.55f, 44f), new Vector3(-32f, -6.55f, 44f), 0.1f, pipeRust);
        Pipe(g, "CampPipe2", new Vector3(-43.7f, -6.5f, 44.35f), new Vector3(-32f, -6.5f, 44.35f), 0.06f, pipeGreen);
        HidingLockerAt(g, "HidingLocker_SvcCamp", new Vector3(-40f, f, 42.32f), 0f);
    }
}
