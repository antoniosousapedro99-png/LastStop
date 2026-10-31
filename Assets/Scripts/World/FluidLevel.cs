using Fusion.Fluid;
using UnityEngine;

// Keeps a FusionWater fluid (its trigger boxes on this object) filled up to its FloodZone's water level, so
// loose things float on the water wherever it stands now: the cistern dropping a step at each valve, a pit or
// the siphon draining. FusionWater reads the surface from the top of the first box, so every box's top moves.
// The object sits at the world origin, unturned, so each box's numbers are world positions.
[RequireComponent(typeof(Fluid))]
public class FluidLevel : MonoBehaviour
{
    public FloodZone zone;

    BoxCollider[] boxes;
    float[] bottoms;
    float shown = float.NaN;

    void Awake()
    {
        boxes = GetComponents<BoxCollider>();
        bottoms = new float[boxes.Length];
        for (int i = 0; i < boxes.Length; i++) bottoms[i] = boxes[i].center.y - boxes[i].size.y * 0.5f;
    }

    void FixedUpdate()
    {
        if (!zone || zone.level == shown) return;
        shown = zone.level;
        for (int i = 0; i < boxes.Length; i++)
        {
            float top = Mathf.Max(bottoms[i] + 0.02f, shown);   // drained below the floor: a sliver, nothing floats
            var c = boxes[i].center; c.y = (bottoms[i] + top) * 0.5f; boxes[i].center = c;
            var s = boxes[i].size; s.y = top - bottoms[i]; boxes[i].size = s;
        }
    }
}
