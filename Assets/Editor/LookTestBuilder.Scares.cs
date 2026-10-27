using System.Linq;
using UnityEditor;
using UnityEngine;

// The atmosphere pass: one-time scares at set spots (ScareTrigger + an IScare each).
// - Before the power: a train rumbles through the west tunnel. (The PA announcement is switched off until
//   the creative director records a proper one: PaAnnouncement and its clip are kept for then.)
// - After the power: lamps pop and die behind you (corridor, platform, east tunnel); a black figure
//   stands far down the east tunnel and vanishes when you look; another rumble near the east train.
public static partial class LookTestBuilder
{
    const string ScareSfx = SfxDir + "/Scares";

    static void BuildScares()
    {
        var group = Group("Scares");
        var pop = Load<AudioClip>(ScareSfx + "/LampPop.wav");

        // ---- the PA speakers on the platform's back wall (silent for now: no announcement until a proper
        //      one is recorded; to bring it back, add a ScareSpot with a PaAnnouncement using these speakers)
        Speaker(group, new Vector3(-6f, 3.6f, 14.13f));
        Speaker(group, new Vector3(6f, 3.6f, 14.13f));

        // ---- train rumbles: the west tunnel before the power, near the east train after it
        Rumble(group, "Scare_RumbleWest", -24f, ScareTrigger.PowerRule.BeforePower);
        Rumble(group, "Scare_RumbleEast", 75f, ScareTrigger.PowerRule.AfterPower);

        // ---- lamps dying behind you, after the power (each trigger sits just past its lamp)
        LampDies(group, "Scare_LampCorridor", new Vector3(0f, 1.5f, 9f), new Vector3(4f, 3f, 1f), new Vector3(0f, 2.98f, 12f), pop);
        LampDies(group, "Scare_LampPlatform", new Vector3(9.5f, 1.5f, 17f), new Vector3(1f, 3f, 6f), new Vector3(6f, 4.48f, 18.6f), pop);
        LampDies(group, "Scare_LampTunnel", new Vector3(52f, 0.75f, 22.75f), new Vector3(1f, 4.5f, 5.5f), new Vector3(48f, 1.4f, 20f), pop);

        // ---- the shadow figure, after the power: on the tracks just before the staff room door
        var spot = ScareSpot(group, "Scare_ShadowFigure", new Vector3(30f, 0.75f, 22.75f), new Vector3(1f, 4.5f, 5.5f), ScareTrigger.PowerRule.AfterPower);
        var shadow = spot.AddComponent<ShadowFigure>();
        shadow.figure = ShadowFigureModel(group, new Vector3(41f, -1.3f, 22.4f));   // about 11 m ahead: any further and the fog hides him
        shadow.tooClose = 6f;
        shadow.seeWithoutTorch = 8f;                                               // he goes when the torch finds him (or you get close)
        // A faint sickly glow on the floor and walls just behind him, so he stands out as a silhouette.
        // It belongs to him, so it dies the moment he vanishes.
        var backlight = new GameObject("Backlight").AddComponent<Light>();
        backlight.transform.SetParent(shadow.figure.transform, false);
        backlight.transform.localPosition = new Vector3(0f, 1.2f, -2.5f);   // behind him (he turns to face the player)
        backlight.type = LightType.Point; backlight.color = new Color(0.7f, 0.85f, 0.7f);
        backlight.intensity = 6f; backlight.range = 5f; backlight.shadows = LightShadows.None;
        shadow.vanishSound = Load<AudioClip>(ScareSfx + "/ShadowWhisper.wav");
    }

