using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C02LanternBearerValidation
    {
        [MenuItem("Have a Break/Validate C02 Lantern Bearer Summon Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData c02 = FindCard("C02");
            CardData skill = FindCard("C05");
            bool valid = c02 != null && skill != null;
            if (valid)
            {
                valid &= ValidateLevelOne(c02, skill);
                valid &= ValidateTurnExpiry(c02);
                valid &= ValidateLevelFour(c02);
                valid &= ValidateLevelFive(c02, skill);
            }
            else
            {
                Debug.LogError("C02 validation requires C02 and C05.");
            }

            if (!valid)
            {
                Debug.LogError("C02 Lantern Bearer summon effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "C02 Lantern Bearer Summon Effect Validation",
                    valid
                        ? "C02 Lantern Bearer summon effect passed."
                        : "C02 Lantern Bearer summon effect failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateLevelOne(CardData c02, CardData skillData)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 1, "L1", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                         result.CostReduction == 1 && result.FirstNumericEffectBonus == 0 &&
                         result.DefenseGained == 0 && modifiers.PendingCount == 1;

            BattleCardInstance skill = CreateCard(skillData, 1, "SKILL-L1", CardZone.DrawPile);
            BattleDeckState deck = new(new[] { skill }, 29);
            valid &= deck.Zones.TryMove(skill.Ids.BattleCardId, CardZone.Hand, out _);
            BattleCardPlayState play = new(deck, 5, null, modifiers);
            play.Mana.StartPlayerTurn();
            int expectedCost = Mathf.Max(0, skill.Resolved.ManaCost - 1);
            valid &= play.TryPreviewPlay(skill.Ids.BattleCardId, out CardPlayPreview preview, out _) &&
                     preview.ManaCost == expectedCost && modifiers.PendingCount == 1;
            valid &= play.TryConfirmPlay(preview, out _) && modifiers.PendingCount == 0 &&
                     play.Mana.CurrentMana == play.Mana.MaximumMana - expectedCost;
            return valid;
        }

        private static bool ValidateTurnExpiry(CardData c02)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 1, "EXPIRY", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out _) && modifiers.PendingCount == 1;
            modifiers.EndPlayerTurn();
            return valid && modifiers.PendingCount == 0;
        }

        private static bool ValidateLevelFour(CardData c02)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 4, "L4", out BattleCardInstance source);
            return ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                   result.DefenseGained == 1 && monster.Defense == 1;
        }

        private static bool ValidateLevelFive(CardData c02, CardData skillData)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 5, "L5", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                         result.FirstNumericEffectBonus == 1 && result.DefenseGained == 1;

            BattleCardInstance skill = CreateCard(skillData, 1, "SKILL-L5", CardZone.DrawPile);
            BattleDeckState deck = new(new[] { skill }, 30);
            valid &= deck.Zones.TryMove(skill.Ids.BattleCardId, CardZone.Hand, out _);
            BattleCardPlayState play = new(deck, 5, null, modifiers);
            play.Mana.StartPlayerTurn();
            valid &= play.TryPreviewPlay(skill.Ids.BattleCardId, out CardPlayPreview preview, out _) &&
                     play.TryConfirmPlay(preview, out _);
            valid &= modifiers.TryTakeFirstNumericEffectBonus(skill.Ids.BattleCardId, out int bonus) &&
                     bonus == 1;
            valid &= !modifiers.TryTakeFirstNumericEffectBonus(skill.Ids.BattleCardId, out _);
            return valid;
        }

        private static bool ResolveSummon(
            BattleMonsterState monster,
            BattleCardInstance source,
            BattleNextSkillModifierState modifiers,
            out C02LanternBearerResult result)
        {
            BattleEventLog log = new();
            BattleEventRecord summoned = log.Record(
                BattleEventType.MonsterSummoned, "C02ValidationSummon",
                source.Ids.BattleCardId, source.Ids.BattleCardId, source.Ids.BattleCardId);
            BattleEffectResolutionTracker tracker = new();
            bool resolved = C02LanternBearerResolver.TryResolve(
                summoned, monster, modifiers, log, tracker, out result);
            return resolved && !C02LanternBearerResolver.TryResolve(
                summoned, monster, modifiers, log, tracker, out _);
        }

        private static BattleMonsterState CreateMonster(
            CardData card, int level, string suffix, out BattleCardInstance battleCard)
        {
            battleCard = CreateCard(card, level, suffix, CardZone.MonsterField);
            return new BattleMonsterState(battleCard);
        }

        private static BattleCardInstance CreateCard(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-{suffix}", $"BATTLE-{suffix}"),
                level,
                zone);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
