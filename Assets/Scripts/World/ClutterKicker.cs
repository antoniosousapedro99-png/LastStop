using UnityEngine;
using UnityEngine.AI;

// Lets whoever carries it (Sousa, Frankie) knock loose clutter (Kickable) about with their feet: anything
// touching their legs as they move is sent off in the direction they're walking, faster the faster they go.
// Small things skitter ahead (a can goes a metre or two at a walk, further at a run); medium things are
// shoved along at walking pace. Sousa crouched only nudges things, quietly; standing, his kicks are loud
// enough for Frankie to hear. This object's own colliders are told to pass through clutter it should kick
// rather than bump (all of it for Frankie, the small things for Sousa), so the push here decides how it moves.
// Assumes the object's position is at its feet.
public class ClutterKicker : MonoBehaviour
{
    public float reach = 0.5f;          // how far round the feet clutter is caught (a little more than the body's width)
    public float legHeight = 0.6f;      // up to the knee
    public bool isPlayer;               // loud kicks (Frankie hears them); crouching makes them gentle
    public bool passThroughAll;         // Frankie: walks through crates too, shoving them aside

    CharacterController controller;
    NavMeshAgent agent;
    PlayerMovement movement;
    readonly Collider[] caught = new Collider[16];

    void Start()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        movement = GetComponent<PlayerMovement>();
        var mine = GetComponentsInChildren<Collider>(true);
        foreach (var clutter in FindObjectsByType<Kickable>(FindObjectsInactive.Include))
        {
            if (!clutter.small && !passThroughAll) continue;
            foreach (var theirs in clutter.GetComponentsInChildren<Collider>(true))
                foreach (var own in mine) Physics.IgnoreCollision(own, theirs);
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = controller ? controller.velocity : agent ? agent.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = Mathf.Min(velocity.magnitude, 8f);   // never more than a sprint (a teleport isn't a kick)
        if (speed < 0.3f) return;
        Vector3 heading = velocity / velocity.magnitude;

        Vector3 feet = transform.position;
        // A box from the floor to the knee round the feet, reaching a little further in front than behind.
        Vector3 middle = feet + heading * (reach * 0.3f) + Vector3.up * (legHeight * 0.5f - 0.05f);
        int count = Physics.OverlapBoxNonAlloc(middle, new Vector3(reach, legHeight * 0.5f + 0.05f, reach), caught,
                                               Quaternion.LookRotation(heading), ~0, QueryTriggerInteraction.Ignore);
        bool crouched = isPlayer && movement && movement.IsCrouched;
        for (int i = 0; i < count; i++)
        {
            var body = caught[i].attachedRigidbody;
            if (!body || body.isKinematic) continue;
            var clutter = body.GetComponent<Kickable>();
            if (!clutter) continue;
            Vector3 centre = body.worldCenterOfMass;
            Vector3 under = caught[i].ClosestPoint(feet) - feet; under.y = 0f;
            if (under.magnitude < 0.2f && caught[i].bounds.max.y < feet.y + 0.08f) continue;   // underfoot (standing on it): leave it be
            Vector3 away = centre - feet; away.y = 0f;
            if (away.sqrMagnitude > 0.0001f && Vector3.Dot(away.normalized, heading) < -0.2f) continue;   // behind: walking away from it
            if (away.sqrMagnitude > 0.0001f) away.Normalize();

            // Mostly the way they're walking, partly out to the side it was touched on (so it leaves their path
            // instead of being pushed along ahead of them); small things hop a bit.
            Vector3 dir = (heading * 0.6f + away * 0.4f).normalized;
            if (clutter.small) dir = (dir + Vector3.up * 0.15f).normalized;
            // Even a slow walk sends small things off properly (Frankie walks slowly); crouched, only a nudge.
            float pace = crouched ? speed * 0.4f : clutter.small ? Mathf.Max(speed, 1.5f) : speed;
            float target = pace * clutter.kickScale * Random.Range(0.85f, 1.15f);
            float already = Vector3.Dot(body.linearVelocity, dir);
            if (already >= target) continue;   // already on its way

            bool resting = already < 0.4f;
            // Struck at shin height, so tall light things (cones, bins) tip and cans spin.
            Vector3 at = caught[i].ClosestPoint(feet + Vector3.up * Mathf.Min(0.25f, legHeight));
            clutter.Kick(dir * (target - already), at, isPlayer && !crouched, crouched || !resting);
        }
    }
}
