using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    public sealed class IntegratedRunPrototypeWindow : EditorWindow
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
        private string selectedEnemyId;
        private string selectedBanishCardId;
        private string selectedUpgradeCardId;
        private string message;
        private Vector2 scroll;

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
                DrawRunInventory();
            }
            EditorGUILayout.Space(8f);
        }

        private void DrawRunInventory()
        {
            EditorGUILayout.LabelField("런 소모아이템", EditorStyles.miniBoldLabel);
            if (progress.RunState.ConsumableItemIds.Count == 0)
            {
                EditorGUILayout.LabelField("보유 아이템 없음");
                return;
            }

            EditorGUILayout.LabelField(string.Join(", ",
                progress.RunState.ConsumableItemIds.Select(itemId =>
                    PrototypeConsumableCatalog.Find(itemId)?.DisplayName ?? itemId)));
            if (!progress.RunState.ConsumableItemIds.Any(itemId =>
                    string.Equals(itemId,
                        PrototypeConsumableCatalog.EnchantHammer,
                        StringComparison.OrdinalIgnoreCase)) ||
                progress.RunDeck.Count == 0)
            {
                return;
            }

            string[] labels = progress.RunDeck.Cards.Select(card =>
                $"{card.Card.DisplayName} · 슬롯 {card.Enchants.SlotCount}/" +
                $"{RunCardEnchantState.MaximumSlotCount}").ToArray();
            int selected = Mathf.Max(0,
                progress.RunDeck.Cards.ToList().FindIndex(card =>
                    string.Equals(card.OwnedCardId, selectedUpgradeCardId,
                        StringComparison.OrdinalIgnoreCase)));
            selected = EditorGUILayout.Popup(
                "인첸트 망치 대상", selected, labels);
            selectedUpgradeCardId =
                progress.RunDeck.Cards[selected].OwnedCardId;
            if (GUILayout.Button("인첸트 망치 사용 · 슬롯 +1"))
            {
                if (PrototypeConsumableService.TryUseEnchantHammer(
                        progress, selectedUpgradeCardId,
                        out PrototypeConsumableFailure failure))
                {
                    message = "인첸트 슬롯을 1칸 늘렸습니다.";
                    SaveRun(null);
                }
                else
                {
                    message = $"인첸트 망치 사용 실패: {failure}";
                }
            }
        }

        private void DrawNodeSelection()
        {
            EditorGUILayout.LabelField("다음 노드 선택", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (RunNodeChoice choice in
                     RunCampaignService.GetChoices(campaign))
            {
                if (GUILayout.Button(
                        $"{choice.DisplayName}\n{choice.NodeId}",
                        GUILayout.MinHeight(62f)))
                {
                    if (!RunCampaignService.TrySelectNode(
                            campaign, choice.NodeId, out RunCampaignFailure failure))
                    {
                        message = $"노드 선택 실패: {failure}";
                    }
                    else if (choice.IsBattle)
                    {
                        BeginSelectedBattle();
                    }
                    else
                    {
                        message = $"{choice.DisplayName} 노드에 들어왔습니다.";
                        SaveRun(null);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNonBattleNode()
        {
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                EditorGUILayout.HelpBox("현재 노드가 없습니다.", MessageType.Error);
                return;
            }

            switch (node.NodeType)
            {
                case RunNodeType.Shop:
                    DrawShop();
                    break;
                case RunNodeType.SituationEvent:
                    DrawSituationEvent();
                    break;
                case RunNodeType.RestOrUpgrade:
                    DrawRestOrUpgrade();
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        $"지원하지 않는 비전투 노드: {node.NodeType}",
                        MessageType.Error);
                    break;
            }
        }

        private void DrawSituationEvent()
        {
            EditorGUILayout.LabelField("상황 이벤트", EditorStyles.boldLabel);
            foreach (RunSituationEventChoice choice in
                     RunCampaignService.GetSituationEventChoices(campaign))
            {
                if (GUILayout.Button(choice.DisplayText, GUILayout.Height(36f)))
                {
                    if (RunCampaignService.TryResolveSituationEvent(
                            campaign, progress.RunState, choice.ChoiceId,
                            out string result, out RunCampaignFailure failure))
                    {
                        message = result;
                        SaveRun(null);
                    }
                    else message = $"이벤트 처리 실패: {failure}";
                }
            }
        }

        private void DrawRestOrUpgrade()
        {
            EditorGUILayout.LabelField("회복 · 강화", EditorStyles.boldLabel);
            RestUpgradeConfig rules = prototypeConfig.RestUpgradeConfig;
            if (GUILayout.Button(
                    $"최대 HP의 {rules.HealingRatio * 100f:0.#}% 회복",
                    GUILayout.Height(34f)))
            {
                if (RunCampaignService.TryRest(
                        campaign, progress.RunState, rules,
                        out int healed, out RunCampaignFailure failure))
                {
                    message = $"HP를 {healed} 회복했습니다.";
                    SaveRun(null);
                }
                else
                {
                    message = $"회복 실패: {failure}";
                }
            }

            string[] cardLabels = progress.RunDeck.Cards.Select(card =>
                $"{card.Card.DisplayName} · 레벨 {card.CurrentLevel}").ToArray();
            int selected = Mathf.Max(0,
                progress.RunDeck.Cards.ToList().FindIndex(card =>
                    string.Equals(card.OwnedCardId, selectedUpgradeCardId,
                        StringComparison.OrdinalIgnoreCase)));
            selected = EditorGUILayout.Popup("강화할 카드", selected, cardLabels);
            if (progress.RunDeck.Count > 0)
            {
                selectedUpgradeCardId =
                    progress.RunDeck.Cards[selected].OwnedCardId;
            }

            if (GUILayout.Button(
                    $"선택 카드 {rules.UpgradeLevelIncrease}레벨 강화",
                    GUILayout.Height(34f)))
            {
                if (RunCampaignService.TryUpgrade(
                        campaign, progress, selectedUpgradeCardId, rules,
                        out RunCampaignFailure failure))
                {
                    message = $"카드를 {rules.UpgradeLevelIncrease}레벨 강화했습니다.";
                    SaveRun(null);
                }
                else
                {
                    message = $"강화 실패: {failure}";
                }
            }
        }

        private void DrawShop()
        {
            EditorGUILayout.LabelField("상점", EditorStyles.boldLabel);
            IReadOnlyList<RunShopProductSlot> offers =
                RunCampaignService.GetShopSlots(campaign,
                    PrototypeConsumableCatalog.All, enchantDatabase.Enchants,
                    prototypeConfig.ShopEconomyConfig);
            EditorGUILayout.LabelField("소모아이템", EditorStyles.miniBoldLabel);
            foreach (RunShopProductSlot offer in offers.Where(value =>
                         value.ProductType == RunShopProductType.Consumable))
            {
                ConsumableData item = PrototypeConsumableCatalog.Find(offer.ContentId);
                if (item == null) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    $"{item.DisplayName} · {item.RulesText}",
                    EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(offer.Purchased))
                {
                    if (GUILayout.Button(offer.Purchased ? "판매 완료" : $"{offer.Price}G",
                            GUILayout.Width(70f)))
                    {
                        if (RunCampaignService.TryBuyConsumableSlot(
                                campaign, progress.RunState, offer.SlotId,
                                out RunCampaignFailure failure))
                        {
                            message = $"{item.DisplayName} 구매 완료.";
                            SaveRun(null);
                        }
                        else message = $"구매 실패: {failure}";
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("인첸트", EditorStyles.miniBoldLabel);
            foreach (RunShopProductSlot offer in offers.Where(value =>
                         value.ProductType == RunShopProductType.Enchant))
            {
                DrawShopEnchant(enchantDatabase.Find(offer.ContentId), offer);
            }

            EditorGUILayout.BeginHorizontal();
            ShopEconomyConfig economy = prototypeConfig.ShopEconomyConfig;
            int rerollCost = RunCampaignService.GetShopRerollCost(campaign, economy);
            if (GUILayout.Button($"전체 리롤 · {rerollCost}G"))
            {
                if (RunCampaignService.TryRerollShop(
                        campaign, progress.RunState, economy,
                        out RunCampaignFailure failure))
                {
                    message = "상점 상품을 다시 생성했습니다.";
                    SaveRun(null);
                }
                else
                {
                    message = $"리롤 실패: {failure}";
                }
            }

            if (GUILayout.Button("상점 나가기"))
            {
                if (RunCampaignService.TryLeaveShop(
                        campaign, progress.RunState,
                        out RunCampaignFailure failure))
                {
                    message = "상점을 나왔습니다.";
                    SaveRun(null);
                }
                else
                {
                    message = $"상점 종료 실패: {failure}";
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawShopEnchant(EnchantData enchant, RunShopProductSlot offer)
        {
            if (enchant == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"{enchant.DisplayName} [{enchant.Rarity}] · {enchant.RulesText}",
                EditorStyles.wordWrappedLabel);
            bool canAttach = TryFindEnchantTarget(enchant,
                out RunCardInstance target, out int slot);
            using (new EditorGUI.DisabledScope(offer.Purchased || !canAttach))
            {
                if (GUILayout.Button(offer.Purchased ? "판매 완료" : $"{offer.Price}G",
                        GUILayout.Width(70f)))
                {
                    if (RunCampaignService.TryBuyEnchantSlot(
                            campaign, progress, enchant,
                            offer.SlotId, target.OwnedCardId, slot,
                            out EnchantAttachmentFailure attachmentFailure,
                            out RunCampaignFailure failure))
                    {
                        message =
                            $"{target.Card.DisplayName}에 {enchant.DisplayName} 장착.";
                        SaveRun(null);
                    }
                    else
                    {
                        message =
                            $"인첸트 구매 실패: {failure} / {attachmentFailure}";
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattle()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            if (session?.Runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "활성 전투를 찾을 수 없습니다.", MessageType.Error);
                if (GUILayout.Button("전투 다시 시작"))
                {
                    BeginSelectedBattle();
                }
                return;
            }

            BattleRuntimeState runtime = session.Runtime;
            EditorGUILayout.LabelField(
                $"{context.Encounter.DisplayName} · 턴 {runtime.Turn.PlayerTurnNumber}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"HP {runtime.Player.CurrentHealth}/{runtime.Player.MaximumHealth}    " +
                $"마력 {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}    결과 {session.Outcome}");
            DrawBattleConsumables(context);
            DrawBattleEnemies(runtime);
            DrawBattleMonsters(runtime);
            DrawBattleHand(context);

            using (new EditorGUI.DisabledScope(session.IsFinished))
            {
                if (GUILayout.Button("턴 종료", GUILayout.Height(36f)))
                {
                    EndPlayerTurn(context);
                }
            }

            if (session.IsFinished)
            {
                EditorGUILayout.HelpBox(
                    $"전투 종료: {session.Outcome}. 정산을 진행하세요.",
                    session.Outcome == BattleOutcome.Victory
                        ? MessageType.Info
                        : MessageType.Error);
                if (GUILayout.Button("전투 정산", GUILayout.Height(38f)))
                {
                    SettleBattle();
                }
            }
        }

        private void DrawBattleConsumables(BattleRuntimeEncounterContext context)
        {
            EditorGUILayout.LabelField("소모아이템", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (string itemId in progress.RunState.ConsumableItemIds.Distinct())
            {
                ConsumableData item =
                    PrototypeConsumableCatalog.Find(itemId);
                if (item == null ||
                    item.Effect == ConsumableEffect.IncreaseEnchantSlot)
                {
                    continue;
                }

                if (GUILayout.Button(item.DisplayName))
                {
                    if (PrototypeConsumableService.TryUseInBattle(
                            context, item.ItemId, out int applied,
                            out PrototypeConsumableFailure failure))
                    {
                        message = $"{item.DisplayName} 사용 · 적용량 {applied}";
                        SaveRun(null);
                    }
                    else
                    {
                        message = $"아이템 사용 실패: {failure}";
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattleEnemies(BattleRuntimeState runtime)
        {
            EditorGUILayout.LabelField("적 필드", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string enemyId = runtime.EnemyPositions.GetOccupant(position);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    EditorGUILayout.LabelField("빈 칸");
                }
                else
                {
                    BattleEnemyRuntimeState enemy = runtime.FindEnemy(enemyId);
                    BattleEnemyStatusState status = runtime.EnemyStatuses.Find(enemyId);
                    EditorGUILayout.LabelField(
                        $"{enemyId}\nHP {enemy.Vital.CurrentHealth} · 공격 {enemy.Attack}\n" +
                        $"부상 {status?.Injury ?? 0} 약화 {status?.Weaken ?? 0} " +
                        $"취약 {status?.Vulnerable ?? 0} 속박 {status?.Bind ?? 0} " +
                        $"기절 {status?.Stun ?? 0}",
                        EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button(
                            string.Equals(selectedEnemyId, enemyId,
                                StringComparison.OrdinalIgnoreCase)
                                ? "선택됨"
                                : "대상 선택"))
                    {
                        selectedEnemyId = enemyId;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattleMonsters(BattleRuntimeState runtime)
        {
            EditorGUILayout.LabelField("아군 몬스터", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string cardId = runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster = string.IsNullOrWhiteSpace(cardId)
                    ? null
                    : runtime.Monsters.Find(cardId);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (monster == null)
                {
                    EditorGUILayout.LabelField("빈 칸");
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"{monster.Card.SourceCard.DisplayName}\n" +
                        $"공격 {monster.Attack} · HP {monster.CurrentHealth}/" +
                        $"{monster.MaximumHealth}");
                    using (new EditorGUI.DisabledScope(
                               string.IsNullOrWhiteSpace(selectedEnemyId)))
                    {
                        if (GUILayout.Button("선택한 적 공격"))
                        {
                            ResolvePlayerAttack(monster.BattleCardId);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattleHand(BattleRuntimeEncounterContext context)
        {
            List<BattleCardInstance> hand =
                context.Runtime.Deck.Zones.GetCards(CardZone.Hand);
            EditorGUILayout.LabelField($"패 ({hand.Count})", EditorStyles.miniBoldLabel);
            foreach (BattleCardInstance card in hand)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    $"{card.SourceCard.CatalogCardId} {card.SourceCard.DisplayName} " +
                    $"· 비용 {card.Resolved.ManaCost}\n{card.Resolved.RulesText}",
                    EditorStyles.wordWrappedLabel);
                if (string.Equals(card.SourceCard.CatalogCardId,
                        TestContentIds.C07, StringComparison.OrdinalIgnoreCase))
                {
                    List<BattleCardInstance> candidates = hand
                        .Where(other => other != card).ToList();
                    if (candidates.Count > 0)
                    {
                        int selected = Mathf.Max(0, candidates.FindIndex(other =>
                            string.Equals(other.Ids.BattleCardId,
                                selectedBanishCardId,
                                StringComparison.OrdinalIgnoreCase)));
                        selected = EditorGUILayout.Popup(selected,
                            candidates.Select(other => other.SourceCard.DisplayName)
                                .ToArray(), GUILayout.Width(130f));
                        selectedBanishCardId =
                            candidates[selected].Ids.BattleCardId;
                    }
                }

                if (GUILayout.Button("사용", GUILayout.Width(65f)))
                {
                    ResolveCardPlay(context, card.Ids.BattleCardId);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRewards()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (context == null || !context.Settlement.IsSettled)
            {
                EditorGUILayout.HelpBox("정산된 전투가 없습니다.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("전투 보상", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                context.VictoryRewards.GoldClaimed
                    ? $"골드 {context.VictoryRewards.GoldReward} 수령 완료"
                    : $"골드 {context.VictoryRewards.GoldReward} 대기 중");

            if (context.VictoryRewards.EnchantChoiceCount > 0)
            {
                EnsureEnchantRewards(context);
                BattleVictoryEnchantRewardService rewards =
                    context.VictoryEnchantRewards;
                if (rewards != null && !rewards.Claimed)
                {
                    foreach (EnchantData enchant in rewards.OfferedChoices)
                    {
                        if (GUILayout.Button(
                                $"{enchant.DisplayName} [{enchant.Rarity}]\n" +
                                enchant.RulesText,
                                GUILayout.MinHeight(42f)))
                        {
                            ClaimEnchantReward(rewards, enchant);
                        }
                    }
                }
                else if (rewards?.Claimed == true)
                {
                    EditorGUILayout.HelpBox(
                        $"{rewards.ClaimedEnchant.DisplayName} 선택 완료",
                        MessageType.Info);
                }
            }

            if (context.VictoryRewards.ConsumableItemRewardCount > 0)
            {
                EnsureConsumableRewards(context);
                if (context.VictoryConsumableRewards != null &&
                    !context.VictoryConsumableRewards.Claimed)
                {
                    foreach (ConsumableData item in
                             PrototypeConsumableCatalog.All.Take(3))
                    {
                        if (GUILayout.Button($"{item.DisplayName} · {item.RulesText}"))
                        {
                            if (context.VictoryConsumableRewards.TryClaim(
                                    item.ItemId,
                                    out BattleVictoryConsumableRewardFailure failure))
                            {
                                message = $"{item.DisplayName} 보상 수령 완료.";
                            }
                            else
                            {
                                message = $"소모아이템 보상 실패: {failure}";
                            }
                        }
                    }
                }
            }

            if (GUILayout.Button("보상 완료 · 다음 노드", GUILayout.Height(40f)))
            {
                CompleteRewards();
            }
        }

        private void RequestStartNewRun()
        {
            if (!DatabasesReady())
            {
                LoadDatabases();
                return;
            }

            bool hasCurrentRun = campaign != null || progress != null;
            bool inspected = RunSaveSlotService.TryInspectDefault(
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunSaveSlotInfo slot,
                out _);
            RunSaveSlotState slotState = slot?.State ?? RunSaveSlotState.Empty;
            if (!RunActionConfirmationPolicy.ShouldConfirmNewRun(
                    hasCurrentRun, inspected, slotState) ||
                EditorUtility.DisplayDialog(
                    "새 런을 시작할까요?",
                    "현재 진행과 저장된 런이 새 런으로 교체됩니다. " +
                    "이 작업은 되돌릴 수 없습니다.",
                    "새 런 시작",
                    "취소"))
            {
                StartNewRun();
            }
        }

        private void RequestContinueRun()
        {
            bool hasCurrentRun = campaign != null && progress != null;
            RunCampaignPhase phase = campaign?.Phase ??
                                     RunCampaignPhase.NodeSelection;
            if (!RunActionConfirmationPolicy.ShouldConfirmContinue(
                    hasCurrentRun, phase) ||
                EditorUtility.DisplayDialog(
                    "전투를 처음부터 다시 시작할까요?",
                    "이어하기를 선택하면 현재 전투 진행을 버리고 " +
                    "전투 시작 체크포인트에서 다시 시작합니다.",
                    "전투 다시 시작",
                    "취소"))
            {
                ContinueRun();
            }
        }

        private void StartNewRun()
        {
            if (!DatabasesReady())
            {
                LoadDatabases();
                return;
            }

            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in cardDatabase.Cards.Where(card => card != null))
            {
                deck.TryAdd(new RunCardInstance(
                        card, $"OWNED-RUN-{++index:00}-{card.CatalogCardId}", 1),
                    out _);
            }

            RunBattleState run =
                prototypeConfig.RunStartProgressionConfig.CreateInitialRunState();
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(
                run, deck, permanentRewards);
            campaign = new RunCampaignState(20260722);
            selectedEnemyId = null;
            selectedBanishCardId = null;
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            message = "새 통합 런을 시작했습니다.";
            SaveRun(null);
        }

        private void ContinueRun()
        {
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    cardDatabase, enchantDatabase, encounterDatabase,
                    permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                message = $"이어하기 실패: {failure}";
                return;
            }

            selectedUpgradeCardId =
                progress.RunDeck.Cards.FirstOrDefault()?.OwnedCardId;
            SelectFirstEnemy();
            message = $"이어하기 완료: {source}";
        }

        private void BeginSelectedBattle()
        {
            BattleEncounterGrade grade = campaign.ActiveNode.NodeType switch
            {
                RunNodeType.EliteBattle => BattleEncounterGrade.Elite,
                RunNodeType.MidBoss => BattleEncounterGrade.MidBoss,
                RunNodeType.FinalBoss => BattleEncounterGrade.FinalBoss,
                _ => BattleEncounterGrade.Normal
            };
            int selectionSeed = campaign.Seed +
                                campaign.CompletedNodeCount * 1009;
            if (!RunEncounterPoolService.TryResolve(
                    encounterDatabase, prototypeConfig.GetEncounterPool(
                        grade, campaign.CompletedNodeCount),
                    grade, selectionSeed, out EncounterData encounter,
                    out string poolError))
            {
                message = $"조우 선택 실패: {poolError}";
                return;
            }

            string battleId =
                $"RUN-{campaign.Seed}-NODE-{campaign.CompletedNodeCount + 1:00}";
            int seed = campaign.Seed + campaign.CompletedNodeCount * 101;
            if (!RunEncounterProgressService.TryBegin(
                    progress, battleId, encounter, seed,
                    prototypeConfig.RunStartProgressionConfig.BattleMaximumMana,
                    Array.Empty<string>(), (uint)Mathf.Abs(seed),
                    prototypeConfig.BattleRewardConfig,
                    out _,
                    out RunEncounterProgressFailure failure,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out RunDeckFailure deckFailure,
                    out BattleRuntimeBootstrapFailure bootstrapFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure turnFailure,
                    out List<string> validationErrors))
            {
                message =
                    $"전투 시작 실패: {failure} / {flowFailure} / {deckFailure} / " +
                    $"{bootstrapFailure} / {sessionFailure} / {redrawFailure} / " +
                    $"{turnFailure}" +
                    (validationErrors.Count == 0
                        ? string.Empty
                        : $"\n{string.Join("\n", validationErrors)}");
                return;
            }

            selectedBanishCardId = null;
            SelectFirstEnemy();
            message = $"{campaign.ActiveNode.DisplayName} 전투 시작.";
            SaveRun(null);
        }

        private void ResolveCardPlay(
            BattleRuntimeEncounterContext context,
            string battleCardId)
        {
            if (!BattleRuntimePlayerCardActionService.TryResolve(
                    context.Runtime, battleCardId, selectedEnemyId,
                    selectedBanishCardId,
                    out BattleRuntimePlayerCardActionResult result,
                    out BattleRuntimePlayerCardActionFailure failure,
                    out BattleRuntimeCardPlayFailure playFailure,
                    out CardPlayFailure cardFailure))
            {
                message =
                    $"카드 사용 실패: {failure} / {playFailure} / {cardFailure}";
                return;
            }

            message = $"{result.Play.Card.SourceCard.DisplayName} 사용 완료.";
            selectedBanishCardId = null;
            FinalizeOutcome();
            SelectFirstEnemy();
        }

        private void ResolvePlayerAttack(string battleCardId)
        {
            if (!BattleRuntimePlayerAttackService.TryResolve(
                    progress.ActiveEncounter.Runtime,
                    battleCardId, selectedEnemyId,
                    out BattleRuntimePlayerAttackResult result,
                    out BattleRuntimePlayerAttackFailure failure))
            {
                message = $"공격 실패: {failure}";
                return;
            }

            message = $"공격 완료 · 피해 {result.DamageApplied}";
            FinalizeOutcome();
            SelectFirstEnemy();
        }

        private void EndPlayerTurn(BattleRuntimeEncounterContext context)
        {
            int tieBreaker = campaign.Seed +
                             context.Session.CompletedRoundCount * 10;
            if (!BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                    context.Session, context.Encounter, tieBreaker,
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeEnemyPatternFailure patternFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure turnFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int actionIndex))
            {
                message =
                    $"턴 종료 실패: {patternFailure} / {sessionFailure} / " +
                    $"{roundFailure} / {turnFailure} / {pipelineFailure} / " +
                    $"{planFailure} / {enemyTurnFailure} / action {actionIndex}";
                return;
            }

            message = result.Outcome == BattleOutcome.Ongoing
                ? $"적 턴 완료 · 플레이어 턴 " +
                  $"{context.Runtime.Turn.PlayerTurnNumber}"
                : $"전투 종료 · {result.Outcome}";
            selectedBanishCardId = null;
            SelectFirstEnemy();
            SaveRun(null);
        }

        private void FinalizeOutcome()
        {
            BattleRuntimeSessionState session = progress.ActiveEncounter.Session;
            if (!session.IsFinished &&
                BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    session, out BattleOutcome outcome, out _))
            {
                message += $" 전투 종료 · {outcome}";
            }
        }

        private void SettleBattle()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (!RunEncounterProgressService.TrySettleActive(
                    progress,
                    out RunEncounterProgressFailure progressFailure,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleSettlementFailure settlementFailure))
            {
                message =
                    $"정산 실패: {progressFailure} / {flowFailure} / " +
                    $"{sessionFailure} / {settlementFailure}";
                return;
            }

            if (context.Settlement.SettledOutcome == BattleOutcome.Defeat)
            {
                RunEncounterProgressService.TryCompleteActive(progress, out _);
                RunCampaignService.MarkBattleReward(
                    campaign, BattleOutcome.Defeat);
                message = "패배 정산 완료 · 런 종료";
                SaveRun(null);
                return;
            }

            if (!context.VictoryRewards.TryClaimGold(
                    out BattleRewardFailure rewardFailure))
            {
                message = $"골드 보상 실패: {rewardFailure}";
                return;
            }

            if (context.VictoryRewards.GrantsFinalBossPermanentReward)
            {
                if (!BattleVictoryPermanentRewardService.TryCreate(
                        progress,
                        out BattleVictoryPermanentRewardService permanent,
                        out BattleVictoryPermanentRewardFailure createFailure))
                {
                    message = $"영구 보상 생성 실패: {createFailure}";
                    return;
                }

                if (!permanent.TryClaim(
                        "PERMANENT-FIRST-RUN-CLEAR",
                        out BattleVictoryPermanentRewardFailure claimFailure))
                {
                    message = $"영구 보상 수령 실패: {claimFailure}";
                    return;
                }
            }

            RunCampaignService.MarkBattleReward(
                campaign, BattleOutcome.Victory);
            message =
                $"승리 정산 완료 · 골드 {context.VictoryRewards.GoldReward} 획득";
        }

        private void EnsureEnchantRewards(BattleRuntimeEncounterContext context)
        {
            if (context.VictoryEnchantRewards != null)
            {
                return;
            }

            List<EnchantData> choices = enchantDatabase.Enchants
                .Where(enchant => enchant != null &&
                    TryFindEnchantTarget(enchant, out _, out _))
                .OrderByDescending(enchant =>
                    (int)enchant.Rarity >=
                    (int)context.VictoryRewards.MinimumGuaranteedEnchantRarity)
                .ThenBy(enchant => enchant.DefinitionId)
                .Take(context.VictoryRewards.EnchantChoiceCount)
                .ToList();
            if (!BattleVictoryEnchantRewardService.TryCreate(
                    context, progress.RunDeck, choices,
                    out _, out BattleVictoryEnchantRewardFailure failure))
            {
                message = $"인첸트 보상 생성 실패: {failure}";
            }
        }

        private void ClaimEnchantReward(
            BattleVictoryEnchantRewardService rewards,
            EnchantData enchant)
        {
            if (!TryFindEnchantTarget(enchant,
                    out RunCardInstance target, out int slot))
            {
                message = "장착 가능한 카드가 없습니다.";
                return;
            }

            if (rewards.TryClaim(
                    enchant.DefinitionId, target.OwnedCardId, slot,
                    out EnchantAttachmentFailure attachmentFailure,
                    out BattleVictoryEnchantRewardFailure failure))
            {
                message =
                    $"{target.Card.DisplayName}에 {enchant.DisplayName} 장착.";
            }
            else
            {
                message = $"보상 선택 실패: {failure} / {attachmentFailure}";
            }
        }

        private void EnsureConsumableRewards(
            BattleRuntimeEncounterContext context)
        {
            if (context.VictoryConsumableRewards == null &&
                !BattleVictoryConsumableRewardService.TryCreate(
                    context, out _,
                    out BattleVictoryConsumableRewardFailure failure))
            {
                message = $"소모아이템 보상 생성 실패: {failure}";
            }
        }

        private void CompleteRewards()
        {
            if (!RunEncounterProgressService.TryCompleteActive(
                    progress, out RunEncounterProgressFailure failure))
            {
                message = $"보상 미완료: {failure}";
                return;
            }

            RunCampaignService.CompleteBattleReward(campaign);
            message = "보상 완료 · 다음 노드를 선택하세요.";
            SaveRun(null);
        }

        private bool TryFindEnchantTarget(
            EnchantData enchant,
            out RunCardInstance target,
            out int slotIndex)
        {
            target = null;
            slotIndex = -1;
            if (enchant == null || progress?.RunDeck == null)
            {
                return false;
            }

            foreach (RunCardInstance card in progress.RunDeck.Cards)
            {
                for (int i = 0; i < card.Enchants.SlotCount; i++)
                {
                    if (card.Enchants.CanAttach(enchant, i, out _))
                    {
                        target = card;
                        slotIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        private void SelectFirstEnemy()
        {
            BattleRuntimeState runtime = progress?.ActiveEncounter?.Runtime;
            if (runtime == null)
            {
                selectedEnemyId = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedEnemyId) &&
                runtime.LivingEnemies.Contains(selectedEnemyId))
            {
                return;
            }

            selectedEnemyId = runtime.Enemies.FirstOrDefault(enemy =>
                enemy != null && enemy.IsAlive)?.EnemyId;
        }

        private void SaveRun(string successMessage)
        {
            if (campaign == null || progress == null)
            {
                return;
            }

            if (IntegratedRunSaveService.TrySave(
                    campaign, progress, out RunSaveDestination destination,
                    out RunCampaignFailure failure))
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = $"{successMessage} · {destination}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(successMessage))
            {
                message = $"저장 실패: {failure}";
            }
        }

        private void LoadDatabases()
        {
            cardDatabase =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(CardDatabasePath);
            enchantDatabase =
                AssetDatabase.LoadAssetAtPath<EnchantDatabase>(EnchantDatabasePath);
            encounterDatabase =
                AssetDatabase.LoadAssetAtPath<EncounterDatabase>(EncounterDatabasePath);
            prototypeConfig = Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig");
            if (!DatabasesReady())
            {
                message = "Card/Enchant/Encounter 데이터베이스를 확인하세요.";
            }
        }

        private void LoadPermanentRewards()
        {
            if (PlayerPermanentRewardSaveService.TryLoadDefault(
                    out PlayerPermanentRewardState loaded,
                    out _, out _))
            {
                permanentRewards = loaded;
            }
            else
            {
                permanentRewards ??= new PlayerPermanentRewardState();
            }
        }

        private bool DatabasesReady()
        {
            return cardDatabase != null && enchantDatabase != null &&
                   encounterDatabase != null && prototypeConfig != null &&
                   prototypeConfig.IsReady;
        }

        private void DrawMessage()
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        private static IEnumerable<T> Rotate<T>(
            IReadOnlyList<T> values,
            int seed)
        {
            if (values == null || values.Count == 0)
            {
                yield break;
            }

            int start = Mathf.Abs(seed % values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                yield return values[(start + i) % values.Count];
            }
        }
    }
}
