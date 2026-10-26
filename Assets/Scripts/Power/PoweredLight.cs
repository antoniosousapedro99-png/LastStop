using System.Collections;
using UnityEngine;

// A lamp that needs the area's power. Dark until the PowerGrid restores power, then it stutters
// on like a real fluorescent tube. A lamp marked "worksWithoutPower" (the flickering platform lamp)
// is lit from the start; when the power returns it stops flickering and burns steadily.
public class PoweredLight : MonoBehaviour
{
    public Light[] lights;                 // the actual light(s)
    public Renderer[] glowParts;           // glowing tubes/bulbs (Unlit material; colour is dimmed)
    public Color glowColour = Color.white; // tube colour when fully on (brighter than white = glow)
    public Color offColour = new Color(0.22f, 0.24f, 0.22f);
    public bool worksWithoutPower;
    public FlickeringLight flicker;        // optional: stopped once the power is back
    public PowerGrid grid;                 // which station's power it runs on (empty = Hollis St's)
    public float poweredReach = 1.5f;      // with the grid back on, lamps throw light further (range x this)
    public float poweredWiden = 15f;       // ...and spot lamps open wider by this many degrees

    float[] baseIntensity;
    MaterialPropertyBlock block;
    static readonly int ColourId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        block = new MaterialPropertyBlock();
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        if (!worksWithoutPower) SetLevel(0f);
    }

    public void PowerOn()
    {
        // Full mains power: bigger pools of light than the weak backup state.
        foreach (var l in lights)
        {
            l.range *= poweredReach;
            if (l.type == LightType.Spot) { l.spotAngle = Mathf.Min(170f, l.spotAngle + poweredWiden); l.innerSpotAngle = Mathf.Min(l.spotAngle - 5f, l.innerSpotAngle + poweredWiden); }
        }
        StartCoroutine(worksWithoutPower ? Steady() : StutterOn());
    }

    IEnumerator StutterOn()
    {
        // A few quick blinks, then on for good.
        int blinks = Random.Range(2, 5);
        for (int i = 0; i < blinks; i++)
        {
            SetLevel(Random.Range(0.4f, 1f));
            yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
            SetLevel(0f);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
        SetLevel(1f);
    }

    // A scare: the lamp pops and stays dark for 'seconds', then stutters back on. Only if it's lit now.
    public void CutOut(float seconds, AudioClip pop)
    {
        if (isActiveAndEnabled && lights.Length > 0 && lights[0].enabled) StartCoroutine(Dark(seconds, pop));
    }

    IEnumerator Dark(float seconds, AudioClip pop)
    {
        if (pop) AudioSource.PlayClipAtPoint(pop, transform.position, 0.9f);
        SetLevel(0f);
        yield return new WaitForSeconds(seconds);
        yield return StutterOn();
    }

    // A single stutter (used when Frankie wakes): off for a moment, a weak blink, then back on.
    public void StutterOnce()
    {
        if (isActiveAndEnabled && lights.Length > 0 && lights[0].enabled) StartCoroutine(Stutter());
    }

    IEnumerator Stutter()
    {
        SetLevel(0f);
        yield return new WaitForSeconds(0.12f);
        SetLevel(0.4f);
        yield return new WaitForSeconds(0.08f);
        SetLevel(0f);
        yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
        SetLevel(1f);
    }

    IEnumerator Steady()
    {
        if (flicker)
        {
            flicker.enabled = false;
            if (flicker.buzz) flicker.buzz.volume *= 0.4f;   // the angry buzz settles into a hum
        }
        SetLevel(1f);
        yield break;
    }

    void SetLevel(float level)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].intensity = baseIntensity[i] * level;
            lights[i].enabled = level > 0f;
        }
        foreach (var r in glowParts)
        {
            r.GetPropertyBlock(block);
            block.SetColor(ColourId, level > 0f ? glowColour * level : offColour);
            r.SetPropertyBlock(block);
        }
    }
}
