using System.Collections;
using UnityEngine;

// The train ride from Hollis St to Canal St Junction. Once the Hollis St train is moving the screen fades
// to black and the ride rumbles on; then the player wakes up inside another car, stalled in Canal St's
// tunnel: it grinds to a halt, its lights stutter and die, and the player walks the rest of the way.
// Canal St lies in the same scene, far away from Hollis St.
public class StationTransfer : MonoBehaviour
{
    public Transform spawn;                 // where the player stands in the stalled car (its forward = facing)
    public AudioSource ride;                // the rumble of the ride, heard in the dark
    public AudioClip halt;                  // the car grinding to a stop
    public Light carLight;                  // the stalled car's cab light: stutters, then dies
    public CanalArrival arrival;

    bool busy;

    public void Begin(Transform player, Transform oldTrain)
    {
        if (!busy) StartCoroutine(Ride(player, oldTrain));
    }

    IEnumerator Ride(Transform player, Transform oldTrain)
    {
        busy = true;
        var fade = ScreenFade.Current;
        if (fade) yield return fade.FadeTo(1f, 1.5f);
        if (ride) ride.Play();
        yield return new WaitForSeconds(3f);

        // Frankie stays behind at Hollis St, out of the game until he catches up (CanalArrival).
        var monster = FindAnyObjectByType<FrankieAI>(FindObjectsInactive.Include);
        if (monster) monster.gameObject.SetActive(false);

        // Wake up in the other car.
        player.SetParent(null, true);
        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
        player.SetPositionAndRotation(spawn.position, Quaternion.Euler(0f, spawn.eulerAngles.y, 0f));
        var look = player.GetComponent<PlayerLook>();
        if (look) look.pitch = 0f;
        if (controller) controller.enabled = true;
        if (halt) AudioSource.PlayClipAtPoint(halt, spawn.position, 1f);
        if (ride) StartCoroutine(FadeOut(ride, 2.5f));
        if (carLight) StartCoroutine(Die(carLight));
        if (fade) yield return fade.FadeTo(0f, 2f);

        foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerCrouch>(),
                                            player.GetComponent<PlayerLean>(), player.GetComponent<FlareHandler>(),
                                            player.GetComponentInChildren<Interactor>(), player.GetComponent<Footsteps>() })
            if (b) b.enabled = true;
        if (arrival) arrival.Begin();
        busy = false;
    }

    static IEnumerator FadeOut(AudioSource source, float seconds)
    {
        float start = source.volume;
        for (float t = 0f; t < seconds; t += Time.deltaTime) { source.volume = start * (1f - t / seconds); yield return null; }
        source.Stop();
    }

    // A few weak stutters, then dark for good.
    static IEnumerator Die(Light light)
    {
        float full = light.intensity;
        for (int i = 0; i < 5; i++)
        {
            light.intensity = full * Random.Range(0.1f, 0.8f);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
            light.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.3f));
        }
        light.enabled = false;
    }
}
