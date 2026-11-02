using UnityEngine;

// Hands a sign's TextMesh font texture to its painted-text material (LastStop/PaintedText), here and whenever
// the font rebuilds its letters (it's a dynamic font). Done per sign (MaterialPropertyBlock), so the shared
// material asset never changes. Also runs in the Editor, so signs read correctly in the Scene view.
[ExecuteAlways]
[RequireComponent(typeof(TextMesh))]
public class PaintedText : MonoBehaviour
{
    MeshRenderer view;
    TextMesh text;
    MaterialPropertyBlock block;

    void OnEnable()
    {
        view = GetComponent<MeshRenderer>();
        text = GetComponent<TextMesh>();
        Font.textureRebuilt += Rebuilt;
        Apply();
    }

    void OnDisable() => Font.textureRebuilt -= Rebuilt;

    void Rebuilt(Font font) { if (text && font == text.font) Apply(); }

    void Apply()
    {
        if (!view || !text || !text.font) return;
        block ??= new MaterialPropertyBlock();
        view.GetPropertyBlock(block);
        block.SetTexture("_MainTex", text.font.material.mainTexture);
        view.SetPropertyBlock(block);
    }
}
