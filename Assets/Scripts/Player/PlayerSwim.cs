using UnityEngine;

// Water for the player, from the flood zones (FloodZone):
// - Wading (water from the ankles up to the chest): walking slows the deeper it gets (Footsteps splash).
// - Swimming (water about chest-deep or more): PlayerMovement steps aside and this moves the body instead,
//   slowly, floating with the eyes just above the surface. Hold Crouch to dive, Jump to come back up;
//   currents (WaterCurrent) drag him along and under. Swimming at a ledge no higher than his shoulders,
//   forward pulls him up and out (a barrier, a walkway, a room's floor).
[RequireComponent(typeof(CharacterController))]
public class PlayerSwim : MonoBehaviour
{
    public float swimDepth = 1.15f;        // water this deep at the feet: swimming (the cistern's 1.3 m swims, the waist-deep 1 m wades)
    public float wadeFrom = 0.15f;         // shallower than this doesn't slow him
    public float swimSpeed = 1.6f, diveSpeed = 1.3f, riseSpeed = 1.5f, floatSpeed = 1f;
    public float floatDepth = 1.45f;       // floating: the feet this far under the surface (eyes just above it)
    public AudioSource source;             // strokes and splashes (2D: they're his own)
    public AudioClip[] strokes;

    public static PlayerSwim Current { get; private set; }
    public bool Swimming { get; private set; }
    public float Depth { get; private set; }   // water above the feet (0 when dry)
    public float Surface { get; private set; }

    CharacterController controller;
    PlayerMovement movement;
    PlayerCrouch crouch;
    PlayerVault vault;
    PlayerHealth health;
    bool tookOver;                         // we switched walking off (so we switch it back on)
    float strokeDistance;
    Vector3 climbFrom, climbTo;
    float climbT = -1f;

    void Awake()
    {
        Current = this;
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        crouch = GetComponent<PlayerCrouch>();
        vault = GetComponent<PlayerVault>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health && health.Dead) return;
        bool inWater = FloodZone.SurfaceAt(transform.position, out float surface);
        Surface = surface;
        Depth = inWater ? Mathf.Max(0f, surface - transform.position.y) : 0f;
        movement.waterMultiplier = Swimming ? 1f : Mathf.Lerp(1f, 0.45f, Mathf.InverseLerp(wadeFrom, swimDepth, Depth));

        // A little give either way, so he doesn't flick between wading and swimming at the edge.
        bool swim = Depth >= (Swimming ? swimDepth - 0.25f : swimDepth);
        if (swim != Swimming)
        {
            if (swim && !movement.enabled) return;   // something else (a locker, a ladder, death) has him
            SetSwimming(swim);
        }
        if (!Swimming) return;
        if (climbT >= 0f) { Climb(); return; }

        Vector2 input = Controls.Move();
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * swimSpeed;
        float floatY = surface - floatDepth, y = transform.position.y;
        float vertical;
        if (SewerValve.Busy) vertical = 0f;                                  // holding on to a valve wheel
        else if (Controls.Held(GameAction.Crouch)) vertical = -diveSpeed;    // dive
        else if (Controls.Held(GameAction.Jump)) vertical = y < floatY ? riseSpeed : 0f;
        else vertical = Mathf.Clamp((floatY - y) * 2f, -floatSpeed, floatSpeed);   // bob back to the surface

        Vector3 v = move + WaterCurrent.At(transform.position + Vector3.up * 0.9f);
        v.y += vertical;
        if (v.y > 0f && y + v.y * Time.deltaTime > floatY) v.y = Mathf.Max(0f, (floatY - y) / Time.deltaTime);   // never out of the water
        controller.Move(v * Time.deltaTime);

        // Strokes: a soft splash every metre and a half at the surface, muffled bubbling strokes below it.
        Vector3 flat = controller.velocity; flat.y = 0f;
        strokeDistance += flat.magnitude * Time.deltaTime;
        if (strokeDistance > 1.5f && strokes != null && strokes.Length > 0)
        {
            strokeDistance = 0f;
            bool atSurface = y > floatY - 0.3f;
            source.pitch = Random.Range(atSurface ? 0.95f : 0.6f, atSurface ? 1.1f : 0.7f);
            source.PlayOneShot(strokes[Random.Range(0, strokes.Length)], atSurface ? 0.25f : 0.12f);
            if (atSurface) Noise.Make(transform.position, 6f);
        }

        if (input.y > 0.5f && y > floatY - 0.4f) TryClimb(surface);
    }

    void SetSwimming(bool on)
    {
        Swimming = on;
        if (on)
        {
            tookOver = movement.enabled;
            movement.enabled = false;
            if (crouch) { crouch.StandUp(); crouch.enabled = false; }
            if (vault) vault.enabled = false;
            if (strokes != null && strokes.Length > 0) source.PlayOneShot(strokes[0], 0.4f);   // the splash going in
        }
        else
        {
            climbT = -1f;
            if (tookOver) movement.enabled = true;
            if (crouch) crouch.enabled = true;
            if (vault) vault.enabled = true;
        }
    }

    // Out of the water: a ledge right in front, its top no more than 0.6 m above the surface and with
    // room to stand on it.
    void TryClimb(float surface)
    {
        Vector3 chest = transform.position + Vector3.up * 1.2f;
        if (!Physics.Raycast(chest, transform.forward, out var wall, 0.8f, ~0, QueryTriggerInteraction.Ignore)) return;
        Vector3 over = wall.point + transform.forward * 0.45f;
        Vector3 from = new Vector3(over.x, surface + 0.8f, over.z);
        if (!Physics.Raycast(from, Vector3.down, out var top, 1.4f, ~0, QueryTriggerInteraction.Ignore)) return;
        if (top.point.y > surface + 0.6f || top.normal.y < 0.7f) return;
        Vector3 feet = top.point + Vector3.up * 0.05f;
        if (Physics.CheckCapsule(feet + Vector3.up * 0.4f, feet + Vector3.up * 1.6f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) return;
        climbFrom = transform.position; climbTo = feet; climbT = 0f;
        if (strokes != null && strokes.Length > 1) source.PlayOneShot(strokes[1], 0.4f);
    }

    void Climb()
    {
        climbT += Time.deltaTime / 0.7f;
        float t = Mathf.Clamp01(climbT);
        // Up first, then forward over the edge.
        var p = new Vector3(Mathf.Lerp(climbFrom.x, climbTo.x, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, t))),
                            Mathf.Lerp(climbFrom.y, climbTo.y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.6f, t))),
                            Mathf.Lerp(climbFrom.z, climbTo.z, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, t))));
        controller.enabled = false;
        transform.position = p;
        controller.enabled = true;
        if (t >= 1f) { climbT = -1f; SetSwimming(false); }
    }
}
