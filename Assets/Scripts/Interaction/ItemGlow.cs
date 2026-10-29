using UnityEngine;

// A faint, slowly breathing glow on an item you can pick up, like in Amnesia: just enough to catch
// your eye in the dark, not a bright outline. Only the item's own surface glows (emission); it
// doesn't light up anything around it. Turned off everywhere when GameSettings.ItemGlow is false.
public class ItemGlow : MonoBehaviour
{
    public Color colour = new Color(1f, 0.92f, 0.75f);   // warm candle-white
    public float strength = 0.44f;                       // emission at the peak of a "breath" (doubled 2026-10-29: testers missed items)
    public float breathSeconds = 2.6f;                   // one slow pulse

    Renderer[] renderers;
    MaterialPropertyBlock block;
    float phase;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        phase = Random.value * 10f;   // items don't all pulse in step

        // Emission only shows if the material allows it; switching it on with a black emission colour
        // changes nothing for other objects that share the material.
        foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
                if (m && m.HasProperty(EmissionId) && !m.IsKeywordEnabled("_EMISSION"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    if (m.GetColor(EmissionId).maxColorComponent > 0f) continue;
                    m.SetColor(EmissionId, Color.black);
                }
    }

    void Update()
    {
        bool on = GameSettings.ItemGlow;
        float breath = on ? 0.55f + 0.45f * Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / breathSeconds) : 0f;
        Color emission = colour * (strength * breath);
        foreach (var r in renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissionId, emission);
            r.SetPropertyBlock(block);
        }
    }
}
