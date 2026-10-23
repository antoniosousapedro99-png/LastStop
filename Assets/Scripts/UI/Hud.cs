using UnityEngine;

// Minimal on-screen text: a small dot in the centre of the screen (brighter when you're looking at
// something you can use), the interaction prompt (bottom centre), short notices such as
// "Fuse (1 carried)" that fade out after a few seconds, and the text of whatever note or poster
// you're looking at (in a dark box). Placeholder look until the UI pass.
public class Hud : MonoBehaviour
{
    static string prompt;
    static string notice;
    static float noticeUntil;
    static string reading;

    public static bool Hidden;   // set while a full-screen page (the inventory) is open

    public static void SetPrompt(string text) => prompt = text;
    public static void SetReading(string text) => reading = text;

    public static void Notice(string text, float seconds = 3f)
    {
        notice = text;
        noticeUntil = Time.time + seconds;
    }

    GUIStyle style, readStyle;

    void OnGUI()
    {
        if (Hidden) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        if (readStyle == null) readStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        float scale = Screen.height / 1080f;   // same size at any resolution
        style.fontSize = Mathf.RoundToInt(26 * scale);
        readStyle.fontSize = Mathf.RoundToInt(28 * scale);

        // Centre dot: shows exactly where the 'look at' ray points.
        bool onTarget = !string.IsNullOrEmpty(prompt) || !string.IsNullOrEmpty(reading);
        float dot = Mathf.Max(2f, (onTarget ? 6f : 3f) * scale);
        var old = GUI.color;
        GUI.color = new Color(0.92f, 0.9f, 0.85f, onTarget ? 0.9f : 0.35f);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f - dot * 0.5f, Screen.height * 0.5f - dot * 0.5f, dot, dot), Texture2D.whiteTexture);
        GUI.color = old;

        // The note you're looking at: wrapped text on a dark panel in the lower middle of the screen.
        if (!string.IsNullOrEmpty(reading))
        {
            float w = Mathf.Min(Screen.width * 0.6f, 900f * scale);
            float h = readStyle.CalcHeight(new GUIContent(reading), w - 40 * scale) + 40 * scale;
            var box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f - h * 0.5f, w, h);
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = old;
            readStyle.normal.textColor = new Color(0.92f, 0.9f, 0.85f);
            GUI.Label(new Rect(box.x + 20 * scale, box.y + 20 * scale, w - 40 * scale, h - 40 * scale), reading, readStyle);
        }

        if (!string.IsNullOrEmpty(prompt))
            Shadowed(new Rect(0, Screen.height * 0.78f, Screen.width, 44 * scale), prompt, 1f);

        if (!string.IsNullOrEmpty(notice) && Time.time < noticeUntil)
        {
            float alpha = Mathf.Clamp01(noticeUntil - Time.time);   // fade during the last second
            Shadowed(new Rect(0, Screen.height * 0.84f, Screen.width, 44 * scale), notice, alpha);
        }
    }

    void Shadowed(Rect r, string text, float alpha)
    {
        style.normal.textColor = new Color(0, 0, 0, 0.8f * alpha);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = new Color(0.92f, 0.9f, 0.85f, alpha);
        GUI.Label(r, text, style);
    }
}
