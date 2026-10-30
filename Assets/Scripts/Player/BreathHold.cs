using UnityEngine;

// Holding his breath under water, heard and never shown as a meter. With his eyes under the surface:
// sound goes muffled, the view goes murky green and close, and he has 'breathSeconds' of air. A calm hold
// at first; from 'strainFrom' his heartbeat swells and quickens; in the last 'panicFrom' seconds bubbles
// burst out faster and faster. Out of air, he drowns. Surfacing after a long hold, he gasps (and coughs).
public class BreathHold : MonoBehaviour
{
    public float breathSeconds = 20f, strainFrom = 10f, panicFrom = 15f;
    public Transform eyes;                     // the camera
    public PlayerHealth health;
    public AudioSource voice;                  // 2D: his own breath
    public AudioClip heartbeat;
    public AudioClip[] bubbles, gasps;
    public AudioClip cough;
    public Color murk = new Color(0.03f, 0.07f, 0.05f, 0.55f);

    public bool Under { get; private set; }
    public float SecondsUnder { get; private set; }

    AudioSource heart;
    AudioLowPassFilter muffle;
    float nextBubble;
    Color dryFog; float dryDensity; bool fogSwapped;

    void Start()
    {
        heart = gameObject.AddComponent<AudioSource>();
        heart.clip = heartbeat; heart.loop = true; heart.spatialBlend = 0f; heart.volume = 0f;
        var listener = FindAnyObjectByType<AudioListener>();
        if (listener)
        {
            muffle = listener.GetComponent<AudioLowPassFilter>();
            if (!muffle) muffle = listener.gameObject.AddComponent<AudioLowPassFilter>();
            muffle.cutoffFrequency = 22000f;
        }
    }

    void Update()
    {
        bool under = !health.Dead && FloodZone.SurfaceAt(eyes.position, out float surface) && eyes.position.y < surface;
        if (under != Under) Switch(under);
        if (!Under) return;

        SecondsUnder += Time.deltaTime;
        float strain = Mathf.InverseLerp(strainFrom, breathSeconds, SecondsUnder);
        heart.volume = Mathf.MoveTowards(heart.volume, strain * 0.9f, Time.deltaTime);
        heart.pitch = 1f + strain * 0.45f;
        if (strain > 0f && !heart.isPlaying && heartbeat) heart.Play();

        // Bubbles: an odd one while calm, then quicker and quicker in the panic.
        if (Time.time >= nextBubble && bubbles != null && bubbles.Length > 0)
        {
            float panic = Mathf.InverseLerp(panicFrom, breathSeconds, SecondsUnder);
            voice.pitch = Random.Range(0.45f, 0.6f);   // the clips pitched down: a deep, wet glug
            voice.PlayOneShot(bubbles[Random.Range(0, bubbles.Length)], 0.35f + panic * 0.5f);
            nextBubble = Time.time + Mathf.Lerp(4f, 0.35f, panic) * Random.Range(0.8f, 1.2f);
        }
        if (SecondsUnder >= breathSeconds) { Switch(false); health.Drown(); }
    }

    void Switch(bool under)
    {
        if (!under && Under && SecondsUnder > 6f)   // a gasp coming up after a long hold, a cough after a very long one
        {
            voice.pitch = 1f;
            if (gasps != null && gasps.Length > 0) voice.PlayOneShot(gasps[Random.Range(0, gasps.Length)], 0.8f);
            if (SecondsUnder > 14f && cough) voice.PlayOneShot(cough, 0.6f);
        }
        Under = under;
        if (under) { SecondsUnder = 0f; nextBubble = Time.time + 1.5f; }
        else { heart.Stop(); heart.volume = 0f; SecondsUnder = 0f; }
        if (muffle) muffle.cutoffFrequency = under ? 600f : 22000f;
        // Close, green-black fog under water; the station's own fog back above it.
        if (under && !fogSwapped) { dryFog = RenderSettings.fogColor; dryDensity = RenderSettings.fogDensity; fogSwapped = true; }
        if (fogSwapped)
        {
            RenderSettings.fogColor = under ? new Color(0.02f, 0.05f, 0.035f) : dryFog;
            RenderSettings.fogDensity = under ? 0.35f : dryDensity;
            if (!under) fogSwapped = false;
        }
    }

    void OnDisable() { if (Under) Switch(false); }

    void OnGUI()
    {
        if (!Under) return;
        GUI.depth = 5;   // under the HUD
        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), murk);
    }
}
