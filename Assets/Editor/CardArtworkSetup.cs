using UnityEditor;
using UnityEngine;

public static class CardArtworkSetup
{
    private readonly struct ArtworkEntry
    {
        public ArtworkEntry(string artworkFile, string cardFile)
        {
            ArtworkPath = $"Assets/Art/CardArtwork/{artworkFile}";
            CardPath = $"Assets/GameData/Cards/{cardFile}";
        }

        public string ArtworkPath { get; }
        public string CardPath { get; }
    }

    private static readonly ArtworkEntry[] Entries =
    {
        new("C01_LastTrainSleepingBagKeeper.png", "C01_막차의 침낭지기.asset"),
        new("C02_LanternPorter.png", "C02_등불 짐꾼.asset"),
        new("C03_SeatRepairer.png", "C03_좌석 수리공.asset"),
        new("C04_TerminalStrayCat.png", "C04_종점 길고양이.asset"),
        new("C05_PlatformShove.png", "C05_승강장 밀어내기.asset"),
        new("C06_EmergencyBrake.png", "C06_비상 제동.asset"),
        new("C07_FindLostTicket.png", "C07_잃어버린 표 찾기.asset"),
        new("C08_ClosingDoors.png", "C08_닫히는 출입문.asset"),
        new("C09_InspectionEvasionBlanket.png", "C09_검표 회피용 담요.asset"),
        new("C10_SeveredCallLine.png", "C10_끊어진 호출선.asset"),
        new("C11_MidnightWaitingHall.png", "C11_심야의 대합실.asset"),
        new("C12_StarlightRouteMap.png", "C12_노선도 위의 별빛.asset")
    };

    [InitializeOnLoadMethod]
    private static void ScheduleAssignment()
    {
        EditorApplication.delayCall += AssignAllArtwork;
    }

    [MenuItem("Have a Break/UI/Apply All Card Artwork")]
    public static void AssignAllArtwork()
    {
        int applied = 0;
        foreach (ArtworkEntry entry in Entries)
        {
            AssetDatabase.ImportAsset(
                entry.ArtworkPath,
                ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(entry.ArtworkPath) is
                TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(
                entry.ArtworkPath);
            Object card = AssetDatabase.LoadMainAssetAtPath(entry.CardPath);
            if (artwork == null || card == null)
            {
                Debug.LogError(
                    $"Card artwork assignment failed: {entry.CardPath}");
                continue;
            }

            SerializedObject serializedCard = new(card);
            SerializedProperty property = serializedCard.FindProperty(
                "artwork");
            if (property == null)
            {
                Debug.LogError(
                    $"Artwork property was not found: {entry.CardPath}");
                continue;
            }

            property.objectReferenceValue = artwork;
            serializedCard.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);
            applied++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Applied otaku-style artwork to {applied}/12 cards.");
    }
}
