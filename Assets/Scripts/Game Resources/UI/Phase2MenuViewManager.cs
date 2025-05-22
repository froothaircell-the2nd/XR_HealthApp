using CoreResources.UI;
using GameResources.Gameplay;
using GameResources.StateMachine;

namespace GameResources.UI
{
    public class Phase2MenuViewManager : UIViewManager<Phase2MenuViewManager, Phase2MenuView>
    {
        protected override void OnInitialize()
        {
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);

            PhysiologicalDataHandler.OnScoreUpdated += OnScoreUpdated;
        }

        protected override void OnDeInitialize()
        {
            PhysiologicalDataHandler.OnScoreUpdated -= OnScoreUpdated;

            view.ExitButton.onClick.RemoveAllListeners();
        }

        private void OnExitButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.QuitCurrentAppPhase();
        }

        private void OnScoreUpdated(int score)
        {
            var maxScore = PhysiologicalDataHandler.Instance.MaxScore;

            view.ScoreText.text = $"Score: {score} / {maxScore}";
        }
    }
}