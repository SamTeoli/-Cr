using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeCardPlayServiceValidation
    {
        [MenuItem("Have a Break/Validate Battle Runtime Card Play Events")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Battle Runtime Card Play Events Validation",
                valid
                    ? "Battle runtime card play events passed."
                    : "Battle runtime card play events failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            if (database == null || database.Cards.Count != 12)
            {
                Debug.LogError("Runtime card play validation requires C01-C12.");
                return false;
            }

            List<BattleCardInstance> cards = database.Cards
                .OrderBy(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase)
                .Select((card, index) => new BattleCardInstance(
                    card,
                    new CardInstanceIds(
                        card.CatalogCardId,
                        $"OWNED-PLAY-{index + 1:D2}",
                        $"BATTLE-PLAY-{index + 1:D2}"),
                    1,
                    CardZone.DrawPile))
                .ToList();
            BattleRuntimeState runtime = new(cards, 351);
            bool valid = runtime.Turn.TryBeginBattle(out _) &&
                         runtime.Turn.TryConfirmStartingHand(
                             Array.Empty<string>(), out _, out _, out _);

            foreach (BattleCardInstance handCard in
                     runtime.Deck.Zones.GetCards(CardZone.Hand))
            {
                valid &= runtime.Deck.Zones.TryMove(
                    handCard.Ids.BattleCardId, CardZone.Graveyard, out _);
            }

            BattleCardInstance c01 = cards.First(card =>
                card.SourceCard.CatalogCardId == TestContentIds.C01);
            BattleCardInstance c05 = cards.First(card =>
                card.SourceCard.CatalogCardId == TestContentIds.C05);
            valid &= runtime.Deck.Zones.TryMove(
                c01.Ids.BattleCardId, CardZone.Hand, out _);
            valid &= runtime.Deck.Zones.TryMove(
                c05.Ids.BattleCardId, CardZone.Hand, out _);

            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime, c01.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult monsterResult,
                out BattleRuntimeCardPlayFailure monsterFailure,
                out CardPlayFailure monsterCardFailure);
            valid &= monsterFailure == BattleRuntimeCardPlayFailure.None &&
                     monsterCardFailure == CardPlayFailure.None &&
                     monsterResult.Card == c01 &&
                     monsterResult.MonsterWasSummoned &&
                     monsterResult.PlayedEvent.EventType == BattleEventType.CardPlayed &&
                     monsterResult.SummonedEvent.EventType == BattleEventType.MonsterSummoned &&
                     monsterResult.SummonedEvent.ParentEventId ==
                     monsterResult.PlayedEvent.EventId &&
                     runtime.Monsters.Find(c01.Ids.BattleCardId) ==
                     monsterResult.SummonedMonster &&
                     c01.Zone == CardZone.MonsterField &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;

            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime, c05.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult skillResult,
                out BattleRuntimeCardPlayFailure skillFailure,
                out CardPlayFailure skillCardFailure);
            valid &= skillFailure == BattleRuntimeCardPlayFailure.None &&
                     skillCardFailure == CardPlayFailure.None &&
                     !skillResult.MonsterWasSummoned &&
                     skillResult.PlayedEvent.EventType == BattleEventType.CardPlayed &&
                     c05.Zone == CardZone.Graveyard &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;

            int playedCount = runtime.EventLog.Events.Count(item =>
                item.EventType == BattleEventType.CardPlayed);
            int summonedCount = runtime.EventLog.Events.Count(item =>
                item.EventType == BattleEventType.MonsterSummoned);
            valid &= playedCount == 2 && summonedCount == 1;

            if (valid)
            {
                Debug.Log("Battle runtime card play events passed.");
            }
            else
            {
                Debug.LogError("Battle runtime card play events failed.");
            }

            return valid;
        }
    }

    internal static class BattleRuntimePlayerAttackValidation
    {
        [MenuItem("Have a Break/Validate Player Monster Attack")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Player Monster Attack Validation",
                valid
                    ? "Player monster attack flow passed."
                    : "Player monster attack flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c03 = FindCard(TestContentIds.C03);
            CardData c04 = FindCard(TestContentIds.C04);
            bool valid = c03 != null && c04 != null &&
                         ValidateFirstTurnVictoryAndVulnerable(c04) &&
                         ValidateAttackLimitAndNextTurn(c04) &&
                         ValidateC03AttackEvent(c03);

            if (valid)
            {
                Debug.Log("Player monster attack flow passed.");
            }
            else
            {
                Debug.LogError("Player monster attack flow failed.");
            }

            return valid;
        }

        private static bool ValidateFirstTurnVictoryAndVulnerable(
            CardData card)
        {
            BattleRuntimeState runtime = Start(
                card,
                "ATTACK-VICTORY",
                3,
                new[]
                {
                    new EnemySetup(
                        "ENEMY-ATTACK-VICTORY",
                        3,
                        EnemyFieldPosition.Center)
                },
                out BattleMonsterState attacker);
            BattleEnemyStatusState status = runtime?.EnemyStatuses.Find(
                "ENEMY-ATTACK-VICTORY");
            if (runtime == null || attacker == null || status == null ||
                status.ApplyVulnerable(2) != 2)
            {
                return false;
            }

            bool resolved = BattleRuntimePlayerAttackService.TryResolve(
                runtime,
                attacker.BattleCardId,
                "ENEMY-ATTACK-VICTORY",
                out BattleRuntimePlayerAttackResult result,
                out BattleRuntimePlayerAttackFailure failure);
            return resolved &&
                   failure == BattleRuntimePlayerAttackFailure.None &&
                   result != null &&
                   result.Attacker == attacker &&
                   result.BaseAttack == 1 &&
                   result.VulnerableBonus == 2 &&
                   result.FinalDamage == 3 &&
                   result.DamageApplied == 3 &&
                   result.TargetDefeated &&
                   status.Vulnerable == 0 &&
                   result.DeclaredAttack.EventType ==
                   BattleEventType.AttackDeclared &&
                   result.VulnerableConsumedEvent != null &&
                   result.VulnerableConsumedEvent.ParentEventId ==
                   result.DeclaredAttack.EventId &&
                   result.DamageEvent != null &&
                   result.DamageEvent.ParentEventId ==
                   result.DeclaredAttack.EventId &&
                   result.CompletedAttack.ParentEventId ==
                   result.DeclaredAttack.EventId &&
                   runtime.LivingEnemies.Count == 0 &&
                   !runtime.EnemyPositions.FindPosition(
                       "ENEMY-ATTACK-VICTORY").HasValue &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   new BattleOutcomeEvaluator(
                       runtime.Player,
                       runtime.LivingEnemies).Evaluate() ==
                   BattleOutcome.Victory;
        }

        private static bool ValidateAttackLimitAndNextTurn(CardData card)
        {
            BattleRuntimeState runtime = Start(
                card,
                "ATTACK-LIMIT",
                3,
                new[]
                {
                    new EnemySetup(
                        "ENEMY-ATTACK-LIMIT-A",
                        10,
                        EnemyFieldPosition.Left),
                    new EnemySetup(
                        "ENEMY-ATTACK-LIMIT-B",
                        10,
                        EnemyFieldPosition.Right)
                },
                out BattleMonsterState attacker);
            if (runtime == null || attacker == null ||
                !BattleRuntimePlayerAttackService.TryResolve(
                    runtime,
                    attacker.BattleCardId,
                    "ENEMY-ATTACK-LIMIT-A",
                    out _,
                    out BattleRuntimePlayerAttackFailure firstFailure) ||
                firstFailure != BattleRuntimePlayerAttackFailure.None)
            {
                return false;
            }

            BattleEnemyRuntimeState second = runtime.FindEnemy(
                "ENEMY-ATTACK-LIMIT-B");
            BattleEnemyStatusState secondStatus =
                runtime.EnemyStatuses.Find(second.EnemyId);
            int secondHealthBefore = second.Vital.CurrentHealth;
            if (secondStatus == null || secondStatus.ApplyVulnerable(2) != 2)
            {
                return false;
            }

            bool duplicateRejected =
                !BattleRuntimePlayerAttackService.TryResolve(
                    runtime,
                    attacker.BattleCardId,
                    second.EnemyId,
                    out _,
                    out BattleRuntimePlayerAttackFailure duplicateFailure);
            if (!duplicateRejected ||
                duplicateFailure !=
                BattleRuntimePlayerAttackFailure.AttackAlreadyUsed ||
                second.Vital.CurrentHealth != secondHealthBefore ||
                secondStatus.Vulnerable != 2 ||
                runtime.Turn.Phase != BattleTurnPhase.PlayerAction ||
                !runtime.Turn.TryEndPlayerTurn(out _) ||
                !runtime.Turn.TryCompleteEnemyTurn(out _))
            {
                return false;
            }

            return BattleRuntimePlayerAttackService.TryResolve(
                       runtime,
                       attacker.BattleCardId,
                       second.EnemyId,
                       out BattleRuntimePlayerAttackResult nextTurnResult,
                       out BattleRuntimePlayerAttackFailure nextTurnFailure) &&
                   nextTurnFailure ==
                   BattleRuntimePlayerAttackFailure.None &&
                   nextTurnResult != null &&
                   nextTurnResult.VulnerableBonus == 2 &&
                   runtime.Turn.PlayerTurnNumber == 2 &&
                   secondStatus.Vulnerable == 0 &&
                   second.Vital.CurrentHealth ==
                   secondHealthBefore - attacker.Attack - 2;
        }

        private static bool ValidateC03AttackEvent(CardData card)
        {
            BattleRuntimeState runtime = Start(
                card,
                "ATTACK-C03",
                1,
                new[]
                {
                    new EnemySetup(
                        "ENEMY-ATTACK-C03",
                        10,
                        EnemyFieldPosition.Center)
                },
                out BattleMonsterState attacker,
                out int firstPlayerTurnEventIndex);
            if (runtime == null || attacker == null ||
                !BattleRuntimePlayerAttackService.TryResolve(
                    runtime,
                    attacker.BattleCardId,
                    "ENEMY-ATTACK-C03",
                    out _,
                    out _) ||
                !BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                    runtime,
                    firstPlayerTurnEventIndex,
                    out BattleRuntimeTurnEffectResult turnResult,
                    out BattleTurnFailure turnFailure))
            {
                return false;
            }

            return turnFailure == BattleTurnFailure.None &&
                   turnResult != null &&
                   turnResult.TotalDefenseGained == 0 &&
                   attacker.Defense == 0;
        }

        private static BattleRuntimeState Start(
            CardData card,
            string suffix,
            int level,
            IEnumerable<EnemySetup> enemies,
            out BattleMonsterState attacker)
        {
            return Start(
                card,
                suffix,
                level,
                enemies,
                out attacker,
                out _);
        }

        private static BattleRuntimeState Start(
            CardData card,
            string suffix,
            int level,
            IEnumerable<EnemySetup> enemies,
            out BattleMonsterState attacker,
            out int firstPlayerTurnEventIndex)
        {
            attacker = null;
            firstPlayerTurnEventIndex = 0;
            BattleCardInstance cardInstance = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-{suffix}",
                    $"BATTLE-{suffix}"),
                level,
                CardZone.DrawPile);
            BattleRuntimeState runtime = new(
                new[] { cardInstance },
                352,
                5);
            foreach (EnemySetup enemy in enemies)
            {
                if (!runtime.TryAddEnemy(
                        enemy.EnemyId,
                        0,
                        enemy.Health,
                        enemy.Position,
                        out _))
                {
                    return null;
                }
            }

            if (!runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _))
            {
                return null;
            }

            firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    cardInstance.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _))
            {
                return null;
            }

            attacker = playResult.SummonedMonster;
            return runtime;
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private readonly struct EnemySetup
        {
            public EnemySetup(
                string enemyId,
                int health,
                EnemyFieldPosition position)
            {
                EnemyId = enemyId;
                Health = health;
                Position = position;
            }

            public string EnemyId { get; }
            public int Health { get; }
            public EnemyFieldPosition Position { get; }
        }
    }

    internal static class BattleRuntimePlayerCardActionValidation
    {
        [MenuItem("Have a Break/Validate Unified Player Card Actions")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Unified Player Card Actions",
                valid
                    ? "Unified player card actions C01-C12 passed."
                    : "Unified player card actions failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase database =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(
                    "Assets/GameData/CardDatabase.asset");
            if (database == null || database.Cards.Count != 12)
            {
                Debug.LogError(
                    "Unified player action validation requires C01-C12.");
                return false;
            }

            Dictionary<string, CardData> cards = database.Cards.ToDictionary(
                card => card.CatalogCardId,
                StringComparer.OrdinalIgnoreCase);
            bool valid = ValidateInvalidInputs(cards) &&
                         ValidateMonsterRoutes(cards) &&
                         ValidateSkillRoutes(cards) &&
                         ValidateInstalledRoutes(cards);
            if (valid)
            {
                Debug.Log("Unified player card actions C01-C12 passed.");
            }
            else
            {
                Debug.LogError("Unified player card actions failed.");
            }

            return valid;
        }

        private static bool ValidateInvalidInputs(
            IReadOnlyDictionary<string, CardData> cards)
        {
            if (!cards.TryGetValue(TestContentIds.C05, out CardData c05) ||
                !cards.TryGetValue(TestContentIds.C07, out CardData c07) ||
                !cards.TryGetValue(TestContentIds.C03, out CardData c03))
            {
                return false;
            }

            BattleRuntimeState targetRuntime = Start(
                c05, 1, null, 1, out BattleCardInstance targetSource, out _);
            int targetMana = targetRuntime?.CardPlay.Mana.CurrentMana ?? -1;
            int targetEvents = targetRuntime?.EventLog.Events.Count ?? -1;
            bool targetRejected = targetRuntime != null &&
                !BattleRuntimePlayerCardActionService.TryResolve(
                    targetRuntime,
                    targetSource.Ids.BattleCardId,
                    "MISSING-ENEMY",
                    null,
                    out _,
                    out BattleRuntimePlayerCardActionFailure targetFailure,
                    out BattleRuntimeCardPlayFailure targetPlayFailure,
                    out CardPlayFailure targetCardFailure) &&
                targetFailure ==
                BattleRuntimePlayerCardActionFailure.MissingTarget &&
                targetPlayFailure == BattleRuntimeCardPlayFailure.None &&
                targetCardFailure == CardPlayFailure.None &&
                targetSource.Zone == CardZone.Hand &&
                targetRuntime.CardPlay.Mana.CurrentMana == targetMana &&
                targetRuntime.EventLog.Events.Count == targetEvents;

            BattleRuntimeState banishRuntime = Start(
                c07, 1, c03, 1,
                out BattleCardInstance banishSource,
                out BattleCardInstance selectable);
            int banishMana = banishRuntime?.CardPlay.Mana.CurrentMana ?? -1;
            int banishEvents = banishRuntime?.EventLog.Events.Count ?? -1;
            bool banishRejected = banishRuntime != null &&
                !BattleRuntimePlayerCardActionService.TryResolve(
                    banishRuntime,
                    banishSource.Ids.BattleCardId,
                    null,
                    banishSource.Ids.BattleCardId,
                    out _,
                    out BattleRuntimePlayerCardActionFailure banishFailure,
                    out _,
                    out _) &&
                banishFailure == BattleRuntimePlayerCardActionFailure
                    .InvalidBanishSelection &&
                banishSource.Zone == CardZone.Hand &&
                selectable.Zone == CardZone.Hand &&
                banishRuntime.CardPlay.Mana.CurrentMana == banishMana &&
                banishRuntime.EventLog.Events.Count == banishEvents;

            return targetRejected && banishRejected;
        }

        private static bool ValidateMonsterRoutes(
            IReadOnlyDictionary<string, CardData> cards)
        {
            string[] ids =
            {
                TestContentIds.C01,
                TestContentIds.C02,
                TestContentIds.C03,
                TestContentIds.C04
            };
            foreach (string id in ids)
            {
                if (!cards.TryGetValue(id, out CardData card))
                {
                    return false;
                }

                BattleRuntimeState runtime = Start(
                    card, 1, null, 1, out BattleCardInstance source, out _);
                if (!Resolve(
                        runtime,
                        source,
                        Is(id, TestContentIds.C01) ? "ENEMY-A" : null,
                        null,
                        out BattleRuntimePlayerCardActionResult result) ||
                    result.Play.SummonedMonster == null ||
                    source.Zone != CardZone.MonsterField)
                {
                    return false;
                }

                bool expectsEffect = Is(id, TestContentIds.C01) ||
                                     Is(id, TestContentIds.C02);
                if (expectsEffect != (result.SummonEffect != null) ||
                    result.SkillEffect != null || result.C07Effect != null ||
                    result.TrapInstallation != null)
                {
                    return false;
                }

                if (Is(id, TestContentIds.C01) &&
                    (result.SummonEffect.C01Result.ResolvedTargetEnemyId !=
                     "ENEMY-A" ||
                     runtime.EnemyPositions.FindPosition("ENEMY-A") !=
                     EnemyFieldPosition.Left))
                {
                    return false;
                }

                if (Is(id, TestContentIds.C02) &&
                    runtime.NextSkillModifiers.PendingCount != 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateSkillRoutes(
            IReadOnlyDictionary<string, CardData> cards)
        {
            if (!cards.TryGetValue(TestContentIds.C03, out CardData c03))
            {
                return false;
            }

            string[] ids =
            {
                TestContentIds.C05,
                TestContentIds.C06,
                TestContentIds.C07
            };
            foreach (string id in ids)
            {
                if (!cards.TryGetValue(id, out CardData card))
                {
                    return false;
                }

                int enemyCount = Is(id, TestContentIds.C06) ? 3 : 1;
                CardData extra = Is(id, TestContentIds.C07) ? c03 : null;
                BattleRuntimeState runtime = Start(
                    card,
                    Is(id, TestContentIds.C06) ? 5 : 1,
                    extra,
                    enemyCount,
                    out BattleCardInstance source,
                    out BattleCardInstance selected);
                if (!Resolve(
                        runtime,
                        source,
                        Is(id, TestContentIds.C07) ? null : "ENEMY-A",
                        selected?.Ids.BattleCardId,
                        out BattleRuntimePlayerCardActionResult result) ||
                    !Is(id, TestContentIds.C07) &&
                    source.Zone != CardZone.Graveyard)
                {
                    return false;
                }

                if ((Is(id, TestContentIds.C05) ||
                     Is(id, TestContentIds.C06)) &&
                    (result.SkillEffect == null || result.C07Effect != null))
                {
                    return false;
                }

                if (Is(id, TestContentIds.C07) &&
                    (result.C07Effect == null || selected == null ||
                     selected.Zone != CardZone.Banished))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateInstalledRoutes(
            IReadOnlyDictionary<string, CardData> cards)
        {
            string[] ids =
            {
                TestContentIds.C08,
                TestContentIds.C09,
                TestContentIds.C10,
                TestContentIds.C11,
                TestContentIds.C12
            };
            foreach (string id in ids)
            {
                if (!cards.TryGetValue(id, out CardData card))
                {
                    return false;
                }

                BattleRuntimeState runtime = Start(
                    card, 1, null, 1, out BattleCardInstance source, out _);
                if (!Resolve(
                        runtime,
                        source,
                        null,
                        null,
                        out BattleRuntimePlayerCardActionResult result) ||
                    source.Zone != CardZone.SkillField)
                {
                    return false;
                }

                bool isTrap = card.CardType == CardType.Trap;
                if (isTrap != (result.TrapInstallation != null) ||
                    runtime.TrapInstallations.Count != (isTrap ? 1 : 0) ||
                    result.SummonEffect != null ||
                    result.SkillEffect != null ||
                    result.C07Effect != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Resolve(
            BattleRuntimeState runtime,
            BattleCardInstance source,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            out BattleRuntimePlayerCardActionResult result)
        {
            result = null;
            return runtime != null && source != null &&
                   BattleRuntimePlayerCardActionService.TryResolve(
                       runtime,
                       source.Ids.BattleCardId,
                       targetEnemyId,
                       selectedBanishBattleCardId,
                       out result,
                       out BattleRuntimePlayerCardActionFailure failure,
                       out BattleRuntimeCardPlayFailure playFailure,
                       out CardPlayFailure cardPlayFailure) &&
                   failure == BattleRuntimePlayerCardActionFailure.None &&
                   playFailure == BattleRuntimeCardPlayFailure.None &&
                   cardPlayFailure == CardPlayFailure.None &&
                   result != null;
        }

        private static BattleRuntimeState Start(
            CardData sourceData,
            int sourceLevel,
            CardData extraData,
            int enemyCount,
            out BattleCardInstance source,
            out BattleCardInstance extra)
        {
            source = Instance(sourceData, sourceLevel, "SOURCE");
            extra = extraData == null
                ? null
                : Instance(extraData, 1, "EXTRA");
            List<BattleCardInstance> deck = new() { source };
            if (extra != null)
            {
                deck.Add(extra);
            }

            BattleRuntimeState runtime = new(deck, 353, 20);
            if (enemyCount >= 1 && !runtime.TryAddEnemy(
                    "ENEMY-A", 3, 10, EnemyFieldPosition.Center, out _))
            {
                return null;
            }

            if (enemyCount >= 2 && !runtime.TryAddEnemy(
                    "ENEMY-B", 5, 10, EnemyFieldPosition.Left, out _))
            {
                return null;
            }

            if (enemyCount >= 3 && !runtime.TryAddEnemy(
                    "ENEMY-C", 4, 10, EnemyFieldPosition.Right, out _))
            {
                return null;
            }

            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _)
                ? runtime
                : null;
        }

        private static BattleCardInstance Instance(
            CardData card,
            int level,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-ACTION-{card.CatalogCardId}-{suffix}",
                    $"BATTLE-ACTION-{card.CatalogCardId}-{suffix}"),
                level,
                CardZone.DrawPile);
        }

        private static bool Is(string actual, string expected)
        {
            return string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