    // An invisible one-time trigger box on the Ignore Raycast layer.
    static GameObject ScareSpot(Transform parent, string name, Vector3 centre, Vector3 size, ScareTrigger.PowerRule rule)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = centre;
        go.layer = 2;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var trigger = go.AddComponent<ScareTrigger>();
        trigger.power = rule;
        trigger.grid = grid;
        return go;
    }

    // A small grey PA speaker box high on the platform's back wall, with its own sound source.
    static AudioSource Speaker(Transform parent, Vector3 wallPoint)
    {
        var box = Prim(PrimitiveType.Cube, "PaSpeaker", parent, wallPoint + new Vector3(0f, 0f, 0.11f), new Vector3(0.4f, 0.28f, 0.2f),
                       Mat("SpeakerGrey", null, new Color(0.35f, 0.36f, 0.37f), 1f, 0.3f, 0.2f));
        Object.DestroyImmediate(box.GetComponent<Collider>());
        var source = box.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0.8f;
        source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 4f; source.maxDistance = 45f;
        box.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Hangar;   // echoing round the hall
        return source;
    }

    static void Rumble(Transform parent, string name, float x, ScareTrigger.PowerRule rule)
    {
        var go = ScareSpot(parent, name, new Vector3(x, 0.75f, 22.75f), new Vector3(1f, 4.5f, 5.5f), rule);
        var rumble = go.AddComponent<TrainRumble>();
        rumble.ceilingY = TunnelTop - 0.1f;
        var source = go.AddComponent<AudioSource>();
        source.clip = Load<AudioClip>(ScareSfx + "/TrainRumble.wav");
        source.playOnAwake = false; source.spatialBlend = 0f;   // all around you, like the ground itself
        source.volume = 0.9f;
        rumble.rumble = source;

        // Dust trickling down from the ceiling in a patch around the player.
        var dust = new GameObject("CeilingDust");
        dust.transform.SetParent(go.transform, false);
        var ps = dust.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = false; main.duration = 3.5f;
        main.startLifetime = 2.2f; main.startSpeed = 0.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.gravityModifier = 0.25f; main.maxParticles = 600;
        main.startColor = new Color(0.55f, 0.52f, 0.48f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = 140f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(5f, 0.1f, 4.5f);
        var renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = DustMat();
        rumble.dust = ps;
    }

    // Faint grey specks that show a little even in the dark (unlit, see-through).
    static Material DustMat()
    {
        const string path = "Assets/Art/Materials/LookTest/M_CeilingDust.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", new Color(0.42f, 0.4f, 0.37f, 0.55f));
        m.SetFloat("_Surface", 1f);                                    // transparent
        m.SetFloat("_Blend", 0f);                                      // alpha blending
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(m);
        return m;
    }

    static void LampDies(Transform parent, string name, Vector3 centre, Vector3 size, Vector3 lampPoint, AudioClip pop)
    {
        var go = ScareSpot(parent, name, centre, size, ScareTrigger.PowerRule.AfterPower);
        var cut = go.AddComponent<LampCutOut>();
        cut.lamp = Object.FindObjectsByType<PoweredLight>().OrderBy(l => Vector3.Distance(l.transform.position, lampPoint)).First();
        cut.pop = pop;
    }

    // Frankie's silhouette, near black, frozen in his idle pose (no Animator, so the pose stays).
    static GameObject ShadowFigureModel(Transform parent, Vector3 floorPoint)
    {
        var holder = new GameObject("ShadowFigure");
        holder.transform.SetParent(parent);
        holder.transform.position = floorPoint;
        var model = Model(FrankieDir + "/Frankie.fbx", null, holder.transform, null, 1f, 0f, FrankieTextures);
        model.transform.localRotation = Load<GameObject>(FrankieDir + "/Frankie.fbx").transform.localRotation;
        model.transform.localScale *= 2.1f / Bounds(model).size.y;
        model.transform.localPosition = Vector3.zero;
        float soleBelowBones = LowestFootBone(model) - Bounds(model).min.y;
        AssetDatabase.LoadAllAssetsAtPath(FrankieDir + "/Frankie.fbx").OfType<AnimationClip>().First(c => c.name.EndsWith("Idle")).SampleAnimation(model, 0.6f);
        model.transform.localPosition = new Vector3(0f, floorPoint.y - (LowestFootBone(model) - soleBelowBones), 0f);
        var black = Mat("ShadowBlack", null, new Color(0.035f, 0.035f, 0.035f), 1f, 0f);   // near black: the lamp just catches his shape
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            r.sharedMaterials = Enumerable.Repeat(black, r.sharedMaterials.Length).ToArray();
            r.updateWhenOffscreen = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var animator = model.GetComponent<Animator>();
        if (animator) Object.DestroyImmediate(animator);
        return holder;
    }
}
