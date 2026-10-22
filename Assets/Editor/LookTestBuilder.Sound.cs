using UnityEditor;
using UnityEngine;

// The level's background sound: area ambience (rain and wind on the street, a drone in the station,
// a darker drone in the tunnels), rare distant noises underground, and a leaking pipe by the
// track puddle. Footstep surfaces and puddle splashes are set up where those floors are built.
public static partial class LookTestBuilder
{
    static void BuildSoundscape(Transform player)
    {
        var sound = Group("Soundscape");

        var ambience = sound.gameObject.AddComponent<AreaAmbience>();
        ambience.player = player;
        ambience.rain = Loop(sound, "Rain", "Assets/Art/Audio/Ambience/Rain/StreetRain.ogg");
        ambience.streetDrone = Loop(sound, "StreetWind", AmbienceDir + "/DSGNDron_Tonal-Tube Wind_HY_SD.wav");
        ambience.stationDrone = Loop(sound, "StationDrone", AmbienceDir + "/DSGNDron_Tonal-Too Quiet_HY_SD.wav");
        ambience.tunnelDrone = Loop(sound, "TunnelDrone", AmbienceDir + "/DSGNDron_Tonal-Always Observing_HY_SD.wav");

        var noises = sound.gameObject.AddComponent<DistantNoises>();
        noises.player = player;
        noises.clips = new[]
        {
            Load<AudioClip>(SfxDir + "/Rockfall1.wav"), Load<AudioClip>(SfxDir + "/Rockfall2.wav"), Load<AudioClip>(SfxDir + "/Rockfall3.wav"),
            Load<AudioClip>(SfxDir + "/Gravelfall1.wav"), Load<AudioClip>(SfxDir + "/Gravelfall2.wav"), Load<AudioClip>(SfxDir + "/Gravelfall3.wav"),
            Load<AudioClip>(SfxDir + "/CrowbarDrag1.wav"), Load<AudioClip>(SfxDir + "/OldDoorCreak.wav"),
        };

        // A pipe leaking somewhere above the track puddle: a 3D loop you hear only nearby.
        var pipe = new GameObject("LeakingPipe").AddComponent<AudioSource>();
        pipe.transform.SetParent(sound);
        pipe.transform.position = new Vector3(3.5f, 2.2f, 25.3f);
        pipe.clip = Load<AudioClip>(SfxDir + "/OverflowingWaterPipeLoop.wav");
        pipe.loop = true; pipe.playOnAwake = true; pipe.volume = 0.35f;
        pipe.spatialBlend = 1f; pipe.rolloffMode = AudioRolloffMode.Linear;
        pipe.minDistance = 2f; pipe.maxDistance = 14f;
    }

    // A looping 2D sound that starts silent; AreaAmbience sets its volume.
    static AudioSource Loop(Transform parent, string name, string clipPath)
    {
        var source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(parent);
        source.clip = Load<AudioClip>(clipPath);
        source.loop = true; source.playOnAwake = true; source.volume = 0f; source.spatialBlend = 0f;
        return source;
    }
}
