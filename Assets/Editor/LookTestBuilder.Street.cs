using System.Linq;
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
// newspaper, a shop note, the van radio and a payphone answering machine. The monster is
// only hinted at (blood dripping down the stairs to a wrapped shape), never seen here.
public static partial class LookTestBuilder
{
    const float StreetY = 6f;
    const string StreetDir = "Assets/Art/Models/Environment/Street";
    const uint OutdoorLayer = 1u << 2;       // rendering layer lit by the moon (underground is not)

    static readonly Color Sodium = new Color(1f, 0.58f, 0.25f);
    static Transform street;
    static AreaBoundary boundary;   // the player is assigned once the player exists

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
        // The ground up to and under the shops is paved like the sidewalk, so the shopfronts stand on it.
        Span("GroundSouth", street, new Vector3(-50, y0, -2), new Vector3(50, y1, 35), pavement);
        Span("Road", street, new Vector3(-50, y0, -26), new Vector3(50, y1 - 0.15f, -16), asphalt);
        Span("SidewalkN", street, new Vector3(-50, y0, -30), new Vector3(50, y1, -26), pavement);
        Span("GroundNorth", street, new Vector3(-50, y0, -85), new Vector3(50, y1, -30), pavement);
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
        BuildCityBeyond(asphalt, pavement, lot);
        CutOutFoliage();   // trees and bushes: see-through leaf edges instead of black squares

        // "I should turn around..." near the invisible walls (the player is linked in Build()).
        boundary = street.gameObject.AddComponent<AreaBoundary>();
        boundary.min = new Vector2(-48f, -60f);
        boundary.max = new Vector2(48f, 30f);

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

