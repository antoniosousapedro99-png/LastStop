using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

// The game's menus, drawn in the FREE PSX UI pack's style (see MenuStyle):
// - Title: a view of the dark tunnel with the flickering cab light. "Press any key", then
//   New game / Load game / Options / Credits / Quit. New game fades to black and hands over to the player.
// - Pause (Esc, or Menu on a controller, while playing): Resume / Load game / Options / Credits /
//   Quit to title / Quit game. The game freezes and goes quiet while it's open. Quitting asks first.
// - Load game opens SaveMenu. Loading reloads the level; when it comes back with a save waiting
//   (SaveSystem.Pending) the title is skipped and the save is put back.
// - Options: every setting in GameSettings, saved as soon as it changes; Key bindings (pick an action,
//   press the new key or mouse button); Controller layout (fixed buttons, shown for reference).
// - Credits.
// Keyboard: W/S or arrows move, A/D or arrows adjust, Enter/Space choose, Esc goes back.
// Controller: d-pad or left stick, A chooses, B goes back. Mouse: hover, click, drag sliders, wheel.
public class MenuSystem : MonoBehaviour
{
    public GameObject player;
    public GameObject titleCamera;
    public AreaAmbience ambience;
    public DistantNoises noises;
    public Font font;
    public Texture2D panel;

    public static bool IsOpen { get; private set; }   // a menu (or the title) is on screen

    enum Page { PressKey, Main, Pause, Options, Credits, Confirm, Keys, Pad }
    Page page;
    Page optionsReturn;                 // where Options/Credits go back to (Main or Pause)
    bool inTitle;
    int selected;
    float fade;                         // black over everything, 0..1
    bool starting;
    float creditsScroll;
    int firstRow;                       // first row shown on the scrolling pages (options, key bindings)
    Vector2 lastMouse;
    bool draggingSlider;
    float timeScaleBefore = 1f;

    string confirmQuestion;
    Action confirmYes;
    Page confirmReturn;

    // Key binding: waiting for the player to press the new key for this action.
    bool waitingForKey;
    GameAction rebinding;
    InputControl pressedWhileWaiting;
    IDisposable keyListener;

    // ---------------------------------------------------------------- options rows
    enum Kind { Header, Slider, Toggle, Button }
    class Row
    {
        public string label;
        public Kind kind;
        public Func<float> get; public Action<float> set; public float min, max, step; public Func<float, string> show;
        public Func<bool> getOn; public Action<bool> setOn;
        public Action press;
    }
    List<Row> rows;

    static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    static string Number(float v, string format) => v.ToString(format, CultureInfo.InvariantCulture);

    void BuildRows()
    {
        rows = new List<Row>
        {
            Header("SOUND"),
            Slider("MASTER VOLUME", () => GameSettings.MasterVolume, v => GameSettings.MasterVolume = v, 0f, 1f, 0.05f, Percent),
            Slider("MUSIC VOLUME", () => GameSettings.MusicVolume, v => GameSettings.MusicVolume = v, 0f, 1f, 0.05f, Percent),
            Slider("EFFECTS & AMBIENCE", () => GameSettings.EffectsVolume, v => GameSettings.EffectsVolume = v, 0f, 1f, 0.05f, Percent),
            Header("CONTROLS"),
            Slider("LOOK SENSITIVITY", () => GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v, 0.2f, 3f, 0.05f, v => "x" + Number(v, "0.00")),
            Toggle("INVERT Y", () => GameSettings.InvertY, v => GameSettings.InvertY = v),
            Button("KEY BINDINGS", () => Go(Page.Keys)),
            Button("CONTROLLER LAYOUT", () => Go(Page.Pad)),
            Header("PICTURE"),
            Slider("BRIGHTNESS", () => GameSettings.Brightness, v => GameSettings.Brightness = v, -1f, 1.5f, 0.1f, v => (v >= 0f ? "+" : "") + Number(v, "0.0")),
            Slider("PS1 FILTER", () => GameSettings.PsxFilter, v => GameSettings.PsxFilter = v, 0f, 0.3f, 0.01f, Percent),
            Slider("FIELD OF VIEW", () => GameSettings.FieldOfView, v => GameSettings.FieldOfView = v, 75f, 110f, 1f, v => Number(v, "0")),
            Slider("VIEWMODEL FOV", () => GameSettings.ViewmodelFov, v => GameSettings.ViewmodelFov = v, 45f, 75f, 1f, v => Number(v, "0")),
            Toggle("ITEM GLOW", () => GameSettings.ItemGlow, v => GameSettings.ItemGlow = v),
            Toggle("CENTRE DOT", () => GameSettings.CentreDot, v => GameSettings.CentreDot = v),
            Header("DISPLAY"),
            Toggle("FULLSCREEN", () => GameSettings.Fullscreen, v => GameSettings.Fullscreen = v),
            Toggle("VSYNC", () => GameSettings.VSync, v => GameSettings.VSync = v),
            Button("RESET TO DEFAULTS", () => GameSettings.ResetToDefaults()),
            Button("BACK", Back),
        };
    }

