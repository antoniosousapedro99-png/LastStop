using System.Collections;
using UnityEngine;

// Scare: a train that never comes. A deep rumble rolls through the tunnel, the view shakes a little,
// and dust trickles down from the ceiling around the player.
public class TrainRumble : MonoBehaviour, IScare
{
    public AudioSource rumble;
    public ParticleSystem dust;
    public float ceilingY = 2.9f;          // where the dust falls from
    public float seconds = 5f;
    public float shake = 1.2f;             // degrees of view shake at the peak

    public void Play(Transform player)
    {
        if (rumble) rumble.Play();
        if (dust)
        {
            dust.transform.position = new Vector3(player.position.x, ceilingY, player.position.z);
            dust.Play();
        }
        StartCoroutine(Shake());
    }

    // The shake swells and fades with the sound.
    IEnumerator Shake()
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            PlayerLook.Shake = shake * Mathf.Sin(Mathf.PI * t / seconds);
            yield return null;
        }
        PlayerLook.Shake = 0f;
    }
}
