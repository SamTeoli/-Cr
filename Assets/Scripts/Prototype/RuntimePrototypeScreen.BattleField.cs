using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private GameObject finalBattleFieldObject;
        private RuntimeBattleFieldView finalBattleFieldView;
        private string finalBattleFieldSignature;
        private string pendingTargetedSkillPlacementCardId;
        private PlayerMonsterFieldPosition?
            pendingTargetedSkillPlacementPosition;

        private void LateUpdate()
        {
            RefreshFinalBattleFieldOverlay();
        }

        private void RefreshFinalBattleFieldOverlay()
        {
            if (FinalUiRoot == null || FinalUiRoot.RootCanvas == null)
            {
                return;
            }

            bool show = FinalUiRoot.CurrentScreen == RuntimeGameScreen.Battle &&
                        campaign?.Phase == RunCampaignPhase.Battle &&
                        progress != null;
            if (!show)
            {
                if (finalBattleFieldObject != null)
                {
                    finalBattleFieldObject.SetActive(false);
                }
                finalBattleFieldSignature = null;
                return;
            }

            EnsureFinalBattleFieldOverlay();
            if (finalBattleFieldObject == null || finalBattleFieldView == null)
            {
                return;
            }

            finalBattleFieldObject.SetActive(true);
            HideLegacyFieldCommandButtons();
            TryFinalizePendingTargetedSkillPlacement();

            BattleScreenSnapshot snapshot =
                battleScreen.CreateSnapshot(progress, campaign);
            RuntimeBattleFieldPresentation presentation =
                RuntimeBattleFieldPresentation.FromSnapshot(snapshot);
            presentation = ApplyPendingTargetedSkillPresentation(
                presentation);
            string signature = CreateBattleFieldSignature(presentation);
            if (string.Equals(
                    signature,
                    finalBattleFieldSignature,
                    StringComparison.Ordinal))
            {
                return;
            }

            finalBattleFieldView.Bind(presentation);
            finalBattleFieldSignature = signature;
        }

        private void EnsureFinalBattleFieldOverlay()
        {
            if (finalBattleFieldView != null)
            {
                return;
            }

            Transform panel = FinalUiRoot.BattleTitleText?.transform.parent;
            if (panel == null)
            {
                return;
            }

            finalBattleFieldObject = new GameObject(
                "BattleField",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RuntimeBattleFieldView));
            finalBattleFieldObject.transform.SetParent(panel, false);
            Image background = finalBattleFieldObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.04f, 0.07f, 0.92f);
            background.raycastTarget = true;

            LayoutElement fieldLayout =
                finalBattleFieldObject.GetComponent<LayoutElement>();
            fieldLayout.preferredHeight = 720f;
            fieldLayout.minHeight = 560f;
            fieldLayout.flexibleHeight = 1f;

            finalBattleFieldView =
                finalBattleFieldObject.GetComponent<RuntimeBattleFieldView>();
            finalBattleFieldView.Initialize(
                ExecuteFinalBattleCommand,
                FinalUiRoot.ShowBattleFieldDetail);
            finalBattleFieldView.Bind(RuntimeBattleFieldPresentation.Empty);

            finalBattleFieldObject.transform.SetAsFirstSibling();

            Transform commandScroll =
                FinalUiRoot.BattleCommandList?.transform.parent?.parent;
            LayoutElement commandLayout =
                commandScroll?.GetComponent<LayoutElement>();
            if (commandLayout != null)
            {
                commandLayout.preferredHeight = 66f;
            }
        }

        private void HideLegacyFieldCommandButtons()
        {
            RectTransform commands = FinalUiRoot.BattleCommandList;
            if (commands == null)
            {
                return;
            }

            for (int index = 0; index < commands.childCount; index++)
            {
                GameObject child = commands.GetChild(index).gameObject;
                Text label = child.GetComponentInChildren<Text>(true);
                string text = label?.text ?? string.Empty;
                bool fieldCommand = text.StartsWith(
                                        "[적 대상]",
                                        StringComparison.Ordinal) ||
                                    text.StartsWith(
                                        "[공격]",
                                        StringComparison.Ordinal) ||
                                    text.StartsWith(
                                        "[소모품]",
                                        StringComparison.Ordinal);
                if (fieldCommand && child.activeSelf)
                {
                    child.SetActive(false);
                }
            }
        }

        private bool TryExecuteFinalFieldCardDrop(
    string cardCommandId,
    string targetCommandId)
{
    if (!TryReadCommandValue(
            targetCommandId,
            "field:monster:",
            out string slotText))
    {
        return TryExecuteFinalSkillFieldCardDrop(
            cardCommandId,
            targetCommandId);
    }
    if (!TryResolveMonsterPosition(
            slotText,
            out PlayerMonsterFieldPosition position))
    {
        message = $"몬스터 소환 실패: 잘못된 몬스터존 {slotText}";
        return true;
    }
    if (!TryReadCommandValue(
            cardCommandId,
            "play:",
            out string battleCardId))
    {
        message = "몬스터 소환 실패: 드래그한 카드를 찾을 수 없습니다.";
        return true;
    }
    if (BeginBanishTargetSelectionIfRequired(battleCardId))
    {
        message = "효과 대상을 손패에서 선택하세요.";
        finalCampaignScreen = null;
        finalBattleFieldSignature = null;
        RefreshFinalBattle();
        return true;
    }
    BattleCardPlayCommandResult command = battleScreen.TryPlayCard(
        progress,
        battleCardId,
        position);
    message = command.Message;
    if (command.Succeeded)
    {
        SaveRun(null);
    }
    finalCampaignScreen = null;
    finalBattleFieldSignature = null;
    if (campaign?.Phase == RunCampaignPhase.Battle)
    {
        RefreshFinalBattle();
        SetFinalUiActive(true);
        FinalUiRoot.ShowScreen(RuntimeGameScreen.Battle);
    }
    else
    {
        RefreshFinalUiVisibility();
    }
    return true;
}

