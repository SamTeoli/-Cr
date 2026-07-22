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
}
