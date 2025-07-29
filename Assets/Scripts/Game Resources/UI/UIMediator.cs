using CoreResources.Singleton;
using CoreResources.UI;
using GameResources.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameResources.UI
{
    public class UIMediator : DestroyableMonoSingleton<UIMediator>
    {
        #region Serialized Fields
        [SerializeField]
        private List<UIViewManager> _viewManagers = new List<UIViewManager>();
        [SerializeField]
        private float _appCalibrationDistance = 25f;
        #endregion

        #region Overrides
        public override void OnInit()
        {
            for (int i = 0; i < _viewManagers.Count; i++)
            {
                // Won't run if already initialized but
                // we're running it here as a safety check
                _viewManagers[i].ManualInitManager();
                _viewManagers[i].HidePanel();
            }

            GameplayHandler.OnPlayEvent += OnApplicationPlaying;
            GameplayHandler.OnExitEvent += ResetMenus;

            SetMenuStatus(UIViewType.MainMenu, true);
        }

        public override void OnDeInit()
        {
            GameplayHandler.OnPlayEvent -= OnApplicationPlaying;
            GameplayHandler.OnExitEvent -= ResetMenus;

            for (int i = 0; i < _viewManagers.Count; i++)
            {
                // Won't run if already initialized but
                // we're running it here as a safety check
                _viewManagers[i].ManualDeInitManager();
                _viewManagers[i].HidePanel();
            }
        }
        #endregion

        #region Public Methods
        public void SetMenuPositions(Transform cameraTransform, Vector3 origin, Vector3 forward, Quaternion rotation)
        {
            Vector3 finalPosition = origin + forward * _appCalibrationDistance;

            foreach (UIViewManager manager in _viewManagers)
            {
                manager.transform.position = finalPosition;
                manager.transform.rotation = rotation;
            }
        }

        public void ResetMenus()
        {
            HideMenus();

            SetMenuStatus(UIViewType.MainMenu, true);
        }

        public void HideMenus()
        {
            for (int i = 0; i < _viewManagers.Count; i++)
            {
                _viewManagers[i].HidePanel();
                _viewManagers[i].SetMenuInteractability(false);
            }
        }
        #endregion

        #region Private Methods
        private void SetMenuStatus(UIViewType viewType, bool status)
        {
            var currMenu = _viewManagers.FirstOrDefault((elem) => elem.AssignedViewType == viewType);
            currMenu.SetMenuInteractability(status);
            
            if (status)
                currMenu.ShowPanel();
            else
                currMenu.HidePanel();
        }
        #endregion

        #region Event Listeners
        private void OnApplicationPlaying(int appPhase)
        {
            HideMenus();

            switch (appPhase)
            {
                case 1:
                    SetMenuStatus(UIViewType.AppWarmup, true);
                    break;
                case 2:
                    SetMenuStatus(UIViewType.AppP1, true);
                    break;
                case 3:
                    SetMenuStatus(UIViewType.AppP2, true);
                    break;
                case 4:
                    SetMenuStatus(UIViewType.AppP3, true);
                    break;
                case 5:
                    SetMenuStatus(UIViewType.AppP4, true);
                    break;
                default:
                    break;
            }
        }
        #endregion    
    }
}