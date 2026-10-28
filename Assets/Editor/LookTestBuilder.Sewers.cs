using System.Collections.Generic;
using UnityEngine;

// Station 3, the sewers: a FIRST DRAFT (greybox in sewer brick) to build on piece by piece. Reached by
// climbing down Canal St's emergency latch (SewerDescent). Built far from the stations, beyond z 600, 20 m
// below them. Walkable and lit, with still water at its starting levels, but nothing works yet: the valves
// and the pump are placeholders, the water doesn't drop, wading isn't slowed and deep water can't drown you.
// Layout (x across, z forward, from the arrival shaft at the south):
//   arrival chamber -> main tunnel (walkway beside a knee-deep channel) -> pumping-station hub (central sump,
//   the pump, a control room) -> three branches, each ending in a valve room:
//   west (knee-deep), east (waist-deep, a step down), north (deep: a narrow ledge along the wall) ->
//   beyond the north valve room, the outfall tunnel to the river, barred by a grate.
public static partial class LookTestBuilder
{
    static readonly Vector3 SewerOrigin = new Vector3(0f, -20f, 600f);
    public static Vector3 SewerSpawn => SewerOrigin + new Vector3(0f, 0.05f, 2.6f);

    static Material sewerBrick, sewerFloor, sewerMud, sewerWater;

