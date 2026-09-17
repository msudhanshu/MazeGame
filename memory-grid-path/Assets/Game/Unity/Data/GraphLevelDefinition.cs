using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Data
{
    [Serializable]
    public struct GraphNodeEntry
    {
        public string Id;
        public Vector2 NormalizedPosition;
    }

    [Serializable]
    public struct GraphEdgeControlPoint
    {
        public Vector2 NormalizedPosition;
        public float TangentDegrees;
        public float TangentLength;
    }

    [Serializable]
    public struct GraphEdgeEntry
    {
        public string NodeA;
        public string NodeB;
        public List<GraphEdgeControlPoint> ControlPoints;

        public bool HasCurve => ControlPoints != null && ControlPoints.Count > 0;

        public GraphEdgeEntry Clone() =>
            new GraphEdgeEntry
            {
                NodeA = NodeA,
                NodeB = NodeB,
                ControlPoints = CopyControlPoints(ControlPoints)
            };

        public static List<GraphEdgeControlPoint> CopyControlPoints(IReadOnlyList<GraphEdgeControlPoint> points)
        {
            if (points == null || points.Count == 0)
                return new List<GraphEdgeControlPoint>();

            var copy = new List<GraphEdgeControlPoint>(points.Count);
            for (var i = 0; i < points.Count; i++)
                copy.Add(points[i]);
            return copy;
        }

        public static bool ContentEquals(GraphEdgeEntry a, GraphEdgeEntry b)
        {
            if (a.NodeA != b.NodeA || a.NodeB != b.NodeB)
                return false;

            var ac = a.ControlPoints == null ? 0 : a.ControlPoints.Count;
            var bc = b.ControlPoints == null ? 0 : b.ControlPoints.Count;
            if (ac != bc)
                return false;

            for (var i = 0; i < ac; i++)
            {
                var left = a.ControlPoints[i];
                var right = b.ControlPoints[i];
                if (left.NormalizedPosition != right.NormalizedPosition ||
                    !Mathf.Approximately(left.TangentDegrees, right.TangentDegrees) ||
                    !Mathf.Approximately(left.TangentLength, right.TangentLength))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Designer-authored graph level: background art, junction nodes, edges, and path shape.
    /// </summary>
    [CreateAssetMenu(fileName = "GraphLevel", menuName = "Nixin Studio/Memory Grid Path/Graph Level")]
    public sealed class GraphLevelDefinition : ScriptableObject
    {
        [SerializeField] string _displayName = "Graph Level";
        [SerializeField] Texture2D _background;
        [SerializeField] float _imageAspect;
        [SerializeField] float _worldWidth = 12f;
        [SerializeField] List<GraphNodeEntry> _nodes = new List<GraphNodeEntry>();
        [SerializeField] List<GraphEdgeEntry> _edges = new List<GraphEdgeEntry>();
        [SerializeField] string _startNodeId = "n0";
        [SerializeField] string _goalNodeId;
        [SerializeField] int _minPathLength = 4;
        [SerializeField] int _maxPathLength = 12;
        [SerializeField] int _minTurns;
        [SerializeField] int _maxTurns = 8;
        [SerializeField] int _livesPerRun = 3;
        [SerializeField] int _runsPerSession = 5;
        [SerializeField] PathPreviewKind _previewKind = PathPreviewKind.CameraFlash;
        [SerializeField] float _previewSeconds;

        public string DisplayName => _displayName;
        public Texture2D Background => _background;
        public float WorldWidth => _worldWidth;
        public float ImageAspect => _imageAspect > 0.01f ? _imageAspect : AspectOf(_background);
        public IReadOnlyList<GraphNodeEntry> Nodes => _nodes;
        public IReadOnlyList<GraphEdgeEntry> Edges => _edges;
        public string StartNodeId => _startNodeId;
        public string GoalNodeId => string.IsNullOrEmpty(_goalNodeId) ? DefaultGoalId() : _goalNodeId;
        public int LivesPerRun => _livesPerRun;
        public int RunsPerSession => _runsPerSession;
        public PathPreviewKind PreviewKind => _previewKind;
        public float PreviewSeconds =>
            GraphPathPreviewSeconds.Resolve(_previewSeconds, _minPathLength, _minTurns, _previewKind);

        public PathShapeSpec Shape => new PathShapeSpec(_minPathLength, _maxPathLength, _minTurns, _maxTurns);

        public GameConfig ToGameConfig() => GameConfig.Default.WithBudget(_livesPerRun, _runsPerSession);

        public void ApplyLadder(GraphLevelSpec spec)
        {
            _minPathLength = spec.MinPath;
            _maxPathLength = spec.MaxPath;
            _minTurns = spec.MinTurns;
            _maxTurns = spec.MaxTurns;
            _livesPerRun = spec.Lives;
            _runsPerSession = spec.Runs;
            _previewKind = spec.PreviewKind;
            _previewSeconds = spec.PreviewSeconds;
            ClampShapeToTopology();
        }

        public GraphTopology ToTopology()
        {
            ValidateOrThrow();
            return BuildTopology();
        }

        GraphTopology BuildTopology()
        {
            var nodes = new List<GraphNodeId>(_nodes.Count);
            for (var i = 0; i < _nodes.Count; i++)
                nodes.Add(new GraphNodeId(_nodes[i].Id));

            var edges = new List<(GraphNodeId, GraphNodeId)>(_edges.Count);
            var positions = new List<(float X, float Y)>(_nodes.Count);
            for (var i = 0; i < _nodes.Count; i++)
                positions.Add((_nodes[i].NormalizedPosition.x, _nodes[i].NormalizedPosition.y));

            for (var i = 0; i < _edges.Count; i++)
            {
                edges.Add((new GraphNodeId(_edges[i].NodeA), new GraphNodeId(_edges[i].NodeB)));
            }

            return new GraphTopology(nodes, edges, positions);
        }

        public GraphNodeId StartNode => new GraphNodeId(_startNodeId);
        public GraphNodeId GoalNode => new GraphNodeId(GoalNodeId);

        public bool TryValidate(out string error)
        {
            if (_nodes == null || _nodes.Count < 2)
            {
                error = "Add at least two nodes.";
                return false;
            }

            var ids = new HashSet<string>();
            for (var i = 0; i < _nodes.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_nodes[i].Id))
                {
                    error = "Every node needs an id.";
                    return false;
                }

                if (!ids.Add(_nodes[i].Id))
                {
                    error = "Duplicate node id: " + _nodes[i].Id;
                    return false;
                }
            }

            if (!ids.Contains(_startNodeId))
            {
                error = "Start node is missing.";
                return false;
            }

            if (!ids.Contains(GoalNodeId))
            {
                error = "Goal node is missing.";
                return false;
            }

            if (_startNodeId == GoalNodeId)
            {
                error = "Start and goal must differ.";
                return false;
            }

            for (var i = 0; i < _edges.Count; i++)
            {
                if (!ids.Contains(_edges[i].NodeA) || !ids.Contains(_edges[i].NodeB))
                {
                    error = "Edge references unknown node.";
                    return false;
                }
            }

            try
            {
                var topology = BuildTopology();
                if (topology.Degree(StartNode) < 2)
                {
                    error = "Start node needs at least two connections for playable choices.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            error = null;
            return true;
        }

        void ValidateOrThrow()
        {
            if (!TryValidate(out var error))
                throw new InvalidOperationException(error);
        }

        string DefaultGoalId() => _nodes.Count > 0 ? _nodes[_nodes.Count - 1].Id : "n0";

        public static float AspectOf(Texture texture)
        {
            if (texture == null)
                return 1f;

            return texture.width / (float)Mathf.Max(1, texture.height);
        }

        public GraphLevelSnapshot CaptureSnapshot() =>
            new GraphLevelSnapshot
            {
                DisplayName = _displayName,
                Background = _background,
                ImageAspect = _imageAspect,
                WorldWidth = _worldWidth,
                Nodes = CopyNodeList(_nodes),
                Edges = CopyEdgeList(_edges),
                StartNodeId = _startNodeId,
                GoalNodeId = string.IsNullOrEmpty(_goalNodeId) ? DefaultGoalId() : _goalNodeId,
                MinPathLength = _minPathLength,
                MaxPathLength = _maxPathLength,
                MinTurns = _minTurns,
                MaxTurns = _maxTurns,
                LivesPerRun = _livesPerRun,
                RunsPerSession = _runsPerSession,
                PreviewKind = _previewKind,
                PreviewSeconds = _previewSeconds
            };

        public void ApplySnapshot(GraphLevelSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            SetRuntimeData(
                snapshot.DisplayName,
                snapshot.Background,
                snapshot.Nodes,
                snapshot.Edges,
                snapshot.StartNodeId,
                snapshot.GoalNodeId);

            _worldWidth = snapshot.WorldWidth;
            _imageAspect = snapshot.ImageAspect;
            _minPathLength = snapshot.MinPathLength;
            _maxPathLength = snapshot.MaxPathLength;
            _minTurns = snapshot.MinTurns;
            _maxTurns = snapshot.MaxTurns;
            _livesPerRun = snapshot.LivesPerRun;
            _runsPerSession = snapshot.RunsPerSession;
            _previewKind = snapshot.PreviewKind;
            _previewSeconds = snapshot.PreviewSeconds;
        }

        public bool TryValidateSnapshot(GraphLevelSnapshot snapshot, out string error)
        {
            if (snapshot == null)
            {
                error = "No level data.";
                return false;
            }

            var temp = CreateInstance<GraphLevelDefinition>();
            try
            {
                temp.ApplySnapshot(snapshot.Clone());
                return temp.TryValidate(out error);
            }
            finally
            {
                if (Application.isEditor)
                    DestroyImmediate(temp);
            }
        }

        public PathShapeSpec ShapeFromSnapshot(GraphLevelSnapshot snapshot) =>
            new PathShapeSpec(snapshot.MinPathLength, snapshot.MaxPathLength, snapshot.MinTurns, snapshot.MaxTurns);

        public GraphTopology TopologyFromSnapshot(GraphLevelSnapshot snapshot)
        {
            if (!TryValidateSnapshot(snapshot, out var error))
                throw new InvalidOperationException(error);

            var temp = CreateInstance<GraphLevelDefinition>();
            try
            {
                temp.ApplySnapshot(snapshot.Clone());
                return temp.ToTopology();
            }
            finally
            {
                if (Application.isEditor)
                    DestroyImmediate(temp);
            }
        }

        static List<GraphNodeEntry> CopyNodeList(IReadOnlyList<GraphNodeEntry> nodes)
        {
            var copy = new List<GraphNodeEntry>(nodes.Count);
            for (var i = 0; i < nodes.Count; i++)
                copy.Add(nodes[i]);
            return copy;
        }

        static List<GraphEdgeEntry> CopyEdgeList(IReadOnlyList<GraphEdgeEntry> edges)
        {
            var copy = new List<GraphEdgeEntry>(edges.Count);
            for (var i = 0; i < edges.Count; i++)
                copy.Add(edges[i].Clone());
            return copy;
        }

        public void SetRuntimeData(
            string displayName,
            Texture2D background,
            IReadOnlyList<GraphNodeEntry> nodes,
            IReadOnlyList<GraphEdgeEntry> edges,
            string startNodeId,
            string goalNodeId)
        {
            _displayName = displayName;
            _background = background;
            _nodes = DeduplicateNodes(new List<GraphNodeEntry>(nodes), edges, ref startNodeId, ref goalNodeId, out var dedupedEdges);
            _edges = dedupedEdges;
            _startNodeId = startNodeId;
            _goalNodeId = goalNodeId;
        }

        static List<GraphNodeEntry> DeduplicateNodes(
            List<GraphNodeEntry> nodes,
            IReadOnlyList<GraphEdgeEntry> edges,
            ref string startNodeId,
            ref string goalNodeId,
            out List<GraphEdgeEntry> dedupedEdges) =>
            DeduplicateNodesForEditor(nodes, edges, ref startNodeId, ref goalNodeId, out dedupedEdges);

        internal static List<GraphNodeEntry> DeduplicateNodesForEditor(
            List<GraphNodeEntry> nodes,
            IReadOnlyList<GraphEdgeEntry> edges,
            ref string startNodeId,
            ref string goalNodeId,
            out List<GraphEdgeEntry> dedupedEdges)
        {
            dedupedEdges = CopyEdgeList(edges);
            var used = new HashSet<string>();
            var remap = new Dictionary<string, string>();

            for (var i = 0; i < nodes.Count; i++)
            {
                var id = nodes[i].Id;
                if (string.IsNullOrWhiteSpace(id))
                    id = "n" + i;

                if (!used.Contains(id))
                {
                    used.Add(id);
                    if (id != nodes[i].Id)
                    {
                        nodes[i] = new GraphNodeEntry { Id = id, NormalizedPosition = nodes[i].NormalizedPosition };
                    }

                    continue;
                }

                var repaired = NextUniqueId(used);
                used.Add(repaired);
                remap[id] = repaired;
                nodes[i] = new GraphNodeEntry { Id = repaired, NormalizedPosition = nodes[i].NormalizedPosition };
            }

            if (remap.TryGetValue(startNodeId, out var newStart))
                startNodeId = newStart;
            if (remap.TryGetValue(goalNodeId, out var newGoal))
                goalNodeId = newGoal;

            for (var i = 0; i < dedupedEdges.Count; i++)
            {
                var edge = dedupedEdges[i];
                if (remap.TryGetValue(edge.NodeA, out var mappedA))
                    edge.NodeA = mappedA;
                if (remap.TryGetValue(edge.NodeB, out var mappedB))
                    edge.NodeB = mappedB;
                dedupedEdges[i] = edge;
            }

            return nodes;
        }

        static string NextUniqueId(HashSet<string> used)
        {
            var max = -1;
            foreach (var id in used)
            {
                if (id != null && id.StartsWith("n") && int.TryParse(id.Substring(1), out var number))
                    max = Math.Max(max, number);
            }

            for (var i = 0; i <= max + 1; i++)
            {
                var candidate = "n" + i;
                if (!used.Contains(candidate))
                    return candidate;
            }

            return "n" + (max + 1);
        }

        public static GraphLevelDefinition CreatePlayable(int levelNumber, GraphLevelDefinition catalog = null)
        {
            var spec = GraphLevelLadder.For(levelNumber);
            var level = CreateInstance<GraphLevelDefinition>();
            if (catalog != null && catalog._nodes != null && catalog._nodes.Count >= 2)
            {
                level.ApplySnapshot(catalog.CaptureSnapshot());
            }
            else
            {
                var sample = CreateSampleRuntime();
                level.ApplySnapshot(sample.CaptureSnapshot());
                if (Application.isPlaying)
                    Destroy(sample);
                else
                    DestroyImmediate(sample);
            }

            level.ApplyLadder(spec);
            return level;
        }

        void ClampShapeToTopology()
        {
            var cap = _nodes != null ? _nodes.Count : 0;
            if (cap < 2)
                return;

            if (_maxPathLength > cap)
                _maxPathLength = cap;
            if (_minPathLength > _maxPathLength)
                _minPathLength = _maxPathLength;

            var turnCeiling = Math.Max(0, _maxPathLength - 2);
            if (_minTurns > turnCeiling)
                _minTurns = turnCeiling;
            if (_maxTurns > turnCeiling)
                _maxTurns = turnCeiling;
            if (_maxTurns < _minTurns)
                _maxTurns = _minTurns;
        }

        public static GraphLevelDefinition CreateSampleRuntime()
        {
            var level = CreateInstance<GraphLevelDefinition>();
            level._displayName = "Sample Village";
            level._worldWidth = 12f;
            level._minPathLength = 4;
            level._maxPathLength = 8;
            level._maxTurns = 8;
            level._previewKind = PathPreviewKind.CameraFlash;
            level._nodes = new List<GraphNodeEntry>
            {
                new GraphNodeEntry { Id = "n0", NormalizedPosition = new Vector2(0.12f, 0.5f) },
                new GraphNodeEntry { Id = "n1", NormalizedPosition = new Vector2(0.28f, 0.72f) },
                new GraphNodeEntry { Id = "n2", NormalizedPosition = new Vector2(0.28f, 0.28f) },
                new GraphNodeEntry { Id = "n3", NormalizedPosition = new Vector2(0.48f, 0.5f) },
                new GraphNodeEntry { Id = "n4", NormalizedPosition = new Vector2(0.68f, 0.72f) },
                new GraphNodeEntry { Id = "n5", NormalizedPosition = new Vector2(0.68f, 0.28f) },
                new GraphNodeEntry { Id = "n6", NormalizedPosition = new Vector2(0.88f, 0.5f) }
            };
            level._edges = new List<GraphEdgeEntry>
            {
                new GraphEdgeEntry { NodeA = "n0", NodeB = "n1" },
                new GraphEdgeEntry { NodeA = "n0", NodeB = "n2" },
                new GraphEdgeEntry { NodeA = "n1", NodeB = "n3" },
                new GraphEdgeEntry { NodeA = "n2", NodeB = "n3" },
                new GraphEdgeEntry { NodeA = "n3", NodeB = "n4" },
                new GraphEdgeEntry { NodeA = "n3", NodeB = "n5" },
                new GraphEdgeEntry { NodeA = "n4", NodeB = "n6" },
                new GraphEdgeEntry { NodeA = "n5", NodeB = "n6" }
            };
            level._startNodeId = "n0";
            level._goalNodeId = "n6";
            return level;
        }
    }
}
