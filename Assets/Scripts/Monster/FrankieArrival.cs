using System.Collections;
using UnityEngine;

// Wakes Frankie. Until then he is switched off and nowhere in the level.
// - Normally: the first time the player reaches the bottom of the service-level stairs (BelowTrigger),
//   7 seconds later a shriek echoes from deep in the west tunnel above and he appears there. He doesn't
//   know exactly where they are: he heads for the stairs and searches (both levels, see FrankieAI).
// - If the power somehow came back first: 5-10 seconds after it, every lamp stutters, the shriek, the
//   lamps stutter again and he is standing under the lamp at the far end of the platform, staring;
//   he walks off into that end's tunnel once he's been seen (FrankieAI's Reveal).
public class FrankieArrival : MonoBehaviour
{
    public PowerGrid grid;
    public FrankieAI monster;                  // switched off until he arrives
    public AudioSource shriek;                 // sits deep in the west tunnel, heard from far away
    public float minDelay = 5f, maxDelay = 10f;
    public float revealAfterShriek = 3.5f;     // seconds between the shriek and him appearing
    public Vector3[] revealSpots;              // where he can appear (one per platform end)
    public Vector3[] walkOffSpots;             // where he walks to from each spot (into that end's tunnel)
    public float belowDelay = 7f;              // seconds after the player first reaches the service level
    public Vector3 belowAppearSpot;            // where he appears then (upstairs, deep in the west tunnel)

    bool waking;

    public static bool Arrived { get; private set; }

    void Awake()
    {
        Arrived = false;
        monster.gameObject.SetActive(false);
        grid.Restored += OnPower;
    }

    void OnDestroy() { if (grid) grid.Restored -= OnPower; }

    void OnPower() { if (!Arrived && !waking) StartCoroutine(Arrive(Random.Range(minDelay, maxDelay))); }

    // The player has reached the bottom of the service stairs ('where').
    public void PlayerWentBelow(Vector3 where)
    {
        if (Arrived || waking) return;
        waking = true;
        StartCoroutine(AppearAndSearch(where));
    }

    IEnumerator AppearAndSearch(Vector3 where)
    {
        yield return new WaitForSeconds(belowDelay);
        shriek.Play();
        Arrived = true;
        monster.gameObject.SetActive(true);
        monster.PutBack(belowAppearSpot, 90f);
        monster.SearchFor(where);
    }

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

    // Loading a save. If he was already awake he's put back where he was; if the power was on but he
    // hadn't come yet, his 5-10 second countdown starts again.
    public void LoadState(bool awake, Vector3 position, float yaw, bool powerOn)
    {
        if (!awake && !powerOn) return;   // (if the player is below, BelowTrigger notices them again)
        grid.Restored -= OnPower;
        if (awake)
        {
            Arrived = true;
            monster.gameObject.SetActive(true);
            monster.PutBack(position, yaw);
        }
        else StartCoroutine(Arrive(Random.Range(minDelay, maxDelay)));
    }

    // The train finale wakes him at once if he hadn't come yet (no shriek or reveal).
    public void WakeNow()
    {
        if (Arrived) return;
        StopAllCoroutines();
        grid.Restored -= OnPower;
        Arrived = true;
        monster.gameObject.SetActive(true);
    }

    // For testing: wake him now.
    public void ArriveNow() => StartCoroutine(Arrive(0f));
}
