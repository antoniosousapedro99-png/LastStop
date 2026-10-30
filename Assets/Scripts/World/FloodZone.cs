using System.Collections.Generic;
using UnityEngine;

// One flooded area of the sewers: a set of boxes sharing one water level (its surface's height in the world).
// Its water surfaces (flat quads) follow the level, but never sink below their own resting height (so the
// tunnels' channels keep their water once an area is drained). Draining lowers the level slowly, with a
// roar heard through the tunnels. Other scripts ask FloodZone.SurfaceAt where the water is.
public class FloodZone : MonoBehaviour
{
    public string id;
    public Bounds[] areas;                 // the boxes this water fills (world space)
    public Transform[] surfaces;           // the water quads
    public float[] surfaceRest;            // each quad's lowest height (its resting place)
    public float level;                    // the surface's height now
    public AudioSource roar;               // the draining roar (a deep, slowed stream)

    float target, speed;
    static readonly List<FloodZone> all = new List<FloodZone>();

    void OnEnable() { all.Add(this); target = level; Place(); }
    void OnDisable() => all.Remove(this);

    // Lower (or raise) the water to 'to' over 'seconds', roaring while it moves.
    public void DrainTo(float to, float seconds = 10f)
    {
        target = to;
        speed = Mathf.Abs(level - to) / Mathf.Max(0.1f, seconds);
        if (roar && !roar.isPlaying) { roar.volume = 0f; roar.Play(); }
    }

    // Straight to a level, silently (loading a save).
    public void SetLevel(float to) { level = target = to; Place(); }

    void Update()
    {
        if (Mathf.Approximately(level, target))
        {
            if (roar && roar.isPlaying) { roar.volume = Mathf.MoveTowards(roar.volume, 0f, Time.deltaTime * 0.5f); if (roar.volume <= 0f) roar.Stop(); }
            return;
        }
        level = Mathf.MoveTowards(level, target, speed * Time.deltaTime);
        if (roar) roar.volume = Mathf.MoveTowards(roar.volume, 0.8f, Time.deltaTime);
        Place();
    }

    void Place()
    {
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (!surfaces[i]) continue;
            var p = surfaces[i].position;
            p.y = Mathf.Max(level, surfaceRest[i]);
            surfaces[i].position = p;
        }
    }

    // Is 'point' inside one of this zone's boxes (any height up to just above the water)?
    bool Holds(Vector3 point)
    {
        foreach (var b in areas)
            if (point.x >= b.min.x && point.x <= b.max.x && point.z >= b.min.z && point.z <= b.max.z && point.y >= b.min.y && point.y <= level + 3f)
                return true;
        return false;
    }

    // The water's surface above 'point', if it stands in a flood zone.
    public static bool SurfaceAt(Vector3 point, out float surface)
    {
        surface = float.MinValue;
        foreach (var z in all) if (z.Holds(point)) surface = Mathf.Max(surface, z.level);
        return surface > float.MinValue;
    }

    public static FloodZone Find(string id) => all.Find(z => z.id == id);
}
