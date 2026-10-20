using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The street level above the station: one block of a 1990s city street at 3 a.m. in light drizzle,
// during the blackout. The player arrives in the maintenance van at the west end and walks to the
// subway entrance, which leads down a stairwell into the tiled corridor.
// Layout (street level is y = 6; the road runs along x):
//   south sidewalk z -16..-2 (subway entrance at x 0), road z -26..-16, north sidewalk z -30..-26,
//   shops behind each sidewalk. Both ends of the road are blocked (bus + barriers / police).
// Lore (all text is a draft for the creative director to rewrite): missing-worker posters, a
// newspaper, a shop note, graffiti, the van radio and a payphone answering machine. The monster is
// only hinted at (drag marks, a wrapped shape on the stairs), never seen here.
public static partial class LookTestBuilder
{
    const float StreetY = 6f;
    const string StreetDir = "Assets/Art/Models/Environment/Street";
    const uint OutdoorLayer = 1u << 2;       // rendering layer lit by the moon (underground is not)

    static readonly Color Sodium = new Color(1f, 0.58f, 0.25f);
    static Transform street;

    public static Vector3 PlayerStartPosition => new Vector3(-37f, StreetY + 0.05f, -13.5f);
    public const float PlayerStartYaw = 90f;   // facing east, toward the subway entrance

    static void BuildStreet(Material concrete, Material wallTile)
    {
        street = Group("Street");
        var asphalt = Mat("StreetAsphalt", Load<Texture2D>(StreetDir + "/Urban/road2.png"), new Color(0.55f, 0.55f, 0.57f), 6f, 0.65f);
        var pavement = Mat("StreetPavement", Load<Texture2D>(StreetDir + "/Urban/sidewalk.jpg"), new Color(0.75f, 0.74f, 0.72f), 1.5f, 0.45f);
        var lot = Mat("StreetLot", Load<Texture2D>(StreetDir + "/Urban/concrete_bare.jpg"), new Color(0.5f, 0.5f, 0.5f), 3f, 0.4f);
        var stairConcrete = Mat("StairConcrete", Tex("ConcreteBare.png"), new Color(0.6f, 0.6f, 0.58f), 1f, 0.15f);

        BuildStairwell(stairConcrete, wallTile, concrete);

        // ---- ground: sidewalks (top y 6), road 15 cm lower, lots and yards behind the shops
        float y0 = StreetY - 0.4f, y1 = StreetY;
        Span("SidewalkS_W", street, new Vector3(-50, y0, -16), new Vector3(-2.3f, y1, -2), pavement);
        Span("SidewalkS_E", street, new Vector3(2.3f, y0, -16), new Vector3(50, y1, -2), pavement);
        Span("SidewalkS_Front", street, new Vector3(-2.3f, y0, -16), new Vector3(2.3f, y1, -12), pavement);
        Span("SidewalkS_Back", street, new Vector3(-2.3f, y0, -4), new Vector3(2.3f, y1, -2), pavement);
        Span("GroundSouth", street, new Vector3(-50, y0, -2), new Vector3(50, y1, 35), lot);
        Span("Road", street, new Vector3(-50, y0, -26), new Vector3(50, y1 - 0.15f, -16), asphalt);
        Span("SidewalkN", street, new Vector3(-50, y0, -30), new Vector3(50, y1, -26), pavement);
        Span("GroundNorth", street, new Vector3(-50, y0, -85), new Vector3(50, y1, -30), lot);
        // Puddles on the wet road and sidewalk.
        var puddle = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Puddle.mat");
        Puddle("RoadPuddle_1", street, new Vector3(-18, y1 - 0.14f, -18.5f), 3.5f, 1.6f, puddle);
        Puddle("RoadPuddle_2", street, new Vector3(12, y1 - 0.14f, -24f), 4.5f, 2f, puddle);
        Puddle("SidewalkPuddle", street, new Vector3(-6, y1 + 0.01f, -9f), 2.2f, 1.2f, puddle);

        // ---- invisible boundary: the player stays on this block
        Blocker("Bound_W", new Vector3(-48.5f, StreetY, -60), new Vector3(-48f, StreetY + 6, 30));
        Blocker("Bound_E", new Vector3(48f, StreetY, -60), new Vector3(48.5f, StreetY + 6, 30));
        Blocker("Bound_N", new Vector3(-48f, StreetY, -60.5f), new Vector3(48f, StreetY + 6, -60f));
        Blocker("Bound_S", new Vector3(-48f, StreetY, 30f), new Vector3(48f, StreetY + 6, 30.5f));

        BuildShops();
        BuildStreetFurniture();
        BuildCars();
        BuildLore();
        BuildOutdoorLighting();

        // Everything up here is lit by the moon (rendering layer), the station below is not.
        foreach (var r in street.GetComponentsInChildren<Renderer>()) r.renderingLayerMask |= OutdoorLayer;
    }

