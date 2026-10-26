using System.Collections;
using UnityEngine;

// The roller shutter over Canal St's street exit. Shut until the station has power; then it rattles up.
public class RollerShutter : MonoBehaviour
{
    public PowerGrid grid;
    public float rise = 3f;                // metres it rolls up
    public float seconds = 4f;
    public AudioClip rattle;

    bool opened;
    Vector3 shutPosition;

    void Awake() => shutPosition = transform.position;

    public bool Open => opened;

    void Update()
    {
        if (opened || !grid || !grid.IsOn) return;
        opened = true;
        // Straight open if the power was already on when the level started (a loaded save).
        if (Time.timeSinceLevelLoad < 3f) transform.position = shutPosition + Vector3.up * rise;
        else StartCoroutine(Roll());
    }

    IEnumerator Roll()
    {
        yield return new WaitForSeconds(2f);   // after the lights have come on
        if (rattle) AudioSource.PlayClipAtPoint(rattle, transform.position, 1f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            transform.position = shutPosition + Vector3.up * (rise * t / seconds);
            yield return null;
        }
        transform.position = shutPosition + Vector3.up * rise;
    }
}
