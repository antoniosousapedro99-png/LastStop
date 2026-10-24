using UnityEngine;
using UnityEngine.InputSystem;

// The save / load screen, in the menus' style (MenuStyle).
// - Save (from the staff room PC): pick one of the five slots, then name the save. The name box starts
//   with a suggestion like "Staff Room - 14:32": Enter keeps it, or type your own (up to 24 letters).
//   The game pauses while this is open.
// - Load (from the title or the pause menu): the autosave plus the five slots; pick one to load it.
//   From the pause menu it asks first, because anything not saved is lost.
// Keyboard: W/S or arrows, Enter, Esc. Controller: d-pad, A, B (a controller keeps the suggested name).
// Mouse: hover and click.
public class SaveMenu : MonoBehaviour
{
    public enum Mode { Save, Load }

    public static SaveMenu Current { get; private set; }
    public static bool IsOpen { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;   // so the Esc that closes it doesn't also open the pause menu

    const int MaxNameLength = 24;
    const float RowHeight = 86f;

    enum Step { List, Naming, ConfirmLoad }
    Mode mode;
    Step step;
    bool pausedGame, askBeforeLoading;
    int selected, chosenSlot, stepFrame;
    string typedName = "";
    readonly SaveData[] saves = new SaveData[SaveSystem.Slots + 1];   // by slot number; 0 = the autosave
    float timeScaleBefore = 1f;
    Behaviour[] switchedOff;               // the player's controls, off while saving (typing a name mustn't crouch or jump)
    Vector2 lastMouse;
    [System.NonSerialized] GUIStyle fieldStyle;

    // Load lists the autosave and slots 1-5, Save only slots 1-5; a BACK row comes last.
    int FirstSlot => mode == Mode.Load ? SaveSystem.Autosave : 1;
    int RowCount => SaveSystem.Slots - FirstSlot + 2;
    int SlotAt(int row) => FirstSlot + row;
    bool IsBackRow(int row) => row == RowCount - 1;

    void Awake()
    {
        Current = this;
        IsOpen = false;   // statics survive between Play runs here, so start every run closed
    }

    // pauseGame: freeze the game while it's open (the PC). The title and pause menu are already paused.
    public void Open(Mode openAs, bool pauseGame, bool askFirst = false)
    {
        mode = openAs;
        askBeforeLoading = askFirst;
        for (int i = 0; i < saves.Length; i++) saves[i] = SaveSystem.Peek(i);
        IsOpen = true;
        selected = 0;
        GoTo(Step.List);
        pausedGame = pauseGame;
        if (!pauseGame) return;
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Hud.Hidden = true;
        var player = FindAnyObjectByType<PlayerInventory>();
        switchedOff = System.Array.FindAll(new Behaviour[]
        {
            player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerCrouch>(), player.GetComponent<PlayerLean>(),
            player.GetComponent<PlayerLook>(), player.GetComponentInChildren<Flashlight>(), player.GetComponentInChildren<FlashlightBattery>(),
            player.GetComponent<FlareHandler>(), player.GetComponentInChildren<Interactor>(),
        }, b => b != null && b.enabled);
        foreach (var b in switchedOff) b.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Close()
    {
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        if (!pausedGame) return;
        Time.timeScale = timeScaleBefore;
        AudioListener.pause = false;
        Hud.Hidden = false;
        foreach (var b in switchedOff) if (b) b.enabled = true;   // PlayerLook locks the cursor again
    }

    void GoTo(Step next)
    {
        step = next;
        stepFrame = Time.frameCount;   // the key press that got us here mustn't also act on the new step
        if (next == Step.List) selected = Mathf.Clamp(chosenSlot - FirstSlot, 0, RowCount - 1);
        if (next == Step.ConfirmLoad) selected = 1;   // "NO" first
    }

    // ---------------------------------------------------------------- input
    void Update()
    {
        if (!IsOpen || Time.frameCount == stepFrame) return;
        if (step == Step.Naming)
        {
            // Letters, Enter and Esc go to the name box (OnGUI). A controller can only keep the name or cancel.
            var pad = Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) SaveHere();
            else if (pad != null && pad.buttonEast.wasPressedThisFrame) GoTo(Step.List);
            return;
        }
        if (Controls.UiBack())
        {
            if (step == Step.ConfirmLoad) GoTo(Step.List); else Close();
            return;
        }
        int count = step == Step.ConfirmLoad ? 2 : RowCount;
        if (Controls.UiUp()) selected = (selected - 1 + count) % count;
        if (Controls.UiDown()) selected = (selected + 1) % count;
        if (Controls.UiAccept()) Choose();
    }

    void Choose()
    {
        if (step == Step.ConfirmLoad)
        {
            if (selected == 0) SaveSystem.Load(chosenSlot); else GoTo(Step.List);
            return;
        }
        if (IsBackRow(selected)) { Close(); return; }
        int slot = SlotAt(selected);
        chosenSlot = slot;
        if (mode == Mode.Save)
        {
            typedName = SaveSystem.DefaultName();
            GoTo(Step.Naming);
        }
        else if (saves[slot] != null)
        {
            if (askBeforeLoading) GoTo(Step.ConfirmLoad);
            else SaveSystem.Load(slot);
        }
    }

    void SaveHere()
    {
        bool ok = SaveSystem.Write(chosenSlot, typedName);
        Close();
        Hud.Notice(ok ? "Game saved" : "Couldn't save the game", 2.5f);
    }

    // ---------------------------------------------------------------- drawing
    float s;
    float VirtualWidth => Screen.width / s;
    Rect R(float x, float y, float w, float h) => new Rect(x * s, y * s, w * s, h * s);

    void OnGUI()
    {
        if (!IsOpen) return;
        GUI.depth = -25;   // above the other menus
        s = MenuStyle.Scale;
        var e = Event.current;
        bool mouseMoved = false;
        if (e.type == EventType.Repaint)
        {
            mouseMoved = (e.mousePosition - lastMouse).sqrMagnitude > 1f;
            lastMouse = e.mousePosition;
        }

        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.75f));
        float w = 1100f, x = (VirtualWidth - w) * 0.5f;
        MenuStyle.Label(R(x, 60, w, 80), mode == Mode.Save ? "SAVE GAME" : "LOAD GAME", 70);
        if (mode == Mode.Save) MenuStyle.Label(R(x, 130, w, 36), "STATION 1 MAINTENANCE TERMINAL", 26, TextAnchor.MiddleLeft, MenuStyle.Faint);
        float panelHeight = RowCount * RowHeight + 40f;
        MenuStyle.DrawPanel(R(x, 170, w, panelHeight));