private RuntimeBattleFieldPresentation
    ApplyPendingTargetedSkillPresentation(
        RuntimeBattleFieldPresentation presentation)
{
    if (presentation == null ||
        string.IsNullOrWhiteSpace(
            pendingTargetedSkillPlacementCardId) ||
        !pendingTargetedSkillPlacementPosition.HasValue)
    {
        return presentation;
    }

    int index = (int)pendingTargetedSkillPlacementPosition.Value;
    if (index < 0 || index >= presentation.Skills.Length)
    {
        return presentation;
    }

    BattleCardInstance card = progress?.ActiveEncounter?.Session?.Runtime?
        .Deck.Zones.Find(pendingTargetedSkillPlacementCardId);
    if (card == null)
    {
        return presentation;
    }

    RuntimeBattleFieldSlotPresentation[] skills =
        (RuntimeBattleFieldSlotPresentation[])presentation.Skills.Clone();
    skills[index] = new RuntimeBattleFieldSlotPresentation(
        RuntimeBattleFieldZone.PlayerSkill,
        index,
        $"{card.SourceCard.DisplayName} · 활성화 중",
        "필드에서 효과 대상을 클릭하세요.",
        true,
        true,
        false,
        string.Empty,
        string.Empty);
    return new RuntimeBattleFieldPresentation(
        presentation.Enemies,
        presentation.Monsters,
        skills);
}

private void TryFinalizePendingTargetedSkillPlacement()
{
    if (string.IsNullOrWhiteSpace(
            pendingTargetedSkillPlacementCardId) ||
        !pendingTargetedSkillPlacementPosition.HasValue)
    {
        return;
    }

    BattleRuntimeState runtime =
        progress?.ActiveEncounter?.Session?.Runtime;
    BattleCardInstance card = runtime?.Deck.Zones.Find(
        pendingTargetedSkillPlacementCardId);
    if (card?.Zone == CardZone.SkillField)
    {
        runtime.PlayerSkillPositions.TryPlace(
            pendingTargetedSkillPlacementCardId,
            pendingTargetedSkillPlacementPosition.Value);
        pendingTargetedSkillPlacementCardId = null;
        pendingTargetedSkillPlacementPosition = null;
        SaveRun(null);
        finalBattleFieldSignature = null;
        return;
    }

    if (card == null ||
        (card.Zone != CardZone.Hand &&
         card.Zone != CardZone.SkillField))
    {
        pendingTargetedSkillPlacementCardId = null;
        pendingTargetedSkillPlacementPosition = null;
        finalBattleFieldSignature = null;
    }
}

