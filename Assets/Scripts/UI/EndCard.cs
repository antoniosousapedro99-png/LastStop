using System.Globalization;
using UnityEngine;

// The end of the game so far: the screen fades to black, then shows "END OF STATION 2 / CANAL ST JUNCTION"
// (set by the builder), the time played and "TO BE CONTINUED", and goes back to the title.
public class EndCard : MonoBehaviour
{
    public float fadeSeconds = 2f, holdSeconds = 7f;
    public string title = "END OF STATION 2", subtitle = "CANAL ST JUNCTION";

    public bool Showing { get; private set; }
    float shownAt;
    string time;

    public void Show()
    {
        if (Showing) return;
        Showing = true;
        shownAt = Time.unscaledTime;
        int seconds = Mathf.FloorToInt(SaveSystem.PlaySeconds);
        time = (seconds / 3600 > 0 ? (seconds / 3600) + ":" + (seconds / 60 % 60).ToString("00", CultureInfo.InvariantCulture) : (seconds / 60).ToString(CultureInfo.InvariantCulture))
               + ":" + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
    }

    void Update()
    {
        if (!Showing) return;
        if (Time.unscaledTime - shownAt > fadeSeconds + holdSeconds) MenuSystem.BackToTitle();
    }

    void OnGUI()
    {
        if (!Showing) return;
        GUI.depth = -30;   // above everything
        float t = Time.unscaledTime - shownAt;
        float black = Mathf.Clamp01(t / fadeSeconds);
        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, black));
        float text = Mathf.Clamp01((t - fadeSeconds - 0.5f) / 1.5f);   // the words fade in after the black
        if (text <= 0f) return;
        Hud.Hidden = true;
        float s = MenuStyle.Scale, w = Screen.width;
        MenuStyle.Label(new Rect(0, 360 * s, w, 110 * s), title, 90, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, text));
        MenuStyle.Label(new Rect(0, 470 * s, w, 50 * s), subtitle, 40, TextAnchor.MiddleCenter, new Color(MenuStyle.Dim.r, MenuStyle.Dim.g, MenuStyle.Dim.b, text));
        MenuStyle.Label(new Rect(0, 580 * s, w, 44 * s), "TIME   " + time, 32, TextAnchor.MiddleCenter, new Color(MenuStyle.Dim.r, MenuStyle.Dim.g, MenuStyle.Dim.b, text));
        MenuStyle.Label(new Rect(0, 760 * s, w, 44 * s), "TO BE CONTINUED", 30, TextAnchor.MiddleCenter, new Color(MenuStyle.Faint.r, MenuStyle.Faint.g, MenuStyle.Faint.b, text));
    }
}
