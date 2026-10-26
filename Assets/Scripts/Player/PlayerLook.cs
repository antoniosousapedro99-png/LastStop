using UnityEngine;
using UnityEngine.InputSystem;

// Mouse look: moving the mouse sideways turns the whole body, up/down tilts only the camera.
// The cursor is hidden and locked to the window (the pause menu frees it; clicking the game locks it again).
// Speed and up/down direction come from the Mouse options (GameSettings); a controller's right stick also looks.
// Look back (hold Left Alt / R3): only the head swings round to look behind; the body keeps facing (and
// running) the way it was, and the mouse can glance a little to the sides. Letting go swings the head back.
public class PlayerLook : MonoBehaviour
{
    public Transform cameraPivot;      // the camera (child of the player)
    public float sensitivity = 0.12f;  // degrees per pixel of mouse movement at sensitivity x1.00
    public float padSpeed = 140f;      // degrees per second with the controller's right stick fully over
    public float pitch;                // current up/down angle (public so tests can set it)
    public float roll;                 // sideways head tilt, set by PlayerLean
    public bool limitYaw;              // while hiding in a locker: turning is kept within yawRange of yawCentre
    public float yawCentre, yawRange = 55f;
    public static float Shake;         // degrees of view shake (TrainRumble sets it; 0 = still)
    public float turnTime = 0.25f;     // seconds for the head to swing round to look back (and back again)
    public float glanceRange = 30f;    // degrees the mouse can glance sideways while looking back

    float headYaw;                     // how far the head is turned from the body (0 = forward, 180 = looking back)
    float glance;                      // sideways glance while looking back

    public bool LookingBack => headYaw > 90f;

    void OnEnable() => LockCursor(true);

    void Awake() => Shake = 0f;   // shared values survive between Play runs here, so start still

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) LockCursor(true);
        if (Cursor.lockState != CursorLockMode.Locked) { Apply(); return; }

        Vector2 delta = mouse != null ? mouse.delta.ReadValue() * sensitivity : Vector2.zero;
        delta += Controls.PadLook() * padSpeed * Time.deltaTime;   // controller right stick
        delta *= GameSettings.MouseSensitivity;
        if (GameSettings.InvertY) delta.y = -delta.y;
        // Look back while held (not inside a locker): the head swings round, the body doesn't turn.
        bool lookBack = Controls.Held(GameAction.TurnAround) && !limitYaw;
        headYaw = Mathf.MoveTowards(headYaw, lookBack ? 180f : 0f, 180f / turnTime * Time.deltaTime);
        if (headYaw > 0f)
        {
            // Sideways mouse movement only glances; it never changes the running direction.
            glance = lookBack ? Mathf.Clamp(glance + delta.x, -glanceRange, glanceRange) : Mathf.MoveTowards(glance, 0f, 180f / turnTime * Time.deltaTime);
        }
        else
        {
            glance = 0f;
            transform.Rotate(0f, delta.x, 0f);
        }
        if (limitYaw)
        {
            float offset = Mathf.Clamp(Mathf.DeltaAngle(yawCentre, transform.eulerAngles.y), -yawRange, yawRange);
            transform.rotation = Quaternion.Euler(0f, yawCentre + offset, 0f);
        }
        pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
        Apply();
    }

    void Apply()
    {
        // A rumble shakes the view with a quick, uneven wobble.
        float t = Time.time * 22f;
        float shakePitch = Shake * (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f, shakeYaw = Shake * (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
        cameraPivot.localRotation = Quaternion.Euler(pitch + shakePitch, headYaw + glance + shakeYaw, roll);
    }

    // Kicks the view upward (used by the shotgun's recoil).
    public void AddRecoil(float upDegrees, float sideDegrees)
    {
        pitch = Mathf.Clamp(pitch - upDegrees, -85f, 85f);
        transform.Rotate(0f, sideDegrees, 0f);
        Apply();
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