private bool TryExecuteFinalSkillFieldCardDrop(
    string cardCommandId,
    string targetCommandId)
{
    if (!TryReadCommandValue(
            targetCommandId,
            "field:skill:",
            out string slotText) ||
        !TryResolveMonsterPosition(
            slotText,
            out PlayerMonsterFieldPosition position))
    {
        return false;
    }
    if (!TryReadCommandValue(
            cardCommandId,
            "play:",
            out string battleCardId))
    {
        message = "카드 설치 실패: 드래그한 카드를 찾을 수 없습니다.";
        return true;
    }

    BattleRuntimeState runtime =
        progress?.ActiveEncounter?.Session?.Runtime;
    if (runtime == null ||
        !string.IsNullOrWhiteSpace(
            runtime.PlayerSkillPositions.GetOccupant(position)))
    {
        message = $"카드 설치 실패: 선택한 스킬존 {slotText}은 사용할 수 없습니다.";
        return true;
    }
    if (BeginBanishTargetSelectionIfRequired(battleCardId))
    {
        message = "효과 대상을 손패에서 선택하세요.";
        finalCampaignScreen = null;
        finalBattleFieldSignature = null;
        RefreshFinalBattle();
        return true;
    }
    BattleCardInstance draggedCard =
        runtime.Deck.Zones.Find(battleCardId);
    bool requiresEnemyTarget = draggedCard != null &&
        CardEffectRegistrationCatalog.TryFind(
            draggedCard.SourceCard.CatalogCardId,
            out CardEffectRegistration registration) &&
        registration.Route == CardEffectRoute.TargetedSkill;
    if (requiresEnemyTarget)
    {
        if (battleScreen.TryBeginCardTargeting(
                progress,
                battleCardId,
                out message))
        {
            pendingTargetedSkillPlacementCardId = battleCardId;
            pendingTargetedSkillPlacementPosition = position;
            message =
                $"{draggedCard.SourceCard.DisplayName}을(를) 스킬존에 " +
                "활성화했습니다. 필드에서 효과 대상을 클릭하세요.";
        }
        finalCampaignScreen = null;
        finalBattleFieldSignature = null;
        RefreshFinalBattle();
        return true;
    }

    BattleCardPlayCommandResult command =
        battleScreen.TryPlayCard(progress, battleCardId);
    message = command.Message;
    if (command.Succeeded)
    {
        BattleCardInstance played = runtime.Deck.Zones.Find(battleCardId);
        if (played?.Zone == CardZone.SkillField)
        {
            runtime.PlayerSkillPositions.TryPlace(
                battleCardId,
                position);
        }
        SaveRun(null);
    }
    finalCampaignScreen = null;
    finalBattleFieldSignature = null;
    if (campaign?.Phase == RunCampaignPhase.Battle)
    {
        RefreshFinalBattle();
        SetFinalUiActive(true);
        FinalUiRoot.ShowScreen(RuntimeGameScreen.Battle);
    }
    else
    {
        RefreshFinalUiVisibility();
    }
    return true;
}

private static bool TryResolveMonsterPosition(
    string slotText,
    out PlayerMonsterFieldPosition position)
{
    position = default;
    if (!int.TryParse(slotText, out int index))
    {
        return false;
    }
    switch (index)
    {
        case 0:
            position = PlayerMonsterFieldPosition.Left;
            return true;
        case 1:
            position = PlayerMonsterFieldPosition.Center;
            return true;
        case 2:
            position = PlayerMonsterFieldPosition.Right;
            return true;
        default:
            return false;
    }
}

        private static string CreateBattleFieldSignature(
            RuntimeBattleFieldPresentation presentation)
        {
            StringBuilder builder = new();
            AppendSlots(builder, presentation.Enemies);
            AppendSlots(builder, presentation.Monsters);
            AppendSlots(builder, presentation.Skills);
            return builder.ToString();
        }

        private static void AppendSlots(
            StringBuilder builder,
            RuntimeBattleFieldSlotPresentation[] slots)
        {
            foreach (RuntimeBattleFieldSlotPresentation slot in slots)
            {
                builder.Append((int)slot.Zone).Append('|')
                    .Append(slot.Index).Append('|')
                    .Append(slot.Title).Append('|')
                    .Append(slot.Detail).Append('|')
                    .Append(slot.Occupied).Append('|')
                    .Append(slot.Selected).Append('|')
                    .Append(slot.Interactable).Append('|')
                    .Append(slot.ClickCommandId).Append('|')
                    .Append(slot.DropCommandId).Append(';');
            }
        }
    }
}
