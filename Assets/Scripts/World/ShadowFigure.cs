using UnityEngine;

// Scare: a pitch-black figure standing far down the tunnel, facing the player. The moment the player
// really looks at it (with the torch on, or from close enough to see it in the lamplight) it's gone,
// with a faint whisper. It also vanishes if they walk up close or never look its way.
public class ShadowFigure : MonoBehaviour, IScare
{
    public GameObject figure;              // switched off until the scare plays
    public AudioClip vanishSound;
    public float seeAngle = 18f;           // degrees from the middle of the view that count as looking at it
    public float seeWithoutTorch = 18f;    // metres it can be made out without the torch
    public float tooClose = 10f;
    public float maxSeconds = 25f;
    public float glimpse = 0.35f;          // it stays this long after being seen, so it registers

    Transform player;
    Camera view;
    Flashlight torch;
    float shownAt, seenAt = -1f;

    void Awake() => figure.SetActive(false);

    public void Play(Transform who)
    {
        player = who;
        view = who.GetComponentInChildren<Camera>();
        torch = who.GetComponentInChildren<Flashlight>();
        Vector3 to = player.position - figure.transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f) figure.transform.rotation = Quaternion.LookRotation(to);   // facing the player
        figure.SetActive(true);
        shownAt = Time.time;
        seenAt = -1f;
    }

    void Update()
    {
        if (!figure.activeSelf || !player) return;
        Vector3 chest = figure.transform.position + Vector3.up * 1.2f;
        float distance = Vector3.Distance(player.position, figure.transform.position);
        if (seenAt < 0f && IsSeen(chest, distance)) seenAt = Time.time;
        bool vanish = (seenAt >= 0f && Time.time - seenAt > glimpse) || distance < tooClose || Time.time - shownAt > maxSeconds;
        if (!vanish) return;
        if (vanishSound) AudioSource.PlayClipAtPoint(vanishSound, chest, 0.8f);
        figure.SetActive(false);
    }

    bool IsSeen(Vector3 chest, float distance)
    {
        if (!view) return false;
        Vector3 to = chest - view.transform.position;
        if (Vector3.Angle(view.transform.forward, to) > seeAngle) return false;
        if (!(torch && torch.IsOn) && distance > seeWithoutTorch) return false;
        // Nothing solid in between (the figure has no collider; the ray starts inside the player's own capsule,
        // which rays don't hit from inside).
        return !Physics.Raycast(view.transform.position, to.normalized, to.magnitude - 0.5f, ~(1 << 2), QueryTriggerInteraction.Ignore);
    }
}
