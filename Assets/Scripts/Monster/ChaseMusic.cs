using UnityEngine;

// Music only while the Pale Man is chasing: it fades in when the chase starts and fades out a few
// seconds after it ends. Follows the Music volume setting (and ignores the effects volume).
public class ChaseMusic : MonoBehaviour
{
    public PaleManAI monster;
    public AudioSource music;      // its own speaker (the same object also holds the first-sighting sting)
    public float volume = 0.7f;
    public float fadeIn = 1.5f, fadeOut = 4f;

    float level;

    void Awake()
    {
        music.loop = true; music.playOnAwake = false; music.spatialBlend = 0f; music.ignoreListenerVolume = true; music.volume = 0f;
    }

    void Update()
    {
        bool chasing = monster && monster.isActiveAndEnabled && monster.Current == PaleManAI.Mood.Chase;
        level = Mathf.MoveTowards(level, chasing ? 1f : 0f, Time.deltaTime / (chasing ? fadeIn : fadeOut));
        if (level > 0f && !music.isPlaying) music.Play();
        if (level <= 0f && music.isPlaying) music.Stop();
        music.volume = level * volume * GameSettings.MusicLevel;
    }
}
