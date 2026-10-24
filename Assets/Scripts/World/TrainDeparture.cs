using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Station 1's ending: the east train wakes up and pulls away.
// - Begin (the reverser key goes in): a heavy clunk, the cab light steadies, the engine starts a low
//   whine, and the noise brings Frankie charging down the tunnel (woken early if he hadn't come yet).
// - Tick: while the player holds the dead man's handle the countdown runs (10 s in total) and the engine
//   whine climbs; letting go pauses it.
// - Leave: the player's controls stop, the train lurches off down the tunnel with a screech, Frankie
//   is left behind at the door, and the screen fades to the end card (EndCard).
// This object is the train car itself; it is not marked static, so it can move.
public class TrainDeparture : MonoBehaviour
{
    public float holdSeconds = 10f;
    public Light cabLight;
    public FlickeringLight cabFlicker;
    public AudioSource engine;               // looping engine drone, silent until started
    public AudioClip startClunk, screech;
    public float monsterStartDistance = 32f; // how far down the tunnel he starts his charge

    public bool Started { get; private set; }
    public bool Leaving { get; private set; }
    public float Progress { get; private set; }

    public void Begin()
    {
        if (Started) return;
        Started = true;
        if (startClunk) AudioSource.PlayClipAtPoint(startClunk, transform.position, 1f);
        if (cabFlicker) cabFlicker.enabled = false;
        if (cabLight) cabLight.intensity *= 2f;
        if (engine) { engine.pitch = 0.4f; engine.volume = 0.3f; engine.Play(); }
        Noise.Make(transform.position, 200f);
        SummonMonster();
    }

    // He comes from down the tunnel behind the player, running.
    void SummonMonster()
    {
        var arrival = FindAnyObjectByType<FrankieArrival>();
        var player = FindAnyObjectByType<PlayerInventory>();
        if (!arrival || !player) return;
        var monster = arrival.monster;
        Vector3 start = new Vector3(player.transform.position.x - monsterStartDistance, -1.3f, 22.7f);
        bool far = !monster.gameObject.activeSelf || Vector3.Distance(monster.transform.position, player.transform.position) > monsterStartDistance + 8f;
        arrival.WakeNow();
        if (far && NavMesh.SamplePosition(start, out var hit, 4f, NavMesh.AllAreas)) monster.PutBack(hit.position, 90f);
        monster.Hunt(30f);
    }

    public void Tick(bool holding)
    {
        if (!Started || Leaving) return;
        if (holding) Progress = Mathf.Min(1f, Progress + Time.deltaTime / holdSeconds);
        if (engine)
        {
            engine.pitch = Mathf.Lerp(0.4f, 1.15f, Progress);
            engine.volume = Mathf.Lerp(0.3f, 0.9f, Progress);
        }
        if (Progress >= 1f) StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        Leaving = true;
        var player = FindAnyObjectByType<PlayerInventory>().transform;
        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
        foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerCrouch>(),
                                            player.GetComponent<PlayerLean>(), player.GetComponent<FlareHandler>(), player.GetComponentInChildren<Interactor>(),
                                            player.GetComponent<Footsteps>() })
            if (b) b.enabled = false;
        player.SetParent(transform, true);   // ride along

        // He's left standing at the door, staring after you.
        var monster = FrankieAI.Active;
        if (monster)
        {
            monster.Play("Idle");
            monster.GetComponent<NavMeshAgent>().isStopped = true;
            monster.enabled = false;
        }

        if (screech) AudioSource.PlayClipAtPoint(screech, transform.position, 1f);
        var endCard = FindAnyObjectByType<EndCard>();
        float speed = 0f;
        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            speed += 1.2f * Time.deltaTime;                 // a slow, heavy start
            transform.position += Vector3.right * speed * Time.deltaTime;
            if (t > 1.5f && endCard && !endCard.Showing) endCard.Show();
            yield return null;
        }
    }
}
