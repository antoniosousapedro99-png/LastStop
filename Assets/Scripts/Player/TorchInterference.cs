using UnityEngine;

// A warning: the torch stutters when the Pale Man is close (within 'range' metres) but not in view.
// The closer he is, the more often it cuts out. Once he's on screen the light holds steady so you can
// see him. Flashlight multiplies its brightness by Multiplier.
public class TorchInterference : MonoBehaviour
{
    public float range = 10f;

    public float Multiplier { get; private set; } = 1f;

    float dipUntil, nextDip, dipLevel;

    void Update()
    {
        Multiplier = 1f;
        var man = PaleManAI.Active;
        if (!man || man.SeenByPlayer) return;
        float distance = Vector3.Distance(man.transform.position, transform.position);
        if (distance > range) return;

        float closeness = 1f - distance / range;   // 0 at the edge of the range, 1 right next to him
        if (Time.time > nextDip)
        {
            // A short cut-out, then a wait before the next one: shorter waits the closer he is.
            dipUntil = Time.time + Random.Range(0.04f, 0.15f);
            dipLevel = Random.Range(0.03f, 0.3f);
            nextDip = dipUntil + Random.Range(0.1f, 1.8f) * (1.15f - closeness);
        }
        // Between cut-outs the light still wavers a little.
        Multiplier = Time.time < dipUntil ? dipLevel : 1f - closeness * 0.3f * Mathf.PerlinNoise(Time.time * 9f, 0f);
    }
}
