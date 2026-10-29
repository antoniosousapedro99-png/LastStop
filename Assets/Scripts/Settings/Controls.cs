using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Every game action goes through here, so keys can be changed in Options and a controller works too.
// - Keyboard/mouse: each action has one key or mouse button, saved in PlayerPrefs as a path such as
//   "<Keyboard>/w" or "<Mouse>/leftButton" (Rebind changes it; ResetToDefaults restores them).
// - Controller: a fixed layout (left stick moves, right stick looks, A jump, B crouch, X use, Y flare,
//   LB/RB lean, L3 sprint, R3 turn around, d-pad up torch, d-pad down battery, View inventory, Menu pause).
// - Menus have their own fixed keys (arrows/WASD, Enter, Esc; d-pad/stick, A, B) via the Ui* calls.
public enum GameAction { Forward, Back, Left, Right, Sprint, Crouch, Jump, LeanLeft, LeanRight, Use, Torch, SwapBattery, Flare, Inventory, Pause, TurnAround, Journal }

public static class Controls
{
    // In the order the key bindings page lists them, with their names there.
    public static readonly (GameAction action, string name)[] Listed =
    {
        (GameAction.Forward, "MOVE FORWARD"), (GameAction.Back, "MOVE BACK"), (GameAction.Left, "MOVE LEFT"), (GameAction.Right, "MOVE RIGHT"),
        (GameAction.Sprint, "SPRINT"), (GameAction.Crouch, "CROUCH"), (GameAction.Jump, "JUMP"),
        (GameAction.LeanLeft, "LEAN LEFT"), (GameAction.LeanRight, "LEAN RIGHT"), (GameAction.TurnAround, "LOOK BACK (HOLD)"), (GameAction.Use, "USE / PICK UP"),
        (GameAction.Torch, "FLASHLIGHT"), (GameAction.SwapBattery, "SWAP BATTERY"), (GameAction.Flare, "FLARE"),
        (GameAction.Inventory, "INVENTORY"), (GameAction.Journal, "JOURNAL"), (GameAction.Pause, "PAUSE"),
    };

    static readonly Dictionary<GameAction, string> Defaults = new Dictionary<GameAction, string>
    {
        { GameAction.Forward, "<Keyboard>/w" }, { GameAction.Back, "<Keyboard>/s" }, { GameAction.Left, "<Keyboard>/a" }, { GameAction.Right, "<Keyboard>/d" },
        { GameAction.Sprint, "<Keyboard>/leftShift" }, { GameAction.Crouch, "<Keyboard>/c" }, { GameAction.Jump, "<Keyboard>/space" },
        { GameAction.LeanLeft, "<Keyboard>/q" }, { GameAction.LeanRight, "<Keyboard>/e" }, { GameAction.Use, "<Mouse>/leftButton" },
        { GameAction.Torch, "<Keyboard>/f" }, { GameAction.SwapBattery, "<Keyboard>/h" }, { GameAction.Flare, "<Keyboard>/g" },
        { GameAction.Inventory, "<Keyboard>/tab" }, { GameAction.Pause, "<Keyboard>/escape" }, { GameAction.TurnAround, "<Keyboard>/leftAlt" },
        { GameAction.Journal, "<Keyboard>/j" },
    };

    static string PrefKey(GameAction a) => "Key_" + a;
    public static string Binding(GameAction a) => PlayerPrefs.GetString(PrefKey(a), Defaults[a]);

    // Gives 'a' a new key. If another action already used it, that action takes 'a''s old key (a swap),
    // so no key ever does two things.
    public static void Rebind(GameAction a, string path)
    {
        string old = Binding(a);
        foreach (var (other, _) in Listed)
            if (other != a && Binding(other) == path) PlayerPrefs.SetString(PrefKey(other), old);
        PlayerPrefs.SetString(PrefKey(a), path);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        foreach (var (a, _) in Listed) PlayerPrefs.DeleteKey(PrefKey(a));
        PlayerPrefs.Save();
    }

