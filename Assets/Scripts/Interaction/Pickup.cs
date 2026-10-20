using UnityEngine;

// An item lying in the world: a fuse, a spare battery, the shotgun or a shotgun shell. Press E to take it.
public class Pickup : MonoBehaviour, IInteractable
{
    public enum Kind { Fuse, Battery, Shotgun, Shell }
    public Kind kind;
    public AudioClip pickupSound;

    public string Prompt => kind switch
    {
        Kind.Fuse => "Pick up fuse",
        Kind.Battery => "Pick up battery",
        Kind.Shotgun => "Take shotgun",
        _ => "Take shotgun shell",
    };

    public void Interact(Interactor player)
    {
        var inv = player.inventory;
        switch (kind)
        {
            case Kind.Fuse:
                inv.fuses++;
                Hud.Notice("Fuse (" + inv.fuses + " carried)");
                break;
            case Kind.Battery:
                inv.spareBatteries++;
                Hud.Notice("Spare battery (" + inv.spareBatteries + ")   [H] swap flashlight battery");
                break;
            case Kind.Shotgun:
                inv.hasShotgun = true;
                Hud.Notice("Shotgun   [Q] raise   [LMB] fire   [R] reload", 5f);
                break;
            case Kind.Shell:
                inv.shells++;
                Hud.Notice("Shotgun shell (" + inv.shells + ")");
                break;
        }
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);
        Destroy(gameObject);
    }
}
