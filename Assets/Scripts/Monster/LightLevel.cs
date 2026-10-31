using System.Collections.Generic;
using UnityEngine;

// How brightly a spot is lit by the level's lamps, candles, flares and emergency lights (0 = pitch black,
// about 1 = standing under a working lamp). Frankie uses it to judge how far away he can see the player.
// Lights that are off, too far away or behind a wall don't count; lights passed in 'ignore' (the player's
// own torch, which Frankie judges separately) don't either.
public static class LightLevel
{
    static readonly List<Light> lights = new List<Light>();
    static float refreshedAt = -999f;

    public static float At(Vector3 point, ICollection<Light> ignore)
    {
        // The list of lights is gathered again every few seconds (flares come and go).
        if (Time.time - refreshedAt > 3f || Time.time < refreshedAt)
        {
            lights.Clear();
            lights.AddRange(Object.FindObjectsByType<Light>());
            refreshedAt = Time.time;
        }
        float level = 0f;
        foreach (var light in lights)
        {
            if (!light || !light.isActiveAndEnabled || light.intensity <= 0.01f || light.type == LightType.Directional) continue;
            if (ignore != null && ignore.Contains(light)) continue;
            Vector3 to = point - light.transform.position;
            float d = to.magnitude;
            if (d > light.range) continue;
            if (light.type == LightType.Spot && Vector3.Angle(light.transform.forward, to) > light.spotAngle * 0.5f) continue;
            // Roughly how the renderer fades light: weaker with distance, reaching nothing at its range.
            float window = 1f - (d / light.range) * (d / light.range);
            float amount = light.intensity / (d * d + 1f) * window * window;
            if (amount < 0.03f) continue;
            // A wall in between blocks it (the lamp's own fitting is skipped by starting a little way out).
            Vector3 dir = to / Mathf.Max(d, 0.001f);
            if (d > 0.6f && Physics.Raycast(light.transform.position + dir * 0.4f, dir, d - 0.9f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
            level += amount;
        }
        return level;
    }
}
