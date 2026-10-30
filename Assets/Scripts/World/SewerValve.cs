using System.Collections.Generic;
using UnityEngine;

// A big valve wheel in the sewers, turned by holding Use (about 4 s) while looking at it: the wheel squeals
// round; let go and it stops where it is (turning is slow and loud). Each of the four valve rooms' valves
// works that room's relief sluice gate (SluiceGate): once fully open, a clunk, the gate grinds up and the
// cistern's water roars out of the relief pipe and down the drain shaft, so the cistern drops a step and
// the room's own flooded pit drains (FloodZone). The final valve, on the cistern's central platform (only
// reachable once the four have drained the tank), opens the main outfall gate: the rest of the water purges
// out to the river and the way out opens (RiverExit).
public class SewerValve : MonoBehaviour, IInteractable
{
    public string id;
    public Transform wheel;
    public FloodZone zone;                 // the room's flooded pit (none for the final valve)
    public SluiceGate gate;                // the relief gate it lifts (none for the final valve)
    public float drainTo;
    public FloodZone cistern;
    public float cisternStart, cisternStep, cisternPurged;
    public bool final;
    public RiverExit exit;                 // the final valve opens it
    public float turnSeconds = 4f;
    public AudioSource squeal;             // loops while turning
    public AudioClip clunk, groan;

    public bool Done { get; private set; }
    public static bool Busy { get; private set; }   // the player is holding on to a wheel (PlayerSwim stays put)

    static readonly List<SewerValve> all = new List<SewerValve>();
    float progress;
    Interactor turner;

    void OnEnable() => all.Add(this);
    void OnDisable() { all.Remove(this); if (turner) Busy = false; }

    static int Opened() { int n = 0; foreach (var v in all) if (v.Done && !v.final) n++; return n; }
    bool Ready => !final || Opened() >= 4;

    public string Prompt => Done ? null : Ready ? "Turn the valve (hold)" : "Seized solid. Too much pressure still in the pipes.";

    public void Interact(Interactor player)
    {
        if (Done || !Ready) return;
        turner = player;
    }

    void Update()
    {
        if (turner == null || Done) return;
        if (!Controls.Held(GameAction.Use) || !ReferenceEquals(turner.Target, this))
        {
            turner = null; Busy = false;
            if (squeal) squeal.Stop();
            return;
        }
        Busy = true;
        if (squeal && !squeal.isPlaying) squeal.Play();
        progress += Time.deltaTime / turnSeconds;
        wheel.Rotate(0f, 80f * Time.deltaTime, 0f, Space.Self);
        Noise.Make(transform.position, 12f);
        if (progress >= 1f) Finish(true);
    }

    void Finish(bool loud)
    {
        Done = true;
        turner = null; Busy = false;
        if (squeal) squeal.Stop();
        if (loud)
        {
            if (clunk) AudioSource.PlayClipAtPoint(clunk, transform.position, 1f);
            if (groan) AudioSource.PlayClipAtPoint(groan, transform.position, 1f);
        }
        if (gate) gate.Open(loud);
        if (zone) { if (loud) zone.DrainTo(drainTo, 12f); else zone.SetLevel(drainTo); }
        if (cistern)
        {
            float to = final ? cisternPurged : cisternStart - cisternStep * Opened();
            if (loud) cistern.DrainTo(to, final ? 14f : 10f); else cistern.SetLevel(Mathf.Min(cistern.level, to));
        }
        if (final && exit) exit.Open(loud);
    }

    // Loading a save: already open, the water already gone.
    public void SetDone() { if (!Done) Finish(false); }

    public static SewerValve Find(string id) => all.Find(v => v.id == id);
    public static IEnumerable<SewerValve> All => all;
}
