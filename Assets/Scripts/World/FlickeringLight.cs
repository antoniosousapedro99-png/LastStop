using UnityEngine;

// Makes a fluorescent lamp stutter: mostly on, with sudden bursts of rapid flicker and short blackouts.
// The glowing tube and the buzzing sound follow the light, so all three die out together.
public class FlickeringLight : MonoBehaviour
{
    public Light lamp;
    public Renderer tube;              // the glowing tube mesh (its colour is dimmed with the light)
    public AudioSource buzz;           // electrical hum that cuts out when the light does
    public Color tubeGlow = Color.white;
    public float minCalm = 1.5f, maxCalm = 6f;   // seconds of steady light between fits

    float baseIntensity, baseBuzz;
    float calmTimer, fitTimer, nextToggle;
    bool on = true;
    MaterialPropertyBlock block;
    static readonly int GlowId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        baseIntensity = lamp.intensity;
        baseBuzz = buzz ? buzz.volume : 0f;
        block = new MaterialPropertyBlock();
        calmTimer = Random.Range(minCalm, maxCalm);
    }

    void Update()
    {
        if (calmTimer > 0f)
        {
            calmTimer -= Time.deltaTime;
            if (calmTimer <= 0f) fitTimer = Random.Range(0.4f, 1.8f); // start a flicker fit
            Apply(1f);
            return;
        }

        fitTimer -= Time.deltaTime;
        nextToggle -= Time.deltaTime;
        if (nextToggle <= 0f)
        {
            on = !on;
            // Off moments are short; occasionally a longer blackout.
            nextToggle = on ? Random.Range(0.02f, 0.15f) : (Random.value < 0.15f ? Random.Range(0.3f, 0.8f) : Random.Range(0.02f, 0.09f));
        }
        Apply(on ? Random.Range(0.6f, 1f) : 0.03f);

        if (fitTimer <= 0f) { calmTimer = Random.Range(minCalm, maxCalm); on = true; }
    }

    void Apply(float level)
    {
        lamp.intensity = baseIntensity * level;
        if (buzz) buzz.volume = baseBuzz * (level > 0.1f ? 1f : 0.15f);
        if (tube)
        {
            tube.GetPropertyBlock(block);
            block.SetColor(GlowId, tubeGlow * level);
            tube.SetPropertyBlock(block);
        }
    }
}
