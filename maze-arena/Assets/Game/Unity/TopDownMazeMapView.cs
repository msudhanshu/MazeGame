using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Draws the same MazeLayout as a flat 2D map in the corner of the screen.</summary>
    public sealed class TopDownMazeMapView : MonoBehaviour
    {
        public MazeArena Arena;
        public int PixelSize = 12;
        public Vector2 Offset = new Vector2(16, 16);

        void OnGUI()
        {
            if (Arena == null || Arena.Layout == null)
                return;

            var layout = Arena.Layout;
            var cell = PixelSize;
            var origin = Offset;

            GUI.Box(new Rect(origin.x - 8, origin.y - 8, layout.Size.Width * cell + 16, layout.Size.Height * cell + 16), "2D map");

            var wall = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            var floor = new Color(0.85f, 0.85f, 0.78f, 0.9f);
            var solution = new Color(0.95f, 0.55f, 0.15f, 0.85f);

            for (var y = 0; y < layout.Size.Height; y++)
            {
                for (var x = 0; x < layout.Size.Width; x++)
                {
                    var guiY = origin.y + (layout.Size.Height - 1 - y) * cell;
                    var rect = new Rect(origin.x + x * cell, guiY, cell, cell);
                    GUI.color = floor;
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                }
            }

            if (layout.Solution != null)
            {
                GUI.color = solution;
                for (var i = 0; i < layout.Solution.Count; i++)
                {
                    var c = layout.Solution[i];
                    var guiY = origin.y + (layout.Size.Height - 1 - c.Y) * cell + cell * 0.3f;
                    var rect = new Rect(origin.x + c.X * cell + cell * 0.3f, guiY, cell * 0.4f, cell * 0.4f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                }
            }

            GUI.color = wall;
            DrawWalls(layout, origin, cell);

            GUI.color = Color.green;
            DrawOpening(layout.Entry, layout.Size, origin, cell);
            GUI.color = Color.red;
            DrawOpening(layout.Exit, layout.Size, origin, cell);
            GUI.color = Color.white;
        }

        static void DrawWalls(MazeLayout layout, Vector2 origin, int cell)
        {
            var walls = layout.Grid.OccupiedWalls();
            for (var i = 0; i < walls.Count; i++)
            {
                var n = walls[i].Normalized();
                var thickness = 2f;

                if (n.Side == WallSide.East)
                {
                    var x = origin.x + (n.Cell.X + 1) * cell;
                    var guiY = origin.y + (layout.Size.Height - 1 - n.Cell.Y) * cell;
                    GUI.DrawTexture(new Rect(x - 1, guiY, thickness, cell), Texture2D.whiteTexture);
                }
                else if (n.Side == WallSide.North)
                {
                    var guiY = origin.y + (layout.Size.Height - 1 - n.Cell.Y) * cell;
                    var x = origin.x + n.Cell.X * cell;
                    GUI.DrawTexture(new Rect(x, guiY - 1, cell, thickness), Texture2D.whiteTexture);
                }
            }
        }

        static void DrawOpening(MazeOpening opening, GridSize size, Vector2 origin, int cell)
        {
            var c = opening.Cell;
            var guiY = origin.y + (size.Height - 1 - c.Y) * cell + cell * 0.25f;
            GUI.DrawTexture(new Rect(origin.x + c.X * cell + cell * 0.25f, guiY, cell * 0.5f, cell * 0.5f), Texture2D.whiteTexture);
        }
    }
}
