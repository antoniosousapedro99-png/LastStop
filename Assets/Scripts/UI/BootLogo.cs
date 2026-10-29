using UnityEngine;

// The company logo after the Unity splash: a black screen, the Witchbrew Productions logo fades in, holds,
// and fades out, then the title screen shows. Once per launch (not when going back to the title). The menus
// ignore input while it's up (MenuSystem checks Showing).
public class BootLogo : MonoBehaviour
{
    public Texture2D logo;
    public float blackBefore = 0.5f, fadeIn = 1f, hold = 2f, fadeOut = 1f, blackAfter = 0.3f;

    public static bool Showing { get; private set; }
    static bool shownThisLaunch;
    float started;

    // Domain reload is off in the Editor: start every Play run as a fresh launch.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { shownThisLaunch = false; Showing = false; }

    void Awake()
    {
        if (shownThisLaunch || !logo || SaveSystem.Pending != null) return;   // not again, nor when loading a save
        shownThisLaunch = true;
        Showing = true;
        started = Time.unscaledTime;
    }

    void Update()
    {
        if (Showing && Time.unscaledTime - started > blackBefore + fadeIn + hold + fadeOut + blackAfter) Showing = false;
    }

    void OnGUI()
    {
        if (!Showing) return;
        GUI.depth = -100;   // over everything
        var old = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        float t = Time.unscaledTime - started - blackBefore;
        float alpha = t < 0f ? 0f : t < fadeIn ? t / fadeIn : t < fadeIn + hold ? 1f : 1f - (t - fadeIn - hold) / fadeOut;
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        float h = Screen.height * 0.6f, w = h * logo.width / logo.height;
        if (w > Screen.width * 0.9f) { w = Screen.width * 0.9f; h = w * logo.height / logo.width; }
        GUI.DrawTexture(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h), logo, ScaleMode.ScaleToFit);
        GUI.color = old;
    }
}
