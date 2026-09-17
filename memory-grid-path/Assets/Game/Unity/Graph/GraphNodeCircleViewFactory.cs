using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using Nixin.Graph.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.Graph
{
    public sealed class GraphNodeCircleViewFactory : IGraphNodeViewFactory
    {
        const float NodeRadius = 0.22f;

        public string ThemeId => "graph_node_circle";
        /// <summary>Scout graph: cloudy fog around the photo instead of a hard edge.</summary>
        public bool OceanBackdrop { get; set; }
        /// <summary>Graph Arena: cloudy fog, contain-fit, bottom-aligned when the photo is short.</summary>
        public bool FogOfWar { get; set; }

        public IGraphNodeView CreateNode(GraphNodeId nodeId, Vector3 worldPosition, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Node " + nodeId.Value;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition + Vector3.up * 0.05f;
            go.transform.localScale = Vector3.one * NodeRadius * 2f;
            Object.Destroy(go.GetComponent<Collider>());

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = ArenaMaterials.Overlay("GraphNodeCircle", new Color(0.25f, 0.3f, 0.38f, 0.45f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var view = go.AddComponent<GraphNodeCircleView>();
            view.Initialise(nodeId, renderer);
            view.SetVisible(false);
            return view;
        }

        public void ApplyEnvironment(Camera camera, GraphBoardLayout layout, Transform parent)
        {
            ArenaEnvironment.Clear(parent);

            if (FogOfWar || OceanBackdrop)
                ScoutFogOfWar.Build(layout.Origin, layout.WorldWidth, layout.WorldDepth, parent);

            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FogOfWar || OceanBackdrop
                ? ScoutFogOfWar.Background
                : DanceFloorPalette.Background;
            BoardCamera.FrameTopDownForBounds(
                camera,
                layout.Origin,
                layout.WorldWidth,
                layout.WorldDepth,
                camera.aspect,
                bottomAlign: FogOfWar);
        }
    }
}