        // Railings around the opening and globe lamps at the top of the stairs.
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
    }

    // ---------------------------------------------------------------- shops and buildings
    static void BuildShops()
    {
        var shops = StreetGroup("Shops");
        // North side, facing the road (south). Yaw turns each model so its shopfront faces the street.
        // Its fittings (and the vending machine outside) stood above the shell's base: stand it on the vending machine.
        Anchor(Place(StreetDir + "/Laundry/Laundry.fbx", shops, new Vector3(-38f, StreetY, -40f), LaundryYaw), "Vending_machine");
        // Its shell includes a lower storey meant to sit below the street: stand it on its door instead.
        Anchor(Place(StreetDir + "/VhsShop/VhsShop.fbx", shops, new Vector3(-20f, StreetY, -39f), VhsYaw), "Door");
        ShopGlass(shops, "VhsGlass", new Vector3(-27.4f, StreetY, -32.75f), new Vector3(-12.6f, StreetY + 3.6f, -32.65f));   // its window frames were empty
        var sixTwelve = Place(StreetDir + "/SixTwelve/SixTwelve.fbx", shops, new Vector3(2f, StreetY, -43f), SixTwelveYaw);
        // Its interior and front stood on a raised shop floor (not brought in): stand the door frames on the pavement.
        Anchor(sixTwelve, "Door_frame");
        // Its doormats belonged on a raised shop floor that wasn't brought in; without it they'd hover.
        foreach (var r in sixTwelve.GetComponentsInChildren<Renderer>()) if (r.name.StartsWith("Rug")) Object.DestroyImmediate(r.gameObject);
        Anchor(Place(StreetDir + "/GasStation/GasStation.fbx", shops, new Vector3(30f, StreetY, -52f), GasYaw), "Sidewalk");
        // South side, behind the entrance sidewalk.
        var pizzeria = Place(StreetDir + "/Pizzeria/Pizzeria.fbx", shops, new Vector3(24f, StreetY, 9f), PizzeriaYaw);
        Anchor(pizzeria, "building_01.001");
        ShopGlass(shops, "PizzeriaGlass", new Vector3(19f, StreetY, 0.12f), new Vector3(29f, StreetY + 2.9f, 0.2f));   // the doorway beside its door stood open
        // Its cellar-door tread hovered just above the pavement: set it down.
        foreach (var r in pizzeria.GetComponentsInChildren<Renderer>())
            if (r.name == "metal_tread") r.transform.position += Vector3.up * (StreetY + 0.004f - r.bounds.min.y);
        string blds = StreetDir + "/Buildings/Buildings.fbx";
        PlacePiece(blds, "Building", shops, new Vector3(-36f, StreetY, 9f), 180f);
        PlacePiece(blds, "Building_08", shops, new Vector3(-14f, StreetY, 6f), 180f);
        PlacePiece(blds, "Building_09", shops, new Vector3(5f, StreetY, 6f), 180f);
        PlacePiece(blds, "Building_07", shops, new Vector3(42f, StreetY, 8f), 180f);
        // The gap between Building_09 and the pizzeria showed the edge of the map: fill it with a building set a
        // little back, its left part hidden inside Building_09 and its right edge meeting the pizzeria.
        PlacePiece(blds, "Building_08", shops, new Vector3(11.2f, StreetY, 7.3f), 180f).name = "Building_08_Gap";
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
            // Set back from the curb (which is at z -16 and -26) so the bases stand fully on the sidewalk.
            Place(u + "street_light.fbx", f, new Vector3(x, StreetY, -14.5f), -90f).name = "StreetLight_S_" + i;   // arm out over the road
            Place(u + "street_light_clean.fbx", f, new Vector3(x + 8f, StreetY, -27.5f), 90f).name = "StreetLight_N_" + i;
        }
        // Trees and hedges.
        string t = StreetDir + "/Trees/";
        string[] trees = { "tree_rt_2", "dead_tree_rt_1", "tree_rt_4", "tree_rt_3", "dead_tree_rt_2", "tree_rt_2_1" };
        var soil = Mat("TreeSoil", Load<Texture2D>("Assets/Art/Models/Environment/Sewer/soil_mud.jpg"), new Color(0.6f, 0.52f, 0.45f), 1.5f, 0.2f);
        for (int i = 0; i < 6; i++)
            TreePit(f, Place(t + trees[i] + ".fbx", f, new Vector3(-44f + i * 17f, StreetY, -3.5f), i * 47f, 1.5f), soil);
        for (int i = 0; i < 5; i++)
            TreePit(f, Place(t + trees[(i + 2) % 6] + ".fbx", f, new Vector3(-36f + i * 17f, StreetY, -29f), i * 31f, 1.4f), soil);
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

    // A see-through glass pane (solid to walk into) set just behind a shopfront, filling its empty frames.
    static void ShopGlass(Transform parent, string name, Vector3 min, Vector3 max)
    {
        var glass = Span(name, parent, min, max, TransparentMat("ShopGlass", new Color(0.35f, 0.45f, 0.5f, 0.22f), 0.95f));
        glass.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // A square of soil in the pavement round a tree's trunk (found by its bark material), 3 cm proud.
    static void TreePit(Transform parent, GameObject tree, Material soil)
    {
        Vector3 at = tree.transform.position;
        foreach (var r in tree.GetComponentsInChildren<Renderer>())
            if (r.sharedMaterials.Any(m => m && m.name.ToLower().Contains("bark"))) { at = r.bounds.center; break; }
        Span("TreePit", parent, new Vector3(at.x - 0.75f, StreetY, at.z - 0.75f), new Vector3(at.x + 0.75f, StreetY + 0.03f, at.z + 0.75f), soil, false);
    }

    // Trees and bushes are leaf pictures on flat cards. Their own materials (inside the model files) draw
    // the see-through parts black, so every one gets a copy that cuts those parts out and shows both sides.
    static void CutOutFoliage()
    {
        var copies = new Dictionary<Material, Material>();
        foreach (var r in street.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                string path = AssetDatabase.GetAssetPath(m);
                if (!path.Contains("/Street/Trees/") && !path.Contains("/Street/Bushes/")) continue;
                if (!copies.TryGetValue(m, out var cut))
                {
                    cut = Mat("Foliage_" + m.name, m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") as Texture2D : null, Color.white, 1f, 0.1f);
                    cut.SetFloat("_AlphaClip", 1f);
                    cut.SetFloat("_Cutoff", 0.5f);
                    cut.EnableKeyword("_ALPHATEST_ON");
                    cut.SetFloat("_Cull", 0f);
                    cut.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    copies[m] = cut;
                }
                mats[i] = cut;
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    // Places a car on the road, scaled to a normal car size. (The car models have their wheels built in;
    // separate wheels used to be added too, which doubled them.)
    static GameObject Car(Transform parent, string body, Vector2 xz, float yaw, string name)
    {
        const float scale = 0.72f;
        var car = Place(body, parent, new Vector3(xz.x, StreetY - 0.15f, xz.y), yaw, scale);
        car.name = name;
        return car;
    }

    // ---------------------------------------------------------------- lore and foreshadowing
    static void BuildLore()
    {
        var lore = StreetGroup("Lore");
        var paper = Mat("PosterPaper", null, new Color(0.86f, 0.84f, 0.78f), 1f, 0.15f);
        var page = Load<AudioClip>(SfxDir + "/PaperDocument1.wav");

        // MISSING posters for the three night-shift workers, stuck flat onto two lamp posts and the
        // bus shelter (found by casting rays at them). Look at one to read it.
        Physics.SyncTransforms();
        var posterMat = Mat("MissingPoster", PosterTexture(), Color.white, 1f, 0.1f);
        string[] names = { "DALE KOWALSKI", "ROSA MENDEZ", "THEO BANKS" };
        string[] ages = { "54", "31", "26" };
        var furniture = street.Find("Furniture");
        GameObject[] holders = { furniture.Find("StreetLight_S_1").gameObject, furniture.Find("Bus_stop").gameObject, furniture.Find("StreetLight_S_3").gameObject };
        for (int i = 0; i < 3; i++)
        {
            var poster = Prim(PrimitiveType.Quad, "MissingPoster_" + (i + 1), lore, Vector3.zero, new Vector3(0.3f, 0.42f, 1f), posterMat);
            Object.DestroyImmediate(poster.GetComponent<Collider>());
            poster.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
            if (!StickTo(poster, holders[i], StreetY + 1.55f, Vector3.forward))   // seen from the road (north)
                Debug.LogWarning("Poster " + i + " found nothing to stick to");
            poster.transform.rotation *= Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
            poster.AddComponent<ReadableNote>().text = "MISSING\n" + names[i] + ", age " + ages[i] + "\nMetro Transit maintenance, Line 4 night shift.\nLast seen entering Hollis St station after midnight.\nIf you have any information call the MTA hotline.";
        }

        // A soaked newspaper lying on the bus shelter's bench.
        var newspaper = Prim(PrimitiveType.Cube, "Newspaper", lore, Vector3.zero, new Vector3(0.5f, 0.012f, 0.35f), Mat("Newsprint", PosterTexture(), new Color(0.8f, 0.8f, 0.78f), 1f, 0.2f));
        newspaper.transform.rotation = Quaternion.Euler(0, 20f, 0);
        if (Physics.Raycast(new Vector3(-10.3f, StreetY + 3f, -14.4f), Vector3.down, out var benchHit, 4f))
            newspaper.transform.position = benchHit.point + Vector3.up * 0.006f;
        newspaper.AddComponent<ReadableNote>().text = "THIRD TRANSIT WORKER VANISHES ON LINE 4\nRosa Mendez, 31, is the third maintenance worker in five weeks to disappear during a night shift. MTA officials dismissed staff reports of 'something wrapped up in the tunnels' as stress.";

        // A note taped to the 6twelve's door.
        var shopNote = Prim(PrimitiveType.Quad, "ShopDoorNote", lore, Vector3.zero, new Vector3(0.22f, 0.3f, 1f), Mat("NotePaper", null, new Color(0.9f, 0.88f, 0.8f), 1f, 0.1f));
        Object.DestroyImmediate(shopNote.GetComponent<Collider>());
        shopNote.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.05f);
        StickTo(shopNote, street.Find("Shops/SixTwelve").gameObject, StreetY + 1.5f, Vector3.back, 1.5f);   // shop is north of the road
        shopNote.AddComponent<ReadableNote>().text = "CLOSED NIGHTS until further notice.\nStaff will not work past dark while the station stays open.\nSorry for the inconvenience. - Management";
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

        // Hints of the monster: blood dripping down the stairs to something wrapped on the steps.
        string[] drops = { "BloodDrop01", "BloodDrop02", "BloodDrop20" };
        int[] steps = { 26, 24, 23, 21, 18, 16, 15 };
        for (int k = 0; k < steps.Length; k++)
        {
            int i = steps[k];
            float zMid = -1f - 0.3f * i + 0.15f;   // middle of step i's tread
            var drop = Decal("BloodDrip_" + k, lore, drops[k % 3], new Vector3(0.2f + 0.35f * Mathf.Sin(k * 1.7f), 0.2f * i + 0.004f, zMid), 0.28f + 0.08f * (k % 2), k * 53f);
        }
        Decal("BloodPool", lore, "BloodSpatter02", new Vector3(0.9f, 0.2f * 20 + 0.004f, -1f - 0.3f * 20 + 0.15f), 0.9f, 30f);
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

        // Blackout: only a few sodium lamps still burn on backup power, two of them stuttering.
        LampHead("StreetLight_S_1", false);
        LampHead("StreetLight_S_4", false);
        LampHead("StreetLight_N_0", true);
        LampHead("StreetLight_N_4", false);
        // A cold, failing tube under the gas station canopy and a dim light left on in the laundromat.
        ShopLight("Shops/GasStation", "The_ceiling", new Color(0.8f, 0.95f, 1f), 18f, 12f, true, -1.0f);
        ShopLight("Shops/Laundry", "Laundry_building", new Color(1f, 0.85f, 0.6f), 6f, 9f, false, -2.0f);
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

    // A light hung just under the top of a shop piece (canopy, ceiling).
    static void ShopLight(string path, string piece, Color colour, float intensity, float range, bool flicker, float belowTop)
    {
        var root = street.Find(path);
        if (root == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (r.name != piece) continue;
            var go = new GameObject(piece + "_Light");
            go.transform.SetParent(root, true);
            go.transform.position = new Vector3(r.bounds.center.x, r.bounds.max.y + belowTop, r.bounds.center.z);
            go.transform.rotation = Quaternion.Euler(90f, 0, 0);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.color = colour; l.intensity = intensity; l.range = range;
            l.spotAngle = 140f; l.innerSpotAngle = 60f; l.shadows = LightShadows.Soft;
            if (flicker) { var f = go.AddComponent<FlickeringLight>(); f.lamp = l; f.minCalm = 1f; f.maxCalm = 4f; }
            return;
        }
    }

    static void LampHead(string lampName, bool flicker)
    {
        var lamp = street.Find("Furniture/" + lampName);   // (groups up here live under Street)
        if (lamp == null) { Debug.LogWarning("Lamp not found: " + lampName); return; }
        // The light hangs just under the lamp head, at the end of the arm, found from the model itself.
        var head = new GameObject("SodiumLight");
        head.transform.SetParent(lamp, true);
        head.transform.position = UnderLampHead(lamp.gameObject);
        head.transform.rotation = Quaternion.Euler(90f, 0, 0);
        var light = head.AddComponent<Light>();
        light.type = LightType.Spot; light.color = Sodium;
        light.intensity = 40f; light.range = 16f; light.spotAngle = 110f; light.innerSpotAngle = 40f;
        light.shadows = LightShadows.Soft;
        light.shadowNearPlane = 0.3f;   // skip the lamp head right above it
        if (flicker)
        {
            var f = head.AddComponent<FlickeringLight>();
            f.lamp = light;
            f.minCalm = 2f; f.maxCalm = 7f;
        }
    }

    // A point just under a street lamp's head: the top of the model furthest out from the pole's base.
    static Vector3 UnderLampHead(GameObject lamp)
    {
        var points = new List<Vector3>();
        foreach (var mf in lamp.GetComponentsInChildren<MeshFilter>())
            foreach (var v in mf.sharedMesh.vertices) points.Add(mf.transform.TransformPoint(v));
        float bottom = points.Min(p => p.y), top = points.Max(p => p.y);
        var baseVerts = points.Where(p => p.y < bottom + 0.3f).ToList();
        Vector3 pole = new Vector3(baseVerts.Average(p => p.x), 0f, baseVerts.Average(p => p.z));
        System.Func<Vector3, float> reach = p => new Vector2(p.x - pole.x, p.z - pole.z).magnitude;
        var topVerts = points.Where(p => p.y > top - 0.45f).ToList();
        float furthest = topVerts.Max(reach);
        var headVerts = topVerts.Where(p => reach(p) > furthest * 0.7f).ToList();
        return new Vector3(headVerts.Average(p => p.x), headVerts.Min(p => p.y) - 0.1f, headVerts.Average(p => p.z));
    }

    // Light drizzle around the player while they're outdoors. Uses the "Rain Particles" pack (Game Seed
    // Assets: falling streaks plus little splashes where drops hit the ground) when it's in the project,
    // otherwise a simple built-in particle rain.
    const string RainPrefab = "Assets/Art/Particles/RainParticles/Prefabs/Rain Particles.prefab";

    static void BuildDrizzle(Transform player)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RainPrefab);
        if (prefab != null)
        {
            var rain = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rain.name = "Drizzle";
            foreach (var pr in rain.GetComponentsInChildren<ParticleSystemRenderer>())
                foreach (var m in pr.sharedMaterials) UpgradeParticleMaterial(m);
            // Drops fall in the world, so walking doesn't drag the rain along with you.
            foreach (var system in rain.GetComponentsInChildren<ParticleSystem>()) { var sysMain = system.main; sysMain.simulationSpace = ParticleSystemSimulationSpace.World; }
            // The pack's emitter is a flat box shooting along its local Z; tip it so drops fall straight down,
            // and thin it out from a downpour to a drizzle.
            rain.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var mainRain = rain.GetComponent<ParticleSystem>();
            if (mainRain) { var rainRate = mainRain.emission; rainRate.rateOverTime = 300f; }
            // The ripple rings left where drops land are 0.7 m wide and solid white in the pack;
            // make them small, dim and see-through so they read as faint splashes.
            var rings = rain.transform.Find("Water Ring Drops");
            if (rings && rings.TryGetComponent(out ParticleSystem ringSystem))
            {
                var ringMain = ringSystem.main;
                ringMain.startSize = 0.25f;
                ringMain.startColor = new Color(0.65f, 0.7f, 0.8f, 0.2f);
            }
            rain.AddComponent<Drizzle>().player = player;
            return;
        }
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

    // Asset-store particle materials often use the old built-in shaders, which show pink in URP.
    // Swap them to URP's unlit particle shader, keeping the texture and colour.
    static void UpgradeParticleMaterial(Material m)
    {
        if (m == null || m.shader.name.StartsWith("Universal Render Pipeline")) return;
        var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        var col = m.HasProperty("_TintColor") ? m.GetColor("_TintColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        m.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", col);
        m.SetFloat("_Surface", 1f);                 // transparent
        m.SetFloat("_Blend", 0f);                   // alpha blending
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }

    // ---------------------------------------------------------------- the city beyond the walls
    // The road, sidewalks and buildings carry on past the invisible walls for two more blocks each
    // way, with a skyline behind, so the street feels like part of a city. None of it can be reached.
    // Each building has an LOD group that stops drawing it once it's tiny on screen.
    static void BuildCityBeyond(Material asphalt, Material pavement, Material lot)
    {
        var city = StreetGroup("CityBeyond");
        float y0 = StreetY - 0.4f, y1 = StreetY;
        foreach (int side in new[] { -1, 1 })
        {
            string s = side < 0 ? "W" : "E";
            Span("Road_" + s, city, Mirror(new Vector3(50, y0, -26), side), Mirror(new Vector3(150, y1 - 0.15f, -16), side), asphalt);
            Span("SidewalkS_" + s, city, Mirror(new Vector3(50, y0, -16), side), Mirror(new Vector3(150, y1, -2), side), pavement);
            Span("SidewalkN_" + s, city, Mirror(new Vector3(50, y0, -30), side), Mirror(new Vector3(150, y1, -26), side), pavement);
            Span("GroundS_" + s, city, Mirror(new Vector3(50, y0, -2), side), Mirror(new Vector3(150, y1, 35), side), lot);
            Span("GroundN_" + s, city, Mirror(new Vector3(50, y0, -85), side), Mirror(new Vector3(150, y1, -30), side), lot);
        }
        string blds = StreetDir + "/Buildings/Buildings.fbx";
        string[] kinds = { "Building_03", "Building_04", "Building_05", "Building_06", "Building", "Building_08", "Building_01", "Building_09", "Building_07" };
        int k = 0;
        foreach (int side in new[] { -1, 1 })
            for (float x = 64f; x < 150f; x += 24f)
            {
                // Facades along both sides of the road, plus a second row behind them as skyline.
                Lod(PlacePiece(blds, kinds[k++ % kinds.Length], city, new Vector3(x * side, StreetY, 12f), 180f));
                Lod(PlacePiece(blds, kinds[k++ % kinds.Length], city, new Vector3(x * side, StreetY, -44f), 0f));
                Lod(PlacePiece(blds, kinds[k++ % kinds.Length], city, new Vector3((x + 12f) * side, StreetY, -78f), 0f));
            }
        for (float x = -60f; x <= 60f; x += 30f)
            Lod(PlacePiece(blds, kinds[k++ % kinds.Length], city, new Vector3(x, StreetY, -110f), 0f, 1.4f));

        // Dead street lamps, parked cars and a crashed car down the road on both sides.
        string u = StreetDir + "/Urban/", c = StreetDir + "/Cars/";
        foreach (int side in new[] { -1, 1 })
        {
            for (float x = 56f; x < 150f; x += 16f)
            {
                Lod(Place(u + "street_light.fbx", city, new Vector3(x * side, StreetY, -15.3f), -90f));
                Lod(Place(u + "street_light_clean.fbx", city, new Vector3((x + 8f) * side, StreetY, -26.7f), 90f));
            }
            Lod(Car(city, c + (side < 0 ? "Car4.fbx" : "Car7.fbx"), new Vector2(62f * side, -17.5f), 90f, "FarCar_" + side));
            Lod(Car(city, c + (side < 0 ? "Car2.fbx" : "Car3.fbx"), new Vector2(88f * side, -24.5f), -88f, "FarCar2_" + side));
            Lod(Car(city, c + "Car6.fbx", new Vector2(110f * side, -21f), 90f + 35f * side, "CrashedCar_" + side));
        }
        // Nothing out here needs to collide: the player can't reach it.
        foreach (var col in city.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
    }

    // One-level LOD group: the object is simply not drawn once it covers under 1.5% of the screen height.
    static GameObject Lod(GameObject go)
    {
        var group = go.AddComponent<LODGroup>();
        group.SetLODs(new[] { new LOD(0.015f, go.GetComponentsInChildren<Renderer>()) });
        group.RecalculateBounds();
        return go;
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

    // Moves a placed model up or down so its named main piece (the shop building) sits on the ground,
    // instead of its lowest part (which may be an underground floor or a pack's own terrain).
    static void Anchor(GameObject placed, string piece)
    {
        foreach (var r in placed.GetComponentsInChildren<Renderer>())
            if (r.name == piece) { placed.transform.position += Vector3.up * (StreetY - r.bounds.min.y); return; }
        Debug.LogWarning("Anchor piece not found: " + piece + " in " + placed.name);
    }

    // Sticks a flat quad onto the surface of 'target': rays are cast toward it from 'fromSide' at
    // height y, sweeping across its width; the nearest hit gives the spot and the surface direction.
    static bool StickTo(GameObject quad, GameObject target, float y, Vector3 fromSide, float within = 99f)
    {
        var b = Bounds(target);
        Vector3 across = Vector3.Cross(Vector3.up, fromSide).normalized;
        float best = float.MaxValue; RaycastHit bestHit = default;
        Vector3 centre = new Vector3(b.center.x, y, b.center.z);
        float half = Mathf.Min(Vector3.Scale(b.extents, new Vector3(Mathf.Abs(across.x), 0, Mathf.Abs(across.z))).magnitude, within);
        for (float t = -half; t <= half; t += 0.02f)
        {
            Vector3 start = centre + across * t - fromSide * (b.extents.magnitude + 2f);
            foreach (var h in Physics.RaycastAll(start, fromSide, b.extents.magnitude * 2f + 4f))
                if (h.collider.transform.IsChildOf(target.transform) && h.distance < best) { best = h.distance; bestHit = h; }
        }
        if (best == float.MaxValue) return false;
        quad.transform.position = bestHit.point + bestHit.normal * 0.006f;
        quad.transform.rotation = Quaternion.LookRotation(-bestHit.normal, Vector3.up);   // a quad shows its front along -Z
        return true;
    }

    // A flat blood decal lying on a surface (texture with transparency), turned by 'spin' degrees.
    static GameObject Decal(string name, Transform parent, string texture, Vector3 pos, float size, float spin)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Decal_" + texture + ".mat");
        if (mat == null)
        {
            mat = TransparentMat("Decal_" + texture, new Color(0.55f, 0.55f, 0.55f, 1f), 0.6f);
            mat.SetFloat("_BlendModePreserveSpecular", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        mat.SetTexture("_BaseMap", Load<Texture2D>("Assets/Art/Textures/Blood/" + texture + ".png"));
        var q = Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size, size, 1f), mat);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.rotation = Quaternion.Euler(90f, spin, 0f);   // lying flat, facing up
        q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return q;
    }

    // A simple MISSING poster drawn by code: red header band, a grey photo box, lines of "text".
    static Texture2D PosterTexture()
    {
        const string path = "Assets/Art/Textures/Generated/MissingPoster.png";
        EnsureFolder("Assets/Art/Textures/Generated");
        if (!System.IO.File.Exists(path))
        {
            const int w = 64, h = 90;
            var tex = new Texture2D(w, h);
            var rng = new System.Random(3);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0.86f, 0.84f, 0.76f);                                  // paper
                    if (y > 74 && y < 86 && x > 4 && x < 59) c = new Color(0.62f, 0.08f, 0.07f); // MISSING band
                    else if (y > 40 && y < 72 && x > 16 && x < 47) c = new Color(0.32f, 0.31f, 0.3f); // photo
                    else if (y < 36 && y > 6 && y % 5 < 2 && x > 6 && x < 57 - (y * 7 % 13)) c = new Color(0.2f, 0.18f, 0.17f); // text lines
                    c *= 0.9f + (float)rng.NextDouble() * 0.12f;                              // grubby
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
