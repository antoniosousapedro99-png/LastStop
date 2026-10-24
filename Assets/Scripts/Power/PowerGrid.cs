using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The electricity for one area. Starts OFF. When the fuse box's breaker is pulled, Restore() plays
// the "power returns" moment: a heavy clunk, then every PoweredLight switches on in a wave that
// travels outward from the fuse box, a hum fades in and a short music sting plays.
// Doors and other things that need power listen to the Restored event.
public class PowerGrid : MonoBehaviour
{
    public AudioClip clunk;               // the breaker slamming over
    public AudioClip musicSting;          // short "power is back" music
    public AudioSource hum;               // electrical hum of the station, silent until power returns
    public float humVolume = 0.25f;
    public float delayBetweenLights = 0.18f;

    public bool IsOn { get; private set; }
    public Vector3 Origin { get; private set; }  // where the power came back from (the fuse box)

    // Fired once the clunk has played; listeners can use Origin to time themselves to the wave.
    public event Action Restored;

    public void Restore(Vector3 origin)
    {
        if (IsOn) return;
        IsOn = true;
        Origin = origin;
        StartCoroutine(PowerWave());
    }

    // Loading a save made after the power came back: everything simply on, with no clunk, wave or music,
    // and without the Restored event (the Pale Man and the autosave have their own saved state).
    public void RestoreQuietly(Vector3 origin)
    {
        if (IsOn) return;
        IsOn = true;
        Origin = origin;
        foreach (var light in FindObjectsByType<PoweredLight>()) light.PowerOn();
        if (hum) { hum.volume = humVolume; hum.Play(); }
    }

    IEnumerator PowerWave()
    {
        if (clunk) AudioSource.PlayClipAtPoint(clunk, Origin, 1f);
        yield return new WaitForSeconds(0.7f);

        if (musicSting)
        {
            var music = gameObject.AddComponent<AudioSource>();
            music.clip = musicSting; music.spatialBlend = 0f;
            music.ignoreListenerVolume = true;              // music has its own volume setting
            music.volume = 0.6f * GameSettings.MusicLevel;
            music.Play();
        }
        Restored?.Invoke();
        if (hum) StartCoroutine(FadeInHum());

        // Nearest lights first, so the light visibly spreads away from the fuse box.
        var lights = new List<PoweredLight>(FindObjectsByType<PoweredLight>());
        lights.Sort((a, b) => Vector3.Distance(a.transform.position, Origin)
                              .CompareTo(Vector3.Distance(b.transform.position, Origin)));
        foreach (var light in lights)
        {
            light.PowerOn();
            yield return new WaitForSeconds(delayBetweenLights);
        }
    }

    IEnumerator FadeInHum()
    {
        hum.volume = 0f;
        hum.Play();
        for (float t = 0; t < 4f; t += Time.deltaTime)
        {
            hum.volume = humVolume * t / 4f;
            yield return null;
        }
        hum.volume = humVolume;
    }
}
