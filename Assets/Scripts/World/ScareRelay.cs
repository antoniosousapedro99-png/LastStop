using UnityEngine;

// Passes a scare spot's trigger on to a scare that lives somewhere else (e.g. the ticket booth phone,
// set off by a spot at the top of the escalator).
public class ScareRelay : MonoBehaviour, IScare
{
    public MonoBehaviour target;           // anything that is an IScare

    public void Play(Transform player)
    {
        if (target is IScare scare) scare.Play(player);
    }
}
