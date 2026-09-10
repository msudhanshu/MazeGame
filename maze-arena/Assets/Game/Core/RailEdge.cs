using Nixin.Grid.Core;

namespace Game.Core
{
    public readonly struct RailEdge
    {
        public RailEdge(GridCoord a, GridCoord b)
        {
            A = a;
            B = b;
        }

        public GridCoord A { get; }
        public GridCoord B { get; }
    }
}
