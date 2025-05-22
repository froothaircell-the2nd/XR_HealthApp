using CoreResources.StateMachine;
using GameResources.Gameplay;
using GameResources.UI;
using UnityEngine;

namespace GameResources.StateMachine
{
    public class App_State : StateHistory<App_StateMachine, App_State>
    {
    }

    /// <summary>
    /// First state to load, initializes menus 
    /// and application specifics.
    /// </summary>
    public class AppState_InitMenu : App_State
    {
        public override void OnEnter()
        {
            UIMediator.Instance.ResetMenus();
        }

        public override void OnExit()
        {
        }
    }

    /// <summary>
    /// State for starting application 1, 
    /// automatically closes application 2
    /// on entry.
    /// </summary>
    public class AppState_AppPhase1 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(1);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
                GameplayHandler.OnExitEvent?.Invoke();
        }
    }

    public class AppState_AppPhase2 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(2);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
                GameplayHandler.OnExitEvent?.Invoke();
        }
    }

    public class AppState_AppPhase3 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(3);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
                GameplayHandler.OnExitEvent?.Invoke();
        }
    }

    /// <summary>
    /// State loaded before application 
    /// quitting. Handles application cleanup.
    /// </summary>
    public class AppState_ApplicationQuit : App_State
    {
        public override void OnEnter()
        {
            Application.Quit();
        }

        public override void OnExit()
        {

        }
    }
}