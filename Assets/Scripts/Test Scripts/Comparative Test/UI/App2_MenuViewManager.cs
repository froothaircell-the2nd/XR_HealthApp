using CoreResources.UI;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class App2_MenuViewManager : UIViewManager<App2_MenuViewManager, App2_MenuView>
{
    #region Overrides 
    protected override void OnInitialize()
    {
        view.PlayButton.onClick.AddListener(OnPlayButtonClicked);
        view.QuitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    protected override void OnDeInitialize()
    {
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
        if (!CTStateMachineMediator.IsInstantiated)
            return;

        CTStateMachineMediator.Instance.StartApplication2();
        SetMode(true);
    }

    private void OnQuitButtonClicked()
    {
        if (!CTStateMachineMediator.IsInstantiated)
            return;

        CTStateMachineMediator.Instance.QuitCurrentApplication();
        SetMode(false);
    }
    #endregion

    #region Private Methods
    private void ResetMenu()
    {
        SetMode(false);
    }

    private void SetMode(bool inGameplay)
    {
        view.PlayButton.interactable = !inGameplay;
        view.PlayButton.gameObject.SetActive(!inGameplay);

        view.QuitButton.interactable = inGameplay;
        view.QuitButton.gameObject.SetActive(inGameplay);
    }
    #endregion
}
