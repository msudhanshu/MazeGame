# Memory Game: Remember the Path Home

A memory puzzle. Watch a hazy radar of the route home, then walk it from memory before your hearts run out.

Package id stays `com.nixin.memorizewayhome`. Display name is **Memory Game: Remember the Path Home**.

## How it plays

1. A **radar preview** shows the hidden path (new path only). Input is locked until it ends.
2. Tap a **glowing neighbor** (square border). Swipe works too.
3. A miss is a **mine**. A spark flies to the HUD heart and takes a slice. The true tile slams on. Remember it.
4. When a heart is empty, **crash-restart** on the **same path** (no popup). White trail returns to floor colors. No second radar.
5. When every heart is gone: **You Failed**. **Play Level N** starts a **new path** and the radar plays again.

## Product rules

- This is a memory game. Luck is not sold. A miss is a memory miss.
- Same-path restart stays. Full fail generates a new path.
- Tile Arena in Journey Hub is the ship. Graph shares HUD, popups, and copy. Graph has no radar pass.

## Nine-point redesign

### 1. Radar path flash

- Night-green veil over the arena. Path as a phosphor glow, not a clear white trail.
- Sweep follows board height and turns, clamped 0.72–1.65s, then a 0.12s hold (`PathPreviewSeconds`). A glance, not a study.
- Plays at the start of a **new-path session** (level start, Play Level N, tutorial first walk).
- Does **not** play after a same-path crash restart.
- Glimpse pickups stay a separate mid-run flash.

### 2. Where to click (FUE)

- Valid neighbors always get a pulsing **square line border** (edge, not a white fill).
- After the first tap of a walk, borders stay clearly brighter than the floor.
- Finger pointers on the tutorial and levels 1–5. They hide after the first correct step; borders remain.

### 3. Correct tap

- Hop squash/stretch, white splash, tile locks.
- Camera micro-nudge toward home.
- Rising Simon note (pitch up by step index).

### 4. Miss tap

1. 2–4 frame freeze (unscaled wait; do not leave `timeScale` at 0).
2. Mine blast on the wrong tile.
3. Spark from that tile to the **current HUD heart**, then the slice goes black.
4. True tile gold/white slam.
5. Magnet-pull the walker onto the true tile.
6. Low sting on the mine, reveal chime on the true tile.

Rejected: a heart flying from the HUD onto the board. That reads as the UI attacking the maze.

### 5. HUD hearts

- One **heart** = one walk / attempt (`runsPerSession`).
- Slices inside that heart = lives on that walk (`livesPerRun`).
- This 25-level ladder: 2 misses per walk everywhere. Levels 1–4 have 2 hearts; levels 5–25 have 3 hearts so a mid-game miss is recoverable.
- Spent walks: whole heart black.
- Future walks: full bright red.
- Current walk: spent slices black from the bottom; remaining red **dims toward gray** as that heart empties.

### 6. Wrong-tile arena effect

The miss sequence in (4) is the arena effect. The spark is the sentence "this step stole health."

### 7. Full walk fail (same path)

No Walk Over popup.

- Avatar flickers out at the fail tile (car-crash restart).
- Traveled white path and trail lerp back to original tile colors.
- Avatar flickers in at start.
- Floor palette does not reshuffle. Next heart becomes active.
- No radar replay.

### 8. Full level fail and success popups

- Success: "Splendid Memory!" with sparkle/confetti.
- Fail: bigger "You Failed!" with a dark wash and a heavier sting.
- Primary button is **Play Level N**, not Retry / Try Again / Replay.
- Secondary: Main Menu.

### 9. Copy

Lead with memory, never luck.

- Opening: remember the path home; watch, then walk.
- Prompt: tap a glowing neighbor.
- Miss: that tile cost a heart; remember the lit one.
- Walk fail: same path, from the start (shown by the crash restart, not a paragraph).
- Session fail: new path on Play Level N.

## Scene and boot

Open `Assets/Scenes/JourneyHub.unity` in this Unity project.

## Economy

Source of truth for the 25-level Tile Arena ladder. Specs live in `Assets/Game/Core/Domain/LevelCatalog.cs`. Rebuild designer copies with the Unity menus at the bottom. Do not invent a second ladder in a ScriptableObject without copying Core defaults.

Radar already shows the route. Difficulty is how twisty the snake is and how little time you get to encode it. Health is tight enough that a miss costs something, loose enough that level 10 is still a memory fight, not a one-miss game over.

Tutorial stays a gentle `4x3` on-ramp. Level 1 is a real `4x4` snake. Players should fail early enough to learn that watching the scan matters, then want to win.

Graph Arena (3 authored maps) and Scout Arena (6 mixed rows) keep their counts. Scout’s first three grid rows inherit Tile Arena levels 1–3.

### Five knobs

