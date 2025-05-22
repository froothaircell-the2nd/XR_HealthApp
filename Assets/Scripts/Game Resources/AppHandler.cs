using CoreResources.Singleton;
using GameResources.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources
{
    public class AppHandler : MonoSingleton<AppHandler>
    {
        #region Private Properties
        [SerializeField] private App_StateMachineMediator _mediator;
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();
        }

        public override void CleanSingleton()
        {
            base.CleanSingleton();
        }
        #endregion
    }
}