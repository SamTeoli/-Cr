using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C11LateNightWaitingRoomValidation
    {
        [MenuItem("Have a Break/Validate C11 Late Night Waiting Room Effect")]
        private static void ValidateFromMenu()
        {
            CardData c11 = FindCard("C11");
            CardData monsterCard = FindCard("C01");
            bool valid = c11 != null && monsterCard != null && Validate(c11, monsterCard);
            if (!valid) Debug.LogError("C11 Late Night Waiting Room effect validation failed.");
            EditorUtility.DisplayDialog(
                "C11 Late Night Waiting Room Effect Validation",
                valid ? "C11 Late Night Waiting Room effect passed." :
                    "C11 Late Night Waiting Room effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData c11, CardData monsterCard)
        {
            List<BattleCardInstance> deckCards = new();
            for (int i = 0; i < 3; i++)
            {
                deckCards.Add(Instance(monsterCard, 1, $"DECK-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(deckCards, 33);
            BattleCardInstance source = Instance(c11, 5, "SOURCE", CardZone.SkillField);
            BattleMonsterRegistry monsters = new();
            BattleCardInstance ally = Instance(monsterCard, 1, "ALLY", CardZone.MonsterField);
            bool valid = monsters.TryAdd(ally, out BattleMonsterState allyState);
            allyState.ApplyDamage(2);
            valid &= C11LateNightWaitingRoomResolver.TryResolve(
                source, 1, deck, monsters, new BattleEventLog(),
                new BattleEffectResolutionTracker(),
                out int drawn, out string defended);
            return valid && drawn == 2 && defended == ally.Ids.BattleCardId &&
                   deck.Zones.Count(CardZone.Hand) == 2 && allyState.Defense == 1;
        }

        private static BattleCardInstance Instance(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C11-{suffix}", $"BATTLE-C11-{suffix}"),
                level, zone);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null &&
                    string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