- **Grid size** — `LevelSpec` width / height. Floor is **4x4**. Cap this slice around **8x9** so phone tiles stay readable.
- **Path complexity** — `minTurns` + `lengthSlack`; turn band **2**. Seeds in one level should feel similar.
- **Walk health** — `LivesPerRun`. Mistakes before the walker restarts. HUD heart fill. **2** on every row.
- **Level health** — `RunsPerSession`. Hearts / retries. HUD heart count. **2** on levels 1–4, **3** from level 5.
- **Scan** — `PathPreviewSeconds.For(height, minTurns)` plus `Hold`. Sweep once, hold the full path briefly, then hide it.

Lighthouses, glimpse, beacon (milestone tiles) and blocked hints (crossed tiles) stay in code and stay **0** on every row. Bring them back only after level 25, on a dedicated teach level.

### Contract

- Levels **1–4**: 2 mistakes per walk, 2 hearts. Still meant to fail a walk if you ignore the scan.
- Levels **5–25**: 2 mistakes per walk, 3 hearts. Extra walk is the recover-and-finish loop (the second walk is easier because revealed mistakes persist).
- Never 1 miss per walk on this slice, and never 3 lives × 4–5 walks.

If live play still rage-quits around L10, add a fourth heart before shrinking the snake. Tougher boards can wait for levels 26+.

### 25-level ladder

`TurnBandWidth = 2`. All aid counts `0`. Always pass `runs` (the constructor default is 5).

Act 1 — Hook

- L1: 4x4, minTurns 6, lives 2, hearts 2, slack 5
- L2: 4x5, minTurns 7, lives 2, hearts 2, slack 5
- L3: 4x5, minTurns 8, lives 2, hearts 2, slack 6
- L4: 5x5, minTurns 8, lives 2, hearts 2, slack 6

Act 2 — Craft

- L5: 5x5, minTurns 9, lives 2, hearts 3, slack 6
- L6: 5x6, minTurns 9, lives 2, hearts 3, slack 6
- L7: 5x6, minTurns 10, lives 2, hearts 3, slack 6
- L8: 5x7, minTurns 10, lives 2, hearts 3, slack 6
- L9: 6x6, minTurns 11, lives 2, hearts 3, slack 6
- L10: 6x6, minTurns 12, lives 2, hearts 3, slack 6

Act 3 — Stretch

- L11: 6x7, minTurns 12, lives 2, hearts 3, slack 6
- L12: 6x7, minTurns 13, lives 2, hearts 3, slack 7
- L13: 6x8, minTurns 13, lives 2, hearts 3, slack 7
- L14: 6x8, minTurns 14, lives 2, hearts 3, slack 7
- L15: 7x7, minTurns 14, lives 2, hearts 3, slack 7
- L16: 7x7, minTurns 15, lives 2, hearts 3, slack 7
- L17: 7x8, minTurns 15, lives 2, hearts 3, slack 8
- L18: 7x8, minTurns 16, lives 2, hearts 3, slack 8

Act 4 — Climax

- L19: 7x9, minTurns 16, lives 2, hearts 3, slack 8
- L20: 7x9, minTurns 17, lives 2, hearts 3, slack 8
- L21: 8x8, minTurns 17, lives 2, hearts 3, slack 8
- L22: 8x8, minTurns 18, lives 2, hearts 3, slack 8
- L23: 8x8, minTurns 18, lives 2, hearts 3, slack 9
- L24: 8x9, minTurns 19, lives 2, hearts 3, slack 9
- L25: 8x9, minTurns 20, lives 2, hearts 3, slack 9

Smoothness for later rows: grow **one** of {width, height, minTurns} most levels; never drop unmarked difficulty (`minLength + minTurns` while aids are 0); keep `minTurns` well under `maxLength - 2` so path generation still succeeds.

Skip rules stay at `SkipEligibleAfterLevel = 8`.

### Scan formula

    sweep = clamp(0.12 * height + 0.035 * minTurns, 0.72, 1.65)
    hold  = 0.12

About 0.85s total on L1 and ~1.8s on L25. Fast enough that radar is a glance, not a study.

### Adding level 26+

1. Append a `LevelSpec` in `LevelCatalog` that follows the smoothness rules.
2. Leave aids at 0 unless you add a teach level first.
3. Bump `JourneyCatalogBuilder.TileArenaLevelCount` if Tile Arena should include the new rows.
4. Rebuild assets (menus below).
5. Run `LevelCatalogTests` and `PathPreviewSecondsTests`.

### Rebuild assets

- **Nixin Studio / Memory Grid Path / Reset Tuning To Nixin Defaults** — copies Core specs into `GridPathTuning.asset`.
- **Nixin Studio / Memory Grid Path / Catalog / Sync Tile Arena Economy From Core** — writes Core Grid rows onto the existing Journey catalog. Play uses Core even if the asset is stale.

`GridPathTuning.asset` and `JourneyCatalog.asset` are designer copies. Core defaults in `LevelCatalog` win when those menus run.
