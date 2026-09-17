using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Scene dressing around a board (floor, photo, VFX, post). Rebuilt each play so one
    /// theme cannot leave particles or a floor sitting under the next.
    /// </summary>
    public static class ArenaEnvironment
    {
        public const string RoomFloorName = "Room Floor";
        public const float RoomFloorDepth = 8f;

        public static void Clear(Transform playRoot)
        {
            if (playRoot == null)
                return;

            DestroyChild(playRoot, RoomFloorName);
            DestroyChild(playRoot, "Mosaic Photo");
            DestroyChild(playRoot, "Mosaic Dust");
            DestroyChild(playRoot, "Patchwork Ocean");
            DestroyChild(playRoot, "Scout Fog Padding");
            DestroyChild(playRoot, "Scout Fog Overlay");
            DestroyChild(playRoot, "DanceFloor Post");
            DestroyChild(playRoot, "MosaicArena Post");
            DestroyChild(playRoot, "PatchworkArena Post");
        }

        /// <summary>
        /// A large dark plane well below the board so it reads as a room, not as a lid on the tiles.
        /// </summary>
        public static Transform PlaceDistantRoomFloor(BoardLayout layout, Transform parent, Color color)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = RoomFloorName;
            floor.transform.SetParent(parent, false);
            floor.transform.position = layout.Origin + new Vector3(0f, -RoomFloorDepth, 0f);
            var span = Mathf.Max(layout.Width, layout.Depth, 4f) * 4f;
            floor.transform.localScale = new Vector3(span / 10f, 1f, span / 10f);
            DestroyNow(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.Unlit("RoomFloor", color);
            return floor.transform;
        }

        public static void DestroyNow(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
