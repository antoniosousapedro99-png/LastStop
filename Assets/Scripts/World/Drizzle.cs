using UnityEngine;

// Rain that follows the player while they're on the street (above ground) and stops once they go
// down into the station. Works with any rain made of particle systems (including splash effects
// inside it): their emission is switched on and off together.
public class Drizzle : MonoBehaviour
{
    public Transform player;
    public float streetLevel = 5f;    // above this height the player is outdoors
    public float height = 8f;         // the rain starts this far above the player

    ParticleSystem[] systems;
    bool wasOutdoors = true;

    void Awake() => systems = GetComponentsInChildren<ParticleSystem>();

    void LateUpdate()
    {
        transform.position = player.position + Vector3.up * height;
        bool outdoors = player.position.y > streetLevel;
        if (outdoors == wasOutdoors) return;
        wasOutdoors = outdoors;
        foreach (var ps in systems)
        {
            var emission = ps.emission;
            emission.enabled = outdoors;
        }
    }
}