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

            BattleScreenSnapshot snapshot =
                battleScreen.CreateSnapshot(progress, campaign);
            RuntimeBattleFieldPresentation presentation =
                RuntimeBattleFieldPresentation.FromSnapshot(snapshot);
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
            fieldLayout.preferredHeight = 210f;
            fieldLayout.minHeight = 198f;

            finalBattleFieldView =
                finalBattleFieldObject.GetComponent<RuntimeBattleFieldView>();
            finalBattleFieldView.Initialize(ExecuteFinalBattleCommand);
            finalBattleFieldView.Bind(RuntimeBattleFieldPresentation.Empty);

            int fieldIndex = FinalUiRoot.BattleCardPlayZone == null
                ? 0
                : FinalUiRoot.BattleCardPlayZone.GetSiblingIndex() + 1;
            finalBattleFieldObject.transform.SetSiblingIndex(fieldIndex);

            Transform handScroll =
                FinalUiRoot.BattleHandCardList?.transform.parent?.parent;
            if (handScroll != null)
            {
                handScroll.SetSiblingIndex(fieldIndex + 1);
                LayoutElement handLayout =
                    handScroll.GetComponent<LayoutElement>();
                if (handLayout != null)
                {
                    handLayout.preferredHeight = 210f;
                }
            }

            Transform commandScroll =
                FinalUiRoot.BattleCommandList?.transform.parent?.parent;
            LayoutElement commandLayout =
                commandScroll?.GetComponent<LayoutElement>();
            if (commandLayout != null)
            {
                commandLayout.preferredHeight = 90f;
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
        return false;
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
