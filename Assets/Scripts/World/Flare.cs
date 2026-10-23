using UnityEngine;

// A lit road flare: bright red, flickering light and a fizzing hiss for 'burnTime' seconds, fading
// out over the last few, then a dead stub. It can be held in the hand or dropped (it keeps burning).
// (Later it will also keep the stalker back.)
public class Flare : MonoBehaviour
{
    public Light glow;
    public Renderer tip;                 // the burning end (its colour is dimmed as it dies)
    public AudioSource fizz;
    public Color tipColour = new Color(4f, 0.6f, 0.3f);   // brighter than white = glows
    public float burnTime = 30f;
    public float fadeTime = 4f;          // the last seconds, dying down

    float burned, baseIntensity, baseFizz;
    bool dropped;
    MaterialPropertyBlock block;
    static readonly int ColourId = Shader.PropertyToID("_BaseColor");

    public bool BurntOut => burned >= burnTime;

    // Every flare that is burning right now (held or dropped), so the Pale Man can keep away from them.
    static readonly System.Collections.Generic.List<Flare> lit = new System.Collections.Generic.List<Flare>();
    void OnEnable() => lit.Add(this);
    void OnDisable() => lit.Remove(this);

    public static Transform NearestLit(Vector3 from)
    {
        Transform best = null; float bestDistance = float.MaxValue;
        foreach (var f in lit)
        {
            if (!f || f.BurntOut) continue;
            float d = Vector3.Distance(from, f.transform.position);
            if (d < bestDistance) { bestDistance = d; best = f.transform; }
        }
        return best;
    }

    void Awake()
    {
        baseIntensity = glow.intensity;
        baseFizz = fizz ? fizz.volume : 0f;
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (BurntOut) return;
        burned += Time.deltaTime;
        float life = Mathf.Clamp01((burnTime - burned) / fadeTime);          // 1 until the fade starts, then down to 0
        float flicker = 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 12f, 0f); // restless, like a real flame
        glow.intensity = baseIntensity * flicker * life;
        if (fizz) fizz.volume = baseFizz * life;
        tip.GetPropertyBlock(block);
        block.SetColor(ColourId, Color.Lerp(new Color(0.1f, 0.05f, 0.04f), tipColour * flicker, life));
        tip.SetPropertyBlock(block);
        if (BurntOut)
        {
            glow.enabled = false;
            if (fizz) fizz.Stop();
            if (dropped) Destroy(gameObject, 60f);   // a dropped stub lies there a while, then is cleared away
        }
    }

    // Lets go of it: it falls (with a little forward toss) and burns on the floor.
    public void Drop(Vector3 toss)
    {
        dropped = true;
        transform.SetParent(null, true);
        foreach (var t in GetComponentsInChildren<Transform>()) t.gameObject.layer = 0;   // a world object now, not a held one
        foreach (var r in GetComponentsInChildren<Renderer>()) r.renderingLayerMask = 1u;   // lit like everything else again
        var body = gameObject.AddComponent<Rigidbody>();
        body.mass = 0.3f;
        body.linearVelocity = toss;
        body.angularVelocity = Random.insideUnitSphere * 4f;
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.direction = 2;          // along the stick's length (local Z)
        col.radius = 0.02f; col.height = 0.24f;
        if (BurntOut) Destroy(gameObject, 60f);
    }
}
