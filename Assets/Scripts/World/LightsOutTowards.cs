using System.Collections;
using UnityEngine;

// Scare: down a long corridor the bulbs go out one by one, from the far end toward the player (each with a
// pop), the last one right overhead. A few seconds of darkness, then they stutter back on together.
public class LightsOutTowards : MonoBehaviour, IScare
{
    public Light[] lights;                 // far to near
    public Renderer[] bulbs;               // the glowing bulb of each light, same order
    public AudioClip pop;
    public float interval = 0.45f;         // seconds between one bulb and the next
    public float darkFor = 4f;

    static readonly int ColourId = Shader.PropertyToID("_BaseColor");

    public void Play(Transform player) => StartCoroutine(Run());

    IEnumerator Run()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            Set(i, false);
            if (pop) AudioSource.PlayClipAtPoint(pop, lights[i].transform.position, 0.8f);
            yield return new WaitForSeconds(interval);
        }
        yield return new WaitForSeconds(darkFor);
        // Stutter back on, all together.
        foreach (float wait in new[] { 0.08f, 0.15f, 0.06f, 0.3f, 0.1f })
        {
            for (int i = 0; i < lights.Length; i++) Set(i, !lights[i].enabled);
            yield return new WaitForSeconds(wait);
        }
        for (int i = 0; i < lights.Length; i++) Set(i, true);
    }

    void Set(int i, bool on)
    {
        lights[i].enabled = on;
        if (i >= bulbs.Length || !bulbs[i]) return;
        var block = new MaterialPropertyBlock();
        if (!on) block.SetColor(ColourId, Color.black);
        bulbs[i].SetPropertyBlock(on ? null : block);
    }
}
