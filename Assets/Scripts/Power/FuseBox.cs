using UnityEngine;

// The fuse box that brings the area's power back. Click it to slot in fuses you carry (one per click);
// when every slot is filled, click it again to pull the breaker and restore the PowerGrid.
public class FuseBox : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public GameObject[] fuseSlots;        // fuse models in the box, hidden until a fuse goes in
    public AudioClip insertSound;

    int installed;
    public int Installed => installed;     // shown on the inventory screen
    PlayerInventory inventory;

    int Carried
    {
        get
        {
            if (!inventory) inventory = FindAnyObjectByType<PlayerInventory>();
            return inventory ? inventory.fuses : 0;
        }
    }

    public int Required => fuseSlots.Length;

    void Awake()
    {
        foreach (var slot in fuseSlots) slot.SetActive(false);
    }

    // Loading a save: put back fuses that were already slotted in.
    public void SetInstalled(int count)
    {
        installed = Mathf.Clamp(count, 0, Required);
        for (int i = 0; i < fuseSlots.Length; i++) fuseSlots[i].SetActive(i < installed);
    }

    public string Prompt
    {
        get
        {
            if (grid.IsOn) return null;
            if (installed >= Required) return "Pull the breaker";
            // Say how many fuses the player carries, so it's clear whether E will do anything.
            int carried = Carried;
            if (carried == 0) return "Fuse box (" + installed + "/" + Required + " fuses) - find fuses";
            return "Insert fuse (" + installed + "/" + Required + ", carrying " + carried + ")";
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
                                        : "All fuses in place   [" + Controls.Label(GameAction.Use) + "] pull the breaker");
    }
}
