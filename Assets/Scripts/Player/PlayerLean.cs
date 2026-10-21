using UnityEngine;
using UnityEngine.InputSystem;

// Hold Q to lean left, E to lean right: the head slides sideways and tilts, so you can peek round
// a corner. A short check stops the head going through walls (it leans only as far as there's room).
public class PlayerLean : MonoBehaviour
{
    public Transform cameraPivot;      // the camera (its local x is moved; y belongs to crouching)
    public PlayerLook look;            // applies the tilt (roll) together with the up/down angle
    public float distance = 0.4f;      // metres the head moves sideways at full lean
    public float tilt = 12f;           // degrees of head tilt at full lean
    public float speed = 6f;           // how quickly the lean eases in and out

    float amount;                      // -1 = full left, 0 = upright, 1 = full right

    void Update()
    {
        var keyboard = Keyboard.current;
        float target = 0f;
        if (keyboard != null)
        {
            if (keyboard.qKey.isPressed) target -= 1f;
            if (keyboard.eKey.isPressed) target += 1f;
        }

        // Don't lean into a wall: check how much room there is beside the head.
        if (target != 0f)
        {
            Vector3 side = transform.right * Mathf.Sign(target);
            Vector3 head = transform.position + Vector3.up * cameraPivot.localPosition.y;
            if (Physics.SphereCast(head, 0.15f, side, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.transform.root != transform.root)
                target *= Mathf.Clamp01((hit.distance - 0.05f) / distance);
        }

        amount = Mathf.MoveTowards(amount, target, speed * Time.deltaTime);
        float eased = Mathf.Sin(amount * Mathf.PI * 0.5f);   // smooth start and stop
        var p = cameraPivot.localPosition;
        p.x = eased * distance;
        cameraPivot.localPosition = p;
        look.roll = -eased * tilt;
    }
}
