using CoreResources.UI;
using GameResources.StateMachine;

namespace GameResources.UI
{
    public class MainMenuViewManager : UIViewManager<MainMenuViewManager, MainMenuView>
    {
        #region Overrides 
        protected override void OnInitialize()
        {
            view.PlayButton.onClick.AddListener(OnPlayButtonClicked);
            view.QuitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        protected override void OnDeInitialize()
        {
            base.DeInitializeManager();
        }

        public override void OnShowPanel()
        {
            ResetMenu();
        }

        public override void OnHidePanel()
        {
            ResetMenu();
        }
        #endregion

        #region Input Listeners
        private void OnPlayButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.StartAppWarmup();
        }

        private void OnQuitButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.QuitApp();
        }
        #endregion

        #region Private Methods
        private void ResetMenu()
        {
        }
        #endregion
    }
}