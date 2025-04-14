using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTStateMachineMediator : MonoSingleton<CTStateMachineMediator>
{
    #region Private Properties
    private CState_StateMachine _fsm;
    #endregion

    #region Subsystems
    [SerializeField]
    private InputManager _inputManager;
    [SerializeField]
    private GameObjectPool _objPool1, _objPool2;
    [SerializeField]
    private CTUIManager _UIManager;
    [SerializeField]
    private CTGameManager _gameManager;
    #endregion

    #region Overrides
    public override void InitSingleton()
    {
        base.InitSingleton();

        InitializeManagers();

        _fsm = new CState_StateMachine();

        _fsm.GoToStateNonHistorically<CTState_InitMenu>();
    }

    public override void CleanSingleton()
    {
        DeInitializeManagers();

        _fsm = null;

        base.CleanSingleton();
    }
    #endregion

    #region Public Methods
    public void StartApplication1()
    {
        if (!IsInstantiated)
            return;

        if (_fsm.CurrentStateType == typeof(CTState_InitMenu))
            _fsm.GoToState<CTState_Application1>();
    }

    public void StartApplication2()
    {
        if (!IsInstantiated)
            return;
        
        if (_fsm.CurrentStateType == typeof(CTState_InitMenu))
            _fsm.GoToState<CTState_Application2>();
    }

    /// <summary>
    /// Function to be called before 
    /// transitioning to another 
    /// application variant or to 
    /// return to main menu
    /// </summary>
    public void QuitCurrentApplication()
    {
        if (!IsInstantiated)
            return;

        if (_fsm.CurrentStateType == typeof(CTState_Application1) || 
            _fsm.CurrentStateType == typeof(CTState_Application2))
            _fsm.GoToPreviousState();
    }

    public void QuitApp()
    {
        if (!IsInstantiated)
            return;

        _fsm.GoToStateNonHistorically<CTState_ApplicationQuit>();
    }
    #endregion

    #region Private Methods
    private void InitializeManagers()
    {
        _inputManager.InitSingleton();
        _objPool1.Init();
        _objPool2.Init();
        _UIManager.InitSingleton();
        _gameManager.InitSingleton();
    }

    private void DeInitializeManagers()
    {
        _inputManager.CleanSingleton();
        _objPool1.DeInit();
        _objPool2.DeInit();
        _UIManager.CleanSingleton();
        _gameManager.CleanSingleton();
    }
    #endregion
}
