using System.Collections;
using UnityEngine;

// The player can take two hits; the second one kills. Painkillers (taken from the inventory) heal one.
// - A hit flashes the screen red, jolts the view and plays a cry of pain; while hurt, the edges of the
//   screen stay faintly red and the player breathes heavily.
// - Death: control stops, the view drops to the floor, the screen fades to black with "YOU DIED", and
//   the game goes back to the title (there are no saves yet).
public class PlayerHealth : MonoBehaviour
{
    public int maxHits = 2;
    public AudioClip[] painSounds;
    public AudioClip hurtBreathing;            // loops quietly while hurt
    public AudioSource voice;

    public int Hits { get; private set; }
    public bool Hurt => Hits > 0;
    public bool Dead { get; private set; }

    float flash;                               // red flash after a hit, 1 -> 0
    float deathFade;
    AudioSource breathing;

    void Awake()
    {
        breathing = gameObject.AddComponent<AudioSource>();
        breathing.clip = hurtBreathing; breathing.loop = true; breathing.volume = 0f; breathing.spatialBlend = 0f;
        if (hurtBreathing) breathing.Play();
    }

    public void Hit(Vector3 from)
    {
        if (Dead) return;
        Hits++;
        flash = 1f;
        if (painSounds.Length > 0) voice.PlayOneShot(painSounds[Random.Range(0, painSounds.Length)], 1f);
        var look = GetComponent<PlayerLook>();
        if (look) look.AddRecoil(-8f, Random.Range(-6f, 6f));   // the view jolts down and sideways
        if (Hits >= maxHits) StartCoroutine(Die());
    }

    // Painkillers: heal one hit. Returns false if there was nothing to heal.
    public bool Heal()
    {
        if (!Hurt || Dead) return false;
        Hits--;
        return true;
    }

    IEnumerator Die()
    {
        Dead = true;
        foreach (var b in new MonoBehaviour[] { GetComponent<PlayerMovement>(), GetComponent<PlayerLook>(), GetComponent<FlareHandler>(),
                                                GetComponentInChildren<Interactor>(), GetComponent<PlayerLean>(), GetComponent<PlayerCrouch>() })
            if (b) b.enabled = false;
        var cam = transform.Find("Main Camera");
        Vector3 startPos = cam.localPosition;
        Quaternion startRot = cam.localRotation;
        // The view sinks to the floor and rolls over.
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f)
        {
            float e = t * t;
            cam.localPosition = Vector3.Lerp(startPos, new Vector3(startPos.x, 0.25f, startPos.z), e);
            cam.localRotation = startRot * Quaternion.Euler(-20f * e, 0f, 70f * e);
            yield return null;
        }
        for (float t = 0f; t < 1f; t += Time.deltaTime / 2f) { deathFade = t; yield return null; }
        deathFade = 1f;
        yield return new WaitForSeconds(2.5f);
        MenuSystem.BackToTitle();
    }

    void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.5f);
        if (breathing) breathing.volume = Mathf.MoveTowards(breathing.volume, Hurt && !Dead ? 0.35f : 0f, Time.deltaTime * 0.5f);
    }

    void OnGUI()
    {
        GUI.depth = -5;
        float hurtTint = Hurt && !Dead ? 0.18f + 0.06f * Mathf.Sin(Time.time * 2.5f) : 0f;
        float red = Mathf.Max(flash * 0.55f, hurtTint);
        if (red > 0f) DrawEdges(new Color(0.55f, 0f, 0f, red));
        if (flash > 0f) MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.6f, 0f, 0f, flash * 0.35f));
        if (deathFade > 0f)
        {
            MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, deathFade));
            MenuStyle.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 120 * MenuStyle.Scale), "YOU DIED", 90,
                            TextAnchor.MiddleCenter, new Color(0.7f, 0.08f, 0.06f, deathFade));
        }
    }

    // A red border that fades toward the middle of the screen.
    static void DrawEdges(Color c)
    {
        float w = Screen.width, h = Screen.height, band = h * 0.22f;
        for (int i = 0; i < 6; i++)
        {
            float k = 1f - i / 6f, d = band * i / 6f, s = band / 6f;
            var col = new Color(c.r, c.g, c.b, c.a * k);
            MenuStyle.Fill(new Rect(0, d, w, s), col);
            MenuStyle.Fill(new Rect(0, h - d - s, w, s), col);
            MenuStyle.Fill(new Rect(d, 0, s, h), col);
            MenuStyle.Fill(new Rect(w - d - s, 0, s, h), col);
        }
    }
}
