using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

// The Pale Man (Character_Monster from Characters PSX 1.2, given a skeleton in Blender, animated with
// TheWrapper's clips through Unity's Humanoid retargeting), his walkable area, his arrival after the
// power comes back, the chase music, and the player's health.
public static partial class LookTestBuilder
{
    const string PaleManDir = ArtDir + "/Characters/PaleMan";
    const string WrapperFbx = ArtDir + "/Characters/TheWrapper/TheWrapper.fbx";
    static readonly Vector3 PaleManSpawn = new Vector3(-70f, -1.3f, 22.7f);   // on the tracks, deep in the west tunnel

    static void BuildMonster()
    {
        BakeUnderground();

        // ---- the Pale Man
        var man = new GameObject("PaleMan");
        man.transform.SetParent(root);
        man.transform.position = PaleManSpawn;
        man.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // facing the station
        var model = Model(PaleManDir + "/PaleMan.fbx", null, man.transform, Tex2("Characters/PaleMan/Character_Monster.png"), 1f, 0f);
        // Keep the file's own turn, then face him forward: Blender's "front" comes into Unity facing backwards.
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * Load<GameObject>(PaleManDir + "/PaleMan.fbx").transform.localRotation;
        float height = Bounds(model).size.y;
        model.transform.localScale *= 2.1f / height;                   // tall and thin
        StandOn(model, PaleManSpawn);
        model.transform.localPosition = new Vector3(0f, model.transform.localPosition.y, 0f);
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

        var animator = model.GetComponent<Animator>();
        if (!animator) animator = model.AddComponent<Animator>();
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(PaleManDir + "/PaleMan.fbx").OfType<Avatar>().First();
        animator.runtimeAnimatorController = PaleManController();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var capsule = man.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.05f, 0f); capsule.height = 2.1f; capsule.radius = 0.3f;

        var agent = man.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f; agent.height = 2.1f; agent.speed = 1.4f;
        agent.acceleration = 12f; agent.angularSpeed = 300f; agent.stoppingDistance = 0.5f;
        agent.autoTraverseOffMeshLink = true;

        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(man.transform, false);
        eyes.localPosition = new Vector3(0f, 1.95f, 0.1f);
        var senses = man.AddComponent<PaleManSenses>();
        senses.eyes = eyes;

        var voice = MonsterAudio(man.transform, "Voice", new Vector3(0f, 1.9f, 0f), 30f);
        voice.gameObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.SewerPipe;   // echoes down the tunnel
        var feet = MonsterAudio(man.transform, "Feet", Vector3.zero, 22f);

        var ai = man.AddComponent<PaleManAI>();
        ai.animator = animator;
        ai.voice = voice; ai.feet = feet;
        ai.breaths = Clips("HumanBreathingOut1", "HumanBreathingOut2", "HumanBreathingOut3", "HumanBreathingOut4");
        ai.shrieks = Clips("HumanInjured4", "HumanInjured5");
        ai.steps = Clips("FootstepsWetGravelStones1", "FootstepsWetGravelStones2", "FootstepsWetGravelStones3", "FootstepsWetGravelStones4");
        ai.hitSound = Load<AudioClip>(SfxDir + "/DesignedPunch1.wav");
        man.SetActive(false);   // nowhere in the level until the power comes back

        // ---- his arrival: a shriek from deep in the west tunnel, heard from far away
        var arrivalObject = new GameObject("PaleManArrival");
        arrivalObject.transform.SetParent(root);
        var shriek = MonsterAudio(arrivalObject.transform, "Shriek", Vector3.zero, 200f);
        shriek.transform.position = new Vector3(-85f, 0f, 22.7f);
        shriek.clip = Load<AudioClip>(SfxDir + "/HumanInjured4.wav");
        shriek.pitch = 0.5f; shriek.minDistance = 15f; shriek.rolloffMode = AudioRolloffMode.Logarithmic;
        shriek.gameObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.SewerPipe;
        var arrival = arrivalObject.AddComponent<PaleManArrival>();
        arrival.grid = grid;
        arrival.monster = ai;
        arrival.shriek = shriek;

        var musicSource = arrivalObject.AddComponent<AudioSource>();
        musicSource.clip = Load<AudioClip>("Assets/Art/Audio/Music/Svirls/Chase1.ogg");
        arrivalObject.AddComponent<ChaseMusic>().monster = ai;

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

    // The area the Pale Man can walk: everything solid underground (corridor, platform, tracks, tunnels,
    // the lower stairs), but not the street. The staff room door is shut when this is made, so the room
    // behind it is cut off: a safe room. Links let him climb between the platform and the tracks.
    static void BakeUnderground()
    {
        var go = new GameObject("NavMesh");
        go.transform.SetParent(root);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(7.5f, 0.25f, 13.5f);
        surface.size = new Vector3(226f, 5.5f, 37f);       // x -105..120, y -2.5..3, z -5..32
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << PreviewLayer);
        surface.BuildNavMesh();
        const string path = "Assets/Scenes/LookTest_NavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);

        foreach (float x in new[] { -10f, 0f, 10f, 16f })
        {
            var link = new GameObject("PlatformEdgeLink").AddComponent<NavMeshLink>();
            link.transform.SetParent(go.transform);
            link.transform.position = new Vector3(x, 0f, 19.6f);
            link.startPoint = new Vector3(0f, 0f, -0.6f);              // on the platform
            link.endPoint = new Vector3(0f, -1.3f, 1.2f);              // down on the track bed
            link.width = 1.2f;
            link.bidirectional = true;
        }
    }

    // Idle, Walk, Run, Attack and Scream, all from TheWrapper's animations; PaleManAI cross-fades between them.
    static AnimatorController PaleManController()
    {
        string path = PaleManDir + "/PaleMan.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var clips = AssetDatabase.LoadAllAssetsAtPath(WrapperFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
        var machine = controller.layers[0].stateMachine;
        foreach (var (state, clipName) in new[] { ("Idle", "Idle_Watchful"), ("Walk", "Walk_Nervous"), ("Run", "Run_Frantic"),
                                                  ("Attack", "Attack_Lunge"), ("Scream", "Scream") })
        {
            var s = machine.AddState(state);
            s.motion = clips.First(c => c.name.Contains(clipName));
            if (state == "Idle") machine.defaultState = s;
        }
        return controller;
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
