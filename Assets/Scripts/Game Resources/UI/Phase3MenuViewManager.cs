using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;
using Wave.Native;

namespace GameResources.UI
{
    public class Phase3MenuViewManager : UIViewManager<Phase3MenuViewManager, Phase3MenuView>
    {
        private bool _phase3Complete = false;

        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.NextButton.onClick.AddListener(OnNextButtonClicked);

            GameplayHandler.OnEnablePhase3NextButton += OnEnableNextButton;
            GameplayHandler.OnPhase3Complete += OnPhase3Complete;
        }

        protected override void OnDeInitialize()
        {
            view.ExitButton.onClick.RemoveAllListeners();
            view.NextButton.onClick.RemoveAllListeners();

            if (GameplayHandler.IsInstantiated)
            {
                GameplayHandler.OnEnablePhase3NextButton -= OnEnableNextButton;
                GameplayHandler.OnPhase3Complete -= OnPhase3Complete;
            }
        }

        public override void OnShowPanel()
        {
            base.OnShowPanel();

            view.NextButton.interactable = false;
            view.ExitButton.interactable = true;

            _phase3Complete = false;
        }

        public override void OnHidePanel()
        {
            _phase3Complete = false;

            base.OnHidePanel();
        }
        #endregion

        private void OnExitButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.GoToMenu();
        }

        private void OnNextButtonClicked()
        {
            if (_phase3Complete)
            {
                if (!App_StateMachineMediator.IsInstantiated)
                    return;

                App_StateMachineMediator.Instance.StartAppPhase4();

                return;
            }

            GameplayHandler.OnPhase3NextItem?.Invoke();
            view.NextButton.interactable = false;
        }

        private void OnEnableNextButton()
        {
            view.NextButton.interactable = true;
        }

        private void OnPhase3Complete()
        {
            _phase3Complete = true;
        }
    }
}