    // ---------------------------------------------------------------- stairwell
    // From the corridor (z 0, y 0) up 30 steps (20 cm each) to the street (z -10, y 6).
    // The lower third runs under the sidewalk (ceiling above); the rest is open to the sky.
    static void BuildStairwell(Material stair, Material wallTile, Material concrete)
    {
        var well = StreetGroup("Stairwell");
        Span("BottomLanding", well, new Vector3(-2, -0.2f, -1), new Vector3(2, 0, 0), stair);
        Span("Landing_Ceiling", well, new Vector3(-2, 3, -1), new Vector3(2, 3.2f, 0), concrete);
        for (int i = 1; i <= 30; i++)
        {
            float zFar = -1f - 0.3f * i, zNear = zFar + 0.3f, top = 0.2f * i;
            Span("Step_" + i, well, new Vector3(-2, 0, zFar), new Vector3(2, top, zNear), stair);
            if (zNear > -4.01f)   // covered part: ceiling 3 m above each step
                Span("StepCeiling_" + i, well, new Vector3(-2, top + 3f, zFar), new Vector3(2, top + 3.2f, zNear), concrete);
        }
        Span("TopLanding", well, new Vector3(-2, StreetY - 0.2f, -12), new Vector3(2, StreetY, -10), stair);
        foreach (int side in new[] { -1, 1 })
        {
            string s = side < 0 ? "W" : "E";
            // Side walls: full height up to the sidewalk where the stairs are open, stopping under the
            // sidewalk slab where it covers them (so the two never overlap).
            Span("Wall_" + s + "_Open", well, Mirror(new Vector3(2, -0.2f, -12), side), Mirror(new Vector3(2.3f, StreetY, -4), side), wallTile);
            Span("Wall_" + s + "_Covered", well, Mirror(new Vector3(2, -0.2f, -4), side), Mirror(new Vector3(2.3f, StreetY - 0.4f, 0), side), wallTile);
        }
        Span("Wall_Front", well, new Vector3(-2.3f, -0.2f, -12.3f), new Vector3(2.3f, StreetY - 0.4f, -12), wallTile);

        // Railings around the opening, globe lamps and the SUBWAY sign at the top of the stairs.
        var rail = StreetDir + "/Urban/metalfence_one_side_topbar.fbx";
        foreach (int side in new[] { -1, 1 })
            for (int k = 0; k < 3; k++)
                Place(rail, well, new Vector3(2.15f * side, StreetY, -5.4f - k * 2.7f), 90f);
        Place(rail, well, new Vector3(-1.1f, StreetY, -4.05f), 0f, 0.8f);
        Place(rail, well, new Vector3(1.1f, StreetY, -4.05f), 0f, 0.8f);
        foreach (int side in new[] { -1, 1 })
        {
            var post = Prim(PrimitiveType.Cylinder, "GlobePost", well, new Vector3(2.6f * side, StreetY + 1.4f, -12.4f), new Vector3(0.12f, 1.4f, 0.12f), Mat("GlobePost", null, new Color(0.1f, 0.18f, 0.12f), 1f, 0.5f, 0.6f));
            var globe = Prim(PrimitiveType.Sphere, "Globe", well, new Vector3(2.6f * side, StreetY + 3.0f, -12.4f), Vector3.one * 0.45f, EmissiveMat("SubwayGlobe", new Color(0.35f, 1f, 0.45f) * 1.6f));
            var gl = AddLight(globe, LightType.Point, new Color(0.45f, 1f, 0.5f), 2.5f, 6f);
            gl.transform.localPosition = Vector3.zero;
        }
        var sign = Prim(PrimitiveType.Cube, "SubwaySign", well, new Vector3(0, StreetY + 2.6f, -12.4f), new Vector3(4.6f, 0.55f, 0.12f), Mat("SignBoard", null, new Color(0.08f, 0.1f, 0.09f), 1f, 0.3f));
        Label(sign.transform, "SUBWAY   HOLLIS ST   LINE 4", new Vector3(0, 0, -0.07f), 0.09f, Color.white, 180f);   // faces the street (north)
    }

