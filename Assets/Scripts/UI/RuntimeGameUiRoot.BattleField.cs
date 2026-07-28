using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimeGameUiRoot
    {
        public RectTransform BattleFieldRoot { get; private set; }
        public RuntimeBattleFieldView BattleFieldView { get; private set; }

        public void BindBattleField(RuntimeBattleFieldPresentation presentation)
        {
            if (BattleFieldView == null)
            {
                throw new System.InvalidOperationException(
                    "RuntimeGameUiRoot.Initialize must be called before binding.");
            }

            BattleFieldView.Bind(
                presentation ?? RuntimeBattleFieldPresentation.Empty);
        }

        private void BuildBattleField(Transform panel)
        {
            GameObject fieldObject = new(
                "BattleField",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RuntimeBattleFieldView));
            fieldObject.transform.SetParent(panel, false);
            BattleFieldRoot = fieldObject.GetComponent<RectTransform>();
            Image background = fieldObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.04f, 0.07f, 0.92f);
            background.raycastTarget = true;
            LayoutElement layout = fieldObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 238f;
            layout.minHeight = 218f;
            BattleFieldView = fieldObject.GetComponent<RuntimeBattleFieldView>();
            BattleFieldView.Initialize(
                commandId => BattleCommandRequested?.Invoke(commandId));
            BattleFieldView.Bind(RuntimeBattleFieldPresentation.Empty);
        }
    }
}
