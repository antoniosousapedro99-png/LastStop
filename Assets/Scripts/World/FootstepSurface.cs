using UnityEngine;

// Put on a floor to give it its own footstep sounds (e.g. wet gravel on the track bed).
// Floors without one use the player's default (concrete) footsteps.
public class FootstepSurface : MonoBehaviour
{
    public AudioClip[] steps;
    public float volumeScale = 1f;
}
