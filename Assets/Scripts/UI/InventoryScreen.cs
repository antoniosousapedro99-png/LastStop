using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// The inventory: the Inventory control (Tab; View on a controller) opens and closes it, and Esc / B
// closes it too. The game pauses while it's open. Drawn on the PSX UI pack's frame:
// - left box: the selected item's model, slowly turning, with its name on the strip below
// - right rows: everything you carry, with counts; W/S, arrows, d-pad, the mouse wheel or a click selects
// - bottom right: what the selected item is and how to use it
// - bottom left (over the frame's printed menu words): torch battery and fuse box progress
public class InventoryScreen : MonoBehaviour
{
    public PlayerInventory inventory;
    public FlashlightBattery battery;
    public FuseBox fuseBox;
    public PlayerLook look;
    public PlayerHealth health;
    public AudioClip pillSound;
    public ItemTurntable turntable;
    public RenderTexture preview;
    public Texture2D frame, coverPanel;

    public static bool IsOpen { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;   // so the same Esc press doesn't also open the pause menu

    struct Entry { public string kind, name, description; public int count; }
    readonly List<Entry> entries = new List<Entry>();
    int selected;
    string message;                   // a reply under the description, e.g. after taking painkillers
    float timeScaleBefore = 1f;
    [System.NonSerialized] GUIStyle rowStyle, nameStyle, textStyle, smallStyle;

    // Where things sit on the frame picture (its own pixels, 864 x 624).
    const float FrameW = 864f, FrameH = 624f;
    static readonly Rect PreviewBox = new Rect(40, 40, 302, 175);
    static readonly Rect NameStrip = new Rect(48, 216, 288, 24);
    static readonly Rect ListBox = new Rect(388, 48, 428, 340);
    static readonly Rect ListCover = new Rect(380, 42, 442, 352);   // over the frame's printed item boxes
    static readonly Rect DescriptionBox = new Rect(396, 440, 412, 140);
    static readonly Rect StatusBox = new Rect(44, 330, 296, 255);
    const int Rows = 7;                // items shown at once; more scroll

    // Statics survive between Play runs here (no domain reload), so start every run closed.
    void Awake()
    {
        IsOpen = false;
        Hud.Hidden = false;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Controls.Pressed(GameAction.Inventory) && !MenuSystem.IsOpen && !SaveMenu.IsOpen) { if (IsOpen) Close(); else Open(); return; }
        if (!IsOpen) return;
        if (Controls.UiBack()) { Close(); return; }

        Refresh();
        int move = 0;
        if (Controls.UiUp()) move = -1;
        if (Controls.UiDown()) move = 1;
        var mouse = Mouse.current;
        if (mouse != null) { float wheel = mouse.scroll.ReadValue().y; if (wheel > 0) move = -1; else if (wheel < 0) move = 1; }
        if (entries.Count > 0) selected = Mathf.Clamp(selected + move, 0, entries.Count - 1);
        if (Controls.UiAccept() && entries.Count > 0 && entries[selected].kind == "Painkillers") TakePainkillers();
        turntable.Show(entries.Count > 0 ? entries[selected].kind : null);
    }

    // Painkillers heal one hit; they're kept if you aren't hurt.
    void TakePainkillers()
    {
        if (!health || !health.Heal()) { message = "You're not hurt."; return; }
        inventory.painkillers--;
        if (pillSound) AudioSource.PlayClipAtPoint(pillSound, transform.position, 0.8f);
        message = "You swallow two pills. The pain dulls.";
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
        message = null;
        Refresh();
        turntable.Show(entries.Count > 0 ? entries[0].kind : null);
    }

