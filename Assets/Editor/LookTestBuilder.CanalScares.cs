using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Canal St's one-time scares (ScareTrigger spots on Canal St's own power grid):
// - the ticket booth phone rings as you first reach the mezzanine (RingingPhone): it stops dead when you get
//   right up to it; answer it in time and there's only breathing on the line;
// - after the power, lamps pop and die behind you: escalator 1, the ticket gates, escalator 2;
// - part of the north-east tunnel's ceiling caves in a few metres ahead (CeilingCollapse); the rubble stays;
// - lower level: the storeroom locker bangs twice from inside as you come near (LockerKnock);
// - lower level: corridor A's bulbs go out one by one toward you as you come up from the stairs
//   (LightsOutTowards), then stutter back on.
public static partial class LookTestBuilder
{
    static void CanalScares(Transform canal)
    {
        var g = new GameObject("CanalScares").transform;
        g.SetParent(canal);
        float z = CanalZ, mezz = 6f;
        var pop = Load<AudioClip>(ScareSfx + "/LampPop.wav");

        // ---- the phone in the ticket booth (on the back counter, by the booth's doorway)
        var phone = BuildPhone(g, new Vector3(57.45f, mezz + 0.95f, z + 0.3f));
        var ring = CanalScareSpot(g, "Scare_CanalPhone", new Vector3(52f, mezz + 1f, z + 10f), new Vector3(1f, 2f, 4f), ScareTrigger.PowerRule.Any);
        ring.AddComponent<ScareRelay>().target = phone;

        // ---- lamps dying behind you, after the power (each trigger just past its lamp)
        CanalLampDies(g, "Scare_CanalLampEscalator1", new Vector3(50.5f, mezz + 1f, z + 10f), new Vector3(1f, 2f, 4f), new Vector3(45f, 9.18f, z + 10f), pop);
        CanalLampDies(g, "Scare_CanalLampGates", new Vector3(66.5f, mezz + 1f, z + 10.5f), new Vector3(1f, 2f, 21f), new Vector3(65f, 10.48f, z + 5f), pop);
        CanalLampDies(g, "Scare_CanalLampEscalator2", new Vector3(90.5f, 13f, z + 10f), new Vector3(1f, 2f, 4f), new Vector3(85f, 14.98f, z + 10f), pop);

        // ---- the tunnel ceiling gives way (north-east tunnel, x 40.3..95, z 300..305.5)
        BuildCeilingCollapse(g, CanalScareSpot(g, "Scare_CanalCeiling", new Vector3(62f, 0.75f, z + 2.75f), new Vector3(1f, 4.5f, 5.5f), ScareTrigger.PowerRule.Any), 67f, z + 2.6f);

        // ---- lower level: the storeroom locker bangs from inside
        var store = GameObject.Find("HidingLocker_CanalLowerStore");
        var knock = CanalScareSpot(g, "Scare_CanalLockerKnock", Cell(6, 10) + Vector3.up * 1f, new Vector3(2f, 2f, 2f), ScareTrigger.PowerRule.Any);
        knock.AddComponent<LockerKnock>().locker = store.GetComponent<HidingLocker>();

        // ---- lower level: corridor A's bulbs die toward you as you come up from the stairs
        var dark = CanalScareSpot(g, "Scare_CanalBulbsOut", Cell(10, 1) + Vector3.up * 1f, new Vector3(2f, 2f, 1f), ScareTrigger.PowerRule.Any);
        var lightsOut = dark.AddComponent<LightsOutTowards>();
        var order = new[] { "LowerBulb_A", "LowerBulb_A2", "LowerBulb_Stairs" };   // far to near
        lightsOut.lights = new Light[order.Length];
        lightsOut.bulbs = new Renderer[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            var holder = GameObject.Find(order[i]).transform;
            lightsOut.lights[i] = holder.GetComponentInChildren<Light>();
            lightsOut.bulbs[i] = holder.Find("Bulb").GetComponent<Renderer>();
        }
        lightsOut.pop = pop;
    }

    // A scare spot that follows Canal St's power, not Hollis St's.
    static GameObject CanalScareSpot(Transform parent, string name, Vector3 centre, Vector3 size, ScareTrigger.PowerRule rule)
    {
        var go = ScareSpot(parent, name, centre, size, rule);
        go.GetComponent<ScareTrigger>().grid = canalGrid;
        return go;
    }

    static void CanalLampDies(Transform parent, string name, Vector3 centre, Vector3 size, Vector3 lampPoint, AudioClip pop)
    {
        LampDies(parent, name, centre, size, lampPoint, pop);
        parent.Find(name).GetComponent<ScareTrigger>().grid = canalGrid;
    }

    // An old desk phone from simple shapes: a dark base, the handset resting on top. Rings (3D) and has its
    // own close-up 'earpiece' sound for when it's answered.
    static RingingPhone BuildPhone(Transform parent, Vector3 onCounter)
    {
        var phone = new GameObject("BoothPhone");
        phone.transform.SetParent(parent);
        phone.transform.SetPositionAndRotation(onCounter, Quaternion.Euler(0f, 160f, 0f));
        var plastic = Mat("PhonePlastic", null, new Color(0.16f, 0.15f, 0.14f), 1f, 0.45f);
        Part(new Vector3(0f, 0.04f, 0f), new Vector3(0.2f, 0.08f, 0.17f));        // base
        Part(new Vector3(0f, 0.105f, 0f), new Vector3(0.21f, 0.04f, 0.05f));      // handset grip
        Part(new Vector3(-0.085f, 0.095f, 0f), new Vector3(0.05f, 0.05f, 0.08f)); // earpiece
        Part(new Vector3(0.085f, 0.095f, 0f), new Vector3(0.05f, 0.05f, 0.08f));  // mouthpiece
        var dial = Prim(PrimitiveType.Cylinder, "Dial", phone.transform, Vector3.zero, new Vector3(0.08f, 0.004f, 0.08f), Mat("PhoneDial", null, new Color(0.7f, 0.68f, 0.62f), 1f, 0.3f));
        dial.transform.localPosition = new Vector3(0f, 0.081f, 0.055f);
        Object.DestroyImmediate(dial.GetComponent<Collider>());
        var box = phone.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.07f, 0f); box.size = new Vector3(0.3f, 0.2f, 0.25f);   // easy to look at

