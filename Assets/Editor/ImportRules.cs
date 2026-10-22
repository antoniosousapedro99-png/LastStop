using UnityEditor;
using UnityEngine;

// Sets import settings automatically whenever a texture or sound lands in Assets/Art.
// - Textures: "point" filtering gives the crunchy, pixel-sharp PS1 look (no blur up close).
// - Long ambience files (drones, music) stream from disk and loop; short effects load into memory.
// Bump the version number if these rules change so Unity re-imports everything.
public class ImportRules : AssetPostprocessor
{
    public override uint GetVersion() => 5;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.anisoLevel = 0;
        importer.mipmapEnabled = true; // keeps far-away tiles from shimmering
        // The blood decals come as huge photos (up to 4K); 512 px is plenty for a PS1-style game.
        if (assetPath.Contains("/Textures/Blood/")) { importer.maxTextureSize = 512; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; }
        // Graffiti tags: the see-through background is stored as white, which would bleed into the black
        // letters when the picture is shrunk for distance; this setting fills it with the letters' colour.
        if (assetPath.Contains("/Textures/Graffiti/")) { importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; }
    }

    // The first-person arms come with animations (grab, push, idle...). "Legacy" animation lets a
    // simple Animation component play a clip by name, e.g. a quick grab when picking something up.
    void OnPreprocessModel()
    {
        if (!assetPath.Contains("/Characters/PsxArms/") && !assetPath.Contains("/Characters/TheWrapper/")) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Art/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool isLong = assetPath.Contains("/Ambience/") || assetPath.Contains("/Music/");

        var settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = isLong ? 0.6f : 0.8f;
        settings.loadType = isLong ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = 48000; // the drones come at 96 kHz, which is wasted space
        importer.defaultSampleSettings = settings;

        // Short effects play as 3D sounds in the world, which only works properly in mono.
        importer.forceToMono = !isLong;
    }
}
