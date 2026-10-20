using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds the look-test scene: a white-tiled corridor that opens onto a metro platform with tracks,
// puddles, fluorescent lamps (one flickering and buzzing), dark tunnel mouths, and a walkable player.
// Menu: Last Stop > Build Look Test. Safe to re-run: it rebuilds the scene from scratch.
// Walls/floors are plain boxes whose textures are laid out in metres, so tiles line up across pieces.
public static class LookTestBuilder
{
    const string ScenePath = "Assets/Scenes/LookTest.unity";
    const string MetroDir = "Assets/Art/Models/Environment/MetroPSX";
    const string MatDir = "Assets/Art/Materials/LookTest";
    const string SfxDir = "Assets/Art/Audio/SFX/Darkworld";
    const string AmbienceDir = "Assets/Art/Audio/Ambience/HeltonYan";

    // Light colours chosen with the creative director: sickly green fluorescent, red in tunnels.
    static readonly Color Fluorescent = new Color(0.78f, 0.95f, 0.80f);
    static readonly Color Emergency = new Color(1f, 0.12f, 0.08f);
    static readonly Color FogColour = new Color(0.010f, 0.014f, 0.012f);

    static Transform root;
    static readonly Dictionary<Material, float> tileSize = new Dictionary<Material, float>();