    // "<Keyboard>/w" for a pressed key or button (only keyboard and mouse can be bound).
    public static string PathOf(InputControl control)
    {
        if (control.device is Keyboard) return "<Keyboard>/" + control.name;
        if (control.device is Mouse) return "<Mouse>/" + control.name;
        return null;
    }

    // ---------------------------------------------------------------- reading actions
    static ButtonControl KeyOrButton(GameAction a)
    {
        string path = Binding(a);
        int slash = path.IndexOf('/');
        if (slash < 0) return null;
        string device = path.Substring(0, slash), name = path.Substring(slash + 1);
        InputDevice d = device == "<Mouse>" ? (InputDevice)Mouse.current : Keyboard.current;
        return d?.TryGetChildControl<ButtonControl>(name);
    }

    static ButtonControl PadButton(GameAction a)
    {
        var pad = Gamepad.current;
        if (pad == null) return null;
        return a switch
        {
            GameAction.Sprint => pad.leftStickButton,
            GameAction.Crouch => pad.buttonEast,
            GameAction.Jump => pad.buttonSouth,
            GameAction.LeanLeft => pad.leftShoulder,
            GameAction.LeanRight => pad.rightShoulder,
            GameAction.Use => pad.buttonWest,
            GameAction.Torch => pad.dpad.up,
            GameAction.SwapBattery => pad.dpad.down,
            GameAction.Flare => pad.buttonNorth,
            GameAction.Inventory => pad.selectButton,
            GameAction.Pause => pad.startButton,
            GameAction.TurnAround => pad.rightStickButton,
            GameAction.Journal => pad.dpad.right,
            _ => null,   // movement comes from the left stick (see Move)
        };
    }

    public static bool Held(GameAction a)
    {
        var k = KeyOrButton(a); var p = PadButton(a);
        return (k != null && k.isPressed) || (p != null && p.isPressed);
    }

    public static bool Pressed(GameAction a)
    {
        var k = KeyOrButton(a); var p = PadButton(a);
        return (k != null && k.wasPressedThisFrame) || (p != null && p.wasPressedThisFrame);
    }

    // Walking direction: x = right, y = forward, length up to 1.
    public static Vector2 Move()
    {
        var v = Vector2.zero;
        if (Held(GameAction.Forward)) v.y += 1f;
        if (Held(GameAction.Back)) v.y -= 1f;
        if (Held(GameAction.Right)) v.x += 1f;
        if (Held(GameAction.Left)) v.x -= 1f;
        if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
        return Vector2.ClampMagnitude(v, 1f);
    }

