using UnityEngine;

// The background sound of each area, crossfading smoothly as the player walks between them:
// - street: rain and a quiet wind drone; the rain gets muffled and fades as you go down the stairs
// - station (platform, corridor): the "Too Quiet" drone
// - tunnels (past the platform ends): a darker drone
// Every sound is a looping 2D AudioSource that plays all the time; only its volume changes.
public class AreaAmbience : MonoBehaviour
{
    public Transform player;
    public AudioSource rain, streetDrone, stationDrone, tunnelDrone;
    public float rainVolume = 0.5f, streetDroneVolume = 0.12f, stationDroneVolume = 0.3f, tunnelDroneVolume = 0.35f;

    [Header("Where the areas are")]
    public float stairsBottomY = 1.5f, stairsTopY = 5.5f;   // between these heights the street sounds fade in
    public float stairsBottomZ = 0f, corridorEndZ = 12f;   // the faint rain fades out between these
    public float platformEndX = 18.3f;                     // past this distance from the middle you're in a tunnel
    public float tunnelBlend = 4f;                          // metres over which station and tunnel sounds swap
    public float fadeSpeed = 0.5f;                          // how fast volumes follow (full fade in about 2 s)

    AudioLowPassFilter rainMuffle;

    void Start()
    {
        // Rain heard from below ground sounds dull: cut its high frequencies there.
        rainMuffle = rain.GetComponent<AudioLowPassFilter>();
        if (rainMuffle == null) rainMuffle = rain.gameObject.AddComponent<AudioLowPassFilter>();
        foreach (var s in new[] { rain, streetDrone, stationDrone, tunnelDrone }) s.volume = 0f;
    }

    void Update()
    {
        Vector3 p = player.position;
        float street = Mathf.InverseLerp(stairsBottomY, stairsTopY, p.y);   // 0 underground, 1 on the street
        float tunnel = Mathf.InverseLerp(platformEndX - tunnelBlend * 0.5f, platformEndX + tunnelBlend * 0.5f, Mathf.Abs(p.x));
        float below = 1f - street;

        // The rain can still be heard faintly at the bottom of the stairs, dying away along the corridor.
        float rainLevel = Mathf.Max(street, 0.25f * Mathf.InverseLerp(corridorEndZ, stairsBottomZ, p.z) * (1f - tunnel));
        Fade(rain, rainVolume * rainLevel);
        Fade(streetDrone, streetDroneVolume * street);
        Fade(stationDrone, stationDroneVolume * below * (1f - tunnel));
        Fade(tunnelDrone, tunnelDroneVolume * below * tunnel);
        rainMuffle.cutoffFrequency = Mathf.Lerp(700f, 22000f, street);
    }

    void Fade(AudioSource source, float target)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, fadeSpeed * Time.deltaTime);
    }
}
