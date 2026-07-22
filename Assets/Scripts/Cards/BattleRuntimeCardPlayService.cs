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
            return TryPlay(
                runtime,
                battleCardId,
                false,
                out result,
                out failure,
                out cardPlayFailure);
        }

        internal static bool TryPlay(
            BattleRuntimeState runtime,
            string battleCardId,
            bool deferSkillResolution,
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
            if (!runtime.CardPlay.TryConfirmPlay(
                    preview,
                    deferSkillResolution,
                    out cardPlayFailure))
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
        public static bool TryValidate(
            BattleRuntimeState runtime,
            string battleCardId,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            out BattleRuntimePlayerCardActionFailure failure,
            out BattleRuntimeCardPlayFailure playFailure,
            out CardPlayFailure cardPlayFailure)
        {
            playFailure = BattleRuntimeCardPlayFailure.None;
            cardPlayFailure = CardPlayFailure.None;
            if (!TryPrepare(
                    runtime,
                    battleCardId,
                    targetEnemyId,
                    selectedBanishBattleCardId,
                    out _,
                    out _,
                    out failure))
            {
                return false;
            }

            if (!runtime.Turn.CanAcceptPlayerAction)
            {
                failure = BattleRuntimePlayerCardActionFailure.CardPlayFailed;
                playFailure = BattleRuntimeCardPlayFailure.InvalidTurnPhase;
                return false;
            }

            if (!runtime.CardPlay.TryPreviewPlay(
                    battleCardId, out _, out cardPlayFailure))
            {
                failure = BattleRuntimePlayerCardActionFailure.CardPlayFailed;
                playFailure = BattleRuntimeCardPlayFailure.PreviewFailed;
                return false;
            }

            failure = BattleRuntimePlayerCardActionFailure.None;
            return true;
        }

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
            if (!TryPrepare(
                    runtime,
                    battleCardId,
                    targetEnemyId,
                    selectedBanishBattleCardId,
                    out BattleCardInstance card,
                    out EnchantFixedTargetDeclaration? summonTarget,
                    out failure))
            {
                return false;
            }

            CardEffectRegistration registration = FindRegistration(card);
            bool deferSkillResolution = registration.DefersSkillResolution;
            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    battleCardId,
                    deferSkillResolution,
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

            if (registration.Route == CardEffectRoute.Summon)
            {
                effectResolved = BattleRuntimeSummonEffectService.TryResolve(
                    runtime,
                    play,
                    summonTarget,
                    out summonEffect,
                    out _);
            }
            else if (registration.Route == CardEffectRoute.TargetedSkill)
            {
                effectResolved = BattleRuntimeSkillEffectService.TryResolve(
                    runtime,
                    play,
                    targetEnemyId,
                    out skillEffect,
                    out _);
            }
            else if (registration.Route == CardEffectRoute.BanishSkill)
            {
                effectResolved = BattleRuntimeC07EffectService.TryResolve(
                    runtime,
                    play,
                    selectedBanishBattleCardId,
                    out c07Effect);

                if (effectResolved)
                {
                    effectResolved = runtime.Deck.TryResolveGraveyardMove(
                        play.Card.Ids.BattleCardId,
                        runtime.Enchants,
                        true,
                        out _);
                }
            }
            else if (registration.Route == CardEffectRoute.TrapInstallation)
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

        private static bool TryPrepare(
            BattleRuntimeState runtime,
            string battleCardId,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            out BattleCardInstance card,
            out EnchantFixedTargetDeclaration? summonTarget,
            out BattleRuntimePlayerCardActionFailure failure)
        {
            card = null;
            summonTarget = null;
            if (runtime?.Deck?.Zones == null ||
                string.IsNullOrWhiteSpace(battleCardId))
            {
                failure = BattleRuntimePlayerCardActionFailure.InvalidRuntime;
                return false;
            }

            card = runtime.Deck.Zones.Find(battleCardId);
            if (card?.SourceCard == null)
            {
                failure = BattleRuntimePlayerCardActionFailure.CardNotFound;
                return false;
            }

            if (!CardEffectRegistrationCatalog.TryFind(
                    card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration))
            {
                failure =
                    BattleRuntimePlayerCardActionFailure.UnsupportedTestCard;
                return false;
            }

            if (registration.Route == CardEffectRoute.Summon &&
                card.SourceCard.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.FixedSingleEnemyTarget))
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
            else if (registration.Route == CardEffectRoute.TargetedSkill)
            {
                if (!IsActiveEnemy(runtime, targetEnemyId))
                {
                    failure =
                        BattleRuntimePlayerCardActionFailure.MissingTarget;
                    return false;
                }
            }
            else if (registration.Route == CardEffectRoute.BanishSkill)
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

        private static CardEffectRegistration FindRegistration(BattleCardInstance card)
        {
            CardEffectRegistrationCatalog.TryFind(
                card.SourceCard.CatalogCardId, out CardEffectRegistration registration);
            return registration;
        }
    }
}
