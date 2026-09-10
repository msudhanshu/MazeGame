# Maze Arena

First-person labyrinth. Maze generation and walls live in `com.nixin.maze`. Free-walk math lives in `com.nixin.locomotion`. Groove / waypoint travel lives in `com.nixin.rail`. Campaign rules and walk-scheme names live in this game.

## Play scene

Open `Assets/Scenes/MazePlay.unity` and select **MazeGame**. Hover inspector fields for tooltips.

| Object | What you set there |
| --- | --- |
| **MazeGame** | Walk scheme, look stick, waypoint style, **Intermediate Waypoints**, look-down, **Swipe To Stop**, **Keyboard WASD**, speeds, **Limit Look Yaw**, debug HUD, campaign on/off |
| **MazeArena** | Size, difficulty, seed, photos (used when campaign is off) |
| **MemoryArenaMemory** | Memory-wall catalog / level / display kit |

Walls are generated at Play (`MazeArena.Rebuild`). Rebuild the scene from **Nixin Studio → Maze Arena → Build Play Scene**. `MazeSandbox` is the old lab; leave **Show Debug Tools** off for player builds.

## Walk schemes

Set **Walk Scheme** on **MazeGame**. Inspector and debug HUD use the same names. A short description sits under the selected scheme in both places.

| Inspector / debug name | Status |
| --- | --- |
| **Dual sticks** | Implemented — free walk |
| **Gaze walk** | Not built — Play uses Rail waypoint |
| **Arrow look** | Not built — Play uses Rail waypoint |
| **Rail waypoint** | Default — groove + tap neighbor |
| **Rail joystick** | Groove + one stick (look X, walk Y) |

**Look Stick**, **Waypoint Style**, **Intermediate Waypoints**, **Allow Look Down**, **Swipe To Stop**, and **Max Look Down** only appear when they apply.

### Dual sticks

Free walk, not on the rail.

- Phone: left stick moves, right stick looks.
- Laptop: WASD moves, mouse looks (Esc frees the cursor).

### Gaze walk (not implemented)

Would walk the way you are looking, with a separate look-drag. Falls back to Rail waypoint.

### Arrow look (not implemented)

Would walk the way you are looking, with left/right look buttons. Falls back to Rail waypoint.

### Rail waypoint (default)

Locked to the corridor centerline (every open hall, not only the solution). Shared groove rules (straight runs, Skip/Remove, swipe-to-stop, look-down) live in `com.nixin.rail`.

- Tap a neighbor **floor chip** or **space blob** to glide there at **Rail Move Speed** (~1.6 m/s). Clicks while moving are ignored.
- Look is independent of travel.
- Laptop: **Keyboard WASD** (on by default) — A/D or arrows look; W/S walks the hall you face if you use them. Tap chips still work.
- **Allow Look Down** (on by default) lets you tilt toward the floor. Off locks look at **Default Look Down** (10°) so nearby floor chips stay in view. Floor chips use **Max Look Down** (default 70°) as the tilt cap. Space blobs keep pitch on the horizon even when look-down is on.

### Rail joystick

Same corridor groove as Rail waypoint. No waypoint chips.

- **Horizontal** look (yaw). Pitch stays on the horizon.
- **Vertical** pushes along the way you face. Only the facing component **along the open corridor** becomes speed (**Joystick Move Speed**, default 0.7 m/s). Face a wall → no move. Glance 45° down a hall → slower. Pull down → walk the groove behind you.
- Laptop: when **Keyboard WASD** is on (default), A/D looks and W/S walks. Turn it off to use only the on-screen stick.


## Rail waypoint: straight halls

Clicking every cell on a long hall is busywork. If the next **turn** is far away, one tap should take you there.

A 90° **L-bend** still gets a waypoint. The hall changes direction, even when there is only one way to go, so we do **not** glide around corners. Junctions (a choice of ways) and dead ends are terminals too.

From where you stand, each open direction is its own arm. We walk along that arm while the next cell is the only way forward **and** it stays on the same axis. The last cell of that walk is the terminal: dead end, junction, or L-bend. Example: cells A–B–C–D then an L or a T at E → the run is B, C, D, E.

### Intermediate Waypoints

Set **Intermediate Waypoints** on **MazeGame** (Rail waypoint only). Same names in the debug HUD.

| Mode | Chips on a straight hall | What you can tap |
| --- | --- | --- |
| **Required** | Every cell until the terminal | Only the **next** neighbor. Old one-step walk. |
| **Skip** (default) | Every cell until the terminal | **Any** of those cells, including the far turn. |
| **Remove** | None on the straight. Only the terminal chip is created. | That terminal (and the other arm’s terminal). |

Skip and Remove glide through every cell on the unique path to the chip you tapped. Clicks while you are already gliding are ignored (no retarget). Required still ignores a far tap.

### Swipe to stop (wall photos)

Wall photos sit on those halls. If **Allow Look Down** is on, downward drag already tilts the head, so swipe-to-stop is off.

If look-down is **off**, pitch is locked at **Default Look Down**. You cannot tilt to a frame while gliding. **Swipe To Stop** (on by default, shown only when look-down is off): a **fast swipe down** while walking parks you so you can yaw and look at a nearby photo.

You never park closer than **one cell** to the chip you were walking toward. If you stood on that chip, Default Look Down could not see it to tap again. If you swipe when you are already farther than one cell, you stop in place (mid-hall is allowed, so you can read a frame you just passed). If you swipe on the last cell of the trip, you rewind to the previous cell center.

Then tap any currently clickable chip to walk again. A normal trip with no swipe **does** arrive on the chip you tapped.

## Look stick (Rail waypoint and Rail joystick)

**Look Stick** on **MazeGame** (same names in debug):

| Mode | Stick art | Input |
| --- | --- | --- |
| **Fixed bottom** | Always at the bottom-center | Only that pad looks (and in Rail joystick, walks) |
| **Appear on drag** (default) | Shows where you press and drag | Drag anywhere |
| **Hidden drag** | None | Same drag, no art |

**Keyboard WASD** (on by default, Rail waypoint and Rail joystick): A/D or arrows look; W/S walks the hall you face if you use them. Tap chips and touch/mouse look still work.

## Look stop (every scheme)

**Limit Look Yaw** (on by default). Look cannot spin a full circle: it stops at **±180°** from the facing you had at spawn.

- You can ease a few degrees past the stop, then it springs back to ±180 so it does not feel like a hard wall.
- A thin dark gradient appears on the **left** or **right** screen edge when you are at that stop.
- Turn **Limit Look Yaw** off on **MazeGame** (or the debug checkbox) to allow a full spin later.

## Layout

Rail input (Street View-style groove, waypoints, swipe-to-stop, look stick) is documented in `com.nixin.rail` (`packages/com.nixin.rail/README.md`). This game only adapts it to the maze grid.

- `Assets/Game/Core` — `CorridorRail` (`IRail` adapter: cell → node id), `RailEdge`, campaign / memory / schemes
- `Assets/Game/Unity` — `MazeGameHost`, `MazeWalker`, `RailLocomotion` (binds `CorridorRail` to `RailMotor`), `FloorWaypoints`, `LookYawGate`