    // ---------------------------------------------------------------- shops and buildings
    static void BuildShops()
    {
        var shops = StreetGroup("Shops");
        // North side, facing the road (south). Yaw turns each model so its shopfront faces the street.
        Place(StreetDir + "/Laundry/Laundry.fbx", shops, new Vector3(-38f, StreetY, -40f), LaundryYaw);
        Place(StreetDir + "/VhsShop/VhsShop.fbx", shops, new Vector3(-20f, StreetY, -39f), VhsYaw);
        Place(StreetDir + "/SixTwelve/SixTwelve.fbx", shops, new Vector3(2f, StreetY, -43f), SixTwelveYaw);
        Place(StreetDir + "/GasStation/GasStation.fbx", shops, new Vector3(30f, StreetY, -52f), GasYaw);
        // South side, behind the entrance sidewalk.
        Place(StreetDir + "/Pizzeria/Pizzeria.fbx", shops, new Vector3(24f, StreetY, 9f), PizzeriaYaw);
        string blds = StreetDir + "/Buildings/Buildings.fbx";
        PlacePiece(blds, "Building", shops, new Vector3(-36f, StreetY, 9f), 180f);
        PlacePiece(blds, "Building_08", shops, new Vector3(-14f, StreetY, 6f), 180f);
        PlacePiece(blds, "Building_09", shops, new Vector3(5f, StreetY, 6f), 180f);
        PlacePiece(blds, "Building_07", shops, new Vector3(42f, StreetY, 8f), 180f);
        // Skyline beyond the north shops.
        PlacePiece(blds, "Building_02", shops, new Vector3(-30f, StreetY, -75f), 0f);
        PlacePiece(blds, "Building_01", shops, new Vector3(8f, StreetY, -78f), 0f);
    }

    // Shopfront directions (degrees); tuned by looking at the built street.
    static float LaundryYaw = 90f, VhsYaw = 180f, SixTwelveYaw = 0f, GasYaw = 0f, PizzeriaYaw = -90f;

