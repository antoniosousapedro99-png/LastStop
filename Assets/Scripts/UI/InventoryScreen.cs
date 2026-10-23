using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// The inventory: Tab opens and closes it (Esc closes it too). The game pauses while it's open.
// Drawn on the PSX UI pack's frame:
// - left box: the selected item's model, slowly turning, with its name on the strip below
// - right rows: everything you carry, with counts; W/S, the arrow keys, the mouse wheel or a click selects
// - bottom right: what the selected item is and how to use it
// - bottom left (over the frame's printed menu words): torch battery and fuse box progress
public class InventoryScreen : MonoBehaviour
{
    public PlayerInventory inventory;
    public FlashlightBattery battery;
    public FuseBox fuseBox;
    public PlayerLook look;
    public ItemTurntable turntable;
    public RenderTexture preview;
    public Texture2D frame, coverPanel;

    public static bool IsOpen { get; private set; }

    struct Entry { public string kind, name, description; public int count; }
    readonly List<Entry> entries = new List<Entry>();
    int selected;
    float timeScaleBefore = 1f;
    GUIStyle rowStyle, nameStyle, textStyle, smallStyle;

    // Where things sit on the frame picture (its own pixels, 864 x 624).
    const float FrameW = 864f, FrameH = 624f;
    static readonly Rect PreviewBox = new Rect(40, 40, 302, 175);
    static readonly Rect NameStrip = new Rect(48, 216, 288, 24);
    static readonly Rect ListBox = new Rect(388, 48, 428, 340);
    static readonly Rect DescriptionBox = new Rect(396, 440, 412, 140);
    static readonly Rect StatusBox = new Rect(44, 330, 296, 255);
    const int Rows = 4;

