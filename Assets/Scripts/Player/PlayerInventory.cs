using UnityEngine;

// What the player is carrying: fuses, spare flashlight batteries, flares, painkillers, the cabinet key,
// the train's reverser key,
// and the shotgun with loose shells (switched off for now; shells already loaded are counted by the Shotgun).
public class PlayerInventory : MonoBehaviour
{
    public int fuses;
    public int spareBatteries;
    public bool hasShotgun;
    public int shells;
    public bool hasCabinetKey;
    public bool hasReverserKey;   // starts the east train (Station 1's way out)
    public int fuelCans;          // Canal St's generator needs two
    public bool hasSparkPlug;     // ...and a spark plug
    public bool hasOfficeKey;     // opens Canal St's station master's office
    public int flares;
    public int painkillers;    // collected only for now: they will heal once the player has health
}