    // ---------------------------------------------------------------- street furniture
    static void BuildStreetFurniture()
    {
        var f = StreetGroup("Furniture");
        string u = StreetDir + "/Urban/";
        // Street lamps along both curbs (most are dead in the blackout; see BuildOutdoorLighting).
        for (int i = 0; i < 6; i++)
        {
            float x = -40f + i * 16f;
            Place(u + "street_light.fbx", f, new Vector3(x, StreetY, -15.3f), 90f).name = "StreetLight_S_" + i;
            Place(u + "street_light_clean.fbx", f, new Vector3(x + 8f, StreetY, -26.7f), -90f).name = "StreetLight_N_" + i;
        }
        // Trees and hedges.
        string t = StreetDir + "/Trees/";
        string[] trees = { "tree_rt_2", "dead_tree_rt_1", "tree_rt_4", "tree_rt_3", "dead_tree_rt_2", "tree_rt_2_1" };
        for (int i = 0; i < 6; i++)
            Place(t + trees[i] + ".fbx", f, new Vector3(-44f + i * 17f, StreetY, -3.5f), i * 47f, 1.5f);
        for (int i = 0; i < 5; i++)
            Place(t + trees[(i + 2) % 6] + ".fbx", f, new Vector3(-36f + i * 17f, StreetY, -29f), i * 31f, 1.4f);
        string bush = StreetDir + "/Bushes/bush_connector_straight_autumn.fbx";
        for (int i = 0; i < 6; i++) Place(bush, f, new Vector3(16f + i * 2f, StreetY, -31f), 0f);
        // Bus stop by the entrance, with the newspaper (see BuildLore).
        var stop = StreetDir + "/BusStop/BusStop.fbx";
        PlacePiece(stop, "Bus_stop", f, new Vector3(-10f, StreetY, -14.6f), 180f);
        PlacePiece(stop, "Bus_stop_sign", f, new Vector3(-13.5f, StreetY, -15.4f), 180f);
        // Bins, bags and a dumpster by the shops.
        Place(u + "metaltrashcan_beaten_overflowing.fbx", f, new Vector3(-5f, StreetY, -14.8f), 20f);
        Place(u + "trash_can_overflowing.fbx", f, new Vector3(9f, StreetY, -27.8f), 0f);
        Place(u + "dumpster_full_lid_open.fbx", f, new Vector3(-28f, StreetY, -33f), 10f);
        Place(u + "trashbag_1.fbx", f, new Vector3(-26f, StreetY, -31f), 30f);
        Place(u + "trashbag_3.fbx", f, new Vector3(-25f, StreetY, -30.6f), 70f);
        Place(u + "trashpile_2.fbx", f, new Vector3(4f, StreetY, -8f), 0f);
        // Dead ends: an abandoned bus and concrete barriers to the west; police barriers to the east.
        PlacePiece(stop, "Bus", f, new Vector3(-45f, StreetY - 0.15f, -21f), 90f);
        for (int i = 0; i < 4; i++) Place(u + (i % 2 == 0 ? "Jersey_barrier_dirty.fbx" : "Jersey_barrier_broken_edge_dirty.fbx"), f, new Vector3(-41.5f, StreetY - 0.15f, -24.5f + i * 2.6f), 90f + i * 4f);
        for (int i = 0; i < 4; i++) Place(u + (i == 2 ? "Type_3_barrier_broken_planks.fbx" : "Type_3_barrier_3_planks.fbx"), f, new Vector3(45f, StreetY - 0.15f, -24.6f + i * 2.5f), 90f);
        for (int i = 0; i < 5; i++) Place(u + "Traffic_cone_round.fbx", f, new Vector3(42.5f, StreetY - 0.15f, -24f + i * 1.9f), i * 25f);
        // Graffiti on the walls near the entrance and the shops.
        Place(u + "graffiti_Alt.fbx", f, new Vector3(-2.31f, StreetY - 1.4f, -8f), -90f);
        Place(u + "graffiti.fbx", f, new Vector3(-13f, StreetY + 0.8f, -31.95f), 0f);
    }

    // ---------------------------------------------------------------- cars
    static void BuildCars()
    {
        var cars = StreetGroup("Cars");
        string c = StreetDir + "/Cars/";
        // The maintenance van the player arrived in (Car8 is a box van), parked at the west end.
        var van = Car(cars, c + "Car8.fbx", new Vector3(-36.5f, -17.6f), 90f, "MaintenanceVan");
        var radio = van.AddComponent<RadioMessages>();
        radio.label = "van radio";
        radio.playOnStart = true;
        radio.crackle = Load<AudioClip>(SfxDir + "/SwitchButton3_On.wav");
        radio.lines = new[]
        {
            "DISPATCH: Unit 12, you there? The whole of Line 4 is dark. Start with Hollis Street.",
            "DISPATCH: Fuse box is down on the platform. Kowalski's crew left spares in the tunnels... somewhere.",
            "DISPATCH: And check in every hour. The last crew stopped checking in.",
        };
        // Parked along the curbs, a taxi left in the road and a police car blocking the east end.
        Car(cars, c + "Car3.fbx", new Vector3(-20f, -17.4f), 90f, "ParkedCar_1");
        Car(cars, c + "Car6.fbx", new Vector3(14f, -17.4f), 92f, "ParkedCar_2");
        Car(cars, c + "Car.fbx", new Vector3(-26f, -24.6f), -90f, "ParkedCar_3");
        Car(cars, c + "Car2.fbx", new Vector3(6f, -24.6f), -88f, "ParkedCar_4");
        Car(cars, c + "Car5_Taxi.fbx", new Vector3(24f, -21.5f), 115f, "AbandonedTaxi");   // stopped at an angle mid-road
        Car(cars, c + "Car5_Police.fbx", new Vector3(38.5f, -21f), 70f, "PoliceCar");
    }

