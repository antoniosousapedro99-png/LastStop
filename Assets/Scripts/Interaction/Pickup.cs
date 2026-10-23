using UnityEngine;

// An item lying in the world: a fuse, a spare battery, a flare, painkillers, a key (or the shotgun and its shells,
// switched off for now). Left click to take it.
public class Pickup : MonoBehaviour, IInteractable
{
    public enum Kind { Fuse, Battery, Shotgun, Shell, Key, Flare, Painkillers }   // new kinds go at the end
    public Kind kind;
    public AudioClip pickupSound;

    public string Prompt => kind switch
    {
        Kind.Fuse => "Pick up fuse",
        Kind.Battery => "Pick up battery",
        Kind.Shotgun => "Take shotgun",
        Kind.Key => "Take key",
        Kind.Flare => "Take flare",
        Kind.Painkillers => "Take painkillers",
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
                Hud.Notice("Shotgun   [hold RMB] aim   [LMB] fire   [R] reload", 5f);
                break;
            case Kind.Key:
                inv.hasCabinetKey = true;
                Hud.Notice("A small key. Tag: 'PLATFORM CABINET'", 4f);
                break;
            case Kind.Flare:
                inv.flares++;
                Hud.Notice("Road flare (" + inv.flares + ")   [G] strike");
                break;
            case Kind.Painkillers:
                inv.painkillers++;
                Hud.Notice("Painkillers (" + inv.painkillers + ")");
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
