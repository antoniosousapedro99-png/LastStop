using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Sits on the player's camera and keeps it in step with the options:
// - field of view (stored as horizontal degrees, turned into Unity's vertical FOV for the current screen shape)
// - brightness (a global exposure change on top of the level's own look)
// - viewmodel FOV: held items (a lit flare, later the arms) are drawn by their own render pass
//   ("Viewmodel", on the Viewmodel layer) with this field of view, so they keep their size and shape
//   whatever the main FOV is.
public class SettingsApplier : MonoBehaviour
{
    public Camera view;
    public RenderObjects viewmodelPass;

    ColorAdjustments exposure;
    float appliedViewmodelFov = -1f;
    const float DefaultViewmodelFov = 60f;

    void Awake()
    {
        // A global volume made at runtime (not saved anywhere), drawn on top of the level's volume.
        var go = new GameObject("BrightnessVolume");
        go.transform.SetParent(transform, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        exposure = profile.Add<ColorAdjustments>();
        exposure.postExposure.overrideState = true;
        volume.sharedProfile = profile;
    }

    void LateUpdate()
    {
        view.fieldOfView = Camera.HorizontalToVerticalFieldOfView(GameSettings.FieldOfView, view.aspect);
        exposure.postExposure.value = GameSettings.Brightness;

        float fov = GameSettings.ViewmodelFov;
        if (viewmodelPass != null && !Mathf.Approximately(fov, appliedViewmodelFov))
        {
            appliedViewmodelFov = fov;
            viewmodelPass.settings.cameraSettings.cameraFieldOfView = fov;
            viewmodelPass.Create();   // rebuild the pass with the new value
        }
    }

    // The pass lives in a project asset; put its value back so playing doesn't leave the asset changed.
    void OnDestroy()
    {
        if (viewmodelPass == null) return;
        viewmodelPass.settings.cameraSettings.cameraFieldOfView = DefaultViewmodelFov;
        viewmodelPass.Create();
    }
}
