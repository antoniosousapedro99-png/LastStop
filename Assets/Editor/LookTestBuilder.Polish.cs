using UnityEditor;
using UnityEngine;

// Shared pieces for station polish:
// - EndlessTunnel: a tunnel's far end is an invisible wall, and the tunnel runs on another 60 m into the
//   dark beyond it (walls, ceiling, gravel, track and rails, nothing solid), so it seems to go on for ever.
// - FenceLine: a line of rusty steel mesh panels on posts (the URBAN pack's metal fence), used for the
//   barriers at the platform ends and the ticket gate line.
// - FitBetween: shortens a bench so it fits a gap (the train benches between their doorways).
public static partial class LookTestBuilder
{
    const string FenceFbx = StreetDir + "/Urban/metalfence_one_side_topbar.fbx";
    static Material fenceMesh, fenceRust;

    // The gravel beside the tracks: the plain stones from the edge of the track image (ballast_gravel.png,
    // cut from road_rails.jpg), at the same tint and size, so the pit floor and the track bed match.
    static Material PitGravel() => Mat("PitGravel", Tex("ballast_gravel.png"), new Color(0.8f, 0.78f, 0.74f), 2.6f, 0.1f);

    // 'endX' is where the tunnel stops (its old end wall's inner face); 'dir' is +1 if the tunnel runs on
    // toward +x, -1 toward -x. Walls at 'zN' and 'zS' (each 30 cm thick outside them), ceiling at 'ceilY'.
    static void EndlessTunnel(Transform parent, string name, float endX, int dir, float zN, float zS, float ceilY, float bedZ, Material bed)
    {
        const float length = 60f;
        var g = new GameObject(name + "_Beyond").transform;
        g.SetParent(parent);
        float far = endX + dir * length, top = ceilY + 0.2f;
        var wall = Mat("TunnelConcrete", Tex("ConcreteBare.png"), new Color(0.55f, 0.53f, 0.5f), 2f, 0.1f);
        var rail = Mat("RailMetal", Tex("metal_02.png"), new Color(0.55f, 0.52f, 0.5f), 1f, 0.6f, 0.8f);
        Span("WallN", g, new Vector3(endX, -1.5f, zN - 0.3f), new Vector3(far, top, zN), wall, false);
        Span("WallS", g, new Vector3(endX, -1.5f, zS), new Vector3(far, top, zS + 0.3f), wall, false);
        Span("Ceiling", g, new Vector3(endX, ceilY, zN), new Vector3(far, top, zS), wall, false);
        Span("Gravel", g, new Vector3(endX, -1.5f, zN), new Vector3(far, -1.3f, zS), PitGravel(), false);
        Span("TrackBed", g, new Vector3(endX, -1.3f, bedZ), new Vector3(far, -1.29f, bedZ + 2.6f), bed, false);
        foreach (float at in new[] { 0.25f, 0.6875f })
        {
            float rz = bedZ + 2.6f * at;
            Span("Rail", g, new Vector3(endX, -1.29f, rz - 0.035f), new Vector3(far, -1.18f, rz + 0.035f), rail, false);
        }
        Span("FarEnd", g, new Vector3(far, -1.5f, zN - 0.3f), new Vector3(far + dir * 0.3f, top, zS + 0.3f), wall, false);   // lost in the fog
        // The real end: solid, but nothing to see.
        var stop = Span(name + "_InvisibleEnd", parent, new Vector3(endX, -1.5f, zN - 0.3f), new Vector3(endX + dir * 0.3f, top, zS + 0.3f), wall);
        Object.DestroyImmediate(stop.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(stop.GetComponent<MeshFilter>());
    }

    // Mesh panels on posts from 'a' to 'b' (on the floor), 'height' tall, each panel at most 2.5 m wide, with
    // a post at both ends. One box collider covers the whole line.
    static Transform FenceLine(Transform parent, string name, Vector3 a, Vector3 b, float height = 1.1f)
    {
        if (fenceMesh == null)
        {
            // A copy of the pack's mesh image without shrunk copies for distance (mipmaps), which would
            // average its thin wires away to nothing, and with crisp PS1-style pixels.
            const string meshTex = "Assets/Art/Textures/Generated/FenceMesh.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(meshTex);
            if (importer.mipmapEnabled || importer.filterMode != FilterMode.Point)
            {
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            fenceMesh = Mat("FenceMeshRusty", Load<Texture2D>(meshTex), new Color(0.42f, 0.36f, 0.32f), 1f, 0.25f, 0.5f);
            fenceMesh.SetTextureScale("_BaseMap", Vector2.one * 0.4f);   // bigger cells (about 8 cm), so the mesh still reads from a distance
            fenceMesh.SetFloat("_AlphaClip", 1f);
            fenceMesh.SetFloat("_Cutoff", 0.5f);
            fenceMesh.EnableKeyword("_ALPHATEST_ON");
            fenceMesh.SetFloat("_Cull", 0f);   // the mesh is one sheet, seen from both sides
            fenceMesh.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            fenceRust = Mat("FencePostRusty", Load<Texture2D>(StreetDir + "/Urban/TexturesCom_Metal_PipeRusted2_1_82x0_47_512_albedo.png"), new Color(0.55f, 0.45f, 0.4f), 1f, 0.2f, 0.4f);
        }
        var run = new GameObject(name).transform;
        run.SetParent(parent);
        Vector3 along = b - a; along.y = 0f;
        float length = along.magnitude;
        var turn = Quaternion.FromToRotation(Vector3.right, along / length);
        int panels = Mathf.Max(1, Mathf.CeilToInt(length / 2.5f));
        for (int i = 0; i < panels; i++)
        {
            // Each panel sits in a holder turned along the line; the model is fitted inside it, its width
            // along the holder's x.
            var holder = new GameObject("Panel").transform;
            holder.SetParent(run);
            var panel = (GameObject)Object.Instantiate(Load<GameObject>(FenceFbx), holder);
            panel.name = "MeshPanel";
            foreach (var r in panel.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = mats[m] != null && mats[m].name.Contains("pipe") ? fenceRust : fenceMesh;
                r.sharedMaterials = mats;
            }
            var s = Bounds(panel).size;
            holder.localScale = new Vector3(length / panels / s.x, height / s.y, 1f);
            var bb = Bounds(panel);
            panel.transform.position -= new Vector3(bb.min.x, bb.min.y, bb.center.z);   // its corner on the holder's origin
            holder.SetPositionAndRotation(a + along * i / panels, turn);
        }
        foreach (var end in new[] { a, b })   // a post at each end of the line
        {
            var post = Prim(PrimitiveType.Cylinder, "Post", run, end + Vector3.up * height * 0.5f, new Vector3(0.05f, height * 0.5f, 0.05f), fenceRust);
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }
        var block = run.gameObject.AddComponent<BoxCollider>();   // the run's own transform is at the origin, unturned
        var all = Bounds(run.gameObject);
        block.center = all.center;
        block.size = new Vector3(Mathf.Max(all.size.x, 0.15f), all.size.y, Mathf.Max(all.size.z, 0.15f));   // at least 15 cm thick
        return run;
    }

    // Shortens 'go' along the world x axis until it is at most 'length' long (it keeps its height and depth).
    static void FitAlongX(GameObject go, float length)
    {
        float now = Bounds(go).size.x;
        if (now <= length) return;
        var local = go.transform.InverseTransformDirection(Vector3.right);
        var scale = go.transform.localScale;
        float ax = Mathf.Abs(local.x), ay = Mathf.Abs(local.y), az = Mathf.Abs(local.z);
        if (ax >= ay && ax >= az) scale.x *= length / now;
        else if (ay >= az) scale.y *= length / now;
        else scale.z *= length / now;
        go.transform.localScale = scale;
    }
}
