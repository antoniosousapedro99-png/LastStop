using UnityEngine;

// Scare: the lamp the player has just walked under pops and goes dark behind them for a few seconds,
// then stutters back on.
public class LampCutOut : MonoBehaviour, IScare
{
    public PoweredLight lamp;
    public float seconds = 4f;
    public AudioClip pop;

    public void Play(Transform player) => lamp.CutOut(seconds, pop);
}