        for (int row = 0; row < RowCount; row++)
        {
            float ry = 190f + row * RowHeight;
            var full = R(x + 30, ry, w - 60, RowHeight - 8);
            bool empty = !IsBackRow(row) && saves[SlotAt(row)] == null;
            bool usable = !(mode == Mode.Load && empty);
            if (step == Step.List)
            {
                if (mouseMoved && full.Contains(e.mousePosition)) selected = row;
                if (e.type == EventType.MouseDown && e.button == 0 && full.Contains(e.mousePosition)) { selected = row; Choose(); e.Use(); return; }
            }
            bool lit = step == Step.List ? row == selected : !IsBackRow(row) && SlotAt(row) == chosenSlot;
            if (lit) MenuStyle.Fill(full, new Color(1f, 0.85f, 0.85f, 0.07f));
            var colour = !usable ? MenuStyle.Faint : lit ? MenuStyle.Lit : MenuStyle.Dim;

            if (IsBackRow(row)) { MenuStyle.Label(R(x + 60, ry, 400, RowHeight - 8), "BACK", 36, TextAnchor.MiddleLeft, colour); continue; }
            int slot = SlotAt(row);
            var data = saves[slot];
            MenuStyle.Label(R(x + 60, ry, 120, RowHeight - 8), slot == SaveSystem.Autosave ? "AUTO" : slot.ToString(), 34, TextAnchor.MiddleLeft, colour);
            if (data == null) { MenuStyle.Label(R(x + 190, ry, 600, RowHeight - 8), "-  EMPTY  -", 34, TextAnchor.MiddleLeft, MenuStyle.Faint); continue; }
            MenuStyle.Label(R(x + 190, ry + 6, 620, 44), data.name, 36, TextAnchor.MiddleLeft, colour);
            MenuStyle.Label(R(x + 190, ry + 46, 620, 30), data.area, 24, TextAnchor.MiddleLeft, MenuStyle.Faint);
            MenuStyle.Label(R(x + 700, ry, 340, RowHeight - 8), SaveSystem.ShortDate(data), 28, TextAnchor.MiddleRight, colour);
        }
        string hint = Controls.UsingGamepad ? "(A) choose   (B) back" : "[ENTER] choose   [ESC] back";
        MenuStyle.Label(R(x, 180 + panelHeight, w, 40), hint, 26, TextAnchor.MiddleRight, MenuStyle.Faint);

