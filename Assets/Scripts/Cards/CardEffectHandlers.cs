using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public interface ICardEffectHandler { }

    public interface ISummonCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult play,
            EnchantFixedTargetDeclaration? target,
            out BattleRuntimeSummonEffectResult result,
            out BattleRuntimeSummonEffectFailure failure);
    }

    public interface ITargetedSkillCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult play,
            string targetEnemyId,
            out BattleRuntimeSkillEffectResult result,
            out BattleRuntimeSkillEffectFailure failure);
    }

    public interface IBanishSkillCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult play,
            string selectedBattleCardId,
            out BattleRuntimeC07EffectResult result);
    }

    public interface IPlayerTurnEndCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleMonsterState monster,
            int firstPlayerTurnEventIndex, out int defenseGained);
    }

    public interface IEnemyMovementMonsterCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleMonsterState monster,
            BattleEventRecord movedEvent, out int attackEnhancementGained);
    }

    public interface IEnemyMovementBarrierCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleCardInstance card,
            BattleEventRecord movedEvent, out int vulnerableGained, out int damageApplied);
    }

    public interface IPlayerTurnStartCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleCardInstance card,
            out int drawn, out string defendedMonsterId);
    }

    public interface IEnemyMoveTrapCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord moveAttemptEvent, int requestedSteps, string movingEnemyId,
            out int replacementSteps);
    }

    public interface IIncomingAttackTrapCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord declaredAttack, string targetBattleCardId, out int defenseGained);
    }

    public interface IEnemyAbilityTrapCardEffectHandler : ICardEffectHandler
    {
        bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord abilityEvent, EnemyAbilityResolutionContext ability,
            out bool cancelled, out bool returnedToHand);
    }

    internal sealed class C01CardEffectHandler : ISummonCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeCardPlayResult play,
            EnchantFixedTargetDeclaration? target, out BattleRuntimeSummonEffectResult result,
            out BattleRuntimeSummonEffectFailure failure)
        {
            result = null;
            if (!target.HasValue)
            {
                failure = BattleRuntimeSummonEffectFailure.MissingTargetDeclaration;
                return false;
            }
            if (!C01SleeperKeeperResolver.TryResolve(play.SummonedEvent, play.SummonedMonster,
                    target.Value, runtime.EnemyPositions, runtime.EnemyMovementLocks,
                    runtime.EventLog, runtime.EffectResolutions, out C01SleeperKeeperResult value))
            {
                failure = BattleRuntimeSummonEffectFailure.ResolutionFailed;
                return false;
            }
            result = new BattleRuntimeSummonEffectResult(
                play.Card.SourceCard.CatalogCardId, value, default);
            failure = BattleRuntimeSummonEffectFailure.None;
            return true;
        }
    }

    internal sealed class C02CardEffectHandler : ISummonCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeCardPlayResult play,
            EnchantFixedTargetDeclaration? target, out BattleRuntimeSummonEffectResult result,
            out BattleRuntimeSummonEffectFailure failure)
        {
            result = null;
            if (!C02LanternBearerResolver.TryResolve(play.SummonedEvent, play.SummonedMonster,
                    runtime.NextSkillModifiers, runtime.EventLog, runtime.EffectResolutions,
                    out C02LanternBearerResult value))
            {
                failure = BattleRuntimeSummonEffectFailure.ResolutionFailed;
                return false;
            }
            result = new BattleRuntimeSummonEffectResult(
                play.Card.SourceCard.CatalogCardId, default, value);
            failure = BattleRuntimeSummonEffectFailure.None;
            return true;
        }
    }

    internal sealed class C05CardEffectHandler : ITargetedSkillCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeCardPlayResult play,
            string targetEnemyId, out BattleRuntimeSkillEffectResult result,
            out BattleRuntimeSkillEffectFailure failure)
        {
            result = null;
            if (!C05PlatformPushResolver.TryResolve(play.PlayedEvent, play.Card, targetEnemyId,
                    runtime.EnemyPositions, runtime.EnemyMovementLocks, runtime.EnemyStatuses,
                    runtime.EventLog, runtime.EffectResolutions, out int moved, out int weaken,
                    out int vulnerable))
            {
                failure = BattleRuntimeSkillEffectFailure.ResolutionFailed;
                return false;
            }
            result = new BattleRuntimeSkillEffectResult(play.Card.SourceCard.CatalogCardId,
                targetEnemyId, moved, weaken, vulnerable, null);
            failure = BattleRuntimeSkillEffectFailure.None;
            return true;
        }
    }

    internal sealed class C06CardEffectHandler : ITargetedSkillCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeCardPlayResult play,
            string targetEnemyId, out BattleRuntimeSkillEffectResult result,
            out BattleRuntimeSkillEffectFailure failure)
        {
            result = null;
            List<BattleEnemyAttackSnapshot> enemies = new();
            foreach (BattleEnemyRuntimeState enemy in runtime.Enemies)
                if (enemy != null && enemy.IsAlive) enemies.Add(enemy.SnapshotAttack());
            if (!C06EmergencyBrakeResolver.TryResolve(play.PlayedEvent, play.Card, targetEnemyId,
                    enemies, runtime.EnemyStatuses, runtime.EventLog, runtime.EffectResolutions,
                    out string secondary))
            {
                failure = BattleRuntimeSkillEffectFailure.ResolutionFailed;
                return false;
            }
            result = new BattleRuntimeSkillEffectResult(play.Card.SourceCard.CatalogCardId,
                targetEnemyId, 0, 0, 0, secondary);
            failure = BattleRuntimeSkillEffectFailure.None;
            return true;
        }
    }

    internal sealed class C07CardEffectHandler : IBanishSkillCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeCardPlayResult play,
            string selectedBattleCardId, out BattleRuntimeC07EffectResult result)
        {
            result = null;
            if (!C07LostTicketResolver.TryResolve(play.PlayedEvent, play.Card,
                    selectedBattleCardId, runtime.Deck, runtime.Monsters, runtime.EventLog,
                    runtime.EffectResolutions, out int drawn, out bool banished,
                    out int defended)) return false;
            result = new BattleRuntimeC07EffectResult(drawn, banished, defended);
            return true;
        }
    }

    internal sealed class C03CardEffectHandler : IPlayerTurnEndCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleMonsterState monster,
            int firstPlayerTurnEventIndex, out int defenseGained)
        {
            defenseGained = 0;
            if (!C03SeatRepairerTurnEndResolver.TryResolve(monster,
                    runtime.Turn.PlayerTurnNumber, firstPlayerTurnEventIndex, runtime.EventLog,
                    runtime.EffectResolutions, out C03SeatRepairerResult result)) return false;
            defenseGained = result.DefenseGained;
            return true;
        }
    }

    internal sealed class C04CardEffectHandler : IEnemyMovementMonsterCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleMonsterState monster,
            BattleEventRecord movedEvent, out int attackEnhancementGained) =>
            C04TerminalCatResolver.TryResolve(movedEvent, runtime.Turn.PlayerTurnNumber,
                monster, runtime.CardTurnTriggers, runtime.EventLog,
                out attackEnhancementGained);
    }

    internal sealed class C12CardEffectHandler : IEnemyMovementBarrierCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleCardInstance card,
            BattleEventRecord movedEvent, out int vulnerableGained, out int damageApplied) =>
            C12RouteMapStarlightResolver.TryResolve(movedEvent,
                runtime.Turn.PlayerTurnNumber, card, runtime.CardTurnTriggers,
                runtime.EnemyStatuses, runtime.FindEnemy(movedEvent.TargetId)?.Vital,
                runtime.EventLog, out vulnerableGained, out damageApplied);
    }

    internal sealed class C11CardEffectHandler : IPlayerTurnStartCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleCardInstance card,
            out int drawn, out string defendedMonsterId) =>
            C11LateNightWaitingRoomResolver.TryResolve(card,
                runtime.Turn.PlayerTurnNumber, runtime.Deck, runtime.Monsters,
                runtime.EventLog, runtime.EffectResolutions, out drawn,
                out defendedMonsterId);
    }

    internal sealed class C08CardEffectHandler : IEnemyMoveTrapCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord moveAttemptEvent, int requestedSteps, string movingEnemyId,
            out int replacementSteps) => C08ClosingDoorResolver.TryReplace(moveAttemptEvent,
                runtime.Turn.PlayerTurnNumber, installation.EligibleEnemyTurn, requestedSteps,
                movingEnemyId, installation.SourceTrap, runtime.EnemyMovementLocks,
                runtime.EnemyStatuses, runtime.CardTurnTriggers, runtime.EventLog,
                out replacementSteps);
    }

    internal sealed class C09CardEffectHandler : IIncomingAttackTrapCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord declaredAttack, string targetBattleCardId, out int defenseGained) =>
            C09InspectionBlanketResolver.TryResolve(declaredAttack,
                runtime.Turn.PlayerTurnNumber, installation.EligibleEnemyTurn,
                installation.SourceTrap, runtime.Monsters.Find(targetBattleCardId),
                runtime.DefenseRetention, runtime.CardTurnTriggers, runtime.EventLog,
                out defenseGained);
    }

    internal sealed class C10CardEffectHandler : IEnemyAbilityTrapCardEffectHandler
    {
        public bool TryResolve(BattleRuntimeState runtime, BattleRuntimeTrapInstallation installation,
            BattleEventRecord abilityEvent, EnemyAbilityResolutionContext ability,
            out bool cancelled, out bool returnedToHand)
        {
            bool resolved = C10BrokenCallLineResolver.TryCancel(abilityEvent, ability,
                installation.SourceTrap, runtime.Deck.Zones, runtime.EnemyStatuses,
                runtime.EventLog, runtime.EffectResolutions, out cancelled, out returnedToHand);
            if (resolved && returnedToHand) runtime.TrapInstallations.TryRemove(installation);
            return resolved;
        }
    }
}
