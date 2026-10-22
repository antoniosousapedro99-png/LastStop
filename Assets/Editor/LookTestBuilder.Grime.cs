using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Grime laid over the finished level, very sparingly:
// - graffiti: four black, worn spray-paint tags: street, stairwell, and one by the platform vending machine
// - blood: a smear dragged down a tunnel wall, drops by the staff room door, a dried stain under a bench
// (The blood trail on the stairs and the spatter in the train are built with the street and the train.)
// Wall pieces are placed by casting a ray at the wall and sticking a flat picture where it hits.
public static partial class LookTestBuilder
{
    const string GraffitiDir = "Assets/Art/Textures/Graffiti";

    static void BuildGrime()
    {
        var grime = Group("Grime");
        Physics.SyncTransforms();   // the rays must see everything just built
        float eye = StreetY + 1.7f;

        // ---- graffiti (width in metres; height follows the picture)
        // Street: on shop and building walls facing the road, where taggers would spray.
        Spray(grime, "Tag_Drips", 2.0f, new Vector3(-30f, eye, -3f), Vector3.forward);  // apartment block, south side
        // Platform: on the back wall beside the vending machine (the machine stands at x -15.2).
        Spray(grime, "Tag_Peso", 1.2f, new Vector3(-13.3f, 1.4f, 16f), Vector3.back);
        // Stairwell: on the tiled side walls, one high up near the top, one lower near the bottom.
        Spray(grime, "Tag_Goru", 1.3f, new Vector3(0f, StreetY - 0.6f, -7.5f), Vector3.left);
        Spray(grime, "Tag_Scrawl", 1.2f, new Vector3(0f, 2.8f, -2.5f), Vector3.right);

        // ---- blood
        // Something was dragged along the east tunnel wall at hand height.
        var smear = Decal("Blood_WallSmear", grime, "BloodFabric11", Vector3.zero, 0.9f, 0f);
        Stick(smear, new Vector3(55f, -0.4f, 22f), Vector3.back);
        Matte(smear.GetComponent<Renderer>().sharedMaterial);
        // A few drops on the gravel just outside the staff room door.
        Decal("Blood_StaffDoorDrop1", grime, "BloodDrop01", new Vector3(43.3f, -1.296f, 25.0f), 0.22f, 40f);
        Decal("Blood_StaffDoorDrop2", grime, "BloodDrop02", new Vector3(43.8f, -1.296f, 24.6f), 0.16f, 160f);
        // An old dried stain under the east platform bench.
        Decal("Blood_BenchStain", grime, "BloodFabric07", new Vector3(9.2f, 0.006f, 14.9f), 0.8f, 75f);
    }

    // A graffiti tag stuck to the first wall hit by a ray from 'from' along 'direction'.
    static void Spray(Transform parent, string tag, float width, Vector3 from, Vector3 direction)
    {
        var tex = Load<Texture2D>(GraffitiDir + "/" + tag + ".png");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Graffiti_" + tag + ".mat");
        if (mat == null)
        {
            // Black paint, a little see-through so it looks worn into the wall.
            mat = TransparentMat("Graffiti_" + tag, new Color(0.05f, 0.05f, 0.05f, 0.75f), 0.2f);
            mat.SetFloat("_BlendModePreserveSpecular", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        mat.SetTexture("_BaseMap", tex);
        Matte(mat);
        float height = tex ? width * tex.height / tex.width : width * 0.5f;
        var quad = Prim(PrimitiveType.Quad, "Graffiti_" + tag, parent, from, new Vector3(width, height, 1f), mat);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Stick(quad, from, direction);
    }

    // No shine at all: on a wall, highlights and reflections would wash paint and blood out to grey.
    static void Matte(Material mat)
    {
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_SpecularHighlights", 0f);
        mat.SetFloat("_EnvironmentReflections", 0f);
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        EditorUtility.SetDirty(mat);
    }

    // Moves a flat quad onto the first solid surface along the ray, facing back out of it.
    static void Stick(GameObject quad, Vector3 from, Vector3 direction)
    {
        if (!Physics.Raycast(from, direction, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("Grime: no wall found for " + quad.name);
            return;
        }
        quad.transform.position = hit.point + hit.normal * 0.008f;
        quad.transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);   // a quad shows its front along -Z
        Debug.Log("Grime: " + quad.name + " on " + hit.collider.name + " at " + hit.point);
    }
}
