using UnityEditor;
using UnityEngine;

// Sets import settings automatically whenever a texture or sound lands in Assets/Art.
// - Textures: "point" filtering gives the crunchy, pixel-sharp PS1 look (no blur up close).
// - Long ambience files (drones, music) stream from disk and loop; short effects load into memory.
// Bump the version number if these rules change so Unity re-imports everything.
public class ImportRules : AssetPostprocessor
{
    public override uint GetVersion() => 8;

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
        // Screen UI (menus, inventory frame): drawn at many sizes, so smooth filtering and no mipmaps.
        if (assetPath.Contains("/Art/UI/")) { importer.filterMode = FilterMode.Bilinear; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; }
    }

    // The first-person arms come with animations (grab, push, idle...). "Legacy" animation lets a
    // simple Animation component play a clip by name, e.g. a quick grab when picking something up.
    void OnPreprocessModel()
    {
        var importer = (ModelImporter)assetImporter;
        // The Pale Man and TheWrapper are "Humanoid": Unity maps both skeletons to one standard human body,
        // so TheWrapper's walk, run, idle, scream and attack can play on the Pale Man (who has none).
        if (assetPath.Contains("/Characters/PaleMan/") || assetPath.Contains("/Characters/TheWrapper/"))
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = assetPath.Contains("/TheWrapper/");
            return;
        }
        if (!assetPath.Contains("/Characters/PsxArms/")) return;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
    }

    // TheWrapper's walk, run and idle repeat; the attack, scream and wall slam play once.
    void OnPreprocessAnimation()
    {
        if (!assetPath.Contains("/Characters/TheWrapper/")) return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = clip.name.Contains("Walk") || clip.name.Contains("Run") || clip.name.Contains("Idle");
            clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = true;   // the AI moves the body, not the clip
            clip.keepOriginalOrientation = clip.keepOriginalPositionY = clip.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clips;
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
