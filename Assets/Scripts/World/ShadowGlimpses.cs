using UnityEngine;

// The entity under Line 4 shows itself only like this: every few minutes, a black human shape stands at
// the edge of the player's view, against a lit wall at the edge of the torchlight or a lamp's glow, facing
// them. The moment they turn to look at it, it's gone, silently, as if it was never there. It also goes
// after a few seconds, or if they walk up to it. Underground only, and never while Frankie is chasing,
// attacking or in view, or while the player hides in a locker.
public class ShadowGlimpses : MonoBehaviour
{
    public GameObject figure;                  // the silhouette (no collider), moved to each new spot
    public float minGap = 180f, maxGap = 300f; // seconds between glimpses (3-5 minutes)
    public float retryAfter = 5f;              // no good spot found: try again this soon
    public float minAngle = 22f, maxAngle = 40f;   // degrees to the side of the view's centre: the corner of your eye
    public float minDistance = 5f, maxDistance = 16f;
    public float lookAngle = 12f;              // within this many degrees of the view's centre counts as looking at it
    public float maxSeconds = 4f;              // it never stays longer than this
    public float tooClose = 4f;

    Transform player;
    Camera view;
    Flashlight torch;
    float nextTry, shownAt;

    void Start()
    {
        figure.SetActive(false);
        nextTry = Time.time + Random.Range(minGap, maxGap);
    }

    void Update()
    {
        if (!player)
        {
            var move = FindAnyObjectByType<PlayerMovement>();
            if (!move) return;
            player = move.transform;
            view = player.GetComponentInChildren<Camera>();
            torch = player.GetComponentInChildren<Flashlight>();
        }
        if (figure.activeSelf) { Watch(); return; }
        if (Time.time < nextTry) return;
        nextTry = Time.time + (Allowed() && TryShow() ? Random.Range(minGap, maxGap) : retryAfter);
    }

    // Is now a moment the entity may show itself?
    bool Allowed()
    {
        if (!view || !view.isActiveAndEnabled || player.position.z < 0.5f) return false;   // the street (z below 0) is above ground
        if (HidingLocker.Occupied) return false;
        var frankie = FrankieAI.Active;
        if (frankie && frankie.isActiveAndEnabled)
        {
            var mood = frankie.Current;
            if (mood == FrankieAI.Mood.Chase || mood == FrankieAI.Mood.Attack || mood == FrankieAI.Mood.BreakIn || frankie.SeenByPlayer) return false;
        }
        return true;
    }

    // Looks for a spot off to one side of the view, on a floor near the player's own height, with a clear
    // line of sight to it and lit ground or wall behind it (so the black shape stands out), and shows it there.
    bool TryShow()
    {
        Vector3 eye = view.transform.position;
        Vector3 ahead = view.transform.forward; ahead.y = 0f;
        if (ahead.sqrMagnitude < 0.01f) return false;
        ahead.Normalize();
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 dir = Quaternion.Euler(0f, side * Random.Range(minAngle, maxAngle), 0f) * ahead;
            Vector3 guess = player.position + dir * Random.Range(minDistance, maxDistance);
            // Floor under it at the player's own level (down on the tracks it would be lost in the dark pit).
            if (!Physics.Raycast(guess + Vector3.up * 1.5f, Vector3.down, out var floor, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(floor.point.y - player.position.y) > 0.6f) continue;
            Vector3 feet = floor.point;
            // Room to stand (nothing solid where its body would be).
            if (Physics.CheckCapsule(feet + Vector3.up * 0.4f, feet + Vector3.up * 1.6f, 0.25f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
            // The player can see its chest and its head from where they are.
            Vector3 chest = feet + Vector3.up * 1.3f;
            if (Blocked(eye, chest) || Blocked(eye, feet + Vector3.up * 1.7f)) continue;
            // Something lit behind it, or it would be black on black.
            if (!LitBehind(eye, chest) && !LitBehind(eye, feet + Vector3.up * 0.5f)) continue;   // behind its body or its legs
            Vector3 face = eye - feet; face.y = 0f;
            figure.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(face));
            figure.SetActive(true);
            shownAt = Time.time;
            return true;
        }
        return false;
    }

    // Gone the moment it's looked at, after a few seconds anyway, or if the player gets close.
    void Watch()
    {
        Vector3 chest = figure.transform.position + Vector3.up * 1.3f;
        bool looked = Vector3.Angle(view.transform.forward, chest - view.transform.position) < lookAngle;
        bool close = Vector3.Distance(player.position, figure.transform.position) < tooClose;
        if (looked || close || Time.time - shownAt > maxSeconds || !Allowed()) figure.SetActive(false);
    }

    static bool Blocked(Vector3 from, Vector3 to) =>
        Physics.Raycast(from, to - from, Vector3.Distance(from, to) - 0.2f, ~(1 << 2), QueryTriggerInteraction.Ignore);

    // The surface seen behind a point on the figure (straight past it from the player's eye) is lit by the
    // torch's wide spill or by a working lamp near it.
    bool LitBehind(Vector3 eye, Vector3 point)
    {
        Vector3 dir = (point - eye).normalized;
        if (!Physics.Raycast(point, dir, out var back, 15f, ~(1 << 2), QueryTriggerInteraction.Ignore)) return false;
        // Inside the torch's wide spill: close enough, and within its cone.
        if (torch && torch.IsOn)
        {
            Transform beam = torch.spill ? torch.spill.transform : torch.transform;
            Vector3 fromTorch = back.point - beam.position;
            if (fromTorch.magnitude < 16f && Vector3.Angle(beam.forward, fromTorch) < 42f) return true;
        }
        foreach (var lamp in FindObjectsByType<Light>())
        {
            if (!lamp.isActiveAndEnabled || lamp.type == LightType.Directional || lamp.intensity <= 0f) continue;
            if (lamp.transform.IsChildOf(player)) continue;
            if (Vector3.Distance(lamp.transform.position, back.point) < lamp.range * 0.5f) return true;
        }
        return false;
    }
}
