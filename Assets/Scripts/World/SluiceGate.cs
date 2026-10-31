using System.Collections;
using UnityEngine;

// A relief sluice gate in a valve room: a steel gate over the mouth of a big pipe (a relief line from the
// cistern), above a drain shaft in the floor. The room's valve opens it (SewerValve): the gate grinds up, and
// the cistern's water roars out of the pipe and down the shaft for a while (taking the room's own flooded
// pit with it).
public class SluiceGate : MonoBehaviour
{
    public Transform gate;
    public float lift = 1.5f;
    public GameObject torrent;             // the falling water, shown while it pours
    public AudioSource roar;               // loops while it pours
    public float pourSeconds = 20f;        // as long as the cistern takes to drop a step

    public void Open(bool loud)
    {
        if (loud) StartCoroutine(Pour());
        else gate.position += Vector3.up * lift;
    }

    IEnumerator Pour()
    {
        Vector3 from = gate.position, to = from + Vector3.up * lift;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 2f)
        {
            gate.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        gate.position = to;
        if (torrent) torrent.SetActive(true);
        if (roar) { roar.volume = 0.9f; roar.Play(); }
        yield return new WaitForSeconds(pourSeconds);
        if (torrent) torrent.SetActive(false);
        for (float v = roar ? roar.volume : 0f; roar && v > 0f; v -= Time.deltaTime * 0.4f) { roar.volume = v; yield return null; }
        if (roar) roar.Stop();
    }
}
