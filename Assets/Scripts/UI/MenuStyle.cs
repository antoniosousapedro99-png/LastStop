using UnityEngine;

// Drawing helpers shared by the menus, in the style of the FREE PSX UI pack: dark wine panels,
// ">> option" lists, white text in the VCR OSD Mono font, sliders made of a ticked line, and
// "[ ON / OFF ]" toggles where the chosen word is lit. Sizes are given for a 1080-pixel-high screen
// and scaled to the real one. The font is also used by the HUD and the inventory.
public static class MenuStyle
{
    public static Font Font;                 // set by MenuSystem from its serialized reference
    public static Texture2D Panel;           // the pack's plain wine text block

    public static readonly Color Lit = new Color(0.97f, 0.94f, 0.92f);
    public static readonly Color Dim = new Color(0.62f, 0.52f, 0.54f);
    public static readonly Color Faint = new Color(0.42f, 0.34f, 0.36f);

    static GUIStyle label;

    public static float Scale => Screen.height / 1080f;

    // A label style with the menu font at a size in 1080p pixels.
    public static GUIStyle Text(int size, TextAnchor anchor = TextAnchor.MiddleLeft, Color? colour = null)
    {
        if (label == null) label = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false };
        label.font = Font;
        label.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * Scale));
        label.alignment = anchor;
        label.wordWrap = false;
        label.normal.textColor = colour ?? Lit;
        return label;
    }

    public static void Label(Rect r, string text, int size, TextAnchor anchor = TextAnchor.MiddleLeft, Color? colour = null)
    {
        // A soft dark shadow under every line keeps it readable over the game.
        var shadow = Text(size, anchor, new Color(0f, 0f, 0f, 0.7f));
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, shadow);
        GUI.Label(r, text, Text(size, anchor, colour));
    }

    public static void Wrapped(Rect r, string text, int size, Color colour)
    {
        var s = Text(size, TextAnchor.UpperLeft, colour);
        s.wordWrap = true;
        GUI.Label(r, text, s);
    }

    public static void DrawPanel(Rect r)
    {
        if (Panel) GUI.DrawTexture(r, Panel, ScaleMode.StretchToFill);
        else Fill(r, new Color(0.12f, 0.05f, 0.06f, 0.92f));
    }

    public static void Fill(Rect r, Color colour)
    {
        var old = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // ">> option": lit and pushed right a little when selected.
    public static void Item(Rect r, string text, bool selected, bool enabled = true)
    {
        var colour = !enabled ? Faint : selected ? Lit : Dim;
        float indent = selected ? 14f * Scale : 0f;
        Label(new Rect(r.x + indent, r.y, r.width, r.height), ">> " + text, 40, TextAnchor.MiddleLeft, colour);
    }

    // The pack's slider: an end-capped line with ticks, and a block for the handle. 'value' is 0..1.
    public static void Slider(Rect r, float value, bool selected)
    {
        float s = Scale;
        var colour = selected ? Lit : Dim;
        float mid = r.y + r.height * 0.5f;
        Fill(new Rect(r.x, mid - 1.5f * s, r.width, 3f * s), colour);
        for (int i = 0; i <= 10; i++)
        {
            float h = (i == 0 || i == 10) ? 22f : (i == 5 ? 14f : 8f);
            Fill(new Rect(r.x + r.width * i / 10f - 1.5f * s, mid - h * 0.5f * s, 3f * s, h * s), colour);
        }
        float hx = r.x + r.width * Mathf.Clamp01(value);
        Fill(new Rect(hx - 7f * s, mid - 13f * s, 14f * s, 26f * s), colour);
    }

    // "[ ON / OFF ]" with the current choice lit.
    public static void Toggle(Rect r, bool on, bool selected)
    {
        var dark = selected ? Dim : Faint;
        var bright = selected ? Lit : Dim;
        string text = on ? "[ ON  /  off ]" : "[ on  /  OFF ]";
        Label(r, text, 34, TextAnchor.MiddleRight, on ? bright : dark);
    }
}
