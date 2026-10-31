using UnityEngine;

// An item lying in the world: a fuse, a spare battery, a flare, painkillers, a key (or the shotgun and its shells,
// switched off for now). Left click to take it.
public class Pickup : MonoBehaviour, IInteractable
{
    public enum Kind { Fuse, Battery, Shotgun, Shell, Key, Flare, Painkillers, ReverserKey, FuelCan, SparkPlug, OfficeKey, BreakerHandle }   // new kinds go at the end
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
        Kind.ReverserKey => "Take reverser key",
        Kind.FuelCan => "Take fuel can",
        Kind.SparkPlug => "Take spark plug",
        Kind.OfficeKey => "Take key",
        Kind.BreakerHandle => "Take breaker handle",
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
                Hud.Notice("Spare battery (" + inv.spareBatteries + ")   [" + Controls.Label(GameAction.SwapBattery) + "] swap flashlight battery");
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
                Hud.Notice("Road flare (" + inv.flares + ")   [" + Controls.Label(GameAction.Flare) + "] strike");
                break;
            case Kind.Painkillers:
                inv.painkillers++;
                Hud.Notice("Painkillers (" + inv.painkillers + ")");
                break;
            case Kind.ReverserKey:
                inv.hasReverserKey = true;
                Hud.Notice("Reverser key. It wakes a train's controls.", 4f);
                break;
            case Kind.FuelCan:
                inv.fuelCans++;
                Hud.Notice("Fuel can (" + inv.fuelCans + " carried)", 3f);
                break;
            case Kind.SparkPlug:
                inv.hasSparkPlug = true;
                Hud.Notice("Spark plug. For the backup generator.", 3f);
                break;
            case Kind.OfficeKey:
                inv.hasOfficeKey = true;
                Hud.Notice("A key. Its tag reads 'STATION MASTER'.", 3.5f);
                break;
            case Kind.BreakerHandle:
                inv.hasBreakerHandle = true;
                Hud.Notice("A breaker handle. Pulled off a switch and left here.", 3.5f);
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
