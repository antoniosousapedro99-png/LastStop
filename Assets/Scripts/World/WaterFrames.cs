using System.Collections.Generic;
using UnityEngine;

// Animated water: plays a loop of rippling surface pictures (FoxTex's water frames) on every water surface
// under this object that uses 'material', each tiled to its own size ('metresPerTile'). Done per surface on
// screen only (MaterialPropertyBlock), so the shared material asset never changes while the game runs.
public class WaterFrames : MonoBehaviour
{
    public Material material;
    public Texture2D[] frames;
    public float framesPerSecond = 8f;
    public float metresPerTile = 3f;

    readonly List<Renderer> surfaces = new List<Renderer>();
    readonly List<Vector4> tiling = new List<Vector4>();
    MaterialPropertyBlock block;
    int shown = -1;

    void Start()
    {
        block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial != material) continue;
            surfaces.Add(r);
            var size = r.bounds.size;   // the quads lie flat: their size is in x and z
            tiling.Add(new Vector4(Mathf.Max(1f, size.x / metresPerTile), Mathf.Max(1f, size.z / metresPerTile), 0f, 0f));
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        int frame = (int)(Time.time * framesPerSecond) % frames.Length;
        if (frame == shown) return;
        shown = frame;
        for (int i = 0; i < surfaces.Count; i++)
        {
            if (!surfaces[i]) continue;
            surfaces[i].GetPropertyBlock(block);
            block.SetTexture("_BaseMap", frames[frame]);
            block.SetVector("_BaseMap_ST", tiling[i]);
            surfaces[i].SetPropertyBlock(block);
        }
    }
}
