using CoreResources.UI;
using GameResources.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.UI
{
    public class Phase1MenuViewManager : UIViewManager<Phase1MenuViewManager, Phase1MenuView>
    {
        #region Overrides 
        protected override void OnInitialize()
        {
            view.ContinueButton.onClick.AddListener(OnContinueButtonClicked);
            view.ExitButton.onClick.AddListener(OnExitButtonClicked);
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
        private void OnContinueButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.StartAppPhase2();
        }

        private void OnExitButtonClicked()
        {
            if (!App_StateMachineMediator.IsInstantiated)
                return;

            App_StateMachineMediator.Instance.QuitCurrentAppPhase();
        }
        #endregion

        #region Private Methods
        private void ResetMenu()
        {
        }
        #endregion
    }
}