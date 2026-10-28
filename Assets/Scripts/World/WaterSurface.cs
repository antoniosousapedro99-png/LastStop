using System.Collections.Generic;
using UnityEngine;

// A body of water: its surface is at this object's height, over a rectangle 'size' (x by z) centred on it.
// Other scripts ask whether a point stands in water deep enough to matter (FallDamage: deep water breaks a fall).
public class WaterSurface : MonoBehaviour
{
    public Vector2 size = Vector2.one;

    static readonly List<WaterSurface> all = new List<WaterSurface>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    // Is 'feet' under at least 'depth' metres of water?
    public static bool DeepAt(Vector3 feet, float depth)
    {
        foreach (var w in all)
        {
            Vector3 c = w.transform.position;
            if (Mathf.Abs(feet.x - c.x) <= w.size.x * 0.5f && Mathf.Abs(feet.z - c.z) <= w.size.y * 0.5f && c.y - feet.y >= depth)
                return true;
        }
        return false;
    }
}
