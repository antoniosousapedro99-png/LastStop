using UnityEngine;
using UnityEngine.SceneManagement;

// Loads the hand-placed dressing scene (LookTest_Dressing: props placed by hand in the Editor) on top of the
// level whenever the level loads: at the start, after loading a save, after a restart. The level itself is
// rebuilt from scratch by LookTestBuilder, so hand-placed things live in their own scene that rebuilds never
// touch. The level stays the "active" scene, because fog, ambient light and restarts all go by that one.
public static class DressingLoader
{
    public const string LevelScene = "LookTest";
    public const string DressingScene = "LookTest_Dressing";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;   // Play runs skip the domain reload, so never hook twice
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // The very first scene can finish loading before the hook above is listening, so check it once here too.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void FirstScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var level = SceneManager.GetSceneByName(LevelScene);
        if (!level.IsValid()) return;
        if (scene == level && !SceneManager.GetSceneByName(DressingScene).IsValid()   // not open yet (in the Editor both may already be)
            && Application.CanStreamedLevelBeLoaded(DressingScene))
            SceneManager.LoadScene(DressingScene, LoadSceneMode.Additive);
        if (level.isLoaded) SceneManager.SetActiveScene(level);
    }
}
