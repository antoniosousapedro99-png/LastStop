using System;
using System.Collections.Generic;
using UnityEngine;

// Everything one save remembers. Written to disk as JSON by SaveSystem.
// When a new feature has state worth keeping, add a field here and fill/use it in SaveSystem.Capture/Apply.
[Serializable]
public class SaveData
{
    public int version = 1;
    public string name;              // what the player called it
    public string savedAt;           // date and time, ISO format
    public string area;              // where the player was, e.g. "Staff Room"
    public float playSeconds;        // time played so far (shown on the end card)

    // The player
    public Vector3 playerPosition;
    public float playerYaw, cameraPitch;
    public int hits;                 // hits taken (0 = unhurt)
    public float batteryCharge;
    public bool torchOn;

    // What they carry
    public int fuses, spareBatteries, flares, painkillers, shells;
    public bool hasCabinetKey, hasShotgun, hasReverserKey;
    public int fuelCans;
    public bool hasSparkPlug, hasOfficeKey;

    // The level
    public int fusesInstalled;
    public bool powerOn;
    public List<string> taken = new List<string>();       // pickups no longer in the level (by hierarchy path)
    public List<string> openDoors = new List<string>();   // doors, lockers and cabinets standing open
    public List<string> openDrawers = new List<string>();
    public List<string> firedScares = new List<string>();   // one-time scares that already happened

    // Canal St Junction
    public int generatorFuel;
    public bool generatorPlug, generatorRunning;

    // Frankie
    public bool monsterAwake;
    public Vector3 monsterPosition;
    public float monsterYaw;
}
