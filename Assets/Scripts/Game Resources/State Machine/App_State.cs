using CoreResources.StateMachine;
using GameResources.Gameplay;
using GameResources.Gameplay.VRController;
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
            CursorHandler.Instance.EnableCursorInteraction(true, true);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
            {
                GameplayHandler.OnExitEvent?.Invoke();
                CursorHandler.Instance.DisableCursorInteraction();
            }
        }
    }

    public class AppState_AppPhase2 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(2);
            CursorHandler.Instance.EnableCursorInteraction();
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
            {
                GameplayHandler.OnExitEvent?.Invoke();
                CursorHandler.Instance.DisableCursorInteraction();
            }
        }
    }

    public class AppState_AppPhase3 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(3);
            CursorHandler.Instance.EnableCursorInteraction(false, false);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
            {
                GameplayHandler.OnExitEvent?.Invoke();
                CursorHandler.Instance.DisableCursorInteraction();
            }
        }
    }

    public class AppState_AppPhase4 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(4);
            CursorHandler.Instance.EnableCursorInteraction(true, false);
        }

        public override void OnExit()
        {
            if (NextState != null && NextState.GetType() == typeof(AppState_InitMenu))
            {
                GameplayHandler.OnExitEvent?.Invoke();
                CursorHandler.Instance.DisableCursorInteraction();
            }
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