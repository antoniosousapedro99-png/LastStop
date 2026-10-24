using UnityEngine;

// What the Pale Man notices.
// - Sight: the player within 'sightRange' metres (further while the torch is on, because the light gives
//   them away), inside a wide cone in front of him, with nothing solid in between.
// - Hearing: any Noise made close enough (running carries far, walking less, crouching makes none).
public class PaleManSenses : MonoBehaviour
{
    public Transform eyes;                  // a point at head height
    public float sightRange = 15f;
    public float torchSightRange = 25f;
    public float sightAngle = 120f;         // degrees, the whole cone

    public Transform Player { get; private set; }
    public bool HasHeard { get; private set; }
    public Vector3 HeardAt { get; private set; }

    Flashlight torch;

    void OnEnable() => Noise.Made += OnNoise;
    void OnDisable() => Noise.Made -= OnNoise;

    void FindPlayer()
    {
        if (Player) return;
        var go = GameObject.Find("Player");
        if (go) { Player = go.transform; torch = go.GetComponentInChildren<Flashlight>(); }
    }

    // The player's head, for line-of-sight checks.
    public Vector3 PlayerHead => Player.position + Vector3.up * 1.5f;

    public bool CanSeePlayer()
    {
        FindPlayer();
        if (!Player || !Player.gameObject.activeInHierarchy) return false;
        if (HidingLocker.Occupied) return false;   // hidden in a locker
        Vector3 to = PlayerHead - eyes.position;
        float range = torch && torch.IsOn ? torchSightRange : sightRange;
        if (to.magnitude > range) return false;
        if (Vector3.Angle(transform.forward, new Vector3(to.x, 0f, to.z)) > sightAngle * 0.5f) return false;
        // Walls, the train, crates... anything solid that isn't the player blocks the view.
        if (Physics.Raycast(eyes.position, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(Player);
        return true;
    }

    // The other way round: is he on the player's screen (near the middle of the view, within 30 m, with
    // nothing solid between the camera and his chest)? Used for his first appearance and the torch flicker.
    public bool PlayerCanSeeMe()
    {
        var cam = Camera.main;
        if (!cam || !cam.isActiveAndEnabled) return false;
        Vector3 chest = transform.position + Vector3.up * 1.3f;
        Vector3 to = chest - cam.transform.position;
        if (to.magnitude > 30f || Vector3.Angle(cam.transform.forward, to) > 35f) return false;
        if (Physics.Raycast(cam.transform.position, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(transform) || (Player && hit.collider.transform.IsChildOf(Player));
        return true;
    }

    void OnNoise(Vector3 where, float carries)
    {
        if (Vector3.Distance(where, transform.position) > carries) return;
        HasHeard = true;
        HeardAt = where;
    }

    public void ForgetNoise() => HasHeard = false;
}
