using UnityEngine;

// Now and then, while the player is underground, a sound comes from somewhere off in the dark:
// a rumble, falling gravel, a metal scrape, a door creak. It plays at a random spot 15-30 m away,
// so it seems to come from a real place. Rare on purpose, so the player never gets used to it.
public class DistantNoises : MonoBehaviour
{
    public Transform player;
    public AudioClip[] clips;
    public float minWait = 60f, maxWait = 120f;      // seconds between noises
    public float minDistance = 15f, maxDistance = 30f;
    public float volume = 0.8f;
    public float undergroundBelowY = 3f;             // no noises on the street

    float timer;
    int lastIndex = -1;

    void Start() => timer = Random.Range(minWait, maxWait);

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(minWait, maxWait);
        if (player.position.y > undergroundBelowY || clips.Length == 0) return;

        // Never the same noise twice in a row.
        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastIndex) index = (index + 1) % clips.Length;
        lastIndex = index;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 spot = player.position + new Vector3(dir.x, 0f, dir.y) * Random.Range(minDistance, maxDistance);

        var go = new GameObject("DistantNoise");
        go.transform.position = spot;
        var source = go.AddComponent<AudioSource>();
        source.clip = clips[index];
        source.spatialBlend = 1f;                       // 3D: you can tell which way it came from
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 5f;
        source.maxDistance = maxDistance + 15f;
        source.volume = volume * Random.Range(0.7f, 1f);
        source.pitch = Random.Range(0.8f, 1f);          // a little lower sounds bigger and further away
        source.Play();
        Destroy(go, clips[index].length / source.pitch + 0.5f);
    }
}
