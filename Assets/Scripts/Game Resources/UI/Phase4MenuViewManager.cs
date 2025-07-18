using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;
using Wave.Native;

namespace GameResources.UI
{
    public class Phase4MenuViewManager : UIViewManager<Phase4MenuViewManager, Phase4MenuView>
    {
        private bool _phase4Complete = false;

        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.NextButton.onClick.AddListener(OnNextButtonClicked);

            GameplayHandler.OnEnablePhase4NextButton += OnEnableNextButton;
            GameplayHandler.OnPhase4Complete += OnPhase4Complete;
        }

        protected override void OnDeInitialize()
        {
            view.ExitButton.onClick.RemoveAllListeners();
            view.NextButton.onClick.RemoveAllListeners();

            GameplayHandler.OnEnablePhase4NextButton -= OnEnableNextButton;
            GameplayHandler.OnPhase4Complete -= OnPhase4Complete;
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

            App_StateMachineMediator.Instance.GoToMenu();
        }

        private void OnNextButtonClicked()
        {
            if (_phase4Complete)
            {
                if (!App_StateMachineMediator.IsInstantiated)
                    return;

                App_StateMachineMediator.Instance.GoToMenu();
                return;
            }

            GameplayHandler.OnPhase4NextItem?.Invoke();
            view.NextButton.interactable = false;
        }

        private void OnEnableNextButton()
        {
            view.NextButton.interactable = true;
        }

        private void OnPhase4Complete()
        {
            _phase4Complete = true;
        }
        #endregion
    }
}