    // Places a car body on the road with four wheels, scaled to a normal car size.
    static GameObject Car(Transform parent, string body, Vector2 xz, float yaw, string name)
    {
        const float scale = 0.72f;
        var car = Place(body, parent, new Vector3(xz.x, StreetY - 0.15f, xz.y), yaw, scale);
        car.name = name;
        var b = Bounds(car);
        float wheelY = b.min.y + 0.32f;
        // Wheels sit at 30% from each end, just inside the sides (in the car's own directions).
        Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward, right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        float halfLen = Mathf.Max(b.size.x, b.size.z) * 0.5f, halfWid = Mathf.Min(b.size.x, b.size.z) * 0.5f;
        foreach (int f in new[] { -1, 1 })
            foreach (int s in new[] { -1, 1 })
            {
                var wheel = Place(StreetDir + "/Cars/Wheel.fbx", car.transform, Vector3.zero, yaw + (s > 0 ? 0f : 180f), scale);
                Vector3 c = new Vector3(b.center.x, wheelY, b.center.z) + fwd * (f * halfLen * 0.62f) + right * (s * (halfWid - 0.12f));
                var wb = Bounds(wheel);
                wheel.transform.position += c - wb.center;
            }
        return car;
    }

    // ---------------------------------------------------------------- lore and foreshadowing
    static void BuildLore()
    {
        var lore = StreetGroup("Lore");
        var paper = Mat("PosterPaper", null, new Color(0.86f, 0.84f, 0.78f), 1f, 0.15f);
        var page = Load<AudioClip>(SfxDir + "/PaperDocument1.wav");

        // MISSING posters for the three night-shift workers, on lamp posts and the bus stop.
        string[] names = { "DALE KOWALSKI", "ROSA MENDEZ", "THEO BANKS" };
        string[] ages = { "54", "31", "26" };
        Vector3[] spots = { new Vector3(-23.85f, StreetY + 1.6f, -15.3f), new Vector3(-12f, StreetY + 1.5f, -14.0f), new Vector3(8.15f, StreetY + 1.6f, -15.3f) };
        for (int i = 0; i < 3; i++)
        {
            var poster = Prim(PrimitiveType.Cube, "MissingPoster_" + (i + 1), lore, spots[i], new Vector3(0.42f, 0.56f, 0.01f), paper);
            poster.transform.rotation = Quaternion.Euler(0, 180f, Random.Range(-4f, 4f));
            Label(poster.transform, "MISSING\n\n" + names[i] + "\nage " + ages[i] + "\n\nLINE 4 NIGHT SHIFT", new Vector3(0, 0.05f, -0.6f), 0.045f, new Color(0.1f, 0.08f, 0.07f), 0f);
            var note = poster.AddComponent<ReadableNote>();
            note.title = "poster";
            note.sound = page;
            note.text = "MISSING: " + names[i] + ", " + ages[i] + ". Metro Transit maintenance, Line 4 night shift. Last seen entering Hollis St station after midnight. If you have any information call the MTA hotline.";
        }

        // A soaked newspaper on the bus stop bench.
        var paperProp = Prim(PrimitiveType.Cube, "Newspaper", lore, new Vector3(-10.3f, StreetY + 0.5f, -14.2f), new Vector3(0.5f, 0.02f, 0.35f), paper);
        paperProp.transform.rotation = Quaternion.Euler(0, 20f, 0);
        var news = paperProp.AddComponent<ReadableNote>();
        news.title = "newspaper";
        news.sound = page;
        news.text = "THIRD TRANSIT WORKER VANISHES ON LINE 4. Rosa Mendez, 31, is the third maintenance worker in five weeks to disappear during a night shift. MTA officials dismissed staff reports of 'something wrapped up in the tunnels' as stress.";

        // A note taped to the 6twelve door.
        var shopNote = Prim(PrimitiveType.Cube, "ShopDoorNote", lore, new Vector3(2f, StreetY + 1.5f, -31.9f), new Vector3(0.25f, 0.32f, 0.01f), paper);
        var sn = shopNote.AddComponent<ReadableNote>();
        sn.title = "note on the door";
        sn.sound = page;
        sn.text = "CLOSED NIGHTS until further notice. Staff will not work past dark while the station stays open. Sorry for the inconvenience. - Management";

        // Graffiti scrawled by the stairs: the monster's name, written by someone who saw it.
        var scrawl = new GameObject("Graffiti_Warning");
        scrawl.transform.SetParent(lore);
        scrawl.transform.position = new Vector3(2.29f, StreetY - 1.9f, -7f);
        scrawl.transform.rotation = Quaternion.Euler(0, 90f, -6f);   // text faces into the stairwell (west)
        Label(scrawl.transform, "HE WRAPS THEM UP", Vector3.zero, 0.12f, new Color(0.55f, 0.06f, 0.05f), 0f);
        var gr = scrawl.AddComponent<BoxCollider>();
        gr.isTrigger = true; gr.size = new Vector3(3.2f, 0.6f, 0.2f);
        var gn = scrawl.AddComponent<ReadableNote>();
        gn.title = "graffiti";
        gn.text = "Red spray paint: HE WRAPS THEM UP. Underneath, smaller: they're still alive in there";

        // Payphone with an answering-machine message (placeholder shape until a model is found).
        var phone = Prim(PrimitiveType.Cube, "Payphone", lore, new Vector3(-16.5f, StreetY + 1.2f, -14.9f), new Vector3(0.45f, 0.75f, 0.3f), Mat("Payphone", null, new Color(0.25f, 0.27f, 0.3f), 1f, 0.5f, 0.6f));
        Prim(PrimitiveType.Cylinder, "PayphonePole", lore, new Vector3(-16.5f, StreetY + 0.4f, -14.9f), new Vector3(0.08f, 0.4f, 0.08f), Mat("GlobePost", null, new Color(0.1f, 0.18f, 0.12f), 1f, 0.5f, 0.6f));
        var machine = phone.AddComponent<RadioMessages>();
        machine.label = "payphone message";
        machine.crackle = Load<AudioClip>(SfxDir + "/SwitchButton3_On.wav");
        machine.lines = new[]
        {
            "PAYPHONE (recorded): ...this is Rosa. If anyone gets this, don't go past the trains.",
            "PAYPHONE (recorded): It's wrapping them. Dale's still moving in there, I can hear him-- [static]",
        };

        // Police tape around the gas station forecourt.
        var tape = Mat("PoliceTape", StripeTexture(), Color.white, 1f, 0.3f);
        Tape(lore, tape, new Vector3(18f, StreetY + 0.95f, -30.6f), new Vector3(40f, StreetY + 0.95f, -30.6f));
        Tape(lore, tape, new Vector3(18f, StreetY + 0.9f, -30.6f), new Vector3(18f, StreetY + 0.9f, -40f));

        // Hints of the monster: drag marks leading to the stairs, and something wrapped on the steps.
        var drag = TransparentMat("DragMarks", new Color(0.12f, 0.08f, 0.07f, 0.45f), 0.6f);   // wet smears, not black lines
        for (int i = 0; i < 2; i++)
        {
            var mark = Prim(PrimitiveType.Cube, "DragMark_" + i, lore, new Vector3(-0.35f + i * 0.7f, StreetY + 0.012f, -14.2f), new Vector3(0.28f, 0.004f, 3.6f), drag);
            mark.transform.rotation = Quaternion.Euler(0, -8f + i * 5f, 0);
        }
        var wrapped = new GameObject("WrappedShape");
        wrapped.transform.SetParent(lore);
        wrapped.transform.position = new Vector3(1.1f, 2.3f, -7.1f);    // lying across the steps, halfway down
        wrapped.transform.rotation = Quaternion.Euler(0, 15f, 0) * Quaternion.Euler(-34f, 0, 90f);
        var cloth = Mat("WrapCloth", null, new Color(0.32f, 0.3f, 0.27f), 1f, 0.1f);
        var bands = Mat("WrapBands", null, new Color(0.18f, 0.16f, 0.14f), 1f, 0.1f);
        var bodyShape = Prim(PrimitiveType.Capsule, "Body", wrapped.transform, wrapped.transform.position, new Vector3(0.45f, 0.85f, 0.4f), cloth);
        bodyShape.transform.SetParent(wrapped.transform, true);
        bodyShape.transform.localRotation = Quaternion.identity;
        for (int i = 0; i < 6; i++)
        {
            var band = Prim(PrimitiveType.Cylinder, "Band_" + i, wrapped.transform, Vector3.zero, new Vector3(0.48f, 0.02f, 0.43f), bands);
            band.transform.SetParent(wrapped.transform, false);
            band.transform.localPosition = new Vector3(0, -0.6f + i * 0.24f, 0);
            band.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f));
        }
    }

    // A strip of police tape between two points (thin box with a striped texture).
    static void Tape(Transform parent, Material mat, Vector3 a, Vector3 b)
    {
        var t = Prim(PrimitiveType.Cube, "PoliceTape", parent, (a + b) * 0.5f, new Vector3(0.005f, 0.08f, Vector3.Distance(a, b)), mat);
        t.transform.rotation = Quaternion.LookRotation(b - a);
        Object.DestroyImmediate(t.GetComponent<Collider>());
    }

    static Texture2D StripeTexture()
    {
        const string path = "Assets/Art/Textures/Generated/PoliceTape.png";
        EnsureFolder("Assets/Art/Textures/Generated");
        if (!System.IO.File.Exists(path))
        {
            var tex = new Texture2D(64, 8);
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 8; y++)
                    tex.SetPixel(x, y, ((x + y) / 8) % 2 == 0 ? new Color(0.95f, 0.8f, 0.1f) : new Color(0.08f, 0.08f, 0.08f));
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------------------------------------------------------- lighting and sky
    static void BuildOutdoorLighting()
    {
        // Moonlight: weak, cool, only on the street (rendering layer), with soft shadows.
        var moon = new GameObject("Moonlight").AddComponent<Light>();
        moon.transform.SetParent(street);
        moon.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        moon.type = LightType.Directional;
        moon.color = new Color(0.55f, 0.65f, 0.9f);
        moon.intensity = 0.6f;
        moon.shadows = LightShadows.Soft;
        SetLightLayers(moon, OutdoorLayer);

        // Blackout: only two sodium lamps still burn (backup power), one of them stuttering.
        LampHead("StreetLight_S_1", false);
        LampHead("StreetLight_S_0", false);     // by the van, so the opening is not pitch black
        LampHead("StreetLight_N_2", true);

        // Skybox: full-moon night (LESED pack), only visible from the street.
        string sky = "Assets/Art/Textures/Sky/FullMoonNight/";
        var skyMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Sky.mat");
        if (skyMat == null) { skyMat = new Material(Shader.Find("Skybox/6 Sided")); AssetDatabase.CreateAsset(skyMat, MatDir + "/M_Sky.mat"); }
        foreach (var face in new[] { ("_FrontTex", "pz"), ("_BackTex", "nz"), ("_LeftTex", "px"), ("_RightTex", "nx"), ("_UpTex", "py"), ("_DownTex", "ny") })
        {
            var tex = Load<Texture2D>(sky + face.Item2 + ".png");
            if (tex) tex.wrapMode = TextureWrapMode.Clamp;
            skyMat.SetTexture(face.Item1, tex);
        }
        skyMat.SetFloat("_Exposure", 0.8f);
        EditorUtility.SetDirty(skyMat);
        RenderSettings.skybox = skyMat;
    }

    static void LampHead(string lampName, bool flicker)
    {
        var lamp = street.Find("Furniture/" + lampName);   // (groups up here live under Street)
        if (lamp == null) { Debug.LogWarning("Lamp not found: " + lampName); return; }
        var b = Bounds(lamp.gameObject);
        // The head hangs out over the road at the top of the pole.
        var head = new GameObject("SodiumLight");
        head.transform.SetParent(lamp, true);
        Vector3 toRoad = new Vector3(0, 0, -20f - b.center.z).normalized;
        head.transform.position = new Vector3(b.center.x, b.max.y - 0.3f, b.center.z) + toRoad * (b.size.z * 0.4f);
        head.transform.rotation = Quaternion.Euler(90f, 0, 0);
        var light = head.AddComponent<Light>();
        light.type = LightType.Spot; light.color = Sodium;
        light.intensity = 40f; light.range = 16f; light.spotAngle = 110f; light.innerSpotAngle = 40f;
        light.shadows = LightShadows.Soft;
        if (flicker)
        {
            var f = head.AddComponent<FlickeringLight>();
            f.lamp = light;
            f.minCalm = 2f; f.maxCalm = 7f;
        }
    }

    // Light drizzle: thin streaks falling around the player while they're outdoors.
    static void BuildDrizzle(Transform player)
    {
        var go = new GameObject("Drizzle");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.4f; main.startSpeed = 9f; main.startSize = 0.008f; main.maxParticles = 1500;
        main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.18f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 700f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(24f, 24f, 0.1f);
        shape.rotation = new Vector3(90f, 0, 0);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 4f; r.velocityScale = 0.02f;
        r.sharedMaterial = TransparentMat("Raindrop", new Color(0.75f, 0.8f, 0.9f, 0.35f), 0.5f);
        go.AddComponent<Drizzle>().player = player;
    }

    // ---------------------------------------------------------------- helpers
    // Places a whole model (keeping its own materials) with the bottom-centre of its bounds at 'pos'.
    static GameObject Place(string path, Transform parent, Vector3 pos, float yaw, float scale = 1f)
    {
        var asset = Load<GameObject>(path);
        if (asset == null) return new GameObject("Missing_" + System.IO.Path.GetFileNameWithoutExtension(path));
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.transform.localScale = asset.transform.localScale * scale;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * asset.transform.rotation;
        StandOn(go, pos);
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh && mf.sharedMesh.bounds.size.magnitude > 0.05f) mf.gameObject.AddComponent<MeshCollider>();
        return go;
    }

    // Places one named piece of a combined model (keeping its materials).
    static GameObject PlacePiece(string path, string piece, Transform parent, Vector3 pos, float yaw, float scale = 1f)
    {
        var go = Place(path, parent, pos, yaw, scale);
        Transform keep = null;
        foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == piece) { keep = t; break; }
        if (keep == null) { Debug.LogWarning("Piece not found: " + piece + " in " + path); return go; }
        keep.SetParent(parent, true);
        Object.DestroyImmediate(go);
        StandOn(keep.gameObject, pos);
        return keep.gameObject;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // 3D text (for signs, posters and graffiti), facing along the parent's -Z by default.
    static void Label(Transform parent, string text, Vector3 localPos, float charSize, Color colour, float yaw)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        // Undo the parent's scale so letters keep their shape on stretched boxes.
        var ps = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(ps.x, 0.001f), 1f / Mathf.Max(ps.y, 0.001f), 1f / Mathf.Max(ps.z, 0.001f));
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = charSize;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = colour;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
    }

    static Transform StreetGroup(string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(street);
        return g;
    }

    static void Blocker(string name, Vector3 min, Vector3 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(street);
        go.transform.position = (min + max) * 0.5f;
        go.AddComponent<BoxCollider>().size = max - min;
    }
}