    [MenuItem("Last Stop/Build Look Test")]
    public static void Build()
    {
        tileSize.Clear();
        EnsureFolder(MatDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Level").transform;

        // ---- materials (texture, how many metres one copy of the texture covers)
        var wallTile = Mat("WallTile", Tex("tile_tile.jpg"), Color.white, 1.6f, 0.35f);
        var redTile = Mat("WallTileRed", Tex("tile_tile.jpg"), new Color(0.48f, 0.10f, 0.09f), 1.6f, 0.35f);
        var floorTile = Mat("FloorTile", Tex("Tiles.png"), Color.white, 1.2f, 0.25f);
        var tactile = Mat("TactileStrip", Tex("TactilePaving.png"), Color.white, 0.6f, 0.2f);
        var concrete = Mat("Concrete", Tex("ConcreteBare.png"), new Color(0.45f, 0.45f, 0.43f), 2f, 0.1f);
        var tunnelConcrete = Mat("TunnelConcrete", Tex("ConcreteBare.png"), new Color(0.25f, 0.24f, 0.23f), 2f, 0.1f);
        var ceiling = Mat("Ceiling", Tex("ConcreteBare.png"), new Color(0.22f, 0.22f, 0.21f), 2.5f, 0.05f);
        var gravel = Mat("PitGravel", Tex("Floor.png"), new Color(0.35f, 0.33f, 0.30f), 1.5f, 0.05f);
        var trackBed = Mat("TrackBed", Tex("road_rails.jpg"), new Color(0.6f, 0.58f, 0.55f), 2.6f, 0.1f);
        var railMetal = Mat("RailMetal", Tex("metal_02.png"), new Color(0.55f, 0.52f, 0.5f), 1f, 0.6f, 0.8f);
        var housing = Mat("LampHousing", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.3f, 0.5f);
        var doorMetal = Mat("DoorMetal", Tex("Door_01.jpg"), new Color(0.7f, 0.7f, 0.7f), 1f, 0.3f, 0.3f);
        var puddle = TransparentMat("Puddle", new Color(0.01f, 0.012f, 0.012f, 0.6f), 0.98f);
        var tubeLit = EmissiveMat("TubeLit", Fluorescent * 2.2f);
        var tubeDead = Mat("TubeDead", null, new Color(0.25f, 0.27f, 0.25f), 1f, 0.6f);
        var redGlow = EmissiveMat("EmergencyGlow", Emergency * 3f);

        // Layout rule (see GeometryCheck.cs): pieces meet edge to edge and never overlap along a face,
        // otherwise their surfaces fight for the same pixels and flicker. Walls run the full height,
        // ceilings and floors fit between them. Each piece is given by its min and max corners.

        // ---- corridor: inside x -2..2, z 0..13.7, ceiling at 3 m
        var corridor = Group("Corridor");
        Span("Floor", corridor, new Vector3(-2, -0.2f, 0), new Vector3(2, 0, 14), floorTile);
        Span("Ceiling", corridor, new Vector3(-2, 3, 0), new Vector3(2, 3.2f, 13.7f), ceiling);
        Span("WallLeft", corridor, new Vector3(-2.3f, 0, 0), new Vector3(-2, 3.2f, 13.7f), wallTile);
        Span("WallRight", corridor, new Vector3(2, 0, 0), new Vector3(2.3f, 3.2f, 13.7f), wallTile);
        Span("WallBack", corridor, new Vector3(-2.3f, 0, -0.3f), new Vector3(2.3f, 3.2f, 0), wallTile);
        Span("StripeLeft", corridor, new Vector3(-2, 2.19f, 0), new Vector3(-1.98f, 2.41f, 14), redTile, false);
        Span("StripeRight", corridor, new Vector3(1.98f, 2.19f, 0), new Vector3(2, 2.41f, 14), redTile, false);
        Span("Door", corridor, new Vector3(-0.55f, 0, 0), new Vector3(0.55f, 2.2f, 0.06f), doorMetal);
        Lamp("Lamp_1", corridor, new Vector3(0, 2.98f, 3), 0f, true, tubeLit, tubeDead, housing);
        Lamp("Lamp_2_Dead", corridor, new Vector3(0, 2.98f, 7.5f), 0f, false, tubeLit, tubeDead, housing);
        Lamp("Lamp_3", corridor, new Vector3(0, 2.98f, 12), 0f, true, tubeLit, tubeDead, housing);

        // ---- platform hall: inside x -18..18, z 14..25.5, ceiling at 4.5 m; track pit z 20..25.5, 1.3 m deep
        var hall = Group("Platform");
        Span("PlatformFloor", hall, new Vector3(-18, -0.2f, 14), new Vector3(18, 0, 19.4f), floorTile);
        Span("TactileStrip", hall, new Vector3(-18, -0.2f, 19.4f), new Vector3(18, 0, 20), tactile);
        Span("PlatformEdge", hall, new Vector3(-18, -1.3f, 19.8f), new Vector3(18, -0.2f, 20), concrete);
        Span("NearWall_L", hall, new Vector3(-18, 0, 13.7f), new Vector3(-2, 4.7f, 14), wallTile);
        Span("NearWall_R", hall, new Vector3(2, 0, 13.7f), new Vector3(18, 4.7f, 14), wallTile);
        Span("NearWall_Top", hall, new Vector3(-2, 3, 13.7f), new Vector3(2, 4.7f, 14), wallTile);
        Span("StripeNear_L", hall, new Vector3(-18, 2.19f, 14), new Vector3(-2, 2.41f, 14.02f), redTile, false);
        Span("StripeNear_R", hall, new Vector3(2, 2.19f, 14), new Vector3(18, 2.41f, 14.02f), redTile, false);
        Span("FarWall", hall, new Vector3(-18.3f, -1.5f, 25.5f), new Vector3(18.3f, 4.7f, 25.8f), wallTile);
        Span("StripeFar", hall, new Vector3(-18, 2.19f, 25.48f), new Vector3(18, 2.41f, 25.5f), redTile, false);
        Span("HallCeiling", hall, new Vector3(-18, 4.5f, 14), new Vector3(18, 4.7f, 25.5f), ceiling);
        foreach (int side in new[] { -1, 1 })
        {
            string s = side < 0 ? "W" : "E";
            // The end wall closes the platform side; the lintel is the beam above the tunnel mouth.
            Span("EndWall_" + s, hall, Mirror(new Vector3(18, -1.5f, 13.7f), side), Mirror(new Vector3(18.3f, 4.7f, 20), side), wallTile);
            Span("TunnelLintel_" + s, hall, Mirror(new Vector3(18, 2, 20), side), Mirror(new Vector3(18.3f, 4.7f, 25.5f), side), concrete);
        }
        float[] lampX = { -12, -6, 0, 6, 12 };
        for (int i = 0; i < lampX.Length; i++)
        {
            bool dead = i == 3;        // one dead tube breaks up the row of light
            bool flicker = i == 1;     // one flickering, buzzing tube
            var lamp = Lamp("HallLamp_" + (i + 1) + (dead ? "_Dead" : flicker ? "_Flicker" : ""), hall,
                            new Vector3(lampX[i], 4.48f, 18.6f), 90f, !dead, tubeLit, tubeDead, housing);
            if (flicker) MakeFlicker(lamp);
        }

        // ---- track pit and the two tunnels it runs into (x -40..40)
        var track = Group("Track");
        Span("PitFloor", track, new Vector3(-40, -1.5f, 20), new Vector3(40, -1.3f, 25.5f), gravel);
        Span("TrackBed", track, new Vector3(-40, -1.3f, 21.45f), new Vector3(40, -1.29f, 24.05f), trackBed, false);
        Span("Rail_N", track, new Vector3(-40, -1.29f, 22f), new Vector3(40, -1.18f, 22.07f), railMetal);
        Span("Rail_S", track, new Vector3(-40, -1.29f, 23.43f), new Vector3(40, -1.18f, 23.5f), railMetal);
        // Flat decals (puddles) float 1 cm above the surface below so they never fight with it.
        Puddle("PitPuddle", track, new Vector3(3.5f, -1.28f, 22.6f), 4.5f, 2.6f, puddle);
        // Short service steps at the west end, so the player can climb back out of the pit.
        for (int k = 0; k < 4; k++)
        {
            float zFront = 20f + (4 - k) * 0.35f;   // lowest step sticks out furthest
            Span("PitStep_" + (k + 1), track, new Vector3(-16.9f, -1.3f, zFront - 0.35f), new Vector3(-15.5f, -1.3f + (k + 1) * 0.3f, zFront), concrete);
        }
        foreach (int side in new[] { -1, 1 })
        {
            string s = side < 0 ? "W" : "E";
            var tunnel = Group("Tunnel_" + s);
            // Tunnel pieces start at x = 18.3, right where the hall's end wall and lintel stop.
            Span("InnerWall", tunnel, Mirror(new Vector3(18.3f, -1.5f, 19.7f), side), Mirror(new Vector3(40, 2.2f, 20), side), tunnelConcrete);
            Span("OuterWall", tunnel, Mirror(new Vector3(18.3f, -1.5f, 25.5f), side), Mirror(new Vector3(40, 2.2f, 25.8f), side), tunnelConcrete);
            Span("Ceiling", tunnel, Mirror(new Vector3(18.3f, 2, 20), side), Mirror(new Vector3(40, 2.2f, 25.5f), side), tunnelConcrete);
            Span("EndCap", tunnel, Mirror(new Vector3(40, -1.5f, 19.7f), side), Mirror(new Vector3(40.3f, 2.2f, 25.8f), side), tunnelConcrete);
        }
        // Red emergency lamp deep in the east tunnel: a distant warning colour at the edge of the fog.
        var red = Box("EmergencyLamp", root.Find("Tunnel_E"), new Vector3(31, 1.2f, 20.03f), new Vector3(0.25f, 0.18f, 0.06f), redGlow);
        var redLight = AddLight(red, LightType.Point, Emergency, 8f, 7f);
        redLight.transform.localPosition = new Vector3(0, 0, 0.4f);

        // Second puddle on the platform floor, under a working lamp.
        Puddle("PlatformPuddle", hall, new Vector3(-0.8f, 0.01f, 17.4f), 2.6f, 1.6f, puddle);

        // ---- props from the Metro PSX pack
        var props = Group("Props");
        Prop("seats", props, new Vector3(-9f, 0, 15.0f), 90f);
        Prop("seats", props, new Vector3(9f, 0, 15.0f), 90f);
        Prop("trash_can", props, new Vector3(3.3f, 0, 14.6f), 0f);
        var vending = Prop("Vending_machine", props, new Vector3(-15.2f, 0, 14.5f), 0f);
        if (vending)
        {
            var glow = AddLight(vending, LightType.Point, new Color(0.85f, 0.9f, 1f), 3f, 3.5f);
            glow.transform.position = vending.GetComponentInChildren<Renderer>().bounds.center + new Vector3(0, 0.2f, 0.9f);
            glow.shadows = LightShadows.None;
        }
                var map = Prop("metro_map", props, new Vector3(1.9f, 1.05f, 9f), -90f);
        if (map) { var mb = Bounds(map); map.transform.position += new Vector3(1.995f - mb.max.x, 0, 0); }

        // ---- world look: darkness, fog, reflections, post effects, sound
        SetupEnvironment();
        var probe = new GameObject("ReflectionProbe").AddComponent<ReflectionProbe>();
        probe.transform.SetParent(root);
        probe.transform.position = new Vector3(0, 1.5f, 20);
        probe.size = new Vector3(36, 7, 12);
        probe.boxProjection = true;
        probe.intensity = 1f;
        probe.resolution = 128;
        BakeReflection(probe);

        var drone = new GameObject("AmbientDrone").AddComponent<AudioSource>();
        drone.clip = Load<AudioClip>(AmbienceDir + "/DSGNDron_Tonal-Too Quiet_HY_SD.wav");
        drone.loop = true; drone.playOnAwake = true; drone.volume = 0.3f; drone.spatialBlend = 0f;

        BuildPlayer(new Vector3(0, 0.05f, 1.6f));

        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;

        GeometryCheck.Find(root); // warns in the Console about any overlapping surfaces

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Look test built: " + ScenePath);
    }

    // ---------------------------------------------------------------- player
    static void BuildPlayer(Vector3 position)
    {
        var player = new GameObject("Player");
        player.transform.position = position;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.35f;
        var movement = player.AddComponent<PlayerMovement>();

        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0, 1.65f, 0);
        var camera = cam.AddComponent<Camera>();
        camera.fieldOfView = 60f;            // vertical; about 90° across a 16:9 screen
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 60f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cam.AddComponent<AudioListener>();
        var camData = cam.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

        var look = player.AddComponent<PlayerLook>();
        look.cameraPivot = cam.transform;

        var torch = new GameObject("Flashlight");
        torch.transform.SetParent(cam.transform, false);
        torch.transform.localPosition = new Vector3(0.22f, -0.18f, 0.1f);
        var beam = torch.AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.color = new Color(1f, 0.94f, 0.82f);
        beam.intensity = 20f; beam.range = 25f;
        beam.spotAngle = 52f; beam.innerSpotAngle = 22f;
        beam.shadows = LightShadows.Soft;
        var torchAudio = torch.AddComponent<AudioSource>();
        torchAudio.playOnAwake = false; torchAudio.spatialBlend = 0f; torchAudio.volume = 0.6f;
        var flashlight = torch.AddComponent<Flashlight>();
        flashlight.beam = beam;
        flashlight.clickOn = Load<AudioClip>(SfxDir + "/SwitchButton1_On.wav");
        flashlight.clickOff = Load<AudioClip>(SfxDir + "/SwitchButton1_Off.wav");

        var stepAudio = player.AddComponent<AudioSource>();
        stepAudio.playOnAwake = false; stepAudio.spatialBlend = 0f;
        var steps = player.AddComponent<Footsteps>();
        steps.movement = movement;
        steps.steps = new[]
        {
            Load<AudioClip>(SfxDir + "/FootstepsConcrete1.wav"), Load<AudioClip>(SfxDir + "/FootstepsConcrete2.wav"),
            Load<AudioClip>(SfxDir + "/FootstepsConcrete3.wav"), Load<AudioClip>(SfxDir + "/FootstepsConcrete4.wav"),
        };
    }

