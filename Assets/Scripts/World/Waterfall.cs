using UnityEngine;

// A canal pouring into the cistern: a falling sheet of water from the canal's mouth ('top') down to the
// cistern's water, whatever height that is now (it drops as the valves drain it), its picture scrolling
// downward; the spray and the roar sit where it lands.
public class Waterfall : MonoBehaviour
{
    public FloodZone zone;              // the water it falls into
    public float top;                   // the height it pours from (world space)
    public float width = 2.6f;
    public Renderer sheet;              // a quad facing out of the wall
    public Transform splash;            // the spray and roar at the bottom
    public float speed = 4f;            // metres per second the water falls (the picture's scroll)
    public float metresPerTile = 2f;

    MaterialPropertyBlock block;

    void Update()
    {
        float bottom = zone ? zone.level : top - 10f;
        float height = Mathf.Max(0.05f, top - bottom);
        var t = sheet.transform;
        t.position = new Vector3(t.position.x, (top + bottom) * 0.5f, t.position.z);
        t.localScale = new Vector3(width, height, 1f);
        if (splash) splash.position = new Vector3(splash.position.x, bottom + 0.05f, splash.position.z);
        if (block == null) block = new MaterialPropertyBlock();
        sheet.GetPropertyBlock(block);
        block.SetVector("_BaseMap_ST", new Vector4(width / metresPerTile, height / metresPerTile, 0f, Time.time * speed / metresPerTile));
        sheet.SetPropertyBlock(block);
    }
}
