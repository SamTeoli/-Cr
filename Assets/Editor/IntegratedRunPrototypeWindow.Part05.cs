using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    public sealed partial class IntegratedRunPrototypeWindow : EditorWindow
    {
        private void SaveRun(string successMessage)
        {
            RunSaveCommandResult result = runLifecycle.Save(
                campaign,
                progress,
                prototypeConfig,
                successMessage);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                message = result.Message;
            }
        }

        private void LoadDatabases()
        {
            cardDatabase =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(CardDatabasePath);
            enchantDatabase =
                AssetDatabase.LoadAssetAtPath<EnchantDatabase>(EnchantDatabasePath);
            encounterDatabase =
                AssetDatabase.LoadAssetAtPath<EncounterDatabase>(EncounterDatabasePath);
            prototypeConfig = Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig");
            if (!DatabasesReady())
            {
                message = "Card/Enchant/Encounter 데이터베이스를 확인하세요.";
            }
        }

        private void LoadPermanentRewards()
        {
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
        }

        private bool DatabasesReady()
        {
            return cardDatabase != null && enchantDatabase != null &&
                   encounterDatabase != null && prototypeConfig != null &&
                   prototypeConfig.IsReady;
        }

        private void DrawMessage()
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        private static IEnumerable<T> Rotate<T>(
            IReadOnlyList<T> values,
            int seed)
        {
            if (values == null || values.Count == 0)
            {
                yield break;
            }

            int start = Mathf.Abs(seed % values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                yield return values[(start + i) % values.Count];
            }
        }
    }
}