    static Row Header(string label) => new Row { label = label, kind = Kind.Header };
    static Row Slider(string label, Func<float> get, Action<float> set, float min, float max, float step, Func<float, string> show)
        => new Row { label = label, kind = Kind.Slider, get = get, set = set, min = min, max = max, step = step, show = show };
    static Row Toggle(string label, Func<bool> get, Action<bool> set) => new Row { label = label, kind = Kind.Toggle, getOn = get, setOn = set };
    static Row Button(string label, Action press) => new Row { label = label, kind = Kind.Button, press = press };

    // ---------------------------------------------------------------- start: the title
    void Awake()
    {
        MenuStyle.Font = font;
        MenuStyle.Panel = panel;
        SaveSystem.RememberLevel();   // before anything in the level is picked up or opened
        BuildRows();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        // Show the title: the player waits (switched off) while a separate camera looks down the tunnel.
        inTitle = true;
        IsOpen = true;
        page = Page.PressKey;
        player.SetActive(false);
        titleCamera.SetActive(true);
        ambience.player = titleCamera.transform;   // background sound follows whichever camera is in use
        noises.player = titleCamera.transform;
        Hud.Hidden = true;
    }

    void Start()
    {
        if (SaveSystem.Pending != null) StartCoroutine(ResumeSavedGame());
        else FreeCursor(true);
    }

    void OnDestroy() => keyListener?.Dispose();

    static void FreeCursor(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    // ---------------------------------------------------------------- input
    void Update()
    {
        Controls.TrackDevice();   // so prompts name the controller's buttons when it's in use
        if (starting) return;
        if (SaveMenu.IsOpen || SaveMenu.ClosedFrame == Time.frameCount) return;   // the save/load screen has the keys

        if (!IsOpen)
        {
            // In the game: the Pause control (or Esc) pauses, unless Esc just closed the inventory.
            bool pause = Controls.Pressed(GameAction.Pause) || (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false);
            if (pause && !InventoryScreen.IsOpen && InventoryScreen.ClosedFrame != Time.frameCount) OpenPause();
            return;
        }

        if (waitingForKey) { WaitForKey(); return; }

        if (page == Page.PressKey)
        {
            if (Controls.UiAnyButton()) Go(Page.Main);
            return;
        }

        if (Controls.UiUp()) Move(-1);
        if (Controls.UiDown()) Move(1);
        if (Controls.UiLeft()) Adjust(-1);
        if (Controls.UiRight()) Adjust(1);
        if (Controls.UiAccept()) Choose();
        if (Controls.UiBack() || (page == Page.Pause && Controls.Pressed(GameAction.Pause) && !(Keyboard.current?.escapeKey.wasPressedThisFrame ?? false))) Back();

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (page == Page.Credits) creditsScroll = Mathf.Max(0f, creditsScroll - wheel * 0.5f);
            else if (wheel != 0f && (page == Page.Options || page == Page.Keys))
                firstRow = Mathf.Clamp(firstRow - Math.Sign(wheel), 0, Mathf.Max(0, ItemCount - VisibleRows));
        }
        if (page == Page.Credits) creditsScroll += Time.unscaledDeltaTime * 30f;   // slow roll upward
    }

    int ItemCount => page switch
    {
        Page.Main => 5, Page.Pause => 6, Page.Options => rows.Count, Page.Confirm => 2,
        Page.Keys => Controls.Listed.Length + 2, Page.Pad => 1, _ => 0,
    };

    void Move(int step)
    {
        int count = ItemCount;
        if (count == 0) return;
        do selected = (selected + step + count) % count;
        while (page == Page.Options && rows[selected].kind == Kind.Header);
        KeepSelectedVisible();
    }