    static void BuildSewers()
    {
        var g = Group("Sewers");
        sewerBrick = Mat("SewerBrick", Tex2(SewerTex + "bricks.jpg"), new Color(0.72f, 0.66f, 0.58f), 2f, 0.15f);
        sewerFloor = Mat("SewerFloor", Tex2(SewerTex + "concrete_dirty_2.jpg"), new Color(0.7f, 0.7f, 0.66f), 3f, 0.2f);
        sewerMud = Mat("SewerMud", Tex2(SewerTex + "soil_mud.jpg"), new Color(0.5f, 0.46f, 0.38f), 2f, 0.3f);
        sewerWater = TransparentMat("SewerWater", new Color(0.04f, 0.06f, 0.045f, 0.82f), 0.95f);
        Vector3 o = SewerOrigin;

        // ---- arrival: a brick chamber with a ladder up the south wall to the latch's hatch (shut overhead)
        SewerRoom(g, "Arrival", -2f, 2f, 0f, 4f, 0f, 3f, (2, -1f, 1f, 2.3f));
        Span("ArrivalHatch", g, o + new Vector3(-0.6f, 2.9f, 0f), o + new Vector3(0.6f, 2.98f, 1.1f), Mat("LatchSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.55f, 0.53f, 0.5f), 1f, 0.35f, 0.6f));
        var rung = Mat("LadderSteel", null, new Color(0.35f, 0.33f, 0.3f), 1f, 0.4f, 0.7f);
        for (int i = 0; i < 8; i++)   // ladder rungs up the south wall
        {
            var r = Prim(PrimitiveType.Cylinder, "LadderRung", g, o + new Vector3(0f, 0.3f + i * 0.33f, 0.12f), new Vector3(0.025f, 0.25f, 0.025f), rung);
            r.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(r.GetComponent<Collider>());
        }
        EmergencyLight(g, o + new Vector3(1.6f, 2.5f, 3.6f), 1.6f, 6f);

        // ---- main tunnel: a walkway on the west, a knee-deep channel on the east
        SewerRoom(g, "MainTunnel", -2f, 2f, 4f, 30f, 0f, 3f, false, (0, -2f, 2f, 3f), (2, -2f, 2f, 3f));   // its floor is the walkway and the channel
        Span("MainWalkway", g, o + new Vector3(-2f, -1.2f, 4f), o + new Vector3(-0.5f, 0f, 30f), sewerFloor);
        Span("MainChannelBed", g, o + new Vector3(-0.5f, -1.2f, 4f), o + new Vector3(2f, -1f, 30f), sewerMud);
        SewerWaterPlane(g, "MainChannelWater", -0.5f, 2f, 4f, 30f, -0.45f);
        CagedBulb(g, "SewerBulb_Main", o + new Vector3(-1.2f, 2.75f, 16f), 1.4f, 6f, flicker: true);

        // ---- the pumping-station hub: 16 x 16 m, 6 m high, a sump in the middle, the pump, a control room
        SewerRoom(g, "Hub", -8f, 8f, 30f, 46f, 0f, 6f, (0, -2f, 2f, 3f), (3, 36f, 38f, 2.4f), (1, 36f, 38f, 2.4f), (2, -1f, 1f, 2.4f));
        Span("HubFloorS", g, o + new Vector3(-8f, -0.2f, 30f), o + new Vector3(8f, 0f, 35f), sewerFloor);
        Span("HubFloorN", g, o + new Vector3(-8f, -0.2f, 41f), o + new Vector3(8f, 0f, 46f), sewerFloor);
        Span("HubFloorW", g, o + new Vector3(-8f, -0.2f, 35f), o + new Vector3(-3f, 0f, 41f), sewerFloor);
        Span("HubFloorE", g, o + new Vector3(3f, -0.2f, 35f), o + new Vector3(8f, 0f, 41f), sewerFloor);
        Span("SumpBed", g, o + new Vector3(-3f, -2.2f, 35f), o + new Vector3(3f, -2f, 41f), sewerMud);
        Span("SumpWallS", g, o + new Vector3(-3f, -2f, 35f), o + new Vector3(3f, -0.2f, 35.2f), sewerBrick);
        Span("SumpWallN", g, o + new Vector3(-3f, -2f, 40.8f), o + new Vector3(3f, -0.2f, 41f), sewerBrick);
        Span("SumpWallW", g, o + new Vector3(-3f, -2f, 35.2f), o + new Vector3(-2.8f, -0.2f, 40.8f), sewerBrick);
        Span("SumpWallE", g, o + new Vector3(2.8f, -2f, 35.2f), o + new Vector3(3f, -0.2f, 40.8f), sewerBrick);
        SewerWaterPlane(g, "SumpWater", -2.8f, 2.8f, 35.2f, 40.8f, -0.7f);
        // The main pump (a placeholder: a big drum with a motor block), its pipe down into the sump.
        var steel = Mat("PumpSteel", Tex2(SewerTex + "Metal.jpg"), new Color(0.5f, 0.52f, 0.5f), 1.5f, 0.35f, 0.6f);
        var drum = Prim(PrimitiveType.Cylinder, "MainPump", g, o + new Vector3(5.2f, 1.25f, 43.2f), new Vector3(2f, 1.25f, 2f), steel);
        Span("PumpMotor", g, o + new Vector3(4.2f, 0f, 40.6f), o + new Vector3(6.2f, 1.4f, 42f), steel);
        PipeRun(g, "PumpSuction", 0.25f, pipeRust, o + new Vector3(4.2f, 1.8f, 43.2f), o + new Vector3(2f, 1.8f, 43.2f), o + new Vector3(2f, 1.8f, 39f), o + new Vector3(2f, -1.5f, 39f));
        PipeRun(g, "PumpRiser", 0.25f, pipeRust, o + new Vector3(5.2f, 2.5f, 43.2f), o + new Vector3(5.2f, 5.9f, 43.2f));
        // The control room in the north-west corner (a wall phone goes here later).
        Span("ControlWallS_W", g, o + new Vector3(-8f, 0f, 41.7f), o + new Vector3(-6.5f, 3f, 42f), sewerBrick);
        Span("ControlWallS_E", g, o + new Vector3(-5.5f, 0f, 41.7f), o + new Vector3(-4f, 3f, 42f), sewerBrick);
        Span("ControlWallS_Lintel", g, o + new Vector3(-6.5f, 2.2f, 41.7f), o + new Vector3(-5.5f, 3f, 42f), sewerBrick);
        Span("ControlWallE", g, o + new Vector3(-4f, 0f, 41.7f), o + new Vector3(-3.7f, 3f, 46f), sewerBrick);
        Span("ControlRoof", g, o + new Vector3(-8f, 3f, 41.7f), o + new Vector3(-3.7f, 3.2f, 46f), sewerBrick);
        Span("ControlDesk", g, o + new Vector3(-7.9f, 0f, 44.6f), o + new Vector3(-5.6f, 0.9f, 45.9f), Mat("ConsoleGrey", null, new Color(0.3f, 0.32f, 0.35f), 1f, 0.35f, 0.3f));
        CagedBulb(g, "SewerBulb_Control", o + new Vector3(-6f, 2.75f, 44f), 1.2f, 5f);
        CagedBulb(g, "SewerBulb_HubW", o + new Vector3(-4f, 3.8f, 34f), 2.2f, 8f, cable: 2f);
        CagedBulb(g, "SewerBulb_HubE", o + new Vector3(5f, 3.8f, 36f), 2f, 7f, flicker: true, cable: 2f);
        Pipe(g, "HubMainW", o + new Vector3(-8f, 5.3f, 44.5f), o + new Vector3(8f, 5.3f, 44.5f), 0.2f, pipeSteel);

        // ---- west branch: knee-deep all the way to the first valve room
        SewerRoom(g, "BranchWest", -28f, -8f, 35.5f, 38.5f, 0f, 3f, (1, 35.5f, 38.5f, 3f), (3, 35.5f, 38.5f, 3f));
        SewerWaterPlane(g, "BranchWestWater", -28f, -8f, 35.5f, 38.5f, 0.4f);
        SewerValveRoom(g, "ValveRoomWest", -32f, -28f, 35f, 39f, 0f, 1, 0.4f);
        CagedBulb(g, "SewerBulb_West", o + new Vector3(-18f, 2.75f, 37f), 1.2f, 5f);

        // ---- east branch: a step down, waist-deep water
        SewerRoom(g, "BranchEast", 8f, 28f, 35.5f, 38.5f, -0.5f, 2.8f, (1, 35.5f, 38.5f, 3.3f), (3, 35.5f, 38.5f, 3.3f));
        Span("BranchEastStep", g, o + new Vector3(8f, -0.5f, 35.5f), o + new Vector3(8.6f, -0.25f, 38.5f), sewerFloor);
        SewerWaterPlane(g, "BranchEastWater", 8f, 28f, 35.5f, 38.5f, 0.4f);
        SewerValveRoom(g, "ValveRoomEast", 28f, 32f, 35f, 39f, -0.5f, 3, 0.4f);
        CagedBulb(g, "SewerBulb_East", o + new Vector3(18f, 2.25f, 37f), 1.2f, 5f, flicker: true);

        // ---- north branch: deep water; only a narrow ledge along the west wall
        SewerRoom(g, "BranchNorth", -1.5f, 1.5f, 46f, 70f, 0f, 3f, false, (0, -1.5f, 1.5f, 3f), (2, -1.5f, 1.5f, 3f));
        Span("NorthLedge", g, o + new Vector3(-1.5f, -1.8f, 46f), o + new Vector3(-0.7f, 0f, 70f), sewerFloor);
        Span("NorthChannelBed", g, o + new Vector3(-0.7f, -2f, 46f), o + new Vector3(1.5f, -1.8f, 70f), sewerMud);
        SewerWaterPlane(g, "BranchNorthWater", -0.7f, 1.5f, 46f, 70f, -0.2f);
        SewerValveRoom(g, "ValveRoomNorth", -2f, 2f, 70f, 74f, 0f, 0, 0f, northDoor: true);
        CagedBulb(g, "SewerBulb_North", o + new Vector3(-1.1f, 2.75f, 58f), 1f, 5f);

        // ---- the outfall tunnel to the river, barred by a grate (the way out once the pump runs)
        SewerRoom(g, "Outfall", -1.5f, 1.5f, 74f, 90f, 0f, 3f, (0, -1.5f, 1.5f, 3f), (2, -1.5f, 1.5f, 3f));
        SewerWaterPlane(g, "OutfallWater", -1.5f, 1.5f, 74f, 90f, 0.15f);
        for (float x = -1.35f; x <= 1.36f; x += 0.3f)
            Prim(PrimitiveType.Cylinder, "GrateBar", g, o + new Vector3(x, 1.5f, 88.5f), new Vector3(0.06f, 1.5f, 0.06f), rung);
        Span("GrateRail", g, o + new Vector3(-1.5f, 2.8f, 88.45f), o + new Vector3(1.5f, 2.95f, 88.55f), rung);
        var night = Prim(PrimitiveType.Quad, "RiverNight", g, o + new Vector3(0f, 1.5f, 89.98f), new Vector3(3f, 3f, 1f), EmissiveMat("RiverNight", new Color(0.12f, 0.16f, 0.24f)));
        night.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        Object.DestroyImmediate(night.GetComponent<Collider>());
        var moon = new GameObject("RiverMoonlight").AddComponent<Light>();
        moon.transform.SetParent(g);
        moon.transform.position = o + new Vector3(0f, 2f, 88f);
        moon.type = LightType.Point; moon.color = new Color(0.55f, 0.65f, 0.9f); moon.intensity = 1.2f; moon.range = 9f;

        // ---- sound: running water in the channels
        var flow = new GameObject("SewerFlow").AddComponent<AudioSource>();
        flow.transform.SetParent(g);
        flow.transform.position = o + new Vector3(0f, 0f, 38f);
        flow.clip = Load<AudioClip>(SfxDir + "/OverflowingWaterPipeLoop.wav");
        flow.loop = true; flow.playOnAwake = true; flow.volume = 0.5f; flow.spatialBlend = 1f;
        flow.rolloffMode = AudioRolloffMode.Linear; flow.minDistance = 4f; flow.maxDistance = 40f;

        // ---- where Sousa lands after climbing down Canal St's latch
        var arrival = new GameObject("SewerArrival").transform;
        arrival.SetParent(g);
        arrival.SetPositionAndRotation(SewerSpawn, Quaternion.identity);   // facing north, up the main tunnel
        var latch = Object.FindAnyObjectByType<EmergencyLatch>();
        if (latch) { latch.sewerArrival = arrival; latch.climb = Load<AudioClip>(SfxDir + "/FootstepsConcrete2.wav"); }
    }

    // A brick room x0..x1, z0..z1 (relative to the sewer origin), floor at 'y0' and ceiling at 'y1', with 0.3 m
    // walls outside its edges. Each gap is (side: 0 south, 1 east, 2 north, 3 west; from; to; top): a doorway
    // in that wall, along x for south/north and along z for east/west, 'top' high above the floor.
    static void SewerRoom(Transform g, string name, float x0, float x1, float z0, float z1, float y0, float y1,
                          params (int side, float from, float to, float top)[] gaps) => SewerRoom(g, name, x0, x1, z0, z1, y0, y1, true, gaps);

    static void SewerRoom(Transform g, string name, float x0, float x1, float z0, float z1, float y0, float y1, bool floor,
                          params (int side, float from, float to, float top)[] gaps)
    {
        Vector3 o = SewerOrigin;
        const float t = 0.3f;
        if (floor) Span(name + "_Floor", g, o + new Vector3(x0, y0 - 0.2f, z0), o + new Vector3(x1, y0, z1), sewerFloor);
        Span(name + "_Ceiling", g, o + new Vector3(x0, y1, z0), o + new Vector3(x1, y1 + 0.2f, z1), sewerBrick);
        for (int side = 0; side < 4; side++)
        {
            bool alongX = side == 0 || side == 2;
            float a0 = alongX ? x0 - (side == 0 ? t : 0f) : z0, a1 = alongX ? x1 + (side == 0 ? t : 0f) : z1;   // south wall covers the corners
            if (alongX) { a0 = x0 - t; a1 = x1 + t; }
            var cuts = new List<(float from, float to, float top)>();
            foreach (var gp in gaps) if (gp.side == side) cuts.Add((gp.from, gp.to, gp.top));
            cuts.Sort((p, q) => p.from.CompareTo(q.from));
            float at = a0; int n = 0;
            foreach (var c in cuts)
            {
                if (c.from > at) WallPiece(g, name + "_Wall" + side + "_" + n++, side, at, c.from, x0, x1, z0, z1, y0 - 2.2f, y1 + 0.2f);
                if (c.top < y1 - y0) WallPiece(g, name + "_Lintel" + side + "_" + n++, side, c.from, c.to, x0, x1, z0, z1, y0 + c.top, y1 + 0.2f);
                at = c.to;
            }
            if (at < a1) WallPiece(g, name + "_Wall" + side + "_" + n, side, at, a1, x0, x1, z0, z1, y0 - 2.2f, y1 + 0.2f);
        }
    }

    // One straight piece of a SewerRoom's wall, from 'a' to 'b' along it, between heights 'lo' and 'hi'.
    static void WallPiece(Transform g, string name, int side, float a, float b, float x0, float x1, float z0, float z1, float lo, float hi)
    {
        Vector3 o = SewerOrigin;
        const float t = 0.3f;
        Vector3 min, max;
        switch (side)
        {
            case 0: min = new Vector3(a, lo, z0 - t); max = new Vector3(b, hi, z0); break;
            case 2: min = new Vector3(a, lo, z1); max = new Vector3(b, hi, z1 + t); break;
            case 1: min = new Vector3(x1, lo, a); max = new Vector3(x1 + t, hi, b); break;
            default: min = new Vector3(x0 - t, lo, a); max = new Vector3(x0, hi, b); break;
        }
        Span(name, g, o + min, o + max, sewerBrick);
    }

    // A small valve room at the end of a branch: a pipe up the far wall with a big valve wheel (a placeholder;
    // turning it comes later). 'doorSide' is the side it's entered from; its water stands at 'waterY'.
    static void SewerValveRoom(Transform g, string name, float x0, float x1, float z0, float z1, float y0, int doorSide, float waterY, bool northDoor = false)
    {
        var gaps = new List<(int, float, float, float)>();
        bool alongX = doorSide == 0 || doorSide == 2;
        float mid = alongX ? (x0 + x1) * 0.5f : (z0 + z1) * 0.5f;
        gaps.Add((doorSide, mid - 1.5f, mid + 1.5f, 3f));
        if (northDoor) gaps.Add((2, -1.5f, 1.5f, 3f));
        SewerRoom(g, name, x0, x1, z0, z1, y0, y0 + 3f, gaps.ToArray());
        if (waterY > y0) SewerWaterPlane(g, name + "_Water", x0, x1, z0, z1, waterY);
        Vector3 o = SewerOrigin;
        int far = northDoor ? 1 : (doorSide + 2) % 4;   // the far wall, or the east wall when the room leads on north
        Vector3 wallPoint = far switch
        {
            0 => new Vector3((x0 + x1) * 0.5f + 1f, y0, z0),
            1 => new Vector3(x1, y0, (z0 + z1) * 0.5f),
            2 => new Vector3((x0 + x1) * 0.5f + 1f, y0, z1),
            _ => new Vector3(x0, y0, (z0 + z1) * 0.5f),
        };
        Vector3 inward = far switch { 0 => Vector3.forward, 1 => Vector3.left, 2 => Vector3.back, _ => Vector3.right };
        Vector3 pipeAt = o + wallPoint + inward * 0.35f;
        Pipe(g, name + "_Pipe", pipeAt, pipeAt + Vector3.up * 3f, 0.2f, pipeRust);
        var wheel = Prim(PrimitiveType.Cylinder, name + "_Wheel", g, pipeAt + Vector3.up * 1.3f + inward * 0.3f, new Vector3(0.7f, 0.03f, 0.7f),
                         Mat("ValveRed", null, new Color(0.5f, 0.12f, 0.08f), 1f, 0.4f, 0.4f));
        wheel.transform.rotation = Quaternion.FromToRotation(Vector3.up, inward);
        var stem = Prim(PrimitiveType.Cylinder, name + "_Stem", g, pipeAt + Vector3.up * 1.3f + inward * 0.15f, new Vector3(0.08f, 0.15f, 0.08f), pipeSteel);
        stem.transform.rotation = wheel.transform.rotation;
        Object.DestroyImmediate(stem.GetComponent<Collider>());
        CagedBulb(g, name + "_Bulb", o + new Vector3((x0 + x1) * 0.5f, y0 + 2.75f, (z0 + z1) * 0.5f), 1.3f, 5f);
    }

    // A still, murky water surface over x0..x1, z0..z1 at height 'y' (relative to the sewer origin).
    static void SewerWaterPlane(Transform g, string name, float x0, float x1, float z0, float z1, float y)
    {
        var water = Prim(PrimitiveType.Quad, name, g, SewerOrigin + new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), new Vector3(x1 - x0, z1 - z0, 1f), sewerWater);
        water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // facing up
        Object.DestroyImmediate(water.GetComponent<Collider>());
    }
}
