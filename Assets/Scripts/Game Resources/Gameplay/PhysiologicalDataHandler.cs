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

        private string _rootFolderPath;

        private string _responsePathCsv;
        private string _responseDataCsv;
        private bool _userResponseMeasurementStarted = false;
        private Plane _cachedTargetPlane;
        private Vector3 _cachedCenter, _cachedTarget, _cachedTargetPlaneUp;
        private List<(Vector2 point, DateTime timestamp)> _responseBuffer;
        private DateTime _responseStartTime;
        private int _responseIndex = 0;
        private const string RESPONSE_PATH_HEADER = "Timestamp,PosX,PosY\n";
        private const string RESPONSE_DATA_HEADER = "ResponseID,StartTime,DurationSeconds,CenterX,CenterY,TargetX,TargetY\n";
        private const string BUFFER_DATA_KEY_P2RESPONSE = "respBuffer";


        public static Action<int> OnScoreUpdated;

        [Header("Data Collection Settings")]
        [SerializeField] private int _bufferSize_HMDPosRot = 60;
        [SerializeField] private int _bufferSize_AppP2Response = 60;

        private List<string> _dataBuffer = new List<string>();
        private string _hmdPosRotCsvPath;
        private string _fileTime;
        private const string HMDPOSROT_HEADER = "Timestamp,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW\n";
        private const string BUFFER_DATA_KEY_HMDPOSROT = "bufferDataHMD";


        #region Overrides
        public override void OnInit()
        {
            ResetMetrics();

            GameplayHandler.OnPhase2Hit += OnTargetHit_AppP2;
            GameplayHandler.OnPlayEvent += SetCurrentMeasurementMode;

            _rootFolderPath = Path.Combine(Application.persistentDataPath, "PhysioLogs");
            if (!Directory.Exists(_rootFolderPath))
                Directory.CreateDirectory(_rootFolderPath);

            _fileTime = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }

        public override void OnDeInit()
        {
            ResetMetrics();

            OnScoreUpdated = null;

            TaskUtilitiesManager.CancelTask(WriteDataAsync_HMDPosRot);

            GameplayHandler.OnPhase2Hit -= OnTargetHit_AppP2;
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
            _userResponseMeasurementStarted = false;

            _cachedCenter = _cachedTarget = default;
            _cachedTargetPlane = default;

            _score = 0;
            _responseIndex = 0;
            _dataBuffer.Clear();

            if (GameplayHandler.Instance.Phase >= (AppPhase) 1)
                OnScoreUpdated?.Invoke(_score);
        }

        public void RecordViewingAngleBounds_Sorted(Vector3 center, List<Vector3> viewLimits)
        {
            if (_camHMD == null)
            {
                Debug.LogError("CamHMD is not set.");
                return;
            }

            if (viewLimits == null || viewLimits.Count != 8)
            {
                Debug.LogError("Expected 8 view limit edge points.");
                return;
            }

            // Direction labels in fixed order
            string[] directionLabels = new[]
            {
                "Top", "TopRight", "Right", "BottomRight",
                "Bottom", "BottomLeft", "Left", "TopLeft"
            };

            Vector3 camPosition = _camHMD.position;
            Vector3 forward = _camHMD.forward;

            List<float> angles = new List<float>();

            for (int i = 0; i < viewLimits.Count; i++)
            {
                Vector3 toEdge = (viewLimits[i] - camPosition).normalized;
                float angle = Vector3.Angle(forward, toEdge);
                angles.Add(angle);
            }

            // Create CSV contents
            string timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            StringBuilder sb = new StringBuilder();

            // Header
            sb.Append("Timestamp");
            foreach (string label in directionLabels)
                sb.Append($",{label}");
            sb.AppendLine();

            // Data
            sb.Append(timestamp);
            foreach (float angle in angles)
                sb.Append($",{angle:F2}");
            sb.AppendLine();

            // Write to file
            string folder = Path.Combine(Application.persistentDataPath, "PhysioLogs");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filename = $"ViewLimits_{_fileTime}.csv";
            string viewLimitsPath = Path.Combine(folder, filename);

            File.WriteAllText(viewLimitsPath, sb.ToString());
        }

        public void MeasureUserResponse_AppP2(Vector3 center, Vector3 target, Vector3 up, Plane targetPlane)
        {
            if (!_userResponseMeasurementStarted)
            {
                _userResponseMeasurementStarted = true;

                _cachedTargetPlane = targetPlane;
                _cachedCenter = center;
                _cachedTarget = target;
                _cachedTargetPlaneUp = up;

                _responseBuffer = new List<(Vector2, DateTime)>();
                _responseStartTime = DateTime.UtcNow;

                // Build file paths for this response:
                string idx = _responseIndex.ToString();
                _responsePathCsv = Path.Combine(_rootFolderPath,
                    $"ResponsePath_{idx}_{_fileTime}.csv");
                _responseDataCsv = Path.Combine(_rootFolderPath,
                    $"ResponseData_{idx}_{_fileTime}.csv");

                File.WriteAllText(_responsePathCsv, RESPONSE_PATH_HEADER);
                File.WriteAllText(_responseDataCsv, RESPONSE_DATA_HEADER);
            }
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

            if (_dataBuffer.Count >= _bufferSize_HMDPosRot)
            {
                // Copy and clear buffer
                List<string> toWrite = new List<string>(_dataBuffer);
                _dataBuffer.Clear();

                UniTaskContext context = new UniTaskContext();

                context.Set(BUFFER_DATA_KEY_HMDPOSROT, toWrite);

                TaskUtilitiesManager.RunTask(WriteDataAsync_HMDPosRot, context);
            }

            if (_userResponseMeasurementStarted)
            {
                // Store Response Path Here
                Ray ray = new Ray(_camHMD.position, _camHMD.forward);
                if (_cachedTargetPlane.Raycast(ray, out float enter))
                {
                    Vector3 hit = ray.GetPoint(enter);

                    Vector3 normal = _cachedTargetPlane.normal;
                    Vector3 right = Vector3.Cross(normal, _cachedTargetPlaneUp).normalized;
                    Vector3 up = Vector3.Cross(right, normal).normalized;

                    Vector3 localOffset = hit - _cachedCenter;
                    Vector2 p2 = new Vector2(Vector3.Dot(localOffset, right), Vector3.Dot(localOffset, up));

                    DateTime ts = DateTime.UtcNow;
                    _responseBuffer.Add((p2, ts));

                    if (_responseBuffer.Count >= _bufferSize_AppP2Response)
                    {
                        FlushResponseBuffer();
                    }
                }
            }
        }

        private async UniTask WriteDataAsync_HMDPosRot(CancellationToken token, UniTaskContext context)
        {
            try
            {
                if (context.TryGet<List<string>>(BUFFER_DATA_KEY_HMDPOSROT, out var entries))
                {
                    var sb = new StringBuilder();
                    foreach (var line in entries)
                        sb.AppendLine(line);

                    using (StreamWriter writer = new StreamWriter(_hmdPosRotCsvPath, append: true))
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

        private void FlushResponseBuffer()
        {
            var copy = new List<(Vector2, DateTime)>(_responseBuffer);
            _responseBuffer.Clear();

            UniTaskContext ctx = new UniTaskContext();
            ctx.Set(BUFFER_DATA_KEY_P2RESPONSE, copy);

            TaskUtilitiesManager.RunTask(FlushResponseBuffer_Async, ctx);
        }
        
        private async UniTask FlushResponseBuffer_Async(CancellationToken token, UniTaskContext context)
        {
            if (context.TryGet<List<(Vector2, DateTime)>>(BUFFER_DATA_KEY_P2RESPONSE, out var list))
            {
                var sb = new StringBuilder();
                foreach (var (pt, ts) in list)
                    sb.AppendLine($"{ts:o},{pt.x:F4},{pt.y:F4}");

                using (var writer = new StreamWriter(_responsePathCsv, append: true))
                {
                    await writer.WriteAsync(sb.ToString());
                }
            }
        }
        #endregion

        #region Event Listeners
        private void OnTargetHit_AppP2()
        {
            if (!_userResponseMeasurementStarted)
                return;

            _score += 1;

            FlushResponseBuffer();

            DateTime end = DateTime.UtcNow;
            double duration = (end - _responseStartTime).TotalSeconds;

            // 2D center and target projection
            Vector3 normal = _cachedTargetPlane.normal;
            Vector3 right = Vector3.Cross(normal, _cachedTargetPlaneUp).normalized;
            Vector3 up = Vector3.Cross(right, normal).normalized;

            Vector2 center2D = Vector2.zero;
            Vector3 offT = _cachedTarget - _cachedCenter;
            Vector2 target2D = new Vector2(Vector3.Dot(offT, right), Vector3.Dot(offT, up));

            string startTime = _responseStartTime.ToString("o", CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.AppendLine($"{_responseIndex},{startTime},{duration:F4},{center2D.x:F4},{center2D.y:F4},{target2D.x:F4},{target2D.y:F4}");

            File.AppendAllText(_responseDataCsv, sb.ToString());

            _userResponseMeasurementStarted = false;
            _responseIndex++;

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
                        string filename = $"HeadPosRot_{_fileTime}.csv";
                        _hmdPosRotCsvPath = Path.Combine(_rootFolderPath, filename);
                        File.WriteAllText(_hmdPosRotCsvPath, HMDPOSROT_HEADER); // Header

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