using CoreResources.Managers.InputManagement;
using CoreResources.StateMachine;
using GameResources.Gameplay;
using GameResources.Gameplay.VRController;
using GameResources.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameResources.StateMachine
{
    public class App_State : StateHistory<App_StateMachine, App_State>
    {
    }

    public class AppState_AppCalibration : App_State
    {
        public override void OnEnter()
        {
            InputManager.InputActions.XRIRightHandInteraction.Select.performed += OnCalibrationComplete;
        }

        public override void OnExit()
        {
            if (!BlackoutScreenHandler.IsInstantiated)
            {
                Debug.LogError("Blackout Screen not instantiated!");
                return;
            }

            BlackoutScreenHandler.Instance.SetBlackoutScreen(false);
            InputManager.InputActions.XRIRightHandInteraction.Select.performed -= OnCalibrationComplete;
        }

        private void OnCalibrationComplete(InputAction.CallbackContext obj)
        {
            if (!BlackoutScreenHandler.IsInstantiated)
            {
                Debug.LogError("Blackout Screen not instantiated!");
                return;
            }

            GameplayHandler.Instance.CalibrateSceneToCameraOrientation();
            App_StateMachineMediator.Instance.GoToMenu();
        }
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

    public class AppState_Warmup : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(1);
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

    public class AppState_AppPhase1 : App_State
    {
        public override void OnEnter()
        {
            GameplayHandler.OnPlayEvent?.Invoke(2);
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
            GameplayHandler.OnPlayEvent?.Invoke(3);
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
            GameplayHandler.OnPlayEvent?.Invoke(4);
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
            GameplayHandler.OnPlayEvent?.Invoke(5);
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