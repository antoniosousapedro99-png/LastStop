using UnityEngine;
using UnityEngine.InputSystem;

// The journal page: the Journal control (J; D-pad right on a controller) opens and closes it, and so does
// Esc / B; it can also be opened from the inventory. The game pauses while it's open, like the inventory.
// Drawn as a worn notebook page (cream paper, ruled lines, a red margin) in blue-black "ink", newest
// thought at the top; thoughts that no longer matter are greyed and struck through.
public class JournalScreen : MonoBehaviour
{
    public Journal journal;
    public PlayerLook look;
    public AudioClip pageSound;

    public static bool IsOpen { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;

    float timeScaleBefore = 1f;
    float scroll;
    int openedFrame = -1;
    Texture2D page;
    [System.NonSerialized] GUIStyle inkStyle, titleStyle, hintStyle;

    void Awake() => IsOpen = false;   // statics survive between Play runs here (no domain reload)

    void Update()
    {
        bool toggle = Controls.Pressed(GameAction.Journal);
        if (!IsOpen)
        {
            if (toggle && !MenuSystem.IsOpen && !SaveMenu.IsOpen && InventoryScreen.ClosedFrame != Time.frameCount) Open();
            return;
        }
        if (openedFrame == Time.frameCount) return;   // just opened from the inventory by the same key press
        if (toggle || Controls.UiBack()) { Close(); return; }
        var mouse = Mouse.current;
        float wheel = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (wheel < 0f) scroll += 60f;
        if (wheel > 0f) scroll = Mathf.Max(0f, scroll - 60f);
        if (Controls.UiDown()) scroll += 60f;
        if (Controls.UiUp()) scroll = Mathf.Max(0f, scroll - 60f);
    }

    // Also called by the inventory screen (it closes itself first).
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        openedFrame = Time.frameCount;
        scroll = 0f;
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (look) look.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Hud.Hidden = true;
        if (pageSound) AudioSource.PlayClipAtPoint(pageSound, transform.position, 0.6f);
    }

    void Close()
    {
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        Time.timeScale = timeScaleBefore;
        AudioListener.pause = false;
        Hud.Hidden = false;
        if (look) look.enabled = true;   // locks the cursor again
    }

    void OnGUI()
    {
        if (!IsOpen || !journal) return;
        GUI.depth = -11;
        float s = Screen.height / 1080f;
        MakeStyles(s);
        if (!page) page = MakePage();

        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        // The page: 720 x 940 (in 1080p units), a little crooked shadow behind it.
        float w = 720 * s, h = 940 * s;
        var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(r.x + 10 * s, r.y + 12 * s, w, h), Texture2D.whiteTexture);
        GUI.color = old;
        GUI.DrawTexture(r, page, ScaleMode.StretchToFill);

        float margin = 128 * s, right = 50 * s;   // clear of the red margin line
        GUI.Label(new Rect(r.x + margin, r.y + 30 * s, w - margin - right, 60 * s), "Notes", titleStyle);

        // Thoughts, newest first, under the title; scrolls if the page fills up.
        var area = new Rect(r.x + margin, r.y + 110 * s, w - margin - right, h - 170 * s);
        GUI.BeginGroup(area);
        float y = -scroll, gap = 22 * s;
        var entries = journal.entries;
        if (entries.Count == 0) GUI.Label(new Rect(0, 0, area.width, 60 * s), "Nothing yet.", inkStyle);
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            var content = new GUIContent("- " + e.text);
            float th = inkStyle.CalcHeight(content, area.width);
            inkStyle.normal.textColor = e.done ? new Color(0.35f, 0.36f, 0.42f, 0.75f) : new Color(0.1f, 0.13f, 0.32f);
            GUI.Label(new Rect(0, y, area.width, th), content, inkStyle);
            if (e.done)   // struck through: a pencil line across each written line
            {
                float lineH = inkStyle.lineHeight;
                GUI.color = new Color(0.2f, 0.22f, 0.3f, 0.8f);
                for (float ly = y + lineH * 0.55f; ly < y + th; ly += lineH)
                    GUI.DrawTexture(new Rect(0, ly, area.width * 0.96f, Mathf.Max(1f, 2f * s)), Texture2D.whiteTexture);
                GUI.color = old;
            }
            y += th + gap;
        }
        scroll = Mathf.Min(scroll, Mathf.Max(0f, y + scroll - area.height));
        GUI.EndGroup();

        string key = Controls.UsingGamepad ? "D-PAD RIGHT" : Controls.Label(GameAction.Journal);
        GUI.Label(new Rect(r.x, r.yMax - 52 * s, w - right, 40 * s), "[" + key + "] close", hintStyle);
    }

    void MakeStyles(float s)
    {
        inkStyle ??= new GUIStyle(GUI.skin.label);
        titleStyle ??= new GUIStyle(GUI.skin.label);
        hintStyle ??= new GUIStyle(GUI.skin.label);
        inkStyle.font = titleStyle.font = hintStyle.font = MenuStyle.Font;
        inkStyle.fontSize = Mathf.RoundToInt(27 * s); inkStyle.wordWrap = true; inkStyle.alignment = TextAnchor.UpperLeft;
        inkStyle.richText = false;
        titleStyle.fontSize = Mathf.RoundToInt(44 * s); titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = new Color(0.1f, 0.13f, 0.32f);
        hintStyle.fontSize = Mathf.RoundToInt(22 * s); hintStyle.alignment = TextAnchor.MiddleRight;
        hintStyle.normal.textColor = new Color(0.35f, 0.3f, 0.28f);
    }

    // A small notebook page drawn by code: cream paper with blotches, faint blue ruled lines, a red margin.
    static Texture2D MakePage()
    {
        const int w = 180, h = 235;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var rng = new System.Random(12);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = new Color(0.9f, 0.86f, 0.74f);
                if ((h - y) % 9 == 0 && h - y > 26) c = Color.Lerp(c, new Color(0.45f, 0.58f, 0.75f), 0.45f);   // ruled lines
                if (x == 26 || x == 27) c = Color.Lerp(c, new Color(0.75f, 0.25f, 0.25f), 0.55f);                // margin
                float edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 4) c *= 0.82f + edge * 0.04f;                                                        // worn edges
                c *= 0.93f + (float)rng.NextDouble() * 0.07f;                                                   // grain
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }
}
