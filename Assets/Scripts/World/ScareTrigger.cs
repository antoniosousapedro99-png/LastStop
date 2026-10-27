using UnityEngine;

// Anything that can be set off by a ScareTrigger (a lamp dying, a rumble, the PA...).
public interface IScare
{
    void Play(Transform player);
}

// A scare that leaves something behind (fallen rubble...): told when a loaded save says it already happened,
// so it can put the aftermath back.
public interface IScareAftermath
{
    void AlreadyHappened();
}

// A one-time scare spot: an invisible box (on the Ignore Raycast layer, so the crosshair passes through)
// that sets off the scares on the same object (IScare) the first time the player walks in.
// 'power' says whether it only works before the power is back, only after, or either way.
// Whether it has fired is kept in saves (SaveSystem).
public class ScareTrigger : MonoBehaviour
{
    public enum PowerRule { Any, BeforePower, AfterPower }
    public PowerRule power;
    public PowerGrid grid;

    public bool Fired { get; private set; }

    // Loading a save where this has already happened.
    public void MarkFired()
    {
        Fired = true;
        foreach (var after in GetComponents<IScareAftermath>()) after.AlreadyHappened();
    }

    void OnTriggerEnter(Collider other)
    {
        if (Fired || !other.GetComponentInParent<PlayerInventory>()) return;
        bool on = grid && grid.IsOn;
        if (power == PowerRule.BeforePower && on) return;
        if (power == PowerRule.AfterPower && !on) return;
        Fired = true;
        foreach (var scare in GetComponents<IScare>()) scare.Play(other.transform);
    }
}
