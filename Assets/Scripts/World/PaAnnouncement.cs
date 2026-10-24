using System.Collections;
using UnityEngine;

// Scare: the station's PA speakers crackle on by themselves, though the power is out: a slowed, broken
// chime, static, a few garbled words (shown on screen), and a slow breath through the speakers.
public class PaAnnouncement : MonoBehaviour, IScare
{
    public AudioSource[] speakers;
    public AudioClip clip;
    [TextArea(1, 3)] public string[] lines;
    public float firstLineAt = 2.2f, secondsPerLine = 1.9f;

    public void Play(Transform player)
    {
        foreach (var s in speakers) if (s) s.PlayOneShot(clip);
        StartCoroutine(Subtitles());
    }

    IEnumerator Subtitles()
    {
        yield return new WaitForSeconds(firstLineAt);
        foreach (var line in lines)
        {
            Hud.Notice(line, secondsPerLine + 0.3f);
            yield return new WaitForSeconds(secondsPerLine);
        }
    }
}
