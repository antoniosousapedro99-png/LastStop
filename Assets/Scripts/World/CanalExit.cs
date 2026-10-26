using UnityEngine;

// Past the open street shutter at the top of Canal St's escalators: out into the night, and the end card.
public class CanalExit : MonoBehaviour
{
    public RollerShutter shutter;
    bool done;

    void OnTriggerEnter(Collider other)
    {
        if (done || !shutter.Open || !other.GetComponentInParent<PlayerInventory>()) return;
        done = true;
        var player = other.GetComponentInParent<PlayerInventory>().transform;
        foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<FlareHandler>(), player.GetComponentInChildren<Interactor>() })
            if (b) b.enabled = false;
        var monster = FrankieAI.Active;
        if (monster) monster.enabled = false;   // no last-second death behind the end card
        FindAnyObjectByType<EndCard>()?.Show();
    }
}
