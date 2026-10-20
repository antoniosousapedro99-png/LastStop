using UnityEngine;

// Rays cast from the player's eyes start inside the player's own body (its capsule collider),
// so a normal raycast would just hit the player. This finds the nearest hit that isn't the player.
public static class PlayerRay
{
    static readonly RaycastHit[] hits = new RaycastHit[16];

    public static bool Cast(Transform fromInsidePlayer, Vector3 origin, Vector3 direction, float range,
                            out RaycastHit nearest, bool includeTriggers)
    {
        var self = fromInsidePlayer.root;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, range, ~0,
                                            includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
        nearest = default;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.root == self) continue;   // skip the player's own colliders
            if (hits[i].distance < best) { best = hits[i].distance; nearest = hits[i]; }
        }
        return best < float.MaxValue;
    }
}
