using UnityEngine;

// The fuse box that brings the area's power back. Press E to slot in fuses you carry (one per press);
// when every slot is filled, press E again to pull the breaker and restore the PowerGrid.
public class FuseBox : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public GameObject[] fuseSlots;        // fuse models in the box, hidden until a fuse goes in
    public AudioClip insertSound;

    int installed;

    int Required => fuseSlots.Length;

    void Awake()
    {
        foreach (var slot in fuseSlots) slot.SetActive(false);
    }

    public string Prompt
    {
        get
        {
            if (grid.IsOn) return null;
            if (installed >= Required) return "Pull the breaker";
            return "Insert fuse (" + installed + "/" + Required + ")";
        }
    }

    public void Interact(Interactor player)
    {
        if (grid.IsOn) return;
        if (installed >= Required)
        {
            grid.Restore(transform.position);
            return;
        }
        if (player.inventory.fuses <= 0)
        {
            Hud.Notice("Missing fuses (" + installed + "/" + Required + " in place)");
            return;
        }
        player.inventory.fuses--;
        fuseSlots[installed].SetActive(true);
        installed++;
        if (insertSound) AudioSource.PlayClipAtPoint(insertSound, transform.position, 1f);
        Hud.Notice(installed < Required ? "Fuse inserted (" + installed + "/" + Required + ")"
                                        : "All fuses in place   [E] pull the breaker");
    }
}
