using System.Collections;
using UnityEngine;

// Wakes the Pale Man once the power is back. 15-20 seconds after the fuse box is fixed, every lamp
// stutters once, a shriek echoes out of the west tunnel, and he starts hunting from there.
// Until then he is switched off and nowhere in the level.
public class PaleManArrival : MonoBehaviour
{
    public PowerGrid grid;
    public PaleManAI monster;                  // switched off until he arrives
    public AudioSource shriek;                 // sits deep in the west tunnel, heard from far away
    public float minDelay = 15f, maxDelay = 20f;

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
        foreach (var lamp in FindObjectsByType<PoweredLight>()) lamp.StutterOnce();
        yield return new WaitForSeconds(0.4f);
        shriek.Play();
        Arrived = true;
        monster.gameObject.SetActive(true);
        monster.WakeWithScream();
    }

    // For testing: wake him now.
    public void ArriveNow() => StartCoroutine(Arrive(0f));
}
