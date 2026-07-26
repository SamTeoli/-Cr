using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    public sealed partial class IntegratedRunPrototypeWindow : EditorWindow
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";
        private const string EncounterDatabasePath =
            "Assets/GameData/EncounterDatabase.asset";

        private RunCampaignState campaign;
        private RunEncounterProgressState progress;
        private PlayerPermanentRewardState permanentRewards;
        private CardDatabase cardDatabase;
        private EnchantDatabase enchantDatabase;
        private EncounterDatabase encounterDatabase;
        private RuntimePrototypeConfig prototypeConfig;
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
        private readonly BattlePlayerActionViewModel battleActions = new();
        private RunOwnedCardState runPreparationCards;

        [MenuItem("Have a Break/Play Integrated Prototype")]
        public static void ShowWindow()
        {
            IntegratedRunPrototypeWindow window =
                GetWindow<IntegratedRunPrototypeWindow>("Integrated Run");
            window.minSize = new Vector2(820f, 680f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabases();
            LoadPermanentRewards();
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (!DatabasesReady())
            {
                EditorGUILayout.HelpBox(
                    message ?? "게임 데이터베이스를 불러올 수 없습니다.",
                    MessageType.Error);
                return;
            }

            if (runPreparationCards != null)
            {
                DrawRunPreparation();
                return;
            }

            if (campaign == null || progress == null)
            {
                DrawStartScreen();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
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
                    EditorGUILayout.HelpBox(
                        "보스를 쓰러뜨리고 런을 완료했습니다.",
                        MessageType.Info);
                    break;
                case RunCampaignPhase.Defeated:
                    EditorGUILayout.HelpBox(
                        "플레이어 HP가 0이 되어 런이 종료되었습니다.",
                        MessageType.Error);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("새 런", EditorStyles.toolbarButton,
                    GUILayout.Width(70f)))
            {
                RequestStartNewRun();
            }

            if (GUILayout.Button("이어하기", EditorStyles.toolbarButton,
                    GUILayout.Width(80f)))
            {
                RequestContinueRun();
            }

            using (new EditorGUI.DisabledScope(
                       campaign == null || progress == null))
            {
                if (GUILayout.Button("저장", EditorStyles.toolbarButton,
                        GUILayout.Width(60f)))
                {
                    SaveRun("수동 저장 완료");
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                campaign == null ? "런 없음" : $"단계: {campaign.Phase}",
                EditorStyles.miniLabel,
                GUILayout.Width(160f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStartScreen()
        {
            GUILayout.Space(24f);
            EditorGUILayout.LabelField(
                "Have a Break, and then.. 통합 프로토타입",
                EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "새 런으로 12개 노드 흐름을 시작하거나 저장된 런을 이어서 " +
                "진행할 수 있습니다. 전투·정산·보상·상점·이벤트·회복/강화와 " +
                "소모아이템이 하나의 런스테이지에 연결됩니다.",
                MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("새 런 시작", GUILayout.Height(42f)))
            {
                RequestStartNewRun();
            }

            if (GUILayout.Button("저장된 런 이어하기", GUILayout.Height(42f)))
            {
                RequestContinueRun();
            }

            EditorGUILayout.EndHorizontal();
            DrawMessage();
        }

        private void DrawRunPreparation()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("새 런 덱 준비", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "보유카드 중 이번 런에서 사용할 카드를 선택하세요. " +
                "카드를 선택한 순서가 덱 순서가 됩니다.",
                MessageType.Info);
            EditorGUILayout.LabelField(
                $"선택 {deckSelection.SelectedCount}장",
                EditorStyles.boldLabel);
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(runPreparationCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(40f)))
            {
                CancelRunPreparation();
            }
            if (GUILayout.Button("이 덱으로 런 시작", GUILayout.Height(40f)))
            {
                ConfirmRunPreparation();
            }
            EditorGUILayout.EndHorizontal();
            DrawMessage();
            EditorGUILayout.EndScrollView();
        }

        private void DrawRunSummary()
        {
            RunBattleState run = progress.RunState;
            EditorGUILayout.LabelField(
                $"막 {campaign.GetAct(prototypeConfig.RunStartProgressionConfig)} · " +
                $"완료 노드 {campaign.CompletedNodeCount}/" +
                $"{prototypeConfig.RunStartProgressionConfig.TotalNodeCount}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"HP {run.CurrentHealth}/{run.MaximumHealth}    " +
                $"골드 {run.Gold}    덱 {progress.RunDeck.Count}장    " +
                $"소모아이템 {run.ConsumableItemIds.Count}개");
            if (campaign.ActiveNode != null)
            {
                EditorGUILayout.LabelField(
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
            EditorGUILayout.Space(8f);
        }

        private void DrawDeckEditor()
        {
            if (progress.RunState.RunEnded) return;
            EditorGUILayout.LabelField("런 덱 편집", EditorStyles.miniBoldLabel);
            if (!deckSelection.IsOpen)
            {
                if (GUILayout.Button("덱 편집 열기"))
                {
                    deckSelection.OpenFromDeck(progress.RunDeck);
                }
                return;
            }

            EditorGUILayout.LabelField($"선택 {deckSelection.SelectedCount}장");
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(progress.OwnedCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소")) deckSelection.Close();
            if (GUILayout.Button("선택한 덱 적용")) ApplyDeckEditing();
            EditorGUILayout.EndHorizontal();
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
