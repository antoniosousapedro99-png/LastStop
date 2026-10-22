using UnityEngine;

// Plays a random footstep sound every time the player has walked a stride.
// Running makes strides come faster simply because the distance is covered sooner; crouching is quieter.
// What you hear depends on the floor: a FootstepSurface (e.g. gravel on the tracks) brings its own
// sounds, and stepping in a puddle (WaterPuddle) splashes. Anything else uses the default steps.
public class Footsteps : MonoBehaviour
{
    public PlayerMovement movement;
    public AudioClip[] steps;            // default floor: concrete
    public AudioClip[] splashes;         // stepping in a puddle
    public float splashVolume = 0.3f;
    public float strideLength = 1.6f;    // metres between footstep sounds
    public float volume = 0.55f;

    AudioSource audioSource;
    float distanceSinceStep;
    int lastIndex = -1;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void Update()
    {
        if (!movement.IsGrounded || movement.CurrentSpeed < 0.3f) return;
        distanceSinceStep += movement.CurrentSpeed * Time.deltaTime;
        if (distanceSinceStep < strideLength) return;
        distanceSinceStep = 0f;

        // Work out what's underfoot.
        AudioClip[] set = steps;
        float level = volume;
        Vector3 feet = transform.position;
        if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            feet = hit.point;
            var surface = hit.collider.GetComponentInParent<FootstepSurface>();
            if (surface != null && surface.steps.Length > 0) { set = surface.steps; level *= surface.volumeScale; }
        }
        if (splashes != null && splashes.Length > 0 && WaterPuddle.At(feet)) { set = splashes; level = splashVolume; }

        // Pick a different clip than last time and vary pitch a little, so it never sounds looped.
        int index = Random.Range(0, set.Length);
        if (set.Length > 1 && index == lastIndex) index = (index + 1) % set.Length;
        lastIndex = index;
        audioSource.pitch = Random.Range(0.92f, 1.06f);
        // Crouched steps are softer: you're placing your feet carefully.
        audioSource.PlayOneShot(set[index], movement.IsCrouched ? level * 0.5f : level);
    }
}
