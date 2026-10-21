using UnityEngine;

// What the player is carrying: fuses, spare flashlight batteries, the shotgun, loose shells and the cabinet key
// (shells already loaded in the shotgun are counted by the Shotgun itself).
public class PlayerInventory : MonoBehaviour
{
    public int fuses;
    public int spareBatteries;
    public bool hasShotgun;
    public int shells;
    public bool hasCabinetKey;
}
