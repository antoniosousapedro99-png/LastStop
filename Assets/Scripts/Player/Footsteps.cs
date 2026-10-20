using UnityEngine;

// Plays a random footstep sound every time the player has walked a stride.
// Running makes strides come faster simply because the distance is covered sooner; crouching is quieter.
public class Footsteps : MonoBehaviour
{
    public PlayerMovement movement;
    public AudioClip[] steps;
    public float strideLength = 1.6f;   // metres between footstep sounds
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

        // Pick a different clip than last time and vary pitch a little, so it never sounds looped.
        int index = Random.Range(0, steps.Length);
        if (steps.Length > 1 && index == lastIndex) index = (index + 1) % steps.Length;
        lastIndex = index;
        audioSource.pitch = Random.Range(0.92f, 1.06f);
        // Crouched steps are softer: you're placing your feet carefully.
        audioSource.PlayOneShot(steps[index], movement.IsCrouched ? volume * 0.5f : volume);
    }
}
