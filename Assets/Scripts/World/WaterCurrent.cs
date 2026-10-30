using System.Collections.Generic;
using UnityEngine;

// Fast water: a box (or, with 'radius' set, a whirlpool round this object) that drags a swimmer along and
// pulls them under. Only while its flood zone's water is high enough (above 'activeAbove'), so draining an
// area calms it. PlayerSwim asks WaterCurrent.At for the push.
public class WaterCurrent : MonoBehaviour
{
    public Bounds area;                    // a box current (world space); unused for a whirlpool
    public Vector3 flow;                   // the way it drags, metres per second (box current)
    public float radius;                   // > 0: a whirlpool this wide, sucking in toward its centre
    public float pull = 1.2f;              // whirlpool: metres per second toward the middle
    public float down = 0.7f;              // metres per second it drags a swimmer under
    public FloodZone zone;
    public float activeAbove = -1000f;     // only while the zone's water is above this height

    static readonly List<WaterCurrent> all = new List<WaterCurrent>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    bool Active => zone == null || zone.level > activeAbove;

    // The drag on a swimmer at 'point' from every current there (zero if none).
    public static Vector3 At(Vector3 point)
    {
        Vector3 push = Vector3.zero;
        foreach (var c in all)
        {
            if (!c.Active) continue;
            if (c.radius > 0f)
            {
                Vector3 toCentre = c.transform.position - point; toCentre.y = 0f;
                float d = toCentre.magnitude;
                if (d > c.radius || d < 0.01f) continue;
                float k = 1f - d / c.radius;   // stronger nearer the middle
                push += toCentre / d * c.pull * (0.4f + k) + Vector3.down * c.down * k;
            }
            else if (point.x >= c.area.min.x && point.x <= c.area.max.x && point.z >= c.area.min.z && point.z <= c.area.max.z)
                push += c.flow + Vector3.down * c.down;
        }
        return push;
    }
}
