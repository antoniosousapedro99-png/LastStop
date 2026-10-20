using UnityEngine;

// Light drizzle that follows the player while they're on the street (above ground), and stops
// once they go down into the station. A particle system makes thin falling streaks.
[RequireComponent(typeof(ParticleSystem))]
public class Drizzle : MonoBehaviour
{
    public Transform player;
    public float streetLevel = 5f;   // above this height the player is outdoors

    ParticleSystem rain;

    void Awake() => rain = GetComponent<ParticleSystem>();

    void LateUpdate()
    {
        transform.position = player.position + Vector3.up * 8f;
        bool outdoors = player.position.y > streetLevel;
        var emission = rain.emission;
        emission.enabled = outdoors;
    }
}
