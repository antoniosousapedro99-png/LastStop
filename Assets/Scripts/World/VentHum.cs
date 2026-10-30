using UnityEngine;

// The air in a vent: a quiet 3D loop that stays silent while its area has no power. Once the power grid
// is back on, it fades in a moment after the lights reach it (farther from the fuse box = later), so the
// air seems to follow the power through the ducts. Each vent starts at a different point of the loop.
public class VentHum : MonoBehaviour
{
    public PowerGrid grid;
    public AudioSource source;
    public float volume = 0.3f;
    public float fadeTime = 3f;

    float startAt = -1f;

    void Update()
    {
        if (grid == null || !grid.IsOn) return;
        if (startAt < 0f)
        {
            startAt = Time.time + Vector3.Distance(transform.position, grid.Origin) / 12f;
            source.volume = 0f;
        }
        if (Time.time < startAt) return;
        if (!source.isPlaying)
        {
            source.time = Random.Range(0f, source.clip.length * 0.9f);
            source.Play();
        }
        source.volume = Mathf.MoveTowards(source.volume, volume, volume / fadeTime * Time.deltaTime);
        if (source.volume >= volume) enabled = false;   // fully on: nothing more to do
    }
}
