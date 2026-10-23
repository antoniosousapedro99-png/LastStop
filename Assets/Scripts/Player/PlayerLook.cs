using UnityEngine;
using UnityEngine.InputSystem;

// Mouse look: moving the mouse sideways turns the whole body, up/down tilts only the camera.
// The cursor is hidden and locked to the window (the pause menu frees it; clicking the game locks it again).
// Speed and up/down direction come from the Mouse options (GameSettings).
public class PlayerLook : MonoBehaviour
{
    public Transform cameraPivot;      // the camera (child of the player)
    public float sensitivity = 0.12f;  // degrees per pixel of mouse movement at sensitivity x1.00
    public float pitch;                // current up/down angle (public so tests can set it)
    public float roll;                 // sideways head tilt, set by PlayerLean

    void OnEnable() => LockCursor(true);

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) LockCursor(true);
        if (mouse == null || Cursor.lockState != CursorLockMode.Locked) { Apply(); return; }

        Vector2 delta = mouse.delta.ReadValue() * sensitivity * GameSettings.MouseSensitivity;
        if (GameSettings.InvertY) delta.y = -delta.y;
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
        Apply();
    }

    void Apply() => cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, roll);

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
