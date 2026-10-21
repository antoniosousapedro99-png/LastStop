using UnityEngine;
using UnityEngine.InputSystem;

// Sits on the camera. Every frame it casts a short invisible ray from the centre of the view.
// - If it hits something usable (IInteractable), a prompt shows and the LEFT MOUSE BUTTON uses it
//   (like Amnesia). While the shotgun is aimed, left mouse fires instead, so nothing is used.
// - If it hits something with writing on it (ReadableNote), the text appears on screen while you
//   look at it; no click needed.
public class Interactor : MonoBehaviour
{
    public float reach = 2.5f;              // metres: arm's length plus a step
    public PlayerInventory inventory;

    public IInteractable Target { get; private set; }

    // Fired whenever the player uses something (the arms play a quick grab).
    public static event System.Action Interacted;

    void Update()
    {
        Target = null;
        string reading = null;
        if (PlayerRay.Cast(transform, transform.position, transform.forward, reach, out var hit, includeTriggers: true))
        {
            var note = hit.collider.GetComponentInParent<ReadableNote>();
            if (note != null) reading = note.text;
            var found = hit.collider.GetComponentInParent<IInteractable>();
            if (found != null && found.Prompt != null) Target = found;
        }

        bool aiming = Shotgun.IsAiming;
        var mouse = Mouse.current;
        if (!aiming && Target != null && mouse != null && mouse.leftButton.wasPressedThisFrame
            && Cursor.lockState == CursorLockMode.Locked)
        {
            Target.Interact(this);
            Interacted?.Invoke();
        }

        Hud.SetPrompt(!aiming && Target != null ? "[LMB] " + Target.Prompt : null);
        Hud.SetReading(aiming ? null : reading);
    }
}
