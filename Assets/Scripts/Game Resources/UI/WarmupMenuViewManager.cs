using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;
using Wave.Native;

namespace GameResources.UI
{
    public class WarmupMenuViewManager : UIViewManager<WarmupMenuViewManager, WarmupMenuView>
    {
        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.NextButton.onClick.AddListener(OnNextButtonClicked);

            GameplayHandler.OnWarmupComplete += OnEnableNextButton;
        }

        protected override void OnDeInitialize()
        {
            view.ExitButton.onClick.RemoveAllListeners();
            view.NextButton.onClick.RemoveAllListeners();

            GameplayHandler.OnWarmupComplete -= OnEnableNextButton;
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
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.StartAppPhase1();
        }

        private void OnEnableNextButton()
        {
            view.NextButton.interactable = true;
        }
    }
}