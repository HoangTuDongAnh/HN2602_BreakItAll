using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using _Game.Scripts.Logic;
using _Game.Scripts.Logic.Spawn;

namespace _Game.Editor
{
    public class SpawnSimulatorWindow : EditorWindow
    {
        private SmartSpawnStrategy _strategy;
        private int _batchSize = 3;
        private int _simulationCount = 1000;
        private float _fillRate = 0.25f;
        private bool _isArcade;
        private bool _resetStrategyBeforeRun = true;
        private Vector2 _scroll;
        private readonly Dictionary<string, int> _shapeCounts = new Dictionary<string, int>();
        private SpawnDebugReport _lastReport;

        [MenuItem("Tools/_Game/Spawn Simulator")]
        [MenuItem("Tools/Block Blast/Spawn Simulator")]
        public static void Open()
        {
            GetWindow<SpawnSimulatorWindow>("Spawn Simulator");
        }

        private void OnEnable()
        {
            if (_strategy == null)
                _strategy = FindFirstStrategy();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Smart Spawn Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Dùng tool này để chạy nhiều lần SmartSpawnStrategy với board giả lập, xem distribution và debug candidate weight.", MessageType.Info);

            _strategy = (SmartSpawnStrategy)EditorGUILayout.ObjectField("Strategy", _strategy, typeof(SmartSpawnStrategy), false);
            _batchSize = EditorGUILayout.IntSlider("Batch Size", _batchSize, 1, 5);
            _simulationCount = EditorGUILayout.IntSlider("Simulation Count", _simulationCount, 1, 5000);
            _fillRate = EditorGUILayout.Slider("Board Fill Rate", _fillRate, 0f, 0.95f);
            _isArcade = EditorGUILayout.Toggle("Arcade Context", _isArcade);
            _resetStrategyBeforeRun = EditorGUILayout.Toggle("Reset Runtime State", _resetStrategyBeforeRun);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find Strategy"))
                    _strategy = FindFirstStrategy();

                GUI.enabled = _strategy != null;
                if (GUILayout.Button("Run Simulation"))
                    RunSimulation();

                if (GUILayout.Button("Build One Debug Report"))
                    BuildOneReport();
                GUI.enabled = true;
            }

            DrawResults();
        }

        private void RunSimulation()
        {
            _shapeCounts.Clear();
            _lastReport = null;

            if (_strategy == null)
                return;

            if (_resetStrategyBeforeRun)
                _strategy.ResetRuntimeState();

            SpawnRuntimeConfig config = new SpawnRuntimeConfig();
            BoardStateSnapshot board = BuildRandomBoard(9, 9, _fillRate);
            SpawnSelectionContext context = new SpawnSelectionContext(board, config, _isArcade);

            for (int i = 0; i < _simulationCount; i++)
            {
                List<SpawnRequest> batch = _strategy.GetNextBatch(_batchSize, context);
                for (int b = 0; b < batch.Count; b++)
                {
                    string id = batch[b].ShapeData != null ? batch[b].ShapeData.Id : "<null>";
                    _shapeCounts.TryGetValue(id, out int count);
                    _shapeCounts[id] = count + 1;
                }
            }

            _lastReport = _strategy.LastDebugReport;
        }

        private void BuildOneReport()
        {
            if (_strategy == null)
                return;

            SpawnRuntimeConfig config = new SpawnRuntimeConfig();
            BoardStateSnapshot board = BuildRandomBoard(9, 9, _fillRate);
            SpawnSelectionContext context = new SpawnSelectionContext(board, config, _isArcade);
            _lastReport = _strategy.BuildDebugReport(_batchSize, context, board.FillRate, 60);
        }

        private void DrawResults()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_shapeCounts.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Distribution", EditorStyles.boldLabel);
                int total = 0;
                foreach (KeyValuePair<string, int> item in _shapeCounts)
                    total += item.Value;

                List<KeyValuePair<string, int>> rows = new List<KeyValuePair<string, int>>(_shapeCounts);
                rows.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (KeyValuePair<string, int> row in rows)
                {
                    float percent = total > 0 ? row.Value * 100f / total : 0f;
                    EditorGUILayout.LabelField(row.Key, $"{row.Value} ({percent:0.0}%)");
                }
            }

            if (_lastReport != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Last Debug Report", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Summary", _lastReport.Summary);
                EditorGUILayout.LabelField("Fill Rate", _lastReport.FillRate.ToString("0.00"));
                EditorGUILayout.LabelField("Raw Pool", _lastReport.RawPoolCount.ToString());
                EditorGUILayout.LabelField("Filtered Pool", _lastReport.FilteredPoolCount.ToString());

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);
                for (int i = 0; i < _lastReport.Selected.Count; i++)
                    DrawCandidate(_lastReport.Selected[i]);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Top Candidates", EditorStyles.boldLabel);
                for (int i = 0; i < _lastReport.Candidates.Count; i++)
                    DrawCandidate(_lastReport.Candidates[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCandidate(SpawnDebugCandidate candidate)
        {
            if (candidate == null)
                return;

            EditorGUILayout.LabelField(
                $"{candidate.ShapeId} r{candidate.RotationIndex}",
                $"w={candidate.Weight:0.00}, fit={candidate.FitCount}, clear={candidate.ClearPotential}, cells={candidate.CellCount}, {candidate.Tier}");
        }

        private static SmartSpawnStrategy FindFirstStrategy()
        {
            string[] guids = AssetDatabase.FindAssets("t:SmartSpawnStrategy");
            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SmartSpawnStrategy>(path);
        }

        private static BoardStateSnapshot BuildRandomBoard(int width, int height, float fillRate)
        {
            bool[,] occupied = new bool[width, height];
            float safeFill = Mathf.Clamp01(fillRate);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    occupied[x, y] = Random.value < safeFill;
            }

            return new BoardStateSnapshot(occupied, width, height);
        }
    }
}
