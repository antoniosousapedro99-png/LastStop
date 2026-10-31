using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Saving and loading games.
// - A save is a SaveData written as a small JSON file in the player's save folder
//   (Application.persistentDataPath/Saves): slot 0 is the autosave, slots 1-5 are the player's.
// - Loading reloads the level (so it starts clean, exactly as built) and then puts everything back the
//   way the save describes (Apply). MenuSystem notices the waiting save and skips the title.
// - Things in the level (pickups, doors) are found again by their place in the hierarchy, their "path",
//   recorded when the level starts, before anything has been picked up or opened.
public static class SaveSystem
{
    public const int Autosave = 0;
    public const int Slots = 5;

    public static SaveData Pending { get; private set; }    // a save waiting to be applied after the reload
    public static bool LoadedGame { get; private set; }     // this run of the level came from a save
    public static float PlaySeconds;                         // time played; MenuSystem counts it while in the game

    static readonly Dictionary<string, Pickup> pickups = new Dictionary<string, Pickup>();
    static readonly Dictionary<string, HingedDoor> doors = new Dictionary<string, HingedDoor>();
    static readonly Dictionary<string, SlidingDrawer> drawers = new Dictionary<string, SlidingDrawer>();
    static readonly Dictionary<string, ScareTrigger> scares = new Dictionary<string, ScareTrigger>();

    static string Folder => Path.Combine(Application.persistentDataPath, "Saves");
    static string FileFor(int slot) => Path.Combine(Folder, slot == Autosave ? "autosave.json" : "slot" + slot + ".json");

