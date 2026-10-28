using System.Collections;
using UnityEngine;

// A torn-down light hanging by its wires: dead most of the time, then every few seconds it shorts out,
// stuttering on and off for a moment with a crackle and a shower of sparks, giving brief glimpses of
// what's around it.
public class SparkingLight : MonoBehaviour
{
    public Light lamp;
    public Renderer tube;                       // the fitting's glowing tube
    public Color tubeGlow = Color.white;
    public ParticleSystem sparks;
    public AudioSource crackle;
    public float minWait = 2.5f, maxWait = 7f;  // seconds of darkness between shorts
    public float minBurst = 0.25f, maxBurst = 0.7f;

    static readonly int ColourId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock block;

    void Start()
    {
        block = new MaterialPropertyBlock();
        Set(false);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            if (sparks) sparks.Emit(Random.Range(8, 18));
            if (crackle) { crackle.pitch = Random.Range(0.85f, 1.15f); crackle.Play(); }
            // Rapid, uneven flicker for the length of the short.
            float end = Time.time + Random.Range(minBurst, maxBurst);
            while (Time.time < end)
            {
                Set(Random.value < 0.55f);
                yield return new WaitForSeconds(Random.Range(0.02f, 0.09f));
            }
            Set(false);
        }
    }

    void Set(bool on)
    {
        lamp.enabled = on;
        if (!tube) return;
        block.SetColor(ColourId, on ? tubeGlow : Color.black);
        tube.SetPropertyBlock(block);
    }
}
