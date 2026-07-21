using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeCardPlayService
    {
        public static bool TryPlay(
            BattleRuntimeState runtime,
            string battleCardId,
            out BattleRuntimeCardPlayResult result,
            out BattleRuntimeCardPlayFailure failure,
            out CardPlayFailure cardPlayFailure)
        {
            result = null;
            cardPlayFailure = CardPlayFailure.None;
            if (runtime == null || string.IsNullOrWhiteSpace(battleCardId))
            {
                failure = BattleRuntimeCardPlayFailure.InvalidRuntime;
                return false;
            }

            if (!runtime.Turn.CanAcceptPlayerAction)
            {
                failure = BattleRuntimeCardPlayFailure.InvalidTurnPhase;
                return false;
            }

            BattleCardInstance card = runtime.Deck.Zones.Find(battleCardId);
            if (!runtime.CardPlay.TryPreviewPlay(
                    battleCardId, out CardPlayPreview preview, out cardPlayFailure))
            {
                failure = BattleRuntimeCardPlayFailure.PreviewFailed;
                return false;
            }

            if (!runtime.Turn.TryBeginPlayerAction(out _))
            {
                failure = BattleRuntimeCardPlayFailure.BeginActionFailed;
                return false;
            }

            int manaBefore = runtime.CardPlay.Mana.CurrentMana;
            if (!runtime.CardPlay.TryConfirmPlay(preview, out cardPlayFailure))
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure = BattleRuntimeCardPlayFailure.ConfirmFailed;
                return false;
            }

            BattleEventRecord playedEvent = runtime.EventLog.Record(
                BattleEventType.CardPlayed,
                "PlayerCardPlayConfirmed",
                card.Ids.BattleCardId,
                card.Ids.BattleCardId,
                card.Ids.BattleCardId,
                beforeValue: manaBefore,
                afterValue: runtime.CardPlay.Mana.CurrentMana);

            BattleEventRecord summonedEvent = null;
            BattleMonsterState summonedMonster = null;
            if (card.SourceCard.CardType == CardType.Monster)
            {
                if (!runtime.TryRegisterFieldMonster(
                        card.Ids.BattleCardId, out summonedMonster))
                {
                    runtime.Turn.TryCompletePlayerAction(out _);
                    failure = BattleRuntimeCardPlayFailure.MonsterRegistrationFailed;
                    return false;
                }

                summonedEvent = runtime.EventLog.Record(
                    BattleEventType.MonsterSummoned,
                    "MonsterPlayResolved",
                    card.Ids.BattleCardId,
                    card.Ids.BattleCardId,
                    card.Ids.BattleCardId,
                    parentEventId: playedEvent.EventId);
            }

            if (!runtime.Turn.TryCompletePlayerAction(out _))
            {
                failure = BattleRuntimeCardPlayFailure.CompleteActionFailed;
                return false;
            }

            result = new BattleRuntimeCardPlayResult(
                card, preview, playedEvent, summonedEvent, summonedMonster);
            failure = BattleRuntimeCardPlayFailure.None;
            return true;
        }
    }

    public enum BattleRuntimePlayerCardActionFailure
    {
        None,
        InvalidRuntime,
        CardNotFound,
        UnsupportedTestCard,
        MissingTarget,
        InvalidBanishSelection,
        CardPlayFailed,
        ImmediateEffectFailed
    }

    public sealed class BattleRuntimePlayerCardActionResult
    {
        internal BattleRuntimePlayerCardActionResult(
            BattleRuntimeCardPlayResult play,
            BattleRuntimeSummonEffectResult summonEffect,
            BattleRuntimeSkillEffectResult skillEffect,
            BattleRuntimeC07EffectResult c07Effect,
            BattleRuntimeTrapInstallation trapInstallation)
        {
            Play = play;
            SummonEffect = summonEffect;
            SkillEffect = skillEffect;
            C07Effect = c07Effect;
            TrapInstallation = trapInstallation;
        }

        public BattleRuntimeCardPlayResult Play { get; }
        public BattleRuntimeSummonEffectResult SummonEffect { get; }
        public BattleRuntimeSkillEffectResult SkillEffect { get; }
        public BattleRuntimeC07EffectResult C07Effect { get; }
        public BattleRuntimeTrapInstallation TrapInstallation { get; }
        public bool ResolvedImmediateEffect =>
            SummonEffect != null || SkillEffect != null ||
            C07Effect != null || TrapInstallation != null;
    }

    // Disposable C01-C12 adapter for the prototype play screen. It keeps the
    // test-content routing out of UI code so the entire content set can later
    // be replaced without changing the generic card-play state machine.
    public static class BattleRuntimePlayerCardActionService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            string battleCardId,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            out BattleRuntimePlayerCardActionResult result,
            out BattleRuntimePlayerCardActionFailure failure,
            out BattleRuntimeCardPlayFailure playFailure,
            out CardPlayFailure cardPlayFailure)
        {
            result = null;
            playFailure = BattleRuntimeCardPlayFailure.None;
            cardPlayFailure = CardPlayFailure.None;
            if (runtime?.Deck?.Zones == null ||
                string.IsNullOrWhiteSpace(battleCardId))
            {
                failure = BattleRuntimePlayerCardActionFailure.InvalidRuntime;
                return false;
            }

            BattleCardInstance card = runtime.Deck.Zones.Find(battleCardId);
            if (card?.SourceCard == null)
            {
                failure = BattleRuntimePlayerCardActionFailure.CardNotFound;
                return false;
            }

            string catalogCardId = card.SourceCard.CatalogCardId;
            if (!IsSupportedTestCard(catalogCardId))
            {
                failure =
                    BattleRuntimePlayerCardActionFailure.UnsupportedTestCard;
                return false;
            }

            EnchantFixedTargetDeclaration? summonTarget = null;
            if (Is(catalogCardId, TestContentIds.C01))
            {
                if (!EnchantFixedTargetResolver.TryDeclare(
                        battleCardId,
                        targetEnemyId,
                        runtime.EnemyPositions,
                        runtime.Enchants,
                        out EnchantFixedTargetDeclaration declaration) ||
                    !IsActiveEnemy(runtime, targetEnemyId))
                {
                    failure =
                        BattleRuntimePlayerCardActionFailure.MissingTarget;
                    return false;
                }

                summonTarget = declaration;
            }
            else if (Is(catalogCardId, TestContentIds.C05) ||
                     Is(catalogCardId, TestContentIds.C06))
            {
                if (!IsActiveEnemy(runtime, targetEnemyId))
                {
                    failure =
                        BattleRuntimePlayerCardActionFailure.MissingTarget;
                    return false;
                }
            }
            else if (Is(catalogCardId, TestContentIds.C07))
            {
                BattleCardInstance selected = runtime.Deck.Zones.Find(
                    selectedBanishBattleCardId);
                if (selected == null || selected == card ||
                    selected.Zone != CardZone.Hand)
                {
                    failure = BattleRuntimePlayerCardActionFailure
                        .InvalidBanishSelection;
                    return false;
                }
            }

            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    battleCardId,
                    out BattleRuntimeCardPlayResult play,
                    out playFailure,
                    out cardPlayFailure))
            {
                failure =
                    BattleRuntimePlayerCardActionFailure.CardPlayFailed;
                return false;
            }

            BattleRuntimeSummonEffectResult summonEffect = null;
            BattleRuntimeSkillEffectResult skillEffect = null;
            BattleRuntimeC07EffectResult c07Effect = null;
            BattleRuntimeTrapInstallation trapInstallation = null;
            bool effectResolved = true;

            if (Is(catalogCardId, TestContentIds.C01) ||
                Is(catalogCardId, TestContentIds.C02))
            {
                effectResolved = BattleRuntimeSummonEffectService.TryResolve(
                    runtime,
                    play,
                    summonTarget,
                    out summonEffect,
                    out _);
            }
            else if (Is(catalogCardId, TestContentIds.C05) ||
                     Is(catalogCardId, TestContentIds.C06))
            {
                effectResolved = BattleRuntimeSkillEffectService.TryResolve(
                    runtime,
                    play,
                    targetEnemyId,
                    out skillEffect,
                    out _);
            }
            else if (Is(catalogCardId, TestContentIds.C07))
            {
                effectResolved = BattleRuntimeC07EffectService.TryResolve(
                    runtime,
                    play,
                    selectedBanishBattleCardId,
                    out c07Effect);
            }
            else if (Is(catalogCardId, TestContentIds.C08) ||
                     Is(catalogCardId, TestContentIds.C09) ||
                     Is(catalogCardId, TestContentIds.C10))
            {
                effectResolved =
                    BattleRuntimeTrapEffectService.TryRegisterInstallation(
                        runtime,
                        play,
                        out trapInstallation);
            }

            if (!effectResolved)
            {
                failure = BattleRuntimePlayerCardActionFailure
                    .ImmediateEffectFailed;
                return false;
            }

            result = new BattleRuntimePlayerCardActionResult(
                play,
                summonEffect,
                skillEffect,
                c07Effect,
                trapInstallation);
            failure = BattleRuntimePlayerCardActionFailure.None;
            return true;
        }

        private static bool IsActiveEnemy(
            BattleRuntimeState runtime,
            string enemyId)
        {
            BattleEnemyRuntimeState enemy = runtime.FindEnemy(enemyId);
            return enemy != null && enemy.IsAlive &&
                   runtime.LivingEnemies.Contains(enemyId) &&
                   runtime.EnemyPositions.FindPosition(enemyId).HasValue &&
                   runtime.EnemyStatuses.Find(enemyId) != null;
        }

        private static bool IsSupportedTestCard(string catalogCardId)
        {
            return Is(catalogCardId, TestContentIds.C01) ||
                   Is(catalogCardId, TestContentIds.C02) ||
                   Is(catalogCardId, TestContentIds.C03) ||
                   Is(catalogCardId, TestContentIds.C04) ||
                   Is(catalogCardId, TestContentIds.C05) ||
                   Is(catalogCardId, TestContentIds.C06) ||
                   Is(catalogCardId, TestContentIds.C07) ||
                   Is(catalogCardId, TestContentIds.C08) ||
                   Is(catalogCardId, TestContentIds.C09) ||
                   Is(catalogCardId, TestContentIds.C10) ||
                   Is(catalogCardId, TestContentIds.C11) ||
                   Is(catalogCardId, TestContentIds.C12);
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
