using System.Collections.Generic;
using UnityEngine;

// A big valve wheel in the sewers, turned by holding Use (about 4 s) while looking at it: the wheel squeals
// round; let go and it stops where it is (turning is slow and loud). Once fully open: a clunk, a deep
// groan in the pipes, and its flood zone drains (FloodZone, over 10 s with a roar). Every valve also lowers
// the cistern a step. The final valve (in the middle of the cistern) is seized until the four others are
// open; turning it purges the water out to the river and opens the way out (RiverExit).
public class SewerValve : MonoBehaviour, IInteractable
{
    public string id;
    public Transform wheel;
    public FloodZone zone;                 // the area this valve drains (none for the final valve)
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
        if (zone) { if (loud) zone.DrainTo(drainTo); else zone.SetLevel(drainTo); }
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