    void Close()
    {
        IsOpen = false;
        ClosedFrame = Time.frameCount;
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
        Add("Photo", "Family photo", 1, "Family photo. Sunday lunch, years ago. I don't remember the last time we sat like this.");
        Add("Fuse", "Fuse", inventory.fuses, "A glass cartridge fuse. The platform fuse box needs two before the breaker will hold.");
        string battery = Controls.Label(GameAction.SwapBattery), flare = Controls.Label(GameAction.Flare);
        Add("Battery", "Torch battery", inventory.spareBatteries, "A spare battery for your flashlight. Press " + battery + " to swap it in when the light starts to die.");
        Add("Flare", "Road flare", inventory.flares, "Thirty seconds of red light. Press " + flare + " to strike it; press " + flare + " again to drop it, still burning. Whatever is down here keeps away from it.");
        Add("Painkillers", "Painkillers", inventory.painkillers, "Strong painkillers. Press " + (Controls.UsingGamepad ? "A" : "ENTER") + " to take them after you've been hurt.");
        if (inventory.hasCabinetKey) Add("Key", "Cabinet key", 1, "A small key. Its tag reads 'PLATFORM CABINET'.");
        if (inventory.hasReverserKey) Add("ReverserKey", "Reverser key", 1, "A train driver's key. It slots into a cab's controls and wakes the train.");
        Add("FuelCan", "Fuel can", inventory.fuelCans, "A can of petrol. The backup generator runs dry without it.");
        if (inventory.hasSparkPlug) Add("SparkPlug", "Spark plug", 1, "A new spark plug, still in its wrapper. The generator won't fire without one.");
        if (inventory.hasOfficeKey) Add("OfficeKey", "Office key", 1, "A small key. Its tag reads 'STATION MASTER'.");
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

        // The frame picture has four tall item boxes printed in it: cover them, then draw one slim box per
        // row in the same wine style (seven fit), empty ones dim, the selected one lit. A click selects.
        GUI.color = new Color(0.06f, 0.03f, 0.04f, 1f);
        GUI.DrawTexture(R(ListCover), Texture2D.whiteTexture);
        GUI.color = old;
        var list = R(ListBox);
        float rowH = list.height / Rows;
        int first = Mathf.Clamp(selected - Rows + 1, 0, Mathf.Max(0, entries.Count - Rows));   // scroll to keep it visible
        for (int slot = 0; slot < Rows; slot++)
        {
            int i = first + slot;
            var box = new Rect(list.x, list.y + slot * rowH + rowH * 0.09f, list.width, rowH * 0.82f);
            GUI.color = i == selected ? new Color(1f, 0.92f, 0.92f) : i < entries.Count ? new Color(0.7f, 0.6f, 0.62f) : new Color(0.38f, 0.32f, 0.34f);
            GUI.DrawTexture(box, coverPanel, ScaleMode.StretchToFill);
            if (i == selected)
            {
                GUI.color = new Color(1f, 0.88f, 0.88f, 0.14f);   // a soft glow over the chosen row
                GUI.DrawTexture(box, Texture2D.whiteTexture);
            }
            GUI.color = old;
        }
        if (entries.Count == 0)
            GUI.Label(new Rect(list.x + 20 * scale, list.y, list.width, rowH), "Your pockets are empty.", rowStyle);
        for (int i = first; i < Mathf.Min(entries.Count, first + Rows); i++)
        {
            var row = new Rect(list.x, list.y + (i - first) * rowH, list.width, rowH);
            rowStyle.normal.textColor = i == selected ? new Color(0.97f, 0.93f, 0.9f) : new Color(0.75f, 0.68f, 0.68f);
            GUI.Label(new Rect(row.x + 20 * scale, row.y, row.width, row.height), entries[i].name, rowStyle);
            if (entries[i].count > 1 || (entries[i].kind != "Key" && entries[i].kind != "ReverserKey" && entries[i].kind != "SparkPlug" && entries[i].kind != "OfficeKey"))
            {
                rowStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(row.x, row.y, row.width - 20 * scale, row.height), "x " + entries[i].count, rowStyle);
                rowStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (GUI.Button(row, GUIContent.none, GUIStyle.none)) selected = i;
        }

        // What the selected item is.
        if (entries.Count > 0)
            GUI.Label(R(DescriptionBox), entries[selected].description + (message != null ? "\n\n" + message : ""), textStyle);

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
        at.y += line;
        if (health) GUI.Label(at, health.Hurt ? "CONDITION   HURT" : "CONDITION   FINE", smallStyle);
        at.y += line * 1.4f;
        smallStyle.normal.textColor = new Color(0.55f, 0.48f, 0.48f);
        GUI.Label(at, "[" + Controls.Label(GameAction.Inventory) + "] close", smallStyle);
    }

    void MakeStyles(float scale)
    {
        if (rowStyle == null)
        {
            rowStyle = new GUIStyle(GUI.skin.label);
            nameStyle = new GUIStyle(GUI.skin.label);
            textStyle = new GUIStyle(GUI.skin.label);
            smallStyle = new GUIStyle(GUI.skin.label);
        }
        // Set every frame: the styles can come back from an editor reload with old settings.
        rowStyle.alignment = TextAnchor.MiddleLeft; rowStyle.fontStyle = FontStyle.Bold; rowStyle.wordWrap = false;
        nameStyle.alignment = TextAnchor.MiddleCenter; nameStyle.fontStyle = FontStyle.Bold; nameStyle.wordWrap = false;
        textStyle.alignment = TextAnchor.UpperLeft; textStyle.wordWrap = true;
        textStyle.normal.textColor = new Color(0.88f, 0.84f, 0.8f);
        smallStyle.alignment = TextAnchor.MiddleLeft; smallStyle.fontStyle = FontStyle.Bold; smallStyle.wordWrap = false;
        rowStyle.font = nameStyle.font = textStyle.font = smallStyle.font = MenuStyle.Font;   // the menus' VCR font
        rowStyle.fontSize = Mathf.RoundToInt(21 * scale);   // small enough for seven rows
        nameStyle.fontSize = Mathf.RoundToInt(22 * scale);
        textStyle.fontSize = Mathf.RoundToInt(24 * scale);
        smallStyle.fontSize = Mathf.RoundToInt(20 * scale);
    }
}
