using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeFriendlyStatusTurnService
    {
        public static bool TryResolveTurnEnd(
            BattleRuntimeState runtime,
            out BattleRuntimeFriendlyStatusTurnResult result)
        {
            result = null;
            if (runtime?.Player == null || runtime.Monsters == null ||
                runtime.Deck == null || runtime.EventLog == null)
            {
                return false;
            }

            List<BattleRuntimeFriendlyStatusTurnEntryResult> entries = new();
            int totalInjuryDamage = 0;
            int defeatedMonsterCount = 0;
            BattleStateBasedChecker checker = new(
                runtime.Deck,
                runtime.Monsters,
                runtime.EventLog,
                runtime.PlayerMonsterPositions);
            List<BattleMonsterState> monsters = new(
                runtime.Monsters.Monsters);
            foreach (BattleMonsterState monster in monsters)
            {
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    monster.IsDestructionCandidate)
                {
                    continue;
                }

                BattleRuntimeFriendlyStatusTurnEntryResult entry = Resolve(
                    runtime,
                    monster.BattleCardId,
                    false,
                    monster.Status,
                    monster.CurrentHealth,
                    monster.ApplyDamage,
                    () => monster.CurrentHealth);
                entries.Add(entry);
                totalInjuryDamage += entry.DamageApplied;
                if (!checker.TryResolveMonsterDestruction(
                        entry.InjuryDamageEvent?.EventId,
                        out List<BattleEventRecord> destructionEvents,
                        out _))
                {
                    return false;
                }

                defeatedMonsterCount += destructionEvents.Count;
            }

            BattleRuntimeFriendlyStatusTurnEntryResult playerEntry = Resolve(
                runtime,
                BattlePlayerState.PlayerTargetId,
                true,
                runtime.Player.Status,
                runtime.Player.CurrentHealth,
                runtime.Player.ApplyDamage,
                () => runtime.Player.CurrentHealth);
            entries.Add(playerEntry);
            totalInjuryDamage += playerEntry.DamageApplied;

            result = new BattleRuntimeFriendlyStatusTurnResult(
                entries,
                totalInjuryDamage,
                defeatedMonsterCount,
                runtime.Player.IsDefeated);
            return true;
        }

        private static BattleRuntimeFriendlyStatusTurnEntryResult Resolve(
            BattleRuntimeState runtime,
            string targetId,
            bool targetsPlayer,
            BattleCommonStatusState status,
            int healthBefore,
            Func<int, int> applyDamage,
            Func<int> currentHealth)
        {
            int injuryBefore = status.Injury;
            int bindBefore = status.Bind;
            int stunBefore = status.Stun;
            int weakenBefore = status.Weaken;
            int baseInjuryDamage = status.ResolveInjuryAtTurnEnd();
            int vulnerableBonus = baseInjuryDamage > 0
                ? status.ConsumeVulnerable()
                : 0;
            if (vulnerableBonus > 0)
            {
                runtime.EventLog.Record(
                    BattleEventType.StatusApplied,
                    "FriendlyVulnerableConsumedByInjury",
                    targetId,
                    targetId,
                    targetId,
                    beforeValue: vulnerableBonus,
                    afterValue: status.Vulnerable);
            }

            int damageApplied = applyDamage(
                baseInjuryDamage + vulnerableBonus);
            BattleEventRecord damageEvent = damageApplied > 0
                ? runtime.EventLog.Record(
                    BattleEventType.DamageApplied,
                    "FriendlyInjuryTurnEndDamage",
                    targetId,
                    targetId,
                    targetId,
                    beforeValue: healthBefore,
                    afterValue: currentHealth())
                : null;

            status.ReduceBindAtTurnEnd();
            status.ClearStunAtTurnEnd();
            status.ReduceWeakenAtTurnEnd();
            RecordDecay(
                runtime,
                targetId,
                "FriendlyInjuryTurnEndDecay",
                injuryBefore,
                status.Injury);
            RecordDecay(
                runtime,
                targetId,
                "FriendlyBindTurnEndDecay",
                bindBefore,
                status.Bind);
            RecordDecay(
                runtime,
                targetId,
                "FriendlyStunTurnEndClear",
                stunBefore,
                status.Stun);
            RecordDecay(
                runtime,
                targetId,
                "FriendlyWeakenTurnEndDecay",
                weakenBefore,
                status.Weaken);

            return new BattleRuntimeFriendlyStatusTurnEntryResult(
                targetId,
                targetsPlayer,
                baseInjuryDamage,
                vulnerableBonus,
                damageApplied,
                healthBefore,
                currentHealth(),
                injuryBefore,
                status.Injury,
                bindBefore,
                status.Bind,
                stunBefore,
                status.Stun,
                weakenBefore,
                status.Weaken,
                damageEvent);
        }

        private static void RecordDecay(
            BattleRuntimeState runtime,
            string targetId,
            string cause,
            int before,
            int after)
        {
            if (before == after)
            {
                return;
            }

            runtime.EventLog.Record(
                BattleEventType.StatusApplied,
                cause,
                targetId,
                targetId,
                targetId,
                beforeValue: before,
                afterValue: after);
        }
    }
}
