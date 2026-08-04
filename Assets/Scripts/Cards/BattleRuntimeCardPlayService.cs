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
                null,
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
            return TryPlay(
                runtime,
                battleCardId,
                deferSkillResolution,
                null,
                out result,
                out failure,
                out cardPlayFailure);
        }

        internal static bool TryPlay(
            BattleRuntimeState runtime,
            string battleCardId,
            bool deferSkillResolution,
            PlayerMonsterFieldPosition? monsterPosition,
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
            if (monsterPosition.HasValue &&
                (card?.SourceCard?.CardType != CardType.Monster ||
                 !Enum.IsDefined(
                     typeof(PlayerMonsterFieldPosition),
                     monsterPosition.Value) ||
                 !string.IsNullOrWhiteSpace(
                     runtime.PlayerMonsterPositions.GetOccupant(
                         monsterPosition.Value))))
            {
                failure =
                    BattleRuntimeCardPlayFailure.MonsterRegistrationFailed;
                return false;
            }

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
                bool registered = monsterPosition.HasValue
                    ? runtime.TryRegisterFieldMonster(
                        card.Ids.BattleCardId,
                        monsterPosition.Value,
                        out summonedMonster)
                    : runtime.TryRegisterFieldMonster(
                        card.Ids.BattleCardId,
                        out summonedMonster);
                if (!registered)
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
            return TryValidate(
                runtime,
                battleCardId,
                targetEnemyId,
                selectedBanishBattleCardId,
                null,
                out failure,
                out playFailure,
                out cardPlayFailure);
        }

        public static bool TryValidate(
            BattleRuntimeState runtime,
            string battleCardId,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            PlayerMonsterFieldPosition? monsterPosition,
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
                    out BattleCardInstance card,
                    out _,
                    out failure))
            {
                return false;
            }

            if (!TryValidateMonsterPosition(
                    runtime,
                    card,
                    monsterPosition,
                    out failure,
                    out playFailure))
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
            return TryResolve(
                runtime,
                battleCardId,
                targetEnemyId,
                selectedBanishBattleCardId,
                null,
                out result,
                out failure,
                out playFailure,
                out cardPlayFailure);
        }

        public static bool TryResolve(
            BattleRuntimeState runtime,
            string battleCardId,
            string targetEnemyId,
            string selectedBanishBattleCardId,
            PlayerMonsterFieldPosition? monsterPosition,
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

            if (!TryValidateMonsterPosition(
                    runtime,
                    card,
                    monsterPosition,
                    out failure,
                    out playFailure))
            {
                return false;
            }

            CardEffectRegistration registration = FindRegistration(card);
            bool deferSkillResolution = registration.DefersSkillResolution;
            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    battleCardId,
                    deferSkillResolution,
                    monsterPosition,
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
                bool waitsForTarget =
                    card.SourceCard.HasEnchantCompatibilityTag(
                        EnchantCompatibilityTag.FixedSingleEnemyTarget) &&
                    !summonTarget.HasValue;
                if (!waitsForTarget)
                {
                    effectResolved =
                        BattleRuntimeSummonEffectService.TryResolve(
                            runtime,
                            play,
                            summonTarget,
                            out summonEffect,
                            out _);
                }
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

        private static bool TryValidateMonsterPosition(
            BattleRuntimeState runtime,
            BattleCardInstance card,
            PlayerMonsterFieldPosition? monsterPosition,
            out BattleRuntimePlayerCardActionFailure failure,
            out BattleRuntimeCardPlayFailure playFailure)
        {
            failure = BattleRuntimePlayerCardActionFailure.None;
            playFailure = BattleRuntimeCardPlayFailure.None;
            if (!monsterPosition.HasValue)
            {
                return true;
            }

            bool validPosition = Enum.IsDefined(
                typeof(PlayerMonsterFieldPosition),
                monsterPosition.Value);
            bool empty = validPosition &&
                         string.IsNullOrWhiteSpace(
                             runtime?.PlayerMonsterPositions?.GetOccupant(
                                 monsterPosition.Value));
            if (card?.SourceCard?.CardType == CardType.Monster && empty)
            {
                return true;
            }

            failure = BattleRuntimePlayerCardActionFailure.CardPlayFailed;
            playFailure =
                BattleRuntimeCardPlayFailure.MonsterRegistrationFailed;
            return false;
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
                if (!string.IsNullOrWhiteSpace(targetEnemyId))
                {
                    if (!EffectTargetResolver.TryResolveSingleTarget(
                            runtime,
                            registration.ResolveTargetSpec(
                                card.SourceCard),
                            targetEnemyId,
                            out EffectTargetCandidate candidate) ||
                        !EnchantFixedTargetResolver.TryDeclare(
                            battleCardId,
                            candidate.TargetId,
                            runtime.EnemyPositions,
                            runtime.Enchants,
                            out EnchantFixedTargetDeclaration declaration))
                    {
                        failure =
                            BattleRuntimePlayerCardActionFailure.MissingTarget;
                        return false;
                    }

                    summonTarget = declaration;
                }
            }
            else if (registration.Route == CardEffectRoute.TargetedSkill)
            {
                if (!EffectTargetResolver.TryResolveSingleTarget(
                        runtime,
                        registration.ResolveTargetSpec(
                            card.SourceCard),
                        targetEnemyId,
                        out _))
                {
                    failure =
                        BattleRuntimePlayerCardActionFailure.MissingTarget;
                    return false;
                }
            }
            else if (registration.Route == CardEffectRoute.BanishSkill)
            {
                if (!EffectTargetResolver.TryResolveSingleTarget(
                        runtime,
                        registration.ResolveTargetSpec(
                            card.SourceCard),
                        selectedBanishBattleCardId,
                        card.Ids.BattleCardId,
                        out _))
                {
                    failure = BattleRuntimePlayerCardActionFailure
                        .InvalidBanishSelection;
                    return false;
                }
            }

            failure = BattleRuntimePlayerCardActionFailure.None;
            return true;
        }

        private static CardEffectRegistration FindRegistration(
            BattleCardInstance card)
        {
            CardEffectRegistrationCatalog.TryFind(
                card.SourceCard.CatalogCardId,
                out CardEffectRegistration registration);
            return registration;
        }
    }
}
