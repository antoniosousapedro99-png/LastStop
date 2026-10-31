using UnityEngine;

// Sousa's family photo in the inventory fades a little for every family voicemail he let ring out without
// answering (and comes back if he answers it later). A pale, see-through layer over the picture grows
// more solid. Runs on unscaled time, since the game is paused while the inventory is open.
public class PhotoFade : MonoBehaviour
{
    public Renderer overlay;                   // the pale layer just in front of the photo
    public Color washed = new Color(0.92f, 0.88f, 0.8f);
    public float perIgnoredCall = 0.15f, maxFade = 0.75f;

    static readonly int ColourId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock block;

    // How faded the photo is now (0 = as it was, up to maxFade).
    public static float Amount(float perCall, float max)
    {
        int ignored = 0;
        foreach (var phone in FindObjectsByType<VoicemailPhone>())
            if (phone.Ignored && !phone.Heard) ignored++;
        return Mathf.Min(max, ignored * perCall);
    }

    void OnEnable() => Apply();
    void Update() => Apply();

    void Apply()
    {
        if (!overlay) return;
        block ??= new MaterialPropertyBlock();
        var c = washed;
        c.a = Amount(perIgnoredCall, maxFade);
        block.SetColor(ColourId, new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a));   // the material blends premultiplied
        overlay.SetPropertyBlock(block);
    }
}
