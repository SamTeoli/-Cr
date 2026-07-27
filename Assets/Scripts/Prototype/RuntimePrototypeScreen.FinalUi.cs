using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void InitializeFinalUi()
        {
            if (FinalUiRoot != null)
            {
                return;
            }

            FinalUiRoot = gameObject.AddComponent<RuntimeGameUiRoot>();
            FinalUiRoot.NewRunRequested += RequestStartNewRun;
            FinalUiRoot.ContinueRequested += RequestContinueRun;
            FinalUiRoot.RunPreparationCardToggleRequested +=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled += CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed += ConfirmRunPreparation;
            FinalUiRoot.Initialize();
            RefreshFinalUiVisibility();
        }

        private void OnDestroy()
        {
            if (FinalUiRoot == null)
            {
                return;
            }

            FinalUiRoot.NewRunRequested -= RequestStartNewRun;
            FinalUiRoot.ContinueRequested -= RequestContinueRun;
            FinalUiRoot.RunPreparationCardToggleRequested -=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled -= CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed -= ConfirmRunPreparation;
        }

        private bool TryShowFinalUi()
        {
            if (FinalUiRoot == null)
            {
                return false;
            }

            if (pendingRunRequest?.ConfirmationRequired == true)
            {
                SetFinalUiActive(false);
                return false;
            }

            if (runPreparationCards != null && deckSelection.IsOpen)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.RunPreparation);
                return true;
            }

            if (campaign == null || progress == null)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
                return true;
            }

            SetFinalUiActive(false);
            return false;
        }

        private void ToggleFinalRunPreparationCard(string ownedCardId)
        {
            if (runPreparationCards == null ||
                !deckSelection.Toggle(ownedCardId))
            {
                return;
            }

            RefreshFinalRunPreparation();
        }

        private void RefreshFinalRunPreparation()
        {
            if (FinalUiRoot == null || runPreparationCards == null ||
                !deckSelection.IsOpen)
            {
                RefreshFinalUiVisibility();
                return;
            }

            RunDeckSelectionOption[] options =
                deckSelection.CreateOptions(runPreparationCards);
            FinalUiRoot.BindRunPreparation(
                options,
                deckSelection.SelectedCount,
                message,
                deckSelection.SelectedCount > 0);
            SetFinalUiActive(true);
            FinalUiRoot.ShowScreen(RuntimeGameScreen.RunPreparation);
        }

        private void RefreshFinalUiVisibility()
        {
            if (FinalUiRoot == null)
            {
                return;
            }

            if (runPreparationCards != null && deckSelection.IsOpen)
            {
                RefreshFinalRunPreparation();
                return;
            }

            bool showStart = pendingRunRequest == null &&
                             (campaign == null || progress == null);
            SetFinalUiActive(showStart);
            if (showStart)
            {
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
            }
        }

        private void SetFinalUiActive(bool active)
        {
            GameObject canvasObject =
                FinalUiRoot?.RootCanvas?.gameObject;
            if (canvasObject != null && canvasObject.activeSelf != active)
            {
                canvasObject.SetActive(active);
            }
        }
    }
}
