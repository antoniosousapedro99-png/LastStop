using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The Editor side of the hand-placed dressing scene (see DressingLoader):
// - whenever the level (LookTest) is open, the dressing scene opens beside it (made empty the first time);
// - anything new dropped into the level's scene goes to the dressing scene instead, keeping where it was put,
//   so a rebuild of the level never wipes it;
// - LookTestBuilder calls SaveIfOpen before a rebuild and Reopen after it, and keeps both in Build Settings.
[InitializeOnLoad]
public static class DressingScene
{
    public const string LevelPath = "Assets/Scenes/LookTest.unity";
    public const string Path = "Assets/Scenes/LookTest_Dressing.unity";

    static DressingScene()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => { if (scene.path == LevelPath) WaitToOpen(); };
        ObjectChangeEvents.changesPublished += MoveNewObjects;
        WaitToOpen();   // the level may already be open when Unity starts
    }

    static void WaitToOpen()
    {
        framesWaited = 0;
        EditorApplication.update -= OpenWhenReady;
        EditorApplication.update += OpenWhenReady;
    }

    // Right after Unity starts (or reloads scripts) the open scenes aren't ready for a moment, so this
    // checks each Editor frame, for a few seconds, until the level shows up.
    static int framesWaited;
    static void OpenWhenReady()
    {
        if (SceneManager.GetSceneByPath(LevelPath).isLoaded) OpenBesideLevel();
        else if (++framesWaited < 600) return;
        EditorApplication.update -= OpenWhenReady;
    }

    [MenuItem("Last Stop/Open Hand-Placed Dressing")]
    public static void OpenBesideLevel()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var level = SceneManager.GetSceneByPath(LevelPath);
        if (!level.isLoaded || SceneManager.GetSceneByPath(Path).isLoaded) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Path) == null)
        {
            var made = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(made, Path);
            AddToBuildSettings();
        }
        else EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(level);   // the level's fog and lighting stay the ones shown
    }

    // Saves the dressing scene if it has unsaved changes, so a rebuild (which closes every scene) loses nothing.
    public static void SaveIfOpen()
    {
        var dressing = SceneManager.GetSceneByPath(Path);
        if (dressing.isLoaded && dressing.isDirty) EditorSceneManager.SaveScene(dressing);
    }

    // The level first, then the dressing scene (if it exists) in Build Settings, so the game can load it.
    public static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(LevelPath, true) };
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Path) != null) scenes.Add(new EditorBuildSettingsScene(Path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // A new object that lands in the level's scene (dragged in from the Project window, made from the
    // GameObject menu, pasted) moves to the root of the dressing scene, staying where it was put.
    static void MoveNewObjects(ref ObjectChangeEventStream stream)
    {
        var dressing = SceneManager.GetSceneByPath(Path);
        if (!dressing.isLoaded || EditorApplication.isPlaying) return;
        for (int i = 0; i < stream.length; i++)
        {
            if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy) continue;
            stream.GetCreateGameObjectHierarchyEvent(i, out var e);
            var go = EditorUtility.EntityIdToObject(e.entityId) as GameObject;
            if (go == null || go.scene.path != LevelPath) continue;
            if (go.transform.parent != null) Undo.SetTransformParent(go.transform, null, "Move to dressing");   // keeps its world position
            Undo.MoveGameObjectToScene(go, dressing, "Move to dressing");
            Debug.Log("\"" + go.name + "\" was put in the hand-placed dressing scene (" + Path + "), so level rebuilds keep it.");
        }
    }
}
