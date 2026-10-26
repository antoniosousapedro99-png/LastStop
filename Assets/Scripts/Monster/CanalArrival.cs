using System.Collections;
using UnityEngine;

// Frankie follows the player to Canal St Junction: about a minute after they arrive, a heavy thud sounds
// from the stalled train behind them (he rode on its roof), and he drops onto the tracks and hunts as usual.
// The arrival is also the moment the game autosaves.
public class CanalArrival : MonoBehaviour
{
    public FrankieAI monster;
    public Vector3 dropPoint;              // on the tracks beside the stalled car
    public float dropYaw = 90f;
    public float delay = 60f;
    public AudioSource thud;               // on the car's roof

    public static bool Arrived { get; private set; }

    void Awake() => Arrived = false;

    public void Begin()
    {
        Arrived = true;
        SaveSystem.Write(SaveSystem.Autosave, "Autosave - Canal St Junction");
        Hud.Notice("Game autosaved", 2f);
        StopAllCoroutines();
        StartCoroutine(Drop(delay));
    }

    // Loading a save made at Canal St: Frankie is put back (if he'd already dropped) by FrankieArrival;
    // otherwise his countdown starts again.
    public void LoadState(bool frankieAwake)
    {
        Arrived = true;
        if (!frankieAwake) StartCoroutine(Drop(delay));
    }

    IEnumerator Drop(float wait)
    {
        yield return new WaitForSeconds(wait);
        if (thud) thud.Play();
        yield return new WaitForSeconds(0.6f);
        FindAnyObjectByType<FrankieArrival>()?.WakeNow();
        monster.gameObject.SetActive(true);
        monster.enabled = true;
        var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) agent.isStopped = false;
        monster.PutBack(dropPoint, dropYaw);
    }
}
