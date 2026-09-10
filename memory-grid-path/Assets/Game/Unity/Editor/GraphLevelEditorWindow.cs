using System.Collections.Generic;
using Game.Core.Domain;
using Game.Unity.Data;
using Game.Unity.Graph;
using Nixin.Game.Core;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    public sealed class GraphLevelEditorWindow : EditorWindow
    {
        enum ToolMode
        {
            AddNode,
            Move,
            Connect,
            Curve,
            SetStart,
            SetGoal,
            Delete
        }

        const float NodeHandleSize = 16f;
        const float ControlHandleSize = 12f;
        const float TangentHandleSize = 10f;
        const float EdgeHitPixels = 8f;

        GraphLevelDefinition _asset;
        GraphLevelSnapshot _draft;
        GraphLevelSnapshot _saved;
        ToolMode _tool = ToolMode.AddNode;
        string _selectedNodeId;
        string _connectFromId;
        string _dragNodeId;
        int _selectedEdgeIndex = -1;
        int _selectedControlIndex = -1;
        int _dragEdgeIndex = -1;
        int _dragControlIndex = -1;
        bool _dragTangent;
        Rect _canvasRect;
        Rect _imageRect;
        Vector2 _scroll;
        int _previewSeed = 7;
        readonly List<GraphNodeId> _previewPath = new List<GraphNodeId>();
        string _validationMessage = string.Empty;

        bool IsDirty => !GraphLevelSnapshot.AreEqual(_draft, _saved);

        [MenuItem("Nixin Studio/Memory Grid Path/Graph Level Editor")]
        public static void Open()
        {
            var window = GetWindow<GraphLevelEditorWindow>("Graph Level Editor");
            window.Show();
        }

        [InitializeOnEnterPlayMode]
        static void SaveOpenDraftOnPlay(EnterPlayModeOptions _)
        {
            var windows = Resources.FindObjectsOfTypeAll<GraphLevelEditorWindow>();
            for (var i = 0; i < windows.Length; i++)
                windows[i].SaveDraftIfDirty();
        }

        void SaveDraftIfDirty()
        {
            if (IsDirty)
                SaveDraft();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Graph Level Editor", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var nextAsset = (GraphLevelDefinition)EditorGUILayout.ObjectField("Level Asset", _asset, typeof(GraphLevelDefinition), false);
            if (EditorGUI.EndChangeCheck())
                TrySwitchAsset(nextAsset);

            DrawSaveBar();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New"))
                CreateNewLevel();
            if (GUILayout.Button("Create Sample Village"))
                CreateSampleLevelAsset();
            if (GUILayout.Button("Repair Duplicate Ids"))
                RepairDuplicateIds();
            EditorGUILayout.EndHorizontal();

            if (_asset == null || _draft == null)
            {
                EditorGUILayout.HelpBox("Assign or create a Graph Level Definition asset.", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawValidation();
            DrawCanvas();
            DrawNodeList();
        }

        void DrawSaveBar()
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!IsDirty))
            {
                if (GUILayout.Button("Save", GUILayout.Height(24f)))
                    SaveDraft();
            }

            using (new EditorGUI.DisabledScope(!IsDirty))
            {
                if (GUILayout.Button("Cancel", GUILayout.Height(24f)))
                    CancelDraft();
            }

            if (IsDirty)
                GUILayout.Label("Unsaved changes", EditorStyles.boldLabel);
            else
                GUILayout.Label("All changes saved");

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            _tool = (ToolMode)GUILayout.Toolbar((int)_tool, new[]
            {
                "Add Node", "Move", "Connect", "Curve", "Set Start", "Set Goal", "Delete"
            });

            EditorGUI.BeginChangeCheck();
            _draft.DisplayName = EditorGUILayout.TextField("Display Name", _draft.DisplayName);
            var renamed = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            _draft.Background = (Texture2D)EditorGUILayout.ObjectField("Background", _draft.Background, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
                _draft.ImageAspect = SourceImageAspect(_draft.Background);

            if (renamed || GUI.changed)
                Repaint();

            _previewSeed = EditorGUILayout.IntField("Preview Seed", _previewSeed);
            if (GUILayout.Button("Preview Path"))
                PreviewPath();

            EditorGUILayout.HelpBox(ToolHelp(), MessageType.None);
        }

        void DrawValidation()
        {
            if (_asset.TryValidateSnapshot(_draft, out var error))
                EditorGUILayout.HelpBox("Draft is valid.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            _validationMessage = error;
        }

        void DrawCanvas()
        {
            var width = Mathf.Max(160f, EditorGUIUtility.currentViewWidth - 28f);
            var aspect = _draft.ResolvedImageAspect;
            var height = Mathf.Clamp(width / Mathf.Max(0.01f, aspect), 160f, 720f);
            _canvasRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(true));
            GUI.Box(_canvasRect, GUIContent.none);
            _imageRect = GraphImageFit.FittedRect(_canvasRect, aspect);

            if (_draft.Background != null)
                GUI.DrawTextureWithTexCoords(_imageRect, _draft.Background, new Rect(0f, 0f, 1f, 1f));

            var nodes = _draft.Nodes;
            var edges = _draft.Edges;

            for (var i = 0; i < edges.Count; i++)
            {
                var a = FindNode(nodes, edges[i].NodeA);
                var b = FindNode(nodes, edges[i].NodeB);
                if (string.IsNullOrEmpty(a.Id) || string.IsNullOrEmpty(b.Id))
                    continue;

                var gui = GraphEdgePath.GuiSamples(edges[i], a.NormalizedPosition, b.NormalizedPosition, _imageRect);
                var points = new Vector3[gui.Length];
                for (var p = 0; p < gui.Length; p++)
                    points[p] = gui[p];

                Handles.BeginGUI();
                Handles.color = i == _selectedEdgeIndex
                    ? new Color(1f, 0.85f, 0.35f, 0.95f)
                    : new Color(0.7f, 0.75f, 0.85f, 0.9f);
                Handles.DrawAAPolyLine(i == _selectedEdgeIndex ? 4.5f : 3f, points);
                Handles.EndGUI();
            }

            DrawControlPoints(edges);

            for (var i = 0; i < nodes.Count; i++)
                DrawNodeHandle(nodes[i]);

            HandleCanvasInput(nodes);
        }

        string ToolHelp()
        {
            switch (_tool)
            {
                case ToolMode.Move:
                    return "Drag nodes or curve handles to reposition them on the background.";
                case ToolMode.Connect:
                    return "Click two nodes to connect them.";
                case ToolMode.Curve:
                    return "Click a connection to add a curve handle. Drag the handle to follow the photo path, and drag the yellow stick to rotate the tangent.";
                case ToolMode.Delete:
                    return "Click a curve handle to remove it, or a node to delete the node.";
                default:
                    return "Edits stay in memory until you press Save.";
            }
        }

        void DrawControlPoints(List<GraphEdgeEntry> edges)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                var points = edges[i].ControlPoints;
                if (points == null)
                    continue;

                for (var p = 0; p < points.Count; p++)
                {
                    var selected = i == _selectedEdgeIndex && p == _selectedControlIndex;
                    var center = GraphImageFit.NormalizedToGui(_imageRect, points[p].NormalizedPosition);
                    if (selected)
                        DrawTangentHandle(points[p]);

                    var size = selected ? ControlHandleSize + 2f : ControlHandleSize;
                    var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
                    EditorGUI.DrawRect(rect, selected ? new Color(1f, 0.82f, 0.2f, 1f) : new Color(0.95f, 0.55f, 0.2f, 0.95f));
                }
            }
        }

        void DrawTangentHandle(GraphEdgeControlPoint point)
        {
            var center = GraphImageFit.NormalizedToGui(_imageRect, point.NormalizedPosition);
            var handle = TangentHandleGui(point);
            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.92f, 0.35f, 0.95f);
            Handles.DrawAAPolyLine(2f, center, handle);
            Handles.EndGUI();
            EditorGUI.DrawRect(
                new Rect(handle.x - TangentHandleSize * 0.5f, handle.y - TangentHandleSize * 0.5f, TangentHandleSize, TangentHandleSize),
                new Color(1f, 0.95f, 0.45f, 1f));
        }

        void DrawNodeHandle(GraphNodeEntry node)
        {
            var center = GraphImageFit.NormalizedToGui(_imageRect, node.NormalizedPosition);
            var nodeRect = new Rect(center.x - NodeHandleSize * 0.5f, center.y - NodeHandleSize * 0.5f, NodeHandleSize, NodeHandleSize);

            var color = Color.gray;
            if (node.Id == _draft.StartNodeId)
                color = Color.green;
            else if (node.Id == _draft.ResolvedGoalNodeId)
                color = Color.yellow;
            else if (node.Id == _selectedNodeId || node.Id == _dragNodeId)
                color = Color.cyan;
            else if (IsOnPreviewPath(node.Id))
                color = Color.white;

            EditorGUI.DrawRect(nodeRect, color);
            GUI.Label(new Rect(center.x + 10f, center.y - 8f, 80f, 20f), node.Id);
        }

        void HandleCanvasInput(List<GraphNodeEntry> nodes)
        {
            var evt = Event.current;
            if (!_imageRect.Contains(evt.mousePosition) &&
                evt.type != EventType.MouseUp &&
                evt.type != EventType.MouseDrag)
                return;

            var hitNodeId = HitTestNode(nodes, evt.mousePosition);
            var hitControl = TryHitControlPoint(evt.mousePosition, out var hitEdge, out var hitPoint);
            var hitTangent = _selectedEdgeIndex >= 0 &&
                             _selectedControlIndex >= 0 &&
                             HitTangentHandle(_selectedEdgeIndex, _selectedControlIndex, evt.mousePosition);

            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0:
                    if (hitTangent && (_tool == ToolMode.Curve || _tool == ToolMode.Move))
                    {
                        _dragEdgeIndex = _selectedEdgeIndex;
                        _dragControlIndex = _selectedControlIndex;
                        _dragTangent = true;
                        evt.Use();
                        return;
                    }

                    if (hitControl && (_tool == ToolMode.Curve || _tool == ToolMode.Move || _tool == ToolMode.Delete))
                    {
                        _selectedEdgeIndex = hitEdge;
                        _selectedControlIndex = hitPoint;
                        _selectedNodeId = null;
                        if (_tool == ToolMode.Delete)
                        {
                            DeleteControlPoint(hitEdge, hitPoint);
                            evt.Use();
                            Repaint();
                            return;
                        }

                        _dragEdgeIndex = hitEdge;
                        _dragControlIndex = hitPoint;
                        _dragTangent = false;
                        evt.Use();
                        Repaint();
                        return;
                    }

                    if (hitNodeId != null)
                    {
                        _selectedNodeId = hitNodeId;
                        if (_tool == ToolMode.Move)
                        {
                            _dragNodeId = hitNodeId;
                            evt.Use();
                            return;
                        }

                        HandleNodeTool(hitNodeId);
                        evt.Use();
                        return;
                    }

                    if (_tool == ToolMode.Curve)
                    {
                        var edgeIndex = HitTestEdge(nodes, evt.mousePosition);
                        if (edgeIndex >= 0)
                        {
                            InsertControlPoint(edgeIndex, evt.mousePosition);
                            evt.Use();
                            Repaint();
                            return;
                        }

                        _selectedEdgeIndex = -1;
                        _selectedControlIndex = -1;
                        evt.Use();
                        Repaint();
                        return;
                    }

                    if (_tool == ToolMode.AddNode && _imageRect.Contains(evt.mousePosition))
                    {
                        AddNodeAt(evt.mousePosition);
                        evt.Use();
                    }

                    break;

                case EventType.MouseDrag when _dragTangent && _dragEdgeIndex >= 0:
                    RotateControlPoint(_dragEdgeIndex, _dragControlIndex, evt.mousePosition);
                    evt.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag when _dragEdgeIndex >= 0 && _dragControlIndex >= 0:
                    MoveControlPoint(_dragEdgeIndex, _dragControlIndex, GraphImageFit.GuiToNormalized(_imageRect, evt.mousePosition));
                    evt.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag when _dragNodeId != null && _tool == ToolMode.Move:
                    MoveNode(_dragNodeId, GraphImageFit.GuiToNormalized(_imageRect, evt.mousePosition));
                    evt.Use();
                    Repaint();
                    break;

                case EventType.MouseUp when _dragNodeId != null || _dragEdgeIndex >= 0:
                    ClearDrag();
                    evt.Use();
                    break;
            }
        }

        void HandleNodeTool(string nodeId)
        {
            switch (_tool)
            {
                case ToolMode.Connect:
                    if (string.IsNullOrEmpty(_connectFromId))
                        _connectFromId = nodeId;
                    else if (_connectFromId != nodeId)
                    {
                        AddEdge(_connectFromId, nodeId);
                        _connectFromId = null;
                    }
                    break;

                case ToolMode.SetStart:
                    _draft.StartNodeId = nodeId;
                    break;

                case ToolMode.SetGoal:
                    _draft.GoalNodeId = nodeId;
                    break;

                case ToolMode.Delete:
                    DeleteNode(nodeId);
                    break;
            }

            Repaint();
        }

        void DrawNodeList()
        {
            EditorGUILayout.LabelField("Nodes", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120f));
            for (var i = 0; i < _draft.Nodes.Count; i++)
            {
                var node = _draft.Nodes[i];
                EditorGUILayout.LabelField(
                    node.Id,
                    $"pos {node.NormalizedPosition.x:0.00}, {node.NormalizedPosition.y:0.00}");
            }
            EditorGUILayout.EndScrollView();
        }

        void AddNodeAt(Vector2 mousePosition)
        {
            var id = NextUniqueNodeId(_draft.Nodes);
            _draft.Nodes.Add(new GraphNodeEntry
            {
                Id = id,
                NormalizedPosition = GraphImageFit.GuiToNormalized(_imageRect, mousePosition)
            });
            _selectedNodeId = id;
            Repaint();
        }

        void MoveNode(string nodeId, Vector2 normalizedPosition)
        {
            for (var i = 0; i < _draft.Nodes.Count; i++)
            {
                if (_draft.Nodes[i].Id != nodeId)
                    continue;

                _draft.Nodes[i] = new GraphNodeEntry
                {
                    Id = nodeId,
                    NormalizedPosition = normalizedPosition
                };
                break;
            }
        }

        void AddEdge(string a, string b)
        {
            for (var i = 0; i < _draft.Edges.Count; i++)
            {
                if ((_draft.Edges[i].NodeA == a && _draft.Edges[i].NodeB == b) ||
                    (_draft.Edges[i].NodeA == b && _draft.Edges[i].NodeB == a))
                    return;
            }

            _draft.Edges.Add(new GraphEdgeEntry
            {
                NodeA = a,
                NodeB = b,
                ControlPoints = new List<GraphEdgeControlPoint>()
            });
        }

        void DeleteNode(string nodeId)
        {
            _draft.Nodes.RemoveAll(n => n.Id == nodeId);
            _draft.Edges.RemoveAll(e => e.NodeA == nodeId || e.NodeB == nodeId);

            if (_draft.StartNodeId == nodeId || _draft.ResolvedGoalNodeId == nodeId)
            {
                _draft.StartNodeId = _draft.Nodes.Count > 0 ? _draft.Nodes[0].Id : "n0";
                _draft.GoalNodeId = _draft.Nodes.Count > 1 ? _draft.Nodes[_draft.Nodes.Count - 1].Id : _draft.StartNodeId;
            }
        }

        void RepairDuplicateIds() => _draft.NormalizeIds();

        void PreviewPath()
        {
            _previewPath.Clear();
            if (!_asset.TryValidateSnapshot(_draft, out _))
                return;

            try
            {
                var topology = _asset.TopologyFromSnapshot(_draft);
                var path = GraphPathGenerator.Generate(
                    topology,
                    new GraphNodeId(_draft.StartNodeId),
                    new GraphNodeId(_draft.ResolvedGoalNodeId),
                    _asset.ShapeFromSnapshot(_draft),
                    new XorShiftRandom(_previewSeed));
                _previewPath.AddRange(path.Nodes);
            }
            catch (System.Exception exception)
            {
                _validationMessage = exception.Message;
            }

            Repaint();
        }

        void SaveDraft()
        {
            if (_asset == null || _draft == null)
                return;

            _draft.WriteTo(_asset);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            _saved = _draft.Clone();
            Repaint();
        }

        void CancelDraft()
        {
            if (_saved == null)
                return;

            _draft = _saved.Clone();
            _selectedNodeId = null;
            _connectFromId = null;
            ClearDrag();
            ClearCurveSelection();
            _previewPath.Clear();
            Repaint();
        }

        void LoadDraftFromAsset(GraphLevelDefinition asset)
        {
            _asset = asset;
            _draft = asset != null ? asset.CaptureSnapshot() : null;
            if (_draft != null && _draft.ImageAspect <= 0.01f)
                _draft.ImageAspect = SourceImageAspect(_draft.Background);
            _saved = _draft?.Clone();
            _selectedNodeId = null;
            _connectFromId = null;
            ClearDrag();
            ClearCurveSelection();
            _previewPath.Clear();
        }

        void TrySwitchAsset(GraphLevelDefinition nextAsset)
        {
            if (nextAsset == _asset)
                return;

            if (IsDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "Unsaved graph level changes",
                    "Save changes before switching levels?",
                    "Save",
                    "Cancel",
                    "Discard");

                switch (choice)
                {
                    case 0:
                        SaveDraft();
                        break;
                    case 1:
                        return;
                    case 2:
                        break;
                }
            }

            LoadDraftFromAsset(nextAsset);
        }

        bool IsOnPreviewPath(string nodeId)
        {
            for (var i = 0; i < _previewPath.Count; i++)
            {
                if (_previewPath[i].Value == nodeId)
                    return true;
            }

            return false;
        }

        void InsertControlPoint(int edgeIndex, Vector2 mousePosition)
        {
            if (edgeIndex < 0 || edgeIndex >= _draft.Edges.Count)
                return;

            var edge = _draft.Edges[edgeIndex];
            var a = FindNode(_draft.Nodes, edge.NodeA);
            var b = FindNode(_draft.Nodes, edge.NodeB);
            if (string.IsNullOrEmpty(a.Id) || string.IsNullOrEmpty(b.Id))
                return;

            var normalized = GraphImageFit.GuiToNormalized(_imageRect, mousePosition);
            var knots = GraphEdgePath.ToKnots(edge, a.NormalizedPosition, b.NormalizedPosition);
            if (!GraphEdgeSpline.TryClosestOnSpline(
                    knots,
                    normalized.x,
                    normalized.y,
                    16,
                    out var segment,
                    out var t,
                    out _,
                    out _,
                    out _))
                return;

            var knot = GraphEdgeSpline.KnotOnSegment(knots[segment], knots[segment + 1], t);
            var points = GraphEdgeEntry.CopyControlPoints(edge.ControlPoints);
            var insertAt = Mathf.Clamp(segment, 0, points.Count);
            points.Insert(insertAt, GraphEdgePath.ControlPointFromKnot(knot));
            edge.ControlPoints = points;
            _draft.Edges[edgeIndex] = edge;
            _selectedEdgeIndex = edgeIndex;
            _selectedControlIndex = insertAt;
        }

        void MoveControlPoint(int edgeIndex, int pointIndex, Vector2 normalizedPosition)
        {
            if (!TryGetControlPoint(edgeIndex, pointIndex, out var edge, out var points, out var point))
                return;

            point.NormalizedPosition = normalizedPosition;
            points[pointIndex] = point;
            edge.ControlPoints = points;
            _draft.Edges[edgeIndex] = edge;
        }

        void RotateControlPoint(int edgeIndex, int pointIndex, Vector2 mousePosition)
        {
            if (!TryGetControlPoint(edgeIndex, pointIndex, out var edge, out var points, out var point))
                return;

            var center = GraphImageFit.NormalizedToGui(_imageRect, point.NormalizedPosition);
            var dx = (mousePosition.x - center.x) / Mathf.Max(0.0001f, _imageRect.width);
            var dy = -(mousePosition.y - center.y) / Mathf.Max(0.0001f, _imageRect.height);
            point.TangentDegrees = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            point.TangentLength = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
            points[pointIndex] = point;
            edge.ControlPoints = points;
            _draft.Edges[edgeIndex] = edge;
        }

        void DeleteControlPoint(int edgeIndex, int pointIndex)
        {
            if (!TryGetControlPoint(edgeIndex, pointIndex, out var edge, out var points, out _))
                return;

            points.RemoveAt(pointIndex);
            edge.ControlPoints = points;
            _draft.Edges[edgeIndex] = edge;
            _selectedEdgeIndex = edgeIndex;
            _selectedControlIndex = points.Count == 0 ? -1 : Mathf.Clamp(pointIndex, 0, points.Count - 1);
            ClearDrag();
        }

        bool TryGetControlPoint(
            int edgeIndex,
            int pointIndex,
            out GraphEdgeEntry edge,
            out List<GraphEdgeControlPoint> points,
            out GraphEdgeControlPoint point)
        {
            edge = default;
            points = null;
            point = default;
            if (edgeIndex < 0 || edgeIndex >= _draft.Edges.Count)
                return false;

            edge = _draft.Edges[edgeIndex];
            points = GraphEdgeEntry.CopyControlPoints(edge.ControlPoints);
            if (pointIndex < 0 || pointIndex >= points.Count)
                return false;

            point = points[pointIndex];
            return true;
        }

        int HitTestEdge(List<GraphNodeEntry> nodes, Vector2 mousePosition)
        {
            var best = EdgeHitPixels * EdgeHitPixels;
            var found = -1;
            for (var i = 0; i < _draft.Edges.Count; i++)
            {
                var a = FindNode(nodes, _draft.Edges[i].NodeA);
                var b = FindNode(nodes, _draft.Edges[i].NodeB);
                if (string.IsNullOrEmpty(a.Id) || string.IsNullOrEmpty(b.Id))
                    continue;

                var dist = GraphEdgePath.SqrDistanceToGui(
                    _draft.Edges[i],
                    a.NormalizedPosition,
                    b.NormalizedPosition,
                    _imageRect,
                    mousePosition);
                if (dist >= best)
                    continue;

                best = dist;
                found = i;
            }

            return found;
        }

        bool TryHitControlPoint(Vector2 mousePosition, out int edgeIndex, out int pointIndex)
        {
            edgeIndex = -1;
            pointIndex = -1;
            var best = ControlHandleSize * ControlHandleSize * 0.25f;
            for (var i = 0; i < _draft.Edges.Count; i++)
            {
                var points = _draft.Edges[i].ControlPoints;
                if (points == null)
                    continue;

                for (var p = 0; p < points.Count; p++)
                {
                    var center = GraphImageFit.NormalizedToGui(_imageRect, points[p].NormalizedPosition);
                    var delta = mousePosition - center;
                    var dist = delta.sqrMagnitude;
                    if (dist > best)
                        continue;

                    best = dist;
                    edgeIndex = i;
                    pointIndex = p;
                }
            }

            return edgeIndex >= 0;
        }

        bool HitTangentHandle(int edgeIndex, int pointIndex, Vector2 mousePosition)
        {
            if (!TryGetControlPoint(edgeIndex, pointIndex, out _, out _, out var point))
                return false;

            var handle = TangentHandleGui(point);
            var rect = new Rect(
                handle.x - TangentHandleSize * 0.5f,
                handle.y - TangentHandleSize * 0.5f,
                TangentHandleSize,
                TangentHandleSize);
            return rect.Contains(mousePosition);
        }

        Vector2 TangentHandleGui(GraphEdgeControlPoint point)
        {
            var center = GraphImageFit.NormalizedToGui(_imageRect, point.NormalizedPosition);
            var radians = point.TangentDegrees * Mathf.Deg2Rad;
            var length = Mathf.Max(0.04f, point.TangentLength);
            return center + new Vector2(
                Mathf.Cos(radians) * length * _imageRect.width,
                -Mathf.Sin(radians) * length * _imageRect.height);
        }

        void ClearDrag()
        {
            _dragNodeId = null;
            _dragEdgeIndex = -1;
            _dragControlIndex = -1;
            _dragTangent = false;
        }

        void ClearCurveSelection()
        {
            _selectedEdgeIndex = -1;
            _selectedControlIndex = -1;
        }

        string HitTestNode(List<GraphNodeEntry> nodes, Vector2 mousePosition)
        {
            for (var i = nodes.Count - 1; i >= 0; i--)
            {
                var center = GraphImageFit.NormalizedToGui(_imageRect, nodes[i].NormalizedPosition);
                var nodeRect = new Rect(center.x - NodeHandleSize * 0.5f, center.y - NodeHandleSize * 0.5f, NodeHandleSize, NodeHandleSize);
                if (nodeRect.Contains(mousePosition))
                    return nodes[i].Id;
            }

            return null;
        }

        static GraphNodeEntry FindNode(List<GraphNodeEntry> nodes, string id)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == id)
                    return nodes[i];
            }

            return default;
        }

        static string NextUniqueNodeId(List<GraphNodeEntry> nodes)
        {
            var used = new HashSet<string>();
            for (var i = 0; i < nodes.Count; i++)
                used.Add(nodes[i].Id);

            return NextUniqueNodeIdFromUsed(used);
        }

        static string NextUniqueNodeIdFromUsed(HashSet<string> used)
        {
            var max = -1;
            foreach (var id in used)
            {
                if (id != null && id.StartsWith("n") && int.TryParse(id.Substring(1), out var number))
                    max = Mathf.Max(max, number);
            }

            for (var i = 0; i <= max + 1; i++)
            {
                var candidate = "n" + i;
                if (!used.Contains(candidate))
                    return candidate;
            }

            return "n" + (max + 1);
        }

        void CreateNewLevel()
        {
            if (IsDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "Unsaved graph level changes",
                    "Save changes before creating a new level?",
                    "Save",
                    "Cancel",
                    "Discard");

                if (choice == 1)
                    return;
                if (choice == 0)
                    SaveDraft();
            }

            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/GraphLevels");
            var asset = ScriptableObject.CreateInstance<GraphLevelDefinition>();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/Unity/Data/GraphLevels/GraphLevel.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            LoadDraftFromAsset(asset);
            Selection.activeObject = asset;
        }

        void CreateSampleLevelAsset()
        {
            if (IsDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "Unsaved graph level changes",
                    "Save changes before loading the sample?",
                    "Save",
                    "Cancel",
                    "Discard");

                if (choice == 1)
                    return;
                if (choice == 0)
                    SaveDraft();
            }

            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/GraphLevels");
            var path = "Assets/Game/Unity/Data/GraphLevels/SampleVillage.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(path);
            if (existing == null)
            {
                var sample = GraphLevelDefinition.CreateSampleRuntime();
                AssetDatabase.CreateAsset(sample, path);
                AssetDatabase.SaveAssets();
                existing = sample;
            }

            LoadDraftFromAsset(existing);
            Selection.activeObject = existing;
        }

        static float SourceImageAspect(Texture2D texture)
        {
            if (texture == null)
                return 1f;

            var path = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.GetSourceTextureWidthAndHeight(out var width, out var height);
                    if (height > 0)
                        return width / (float)height;
                }
            }

            return GraphLevelDefinition.AspectOf(texture);
        }

        void OnDisable()
        {
            if (!IsDirty || _asset == null)
                return;

            var save = EditorUtility.DisplayDialog(
                "Unsaved graph level changes",
                "Save changes before closing the Graph Level Editor?",
                "Save",
                "Discard");

            if (save)
                SaveDraft();
        }
    }
}
