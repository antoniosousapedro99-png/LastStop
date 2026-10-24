using UnityEngine;

// A candle or lantern flame: the light wavers gently and unevenly.
[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    public float amount = 0.35f;           // how much the brightness dips (fraction)
    public float speed = 6f;

    Light flame;
    float baseIntensity, seed;

    void Awake()
    {
        flame = GetComponent<Light>();
        baseIntensity = flame.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 0.7f + Mathf.PerlinNoise(seed + 7f, Time.time * speed * 3f) * 0.3f;
        flame.intensity = baseIntensity * (1f - amount + amount * n * 1.4f);
    }
}
