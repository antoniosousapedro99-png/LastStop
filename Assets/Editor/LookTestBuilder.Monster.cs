using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

// Frankie, the monster: since 2026-10-28 he has the Ridgeback's body (the worker who feared death, pale
// skin) and its own idle, walk, chase, notice and attack; his AI, senses and sounds are unchanged. Also his
// walkable area, his arrival, the chase music, and the player's health.
// (The old Big Frankie model is still in Characters/Frankie, used by nothing but ShadowFigureModel.)
public static partial class LookTestBuilder
{
    const string FrankieDir = ArtDir + "/Characters/Frankie";
    const string MonsterFbx = ArtDir + "/Characters/Ridgeback/Ridgeback.fbx";
    static readonly System.Collections.Generic.Dictionary<string, string> MonsterTextures = new System.Collections.Generic.Dictionary<string, string>
    {
        { "PSX_ManThing", "Characters/Ridgeback/ManThing_pale.png" },
    };
    const float MonsterHeight = 2f;
    // His animation clips (by the end of their names) for each state the AI plays.
    static readonly System.Collections.Generic.Dictionary<string, string> MonsterClips = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Idle", "_IDLE" }, { "Walk", "_WALK" }, { "Run", "_CHASE" }, { "Attack", "_ATTACK" }, { "Scream", "_NOTICE" },
    };
    // His three textures, by material name. Every copy of his model must pass these (the materials are
    // shared files, so a copy made without them would wipe them for him too).
    static readonly System.Collections.Generic.Dictionary<string, string> FrankieTextures = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Frankie_Body", "Characters/Frankie/Frankie_Body.png" }, { "Frankie_Bones", "Characters/Frankie/Frankie_Bones.png" },
        { "Frankie_Face", "Characters/Frankie/Frankie_Face.png" },
    };
    static readonly Vector3 FrankieSpawn = new Vector3(-70f, -1.3f, 22.7f);   // on the tracks, deep in the west tunnel

    static void BuildMonster()
    {
        BakeUnderground();

        // ---- Frankie
        var man = new GameObject("Frankie");
        man.transform.SetParent(root);
        man.transform.position = FrankieSpawn;
        man.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // facing the station
        var model = Model(MonsterFbx, null, man.transform, null, 1f, 0f, MonsterTextures);
        model.transform.localRotation = Load<GameObject>(MonsterFbx).transform.localRotation;   // the file already faces forward
        model.transform.localPosition = Vector3.zero;
        var idle = AssetDatabase.LoadAllAssetsAtPath(MonsterFbx).OfType<AnimationClip>().First(c => c.name.EndsWith(MonsterClips["Idle"]));
        // 2 m tall standing in his idle pose, his soles on the floor (measured on the posed body itself).
        idle.SampleAnimation(model, 0f);
        var (low, high) = PosedHeights(model);
        model.transform.localScale *= MonsterHeight / (high - low);
        idle.SampleAnimation(model, 0f);
        model.transform.localPosition = new Vector3(0f, man.transform.position.y - PosedHeights(model).low, 0f);
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

        var animator = model.GetComponent<Animator>();
        if (!animator) animator = model.AddComponent<Animator>();
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(MonsterFbx).OfType<Avatar>().First();
        animator.runtimeAnimatorController = FrankieController();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var capsule = man.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, MonsterHeight * 0.5f, 0f); capsule.height = MonsterHeight; capsule.radius = 0.3f;

        var agent = man.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f; agent.height = MonsterHeight; agent.speed = 0.9f;
        agent.acceleration = 10f; agent.angularSpeed = 180f; agent.stoppingDistance = 0.5f;   // 180: turns smoothly, not snapping round
        agent.autoTraverseOffMeshLink = false;   // FrankieAI leaps across links itself, in an arc
        // The walkable area floats a few centimetres above the real floor; lower him by that gap so his feet touch it.
        if (NavMesh.SamplePosition(FrankieSpawn, out var navHit, 1f, NavMesh.AllAreas)) agent.baseOffset = FrankieSpawn.y - navHit.position.y;

        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(man.transform, false);
        eyes.localPosition = new Vector3(0f, MonsterHeight - 0.15f, 0.1f);
        var senses = man.AddComponent<FrankieSenses>();
        senses.eyes = eyes;

        var voice = MonsterAudio(man.transform, "Voice", new Vector3(0f, 1.9f, 0f), 30f);
        voice.gameObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.SewerPipe;   // echoes down the tunnel
        var feet = MonsterAudio(man.transform, "Feet", Vector3.zero, 22f);

        var ai = man.AddComponent<FrankieAI>();
        ai.animator = animator;
        // The Ridgeback's clips, measured in the Editor: his planted foot slides back about 0.4 m/s in the walk and 1.15 m/s
        // in the chase, and the claws reach furthest 0.6 s into the attack. The AI speeds the clips up to match
        // how fast he really moves (the chase plays up to 3x, a frantic scuttle). Footfalls: where each foot lands.
        ai.clipWalkSpeed = 0.4f; ai.clipRunSpeed = 1.15f; ai.clipHitAt = 0.6f;
        ai.walkFootfalls = new[] { 0.02f, 0.52f }; ai.runFootfalls = new[] { 0.43f, 0.98f };
        ai.maxWalkPlayback = 2.3f; ai.maxRunPlayback = 3f;
        // Where he slips away to when the player stays out of his reach (on the street) for 20 s: deep in the
        // tunnels and lower levels of both stations (the furthest one from the player is used).
        ai.hideaways = new[]
        {
            FrankieSpawn, new Vector3(75f, -1.3f, 22.75f), new Vector3(-56f, -9.3f, 47f),
            new Vector3(-110f, -1.3f, 302.75f), new Vector3(75f, -1.3f, 318.25f), new Vector3(-46f, -9.3f, 346f),
        };
        ai.voice = voice; ai.feet = feet;
        // His own sounds, made from the Darkworld pack with ffmpeg (see Assets/Art/CREDITS.md).
        ai.breaths = Clips("Frankie/FrankieBreath1", "Frankie/FrankieBreath2", "Frankie/FrankieBreath3");
        // His scream when he spots you (the strongest 3 s of each close scream).
        ai.shrieks = new[] { Load<AudioClip>(CharacterSfx + "/FrankieScreamClose1.ogg"), Load<AudioClip>(CharacterSfx + "/FrankieScreamClose2.ogg") };
        ai.steps = Clips("Frankie/FrankieStep1", "Frankie/FrankieStep2", "Frankie/FrankieStep3", "Frankie/FrankieStep4");
        ai.cracks = Clips("Frankie/FrankieCrack1", "Frankie/FrankieCrack2", "Frankie/FrankieCrack3");
        ai.hitSound = Load<AudioClip>(SfxDir + "/DesignedPunch1.wav");
        man.SetActive(false);   // nowhere in the level until he wakes (see FrankieArrival)

        // ---- his arrival: a shriek from deep in the west tunnel, heard from far away
        var arrivalObject = new GameObject("FrankieArrival");
        arrivalObject.transform.SetParent(root);
        var shriek = MonsterAudio(arrivalObject.transform, "Shriek", Vector3.zero, 200f);
        shriek.transform.position = new Vector3(-85f, 0f, 22.7f);
        shriek.clip = Load<AudioClip>(CharacterSfx + "/FrankieScreamFar.ogg");   // already echoing, so no reverb on top
        shriek.minDistance = 15f; shriek.rolloffMode = AudioRolloffMode.Logarithmic;
        var arrival = arrivalObject.AddComponent<FrankieArrival>();
        arrival.grid = grid;
        arrivalObject.AddComponent<AutosaveOnPower>().grid = grid;   // the autosave, a few seconds after the power returns
        arrival.monster = ai;
        arrival.shriek = shriek;
        // He first appears under the lamp at the far end of the platform, then walks into that end's tunnel.
        arrival.revealSpots = new[] { new Vector3(-12f, 0f, 17.6f), new Vector3(12f, 0f, 17.6f) };
        arrival.walkOffSpots = new[] { new Vector3(-38f, -1.3f, 22.7f), new Vector3(38f, -1.3f, 22.7f) };
        // Normally he wakes 7 s after the player first reaches the bottom of the service stairs, deep in the
        // west tunnel above (where the shriek comes from), and comes searching.
        arrival.belowDelay = 7f;
        arrival.belowAppearSpot = FrankieSpawn;
        var below = new GameObject("ServiceStairsBottom");
        below.transform.SetParent(arrivalObject.transform);
        below.transform.position = new Vector3(StairCentre.x, SvcFloor + 1f, StairCentre.z);
        below.layer = 2;
        var belowBox = below.AddComponent<BoxCollider>();
        belowBox.isTrigger = true; belowBox.size = new Vector3(4.6f, 2f, 4.6f);
        below.AddComponent<BelowTrigger>().arrival = arrival;

        // The sting when the player first sees him: a music cue, so it follows the Music volume.
        var sting = arrivalObject.AddComponent<AudioSource>();
        sting.clip = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Jumpscare.ogg");
        sting.playOnAwake = false; sting.spatialBlend = 0f; sting.ignoreListenerVolume = true;
        ai.sting = sting;

        var musicSource = arrivalObject.AddComponent<AudioSource>();
        musicSource.clip = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Chase1.ogg");
        musicSource.playOnAwake = false;
        var chaseMusic = arrivalObject.AddComponent<ChaseMusic>();
        chaseMusic.monster = ai;
        chaseMusic.music = musicSource;

        // ---- the player can be hurt, and painkillers heal
        var player = Object.FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include).gameObject;
        var health = player.AddComponent<PlayerHealth>();
        var playerVoice = player.AddComponent<AudioSource>();
        playerVoice.playOnAwake = false; playerVoice.spatialBlend = 0f;
        health.voice = playerVoice;
        health.painSounds = Clips("HumanInjured1", "HumanInjured2", "HumanInjured3");
        health.hurtBreathing = Load<AudioClip>(SfxDir + "/HumanExhausted1.wav");
        var screen = player.GetComponent<InventoryScreen>();
        screen.health = health;
        screen.pillSound = Load<AudioClip>(SfxDir + "/PillsBox1.wav");
    }

    // The area Frankie can walk: everything solid underground (corridor, platform, tracks, tunnels,
    // the lower stairs, the service level), but not the street. The staff room door is shut when this is
    // made, so the room behind it is cut off: a safe room. Links let him climb between the platform and
    // the tracks; walking routes (LookTestBuilder.Service.cs) take him through the maintenance door and
    // down the service stairs.
    static void BakeUnderground()
    {
        var go = new GameObject("NavMesh");
        go.transform.SetParent(root);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(7.5f, -3.5f, 25.5f);
        surface.size = new Vector3(226f, 13f, 61f);        // x -105..120, y -10..3, z -5..56
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << PreviewLayer);
        surface.BuildNavMesh();
        const string path = "Assets/Scenes/LookTest_NavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);

        EdgeLinks(go.transform, "PlatformEdgeLink", -18f, 18f, 19.6f, 1);
    }

    // Jump links side by side along a whole platform edge (x 'fromX'..'toX'), so Frankie can drop onto
    // the track bed or climb back up anywhere. 'atZ' is just inside the edge; 'side' is +1 if the track
    // bed is toward +z, -1 if toward -z. He lands 'reach' metres out, 'drop' metres below the platform.
    static void EdgeLinks(Transform parent, string name, float fromX, float toX, float atZ, int side, float reach = 1.2f, float drop = 1.3f)
    {
        const float width = 4f;
        for (float x = fromX + width / 2f; x <= toX - width / 2f + 0.01f; x += width)
        {
            var link = new GameObject(name).AddComponent<NavMeshLink>();
            link.transform.SetParent(parent);
            link.transform.position = new Vector3(x, 0f, atZ);
            link.startPoint = new Vector3(0f, 0f, -side * 0.9f);       // on the platform, safely inside its walkable area
            link.endPoint = new Vector3(0f, -drop, side * reach);      // down on the track bed
            link.width = width;
            link.bidirectional = true;
        }
    }

    // Idle, Walk, Run, Attack and Scream, the monster's own animations; FrankieAI cross-fades between them.
    static AnimatorController FrankieController()
    {
        string path = "Assets/Art/Models/Characters/Ridgeback/Monster.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var clips = AssetDatabase.LoadAllAssetsAtPath(MonsterFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
        var machine = controller.layers[0].stateMachine;
        foreach (var state in new[] { "Idle", "Walk", "Run", "Attack", "Scream" })
        {
            var s = machine.AddState(state);
            s.motion = clips.First(c => c.name.EndsWith(MonsterClips[state]));
            if (state == "Idle") machine.defaultState = s;
        }
        return controller;
    }

    // The lowest and highest points of a model's skinned body in its current pose, in world space. (A skinned
    // renderer's bounds don't follow a pose sampled in the Editor, so the posed mesh is baked and measured.)
    static (float low, float high) PosedHeights(GameObject model)
    {
        float low = float.MaxValue, high = float.MinValue;
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh = new Mesh();
            r.BakeMesh(mesh, true);
            foreach (var v in mesh.vertices) { float y = r.transform.TransformPoint(v).y; low = Mathf.Min(low, y); high = Mathf.Max(high, y); }
            Object.DestroyImmediate(mesh);
        }
        return (low, high);
    }

    // The height of the lowest foot bone (foot / foot2 on either side) in the model's current pose.
    static float LowestFootBone(GameObject model)
    {
        float lowest = float.MaxValue;
        foreach (var t in model.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("foot")) lowest = Mathf.Min(lowest, t.position.y);
        return lowest;
    }

    static AudioSource MonsterAudio(Transform parent, string name, Vector3 localPos, float maxDistance)
    {
        var source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(parent, false);
        source.transform.localPosition = localPos;
        source.playOnAwake = false; source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2f; source.maxDistance = maxDistance;
        return source;
    }

    static AudioClip[] Clips(params string[] names) => names.Select(n => Load<AudioClip>(SfxDir + "/" + n + ".wav")).ToArray();
}