        var bell = phone.AddComponent<AudioSource>();
        bell.clip = Load<AudioClip>(ScareSfx + "/PhoneRing.wav");
        bell.loop = true; bell.playOnAwake = false; bell.volume = 0.9f; bell.spatialBlend = 1f;
        bell.rolloffMode = AudioRolloffMode.Linear; bell.minDistance = 2f; bell.maxDistance = 30f;
        var ear = new GameObject("Earpiece").AddComponent<AudioSource>();
        ear.transform.SetParent(phone.transform, false);
        ear.playOnAwake = false; ear.spatialBlend = 0f;
        var ringing = phone.AddComponent<RingingPhone>();
        ringing.bell = bell; ringing.earpiece = ear;
        ringing.pickUp = Load<AudioClip>(SfxDir + "/SwitchButton1_On.wav");
        ringing.breathing = Load<AudioClip>(ScareSfx + "/PhoneBreath.wav");
        ringing.hangUp = Load<AudioClip>(SfxDir + "/SwitchButton1_Off.wav");
        return ringing;

        void Part(Vector3 local, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "PhonePart";
            part.transform.SetParent(phone.transform, false);
            part.transform.localPosition = local; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = plastic;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }
    }

    // Rubble on the north track around (x, z), a dark gap in the ceiling above it, dust and a crash.
    static void BuildCeilingCollapse(Transform parent, GameObject spot, float x, float z)
    {
        var concrete = Mat("TunnelConcrete", Tex("ConcreteBare.png"), new Color(0.55f, 0.53f, 0.5f), 2f, 0.1f);
        var rubble = new GameObject("CeilingRubble").transform;
        rubble.SetParent(parent);
        var rng = new System.Random(5);
        var pieces = new System.Collections.Generic.List<Transform>();
        (Vector3 size, float dx, float dz)[] chunks =
        {
            (new Vector3(1.3f, 0.25f, 0.9f), 0f, 0f), (new Vector3(0.7f, 0.3f, 0.5f), -0.9f, 0.6f), (new Vector3(0.5f, 0.35f, 0.45f), 0.9f, -0.5f),
            (new Vector3(0.35f, 0.2f, 0.3f), -0.4f, -0.9f), (new Vector3(0.3f, 0.25f, 0.35f), 1.2f, 0.7f), (new Vector3(0.25f, 0.15f, 0.2f), -1.4f, -0.2f),
            (new Vector3(0.2f, 0.15f, 0.25f), 0.4f, 1.1f),
        };
        foreach (var c in chunks)
        {
            var piece = Box("Rubble", rubble, Vector3.zero, c.size, concrete);
            piece.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 30f - 15f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f - 15f);
            StandOn(piece, new Vector3(x + c.dx, -1.3f, z + c.dz));
            if (c.size.x > 0.6f)
            {
                var carve = piece.AddComponent<NavMeshObstacle>();
                carve.shape = NavMeshObstacleShape.Box; carve.size = c.size; carve.carving = true; carve.carveOnlyStationary = true;
            }
            else Object.DestroyImmediate(piece.GetComponent<Collider>());   // small bits: kicked about, not walls
            pieces.Add(piece.transform);
        }

        // The gap left in the ceiling (3 m up), seen from below.
        var hole = Prim(PrimitiveType.Quad, "CeilingHole", rubble, new Vector3(x, 2.995f, z), new Vector3(1.8f, 1.4f, 1f), EmissiveMat("ShaftBlack", Color.black));
        hole.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // facing down
        Object.DestroyImmediate(hole.GetComponent<Collider>());

        // Dust: a burst of grey puffs that drift down and fade.
        var dustObject = new GameObject("Dust");
        dustObject.transform.SetParent(rubble);
        dustObject.transform.position = new Vector3(x, 1.5f, z);
        var dust = dustObject.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = dust.main;
        main.playOnAwake = false; main.loop = false; main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startColor = new Color(0.45f, 0.43f, 0.4f, 0.55f);
        main.gravityModifier = 0.03f;
        var emission = dust.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50), new ParticleSystem.Burst(0.3f, 25) });
        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(2f, 3f, 2f);
        var fade = dust.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var dustMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Dust.mat");
        if (!dustMat)
        {
            dustMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(dustMat, MatDir + "/M_Dust.mat");
        }
        dustMat.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        dustMat.SetFloat("_Surface", 1f);
        dustMat.SetOverrideTag("RenderType", "Transparent");
        dustMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        dustMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        dustMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        dustMat.SetFloat("_ZWrite", 0f);
        dustMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(dustMat);
        dustObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = dustMat;

        var crash = dustObject.AddComponent<AudioSource>();
        crash.clip = Load<AudioClip>(SfxDir + "/Rockfall2.wav");
        crash.playOnAwake = false; crash.spatialBlend = 1f; crash.volume = 1f;
        crash.rolloffMode = AudioRolloffMode.Linear; crash.minDistance = 4f; crash.maxDistance = 45f;

        var collapse = spot.AddComponent<CeilingCollapse>();
        collapse.pieces = pieces.ToArray();
        collapse.hole = hole;
        collapse.ceilingY = 2.9f;
        collapse.dust = dust;
        collapse.crash = crash;
    }
}