    void Adjust(int direction)
    {
        if (page != Page.Options) return;
        var row = rows[selected];
        if (row.kind == Kind.Slider) row.set(Mathf.Clamp(row.get() + row.step * direction, row.min, row.max));
        else if (row.kind == Kind.Toggle) row.setOn(!row.getOn());
    }

    void Choose()
    {
        switch (page)
        {
            case Page.Main:
                if (selected == 0) StartGame();
                else if (selected == 1) SaveMenu.Current.Open(SaveMenu.Mode.Load, false);
                else if (selected == 2) Go(Page.Options, Page.Main);
                else if (selected == 3) Go(Page.Credits, Page.Main);
                else Confirm("QUIT THE GAME?", QuitGame, Page.Main);
                break;
            case Page.Pause:
                if (selected == 0) ClosePause();
                else if (selected == 1) SaveMenu.Current.Open(SaveMenu.Mode.Load, false, askFirst: true);
                else if (selected == 2) Go(Page.Options, Page.Pause);
                else if (selected == 3) Go(Page.Credits, Page.Pause);
                else if (selected == 4) Confirm("QUIT TO TITLE?", QuitToTitle, Page.Pause);
                else Confirm("QUIT THE GAME?", QuitGame, Page.Pause);
                break;
            case Page.Options:
                var row = rows[selected];
                if (row.kind == Kind.Toggle) row.setOn(!row.getOn());
                else if (row.kind == Kind.Button) row.press();
                break;
            case Page.Keys:
                int n = Controls.Listed.Length;
                if (selected < n) StartRebind(Controls.Listed[selected].action);
                else if (selected == n) Controls.ResetToDefaults();
                else Back();
                break;
            case Page.Credits:
            case Page.Pad:
                Back();
                break;
            case Page.Confirm:
                if (selected == 0) confirmYes(); else Go(confirmReturn);
                break;
        }
    }

    void Back()
    {
        switch (page)
        {
            case Page.Options:
            case Page.Credits: Go(optionsReturn); break;
            case Page.Keys:
            case Page.Pad: Go(Page.Options, optionsReturn); break;
            case Page.Confirm: Go(confirmReturn); break;
            case Page.Pause: ClosePause(); break;
            case Page.Main: Go(Page.PressKey); break;
        }
    }

    void Go(Page next, Page returnTo = Page.Main)
    {
        if (next == Page.Options || next == Page.Credits) optionsReturn = returnTo;
        page = next;
        selected = 0;
        creditsScroll = 0f;
        firstRow = 0;
        if (next == Page.Options) Move(1);   // skip the first header
        draggingSlider = false;
    }

    void Confirm(string question, Action yes, Page returnTo)
    {
        confirmQuestion = question;
        confirmYes = yes;
        confirmReturn = returnTo;
        Go(Page.Confirm);
        selected = 1;   // "NO" first, so a stray Enter doesn't quit
    }

    // ---------------------------------------------------------------- key binding
    void StartRebind(GameAction action)
    {
        rebinding = action;
        waitingForKey = true;
        pressedWhileWaiting = null;
        Listen();
    }

    // The next key or mouse button pressed anywhere (controller buttons are ignored: its layout is fixed).
    void Listen()
    {
        keyListener?.Dispose();
        keyListener = InputSystem.onAnyButtonPress
            .Where(c => c.device is Keyboard || c.device is Mouse)
            .CallOnce(c => pressedWhileWaiting = c);
    }

