using System.Collections.Generic;
using UnityEngine;

// Marks a puddle on the floor as an oval area. Footsteps inside it splash.
// Puddles have no collider (they're thin see-through decals), so footsteps check this list instead.
public class WaterPuddle : MonoBehaviour
{
    public float halfLength = 1f, halfWidth = 0.5f;   // the oval's size, along the puddle's local X and Z

    public static readonly List<WaterPuddle> All = new List<WaterPuddle>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Contains(Vector3 worldPoint)
    {
        Vector3 local = Quaternion.Inverse(transform.rotation) * (worldPoint - transform.position);
        if (Mathf.Abs(local.y) > 0.5f) return false;   // a floor above or below
        float u = local.x / halfLength, v = local.z / halfWidth;
        return u * u + v * v <= 1f;
    }

    public static bool At(Vector3 worldPoint)
    {
        foreach (var p in All) if (p.Contains(worldPoint)) return true;
        return false;
    }
}