    // Sprinting: hold the sprint key; on a controller, click L3 and it stays on until you stop moving.
    static bool padSprint;
    public static bool Sprinting()
    {
        var pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.leftStickButton.wasPressedThisFrame) padSprint = true;
            if (pad.leftStick.ReadValue().magnitude < 0.3f) padSprint = false;
        }
        var k = KeyOrButton(GameAction.Sprint);
        return (k != null && k.isPressed) || padSprint;
    }

    // Right stick, -1..1 on each axis (the camera turns it into degrees per second).
    public static Vector2 PadLook() => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;

    // ---------------------------------------------------------------- names for prompts
    // True when the controller was used more recently than the keyboard or mouse (prompts then name pad buttons).
    public static bool UsingGamepad => padLast && Gamepad.current != null;
    static bool padLast;
    // Called once a frame by MenuSystem: remembers which kind of device was used last.
    public static void TrackDevice()
    {
        var pad = Gamepad.current;
        if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f || AnyPadButton(pad))) padLast = true;
        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 4f || Mouse.current.leftButton.wasPressedThisFrame))) padLast = false;
    }

    static bool AnyPadButton(Gamepad pad)
    {
        foreach (var c in pad.allControls) if (c is ButtonControl b && !b.synthetic && b.wasPressedThisFrame) return true;
        return false;
    }

    // What to show in prompts, e.g. "G", "LMB", or "Y" when playing with a controller.
    public static string Label(GameAction a)
    {
        if (UsingGamepad) return PadLabel(a);
        return KeyLabel(Binding(a));
    }

    public static string KeyLabel(string path)
    {
        switch (path)
        {
            case "<Mouse>/leftButton": return "LMB";
            case "<Mouse>/rightButton": return "RMB";
            case "<Mouse>/middleButton": return "MMB";
            case "<Mouse>/forwardButton": return "MOUSE 5";
            case "<Mouse>/backButton": return "MOUSE 4";
        }
        int slash = path.IndexOf('/');
        string name = slash >= 0 ? path.Substring(slash + 1) : path;
        var control = Keyboard.current?.TryGetChildControl(name);
        string shown = control != null ? control.displayName : name;
        return string.IsNullOrEmpty(shown) ? name.ToUpperInvariant() : shown.ToUpperInvariant();
    }

    public static string PadLabel(GameAction a) => a switch
    {
        GameAction.Forward or GameAction.Back or GameAction.Left or GameAction.Right => "L-STICK",
        GameAction.Sprint => "L3", GameAction.Crouch => "B", GameAction.Jump => "A",
        GameAction.LeanLeft => "LB", GameAction.LeanRight => "RB", GameAction.Use => "X",
        GameAction.Torch => "D-PAD UP", GameAction.SwapBattery => "D-PAD DOWN", GameAction.Flare => "Y",
        GameAction.Inventory => "VIEW", GameAction.Pause => "MENU", GameAction.TurnAround => "R3", GameAction.Journal => "D-PAD RIGHT", _ => "?",
    };

    // ---------------------------------------------------------------- menu navigation (fixed keys)
    static int stickY, stickX;        // the left stick acts like single d-pad presses in menus
    static int stickFrame = -1;
    static Vector2Int stickPressed;

    static void ReadStick()
    {
        if (stickFrame == Time.frameCount) return;
        stickFrame = Time.frameCount;
        stickPressed = Vector2Int.zero;
        var pad = Gamepad.current;
        if (pad == null) return;
        var v = pad.leftStick.ReadValue();
        int y = v.y > 0.6f ? 1 : v.y < -0.6f ? -1 : 0;
        int x = v.x > 0.6f ? 1 : v.x < -0.6f ? -1 : 0;
        if (y != 0 && y != stickY) stickPressed.y = y;
        if (x != 0 && x != stickX) stickPressed.x = x;
        stickY = y; stickX = x;
    }

    static bool KeysPressed(params UnityEngine.InputSystem.Key[] keys)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        foreach (var k in keys) if (kb[k].wasPressedThisFrame) return true;
        return false;
    }

    public static bool UiUp() { ReadStick(); return KeysPressed(UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.UpArrow) || (Gamepad.current?.dpad.up.wasPressedThisFrame ?? false) || stickPressed.y > 0; }
    public static bool UiDown() { ReadStick(); return KeysPressed(UnityEngine.InputSystem.Key.S, UnityEngine.InputSystem.Key.DownArrow) || (Gamepad.current?.dpad.down.wasPressedThisFrame ?? false) || stickPressed.y < 0; }
    public static bool UiLeft() { ReadStick(); return KeysPressed(UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.LeftArrow) || (Gamepad.current?.dpad.left.wasPressedThisFrame ?? false) || stickPressed.x < 0; }
    public static bool UiRight() { ReadStick(); return KeysPressed(UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.RightArrow) || (Gamepad.current?.dpad.right.wasPressedThisFrame ?? false) || stickPressed.x > 0; }
    public static bool UiAccept() => KeysPressed(UnityEngine.InputSystem.Key.Enter, UnityEngine.InputSystem.Key.NumpadEnter, UnityEngine.InputSystem.Key.Space) || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);
    public static bool UiBack() => KeysPressed(UnityEngine.InputSystem.Key.Escape) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false);
    public static bool UiAnyButton() =>
        (Keyboard.current?.anyKey.wasPressedThisFrame ?? false) || (Mouse.current?.leftButton.wasPressedThisFrame ?? false) ||
        (Gamepad.current != null && AnyPadButton(Gamepad.current));
}
