using UnityEngine;

// Decides what Sousa writes in his journal, by watching the game twice a second: what he carries, what's
// been fixed, and which important things he has come close to. Each thought is written once; it's crossed
// out when it no longer matters. All lines are DRAFTS for the creative director to rewrite.
public class JournalDirector : MonoBehaviour
{
    public float noticeDistance = 4f;          // how close he has to come to a fuse box, generator, etc.

    Journal journal;
    PlayerInventory inv;
    Transform player;
    FuseBox fuseBox;
    GeneratorPuzzle generator;
    TrainConsole trainConsole;
    EmergencyLatch latch;
    HingedDoor cabinetDoor, officeDoor;
    float nextCheck;
    bool hollisPowerSeen;

    void Start()
    {
        journal = GetComponent<Journal>();
        inv = GetComponent<PlayerInventory>();
        player = transform;
        fuseBox = FindAnyObjectByType<FuseBox>();
        generator = FindAnyObjectByType<GeneratorPuzzle>();
        trainConsole = FindAnyObjectByType<TrainConsole>();
        latch = FindAnyObjectByType<EmergencyLatch>();
        foreach (var d in FindObjectsByType<HingedDoor>(FindObjectsSortMode.None))
        {
            if (d.needsCabinetKey) cabinetDoor = d;
            if (d.needsOfficeKey) officeDoor = d;
        }
    }

    bool Near(Component c) => c && Vector3.Distance(player.position, c.transform.position) < noticeDistance;

    void Update()
    {
        if (Time.time < nextCheck || !journal || !inv) return;
        nextCheck = Time.time + 0.5f;
        Vector3 p = player.position;
        bool canal = SaveSystem.IsCanal(p), sewers = SaveSystem.IsSewers(p), hollis = !canal && !sewers;
        bool hollisPower = fuseBox && fuseBox.grid && fuseBox.grid.IsOn;
        int fuses = inv.fuses + (fuseBox ? fuseBox.Installed : 0);
        bool genFound = journal.Has("generator");
        int fuel = inv.fuelCans + (generator ? generator.FuelPoured : 0);
        bool plugIn = generator && generator.PlugFitted, running = generator && generator.Running;

        // ---- Station 1, Hollis St
        if (hollis) Write("start", "Dispatch says the whole of Line 4 is dark. Start with Hollis St: the fuse box is down on the platform.");
        if (hollis && Near(fuseBox)) Write("fusebox", "Found the fuse box on the platform. It needs two fuses. Dispatch said there might be spares down here... somewhere.");
        if (fuses == 1 && !hollisPower) Write("fuse1", "One fuse. There has to be another one around here.");
        if (fuses >= 2 && !hollisPower) Write("fuse2", "That's both fuses. Back to the fuse box on the platform.");
        if (inv.hasCabinetKey) Write("cabkey", "Interesting key. Maybe it opens the blue cabinet on the main platform.");
        if (hollisPower) Write("power", "The lights are back. That locked door in the east tunnel, the staff room, might open now.");
        if (Near(trainConsole) && !inv.hasReverserKey) Write("train", "This train could get me out of here, but it needs a reverser key. The note says there's a spare in the staff room desk.");
        if (inv.hasReverserKey) Write("reverser", "The reverser key. Now, the train in the east tunnel.");

        // ---- Station 2, Canal St
        if (canal) Write("canal", "The train died before the station. If I can get some power back here, maybe there's a way out.");
        if (canal && Near(generator)) Write("generator", "I just found the generator. Bone dry, and the spark plug's shot. I should look around for some jerry cans... maybe downstairs.");
        if (fuel > 0 && !genFound) Write("can_before", "This can looks useful, maybe there's a generator nearby...");
        if (genFound && fuel == 1) Write("can_more", "One jerry can. The generator will need another one.");
        if (inv.hasSparkPlug && !genFound) Write("plug_before", "I wonder what this spark plug is for...");
        if (inv.hasSparkPlug && genFound) Write("plug_after", "A spark plug. That's what the generator needed.");
        if (inv.hasOfficeKey) Write("officekey", "A key tagged STATION MASTER. The office has to be around here somewhere.");
        if (canal && Near(latch) && !running) Write("latch", "An emergency latch in the floor. Sealed: it only opens on backup power.");
        if (running) Write("running", "The generator's running. The log said backup power opens the emergency latch on the lower level.");

        // ---- Station 3, the sewers (draft)
        if (sewers) Write("sewers", "The sewers. The water's high... there has to be a pumping station somewhere ahead.");

        // ---- what's done (crossed out)
        Done("start", journal.Has("fusebox") || hollisPower || !hollis);
        Done("fusebox", hollisPower || !hollis);
        Done("fuse1", fuses >= 2 || hollisPower);
        Done("fuse2", hollisPower);
        Done("cabkey", cabinetDoor && cabinetDoor.IsOpen);
        Done("power", SaveSystem.AreaAt(p) == "Staff Room" || inv.hasReverserKey || !hollis);
        Done("train", inv.hasReverserKey);
        Done("reverser", !hollis);
        Done("canal", genFound || running || sewers);
        Done("generator", running || sewers);
        Done("can_before", genFound);
        Done("can_more", fuel >= 2 || running);
        Done("plug_before", plugIn || genFound);
        Done("plug_after", plugIn);
        Done("officekey", officeDoor && officeDoor.IsOpen);
        Done("latch", running);
        Done("running", sewers);
    }

    void Write(string id, string text) => journal.Add(id, text);
    void Done(string id, bool done) => journal.SetDone(id, done);
}
