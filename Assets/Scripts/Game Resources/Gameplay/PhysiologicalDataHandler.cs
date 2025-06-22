using CoreResources.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Gameplay
{
    public class PhysiologicalDataHandler : DestroyableMonoSingleton<PhysiologicalDataHandler>
    {
        private int _score;
        private const int MAX_SCORE = 15;

        public int MaxScore => MAX_SCORE;

        public static Action<int> OnScoreUpdated;

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            ResetMetrics();

            GameplayHandler.OnPhase2Hit += IncrementScore;
        }

        public override void CleanSingleton()
        {
            ResetMetrics();

            OnScoreUpdated = null;
            
            GameplayHandler.OnPhase2Hit -= IncrementScore;

            base.CleanSingleton();
        }
        #endregion

        #region Public Methods
        public void ResetMetrics()
        {
            _score = 0;

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }
        #endregion

        #region Event Listeners
        private void IncrementScore()
        {
            _score += 1;

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }
        #endregion
    }
}