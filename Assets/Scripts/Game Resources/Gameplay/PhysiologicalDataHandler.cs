using CoreResources.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Gameplay
{
    public class PhysiologicalDataHandler : DestroyableMonoSingleton<PhysiologicalDataHandler>
    {
        private Transform _camHMD;

        private bool _userMeasurementStarted = false;
        private int _score;
        private const int MAX_SCORE = 15;

        public int MaxScore => MAX_SCORE;

        public static Action<int> OnScoreUpdated;

        #region Overrides
        public override void OnInit()
        {
            ResetMetrics();

            GameplayHandler.OnPhase2Hit += IncrementScore;
            GameplayHandler.OnPlayEvent += SetCurrentMeasurementMode;
        }

        public override void OnDeInit()
        {
            ResetMetrics();

            OnScoreUpdated = null;
            
            GameplayHandler.OnPhase2Hit -= IncrementScore;
            GameplayHandler.OnPlayEvent -= SetCurrentMeasurementMode;
        }

        private void FixedUpdate()
        {
            ProcessMetrics();
        }
        #endregion

        #region Public Methods
        public void InitSingleton(Transform camTransform)
        {
            if (camTransform == null)
            {
                Debug.LogError("Injected Camera transform is null!");
                return;
            }

            _camHMD = camTransform;
            InitSingleton();
        }

        public void ResetMetrics()
        {
            _userMeasurementStarted = false;
            _score = 0;

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }
        #endregion

        #region Private Methods
        private void ProcessMetrics()
        {

        }
        #endregion

        #region Event Listeners
        private void IncrementScore()
        {
            _score += 1;

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }

        private void SetCurrentMeasurementMode(int currMode)
        {
            switch (currMode)
            {
                case 0:
                    if (_userMeasurementStarted)
                    {
                        _userMeasurementStarted = false;

                    }
                    break;
                case 1:
                    if (!_userMeasurementStarted)
                    {
                        _userMeasurementStarted = true;
                    }
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}