    // Statics survive between Play runs here (no domain reload), so start every run closed.
    void Awake()
    {
        IsOpen = false;
        Hud.Hidden = false;
        AudioListener.pause = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.tabKey.wasPressedThisFrame) { if (IsOpen) Close(); else Open(); return; }
        if (!IsOpen) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }

        Refresh();
        int move = 0;
        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) move = -1;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) move = 1;
        var mouse = Mouse.current;
        if (mouse != null) { float wheel = mouse.scroll.ReadValue().y; if (wheel > 0) move = -1; else if (wheel < 0) move = 1; }
        if (entries.Count > 0) selected = Mathf.Clamp(selected + move, 0, entries.Count - 1);
        turntable.Show(entries.Count > 0 ? entries[selected].kind : null);
    }

    void Open()
    {
        IsOpen = true;
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;           // the world freezes
        AudioListener.pause = true;    // and goes quiet
        look.enabled = false;          // mouse-look off, cursor free for clicking rows
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Hud.Hidden = true;
        selected = 0;
        Refresh();
        turntable.Show(entries.Count > 0 ? entries[0].kind : null);
    }

    void Close()
    {
        IsOpen = false;
        Time.timeScale = timeScaleBefore;
        AudioListener.pause = false;
        Hud.Hidden = false;
        turntable.Show(null);
        look.enabled = true;           // locks the cursor again
    }

    // The list of what's carried right now, in a fixed order.
    void Refresh()
    {
        entries.Clear();
        Add("Fuse", "Fuse", inventory.fuses, "A glass cartridge fuse. The platform fuse box needs two before the breaker will hold.");
        Add("Battery", "Torch battery", inventory.spareBatteries, "A spare battery for your flashlight. Press H to swap it in when the light starts to die.");
        Add("Flare", "Road flare", inventory.flares, "Thirty seconds of red light. Press G to strike it; press G again to drop it, still burning.");
        Add("Painkillers", "Painkillers", inventory.painkillers, "Strong painkillers. For when something down here hurts you.");
        if (inventory.hasCabinetKey) Add("Key", "Cabinet key", 1, "A small key. Its tag reads 'PLATFORM CABINET'.");
        if (entries.Count > 0) selected = Mathf.Clamp(selected, 0, entries.Count - 1);
    }

    void Add(string kind, string name, int count, string description)
    {
        if (count > 0) entries.Add(new Entry { kind = kind, name = name, count = count, description = description });
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        GUI.depth = -10;   // on top of the normal HUD
        float scale = Screen.height / 1080f;
        MakeStyles(scale);

        // Dim the frozen game, then place the frame in the middle, as large as fits comfortably.
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
        float s = Mathf.Min(Screen.width * 0.8f / FrameW, Screen.height * 0.8f / FrameH);
        var origin = new Vector2((Screen.width - FrameW * s) * 0.5f, (Screen.height - FrameH * s) * 0.5f);
        Rect R(Rect r) => new Rect(origin.x + r.x * s, origin.y + r.y * s, r.width * s, r.height * s);
        GUI.DrawTexture(R(new Rect(0, 0, FrameW, FrameH)), frame);

        // Preview of the selected item, and its name on the light strip.
        if (entries.Count > 0)
        {
            GUI.DrawTexture(R(PreviewBox), preview, ScaleMode.ScaleAndCrop);
            nameStyle.normal.textColor = new Color(0.16f, 0.08f, 0.09f);
            GUI.Label(R(NameStrip), entries[selected].name.ToUpperInvariant(), nameStyle);
        }

        // The rows: the selected one gets a soft highlight; a click selects.
        var list = R(ListBox);
        float rowH = list.height / Rows;
        int first = Mathf.Clamp(selected - Rows + 1, 0, Mathf.Max(0, entries.Count - Rows));   // scroll to keep it visible
        if (entries.Count == 0)
            GUI.Label(new Rect(list.x, list.y, list.width, rowH), "Your pockets are empty.", rowStyle);
        for (int i = first; i < Mathf.Min(entries.Count, first + Rows); i++)
        {
            var row = new Rect(list.x, list.y + (i - first) * rowH, list.width, rowH);
            if (i == selected)
            {
                GUI.color = new Color(0.95f, 0.85f, 0.85f, 0.18f);
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = old;
            }
            rowStyle.normal.textColor = i == selected ? new Color(0.97f, 0.93f, 0.9f) : new Color(0.75f, 0.68f, 0.68f);
            GUI.Label(new Rect(row.x + 24 * scale, row.y, row.width, row.height), entries[i].name, rowStyle);
            if (entries[i].count > 1 || entries[i].kind != "Key")
            {
                rowStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(row.x, row.y, row.width - 24 * scale, row.height), "x " + entries[i].count, rowStyle);
                rowStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (GUI.Button(row, GUIContent.none, GUIStyle.none)) selected = i;
        }

        // What the selected item is.
        if (entries.Count > 0)
            GUI.Label(R(DescriptionBox), entries[selected].description, textStyle);

        // Status, drawn over the frame's printed menu words.
        var status = R(StatusBox);
        GUI.DrawTexture(status, coverPanel, ScaleMode.StretchToFill);
        float pad = 20 * scale, line = 40 * scale;
        var at = new Rect(status.x + pad, status.y + pad, status.width - pad * 2, line);
        smallStyle.normal.textColor = new Color(0.75f, 0.68f, 0.68f);
        GUI.Label(at, "TORCH BATTERY", smallStyle);
        at.y += line;
        var bar = new Rect(at.x, at.y + 6 * scale, at.width, 16 * scale);
        GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = battery.charge < battery.lowThreshold ? new Color(0.75f, 0.2f, 0.15f) : new Color(0.85f, 0.78f, 0.7f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * battery.charge, bar.height), Texture2D.whiteTexture);
        GUI.color = old;
        at.y += line;
        GUI.Label(at, "FUSE BOX   " + fuseBox.Installed + " / " + fuseBox.Required, smallStyle);
        at.y += line;
        GUI.Label(at, fuseBox.grid.IsOn ? "POWER   ON" : "POWER   OFF", smallStyle);
        at.y += line * 1.4f;
        smallStyle.normal.textColor = new Color(0.55f, 0.48f, 0.48f);
        GUI.Label(at, "[TAB] close", smallStyle);
    }

    void MakeStyles(float scale)
    {
        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            textStyle.normal.textColor = new Color(0.88f, 0.84f, 0.8f);
        }
        rowStyle.fontSize = Mathf.RoundToInt(30 * scale);
        nameStyle.fontSize = Mathf.RoundToInt(22 * scale);
        textStyle.fontSize = Mathf.RoundToInt(24 * scale);
        smallStyle.fontSize = Mathf.RoundToInt(20 * scale);
    }
}
