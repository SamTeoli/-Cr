using System;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    public static class CardEffectTextAssetMigration
    {
        private const string OutputFolder =
            "Assets/GameData/CardEffectTexts";

        [MenuItem("Have A Break/Cards/Migrate Authored Effect Text")]
        public static void Migrate()
        {
            EnsureFolder("Assets/GameData", "CardEffectTexts");

            string[] guids = AssetDatabase.FindAssets(
                "t:CardData",
                new[] { "Assets/GameData/Cards" });
            int migrated = 0;
            foreach (string guid in guids)
            {
                string cardPath = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
                if (card == null ||
                    string.IsNullOrWhiteSpace(card.CatalogCardId))
                {
                    continue;
                }

                string textPath =
                    $"{OutputFolder}/{card.CatalogCardId}.asset";
                CardEffectTextAsset textAsset =
                    AssetDatabase.LoadAssetAtPath<CardEffectTextAsset>(
                        textPath);
                if (textAsset == null)
                {
                    textAsset =
                        ScriptableObject.CreateInstance<CardEffectTextAsset>();
                    textAsset.name = card.CatalogCardId;
                    AssetDatabase.CreateAsset(textAsset, textPath);
                }

                CopyAuthoredText(card, textAsset);
                AssignTextAsset(card, textAsset);
                migrated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Migrated {migrated} card effect text assets to " +
                $"{OutputFolder}.");
        }

        public static void MigrateFromCommandLine()
        {
            try
            {
                Migrate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CopyAuthoredText(
            CardData card,
            CardEffectTextAsset textAsset)
        {
            SerializedObject textObject = new(textAsset);
            textObject.FindProperty("catalogCardId").stringValue =
                card.CatalogCardId;

            SerializedProperty localizations =
                textObject.FindProperty("localizations");
            localizations.arraySize = 1;
            SerializedProperty korean =
                localizations.GetArrayElementAtIndex(0);
            korean.FindPropertyRelative("locale").enumValueIndex =
                (int)CardTextLocale.Korean;
            korean.FindPropertyRelative("rulesText").stringValue =
                card.RulesText ?? string.Empty;
            korean.FindPropertyRelative("detailedRulesText").stringValue =
                card.DetailedRulesText ?? string.Empty;

            SerializedProperty levelTexts =
                korean.FindPropertyRelative("levels");
            levelTexts.arraySize = card.Levels.Count;
            for (int i = 0; i < card.Levels.Count; i++)
            {
                CardLevelData source = card.Levels[i];
                SerializedProperty destination =
                    levelTexts.GetArrayElementAtIndex(i);
                destination.FindPropertyRelative("level").intValue =
                    source?.Level ?? i + CardData.MinimumLevel;
                destination.FindPropertyRelative("rulesText").stringValue =
                    source?.RulesText ?? string.Empty;
            }

            textObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(textAsset);
        }

        private static void AssignTextAsset(
            CardData card,
            CardEffectTextAsset textAsset)
        {
            SerializedObject cardObject = new(card);
            cardObject.FindProperty("effectTextAsset").objectReferenceValue =
                textAsset;
            cardObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
