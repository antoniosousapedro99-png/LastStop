using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The game's menus, drawn in the FREE PSX UI pack's style (see MenuStyle):
// - Title: a view of the dark tunnel with the flickering cab light. "Press any key", then
//   New game / Options / Credits / Quit. New game fades to black and hands over to the player.
// - Pause (Esc while playing): Resume / Options / Credits / Quit to title / Quit game.
//   The game freezes and goes quiet while it's open. Quitting asks first.
// - Options: every setting in GameSettings, saved as soon as it changes.
// - Credits.
// Keyboard: W/S or arrows move, A/D or arrows adjust, Enter/Space choose, Esc goes back.
// Mouse: hover selects, click chooses, drag sliders, wheel scrolls.
public class MenuSystem : MonoBehaviour
{
    public GameObject player;
    public GameObject titleCamera;
    public AreaAmbience ambience;
    public DistantNoises noises;
    public Font font;
    public Texture2D panel;

    public static bool IsOpen { get; private set; }   // a menu (or the title) is on screen

    enum Page { PressKey, Main, Pause, Options, Credits, Confirm }
    Page page;
    Page optionsReturn;                 // where Options/Credits go back to (Main or Pause)
    bool inTitle;
    int selected;
    float fade;                         // black over everything, 0..1
    bool starting;
    float creditsScroll;
    int optionsFirst;                   // first options row shown (the list scrolls)
    Vector2 lastMouse;
    bool draggingSlider;
    float timeScaleBefore = 1f;

    string confirmQuestion;
    Action confirmYes;
    Page confirmReturn;

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
            Header("MOUSE"),
            Slider("MOUSE SENSITIVITY", () => GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v, 0.2f, 3f, 0.05f, v => "x" + Number(v, "0.00")),
            Toggle("INVERT Y", () => GameSettings.InvertY, v => GameSettings.InvertY = v),
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

    void Start() => FreeCursor(true);

    static void FreeCursor(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    // ---------------------------------------------------------------- input
    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (starting || keyboard == null) return;

        if (!IsOpen)
        {
            // In the game: Esc pauses (unless it was just used to close the inventory).
            if (keyboard.escapeKey.wasPressedThisFrame && !InventoryScreen.IsOpen && InventoryScreen.ClosedFrame != Time.frameCount)
                OpenPause();
            return;
        }

        if (page == Page.PressKey)
        {
            if (keyboard.anyKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame)) Go(Page.Main);
            return;
        }

