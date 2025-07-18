using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;

namespace GameResources.UI
{
    public class Phase2MenuViewManager : UIViewManager<Phase2MenuViewManager, Phase2MenuView>
    {
        #region Overrides
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
            view.ContinueButton.onClick.AddListener(OnContinueButtonClicked);

            PhysiologicalDataHandler.OnScoreUpdated += OnScoreUpdated;
            GameplayHandler.OnPhase2Complete += OnContinueButtonEnabled;
        }

        protected override void OnDeInitialize()
        {
            PhysiologicalDataHandler.OnScoreUpdated -= OnScoreUpdated;
            GameplayHandler.OnPhase2Complete -= OnContinueButtonEnabled;

            view.ExitButton.onClick.RemoveAllListeners();
            view.ContinueButton.onClick.RemoveAllListeners();
        }

        public override void OnShowPanel()
        {
            base.OnShowPanel();

            view.ContinueButton.interactable = false;
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

            App_StateMachineMediator.Instance.GoToMenu();
        }

        private void OnContinueButtonEnabled()
        {
            view.ContinueButton.interactable = true;
        }

        private void OnContinueButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.StartAppPhase3();
        }

        private void OnScoreUpdated(int score)
        {
            view.ScoreText.text = $"Score: {score}";
        }
    }
}