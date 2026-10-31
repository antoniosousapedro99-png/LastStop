using UnityEngine;

// The edge of the playable street. Invisible walls (plain colliders) stop the player; this script
// shows "I should turn around..." whenever they walk up close to an edge, so the walls feel like
// the character's choice rather than an invisible barrier.
public class AreaBoundary : MonoBehaviour
{
    public Transform player;
    public Vector2 min, max;           // playable rectangle on the ground (x, z)
    public float warnDistance = 1.5f;  // metres from an edge where the thought appears
    public float aboveHeight = 4f;     // only applies while the player is above this height (the street)
    public string message = "I should turn around...";

    float nextMessage;

    void Update()
    {
        Vector3 p = player.position;
        if (p.y < aboveHeight) return;
        float edge = Mathf.Min(p.x - min.x, max.x - p.x, p.z - min.y, max.y - p.z);
        // Well outside the rectangle means somewhere else entirely (the Canal St stairs are high up
        // too): not this edge.
        if (edge < -3f) return;
        if (edge < warnDistance && Time.time > nextMessage)
        {
            Hud.Notice(message, 2.5f);
            nextMessage = Time.time + 3f;
        }
    }
}
