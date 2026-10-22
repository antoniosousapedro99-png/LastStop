using UnityEngine;

// Player options that a settings menu will show later. Each is saved between sessions
// (PlayerPrefs) and can be read by any script at any time.
public static class GameSettings
{
    const string ItemGlowKey = "ItemGlow";

    // Faint Amnesia-style glow on pickups (fuses, batteries, shells, keys). On by default.
    public static bool ItemGlow
    {
        get => PlayerPrefs.GetInt(ItemGlowKey, 1) == 1;
        set { PlayerPrefs.SetInt(ItemGlowKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void ResetToDefaults() => PlayerPrefs.DeleteKey(ItemGlowKey);
}
