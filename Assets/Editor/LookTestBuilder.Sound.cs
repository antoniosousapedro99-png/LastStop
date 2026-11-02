using UnityEditor;
using UnityEngine;

// The level's background sound: area ambience (rain and wind on the street, a drone in the station,
// a darker drone in the tunnels), rare distant noises underground, and a leaking pipe by the
// track puddle. Footstep surfaces and puddle splashes are set up where those floors are built.
// Added 2026-10-31 (the director's sound pass, all subtle): water dripping somewhere in the tunnels and
// service levels, a far-off train rumbling through now and then, and a thin buzz from flickering lamps.
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
        ambience.serviceDrone = Loop(sound, "ServiceDrone", AmbienceDir + "/DSGNDron_Noise-Thrusters Inside_HY_SD.wav");

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

        // Water dripping in the dark: faint 3D loops of the cave-drip recording, each a little different in
        // pitch so neighbours never sound alike. Only heard within a few metres. None in the metro tunnels
        // since 2026-11-02 (the director's call; one also carried to the title screen).
        var drips = new GameObject("Drips").transform;
        drips.SetParent(sound);
        Vector3[] dripSpots =
        {
            new Vector3(-45f, -7.5f, 45f), new Vector3(-72f, -7.5f, 44f),                       // Hollis St's service level
            new Vector3(-30f, -7.5f, 345f), new Vector3(-80f, -7.5f, 350f), new Vector3(-20f, -7.5f, 330f),   // Canal St's lower level
        };
        var rng = new System.Random(5);
        for (int i = 0; i < dripSpots.Length; i++)
        {
            var drip = new GameObject("Drip_" + i).AddComponent<AudioSource>();
            drip.transform.SetParent(drips);
            drip.transform.position = dripSpots[i];
            drip.clip = Load<AudioClip>("Assets/Art/Audio/Ambience/Drips/CaveDrips.ogg");
            drip.loop = true; drip.playOnAwake = true; drip.volume = 0.3f;
            drip.pitch = 0.85f + 0.25f * (float)rng.NextDouble();
            drip.spatialBlend = 1f; drip.rolloffMode = AudioRolloffMode.Linear;
            drip.minDistance = 2f; drip.maxDistance = 12f;
        }

        // A train somewhere far off down the line, every 2-4 minutes underground (the line isn't quite dead).
        var trains = sound.gameObject.AddComponent<DistantNoises>();
        trains.player = player;
        trains.clips = new[] { Load<AudioClip>(SfxDir + "/Scares/TrainRumble.wav") };
        trains.minWait = 120f; trains.maxWait = 240f;
        trains.minDistance = 35f; trains.maxDistance = 60f;
        trains.volume = 0.45f;
    }

    // A thin electrical buzz for a flickering light that had none: it cuts out with the light (FlickeringLight).
    static void FlickerBuzz(FlickeringLight flicker, float volume = 0.12f)
    {
        var buzz = flicker.gameObject.AddComponent<AudioSource>();
        buzz.clip = Load<AudioClip>(SfxDir + "/Scares/TransformerBuzz.wav");
        buzz.loop = true; buzz.playOnAwake = true; buzz.volume = volume; buzz.pitch = 1.8f;
        buzz.spatialBlend = 1f; buzz.rolloffMode = AudioRolloffMode.Linear;
        buzz.minDistance = 1f; buzz.maxDistance = 7f;
        flicker.buzz = buzz;
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
