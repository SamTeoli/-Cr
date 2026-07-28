using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed partial class BattleScreenViewModel
    {
        private const string CheckpointNotice =
            "전투 중 이어하기는 현재 전투의 시작 체크포인트에서 재개됩니다.";
        private readonly BattlePlayerActionViewModel actions = new();

        public void Reset()
        {
            actions.Reset();
        }

        public BattleScreenSnapshot CreateSnapshot(
            RunEncounterProgressState progress,
            RunCampaignState campaign)
        {
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            BattleRuntimeState runtime = session?.Runtime;
            if (runtime == null)
            {
                actions.Reset();
                return new BattleScreenSnapshot(
                    false,
                    "활성 전투를 찾을 수 없습니다.",
                    null,
                    null,
                    null,
                    null,
                    CheckpointNotice,
                    BattleOutcome.Ongoing,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            actions.Refresh(context);
            int tieBreaker = GetTieBreaker(campaign, session);
            Dictionary<string, string> intents =
                BuildEnemyIntentLabels(context, tieBreaker);
            BattleEnemyDisplayOption[] enemies = actions
                .CreateEnemyTargets(context)
                .Select(target => CreateEnemyOption(target, intents))
                .ToArray();
            BattleMonsterDisplayOption[] monsters = actions
                .CreateMonsterAttackOptions(context)
                .Select(CreateMonsterOption)
                .ToArray();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string occupant =
                    runtime.PlayerSkillPositions.GetOccupant(position);
                if (!string.IsNullOrWhiteSpace(occupant) &&
                    runtime.Deck.Zones.Find(occupant)?.Zone !=
                    CardZone.SkillField)
                {
                    runtime.PlayerSkillPositions.TryRemove(occupant);
                }
            }
            IReadOnlyList<BattleCardInstance> installedCards =
                runtime.Deck.Zones.GetCards(CardZone.SkillField);
            foreach (BattleCardInstance card in installedCards)
            {
                if (!runtime.PlayerSkillPositions.FindPosition(
                        card.Ids.BattleCardId).HasValue &&
                    runtime.PlayerSkillPositions.TryGetFirstEmpty(
                        out PlayerMonsterFieldPosition emptyPosition))
                {
                    runtime.PlayerSkillPositions.TryPlace(
                        card.Ids.BattleCardId,
                        emptyPosition);
                }
            }
            BattleInstalledCardDisplayOption[] installed = installedCards
                .Where(card => card != null)
                .Select(card => new BattleInstalledCardDisplayOption(
                    card.Ids.BattleCardId,
                    card.SourceCard.DisplayName,
                    card.SourceCard.CardType,
                    runtime.TrapInstallations.Find(
                        card.Ids.BattleCardId) != null,
                    runtime.PlayerSkillPositions.FindPosition(
                        card.Ids.BattleCardId) ??
                    PlayerMonsterFieldPosition.Left))
                .ToArray();
            IReadOnlyList<BattleEventRecord> eventLog =
                runtime.EventLog.Events;
            BattleEventDisplayOption[] recentEvents = eventLog
                .Skip(Math.Max(0, eventLog.Count - 6))
                .Where(record => record != null)
                .Select(record => new BattleEventDisplayOption(record))
                .ToArray();
            string encounterName = string.IsNullOrWhiteSpace(
                    context.Encounter?.DisplayName)
                ? "전투"
                : context.Encounter.DisplayName;
            string playerStatus = DescribeStatus(runtime.Player.Status);
            return new BattleScreenSnapshot(
                true,
                null,
                $"{encounterName} · 턴 {runtime.Turn.PlayerTurnNumber}",
                $"HP {runtime.Player.CurrentHealth}/" +
                $"{runtime.Player.MaximumHealth}   " +
                $"마력 {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}   " +
                $"단계 {runtime.Turn.Phase}   결과 {session.Outcome}",
                $"드로우 {runtime.Deck.Zones.Count(CardZone.DrawPile)} · " +
                $"묘지 {runtime.Deck.Zones.Count(CardZone.Graveyard)} · " +
                $"소멸 {runtime.Deck.Zones.Count(CardZone.Banished)} · " +
                $"설치 {runtime.Deck.Zones.Count(CardZone.SkillField)}/" +
                $"{BattleCardZoneState.MaximumSkillFieldSize}",
                string.IsNullOrWhiteSpace(playerStatus)
                    ? null
                    : $"플레이어 {playerStatus}",
                CheckpointNotice,
                session.Outcome,
                session.IsFinished,
                actions.CreateConsumableOptions(progress),
                enemies,
                monsters,
                installed,
                actions.CreateHandOptions(context),
                recentEvents,
                actions.IsSelectingEnemyTarget,
                actions.PendingTargetedCardId ??
                actions.PendingAttackerId,
                new BattleChainDisplayOption(
                    runtime.Chain.Phase,
                    runtime.Chain.NextParticipant,
                    runtime.Chain.Links.ToArray()));
        }

        public bool SelectEnemy(
            RunEncounterProgressState progress,
            string enemyId)
        {
            return actions.SelectEnemy(
                progress?.ActiveEncounter,
                enemyId);
        }

        public bool TryBeginCardTargeting(
            RunEncounterProgressState progress,
            string battleCardId,
            out string message)
        {
            return actions.TryBeginCardTargeting(
                progress?.ActiveEncounter,
                battleCardId,
                out message);
        }

        public bool TryBeginAttackTargeting(
            RunEncounterProgressState progress,
            string battleCardId,
            out string message)
        {
            return actions.TryBeginAttackTargeting(
                progress?.ActiveEncounter,
                battleCardId,
                out message);
        }

        public bool TryDeclareTargetedCardActivation(
            RunEncounterProgressState progress,
            string battleCardId,
            out string message)
        {
            return actions.TryDeclareTargetedCardActivation(
                progress?.ActiveEncounter,
                battleCardId,
                out message);
        }

        public string PendingTargetedCardId =>
            actions.PendingTargetedCardId;

        public string PendingAttackerId => actions.PendingAttackerId;
        public string SelectedEnemyId => actions.SelectedEnemyId;

        public void ClearPendingTargeting()
        {
            actions.ClearPendingTargeting();
        }

        public BattleBanishTargetOption CycleBanishTarget(
            RunEncounterProgressState progress,
            string sourceBattleCardId)
        {
            return actions.CycleBanishTarget(
                progress?.ActiveEncounter,
                sourceBattleCardId);
        }

        public bool SelectBanishTarget(
            RunEncounterProgressState progress,
            string sourceBattleCardId,
            string targetBattleCardId)
        {
            return actions.SelectBanishTarget(
                progress?.ActiveEncounter,
                sourceBattleCardId,
                targetBattleCardId);
        }

        public BattleConsumableCommandResult TryUseConsumable(
            RunEncounterProgressState progress,
            string itemId)
        {
            return actions.TryUseConsumable(progress, itemId);
        }

        public BattleCardPlayCommandResult TryPlayCard(
            RunEncounterProgressState progress,
            string battleCardId)
        {
            return actions.TryPlayCard(
                progress?.ActiveEncounter,
                battleCardId);
        }

        public BattleCardPlayCommandResult TryPlayCard(
    RunEncounterProgressState progress,
    string battleCardId,
    PlayerMonsterFieldPosition position)
{
    return actions.TryPlayCard(
        progress?.ActiveEncounter,
        battleCardId,
        position);
}

        public BattleMonsterAttackCommandResult TryAttack(
            RunEncounterProgressState progress,
            string battleCardId)
        {
            return actions.TryAttack(
                progress?.ActiveEncounter,
                battleCardId);
        }

        public BattleChainCommandResult TryPassAndResolveChain(
            RunEncounterProgressState progress)
        {
            return actions.TryPassAndResolveChain(
                progress?.ActiveEncounter);
        }

        public BattleEndTurnCommandResult TryEndPlayerTurn(
            RunEncounterProgressState progress,
            RunCampaignState campaign)
        {
            BattleRuntimeSessionState session =
                progress?.ActiveEncounter?.Session;
            int tieBreaker = GetTieBreaker(campaign, session);
            return actions.TryEndPlayerTurn(
                progress?.ActiveEncounter,
                tieBreaker);
        }
    }
}
