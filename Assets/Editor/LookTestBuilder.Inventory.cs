using UnityEditor;
using UnityEngine;

// The inventory screen (Tab): the PSX UI pack's frame (skin 1, dark wine), plus a preview stage far
// below the level where each item's model turns slowly in front of its own small camera. That
// camera draws into a low-resolution picture (chunky, PS1-style) that the screen shows.
public static partial class LookTestBuilder
{
    const string UiDir = "Assets/Art/UI/PsxUI";
    const int PreviewLayer = 31;                                   // only the preview camera sees this layer
    static readonly Vector3 StagePosition = new Vector3(0f, -500f, 0f);

    static void BuildInventoryScreen()
    {
        var player = Object.FindAnyObjectByType<PlayerInventory>().gameObject;
        var cam = player.transform.Find("Main Camera").GetComponent<Camera>();
        cam.cullingMask &= ~(1 << PreviewLayer);

        // ---- the preview stage
        var stage = new GameObject("ItemPreviewStage").transform;
        stage.SetParent(root);
        stage.position = StagePosition;
        var turntable = new GameObject("Turntable").transform;
        turntable.SetParent(stage, false);
        PreviewModel(turntable, "Fuse", ModelPiece(BreakerFbx, "Fuse", turntable, Tex2("Props/Plumbing/FuseBox.png"), 1.6f));
        PreviewModel(turntable, "Battery", ModelPiece(SurvivalFbx, "Battery", turntable, Tex2("Props/Survival/Battery.png"), 1.5f), 0.18f);
        PreviewModel(turntable, "Flare", FlareModel(turntable, lit: false), 0.26f);   // long and thin: fit by length
        PreviewModel(turntable, "Painkillers", Model(PillBottleFbx, null, turntable, Tex2("Props/PillBottle/T_PillBottle_PSX.png"), 1f, 0f));
        PreviewModel(turntable, "Key", BuildKey(turntable), 0.24f);
        PreviewModel(turntable, "ReverserKey", ReverserKeyModel(turntable), 0.24f);

        // A warm key light from the front left and a dim red rim from behind; far from anything else.
        PreviewLight(stage, new Vector3(-0.5f, 0.4f, -0.6f), new Color(1f, 0.9f, 0.8f), 1.2f);
        PreviewLight(stage, new Vector3(0.4f, 0.2f, 0.6f), new Color(0.8f, 0.2f, 0.15f), 0.8f);

        const string rtPath = UiDir + "/ItemPreview.renderTexture";
        var picture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (picture == null)
        {
            picture = new RenderTexture(320, 185, 16) { filterMode = FilterMode.Point, name = "ItemPreview" };   // small = chunky
            AssetDatabase.CreateAsset(picture, rtPath);
        }
        var previewCam = new GameObject("PreviewCamera").AddComponent<Camera>();
        previewCam.transform.SetParent(stage, false);
        previewCam.transform.localPosition = new Vector3(0f, 0.3f, -0.52f);   // looking down ~30°, so long items show their length
        previewCam.transform.LookAt(stage.position);
        previewCam.fieldOfView = 30f;
        previewCam.nearClipPlane = 0.05f; previewCam.farClipPlane = 5f;
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = new Color(0.05f, 0.025f, 0.03f);
        previewCam.cullingMask = 1 << PreviewLayer;
        previewCam.targetTexture = picture;
        previewCam.enabled = false;                                 // only while the inventory is open
        var spinner = turntable.gameObject.AddComponent<ItemTurntable>();
        spinner.previewCamera = previewCam;

        // ---- the screen itself
        var screen = player.AddComponent<InventoryScreen>();
        screen.inventory = player.GetComponent<PlayerInventory>();
        screen.battery = player.GetComponentInChildren<FlashlightBattery>();
        screen.fuseBox = Object.FindAnyObjectByType<FuseBox>();
        screen.look = player.GetComponent<PlayerLook>();
        screen.turntable = spinner;
        screen.preview = picture;
        screen.frame = Load<Texture2D>(UiDir + "/InventoryFrame.png");
        screen.coverPanel = Load<Texture2D>(UiDir + "/TextBlock.png");
    }

    // Readies one item model for the stage: no colliders, preview layer only, 'size' metres at its longest,
    // centred on the turntable, hidden until selected.
    static void PreviewModel(Transform turntable, string kind, GameObject model, float size = 0.15f)
    {
        model.name = kind;
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer = PreviewLayer;
        model.transform.SetParent(turntable, true);
        var b = Bounds(model);
        float biggest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (biggest > 0f) model.transform.localScale *= size / biggest;
        b = Bounds(model);
        model.transform.position += turntable.position - b.center;
        model.SetActive(false);
    }

    static void PreviewLight(Transform stage, Vector3 offset, Color colour, float intensity)
    {
        var light = new GameObject("PreviewLight").AddComponent<Light>();
        light.transform.SetParent(stage, false);
        light.transform.localPosition = offset;
        light.type = LightType.Point; light.color = colour; light.intensity = intensity; light.range = 2f;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << PreviewLayer;
    }
}
