using UnityEngine;

// An escalator. Dead (just a steep ramp of steps) until its station has power; then its steps roll and it
// carries whoever stands on it along 'carry' (the up direction of the escalator).
public class Escalator : MonoBehaviour
{
    public PowerGrid grid;
    public Vector3 carry;                  // world direction and speed (metres per second)
    public Renderer steps;                 // its step surface: the texture scrolls while it runs
    public Vector2 tiling = new Vector2(28f, 1f);   // how its step stripes repeat (must match the material)
    public float length = 11.2f;           // metres from bottom to top, so the stripes move at the carrying speed
    public AudioSource hum;                // a motor hum while running

    CharacterController rider;
    float lastTouched = -1f;               // when the rider was last felt in the zone
    MaterialPropertyBlock block;
    float scroll;

    void Awake() => block = new MaterialPropertyBlock();

    bool Running => grid && grid.IsOn;

    void Update()
    {
        if (!Running) return;
        if (hum && !hum.isPlaying) hum.Play();
        scroll += Time.deltaTime * carry.magnitude;
        if (steps)
        {
            steps.GetPropertyBlock(block);
            block.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, -scroll * tiling.x / length, 0f));
            steps.SetPropertyBlock(block);
        }
        // Only carry someone felt in the zone a moment ago (a teleport, like loading, skips the exit message).
        if (rider && rider.enabled && Time.time - lastTouched < 0.15f) rider.Move(carry * Time.deltaTime);
    }

    // Called every physics step while something is in the zone.
    void OnTriggerStay(Collider other)
    {
        var cc = other.GetComponent<CharacterController>();
        if (!cc || !other.GetComponentInParent<PlayerInventory>()) return;
        rider = cc;
        lastTouched = Time.time;
    }
}
