using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private RuntimePrototypeConfig config;
        private RunCampaignState campaign;
        private RunEncounterProgressState progress;
        private PlayerPermanentRewardState permanentRewards;
        private string selectedUpgradeCardId;
        private string message;
        private Vector2 scroll;
        private readonly RunDeckSelectionViewModel deckSelection = new();
        private readonly RunNodeSelectionViewModel nodeSelection = new();
        private readonly RunSituationEventViewModel situationEvent = new();
        private readonly RunRestUpgradeViewModel restUpgrade = new();
        private readonly RunShopViewModel shop = new();
        private readonly RunBattleRewardViewModel battleReward = new();
        private readonly RunConsumableViewModel runConsumables = new();
        private readonly BattleScreenViewModel battleScreen = new();
        private readonly RunLifecycleViewModel runLifecycle = new();
        private RunOwnedCardState runPreparationCards;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle wrappedStyle;
        private RunLifecycleRequest pendingRunRequest;

        public void Initialize(RuntimePrototypeConfig value)
        {
            config = value;
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
        }

        private void OnGUI()
        {
            EnsureStyles();
            Rect safe = Screen.safeArea;
            float width = Mathf.Min(1100f, Mathf.Max(1f, safe.width - 24f));
            float height = Mathf.Max(1f, safe.height - 24f);
            Rect panel = new(
                safe.x + (safe.width - width) * 0.5f,
                safe.y + (safe.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(
                panel.x + 12f, panel.y + 10f,
                panel.width - 24f, panel.height - 20f));
            if (pendingRunRequest?.ConfirmationRequired == true)
            {
                DrawRunActionConfirmation();
                GUILayout.EndArea();
                return;
            }

            DrawToolbar();
            GUILayout.Space(8f);

            if (config == null || !config.IsReady)
            {
                GUILayout.Label("게임 데이터베이스를 불러올 수 없습니다.",
                    headingStyle);
                GUILayout.EndArea();
                return;
            }

            if (runPreparationCards != null)
            {
                DrawRunPreparation();
                GUILayout.EndArea();
                return;
            }

            if (campaign == null || progress == null)
            {
                DrawStartScreen();
                GUILayout.EndArea();
                return;
            }

            scroll = GUILayout.BeginScrollView(scroll);
            DrawRunSummary();
            switch (campaign.Phase)
            {
                case RunCampaignPhase.NodeSelection:
                    DrawNodeSelection();
                    break;
                case RunCampaignPhase.NodeResolution:
                    DrawNonBattleNode();
                    break;
                case RunCampaignPhase.Battle:
                    DrawBattle();
                    break;
                case RunCampaignPhase.Reward:
                    DrawRewards();
                    break;
                case RunCampaignPhase.Completed:
                    Notice("보스를 쓰러뜨리고 런을 완료했습니다.");
                    break;
                case RunCampaignPhase.Defeated:
                    Notice("플레이어 HP가 0이 되어 런이 종료되었습니다.");
                    break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headingStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold
            };
            wrappedStyle ??= new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("새 런", GUILayout.Width(90f)))
            {
                RequestStartNewRun();
            }
            if (GUILayout.Button("이어하기", GUILayout.Width(100f)))
            {
                RequestContinueRun();
            }
            bool previous = GUI.enabled;
            GUI.enabled = campaign != null && progress != null;
            if (GUILayout.Button("저장", GUILayout.Width(80f)))
            {
                SaveRun("수동 저장 완료");
            }
            GUI.enabled = previous;
            GUILayout.FlexibleSpace();
            GUILayout.Label(campaign == null ? "런 없음" : campaign.Phase.ToString());
            GUILayout.EndHorizontal();
        }

        private void DrawStartScreen()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Have a Break, and then..", titleStyle);
            GUILayout.Label(
                "12개 노드의 전투·상점·이벤트·회복/강화·보상 흐름을 " +
                "플레이 모드에서 진행합니다.", wrappedStyle);
            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("새 런 시작", GUILayout.Height(52f)))
            {
                RequestStartNewRun();
            }
            if (GUILayout.Button("저장된 런 이어하기", GUILayout.Height(52f)))
            {
                RequestContinueRun();
            }
            GUILayout.EndHorizontal();
            DrawMessage();
            GUILayout.FlexibleSpace();
        }

        private void DrawRunPreparation()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("새 런 덱 준비", titleStyle);
            GUILayout.Label(
                "보유카드 중 이번 런에서 사용할 카드를 선택하세요. " +
                "카드를 선택한 순서가 덱 순서가 됩니다.", wrappedStyle);
            GUILayout.Space(12f);
            GUILayout.Label($"선택 {deckSelection.SelectedCount}장", headingStyle);
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(runPreparationCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(44f)))
            {
                CancelRunPreparation();
            }
            if (GUILayout.Button("이 덱으로 런 시작", GUILayout.Height(44f)))
            {
                ConfirmRunPreparation();
            }
            GUILayout.EndHorizontal();
            DrawMessage();
            GUILayout.EndScrollView();
        }

        private void DrawRunSummary()
        {
            RunBattleState run = progress.RunState;
            GUILayout.Label(
                $"막 {campaign.GetAct(config.RunStartProgressionConfig)} · 완료 " +
                $"{campaign.CompletedNodeCount}/" +
                $"{config.RunStartProgressionConfig.TotalNodeCount}",
                headingStyle);
            GUILayout.Label(
                $"HP {run.CurrentHealth}/{run.MaximumHealth}   골드 {run.Gold}   " +
                $"덱 {progress.RunDeck.Count}장   " +
                $"소모아이템 {run.ConsumableItemIds.Count}개");
            if (campaign.ActiveNode != null)
            {
                GUILayout.Label(
                    $"현재 노드: {campaign.ActiveNode.DisplayName} " +
                    $"({campaign.ActiveNode.NodeId})");
            }
            DrawMessage();
            if (campaign.Phase != RunCampaignPhase.Battle &&
                campaign.Phase != RunCampaignPhase.Reward)
            {
                DrawDeckEditor();
                DrawRunInventory();
            }
            GUILayout.Space(8f);
        }

        private void DrawDeckEditor()
        {
            if (progress.RunState.RunEnded) return;
            GUILayout.Label("런 덱 편집", headingStyle);
            if (!deckSelection.IsOpen)
            {
                if (GUILayout.Button("덱 편집 열기", GUILayout.Width(140f)))
                {
                    deckSelection.OpenFromDeck(progress.RunDeck);
                }
                return;
            }

            GUILayout.Label($"선택 {deckSelection.SelectedCount}장");
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(progress.OwnedCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소")) deckSelection.Close();
            if (GUILayout.Button("선택한 덱 적용")) ApplyDeckEditing();
            GUILayout.EndHorizontal();
        }

        private void ApplyDeckEditing()
        {
            if (deckSelection.TryApply(
                    progress, out RunDeckFailure failure))
            {
                selectedUpgradeCardId =
                    progress.RunDeck.Cards.FirstOrDefault()?.OwnedCardId;
                message = $"런 덱을 {progress.RunDeck.Count}장으로 변경했습니다.";
                SaveRun(null);
                return;
            }
            message = $"런 덱 변경 실패: {failure}";
        }

    }
}
