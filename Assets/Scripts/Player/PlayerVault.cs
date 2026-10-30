using System.Collections;
using UnityEngine;

// Vaulting: hold Jump while facing a ledge between knee and chest height (0.5 to 1.55 m: the platform edge
// from the tracks, crates, low walls) and the player heaves themselves up onto it in 0.7 s.
// A quick tap of Jump at a ledge is still an ordinary jump (PlayerMovement does it when the key comes up).
// It can't be done while winded (out of sprint stamina), and it's loud: Frankie hears it like running.
[RequireComponent(typeof(CharacterController))]
public class PlayerVault : MonoBehaviour
{
    public float minHeight = 0.5f, maxHeight = 1.55f;   // ledge heights it works on, above the feet
    public float reach = 0.8f;                          // how far ahead a ledge can be
    public float holdTime = 0.25f;                      // seconds Jump must be held to vault
    public float duration = 0.7f;
    public float noise = 15f;                           // metres it carries (same as running)
    public AudioSource voice;
    public AudioClip[] efforts;                         // the grunt as you heave up
    public AudioClip landing;

    public bool Vaulting { get; private set; }
    // A ledge you could vault onto right now (PlayerMovement holds back the jump while this is true).
    public bool LedgeAhead => Ready && FindLedge(out _);

    CharacterController controller;
    PlayerMovement movement;
    PlayerLook look;
    float held;
    bool pressedAtLedge;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
    }

    bool Ready => !Vaulting && movement.enabled && controller.enabled && controller.isGrounded && !movement.Winded;

    void Update()
    {
        if (Vaulting) return;
        if (Controls.Pressed(GameAction.Jump)) { held = 0f; pressedAtLedge = LedgeAhead; }
        if (!pressedAtLedge) return;
        if (Controls.Held(GameAction.Jump))
        {
            held += Time.deltaTime;
            if (held < holdTime) return;
            pressedAtLedge = false;
            if (Ready && FindLedge(out var landAt)) StartCoroutine(Vault(landAt));
        }
        else
        {
            pressedAtLedge = false;
            movement.QueueJump();          // only a tap: a jump after all
        }
    }

    // Is there a ledge of the right height straight ahead, with room to stand on top? 'landAt' is where the
    // feet end up, a little past its edge.
    bool FindLedge(out Vector3 landAt)
    {
        landAt = Vector3.zero;
        Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 feet = transform.position;
        const QueryTriggerInteraction noTriggers = QueryTriggerInteraction.Ignore;
        // Something in front at knee height, faced roughly head-on.
        if (!Physics.Raycast(feet + Vector3.up * 0.35f, forward, out var wall, reach + controller.radius, ~0, noTriggers)) return false;
        if (Vector3.Dot(wall.normal, -forward) < 0.5f) return false;
        // Its top: look down from above, just past the face.
        Vector3 above = wall.point + forward * 0.3f;
        above.y = feet.y + maxHeight + 0.5f;
        if (!Physics.Raycast(above, Vector3.down, out var top, maxHeight + 0.5f, ~0, noTriggers)) return false;
        float height = top.point.y - feet.y;
        if (height < minHeight || height > maxHeight || top.normal.y < 0.7f) return false;
        // Room for the body up there.
        landAt = top.point + forward * 0.25f;
        float r = controller.radius * 0.9f;
        return !Physics.CheckCapsule(landAt + Vector3.up * (r + 0.05f), landAt + Vector3.up * (controller.height - r), r, ~0, noTriggers);
    }

    IEnumerator Vault(Vector3 landAt)
    {
        Vaulting = true;
        movement.enabled = false;
        controller.enabled = false;
        Noise.Make(transform.position, noise);
        if (voice && efforts.Length > 0) voice.PlayOneShot(efforts[Random.Range(0, efforts.Length)], 0.8f);
        if (look) look.AddRecoil(-4f, 0f);         // the head dips as you heave

        // Up to the ledge's height (and a little forward), then over the edge onto it.
        Vector3 start = transform.position;
        Vector3 forward = landAt - start; forward.y = 0f; forward.Normalize();
        Vector3 up = new Vector3(start.x, landAt.y + 0.05f, start.z) + forward * 0.15f;
        float rise = duration * 0.6f, over = duration - rise;
        for (float t = 0f; t < rise; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, up, Mathf.SmoothStep(0f, 1f, t / rise));
            yield return null;
        }
        for (float t = 0f; t < over; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(up, landAt + Vector3.up * 0.02f, Mathf.SmoothStep(0f, 1f, t / over));
            yield return null;
        }
        transform.position = landAt + Vector3.up * 0.02f;
        if (voice && landing) voice.PlayOneShot(landing, 0.7f);
        controller.enabled = true;
        movement.enabled = true;
        Vaulting = false;
    }
}
