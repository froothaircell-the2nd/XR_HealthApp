using CoreResources.Managers;
using CoreResources.Singleton;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
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

        [Header("Data Collection Settings")]
        [SerializeField] private int _bufferSizeBeforeWrite = 60;

        private List<string> _dataBuffer = new List<string>();
        private string _csvPath;

        private const string BUFFER_DATA_KEY = "bufferData";

        #region Overrides
        public override void OnInit()
        {
            ResetMetrics();

            GameplayHandler.OnPhase2Hit += IncrementScore;
            GameplayHandler.OnPlayEvent += SetCurrentMeasurementMode;

            string folder = Path.Combine(Application.persistentDataPath, "PhysioLogs");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filename = $"PhysioLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            _csvPath = Path.Combine(folder, filename);

            File.WriteAllText(_csvPath, "Timestamp,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW\n"); // Header
        }

        public override void OnDeInit()
        {
            ResetMetrics();

            OnScoreUpdated = null;

            TaskUtilitiesManager.CancelTask(WriteDataAsync);

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
            _dataBuffer.Clear();

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }
        #endregion

        #region Private Methods
        private void ProcessMetrics()
        {
            if (!_userMeasurementStarted)
                return;

            // Capture current state
            Vector3 pos = _camHMD.position;
            Quaternion rot = _camHMD.rotation;
            string timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); // ISO 8601

            string entry = $"{timestamp},{pos.x:F4},{pos.y:F4},{pos.z:F4},{rot.x:F4},{rot.y:F4},{rot.z:F4},{rot.w:F4}";
            _dataBuffer.Add(entry);

            if (_dataBuffer.Count >= _bufferSizeBeforeWrite)
            {
                // Copy and clear buffer
                List<string> toWrite = new List<string>(_dataBuffer);
                _dataBuffer.Clear();

                UniTaskContext context = new UniTaskContext();

                context.Set(BUFFER_DATA_KEY, toWrite);

                TaskUtilitiesManager.RunTask(WriteDataAsync, context);
            }
        }

        private async UniTask WriteDataAsync(CancellationToken token, UniTaskContext context)
        {
            try
            {
                if (context.TryGet<List<string>>(BUFFER_DATA_KEY, out var entries))
                {
                    var sb = new StringBuilder();
                    foreach (var line in entries)
                        sb.AppendLine(line);

                    using (StreamWriter writer = new StreamWriter(_csvPath, append: true))
                    {
                        await writer.WriteAsync(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PhysioData] Error writing to file: {ex.Message}");
            }
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