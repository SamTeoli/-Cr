using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void DrawShopProducts(
            string sectionLabel,
            IEnumerable<RunShopProductOption> options)
        {
            GUILayout.Label(sectionLabel);
            foreach (RunShopProductOption option in options)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string targetText = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty : $"\n장착 대상: {option.TargetLabel}";
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty : $"\n{option.BlockReason}";
                GUILayout.Label(option.DisplayText + targetText + blockText,
                    wrappedStyle);
                bool previous = GUI.enabled;
                GUI.enabled = option.CanPurchase;
                if (GUILayout.Button(option.PurchaseButtonLabel,
                        GUILayout.Width(80f)))
                {
                    if (shop.TryBuy(campaign, progress, config.EnchantDatabase,
                            config.ShopEconomyConfig, option.SlotId, out _,
                            out string result,
                            out EnchantAttachmentFailure attachmentFailure,
                            out RunCampaignFailure failure))
                    {
                        message = result;
                        SaveRun(null);
                    }
                    else message = option.ProductType == RunShopProductType.Enchant
                        ? $"인첸트 구매 실패: {failure} / {attachmentFailure}"
                        : $"구매 실패: {failure}";
                }
                GUI.enabled = previous;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawBattle()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            if (session?.Runtime == null)
            {
                Notice("활성 전투를 찾을 수 없습니다.");
                if (GUILayout.Button("전투 다시 시작")) BeginSelectedBattle();
                return;
            }

            battleActions.Refresh(context);
            BattleRuntimeState runtime = session.Runtime;
            GUILayout.Label(
                $"{context.Encounter.DisplayName} · 턴 {runtime.Turn.PlayerTurnNumber}",
                headingStyle);
            GUILayout.Label(
                $"HP {runtime.Player.CurrentHealth}/{runtime.Player.MaximumHealth}   " +
                $"마력 {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}   " +
                $"단계 {runtime.Turn.Phase}   결과 {session.Outcome}");
            GUILayout.Label(
                $"드로우 {runtime.Deck.Zones.Count(CardZone.DrawPile)} · " +
                $"묘지 {runtime.Deck.Zones.Count(CardZone.Graveyard)} · " +
                $"소멸 {runtime.Deck.Zones.Count(CardZone.Banished)} · " +
                $"설치 {runtime.Deck.Zones.Count(CardZone.SkillField)}/" +
                $"{BattleCardZoneState.MaximumSkillFieldSize}");
            string playerStatus = DescribeCommonStatus(runtime.Player.Status);
            if (!string.IsNullOrWhiteSpace(playerStatus))
                GUILayout.Label($"플레이어 {playerStatus}", wrappedStyle);
            GUILayout.Label(
                "전투 중 이어하기는 현재 전투의 시작 체크포인트에서 재개됩니다.",
                wrappedStyle);
            DrawBattleConsumables();
            DrawEnemies(context);
            DrawMonsters(context);
            DrawInstalledCards(runtime);
            DrawHand(context);
            DrawRecentEvents(runtime);

            bool previous = GUI.enabled;
            GUI.enabled = !session.IsFinished;
            if (GUILayout.Button("턴 종료", GUILayout.Height(42f)))
            {
                int tieBreaker = campaign.Seed +
                                 context.Session.CompletedRoundCount * 10;
                BattleEndTurnCommandResult command =
                    battleActions.TryEndPlayerTurn(context, tieBreaker);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            GUI.enabled = previous;
            if (session.IsFinished &&
                GUILayout.Button("전투 정산", GUILayout.Height(44f)))
            {
                SettleBattle();
            }
        }

        private void DrawBattleConsumables()
        {
            GUILayout.Label("소모아이템");
            GUILayout.BeginHorizontal();
            foreach (BattleConsumableActionOption option in
                     battleActions.CreateConsumableOptions(progress))
            {
                bool previous = GUI.enabled;
                GUI.enabled = option.CanUse;
                bool clicked = GUILayout.Button(option.DisplayLabel);
                GUI.enabled = previous;
                if (!clicked)
                {
                    continue;
                }

                BattleConsumableCommandResult command =
                    battleActions.TryUseConsumable(progress, option.ItemId);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawInstalledCards(BattleRuntimeState runtime)
        {
            List<BattleCardInstance> installed =
                runtime.Deck.Zones.GetCards(CardZone.SkillField);
            GUILayout.Label($"설치 카드 ({installed.Count})", headingStyle);
            if (installed.Count == 0)
            {
                GUILayout.Label("설치된 스킬·트랩·결계가 없습니다.");
                return;
            }

            GUILayout.BeginHorizontal();
            foreach (BattleCardInstance card in installed)
            {
                bool isRegisteredTrap = runtime.TrapInstallations.Find(
                    card.Ids.BattleCardId) != null;
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(160f));
                GUILayout.Label(
                    $"{card.SourceCard.DisplayName}\n{card.SourceCard.CardType}" +
                    (isRegisteredTrap ? " · 대기 중" : string.Empty),
                    wrappedStyle);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawEnemies(BattleRuntimeEncounterContext context)
        {
            Dictionary<string, string> intents = BuildEnemyIntentLabels(context);
            BattleEnemyTargetOption[] targets =
                battleActions.CreateEnemyTargets(context);
            GUILayout.Label("적 필드");
            GUILayout.BeginHorizontal();
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                BattleEnemyTargetOption option = targets.FirstOrDefault(value =>
                    value.Position == position);
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                if (option?.IsOccupied != true)
                {
                    GUILayout.Label("빈 칸");
                }
                else
                {
                    BattleEnemyRuntimeState enemy = option.Enemy;
                    string selection = option.IsSelected ? "▶ " : string.Empty;
                    string nextIntent = intents.TryGetValue(
                            option.EnemyId,
                            out string intent)
                        ? intent
                        : "없음";
                    GUILayout.Label(
                        $"{selection}{option.DisplayName}\n" +
                        $"HP {enemy.Vital.CurrentHealth}/{option.MaximumHealth} · " +
                        $"공격 {enemy.Attack}\n" +
                        $"다음 행동: {nextIntent}",
                        wrappedStyle);
                    string statusText = DescribeEnemyStatus(option.Status);
                    if (!string.IsNullOrWhiteSpace(statusText))
                    {
                        GUILayout.Label(statusText, wrappedStyle);
                    }

                    bool previous = GUI.enabled;
                    GUI.enabled = option.CanSelect;
                    if (GUILayout.Button(
                            option.IsSelected ? "선택됨" : "대상 선택"))
                    {
                        battleActions.SelectEnemy(context, option.EnemyId);
                    }
                    GUI.enabled = previous;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private Dictionary<string, string> BuildEnemyIntentLabels(
            BattleRuntimeEncounterContext context)
        {
            Dictionary<string, List<string>> actions = new(
                StringComparer.OrdinalIgnoreCase);
            int tieBreaker = campaign.Seed +
                             context.Session.CompletedRoundCount * 10;
            if (!BattleRuntimeEnemyPatternService.TryCreateCommands(
                    context.Session,
                    context.Encounter,
                    tieBreaker,
                    out List<BattleRuntimeEnemyTurnCommand> commands,
                    out _))
            {
                return new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            }

            foreach (BattleRuntimeEnemyTurnCommand command in commands)
            {
                if (command == null ||
                    string.IsNullOrWhiteSpace(command.EnemyId))
                {
                    continue;
                }

                if (!actions.TryGetValue(command.EnemyId, out List<string> labels))
                {
                    labels = new List<string>();
                    actions.Add(command.EnemyId, labels);
                }

                labels.Add(DescribeEnemyCommand(command));
            }

            return actions.ToDictionary(
                pair => pair.Key,
                pair => string.Join(" → ", pair.Value),
                StringComparer.OrdinalIgnoreCase);
        }

    }
}
