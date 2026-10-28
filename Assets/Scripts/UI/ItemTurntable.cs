using UnityEngine;

// A little stage far below the level where one item model at a time slowly turns in front of its
// own camera. The inventory screen shows that camera's picture in its preview box.
// Each child is one item's model, named after its Pickup.Kind (e.g. "Flare"). Turns with
// unscaled time, so it keeps spinning while the game is paused.
public class ItemTurntable : MonoBehaviour
{
    public Camera previewCamera;
    public float degreesPerSecond = 40f;
    public string[] faceForward = { "Photo" };   // flat things (the family photo) sway gently facing the camera instead
    public float swayDegrees = 14f, swaySpeed = 0.8f;

    Transform shown;

    // Shows the model for this kind of item (or nothing) and switches the camera on or off.
    public void Show(string kind)
    {
        Transform next = kind == null ? null : transform.Find(kind);
        if (next != shown)
        {
            if (shown) shown.gameObject.SetActive(false);
            shown = next;
            if (shown) shown.gameObject.SetActive(true);
        }
        previewCamera.enabled = kind != null;
    }

    void Update()
    {
        if (!shown) return;
        if (System.Array.IndexOf(faceForward, shown.name) >= 0)
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * swaySpeed) * swayDegrees, 0f);
        else
            transform.Rotate(0f, degreesPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
    }
}
