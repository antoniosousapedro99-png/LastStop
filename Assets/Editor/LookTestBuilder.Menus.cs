using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Menus and settings: the title camera (looking down the east tunnel at the stalled train's flickering
// cab light), the MenuSystem (title, pause, options, credits), the SettingsApplier on the player's
// camera, and the "Viewmodel" render pass that draws held items with their own field of view.
public static partial class LookTestBuilder
{
    const int ViewmodelGameLayer = 30;   // object layer for held items (the lit flare, later the arms and gun)
    const string FontPath = "Assets/Art/UI/Fonts/VcrOsdMono.ttf";

    static void BuildMenus()
    {
        var player = Object.FindAnyObjectByType<PlayerInventory>().gameObject;
        var cam = player.transform.Find("Main Camera");

        // ---- title camera
        var title = new GameObject("TitleCamera");
        title.transform.SetParent(root);
        title.transform.position = new Vector3(93f, 0.5f, 21.6f);
        title.transform.LookAt(new Vector3(99.4f, 0.7f, 25.6f));   // aimed left of the train, so it sits right of the menu
        var titleView = title.AddComponent<Camera>();
        titleView.fieldOfView = 50f;
        titleView.nearClipPlane = 0.05f; titleView.farClipPlane = 150f;
        titleView.clearFlags = CameraClearFlags.SolidColor;
        titleView.backgroundColor = Color.black;
        titleView.cullingMask &= ~(1 << PreviewLayer);
        title.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        title.AddComponent<AudioListener>();
        title.AddComponent<TitleCameraDrift>();
        title.SetActive(false);   // MenuSystem turns it on when the game starts

        // ---- the menus
        var menus = new GameObject("Menus");
        menus.transform.SetParent(root);
        menus.AddComponent<EndCard>();    // the end of Station 2, down Canal St's emergency latch
        menus.AddComponent<ScreenFade>(); // black fades (the train ride between stations)
        menus.AddComponent<SaveMenu>();   // the save / load screen (the staff PC and Load game open it)
        menus.AddComponent<BootLogo>().logo = Load<Texture2D>("Assets/Art/UI/WitchbrewLogo.png");   // Witchbrew Productions, before the title
        var system = menus.AddComponent<MenuSystem>();
        system.player = player;
        system.watermark = Load<Texture2D>("Assets/Art/UI/WitchbrewGothicLogo.png");
        system.titleCamera = title;
        system.ambience = Object.FindAnyObjectByType<AreaAmbience>();
        system.noises = Object.FindAnyObjectByType<DistantNoises>();
        system.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        system.panel = Load<Texture2D>(UiDir + "/TextBlock.png");

        // ---- settings on the player's camera, and held items on their own layer and pass
        var applier = cam.gameObject.AddComponent<SettingsApplier>();
        applier.view = cam.GetComponent<Camera>();
        applier.viewmodelPass = EnsureViewmodelPass();
        foreach (var held in new[] { cam.Find("FlareHand"), cam.Find("ShotgunRig") })
            if (held) foreach (var t in held.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ViewmodelGameLayer;
    }

    // Adds (once) a "Render Objects" pass to the PC renderer that draws only the Viewmodel layer, after
    // the solid scene, with its own field of view; the renderer's normal passes skip that layer.
    static RenderObjects EnsureViewmodelPass()
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        data.opaqueLayerMask &= ~(1 << ViewmodelGameLayer);
        data.transparentLayerMask &= ~(1 << ViewmodelGameLayer);
        EditorUtility.SetDirty(data);

        RenderObjects pass = null;
        foreach (var f in data.rendererFeatures) if (f is RenderObjects ro && ro.name == "Viewmodel") pass = ro;
        if (pass == null)
        {
            pass = ScriptableObject.CreateInstance<RenderObjects>();
            pass.name = "Viewmodel";
            AssetDatabase.AddObjectToAsset(pass, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pass, out string _, out long localId);
            var so = new SerializedObject(data);
            var list = so.FindProperty("m_RendererFeatures");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = pass;
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedProperties();
        }
        pass.settings.passTag = "Viewmodel";
        pass.settings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingOpaques;
        pass.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
        pass.settings.filterSettings.LayerMask = 1 << ViewmodelGameLayer;
        pass.settings.cameraSettings.overrideCamera = true;
        pass.settings.cameraSettings.restoreCamera = true;
        pass.settings.cameraSettings.cameraFieldOfView = 60f;
        pass.Create();
        EditorUtility.SetDirty(pass);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return pass;
    }
}
