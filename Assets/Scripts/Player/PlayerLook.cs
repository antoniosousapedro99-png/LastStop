using UnityEngine;
using UnityEngine.InputSystem;

// Mouse look: moving the mouse sideways turns the whole body, up/down tilts only the camera.
// The cursor is hidden and locked to the window; Esc frees it, clicking the game locks it again.
public class PlayerLook : MonoBehaviour
{
    public Transform cameraPivot;      // the camera (child of the player)
    public float sensitivity = 0.12f;  // degrees per pixel of mouse movement
    public float pitch;                // current up/down angle (public so tests can set it)

    void OnEnable() => LockCursor(true);

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) LockCursor(true);
        if (mouse == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = mouse.delta.ReadValue() * sensitivity;
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Kicks the view upward (used by the shotgun's recoil).
    public void AddRecoil(float upDegrees, float sideDegrees)
    {
        pitch = Mathf.Clamp(pitch - upDegrees, -85f, 85f);
        transform.Rotate(0f, sideDegrees, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