        if (Pressed(keyboard.wKey, keyboard.upArrowKey)) Move(-1);
        if (Pressed(keyboard.sKey, keyboard.downArrowKey)) Move(1);
        if (Pressed(keyboard.aKey, keyboard.leftArrowKey)) Adjust(-1);
        if (Pressed(keyboard.dKey, keyboard.rightArrowKey)) Adjust(1);
        if (Pressed(keyboard.enterKey, keyboard.spaceKey) || keyboard.numpadEnterKey.wasPressedThisFrame) Choose();
        if (keyboard.escapeKey.wasPressedThisFrame) Back();
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (page == Page.Credits) creditsScroll = Mathf.Max(0f, creditsScroll - wheel * 0.5f);
            else if (page == Page.Options && wheel != 0f) optionsFirst = Mathf.Clamp(optionsFirst - Math.Sign(wheel), 0, Mathf.Max(0, rows.Count - OptionsVisible));
        }
        if (page == Page.Credits) creditsScroll += Time.unscaledDeltaTime * 30f;   // slow roll upward
    }

    static bool Pressed(params UnityEngine.InputSystem.Controls.KeyControl[] keys)
    {
        foreach (var k in keys) if (k.wasPressedThisFrame) return true;
        return false;
    }

    int ItemCount => page switch
    {
        Page.Main => 4, Page.Pause => 5, Page.Options => rows.Count, Page.Confirm => 2, _ => 0,
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
                else if (selected == 1) Go(Page.Options, Page.Main);
                else if (selected == 2) Go(Page.Credits, Page.Main);
                else Confirm("QUIT THE GAME?", QuitGame, Page.Main);
                break;
            case Page.Pause:
                if (selected == 0) ClosePause();
                else if (selected == 1) Go(Page.Options, Page.Pause);
                else if (selected == 2) Go(Page.Credits, Page.Pause);
                else if (selected == 3) Confirm("QUIT TO TITLE?", QuitToTitle, Page.Pause);
                else Confirm("QUIT THE GAME?", QuitGame, Page.Pause);
                break;
            case Page.Options:
                var row = rows[selected];
                if (row.kind == Kind.Toggle) row.setOn(!row.getOn());
                else if (row.kind == Kind.Button) row.press();
                break;
            case Page.Credits:
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
        optionsFirst = 0;
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
        titleCamera.SetActive(false);
        player.SetActive(true);              // its PlayerLook locks the cursor
        ambience.player = player.transform;
        noises.player = player.transform;
        inTitle = false;
        IsOpen = false;
        Hud.Hidden = false;
        FreeCursor(false);
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.8f) { fade = t; yield return null; }
        fade = 0f;
        starting = false;
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

    void QuitToTitle()
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
    const int OptionsVisible = 13;

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

        if (IsOpen)
        {
            switch (page)
            {
                case Page.PressKey: DrawTitle(); DrawPressKey(); break;
                case Page.Main: DrawTitle(); DrawList(new[] { "NEW GAME", "OPTIONS", "CREDITS", "QUIT" }, 120f, 560f, mouseMoved); break;
                case Page.Pause: DrawPauseBackdrop(); DrawList(new[] { "RESUME", "OPTIONS", "CREDITS", "QUIT TO TITLE", "QUIT GAME" }, 120f, 400f, mouseMoved); break;
                case Page.Options: DrawBackdrop(); DrawOptions(mouseMoved); break;
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

    // A ">> option" list; returns nothing, but hovering selects and clicking chooses.
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

    void DrawOptions(bool mouseMoved)
    {
        var e = Event.current;
        float w = 1240f, x = (VirtualWidth - w) * 0.5f, y = 150f;
        MenuStyle.Label(R(x, 70, w, 80), "OPTIONS", 70);
        MenuStyle.DrawPanel(R(x, y, w, OptionsVisible * RowHeight + 60f));
        for (int i = optionsFirst; i < Mathf.Min(rows.Count, optionsFirst + OptionsVisible); i++)
        {
            var row = rows[i];
            float ry = y + 30f + (i - optionsFirst) * RowHeight;
            var full = R(x + 40, ry, w - 80, RowHeight);
            bool isSel = i == selected;
            if (row.kind == Kind.Header)
            {
                MenuStyle.Label(R(x + 40, ry, w - 80, RowHeight), row.label, 30, TextAnchor.LowerLeft, MenuStyle.Faint);
                continue;
            }
            if (mouseMoved && full.Contains(e.mousePosition)) selected = i;
            if (isSel) MenuStyle.Fill(full, new Color(1f, 0.85f, 0.85f, 0.07f));
            MenuStyle.Label(R(x + 70, ry, 560, RowHeight), row.label, 36, TextAnchor.MiddleLeft, isSel ? MenuStyle.Lit : MenuStyle.Dim);

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
        if (rows.Count > OptionsVisible)
            MenuStyle.Label(R(x, y + OptionsVisible * RowHeight + 70f, w, 40), "mouse wheel / arrows to scroll", 26, TextAnchor.MiddleRight, MenuStyle.Faint);
    }

    void KeepSelectedVisible()
    {
        if (page != Page.Options) return;
        if (selected < optionsFirst) optionsFirst = Mathf.Max(0, selected - 1);   // show its header too
        if (selected >= optionsFirst + OptionsVisible) optionsFirst = selected - OptionsVisible + 1;
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
