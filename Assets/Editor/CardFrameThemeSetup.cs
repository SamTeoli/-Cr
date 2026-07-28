using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

public static class CardFrameThemeSetup
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = ResourcesFolder + "/UI";
    private const string AssetPath = UiFolder + "/CardFrameTheme.asset";
    private const string LayoutAssetPath =
        UiFolder + "/CardLayoutSettings.asset";
    private const string FrameFolder = "Assets/Art/CardFrames/";

    private static readonly string[] FramePaths =
    {
        FrameFolder + "frame_common.png",
        FrameFolder + "frame_rare.png",
        FrameFolder + "frame_legendary.png",
        FrameFolder + "frame_nonmonster_common.png",
        FrameFolder + "frame_nonmonster_rare.png",
        FrameFolder + "frame_nonmonster_legendary.png"
    };

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticSetup()
    {
        EditorApplication.delayCall += () => EnsureConfigured(false);
    }

    [MenuItem("Have a Break/UI/Apply All Card Frames")]
    public static void ApplyAllCardFrames()
    {
        EnsureConfigured(true);
    }

    private static void EnsureConfigured(bool selectAsset)
    {
        EnsureFolder(ResourcesFolder, "Resources");
        EnsureFolder(UiFolder, "UI");
        EnsureLayoutSettings();

        for (int index = 0; index < FramePaths.Length; index++)
        {
            ConfigureSpriteImporter(FramePaths[index]);
        }

        CardFrameTheme theme =
            AssetDatabase.LoadAssetAtPath<CardFrameTheme>(AssetPath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<CardFrameTheme>();
            theme.EditorInitializeDefaults();
            AssetDatabase.CreateAsset(theme, AssetPath);
        }

        theme.EditorAssignFrames(
            LoadFrame(0),
            LoadFrame(1),
            LoadFrame(2),
            LoadFrame(3),
            LoadFrame(4),
            LoadFrame(5));
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();

        if (selectAsset)
        {
            Selection.activeObject = theme;
            EditorGUIUtility.PingObject(theme);
        }

        Debug.Log("All six card frames are configured and applied.");
    }

    private static void EnsureLayoutSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<CardLayoutSettings>(
                LayoutAssetPath) != null)
        {
            return;
        }

        CardLayoutSettings layout =
            ScriptableObject.CreateInstance<CardLayoutSettings>();
        AssetDatabase.CreateAsset(layout, LayoutAssetPath);
    }

    private static Sprite LoadFrame(int index)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            FramePaths[index]);
        if (sprite == null)
        {
            Debug.LogError($"Card frame could not be loaded: {FramePaths[index]}");
        }

        return sprite;
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
        {
            Debug.LogError($"Card frame importer is missing: {assetPath}");
            return;
        }

        bool changed =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            !importer.alphaIsTransparency ||
            importer.mipmapEnabled;
        if (!changed)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 100f;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string fullPath, string folderName)
    {
        if (AssetDatabase.IsValidFolder(fullPath))
        {
            return;
        }

        string parent = fullPath.Substring(
            0, fullPath.Length - folderName.Length - 1);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
