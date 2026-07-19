using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantRoutePinValidation
    {
        [MenuItem("Have a Break/Validate E08 Route Pin Position Target")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C01");
            EnchantData enchant = FindEnchant("E08");
            bool valid = card != null && enchant != null;

            if (valid)
            {
                BattleCardInstance source = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E08", "BATTLE-E08"),
                    1,
                    CardZone.MonsterField);
                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;
                BattleCardEnchantRegistry registry = new();
                valid &= registry.TryRegister(source, runEnchants);

                BattleEnemyPositionState positions = new();
                valid &= positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Center) &&
                         !positions.TryPlace("ENEMY-C", EnemyFieldPosition.Left);

                valid &= EnchantFixedTargetResolver.TryDeclare(
                             source.Ids.BattleCardId,
                             "ENEMY-A",
                             positions,
                             registry,
                             out EnchantFixedTargetDeclaration positionTarget) &&
                         positionTarget.TargetsPosition &&
                         positionTarget.Position == EnemyFieldPosition.Left;

                valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Right) &&
                         positions.TryMove("ENEMY-B", EnemyFieldPosition.Left) &&
                         EnchantFixedTargetResolver.Resolve(positionTarget, positions) == "ENEMY-B";

                valid &= positions.TryMove("ENEMY-B", EnemyFieldPosition.Center) &&
                         EnchantFixedTargetResolver.Resolve(positionTarget, positions) == null;

                valid &= EnchantFixedTargetResolver.TryDeclare(
                             source.Ids.BattleCardId,
                             "ENEMY-A",
                             positions,
                             null,
                             out EnchantFixedTargetDeclaration fixedTarget) &&
                         !fixedTarget.TargetsPosition;
                valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Left) &&
                         EnchantFixedTargetResolver.Resolve(fixedTarget, positions) == "ENEMY-A";

                valid &= !EnchantFixedTargetResolver.TryDeclare(
                    source.Ids.BattleCardId,
                    "ENEMY-NOT-PLACED",
                    positions,
                    registry,
                    out _);
            }
            else
            {
                Debug.LogError("E08 position target validation requires C01 and E08.");
            }

            if (!valid)
            {
                Debug.LogError("E08 Route Pin position target validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E08 Route Pin Position Target Validation",
                    valid
                        ? "E08 Route Pin position target passed."
                        : "E08 Route Pin position target failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string id)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EnchantData>(path))
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
