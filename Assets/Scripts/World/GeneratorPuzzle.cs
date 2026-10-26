using System.Collections;
using UnityEngine;

// Canal St Junction's backup generator: the way to bring the station's power back.
// It needs two cans of fuel and a new spark plug. Then the player pulls the starter cord: the first pull
// only sputters, the second catches. It roars into life, loud enough to bring Frankie, and the station's
// power comes back (PowerGrid): lights, escalators, the street shutter.
public class GeneratorPuzzle : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public int fuelNeeded = 2;
    public AudioSource engine;             // the running loop, on the generator
    public AudioClip pour, fitPlug, pull, sputter;

    public int FuelPoured { get; private set; }
    public bool PlugFitted { get; private set; }
    public bool Running { get; private set; }

    int pulls;
    bool busy;
    PlayerInventory inventory;
    PlayerInventory Inventory => inventory ? inventory : inventory = FindAnyObjectByType<PlayerInventory>();

    public string Prompt
    {
        get
        {
            if (Running || busy) return null;
            if (FuelPoured < fuelNeeded)
                return Inventory && Inventory.fuelCans > 0 ? "Pour fuel into the generator (" + FuelPoured + "/" + fuelNeeded + ")" : "Generator - the tank is dry (" + FuelPoured + "/" + fuelNeeded + " fuel)";
            if (!PlugFitted)
                return Inventory && Inventory.hasSparkPlug ? "Fit the spark plug" : "Generator - the spark plug is missing";
            return "Pull the starter cord";
        }
    }

    public void Interact(Interactor player)
    {
        if (Running || busy) return;
        if (FuelPoured < fuelNeeded)
        {
            if (Inventory.fuelCans <= 0) { Hud.Notice("It needs fuel. Two cans should do.", 3f); return; }
            Inventory.fuelCans--;
            FuelPoured++;
            if (pour) AudioSource.PlayClipAtPoint(pour, transform.position, 0.9f);
            Hud.Notice(FuelPoured < fuelNeeded ? "Fuel poured (" + FuelPoured + "/" + fuelNeeded + ")" : "The tank is full.", 2.5f);
            return;
        }
        if (!PlugFitted)
        {
            if (!Inventory.hasSparkPlug) { Hud.Notice("The spark plug socket is empty. There must be a spare somewhere.", 3.5f); return; }
            Inventory.hasSparkPlug = false;
            PlugFitted = true;
            if (fitPlug) AudioSource.PlayClipAtPoint(fitPlug, transform.position, 1f);
            Hud.Notice("Spark plug fitted. Now the cord.", 2.5f);
            return;
        }
        StartCoroutine(Pull());
    }

    IEnumerator Pull()
    {
        busy = true;
        pulls++;
        if (pull) AudioSource.PlayClipAtPoint(pull, transform.position, 1f);
        yield return new WaitForSeconds(0.4f);
        if (pulls < 2)
        {
            if (sputter) AudioSource.PlayClipAtPoint(sputter, transform.position, 1f);
            Noise.Make(transform.position, 25f);
            Hud.Notice("It coughs and dies. Again.", 2.5f);
            yield return new WaitForSeconds(1.5f);
            busy = false;
            yield break;
        }
        if (sputter) AudioSource.PlayClipAtPoint(sputter, transform.position, 1f);
        yield return new WaitForSeconds(0.8f);
        StartRunning();
        Noise.Make(transform.position, 80f);   // the whole station hears it
        grid.Restore(transform.position);
        busy = false;
    }

    void StartRunning()
    {
        Running = true;
        if (engine) { engine.loop = true; engine.Play(); }
    }

    // Loading a save.
    public void LoadState(int fuel, bool plug, bool running)
    {
        FuelPoured = fuel;
        PlugFitted = plug;
        pulls = running ? 2 : 0;
        if (!running) return;
        StartRunning();
        grid.RestoreQuietly(transform.position);
    }
}