    // ---------------------------------------------------------------- level start
    // Called by MenuSystem as the level wakes up: remembers every pickup and door by its path.
    public static void RememberLevel()
    {
        LoadedGame = Pending != null;
        PlaySeconds = 0f;
        pickups.Clear();
        doors.Clear();
        drawers.Clear();
        scares.Clear();
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include))
            pickups[PathOf(p.transform)] = p;
        foreach (var d in UnityEngine.Object.FindObjectsByType<HingedDoor>(FindObjectsInactive.Include))
            doors[PathOf(d.transform)] = d;
        foreach (var d in UnityEngine.Object.FindObjectsByType<SlidingDrawer>(FindObjectsInactive.Include))
            drawers[PathOf(d.transform)] = d;
        foreach (var s in UnityEngine.Object.FindObjectsByType<ScareTrigger>(FindObjectsInactive.Include))
            scares[PathOf(s.transform)] = s;
    }

    // "Root#0/Pickups#3/Battery#7": names plus their position among their siblings, so two objects
    // with the same name still get different paths. The level is generated, so these never change.
    static string PathOf(Transform t)
    {
        string path = t.name + "#" + t.GetSiblingIndex();
        for (var p = t.parent; p != null; p = p.parent) path = p.name + "#" + p.GetSiblingIndex() + "/" + path;
        return path;
    }

    // ---------------------------------------------------------------- files
    public static SaveData Peek(int slot)
    {
        try
        {
            string file = FileFor(slot);
            return File.Exists(file) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(file)) : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't read save " + slot + ": " + e.Message);
            return null;
        }
    }

    public static bool Write(int slot, string name)
    {
        try
        {
            var data = Capture();
            data.name = string.IsNullOrWhiteSpace(name) ? DefaultName() : name.Trim();
            data.savedAt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FileFor(slot), JsonUtility.ToJson(data, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't save to slot " + slot + ": " + e.Message);
            return false;
        }
    }

    // Starts loading: reloads the level; MenuSystem then calls ApplyPending once it's back.
    public static void Load(int slot)
    {
        var data = Peek(slot);
        if (data == null) return;
        Pending = data;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // "Platform - 14:32": where the player is and the time of day, the suggested name for a new save.
    public static string DefaultName()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        return (player ? AreaAt(player.transform.position) : "Station") + " - " + DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    // A plain name for each part of the level, from a position (the layout in LookTestBuilder).
    // Canal St Junction is built far from Hollis St, beyond z 250.
    public static bool IsCanal(Vector3 p) => p.z > 250f && p.z < 550f;
    // The sewers (Station 3) are built further still, beyond z 550.
    public static bool IsSewers(Vector3 p) => p.z > 550f;

    public static string AreaAt(Vector3 p)
    {
        if (IsSewers(p)) return "The Sewers";
        if (IsCanal(p))
        {
            if (p.y > 9f) return "Canal St - Street Exit";
            if (p.y > 4f) return p.z > 321f ? "Canal St - Station Office" : p.z < 300f ? "Canal St - Generator Room" : "Canal St - Mezzanine";
            return p.x < -40f || p.x > 40f ? "Canal St - Tunnel" : "Canal St - Platform";
        }
        if (p.y > 3f) return "Street";
        if (p.x > 40f && p.x < 45.5f && p.z > 25.8f) return "Staff Room";
        if (p.x < -18.3f) return "West Tunnel";
        if (p.x > 18.3f) return "East Tunnel";
        if (p.z < 13.7f) return "Corridor";
        return p.y < -0.5f ? "Tracks" : "Platform";
    }

    // "24 OCT 14:32" for the slot list.
    public static string ShortDate(SaveData data)
    {
        if (!DateTime.TryParse(data.savedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)) return "";
        return when.ToString("dd MMM  HH:mm", CultureInfo.InvariantCulture).ToUpperInvariant();
    }

    // ---------------------------------------------------------------- capture and apply
    static SaveData Capture()
    {
        var d = new SaveData();
        var inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        var player = inv.transform;
        d.area = AreaAt(player.position);
        d.playSeconds = PlaySeconds;
        d.playerPosition = player.position;
        d.playerYaw = player.eulerAngles.y;
        var look = player.GetComponent<PlayerLook>();
        if (look) d.cameraPitch = look.pitch;
        var health = player.GetComponent<PlayerHealth>();
        if (health) d.hits = health.Hits;
        var battery = player.GetComponentInChildren<FlashlightBattery>(true);
        if (battery) d.batteryCharge = battery.charge;
        var torch = player.GetComponentInChildren<Flashlight>(true);
        d.torchOn = torch && torch.IsOn;

        d.fuses = inv.fuses; d.spareBatteries = inv.spareBatteries; d.flares = inv.flares;
        d.painkillers = inv.painkillers; d.shells = inv.shells;
        d.hasCabinetKey = inv.hasCabinetKey; d.hasShotgun = inv.hasShotgun; d.hasReverserKey = inv.hasReverserKey;
        d.fuelCans = inv.fuelCans; d.hasSparkPlug = inv.hasSparkPlug; d.hasOfficeKey = inv.hasOfficeKey;
        d.hasBreakerHandle = inv.hasBreakerHandle;
        var generator = UnityEngine.Object.FindAnyObjectByType<GeneratorPuzzle>();
        if (generator) { d.generatorFuel = generator.FuelPoured; d.generatorPlug = generator.PlugFitted; d.generatorRunning = generator.Running; }

        var fuseBox = UnityEngine.Object.FindAnyObjectByType<FuseBox>();
        if (fuseBox) { d.fusesInstalled = fuseBox.Installed; d.powerOn = fuseBox.grid.IsOn; }
        foreach (var pair in pickups) if (pair.Value == null) d.taken.Add(pair.Key);           // destroyed = picked up
        foreach (var pair in doors) if (pair.Value != null && pair.Value.IsOpen) d.openDoors.Add(pair.Key);
        foreach (var pair in drawers) if (pair.Value != null && pair.Value.IsOpen) d.openDrawers.Add(pair.Key);
        foreach (var pair in scares) if (pair.Value != null && pair.Value.Fired) d.firedScares.Add(pair.Key);
        foreach (var phone in UnityEngine.Object.FindObjectsByType<VoicemailPhone>())
        {
            if (phone.Heard) d.heardVoicemails.Add(phone.id);
            if (phone.Ignored) d.ignoredVoicemails.Add(phone.id);
        }
        var journal = Journal.Current;
        if (journal) journal.Save(d.journalIds, d.journalTexts);

        foreach (var valve in SewerValve.All) if (valve.Done) d.sewerValves.Add(valve.id);
        var breaker = UnityEngine.Object.FindAnyObjectByType<SewerBreaker>();
        if (breaker) { d.sewerHandleFitted = breaker.HandleFitted; d.sewerPowerOn = breaker.grid.IsOn; }
        var shortcut = UnityEngine.Object.FindAnyObjectByType<BoltedGate>();
        if (shortcut) d.sewerShortcutOpen = shortcut.IsOpen;

        var monster = UnityEngine.Object.FindAnyObjectByType<FrankieAI>();   // only found while he's awake
        if (monster)
        {
            d.monsterAwake = true;
            d.monsterPosition = monster.transform.position;
            d.monsterYaw = monster.transform.eulerAngles.y;
        }
        return d;
    }

    // Called by MenuSystem after the reload, once the player has been switched on.
    public static void ApplyPending()
    {
        var d = Pending;
        Pending = null;
        if (d == null) return;

        foreach (string path in d.taken)
            if (pickups.TryGetValue(path, out var p) && p) UnityEngine.Object.Destroy(p.gameObject);
        foreach (string path in d.openDoors)
            if (doors.TryGetValue(path, out var door) && door) door.OpenNow();
        foreach (string path in d.openDrawers)
            if (drawers.TryGetValue(path, out var drawer) && drawer) drawer.OpenNow();
        foreach (string path in d.firedScares)
            if (scares.TryGetValue(path, out var scare) && scare) scare.MarkFired();
        foreach (var phone in UnityEngine.Object.FindObjectsByType<VoicemailPhone>())
        {
            if (d.heardVoicemails.Contains(phone.id)) phone.SetHeard();
            if (d.ignoredVoicemails.Contains(phone.id)) phone.SetIgnored();
        }
        Journal.Current?.Load(d.journalIds, d.journalTexts);
        PlaySeconds = d.playSeconds;

        var inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        inv.fuses = d.fuses; inv.spareBatteries = d.spareBatteries; inv.flares = d.flares;
        inv.painkillers = d.painkillers; inv.shells = d.shells;
        inv.hasCabinetKey = d.hasCabinetKey; inv.hasShotgun = d.hasShotgun; inv.hasReverserKey = d.hasReverserKey;
        inv.fuelCans = d.fuelCans; inv.hasSparkPlug = d.hasSparkPlug; inv.hasOfficeKey = d.hasOfficeKey;
        inv.hasBreakerHandle = d.hasBreakerHandle;
        var generator = UnityEngine.Object.FindAnyObjectByType<GeneratorPuzzle>();
        if (generator) generator.LoadState(d.generatorFuel, d.generatorPlug, d.generatorRunning);
        // The sewer valves already open: the four first (they set the cistern's level), the final one last.
        foreach (bool finalOne in new[] { false, true })
            foreach (string id in d.sewerValves)
            {
                var valve = SewerValve.Find(id);
                if (valve && valve.final == finalOne) valve.SetDone();
            }
        UnityEngine.Object.FindAnyObjectByType<SewerBreaker>()?.LoadState(d.sewerHandleFitted, d.sewerPowerOn);
        if (d.sewerShortcutOpen) UnityEngine.Object.FindAnyObjectByType<BoltedGate>()?.OpenNow();

        var player = inv.transform;
        var battery = player.GetComponentInChildren<FlashlightBattery>(true);
        if (battery) battery.charge = d.batteryCharge;
        var torch = player.GetComponentInChildren<Flashlight>(true);
        if (torch) torch.SetOn(d.torchOn);
        var health = player.GetComponent<PlayerHealth>();
        if (health) health.SetHits(d.hits);

        var fuseBox = UnityEngine.Object.FindAnyObjectByType<FuseBox>();
        if (fuseBox)
        {
            fuseBox.SetInstalled(d.fusesInstalled);
            if (d.powerOn)
            {
                fuseBox.grid.RestoreQuietly(fuseBox.transform.position);
                foreach (var door in doors.Values)
                    if (door && door.needsPower == fuseBox.grid) door.OpenNow();   // even if it was still swinging when saved
            }
        }
        var arrival = UnityEngine.Object.FindAnyObjectByType<FrankieArrival>();
        bool atCanal = IsCanal(d.playerPosition) || IsSewers(d.playerPosition);
        if (arrival) arrival.LoadState(d.monsterAwake, d.monsterPosition, d.monsterYaw, d.powerOn || atCanal);
        if (atCanal)
        {
            if (!d.monsterAwake && arrival) { arrival.StopAllCoroutines(); arrival.WakeNow(); arrival.monster.gameObject.SetActive(false); }   // not at Hollis St
            UnityEngine.Object.FindAnyObjectByType<CanalArrival>()?.LoadState(d.monsterAwake);
        }
        if (IsSewers(d.playerPosition))   // Frankie never follows into the sewers
        {
            var frankie = UnityEngine.Object.FindAnyObjectByType<FrankieAI>(FindObjectsInactive.Include);
            if (frankie) frankie.gameObject.SetActive(false);
        }

        // Move the player last; the CharacterController has to be off to be moved directly.
        var controller = player.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
        player.SetPositionAndRotation(d.playerPosition, Quaternion.Euler(0f, d.playerYaw, 0f));
        if (controller) controller.enabled = true;
        var look = player.GetComponent<PlayerLook>();
        if (look) look.pitch = d.cameraPitch;
    }
}
