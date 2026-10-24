using System.Collections;
using UnityEngine;

// Wakes the Pale Man once the power is back. 15-20 seconds after the fuse box is fixed, every lamp
// stutters and a shriek echoes out of the west tunnel. A few seconds later the lamps stutter again, and
// when they come back he is standing under the lamp at the far end of the platform (the end further
// from the player), staring. He walks off into that end's tunnel once he's been seen (PaleManAI's Reveal).
// Until then he is switched off and nowhere in the level.
public class PaleManArrival : MonoBehaviour
{
    public PowerGrid grid;
    public PaleManAI monster;                  // switched off until he arrives
    public AudioSource shriek;                 // sits deep in the west tunnel, heard from far away
    public float minDelay = 15f, maxDelay = 20f;
    public float revealAfterShriek = 3.5f;     // seconds between the shriek and him appearing
    public Vector3[] revealSpots;              // where he can appear (one per platform end)
    public Vector3[] walkOffSpots;             // where he walks to from each spot (into that end's tunnel)

    public static bool Arrived { get; private set; }

    void Awake()
    {
        Arrived = false;
        monster.gameObject.SetActive(false);
        grid.Restored += OnPower;
    }

    void OnDestroy() { if (grid) grid.Restored -= OnPower; }

    void OnPower() => StartCoroutine(Arrive(Random.Range(minDelay, maxDelay)));

    IEnumerator Arrive(float delay)
    {
        yield return new WaitForSeconds(delay);
        var lamps = FindObjectsByType<PoweredLight>();
        foreach (var lamp in lamps) lamp.StutterOnce();
        yield return new WaitForSeconds(0.4f);
        shriek.Play();
        yield return new WaitForSeconds(revealAfterShriek);

        // The second stutter: he steps into place while the lights are out.
        foreach (var lamp in lamps) lamp.StutterOnce();
        yield return new WaitForSeconds(0.2f);
        int spot = FarthestSpotFromPlayer();
        Arrived = true;
        monster.gameObject.SetActive(true);
        monster.StageReveal(revealSpots[spot], walkOffSpots[spot]);
    }

    int FarthestSpotFromPlayer()
    {
        var player = GameObject.Find("Player");
        if (!player) return 0;
        int best = 0;
        for (int i = 1; i < revealSpots.Length; i++)
            if (Vector3.Distance(revealSpots[i], player.transform.position) > Vector3.Distance(revealSpots[best], player.transform.position))
                best = i;
        return best;
    }

    // For testing: wake him now.
    public void ArriveNow() => StartCoroutine(Arrive(0f));
}
