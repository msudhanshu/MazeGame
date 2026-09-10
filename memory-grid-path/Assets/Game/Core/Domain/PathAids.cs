using System;
using System.Collections.Generic;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    public enum PathPickupKind
    {
        Glimpse,
        Beacon
    }

    public readonly struct PathPickup
    {
        public PathPickup(GridCoord cell, PathPickupKind kind)
        {
            Cell = cell;
            Kind = kind;
        }

        public GridCoord Cell { get; }
        public PathPickupKind Kind { get; }
    }

    /// <summary>
    /// Lighthouses and one-shot pickups sitting on one generated path. Placement is Core's job;
    /// Unity only paints whatever this contains.
    /// </summary>
    public sealed class PathAids
    {
        public static PathAids None { get; } = new PathAids(Array.Empty<GridCoord>(), Array.Empty<PathPickup>());

        public PathAids(
            IReadOnlyList<GridCoord> lighthouses,
            IReadOnlyList<PathPickup> pickups,
            IReadOnlyList<GridCoord> blockedHints = null)
        {
            Lighthouses = lighthouses ?? Array.Empty<GridCoord>();
            Pickups = pickups ?? Array.Empty<PathPickup>();
            BlockedHints = blockedHints ?? Array.Empty<GridCoord>();
        }

        public IReadOnlyList<GridCoord> Lighthouses { get; }
        public IReadOnlyList<PathPickup> Pickups { get; }
        public IReadOnlyList<GridCoord> BlockedHints { get; }
    }
}