        if (step == Step.Naming) DrawNaming(e);
        else if (step == Step.ConfirmLoad) DrawConfirm(e, mouseMoved);
    }

    void DrawNaming(Event e)
    {
        // Enter and Esc are read before the text box can swallow them.
        if (e.type == EventType.KeyDown && Time.frameCount != stepFrame)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { e.Use(); SaveHere(); return; }
            if (e.keyCode == KeyCode.Escape) { e.Use(); GoTo(Step.List); return; }
        }
        float w = 860f, x = (VirtualWidth - w) * 0.5f, y = 390f;
        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
        MenuStyle.DrawPanel(R(x, y, w, 290));
        string old = saves[chosenSlot]?.name;
        MenuStyle.Label(R(x, y + 25, w, 60), old == null ? "NAME THIS SAVE" : "OVERWRITE \"" + old + "\"?", old == null ? 40 : 32, TextAnchor.MiddleCenter);

        var box = R(x + 60, y + 105, w - 120, 76);
        MenuStyle.Fill(box, new Color(0f, 0f, 0f, 0.55f));
        MenuStyle.Fill(new Rect(box.x, box.yMax - 3f * s, box.width, 3f * s), MenuStyle.Dim);   // an underline, like a terminal field
        if (fieldStyle == null)
        {
            fieldStyle = new GUIStyle(GUI.skin.textField);
            fieldStyle.normal.background = fieldStyle.focused.background = fieldStyle.hover.background = fieldStyle.active.background = null;
            fieldStyle.onNormal.background = fieldStyle.onFocused.background = fieldStyle.onHover.background = fieldStyle.onActive.background = null;
            fieldStyle.border = new RectOffset(0, 0, 0, 0);
            fieldStyle.alignment = TextAnchor.MiddleLeft;
        }
        fieldStyle.font = MenuStyle.Font;
        fieldStyle.fontSize = Mathf.RoundToInt(40 * s);
        fieldStyle.normal.textColor = fieldStyle.focused.textColor = fieldStyle.hover.textColor = MenuStyle.Lit;
        fieldStyle.padding = new RectOffset(Mathf.RoundToInt(18 * s), Mathf.RoundToInt(18 * s), 0, 0);
        GUI.skin.settings.cursorColor = MenuStyle.Lit;
        GUI.skin.settings.selectionColor = new Color(0.6f, 0.2f, 0.25f, 0.8f);   // the suggested name starts selected: typing replaces it
        GUI.SetNextControlName("SaveName");
        typedName = GUI.TextField(box, typedName, MaxNameLength, fieldStyle);
        GUI.FocusControl("SaveName");

        string hint = Controls.UsingGamepad ? "(A) save   (B) cancel" : "[ENTER] save   [ESC] cancel";
        MenuStyle.Label(R(x, y + 205, w, 50), hint, 28, TextAnchor.MiddleCenter, MenuStyle.Faint);
    }

    void DrawConfirm(Event e, bool mouseMoved)
    {
        float w = 860f, x = (VirtualWidth - w) * 0.5f, y = 390f;
        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
        MenuStyle.DrawPanel(R(x, y, w, 320));
        MenuStyle.Label(R(x, y + 25, w, 60), "LOAD \"" + saves[chosenSlot].name + "\"?", 36, TextAnchor.MiddleCenter);
        MenuStyle.Label(R(x, y + 80, w, 40), "ANYTHING NOT SAVED WILL BE LOST", 26, TextAnchor.MiddleCenter, MenuStyle.Faint);
        string[] items = { "YES", "NO" };
        for (int i = 0; i < 2; i++)
        {
            var r = R(x + 330, y + 140 + i * 70f, 400, 64);
            if (mouseMoved && r.Contains(e.mousePosition)) selected = i;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { selected = i; Choose(); e.Use(); return; }
            MenuStyle.Item(r, items[i], i == selected);
        }
    }
}
