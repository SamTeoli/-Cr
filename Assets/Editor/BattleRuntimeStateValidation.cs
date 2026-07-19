using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeStateValidation
    {
        [MenuItem("Have a Break/Validate Battle Runtime State Composition")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Battle Runtime State Validation",
                valid
                    ? "Battle runtime state composition passed."
                    : "Battle runtime state composition failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            bool valid = database != null && database.Cards.Count == 12;
            if (!valid)
            {
                Debug.LogError("Battle runtime validation requires CardDatabase with C01-C12.");
                return false;
            }

            List<BattleCardInstance> cards = database.Cards
                .OrderBy(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase)
                .Select((card, index) => new BattleCardInstance(
                    card,
                    new CardInstanceIds(
                        card.CatalogCardId,
                        $"OWNED-RUNTIME-{index + 1:D2}",
                        $"BATTLE-RUNTIME-{index + 1:D2}"),
                    1,
                    CardZone.DrawPile))
                .ToList();

            BattleRuntimeState runtime = new(cards, 35);
            valid &= runtime.Deck.Zones.Cards.Count == 12;
            valid &= ReferenceEquals(runtime.CardPlay.Deck, runtime.Deck);
            valid &= ReferenceEquals(runtime.CardPlay.Enchants, runtime.Enchants);
            valid &= ReferenceEquals(
                runtime.CardPlay.NextSkillModifiers,
                runtime.NextSkillModifiers);
            valid &= ReferenceEquals(runtime.Turn.CardPlay, runtime.CardPlay);

            valid &= runtime.TryAddEnemy(
                "ENEMY-LEFT", 3, 10, EnemyFieldPosition.Left, out BattleEnemyRuntimeState left);
            valid &= runtime.TryAddEnemy(
                "ENEMY-CENTER", 5, 12, EnemyFieldPosition.Center, out BattleEnemyRuntimeState center);
            valid &= runtime.TryAddEnemy(
                "ENEMY-RIGHT", 2, 8, EnemyFieldPosition.Right, out BattleEnemyRuntimeState right);
            valid &= runtime.LivingEnemies.Count == 3 &&
                     runtime.EnemyStatuses.Enemies.Count == 3 &&
                     runtime.Enemies.Count == 3 &&
                     left.Vital.CurrentHealth == 10 &&
                     center.SnapshotAttack().Attack == 5 &&
                     right.IsAlive;
            valid &= !runtime.TryAddEnemy(
                "ENEMY-DUPLICATE-POSITION", 1, 1,
                EnemyFieldPosition.Left, out _);

            valid &= runtime.Turn.TryBeginBattle(out _);
            valid &= runtime.Turn.TryConfirmStartingHand(
                Array.Empty<string>(), out _, out _, out _);
            valid &= runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                     runtime.CardPlay.Mana.CurrentMana ==
                     runtime.CardPlay.Mana.MaximumMana;

            BattleCardInstance monsterCard = cards.First(card =>
                card.SourceCard.CardType == CardType.Monster);
            valid &= runtime.Deck.Zones.TryMove(
                monsterCard.Ids.BattleCardId, CardZone.MonsterField, out _);
            valid &= runtime.TryRegisterFieldMonster(
                monsterCard.Ids.BattleCardId, out BattleMonsterState monster);
            valid &= monster != null &&
                     runtime.Monsters.Find(monsterCard.Ids.BattleCardId) == monster;

            if (valid)
            {
                Debug.Log("Battle runtime state composition passed.");
            }
            else
            {
                Debug.LogError("Battle runtime state composition failed.");
            }

            return valid;
        }
    }
}