    // ---------------------------------------------------------------- environment
    static void SetupEnvironment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.052f, 0.05f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = true;
        // Fog that thickens gradually: things are mostly gone by ~15 m, but bright lamps still glow through.
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogColour;
        RenderSettings.fogDensity = 0.1f;
        RenderSettings.sun = null;

        // Post effects: soft glow on tubes, dark edges, film grain, slightly drained colour.
        string profilePath = "Assets/Settings/LookTestVolume.asset";
        AssetDatabase.DeleteAsset(profilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f); bloom.intensity.Override(0.7f); bloom.scatter.Override(0.55f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.38f); vignette.smoothness.Override(0.5f);
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium1); grain.intensity.Override(0.3f);
        var colour = profile.Add<ColorAdjustments>(true);
        colour.saturation.Override(-18f); colour.contrast.Override(12f); colour.postExposure.Override(0.6f);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);

        var volume = new GameObject("PostProcessing").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    // ---------------------------------------------------------------- lamps
    // A ceiling fluorescent fixture: dark housing, a tube, and a downward spot light that makes a pool.
    static GameObject Lamp(string name, Transform parent, Vector3 pos, float yaw, bool lit,
                           Material tubeLit, Material tubeDead, Material housing)
    {
        var lamp = new GameObject(name);
        lamp.transform.SetParent(parent);
        lamp.transform.position = pos;
        lamp.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var body = Box("Housing", lamp.transform, Vector3.zero, new Vector3(0.28f, 0.07f, 1.45f), housing);
        body.transform.localPosition = Vector3.zero;
        var tube = Box("Tube", lamp.transform, Vector3.zero, new Vector3(0.09f, 0.045f, 1.3f), lit ? tubeLit : tubeDead);
        tube.transform.localPosition = new Vector3(0, -0.05f, 0);
        Object.DestroyImmediate(tube.GetComponent<Collider>());
        if (lit)
        {
            // Lights fade with distance squared, so the high platform lamps need more power and reach.
            bool high = pos.y > 4f;
            var light = AddLight(lamp, LightType.Spot, Fluorescent, high ? 45f : 16f, high ? 13f : 9f);
            light.transform.localPosition = new Vector3(0, -0.1f, 0);
            light.transform.rotation = Quaternion.Euler(90, 0, 0);
            light.spotAngle = 120f; light.innerSpotAngle = 50f;
            light.shadows = LightShadows.Soft;
        }
        return lamp;
    }

    static void MakeFlicker(GameObject lamp)
    {
        var buzz = lamp.AddComponent<AudioSource>();
        buzz.clip = Load<AudioClip>(AmbienceDir + "/DSGNDron_Noise-Broken Speakers_HY_SD.wav");
        buzz.loop = true; buzz.playOnAwake = true; buzz.volume = 0.45f;
        buzz.spatialBlend = 1f; buzz.rolloffMode = AudioRolloffMode.Linear;
        buzz.minDistance = 1f; buzz.maxDistance = 12f;
        var flicker = lamp.AddComponent<FlickeringLight>();
        flicker.lamp = lamp.GetComponentInChildren<Light>();
        flicker.tube = lamp.transform.Find("Tube").GetComponent<Renderer>();
        flicker.tubeGlow = Fluorescent * 2.2f;
        flicker.buzz = buzz;
    }

    static Light AddLight(GameObject owner, LightType type, Color colour, float intensity, float range)
    {
        var go = new GameObject(type + "Light");
        go.transform.SetParent(owner.transform, false);
        var light = go.AddComponent<Light>();
        light.type = type; light.color = colour; light.intensity = intensity; light.range = range;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        return light;
    }

    // ---------------------------------------------------------------- props
    // Copies one named piece out of the Metro PSX model, stands it on the floor at 'floorPos'
    // turned by 'yaw' degrees, and gives it the matching material.
    static GameObject Prop(string piece, Transform parent, Vector3 floorPos, float yaw)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MetroDir + "/MetroPSX.fbx");
        Transform source = null;
        foreach (var t in model.GetComponentsInChildren<Transform>()) if (t.name == piece) { source = t; break; }
        if (source == null) { Debug.LogWarning("Metro piece not found: " + piece); return null; }

        var go = Object.Instantiate(source.gameObject, parent);
        go.name = piece;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * source.rotation;
        go.transform.localScale = source.lossyScale;
        foreach (Transform child in go.transform) Object.DestroyImmediate(child.gameObject); // only this piece

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null) mats[i] = PropMat(mats[i].name);
            r.sharedMaterials = mats;
        }

        var b = Bounds(go);
        go.transform.position += floorPos - new Vector3(b.center.x, b.min.y, b.center.z);
        go.AddComponent<MeshCollider>();
        return go;
    }


    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static Material PropMat(string sourceName)
    {
        string clean = sourceName.Replace(" (Instance)", "");
        string[] exts = { ".png", ".jpg" };
        Texture2D tex = null;
        foreach (var e in exts) { tex = Tex(clean + e); if (tex) break; }
        return Mat("Prop_" + clean, tex, Color.white, 1f, 0.2f);
    }

    // ---------------------------------------------------------------- geometry
    // A puddle: one big flat oval plus a small separate splash beside it. They must not overlap,
    // because see-through layers on top of each other would look darker where they cross.
    static void Puddle(string name, Transform parent, Vector3 center, float length, float width, Material mat)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent);
        group.position = center;
        Oval(group, "Main", Vector3.zero, new Vector3(length * 0.8f, 0.002f, width), 8f, mat);
        Oval(group, "Splash", new Vector3(length * 0.58f, 0, width * 0.25f), new Vector3(length * 0.22f, 0.002f, width * 0.35f), -25f, mat);
    }

    static void Oval(Transform parent, string name, Vector3 localPos, Vector3 scale, float yaw, Material mat)
    {
        var blob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        blob.name = name;
        Object.DestroyImmediate(blob.GetComponent<Collider>());
        blob.transform.SetParent(parent, false);
        blob.transform.localPosition = localPos;
        blob.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        blob.transform.localScale = scale;
        blob.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
    static Transform Group(string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(root);
        return g;
    }

    // A box given by its lowest and highest corners (easier to make pieces meet exactly).
    static GameObject Span(string name, Transform parent, Vector3 min, Vector3 max, Material mat, bool collide = true)
    {
        Vector3 lo = Vector3.Min(min, max), hi = Vector3.Max(min, max);
        return Slab(name, parent, (lo + hi) * 0.5f, hi - lo, mat, collide);
    }

    // Flips a point to the west side (x becomes -x) when side is -1.
    static Vector3 Mirror(Vector3 p, int side) => new Vector3(p.x * side, p.y, p.z);

    // A solid box (with collider) whose texture repeats every N metres in world space.
    static GameObject Slab(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collide = true)
    {
        var go = Box(name, parent, center, size, mat);
        if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center; // centres are given in world space
        float tile = tileSize.TryGetValue(mat, out var t) ? t : 1f;
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(center, size, tile, name);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        return go;
    }

    // Builds a box mesh around (0,0,0) whose UVs come from world position, so neighbouring boxes
    // share one continuous tile grid.
    static Mesh BoxMesh(Vector3 worldCenter, Vector3 size, float tile, string name)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var norms = new List<Vector3>(); var tris = new List<int>();
        Vector3 h = size * 0.5f;
        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var n in normals)
        {
            Vector3 a = n.x != 0 ? Vector3.forward : Vector3.right;   // first edge direction
            Vector3 b = n.y != 0 ? Vector3.forward : Vector3.up;      // second edge direction
            Vector3 c = Vector3.Scale(n, h);
            Vector3 ha = Vector3.Scale(a, h), hb = Vector3.Scale(b, h);
            Vector3[] q = { c - ha - hb, c - ha + hb, c + ha + hb, c + ha - hb };
            int start = verts.Count;
            foreach (var p in q)
            {
                verts.Add(p);
                norms.Add(n);
                Vector3 w = worldCenter + p;
                Vector2 uv = n.x != 0 ? new Vector2(w.z, w.y) : n.y != 0 ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
                uvs.Add(uv / tile);
            }
            // Make sure the triangles face outward (Unity shows only the front side).
            bool flip = Vector3.Dot(Vector3.Cross(q[1] - q[0], q[2] - q[0]), n) < 0;
            if (flip) tris.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            else tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------------------------------------------------------------- assets
    static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(MetroDir + "/Textures/" + file);
    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("Missing asset: " + path);
        return a;
    }

    static Material Mat(string name, Texture2D tex, Color tint, float metresPerTile, float smoothness, float metallic = 0f)
    {
        string path = MatDir + "/M_" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        tileSize[mat] = metresPerTile;
        return mat;
    }

    // Glowing surfaces (lamp tubes) use the Unlit shader with a colour brighter than white,
    // which the bloom effect turns into a soft glow. Unlit ignores lighting, so it always shines.
    // A see-through glossy surface (puddles): the floor shows through, darkened, with reflections on top.
    static Material TransparentMat(string name, Color colour, float smoothness)
    {
        var mat = Mat(name, null, colour, 1f, smoothness);
        mat.SetFloat("_Surface", 1f);            // transparent
        mat.SetFloat("_Blend", 0f);              // alpha blending
        mat.SetFloat("_BlendModePreserveSpecular", 1f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        return mat;
    }

    // Captures what the station looks like from the probe's spot into a reflection image once, at build
    // time, so puddles reflect the glowing tubes. (Live reflections are off in this project's quality settings.)
    static void BakeReflection(ReflectionProbe probe)
    {
        const string path = "Assets/Lighting/LookTest_Reflection.exr";
        EnsureFolder("Assets/Lighting");
        probe.mode = ReflectionProbeMode.Custom;
        if (Lightmapping.BakeReflectionProbe(probe, path))
        {
            AssetDatabase.ImportAsset(path);
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
        }
        else Debug.LogWarning("Reflection bake failed");
    }

    static Material EmissiveMat(string name, Color glow)
    {
        string path = MatDir + "/M_" + name + ".mat";
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null && mat.shader != unlit) { AssetDatabase.DeleteAsset(path); mat = null; }
        if (mat == null) { mat = new Material(unlit); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", glow);
        EditorUtility.SetDirty(mat);
        tileSize[mat] = 1f;
        return mat;
    }

    static void EnsureFolder(string path)
    {
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
