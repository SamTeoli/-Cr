using UnityEditor;
using UnityEngine;

public static class CardVisualQualitySetup
{
    private const string CardFrameFolder = "Assets/Art/CardFrames";

    [MenuItem("Have A Break/Cards/Apply High Quality Card Visuals")]
    public static void Apply()
    {
        string[] textureGuids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { CardFrameFolder });
        int updated = 0;
        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 2048;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 100;
            importer.crunchedCompression = false;

            TextureImporterPlatformSettings standalone =
                importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = 2048;
            standalone.textureCompression =
                TextureImporterCompression.Uncompressed;
            standalone.compressionQuality = 100;
            importer.SetPlatformTextureSettings(standalone);

            importer.SaveAndReimport();
            updated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"High quality card visuals applied to {updated} frame textures.");
    }

    public static void ApplyFromCommandLine()
    {
        Apply();
    }
}
