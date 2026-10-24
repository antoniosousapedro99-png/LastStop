using UnityEngine;

// Things change when you look away: once the player has seen this object, and later has gone at least
// 'awayDistance' metres off and isn't looking at it, it quietly moves (a chair turned to face the door,
// a lantern on another crate). It happens once.
public class LookAwayChange : MonoBehaviour
{
    public Transform target;               // what changes (this object if empty)
    public Vector3 newPosition;            // world position after the change
    public Vector3 faceTowards;            // it turns to look at this point (horizontal turn only)
    public float seenDistance = 8f;        // the player has to have seen it from this close first
    public float awayDistance = 10f;
    public float viewAngle = 40f;          // degrees from the middle of the view that count as looking

    bool seen, done;
    Transform player;
    Camera view;

    void Awake() { if (!target) target = transform; }

    void Update()
    {
        if (done) return;
        if (!player)
        {
            var inv = FindAnyObjectByType<PlayerInventory>();
            if (!inv) return;
            player = inv.transform;
            view = inv.GetComponentInChildren<Camera>();
        }
        if (!view || !view.isActiveAndEnabled) return;
        float distance = Vector3.Distance(player.position, target.position);
        bool looking = Vector3.Angle(view.transform.forward, target.position - view.transform.position) < viewAngle;
        if (!seen) { seen = looking && distance < seenDistance; return; }
        if (distance < awayDistance || looking) return;

        target.position = newPosition;
        // Turn round the vertical axis so its front ('frontYaw', the way it faced when built) points at 'faceTowards'.
        Vector3 to = faceTowards - newPosition; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
        {
            float wanted = Quaternion.LookRotation(to).eulerAngles.y;
            target.rotation = Quaternion.Euler(0f, wanted - frontYaw, 0f) * target.rotation;
        }
        done = true;
    }

    public float frontYaw;                 // world direction (degrees) its front faced when it was placed
}
