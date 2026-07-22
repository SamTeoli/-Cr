using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed class RuntimePrototypeScreen : MonoBehaviour
    {
        private enum PendingRunAction
        {
            None,
            StartNewRun,
            ContinueRun
        }

        private RuntimePrototypeConfig config;
        private RunCampaignState campaign;
        private RunEncounterProgressState progress;
        private PlayerPermanentRewardState permanentRewards;
        private string selectedEnemyId;
        private readonly Dictionary<string, string> selectedBanishCardIds = new();
        private string selectedUpgradeCardId;
        private string message;
        private Vector2 scroll;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle wrappedStyle;
        private PendingRunAction pendingRunAction;

        public void Initialize(RuntimePrototypeConfig value)
        {
            config = value;
            LoadPermanentRewards();
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
            if (pendingRunAction != PendingRunAction.None)
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

        private void DrawRunSummary()
        {
            RunBattleState run = progress.RunState;
            GUILayout.Label(
                $"막 {campaign.Act} · 완료 {campaign.CompletedNodeCount}/12",
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
                DrawRunInventory();
            }
            GUILayout.Space(8f);
        }

        private void DrawRunInventory()
        {
            GUILayout.Label("런 소모아이템", headingStyle);
            GUILayout.Label(progress.RunState.ConsumableItemIds.Count == 0
                ? "보유 아이템 없음"
                : string.Join(", ", progress.RunState.ConsumableItemIds.Select(
                    id => PrototypeConsumableCatalog.Find(id)?.DisplayName ?? id)));

            if (!progress.RunState.ConsumableItemIds.Contains(
                    PrototypeConsumableCatalog.EnchantHammer) ||
                progress.RunDeck.Count == 0)
            {
                return;
            }

            RunCardInstance selected = SelectedUpgradeCard();
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"망치 대상: {selected?.Card.DisplayName}");
            if (GUILayout.Button("다음 카드", GUILayout.Width(100f)))
            {
                CycleUpgradeCard();
            }
            if (GUILayout.Button("인첸트 슬롯 +1", GUILayout.Width(140f)))
            {
                if (PrototypeConsumableService.TryUseEnchantHammer(
                        progress, selectedUpgradeCardId, out var failure))
                {
                    message = "인첸트 슬롯을 1칸 늘렸습니다.";
                    SaveRun(null);
                }
                else message = $"인첸트 망치 사용 실패: {failure}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNodeSelection()
        {
            GUILayout.Label("다음 노드 선택", headingStyle);
            foreach (RunNodeChoice choice in RunCampaignService.GetChoices(campaign))
            {
                if (!GUILayout.Button(
                        $"{choice.DisplayName}  ·  {choice.NodeId}",
                        GUILayout.Height(46f))) continue;
                if (!RunCampaignService.TrySelectNode(
                        campaign, choice.NodeId, out var failure))
                {
                    message = $"노드 선택 실패: {failure}";
                }
                else if (choice.IsBattle) BeginSelectedBattle();
                else
                {
                    message = $"{choice.DisplayName} 노드에 들어왔습니다.";
                    SaveRun(null);
                }
            }
        }

        private void DrawNonBattleNode()
        {
            if (campaign.ActiveNode == null)
            {
                Notice("현재 노드가 없습니다.");
                return;
            }
            switch (campaign.ActiveNode.NodeType)
            {
                case RunNodeType.Shop: DrawShop(); break;
                case RunNodeType.SituationEvent: DrawSituationEvent(); break;
                case RunNodeType.RestOrUpgrade: DrawRestOrUpgrade(); break;
                default: Notice($"지원하지 않는 노드: {campaign.ActiveNode.NodeType}"); break;
            }
        }

        private void DrawSituationEvent()
        {
            GUILayout.Label("상황 이벤트", headingStyle);
            GUILayout.Label("골드 획득, 피해, 최대 HP 증가 중 하나가 발생합니다.");
            if (!GUILayout.Button("이벤트 진행", GUILayout.Height(42f))) return;
            if (RunCampaignService.TryResolveSituationEvent(
                    campaign, progress.RunState, out string result, out var failure))
            {
                message = result;
                SaveRun(null);
            }
            else message = $"이벤트 처리 실패: {failure}";
        }

        private void DrawRestOrUpgrade()
        {
            GUILayout.Label("회복 · 강화", headingStyle);
            if (GUILayout.Button("최대 HP의 30% 회복", GUILayout.Height(38f)))
            {
                if (RunCampaignService.TryRest(
                        campaign, progress.RunState, out int healed, out var failure))
                {
                    message = $"HP를 {healed} 회복했습니다.";
                    SaveRun(null);
                }
                else message = $"회복 실패: {failure}";
            }
            RunCardInstance selected = SelectedUpgradeCard();
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(selected == null
                ? "강화할 카드 없음"
                : $"강화 대상: {selected.Card.DisplayName} Lv.{selected.CurrentLevel}");
            if (GUILayout.Button("다음 카드", GUILayout.Width(100f))) CycleUpgradeCard();
            if (GUILayout.Button("1레벨 강화", GUILayout.Width(120f)))
            {
                if (RunCampaignService.TryUpgrade(
                        campaign, progress, selectedUpgradeCardId, out var failure))
                {
                    message = "카드를 1레벨 강화했습니다.";
                    SaveRun(null);
                }
                else message = $"강화 실패: {failure}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawShop()
        {
            GUILayout.Label("상점", headingStyle);
            int seed = campaign.Seed + campaign.CompletedNodeCount * 31 +
                       campaign.ShopRerollCount * 101;
            GUILayout.Label("소모아이템");
            foreach (ConsumableData item in
                     Rotate(PrototypeConsumableCatalog.All, seed).Take(3))
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{item.DisplayName} · {item.RulesText}", wrappedStyle);
                if (GUILayout.Button($"{item.ShopPrice}G", GUILayout.Width(80f)))
                {
                    if (RunCampaignService.TryBuyConsumable(
                            campaign, progress.RunState, item.ItemId, out var failure))
                    {
                        message = $"{item.DisplayName} 구매 완료.";
                        SaveRun(null);
                    }
                    else message = $"구매 실패: {failure}";
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("인첸트");
            foreach (EnchantData enchant in
                     Rotate(config.EnchantDatabase.Enchants, seed + 7).Take(4))
            {
                DrawShopEnchant(enchant);
            }
            GUILayout.BeginHorizontal();
            int cost = RunCampaignService.GetShopRerollCost(campaign);
            if (GUILayout.Button($"전체 리롤 · {cost}G"))
            {
                if (RunCampaignService.TryRerollShop(
                        campaign, progress.RunState, out var failure))
                {
                    message = "상점 상품을 다시 생성했습니다.";
                    SaveRun(null);
                }
                else message = $"리롤 실패: {failure}";
            }
            if (GUILayout.Button("상점 나가기"))
            {
                if (RunCampaignService.TryLeaveShop(
                        campaign, progress.RunState, out var failure))
                {
                    message = "상점을 나왔습니다.";
                    SaveRun(null);
                }
                else message = $"상점 종료 실패: {failure}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawShopEnchant(EnchantData enchant)
        {
            if (enchant == null) return;
            int price = enchant.Rarity switch
            {
                CardRarity.Legendary => 120,
                CardRarity.Rare => 80,
                _ => 45
            };
            bool canAttach = TryFindEnchantTarget(enchant, out var target, out int slot);
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(
                $"{enchant.DisplayName} [{enchant.Rarity}] · {enchant.RulesText}",
                wrappedStyle);
            bool previous = GUI.enabled;
            GUI.enabled = canAttach;
            if (GUILayout.Button($"{price}G", GUILayout.Width(80f)))
            {
                if (RunCampaignService.TryBuyEnchant(
                        campaign, progress, enchant, target.OwnedCardId, slot, price,
                        out var attachFailure, out var failure))
                {
                    message = $"{target.Card.DisplayName}에 {enchant.DisplayName} 장착.";
                    SaveRun(null);
                }
                else message = $"인첸트 구매 실패: {failure} / {attachFailure}";
            }
            GUI.enabled = previous;
            GUILayout.EndHorizontal();
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
            DrawBattleConsumables(context);
            DrawEnemies(context);
            DrawMonsters(runtime);
            DrawInstalledCards(runtime);
            DrawHand(context);
            DrawRecentEvents(runtime);
            bool previous = GUI.enabled;
            GUI.enabled = !session.IsFinished;
            if (GUILayout.Button("턴 종료", GUILayout.Height(42f))) EndPlayerTurn(context);
            GUI.enabled = previous;
            if (session.IsFinished && GUILayout.Button("전투 정산", GUILayout.Height(44f)))
            {
                SettleBattle();
            }
        }

        private void DrawBattleConsumables(BattleRuntimeEncounterContext context)
        {
            GUILayout.Label("소모아이템");
            GUILayout.BeginHorizontal();
            foreach (string itemId in progress.RunState.ConsumableItemIds
                         .Distinct().ToList())
            {
                ConsumableData item =
                    PrototypeConsumableCatalog.Find(itemId);
                if (item == null ||
                    item.Effect == ConsumableEffect.IncreaseEnchantSlot)
                    continue;
                int owned = progress.RunState.ConsumableItemIds.Count(value =>
                    string.Equals(value, itemId, StringComparison.OrdinalIgnoreCase));
                int consumed = context.RunChanges.ConsumedItemIds.Count(value =>
                    string.Equals(value, itemId, StringComparison.OrdinalIgnoreCase));
                int remaining = Mathf.Max(0, owned - consumed);
                bool previous = GUI.enabled;
                GUI.enabled = !context.Session.IsFinished && remaining > 0;
                bool clicked = GUILayout.Button($"{item.DisplayName} ×{remaining}");
                GUI.enabled = previous;
                if (!clicked) continue;
                if (PrototypeConsumableService.TryUseInBattle(
                        context, item.ItemId, out int applied, out var failure))
                {
                    message = $"{item.DisplayName} 사용 · 적용량 {applied}";
                    SaveRun(null);
                }
                else message = $"아이템 사용 실패: {failure}";
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
            BattleRuntimeState runtime = context.Runtime;
            Dictionary<string, string> intents = BuildEnemyIntentLabels(context);
            GUILayout.Label("적 필드");
            GUILayout.BeginHorizontal();
            foreach (EnemyFieldPosition position in Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string id = runtime.EnemyPositions.GetOccupant(position);
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                if (string.IsNullOrWhiteSpace(id)) GUILayout.Label("빈 칸");
                else
                {
                    BattleEnemyRuntimeState enemy = runtime.FindEnemy(id);
                    BattleEnemyStatusState status = runtime.EnemyStatuses.Find(id);
                    EncounterEnemySlot slot = context.Encounter.EnemySlots
                        .FirstOrDefault(value => value != null &&
                            string.Equals(value.EnemyInstanceId, id,
                                StringComparison.OrdinalIgnoreCase));
                    string enemyName = slot?.Enemy?.DisplayName ?? id;
                    int maximumHealth = slot?.Enemy?.MaximumHealth ??
                                        enemy.Vital.CurrentHealth;
                    string selection = string.Equals(selectedEnemyId, id,
                        StringComparison.OrdinalIgnoreCase) ? "▶ " : string.Empty;
                    string nextIntent = intents.TryGetValue(id, out string intent)
                        ? intent
                        : "없음";
                    GUILayout.Label(
                        $"{selection}{enemyName}\n" +
                        $"HP {enemy.Vital.CurrentHealth}/{maximumHealth} · " +
                        $"공격 {enemy.Attack}\n" +
                        $"다음 행동: {nextIntent}",
                        wrappedStyle);
                    string statusText = DescribeEnemyStatus(status);
                    if (!string.IsNullOrWhiteSpace(statusText))
                        GUILayout.Label(statusText, wrappedStyle);
                    if (GUILayout.Button(selectedEnemyId == id ? "선택됨" : "대상 선택"))
                        selectedEnemyId = id;
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

        private static string DescribeEnemyCommand(
            BattleRuntimeEnemyTurnCommand command)
        {
            switch (command.ActionType)
            {
                case BattleRuntimeEnemyTurnActionType.Move:
                    string direction = command.MoveDirection ==
                        EnemyMoveDirection.Left ? "왼쪽" : "오른쪽";
                    return $"{direction} 이동 {command.MoveSteps}";
                case BattleRuntimeEnemyTurnActionType.Attack:
                    int count = Mathf.Max(1, command.AutomaticAttackCount);
                    return count == 1 ? "공격" : $"공격 ×{count}";
                case BattleRuntimeEnemyTurnActionType.Ability:
                    EnemyAbilityResolutionContext ability = command.Ability;
                    string range = ability.IsAreaAbility ? "광역" : "단일";
                    string effect = ability.HasStatusEffect
                        ? $" · {DescribeStatusKeyword(ability.StatusKeyword)} " +
                          $"{ability.StatusAmount}"
                        : string.Empty;
                    return $"능력 {ability.AbilityId} ({range}{effect})";
                default:
                    return command.ActionType.ToString();
            }
        }

        private static string DescribeEnemyStatus(BattleEnemyStatusState status)
        {
            if (status == null) return string.Empty;
            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? string.Empty
                : "상태: " + string.Join(" · ", values);
        }

        private static string DescribeCommonStatus(BattleCommonStatusState status)
        {
            if (status == null) return string.Empty;
            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? string.Empty
                : "상태: " + string.Join(" · ", values);
        }

        private static void AddStatus(
            ICollection<string> values,
            string label,
            int amount)
        {
            if (amount > 0) values.Add($"{label} {amount}");
        }

        private static string DescribeStatusKeyword(StatusKeyword keyword)
        {
            return keyword switch
            {
                StatusKeyword.Injury => "부상",
                StatusKeyword.Bind => "속박",
                StatusKeyword.Stun => "기절",
                StatusKeyword.Weaken => "약화",
                StatusKeyword.Vulnerable => "취약",
                _ => keyword.ToString()
            };
        }

        private void DrawMonsters(BattleRuntimeState runtime)
        {
            GUILayout.Label("아군 몬스터");
            GUILayout.BeginHorizontal();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string id = runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster = string.IsNullOrWhiteSpace(id)
                    ? null : runtime.Monsters.Find(id);
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                if (monster == null) GUILayout.Label("빈 칸");
                else
                {
                    GUILayout.Label(
                        $"{monster.Card.SourceCard.DisplayName}\n공격 {monster.Attack} · " +
                        $"HP {monster.CurrentHealth}/{monster.MaximumHealth}");
                    string statusText = DescribeCommonStatus(monster.Status);
                    if (!string.IsNullOrWhiteSpace(statusText))
                        GUILayout.Label(statusText, wrappedStyle);
                    bool previous = GUI.enabled;
                    GUI.enabled = monster.Status.CanAttack &&
                                  !string.IsNullOrWhiteSpace(selectedEnemyId);
                    if (GUILayout.Button("선택한 적 공격"))
                        ResolvePlayerAttack(monster.BattleCardId);
                    GUI.enabled = previous;
                    if (!monster.Status.CanAttack)
                        GUILayout.Label("속박·기절로 공격 불가", wrappedStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHand(BattleRuntimeEncounterContext context)
        {
            List<BattleCardInstance> hand =
                context.Runtime.Deck.Zones.GetCards(CardZone.Hand);
            GUILayout.Label($"패 ({hand.Count})", headingStyle);
            foreach (BattleCardInstance card in hand.ToList())
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(
                    $"{card.SourceCard.CatalogCardId} {card.SourceCard.DisplayName} · " +
                    $"비용 {card.Resolved.ManaCost}\n{card.Resolved.RulesText}", wrappedStyle);
                string banishTargetId = card.SourceCard.CatalogCardId ==
                    TestContentIds.C07
                    ? DrawBanishSelection(hand, card)
                    : null;

                bool canPlay = BattleRuntimePlayerCardActionService.TryValidate(
                    context.Runtime,
                    card.Ids.BattleCardId,
                    selectedEnemyId,
                    banishTargetId,
                    out var actionFailure,
                    out var playFailure,
                    out var cardFailure);
                bool previous = GUI.enabled;
                GUI.enabled = canPlay;
                bool clicked = GUILayout.Button("사용", GUILayout.Width(75f));
                GUI.enabled = previous;
                if (clicked)
                    ResolveCardPlay(context, card.Ids.BattleCardId);
                else if (!canPlay)
                    GUILayout.Label(
                        DescribeCardBlock(actionFailure, playFailure, cardFailure),
                        GUILayout.Width(135f));
                GUILayout.EndHorizontal();
            }
        }

        private string DrawBanishSelection(
            IReadOnlyList<BattleCardInstance> hand,
            BattleCardInstance source)
        {
            List<BattleCardInstance> candidates = hand
                .Where(card => card != source)
                .ToList();
            BattleCardInstance selected = candidates.FirstOrDefault(card =>
                string.Equals(
                    card.Ids.BattleCardId,
                    SelectedBanishCardId(source.Ids.BattleCardId),
                    StringComparison.OrdinalIgnoreCase));
            selected ??= candidates.FirstOrDefault();
            if (selected == null)
            {
                selectedBanishCardIds.Remove(source.Ids.BattleCardId);
                GUILayout.Label("소멸 대상 없음", GUILayout.Width(150f));
                return null;
            }

            selectedBanishCardIds[source.Ids.BattleCardId] =
                selected.Ids.BattleCardId;

            if (GUILayout.Button(
                    $"소멸: {selected.SourceCard.DisplayName}",
                    GUILayout.Width(170f)))
            {
                int index = candidates.IndexOf(selected);
                selected = candidates[(index + 1) % candidates.Count];
                selectedBanishCardIds[source.Ids.BattleCardId] =
                    selected.Ids.BattleCardId;
            }
            return selected.Ids.BattleCardId;
        }

        private void DrawRecentEvents(BattleRuntimeState runtime)
        {
            IReadOnlyList<BattleEventRecord> events = runtime.EventLog.Events;
            GUILayout.Label("최근 전투 기록", headingStyle);
            if (events.Count == 0)
            {
                GUILayout.Label("기록 없음");
                return;
            }

            foreach (BattleEventRecord record in events
                         .Skip(Mathf.Max(0, events.Count - 6)))
            {
                if (record == null) continue;
                GUILayout.Label(
                    $"{record.EventType} · {record.Cause} · " +
                    $"{record.ActorId} → {record.TargetId}",
                    wrappedStyle);
            }
        }

        private void DrawRewards()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (context == null || !context.Settlement.IsSettled)
            {
                Notice("정산된 전투가 없습니다.");
                return;
            }
            GUILayout.Label("전투 보상", headingStyle);
            GUILayout.Label($"골드 {context.VictoryRewards.GoldReward} 수령 완료");
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
                                $"{enchant.DisplayName} [{enchant.Rarity}] · " +
                                enchant.RulesText, GUILayout.Height(40f)))
                            ClaimEnchantReward(rewards, enchant);
                    }
                }
                else if (rewards?.Claimed == true)
                    Notice($"{rewards.ClaimedEnchant.DisplayName} 선택 완료");
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
                        if (!GUILayout.Button($"{item.DisplayName} · {item.RulesText}"))
                            continue;
                        if (context.VictoryConsumableRewards.TryClaim(
                                item.ItemId, out var failure))
                        {
                            message = $"{item.DisplayName} 보상 수령 완료.";
                            SaveRun(null);
                        }
                        else message = $"소모아이템 보상 실패: {failure}";
                    }
                }
            }
            if (GUILayout.Button("보상 완료 · 다음 노드", GUILayout.Height(46f)))
                CompleteRewards();
        }

        private void RequestStartNewRun()
        {
            if (config == null || !config.IsReady)
            {
                message = "게임 데이터베이스를 불러올 수 없습니다.";
                return;
            }

            bool hasCurrentRun = campaign != null || progress != null;
            bool inspected = RunSaveSlotService.TryInspectDefault(
                config.CardDatabase,
                config.EnchantDatabase,
                config.EncounterDatabase,
                permanentRewards,
                out RunSaveSlotInfo slot,
                out _);
            RunSaveSlotState slotState = slot?.State ?? RunSaveSlotState.Empty;
            if (RunActionConfirmationPolicy.ShouldConfirmNewRun(
                    hasCurrentRun, inspected, slotState))
            {
                pendingRunAction = PendingRunAction.StartNewRun;
                return;
            }

            StartNewRun();
        }

        private void RequestContinueRun()
        {
            bool hasCurrentRun = campaign != null && progress != null;
            RunCampaignPhase phase = campaign?.Phase ??
                                     RunCampaignPhase.NodeSelection;
            if (RunActionConfirmationPolicy.ShouldConfirmContinue(
                    hasCurrentRun, phase))
            {
                pendingRunAction = PendingRunAction.ContinueRun;
                return;
            }

            ContinueRun();
        }

        private void DrawRunActionConfirmation()
        {
            bool startsNewRun = pendingRunAction == PendingRunAction.StartNewRun;
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(
                startsNewRun
                    ? "새 런을 시작할까요?"
                    : "전투를 처음부터 다시 시작할까요?",
                titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(
                startsNewRun
                    ? "현재 진행과 저장된 런이 새 런으로 교체됩니다. " +
                      "이 작업은 되돌릴 수 없습니다."
                    : "이어하기를 선택하면 현재 전투 진행을 버리고 " +
                      "전투 시작 체크포인트에서 다시 시작합니다.",
                wrappedStyle);
            GUILayout.Space(18f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(44f)))
            {
                pendingRunAction = PendingRunAction.None;
            }
            if (GUILayout.Button(
                    startsNewRun ? "새 런 시작" : "전투 다시 시작",
                    GUILayout.Height(44f)))
            {
                PendingRunAction confirmedAction = pendingRunAction;
                pendingRunAction = PendingRunAction.None;
                if (confirmedAction == PendingRunAction.StartNewRun)
                {
                    StartNewRun();
                }
                else
                {
                    ContinueRun();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        private void StartNewRun()
        {
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in config.CardDatabase.Cards.Where(card => card != null))
            {
                deck.TryAdd(new RunCardInstance(
                    card, $"OWNED-RUN-{++index:00}-{card.CatalogCardId}", 1), out _);
            }
            RunBattleState run = new(30, 30, 60, new[]
            {
                PrototypeConsumableCatalog.HealingPotion,
                PrototypeConsumableCatalog.CleanseScroll,
                PrototypeConsumableCatalog.ManaBattery,
                PrototypeConsumableCatalog.EnchantHammer
            });
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(run, deck, permanentRewards);
            campaign = new RunCampaignState(Environment.TickCount & int.MaxValue);
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            selectedEnemyId = null;
            selectedBanishCardIds.Clear();
            scroll = Vector2.zero;
            message = "새 런을 시작했습니다.";
            SaveRun(null);
        }

        private void ContinueRun()
        {
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    config.CardDatabase, config.EnchantDatabase,
                    config.EncounterDatabase, permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                message = $"이어하기 실패: {failure}";
                return;
            }
            selectedUpgradeCardId = progress.RunDeck.Cards.FirstOrDefault()?.OwnedCardId;
            selectedBanishCardIds.Clear();
            scroll = Vector2.zero;
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
                    config.EncounterDatabase, config.GetEncounterPool(grade),
                    grade, selectionSeed, out var encounter, out string poolError))
            {
                message = $"조우 선택 실패: {poolError}";
                return;
            }
            string battleId =
                $"RUN-{campaign.Seed}-NODE-{campaign.CompletedNodeCount + 1:00}";
            int seed = campaign.Seed + campaign.CompletedNodeCount * 101;
            if (!RunEncounterProgressService.TryBegin(
                    progress, battleId, encounter, seed, 5, Array.Empty<string>(),
                    (uint)Mathf.Abs(seed), out _, out var failure, out var flowFailure,
                    out var deckFailure, out var bootstrapFailure, out var sessionFailure,
                    out var redrawFailure, out var turnFailure,
                    out List<string> validationErrors))
            {
                message = $"전투 시작 실패: {failure} / {flowFailure} / " +
                          $"{deckFailure} / {bootstrapFailure} / {sessionFailure} / " +
                          $"{redrawFailure} / {turnFailure}" +
                          (validationErrors.Count == 0 ? string.Empty :
                              $"\n{string.Join("\n", validationErrors)}");
                return;
            }
            selectedBanishCardIds.Clear();
            SelectFirstEnemy();
            message = $"{campaign.ActiveNode.DisplayName} 전투 시작.";
            SaveRun(null, true);
        }

        private void ResolveCardPlay(
            BattleRuntimeEncounterContext context, string battleCardId)
        {
            if (!BattleRuntimePlayerCardActionService.TryResolve(
                    context.Runtime, battleCardId, selectedEnemyId,
                    SelectedBanishCardId(battleCardId),
                    out var result, out var failure,
                    out var playFailure, out var cardFailure))
            {
                message = $"카드 사용 실패: {failure} / {playFailure} / {cardFailure}";
                return;
            }
            message = $"{result.Play.Card.SourceCard.DisplayName} 사용 완료.";
            selectedBanishCardIds.Remove(battleCardId);
            FinalizeOutcome();
            SelectFirstEnemy();
            SaveRun(null);
        }

        private void ResolvePlayerAttack(string battleCardId)
        {
            if (!BattleRuntimePlayerAttackService.TryResolve(
                    progress.ActiveEncounter.Runtime, battleCardId, selectedEnemyId,
                    out var result, out var failure))
            {
                message = $"공격 실패: {failure}";
                return;
            }
            message = $"공격 완료 · 피해 {result.DamageApplied}";
            FinalizeOutcome();
            SelectFirstEnemy();
            SaveRun(null);
        }

        private void EndPlayerTurn(BattleRuntimeEncounterContext context)
        {
            int tieBreaker = campaign.Seed + context.Session.CompletedRoundCount * 10;
            if (!BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                    context.Session, context.Encounter, tieBreaker, out var result,
                    out var patternFailure, out var sessionFailure,
                    out var roundFailure, out var turnFailure,
                    out var pipelineFailure, out var planFailure,
                    out var enemyTurnFailure, out int actionIndex))
            {
                message = $"턴 종료 실패: {patternFailure} / {sessionFailure} / " +
                          $"{roundFailure} / {turnFailure} / {pipelineFailure} / " +
                          $"{planFailure} / {enemyTurnFailure} / action {actionIndex}";
                return;
            }
            message = result.Outcome == BattleOutcome.Ongoing
                ? $"적 턴 완료 · 플레이어 턴 {context.Runtime.Turn.PlayerTurnNumber}"
                : $"전투 종료 · {result.Outcome}";
            selectedBanishCardIds.Clear();
            SelectFirstEnemy();
            SaveRun(null);
        }

        private void FinalizeOutcome()
        {
            BattleRuntimeSessionState session = progress.ActiveEncounter.Session;
            if (!session.IsFinished && BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    session, out BattleOutcome outcome, out _))
                message += $" 전투 종료 · {outcome}";
        }

        private void SettleBattle()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (!RunEncounterProgressService.TrySettleActive(
                    progress, out var progressFailure, out var flowFailure,
                    out var sessionFailure, out var settlementFailure))
            {
                message = $"정산 실패: {progressFailure} / {flowFailure} / " +
                          $"{sessionFailure} / {settlementFailure}";
                return;
            }
            if (context.Settlement.SettledOutcome == BattleOutcome.Defeat)
            {
                RunEncounterProgressService.TryCompleteActive(progress, out _);
                RunCampaignService.MarkBattleReward(campaign, BattleOutcome.Defeat);
                message = "패배 정산 완료 · 런 종료";
                SaveRun(null);
                return;
            }
            if (!context.VictoryRewards.TryClaimGold(out var rewardFailure))
            {
                message = $"골드 보상 실패: {rewardFailure}";
                return;
            }
            if (context.VictoryRewards.GrantsFinalBossPermanentReward)
            {
                if (!BattleVictoryPermanentRewardService.TryCreate(
                        progress, out var permanent, out var createFailure))
                {
                    message = $"영구 보상 생성 실패: {createFailure}";
                    return;
                }
                if (!permanent.TryClaim(
                        "PERMANENT-FIRST-RUN-CLEAR", out var claimFailure))
                {
                    message = $"영구 보상 수령 실패: {claimFailure}";
                    return;
                }
            }
            RunCampaignService.MarkBattleReward(campaign, BattleOutcome.Victory);
            message = $"승리 정산 완료 · 골드 {context.VictoryRewards.GoldReward} 획득";
            SaveRun(null);
        }

        private void EnsureEnchantRewards(BattleRuntimeEncounterContext context)
        {
            if (context.VictoryEnchantRewards != null) return;
            List<EnchantData> choices = config.EnchantDatabase.Enchants
                .Where(enchant => enchant != null &&
                                  TryFindEnchantTarget(enchant, out _, out _))
                .OrderByDescending(enchant => (int)enchant.Rarity >=
                    (int)context.VictoryRewards.MinimumGuaranteedEnchantRarity)
                .ThenBy(enchant => enchant.DefinitionId)
                .Take(context.VictoryRewards.EnchantChoiceCount).ToList();
            if (!BattleVictoryEnchantRewardService.TryCreate(
                    context, progress.RunDeck, choices, out _, out var failure))
                message = $"인첸트 보상 생성 실패: {failure}";
        }

        private void ClaimEnchantReward(
            BattleVictoryEnchantRewardService rewards, EnchantData enchant)
        {
            if (!TryFindEnchantTarget(enchant, out var target, out int slot))
            {
                message = "장착 가능한 카드가 없습니다.";
                return;
            }
            if (rewards.TryClaim(enchant.DefinitionId, target.OwnedCardId, slot,
                    out var attachmentFailure, out var failure))
            {
                message = $"{target.Card.DisplayName}에 {enchant.DisplayName} 장착.";
                SaveRun(null);
            }
            else message = $"보상 선택 실패: {failure} / {attachmentFailure}";
        }

        private void EnsureConsumableRewards(BattleRuntimeEncounterContext context)
        {
            if (context.VictoryConsumableRewards == null &&
                !BattleVictoryConsumableRewardService.TryCreate(
                    context, out _, out var failure))
                message = $"소모아이템 보상 생성 실패: {failure}";
        }

        private void CompleteRewards()
        {
            if (!RunEncounterProgressService.TryCompleteActive(
                    progress, out var failure))
            {
                message = $"보상 미완료: {failure}";
                return;
            }
            RunCampaignService.CompleteBattleReward(campaign);
            message = "보상 완료 · 다음 노드를 선택하세요.";
            SaveRun(null);
        }

        private bool TryFindEnchantTarget(
            EnchantData enchant, out RunCardInstance target, out int slot)
        {
            target = null;
            slot = -1;
            if (enchant == null || progress?.RunDeck == null) return false;
            foreach (RunCardInstance card in progress.RunDeck.Cards)
            for (int i = 0; i < card.Enchants.SlotCount; i++)
            {
                if (!card.Enchants.CanAttach(enchant, i, out _)) continue;
                target = card;
                slot = i;
                return true;
            }
            return false;
        }

        private RunCardInstance SelectedUpgradeCard()
        {
            RunCardInstance selected = progress?.RunDeck?.Cards.FirstOrDefault(card =>
                string.Equals(card.OwnedCardId, selectedUpgradeCardId,
                    StringComparison.OrdinalIgnoreCase));
            return selected ?? progress?.RunDeck?.Cards.FirstOrDefault();
        }

        private void CycleUpgradeCard()
        {
            if (progress?.RunDeck == null || progress.RunDeck.Count == 0) return;
            List<RunCardInstance> cards = progress.RunDeck.Cards.ToList();
            int index = cards.FindIndex(card => card.OwnedCardId == selectedUpgradeCardId);
            selectedUpgradeCardId = cards[(index + 1 + cards.Count) % cards.Count].OwnedCardId;
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
                runtime.LivingEnemies.Contains(selectedEnemyId)) return;
            selectedEnemyId = runtime.Enemies.FirstOrDefault(enemy =>
                enemy != null && enemy.IsAlive)?.EnemyId;
        }

        private string SelectedBanishCardId(string battleCardId)
        {
            return !string.IsNullOrWhiteSpace(battleCardId) &&
                   selectedBanishCardIds.TryGetValue(
                       battleCardId, out string selected)
                ? selected
                : null;
        }

        private void SaveRun(string successMessage, bool forceActive = false)
        {
            if (campaign == null || progress == null) return;
            if (progress.HasActiveEncounter && !forceActive)
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = "전투 시작 체크포인트가 이미 저장되어 있습니다. " +
                              "이어하기 시 현재 전투를 처음부터 다시 시작합니다.";
                }
                return;
            }

            if (IntegratedRunSaveService.TrySave(
                    campaign, progress, out var destination, out var failure))
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                    message = $"{successMessage} · {destination}";
            }
            else
            {
                string prefix = string.IsNullOrWhiteSpace(message)
                    ? string.Empty
                    : message + "\n";
                string label = string.IsNullOrWhiteSpace(successMessage)
                    ? "자동 저장 실패"
                    : "저장 실패";
                message = $"{prefix}{label}: {failure}";
            }
        }

        private static string DescribeCardBlock(
            BattleRuntimePlayerCardActionFailure actionFailure,
            BattleRuntimeCardPlayFailure playFailure,
            CardPlayFailure cardFailure)
        {
            if (actionFailure == BattleRuntimePlayerCardActionFailure.MissingTarget)
                return "적 대상 필요";
            if (actionFailure ==
                BattleRuntimePlayerCardActionFailure.InvalidBanishSelection)
                return "소멸 대상 필요";
            return cardFailure switch
            {
                CardPlayFailure.NotEnoughMana => "마력 부족",
                CardPlayFailure.DestinationFull => "필드 포화",
                CardPlayFailure.DuplicateBarrier => "동일 결계 설치됨",
                _ when playFailure ==
                    BattleRuntimeCardPlayFailure.InvalidTurnPhase => "행동 불가 단계",
                _ => actionFailure.ToString()
            };
        }

        private void LoadPermanentRewards()
        {
            if (PlayerPermanentRewardSaveService.TryLoadDefault(
                    out var loaded, out _, out _)) permanentRewards = loaded;
            else permanentRewards ??= new PlayerPermanentRewardState();
        }

        private void DrawMessage()
        {
            if (!string.IsNullOrWhiteSpace(message)) Notice(message);
        }

        private void Notice(string text)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(text, wrappedStyle);
            GUILayout.EndVertical();
        }

        private static IEnumerable<T> Rotate<T>(IReadOnlyList<T> values, int seed)
        {
            if (values == null || values.Count == 0) yield break;
            int start = Mathf.Abs(seed % values.Count);
            for (int i = 0; i < values.Count; i++)
                yield return values[(start + i) % values.Count];
        }
    }
}
