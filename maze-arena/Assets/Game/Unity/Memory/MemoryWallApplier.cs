using Game.Core.Memory;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity.Memory
{
    public static class MemoryWallApplier
    {
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        const float ObjectCorridorOffset = 0.03f;

        public static int Apply(
            MemoryWallPlan plan,
            MazeArena arena,
            MemoryWallCatalog catalog,
            MemoryWallDisplayKit displayKit)
        {
            if (arena == null || arena.Layout == null)
                return 0;

            var segments = CollectSegments(arena);
            return Apply(plan, segments, catalog, displayKit, arena.Dimensions, arena.Layout.Size);
        }

        public static int Apply(
            MemoryWallPlan plan,
            System.Collections.Generic.IReadOnlyDictionary<MazeEdge, MazeWallSegment> segments,
            MemoryWallCatalog catalog,
            MemoryWallDisplayKit displayKit,
            MazeDimensions dimensions,
            GridSize size)
        {
            if (plan == null || segments == null || catalog == null || displayKit == null || dimensions == null)
                return 0;

            var painted = 0;
            for (var i = 0; i < plan.Placements.Count; i++)
            {
                var placement = plan.Placements[i];
                if (!segments.TryGetValue(placement.Edge.Normalized(), out var segment) || segment == null)
                    continue;

                switch (placement.Kind)
                {
                    case MemoryDisplayKind.Photo:
                        if (ApplyPhoto(segment, placement, catalog, displayKit, dimensions, size))
                            painted++;
                        break;
                    case MemoryDisplayKind.Relief:
                        if (ApplyRelief(segment, placement, catalog, displayKit, dimensions, size))
                            painted++;
                        break;
                    case MemoryDisplayKind.Object3d:
                        if (ApplyObject3d(segment, placement, catalog, displayKit))
                            painted++;
                        break;
                }
            }

            return painted;
        }

        static System.Collections.Generic.Dictionary<MazeEdge, MazeWallSegment> CollectSegments(MazeArena arena)
        {
            var segments = new System.Collections.Generic.Dictionary<MazeEdge, MazeWallSegment>();
            if (arena.Layout == null)
                return segments;

            var walls = arena.Layout.Grid.OccupiedWalls();
            for (var i = 0; i < walls.Count; i++)
            {
                var segment = arena.SegmentFor(walls[i]);
                if (segment != null)
                    segments[walls[i].Normalized()] = segment;
            }

            return segments;
        }

        static bool ApplyPhoto(
            MazeWallSegment segment,
            MemoryWallPlacement placement,
            MemoryWallCatalog catalog,
            MemoryWallDisplayKit displayKit,
            MazeDimensions dimensions,
            GridSize size)
        {
            var texture = catalog.ResolveImage(placement.EntryId);
            if (texture == null)
                return false;

            EnsureFrameVariant(segment, placement, displayKit, dimensions, size);
            segment.SetPhoto(placement.Face, texture);
            BindTag(segment, placement);
            var anchor = segment.AnchorFor(placement.Face);
            return anchor != null && anchor.gameObject.activeSelf;
        }

        static bool ApplyRelief(
            MazeWallSegment segment,
            MemoryWallPlacement placement,
            MemoryWallCatalog catalog,
            MemoryWallDisplayKit displayKit,
            MazeDimensions dimensions,
            GridSize size)
        {
            var texture = catalog.ResolveImage(placement.EntryId);
            if (texture == null)
                return false;

            EnsureDisplayRoot(segment, placement, displayKit, dimensions, size, out var meshRenderer);
            if (meshRenderer == null)
                return false;

            if (displayKit.ReliefMaterial != null)
                meshRenderer.sharedMaterial = displayKit.ReliefMaterial;

            SetTexture(meshRenderer, texture, catalog.FindNormalMap(placement.EntryId));
            ScalePictureMesh(meshRenderer.transform, displayKit.ReliefPictureScale);
            BindTag(segment, placement);
            segment.EnsureDisplayVisible(placement.Face);
            return true;
        }

        static bool ApplyObject3d(
            MazeWallSegment segment,
            MemoryWallPlacement placement,
            MemoryWallCatalog catalog,
            MemoryWallDisplayKit displayKit)
        {
            var prefab = catalog.FindObjectPrefab(placement.EntryId);
            if (prefab == null)
                return false;

            var anchor = segment.AnchorFor(placement.Face);
            if (anchor == null)
                return false;

            ClearChildren(anchor);

            var instance = Object.Instantiate(prefab, anchor);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.forward * ObjectCorridorOffset;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * displayKit.ObjectScale;
            instance.SetActive(true);
            BindTag(segment, placement);
            segment.EnsureDisplayVisible(placement.Face);
            return true;
        }

        static void EnsureFrameVariant(
            MazeWallSegment segment,
            MemoryWallPlacement placement,
            MemoryWallDisplayKit displayKit,
            MazeDimensions dimensions,
            GridSize size)
        {
            var anchor = segment.AnchorFor(placement.Face);
            if (anchor == null)
                return;

            var framePrefab = displayKit.FramePrefabFor(placement.FrameVariantId);
            if (framePrefab == null)
            {
                MazeGeometry.ApplyPhotoLocal(anchor, segment.Edge, dimensions, size, placement.Face);
                return;
            }

            var existing = anchor.childCount > 0 ? anchor.GetChild(0).gameObject : null;
            if (existing != null && PrefabMatches(existing, framePrefab))
            {
                existing.SetActive(true);
                MazeGeometry.ApplyPhotoLocal(anchor, segment.Edge, dimensions, size, placement.Face);
                return;
            }

            ClearChildren(anchor);
            var instance = Object.Instantiate(framePrefab, anchor);
            instance.name = framePrefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);
            MazeGeometry.ApplyPhotoLocal(anchor, segment.Edge, dimensions, size, placement.Face);
        }

        static void EnsureDisplayRoot(
            MazeWallSegment segment,
            MemoryWallPlacement placement,
            MemoryWallDisplayKit displayKit,
            MazeDimensions dimensions,
            GridSize size,
            out Renderer meshRenderer)
        {
            meshRenderer = null;
            EnsureFrameVariant(segment, placement, displayKit, dimensions, size);
            var anchor = segment.AnchorFor(placement.Face);
            if (anchor == null)
                return;

            var mesh = FindDescendant(anchor, "Mesh");
            meshRenderer = mesh != null ? mesh.GetComponent<Renderer>() : anchor.GetComponentInChildren<Renderer>(true);
        }

        static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            var direct = root.Find(name);
            if (direct != null)
                return direct;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void BindTag(MazeWallSegment segment, MemoryWallPlacement placement)
        {
            var anchor = segment.AnchorFor(placement.Face);
            if (anchor == null)
                return;

            var tag = anchor.GetComponent<MemoryDisplayTag>();
            if (tag == null)
                tag = anchor.gameObject.AddComponent<MemoryDisplayTag>();
            tag.Bind(placement);
        }

        static void ClearChildren(Transform anchor)
        {
            for (var c = anchor.childCount - 1; c >= 0; c--)
            {
                var child = anchor.GetChild(c);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        static bool PrefabMatches(GameObject instance, GameObject prefab)
        {
            if (prefab == null)
                return instance != null;
            return instance != null && instance.name.StartsWith(prefab.name, System.StringComparison.Ordinal);
        }

        static void SetTexture(Renderer renderer, Texture2D albedo, Texture2D normalMap)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            block.SetTexture(BaseMapId, albedo);
            block.SetTexture(MainTexId, albedo);
            if (normalMap != null)
                block.SetTexture(BumpMapId, normalMap);
            renderer.SetPropertyBlock(block, 0);
            renderer.enabled = true;
            if (renderer.transform.parent != null)
                renderer.transform.parent.gameObject.SetActive(true);
        }

        static void ScalePictureMesh(Transform mesh, float scale)
        {
            if (mesh == null)
                return;
            var s = mesh.localScale;
            mesh.localScale = new Vector3(s.x * scale, s.y * scale, s.z);
        }
    }
}
