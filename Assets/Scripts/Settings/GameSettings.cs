using UnityEngine;

// Every player option, saved between sessions (PlayerPrefs) and readable by any script at any time.
// The options page changes them; Apply() pushes the ones that act on the whole game (volume, the PS1
// filter, vsync, fullscreen). Camera-side ones (field of view, brightness, viewmodel FOV) are applied
// by SettingsApplier; mouse ones are read by PlayerLook; the centre dot by Hud; item glow by ItemGlow.
public static class GameSettings
{
    // ---- sound (0..1)
    public static float MasterVolume { get => Get("MasterVolume", 1f); set => Set("MasterVolume", value); }
    public static float MusicVolume { get => Get("MusicVolume", 0.8f); set => Set("MusicVolume", value); }
    public static float EffectsVolume { get => Get("EffectsVolume", 1f); set => Set("EffectsVolume", value); }   // effects and ambience

    // ---- mouse
    public static float MouseSensitivity { get => Get("MouseSensitivity", 1f); set => Set("MouseSensitivity", value); }   // x the base speed (0.2..3)
    public static bool InvertY { get => GetBool("InvertY", false); set => SetBool("InvertY", value); }

    // ---- picture
    public static float Brightness { get => Get("Brightness", 0f); set => Set("Brightness", value); }       // exposure stops, -1..+1.5
    public static float PsxFilter { get => Get("PsxFilter", 0.15f); set => Set("PsxFilter", value); }        // 0..0.3 (15% default)
    public static float FieldOfView { get => Get("FieldOfView", 90f); set => Set("FieldOfView", value); }    // horizontal degrees, 75..110
    public static float ViewmodelFov { get => Get("ViewmodelFov", 60f); set => Set("ViewmodelFov", value); } // vertical degrees for held items, 45..75

    // ---- display and HUD
    public static bool Fullscreen { get => GetBool("Fullscreen", true); set => SetBool("Fullscreen", value); }
    public static bool VSync { get => GetBool("VSync", true); set => SetBool("VSync", value); }
    public static bool CentreDot { get => GetBool("CentreDot", true); set => SetBool("CentreDot", value); }
    // Faint Amnesia-style glow on pickups (fuses, batteries, flares, painkillers, keys). On by default.
    public static bool ItemGlow { get => GetBool("ItemGlow", true); set => SetBool("ItemGlow", value); }

    static readonly string[] Keys =
    {
        "MasterVolume", "MusicVolume", "EffectsVolume", "MouseSensitivity", "InvertY", "Brightness", "PsxFilter",
        "FieldOfView", "ViewmodelFov", "Fullscreen", "VSync", "CentreDot", "ItemGlow",
    };

    // Music sources ignore the listener volume (which carries Master x Effects) and use this instead.
    public static float MusicLevel => MasterVolume * MusicVolume;

    // Pushes the game-wide settings into Unity. Called when the game starts and after every change.
    public static void Apply()
    {
        AudioListener.volume = MasterVolume * EffectsVolume;
        Shader.SetGlobalFloat("_PSXStrength", PsxFilter);
        // Display options only in the built game: in the Editor they would rewrite the project's own settings.
        if (Application.isEditor) return;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStart() => Apply();

    public static void ResetToDefaults()
    {
        foreach (var key in Keys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Apply();
    }

    static float Get(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    static void Set(string key, float value) { PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); Apply(); }
    static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
    static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
}
