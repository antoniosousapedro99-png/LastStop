using UnityEngine;

// The player's own breathing.
// - Out of breath: follows PlayerMovement's sprint stamina; heavy breathing fades in as it runs low
//   and dies away as it refills.
// - Hiding in a locker: slow, held breathing instead, louder the closer Frankie is.
// (Breathing while hurt is PlayerHealth's job.)
public class PlayerBreathing : MonoBehaviour
{
    public AudioSource heavy;              // looping heavy breathing
    public AudioSource slow;               // looping slow breathing
    public float heavyVolume = 0.55f;
    public float slowVolume = 0.3f, slowVolumeFrankieNear = 0.8f;
    public float frankieNearDistance = 10f;   // within this, the held breath gets louder (and shakier)

    PlayerMovement movement;
    PlayerHealth health;
    float exertion;                        // 0 rested .. 1 completely out of breath

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // Follows the sprint stamina: the emptier it is, the harder they breathe (fully winded = loudest).
        exertion = movement ? 1f - movement.Stamina : 0f;

        bool hiding = HidingLocker.Occupied;
        bool dead = health && health.Dead;
        // Heavy breathing only once properly winded (above a quarter), so a short dash stays quiet.
        float heavyTarget = hiding || dead ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, exertion)) * heavyVolume;
        float slowTarget = 0f;
        if (hiding && !dead)
        {
            var frankie = FrankieAI.Active;
            float near = frankie ? 1f - Mathf.InverseLerp(2f, frankieNearDistance, Vector3.Distance(frankie.transform.position, transform.position)) : 0f;
            slowTarget = Mathf.Lerp(slowVolume, slowVolumeFrankieNear, near);
        }
        Fade(heavy, heavyTarget, 0.6f);
        Fade(slow, slowTarget, 0.8f);
    }

    // Eases a loop toward its target volume (per second), starting and stopping it as needed.
    static void Fade(AudioSource source, float target, float speed)
    {
        if (!source) return;
        source.volume = Mathf.MoveTowards(source.volume, target, speed * Time.deltaTime);
        if (source.volume > 0.001f && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Stop();
    }
}
