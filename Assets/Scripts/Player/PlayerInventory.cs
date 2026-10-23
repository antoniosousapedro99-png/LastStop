using UnityEngine;

// What the player is carrying: fuses, spare flashlight batteries, flares, painkillers, the cabinet key,
// and the shotgun with loose shells (switched off for now; shells already loaded are counted by the Shotgun).
public class PlayerInventory : MonoBehaviour
{
    public int fuses;
    public int spareBatteries;
    public bool hasShotgun;
    public int shells;
    public bool hasCabinetKey;
    public int flares;
    public int painkillers;    // collected only for now: they will heal once the player has health
}
