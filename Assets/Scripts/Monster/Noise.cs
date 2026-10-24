using System;
using UnityEngine;

// Sounds the monster can hear. Anything that makes a noise calls Noise.Make with where it happened and
// how far it carries (in metres); listeners (Frankie) decide whether they were close enough.
public static class Noise
{
    public static event Action<Vector3, float> Made;

    public static void Make(Vector3 position, float carries)
    {
        if (carries > 0f) Made?.Invoke(position, carries);
    }
}