    void WaitForKey()
    {
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) { StopWaiting(); return; }   // B cancels
        if (pressedWhileWaiting == null) return;
        string path = Controls.PathOf(pressedWhileWaiting);
        if (path != "<Keyboard>/escape" && path != null) Controls.Rebind(rebinding, path);   // Esc cancels
        StopWaiting();
    }

    void StopWaiting()
    {
        waitingForKey = false;
        pressedWhileWaiting = null;
        keyListener?.Dispose();
        keyListener = null;
    }

    // ---------------------------------------------------------------- actions
    void StartGame()
    {
        if (!starting) StartCoroutine(StartGameRoutine());
    }

    System.Collections.IEnumerator StartGameRoutine()
    {
        starting = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f) { fade = t; yield return null; }
        fade = 1f;
        EnterGame();
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.8f) { fade = t; yield return null; }
        fade = 0f;
        starting = false;
    }

    // After loading a save: straight from black into the game, with everything put back.
    System.Collections.IEnumerator ResumeSavedGame()
    {
        starting = true;
        fade = 1f;
        EnterGame();
        yield return null;                   // let the player's scripts start up first
        SaveSystem.ApplyPending();
        yield return null;
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.8f) { fade = t; yield return null; }
        fade = 0f;
        starting = false;
    }

    // Swaps the title camera for the player.
    void EnterGame()
    {
        titleCamera.SetActive(false);
        player.SetActive(true);              // its PlayerLook locks the cursor
        ambience.player = player.transform;
        noises.player = player.transform;
        inTitle = false;
        IsOpen = false;
        Hud.Hidden = false;
        FreeCursor(false);
    }

    void OpenPause()
    {
        IsOpen = true;
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Hud.Hidden = true;
        SetLook(false);
        FreeCursor(true);
        Go(Page.Pause);
    }

    void ClosePause()
    {
        IsOpen = false;
        Time.timeScale = timeScaleBefore;
        AudioListener.pause = false;
        Hud.Hidden = false;
        SetLook(true);                        // locks the cursor again
    }

    void SetLook(bool on)
    {
        var look = player.GetComponent<PlayerLook>();
        if (look) look.enabled = on;
    }

    void QuitToTitle() => BackToTitle();

    // Reloads the level, which starts at the title (also used after dying).
    public static void BackToTitle()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // a fresh level, starting at the title
    }

    static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- drawing
    float s;                                  // 1080p units to screen pixels
    float VirtualWidth => Screen.width / s;
    Rect R(float x, float y, float w, float h) => new Rect(x * s, y * s, w * s, h * s);
    const float RowHeight = 54f;
    const int VisibleRows = 13;

    void OnGUI()
    {
        GUI.depth = -20;   // above the HUD and the inventory
        s = MenuStyle.Scale;
        var e = Event.current;
        bool mouseMoved = false;
        if (e.type == EventType.Repaint)
        {
            mouseMoved = (e.mousePosition - lastMouse).sqrMagnitude > 1f;
            lastMouse = e.mousePosition;
        }
        if (e.type == EventType.MouseUp) draggingSlider = false;
        if (waitingForKey && e.isMouse) e.Use();   // the click is the new binding, not a menu click

        if (IsOpen && SaveMenu.IsOpen && inTitle) DrawTitle();   // the save list draws itself on top
        else if (IsOpen)
        {
            switch (page)
            {
                case Page.PressKey: DrawTitle(); DrawPressKey(); break;
                case Page.Main: DrawTitle(); DrawList(new[] { "NEW GAME", "LOAD GAME", "OPTIONS", "CREDITS", "QUIT" }, 120f, 540f, mouseMoved); break;
                case Page.Pause: DrawPauseBackdrop(); DrawList(new[] { "RESUME", "LOAD GAME", "OPTIONS", "CREDITS", "QUIT TO TITLE", "QUIT GAME" }, 120f, 380f, mouseMoved); break;
                case Page.Options: DrawBackdrop(); DrawOptions(mouseMoved); break;
                case Page.Keys: DrawBackdrop(); DrawKeys(mouseMoved); break;
                case Page.Pad: DrawBackdrop(); DrawPad(); break;
                case Page.Credits: DrawBackdrop(); DrawCredits(); break;
                case Page.Confirm: DrawBackdrop(); DrawConfirm(mouseMoved); break;
            }
        }
        if (fade > 0f) MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, fade));
    }

    void DrawTitle()
    {
        // A dark wash from the left keeps the text readable over the tunnel.
        MenuStyle.Fill(new Rect(0, 0, Screen.width * 0.55f, Screen.height), new Color(0f, 0f, 0f, 0.35f));
        MenuStyle.Label(R(110, 150, 1200, 150), "LAST STOP", 150);
        MenuStyle.Label(R(118, 285, 1200, 50), "LINE 4  -  HOLLIS ST", 34, TextAnchor.MiddleLeft, MenuStyle.Dim);
    }

    void DrawPressKey()
    {
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        MenuStyle.Label(R(0, 880, VirtualWidth, 60), "PRESS ANY KEY", 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.35f + 0.65f * blink));
    }

    void DrawBackdrop() => MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, inTitle ? 0.55f : 0.7f));

    void DrawPauseBackdrop()
    {
        DrawBackdrop();
        MenuStyle.Label(R(0, 140, VirtualWidth, 140), "P A U S E", 110, TextAnchor.MiddleCenter);
    }

    // A ">> option" list: hovering selects, clicking chooses.
    void DrawList(string[] items, float x, float y, bool mouseMoved)
    {
        var e = Event.current;
        for (int i = 0; i < items.Length; i++)
        {
            var r = R(x, y + i * 70f, 640, 64);
            if (mouseMoved && r.Contains(e.mousePosition)) selected = i;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { selected = i; Choose(); e.Use(); return; }
            MenuStyle.Item(r, items[i], i == selected);
        }
    }

    // The panel and title shared by the scrolling pages; returns where the first row goes.
    Rect ListPanel(string title, out float x, out float w)
    {
        w = 1240f; x = (VirtualWidth - w) * 0.5f;
        MenuStyle.Label(R(x, 70, w, 80), title, 70);
        var panelRect = R(x, 150, w, VisibleRows * RowHeight + 60f);
        MenuStyle.DrawPanel(panelRect);
        return panelRect;
    }

    void DrawOptions(bool mouseMoved)
    {
        var e = Event.current;
        ListPanel("OPTIONS", out float x, out float w);
        const float y = 150f;
        for (int i = firstRow; i < Mathf.Min(rows.Count, firstRow + VisibleRows); i++)
        {
            var row = rows[i];
            float ry = y + 30f + (i - firstRow) * RowHeight;
            var full = R(x + 40, ry, w - 80, RowHeight);
            bool isSel = i == selected;
            if (row.kind == Kind.Header)
            {
                MenuStyle.Label(R(x + 40, ry, w - 80, RowHeight), row.label, 30, TextAnchor.LowerLeft, MenuStyle.Faint);
                continue;
            }
            if (mouseMoved && full.Contains(e.mousePosition)) selected = i;
            if (isSel) MenuStyle.Fill(full, new Color(1f, 0.85f, 0.85f, 0.07f));
            MenuStyle.Label(R(x + 70, ry, 560, RowHeight), row.label + (row.kind == Kind.Button && row.label != "BACK" && !row.label.StartsWith("RESET") ? "  >" : ""), 36, TextAnchor.MiddleLeft, isSel ? MenuStyle.Lit : MenuStyle.Dim);

            if (row.kind == Kind.Slider)
            {
                var bar = R(x + 660, ry + 8, 380, RowHeight - 16);
                float v = Mathf.InverseLerp(row.min, row.max, row.get());
                if (e.type == EventType.MouseDown && e.button == 0 && bar.Contains(e.mousePosition)) { selected = i; draggingSlider = true; }
                if (draggingSlider && isSel && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                {
                    float t = Mathf.Clamp01((e.mousePosition.x - bar.x) / bar.width);
                    float value = Mathf.Lerp(row.min, row.max, t);
                    row.set(Mathf.Round(value / row.step) * row.step);
                    e.Use();
                }
                MenuStyle.Slider(bar, v, isSel);
                MenuStyle.Label(R(x + 1060, ry, 120, RowHeight), row.show(row.get()), 32, TextAnchor.MiddleRight, isSel ? MenuStyle.Lit : MenuStyle.Dim);
            }
            else if (row.kind == Kind.Toggle)
            {
                MenuStyle.Toggle(R(x + 660, ry, 520, RowHeight), row.getOn(), isSel);
                if (e.type == EventType.MouseDown && e.button == 0 && full.Contains(e.mousePosition)) { selected = i; row.setOn(!row.getOn()); e.Use(); }
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && full.Contains(e.mousePosition)) { selected = i; row.press(); e.Use(); return; }
        }
        ScrollHint(x, w, rows.Count);
    }

    void DrawKeys(bool mouseMoved)
    {
        var e = Event.current;
        ListPanel("KEY BINDINGS", out float x, out float w);
        const float y = 150f;
        int n = Controls.Listed.Length;
        for (int i = firstRow; i < Mathf.Min(n + 2, firstRow + VisibleRows); i++)
        {
            float ry = y + 30f + (i - firstRow) * RowHeight;
            var full = R(x + 40, ry, w - 80, RowHeight);
            bool isSel = i == selected;
            if (!waitingForKey && mouseMoved && full.Contains(e.mousePosition)) selected = i;
            if (isSel) MenuStyle.Fill(full, new Color(1f, 0.85f, 0.85f, 0.07f));
            string label = i < n ? Controls.Listed[i].name : i == n ? "RESET KEYS" : "BACK";
            MenuStyle.Label(R(x + 70, ry, 560, RowHeight), label, 36, TextAnchor.MiddleLeft, isSel ? MenuStyle.Lit : MenuStyle.Dim);
            if (i < n)
            {
                bool waitingHere = waitingForKey && isSel;
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                string shown = waitingHere ? "PRESS A KEY...  (ESC CANCELS)" : "[ " + Controls.KeyLabel(Controls.Binding(Controls.Listed[i].action)) + " ]";
                MenuStyle.Label(R(x + 660, ry, 520, RowHeight), shown, 34, TextAnchor.MiddleRight,
                                waitingHere ? new Color(1f, 1f, 1f, 0.4f + 0.6f * blink) : isSel ? MenuStyle.Lit : MenuStyle.Dim);
            }
            if (!waitingForKey && e.type == EventType.MouseDown && e.button == 0 && full.Contains(e.mousePosition)) { selected = i; Choose(); e.Use(); return; }
        }
        ScrollHint(x, w, n + 2);
    }

    void ScrollHint(float x, float w, int count)
    {
        if (count > VisibleRows)
            MenuStyle.Label(R(x, 150 + VisibleRows * RowHeight + 70f, w, 40), "mouse wheel / arrows to scroll", 26, TextAnchor.MiddleRight, MenuStyle.Faint);
    }

    void DrawPad()
    {
        float w = 1100f, x = (VirtualWidth - w) * 0.5f;
        MenuStyle.Label(R(x, 70, w, 80), "CONTROLLER", 70);
        MenuStyle.DrawPanel(R(x, 150, w, 760));
        string[,] layout =
        {
            { "LEFT STICK", "MOVE" }, { "RIGHT STICK", "LOOK" }, { "L3 (CLICK)", "SPRINT" }, { "A", "JUMP" }, { "B", "CROUCH" },
            { "X", "USE / PICK UP" }, { "Y", "FLARE" }, { "LB / RB", "LEAN LEFT / RIGHT" }, { "D-PAD UP", "FLASHLIGHT" },
            { "D-PAD DOWN", "SWAP BATTERY" }, { "VIEW", "INVENTORY" }, { "MENU", "PAUSE" },
        };
        for (int i = 0; i < layout.GetLength(0); i++)
        {
            float ry = 185f + i * 56f;
            MenuStyle.Label(R(x + 80, ry, 420, 56), layout[i, 0], 34, TextAnchor.MiddleLeft, MenuStyle.Lit);
            MenuStyle.Label(R(x + 520, ry, 500, 56), layout[i, 1], 34, TextAnchor.MiddleLeft, MenuStyle.Dim);
        }
        MenuStyle.Label(R(x, 925, w, 40), Gamepad.current != null ? "controller connected   [B / ESC] back" : "no controller connected   [ESC] back",
                        26, TextAnchor.MiddleRight, MenuStyle.Faint);
    }

    void KeepSelectedVisible()
    {
        if (page != Page.Options && page != Page.Keys) return;
        if (selected < firstRow) firstRow = Mathf.Max(0, selected - (page == Page.Options ? 1 : 0));   // show its header too
        if (selected >= firstRow + VisibleRows) firstRow = selected - VisibleRows + 1;
    }

    void DrawCredits()
    {
        float w = 1100f, x = (VirtualWidth - w) * 0.5f;
        MenuStyle.Label(R(x, 70, w, 80), "CREDITS", 70);
        var area = R(x, 150, w, 800);
        MenuStyle.DrawPanel(area);
        GUI.BeginGroup(area);
        var style = MenuStyle.Text(32, TextAnchor.UpperCenter, MenuStyle.Lit);
        style.wordWrap = true;
        float height = style.CalcHeight(new GUIContent(CreditsList.Text), area.width - 80 * s);
        float scroll = creditsScroll * s;
        if (scroll > height + area.height) creditsScroll = 0f;   // roll round again
        GUI.Label(new Rect(40 * s, area.height - scroll, area.width - 80 * s, height), CreditsList.Text, style);
        GUI.EndGroup();
        MenuStyle.Label(R(x, 965, w, 40), "[ESC] back", 26, TextAnchor.MiddleRight, MenuStyle.Faint);
    }

    void DrawConfirm(bool mouseMoved)
    {
        float w = 700f, x = (VirtualWidth - w) * 0.5f, y = 380f;
        MenuStyle.DrawPanel(R(x, y, w, 280));
        MenuStyle.Label(R(x, y + 30, w, 70), confirmQuestion, 46, TextAnchor.MiddleCenter);
        DrawList(new[] { "YES", "NO" }, x + 250f, y + 120f, mouseMoved);
    }
}
