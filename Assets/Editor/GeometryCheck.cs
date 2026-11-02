using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Finds "z-fighting": two wall/floor boxes whose surfaces sit in exactly the same spot and face the
// same way. The graphics card can't tell which one is in front, so the two textures flicker and
// tear in jagged stripes as the camera moves. Every scene builder runs this check after building;
// it can also be run by hand from the menu: Last Stop > Check Geometry.
// Rule for building: where two pieces meet, one must END where the other BEGINS (a butt joint).
// Never let two pieces overlap along a visible face.
public static class GeometryCheck
{
    [MenuItem("Last Stop/Check Geometry")]
    public static void CheckOpenScene()
    {
        var level = GameObject.Find("Level");
        if (level == null) { Debug.LogWarning("No 'Level' object in this scene."); return; }
        var problems = Find(level.transform);
        if (problems.Count == 0) Debug.Log("Geometry check: no overlapping surfaces found.");
    }

    // Returns one line per problem and logs each as a warning.
    public static List<string> Find(Transform root)
    {
        var boxes = new List<Renderer>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            if (r.transform.rotation == Quaternion.identity && r.GetComponent<MeshFilter>()
                && r.GetComponent<MeshFilter>().sharedMesh && IsBox(r.GetComponent<MeshFilter>().sharedMesh))
                boxes.Add(r); // only our axis-aligned boxes (24 corners = 6 faces x 4)

        var problems = new List<string>();
        const float samePlane = 0.002f, minOverlap = 0.01f;
        for (int i = 0; i < boxes.Count; i++)
        for (int j = i + 1; j < boxes.Count; j++)
        {
            Bounds a = boxes[i].bounds, b = boxes[j].bounds;
            for (int axis = 0; axis < 3; axis++)
            for (int side = 0; side < 2; side++) // 0 = face pointing negative, 1 = positive
            {
                float pa = side == 0 ? a.min[axis] : a.max[axis];
                float pb = side == 0 ? b.min[axis] : b.max[axis];
                if (Mathf.Abs(pa - pb) > samePlane) continue;
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                float ou = Mathf.Min(a.max[u], b.max[u]) - Mathf.Max(a.min[u], b.min[u]);
                float ov = Mathf.Min(a.max[v], b.max[v]) - Mathf.Max(a.min[v], b.min[v]);
                if (ou < minOverlap || ov < minOverlap) continue;
                string msg = $"Z-fighting: {Path(boxes[i].transform)} and {Path(boxes[j].transform)} share a " +
                             $"{(side == 0 ? "-" : "+")}{"XYZ"[axis]} face at {pa:F2} ({ou * ov:F2} m²)";
                problems.Add(msg);
                Debug.LogWarning(msg, boxes[i]);
            }
        }
        return problems;
    }

    static string Path(Transform t) => t.parent ? t.parent.name + "/" + t.name : t.name;

    // 24 corners, every one on a corner of the mesh's bounds: a real upright box. (Other 24-corner shapes, like
    // the cistern's wedge-shaped wall pieces, would be judged by their bounds and flagged wrongly.)
    static bool IsBox(Mesh mesh)
    {
        if (mesh.vertexCount != 24) return false;
        if (!mesh.isReadable) return true;
        Vector3 lo = mesh.bounds.min, hi = mesh.bounds.max;
        foreach (var v in mesh.vertices)
            for (int axis = 0; axis < 3; axis++)
                if (Mathf.Abs(v[axis] - lo[axis]) > 0.0005f && Mathf.Abs(v[axis] - hi[axis]) > 0.0005f) return false;
        return true;
    }
}
