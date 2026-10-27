using System.Collections;
using UnityEngine;

// Scare: as the player approaches, a hiding locker bangs twice from inside (the door jolts in its frame).
// Open it and there's nobody there.
public class LockerKnock : MonoBehaviour, IScare
{
    public HidingLocker locker;
    public int knocks = 2;
    public float delay = 0.4f, gap = 0.75f;

    public void Play(Transform player) => StartCoroutine(Knock());

    IEnumerator Knock()
    {
        yield return new WaitForSeconds(delay);
        for (int i = 0; i < knocks; i++)
        {
            locker.Bang();
            yield return new WaitForSeconds(gap);
        }
    }
}
