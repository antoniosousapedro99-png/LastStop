using UnityEditor;
using UnityEngine;

// Menu shortcuts that point the Scene view at each part of the level (they're far apart: Hollis St at the
// start, Canal St 300 m on, the sewers 600 m on and 20 m down), selecting it in the Hierarchy too.
public static class GoToArea
{
    [MenuItem("Last Stop/Go To/Hollis St")] static void Hollis() => Go("Level/Platform", new Vector3(0f, 2f, 18f), 40f);
    [MenuItem("Last Stop/Go To/Canal St")] static void Canal() => Go("Level/CanalSt", new Vector3(0f, 2f, 310f), 80f);
    [MenuItem("Last Stop/Go To/The Sewers")] static void Sewers() => Go("Level/Sewers", new Vector3(45f, -20f, 670f), 110f);

    static void Go(string path, Vector3 centre, float size)
    {
        var target = GameObject.Find(path);
        if (target) Selection.activeGameObject = target;
        var view = SceneView.lastActiveSceneView;
        if (view == null) { EditorApplication.ExecuteMenuItem("Window/General/Scene"); view = SceneView.lastActiveSceneView; }
        if (view == null) return;
        view.LookAt(centre, Quaternion.Euler(55f, 0f, 0f), size);   // from above at an angle, the whole area in view
        view.Focus();
    }
}
