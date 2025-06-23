using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;
using Wave.Native;

namespace GameResources.UI
{
    public class Phase3MenuViewManager : UIViewManager<Phase3MenuViewManager, Phase3MenuView>
    {
        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.NextButton.onClick.AddListener(OnNextButtonClicked);

            GameplayHandler.Instance.OnEnablePhase3NextButton += OnEnableNextButton;
        }

        protected override void OnDeInitialize()
        {
            view.ExitButton.onClick.RemoveAllListeners();
            view.NextButton.onClick.RemoveAllListeners();

            if (GameplayHandler.IsInstantiated)
                GameplayHandler.Instance.OnEnablePhase3NextButton -= OnEnableNextButton;
        }

        public override void OnShowPanel()
        {
            base.OnShowPanel();

            view.NextButton.interactable = false;
            view.ExitButton.interactable = true;
        }

        public override void OnHidePanel()
        {
            base.OnHidePanel();
        }
        #endregion

        private void OnExitButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.QuitCurrentAppPhase();
        }

        private void OnNextButtonClicked()
        {
            GameplayHandler.OnPhase3NextItem?.Invoke();
            view.NextButton.interactable = false;
        }

        private void OnEnableNextButton()
        {
            view.NextButton.interactable = true;
        }
    }
}