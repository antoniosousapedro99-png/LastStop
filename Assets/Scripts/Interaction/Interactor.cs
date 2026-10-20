using UnityEngine;
using UnityEngine.InputSystem;

// Sits on the camera. Every frame it casts a short invisible ray from the centre of the view;
// if it hits something interactable, the prompt is shown and E uses it.
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
        if (PlayerRay.Cast(transform, transform.position, transform.forward, reach, out var hit, includeTriggers: true))
        {
            var found = hit.collider.GetComponentInParent<IInteractable>();
            if (found != null && found.Prompt != null) Target = found;
        }

        var keyboard = Keyboard.current;
        if (Target != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            Target.Interact(this);
            Interacted?.Invoke();
        }

        Hud.SetPrompt(Target != null ? "[E] " + Target.Prompt : null);
    }
}
