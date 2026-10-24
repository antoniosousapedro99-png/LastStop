using System.Collections;
using UnityEngine;

// Scare: sounds with no source. A short run of sounds from one spot, e.g. slow footsteps crossing the
// floor above, a cough from an empty room, a whisper behind you. Set off by a ScareTrigger.
public class SoundSequence : MonoBehaviour, IScare
{
    public AudioSource source;             // where the sounds come from
    public AudioClip[] clips;              // played in turn (looping round the list)
    public int count = 1;                  // how many sounds in the run
    public float delay = 0.5f;             // before the first one
    public float gap = 0.7f;               // between sounds
    public Vector2 pitch = new Vector2(0.95f, 1.05f);
    public float volume = 1f;
    public Vector3 drift;                  // the source moves by this much over the run (footsteps walking)

    public void Play(Transform player) => StartCoroutine(Run());

    IEnumerator Run()
    {
        yield return new WaitForSeconds(delay);
        Vector3 start = source.transform.position;
        for (int i = 0; i < count; i++)
        {
            source.transform.position = start + drift * (count > 1 ? i / (float)(count - 1) : 0f);
            source.pitch = Random.Range(pitch.x, pitch.y);
            source.PlayOneShot(clips[i % clips.Length], volume);
            yield return new WaitForSeconds(gap);
        }
    }
}
