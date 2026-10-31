using UnityEngine;

// A piece of loose clutter (a can, a cup, a bag, a brick, a crate, a barrel, the shopping trolley...) that
// Sousa and Frankie knock about as they walk into it (see ClutterKicker). It has its own physics body and
// rattles, clinks or thuds when it's kicked and when it lands or hits something.
// A kick by Sousa standing up is loud: for a couple of seconds afterwards every knock it makes is a noise
// Frankie can hear (Noise.Make). Crouched, Sousa only nudges things gently and they make no noise for him.
// Frankie's own kicks only make sound (he doesn't come to check on himself).
// Small things (cans, cups, bags, bricks, cones) don't block the player: he walks through them, kicking them
// aside. Medium things (crates, barrels, bins, tyres, chairs, the trolley) are solid: he shoves them along.
// Where clutter ends up isn't saved: a reload puts it back where it started.
[RequireComponent(typeof(Rigidbody))]
public class Kickable : MonoBehaviour
{
    public enum Sound { Tin, Plastic, Soft, Stone, Wood, Metal, Rubber }

    public Sound sound = Sound.Tin;
    public bool small = true;          // walked through and kicked aside (true) or solid and shoved (false)
    public float kickScale = 1.3f;     // how fast it leaves the foot, compared to the kicker's speed
    public AudioClip[] clips;          // knocks, one picked at random
    public float pitch = 1f;
    public float volume = 0.6f;

    [System.NonSerialized] public Rigidbody body;
    float loudUntil = -1f;             // knocks before this time are heard by Frankie
    float lastKnock = -1f;

    // How far a knock carries, at full strength (metres Frankie can hear it from).
    static float Carries(Sound s) => s switch
    {
        Sound.Tin => 10f, Sound.Plastic => 6f, Sound.Soft => 2f, Sound.Stone => 8f,
        Sound.Wood => 10f, Sound.Metal => 14f, _ => 4f,
    };

    static PhysicsMaterial skittery;   // shared by all small things: they slide and bounce a little

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.Sleep();   // lies still until something touches it
        if (!small) return;
        if (!skittery) skittery = new PhysicsMaterial("Skittery") { dynamicFriction = 0.25f, staticFriction = 0.4f, bounciness = 0.25f, bounceCombine = PhysicsMaterialCombine.Average };
        foreach (var shape in GetComponentsInChildren<Collider>()) shape.sharedMaterial = skittery;
    }

    // Called by ClutterKicker: 'push' is the change of speed (metres per second) given at 'at'.
    public void Kick(Vector3 push, Vector3 at, bool loud, bool quiet)
    {
        body.WakeUp();
        body.AddForceAtPosition(push, at, ForceMode.VelocityChange);
        if (loud) loudUntil = Time.time + 2f;
        if (!quiet) Knock(push.magnitude);
    }

    void OnCollisionEnter(Collision hit)
    {
        Knock(hit.relativeVelocity.magnitude);
    }

    // A rattle, clink or thud as loud as the hit; while a loud kick is still fresh, Frankie hears it too.
    void Knock(float speed)
    {
        if (speed < 0.5f || Time.time - lastKnock < 0.15f || clips == null || clips.Length == 0) return;
        lastKnock = Time.time;
        float strength = Mathf.Clamp01(speed / 4f);
        PlayAt(clips[Random.Range(0, clips.Length)], transform.position, volume * (0.3f + 0.7f * strength), pitch * Random.Range(0.9f, 1.1f));
        if (Time.time < loudUntil) Noise.Make(transform.position, Carries(sound) * (0.4f + 0.6f * strength));
    }

    // A one-off 3D sound at a set pitch (AudioSource.PlayClipAtPoint can't change pitch).
    static void PlayAt(AudioClip clip, Vector3 at, float volume, float pitch)
    {
        var go = new GameObject("Knock");
        go.transform.position = at;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();
        Destroy(go, clip.length / pitch + 0.1f);
    }
}
