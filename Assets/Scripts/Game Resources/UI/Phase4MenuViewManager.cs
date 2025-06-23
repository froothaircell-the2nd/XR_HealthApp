using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;
using Wave.Native;

namespace GameResources.UI
{
    public class Phase4MenuViewManager : UIViewManager<Phase4MenuViewManager, Phase4MenuView>
    {
        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.NextButton.onClick.AddListener(OnNextButtonClicked);

            GameplayHandler.OnPhase4Complete += OnNextButtonEnabled;
        }

        protected override void OnDeInitialize()
        {
            view.ExitButton.onClick.RemoveAllListeners();
            view.NextButton.onClick.RemoveAllListeners();

            GameplayHandler.OnPhase4Complete -= OnNextButtonEnabled;
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

        #region Event Listeners
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

            App_StateMachineMediator.Instance.QuitCurrentAppPhase();
        }

        private void OnNextButtonEnabled()
        {
            view.NextButton.interactable = true;
        }
        #endregion
    }
}