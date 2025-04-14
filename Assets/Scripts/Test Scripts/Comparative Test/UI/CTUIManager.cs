using CoreResources.Singleton;
using CoreResources.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CTUIManager : DestroyableMonoSingleton<CTUIManager>
{
    #region Serialized Fields
    [SerializeField]
    private List<UIViewManager> _viewManagers = new List<UIViewManager>();
    #endregion

    #region Overrides
    public override void OnInit()
    {
        for (int i = 0; i < _viewManagers.Count; i++)
        {
            // Won't run if already initialized but
            // we're running it here as a safety check
            _viewManagers[i].ManualInitManager(); 
            _viewManagers[i].ShowPanel();
        }

        CTGameManager.OnPlayEvent += OnApplicationPlaying;
        CTGameManager.OnExitEvent += ResetMenus;
    }

    public override void OnDeInit()
    {
        CTGameManager.OnPlayEvent -= OnApplicationPlaying;
        CTGameManager.OnExitEvent -= ResetMenus;

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
    public void ResetMenus()
    {
        for (int i = 0; i < _viewManagers.Count; i++)
        {
            _viewManagers[i].ShowPanel();
            _viewManagers[i].SetMenuInteractability(true);
        }
    }
    #endregion

    #region Event Listeners
    private void OnApplicationPlaying(bool isApp1)
    {
        UIViewManager currView;
        UIViewManager otherView;

        if (isApp1)
        {
            currView = _viewManagers.First((elem) => elem.AssignedViewType == UIViewType.App1);
            otherView = _viewManagers.First((elem) => elem.AssignedViewType == UIViewType.App2);
        }
        else
        {
            currView = _viewManagers.First((elem) => elem.AssignedViewType == UIViewType.App2);
            otherView = _viewManagers.First((elem) => elem.AssignedViewType == UIViewType.App1);
        }

        currView.SetMenuInteractability(true);
        otherView.SetMenuInteractability(false);
    }
    #endregion
}
