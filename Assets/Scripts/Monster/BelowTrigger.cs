using UnityEngine;

// An invisible zone at the bottom of Hollis St's service stairs. The first time the player steps into it,
// Frankie wakes upstairs and comes looking (FrankieArrival.PlayerWentBelow).
public class BelowTrigger : MonoBehaviour
{
    public FrankieArrival arrival;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerInventory>();
        if (player) arrival.PlayerWentBelow(player.transform.position);
    }
}
