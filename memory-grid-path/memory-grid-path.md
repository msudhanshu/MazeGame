Overview:
There will be grid like a chess board,  and user will start from source and reach to the destination.  This board will have particular path , like for different level there will be different path which we can generate and keep it saved for different levels.  
But path will be hidden at the beginning so the grid will be coloured in the normal way without revealing the path as user moves from one corner to another he will reveal the path in the process.
 so at every step user need to choose a grid-box among its neighbouring boxes and out of which only one box will be correct one which is part of the path so if he does any mistake is health will we decreased and the actual grid will be reveal to him so he can jamp to that one and again and that point of time he will have to choose the next part from surrounding box, like wise he will keep on moving till his health finishes. so suppose there were 3 health given to him. for one path journey that three health is gone he start the journey from the beginning again and still in the same game session so same path, but this time he should have memorize the last path grids till the point reached and again he will start moving with his memory. He might have forgotten some then he might consume more health .And this time, this run too, 3 health will we given to him. He can do  mistake up to three times and he will reach little further this time/run.		
Ocourse this three and five where examples you can make it configurable.
So based on how far he reaches after consuming all those 5 cross 3 health like 5 runs his score will be calculated.  if you complete the path till the end next level. will be unlocked. 
Next level will have a different path maybe little complicated with more turns. 

Health:
For each game session he will have five walks and in each work he will be allowed to do three mistakes which means three health but this you need to make it configurable and you should also come up with the code number with you find suitable

Level Progression:
So we need to create level and each level will have a particular type of complexity but its path should not be fixed like once user have played a level once in one session if he tries to play the game again he should not see the same path because he might have memorize it last time but you will have similar path so each level will have similar kind of path like path will be different but complexity level will be same like how many turns it has. So for each level you will have to define a method to create the path with the random function in walking to generate different kind of path at the runtime so each level will have a skeleton of the path maybe a c# function. 
Same with the next level will have little complicated path and it should be able to create many past for different sessions but complexity level will be same for that particular level.  so at the runtime it should take advantage of random methods to generate different kind of path but at some level it will have a conditioning to restrict the complication of that path.
As the level grows the complication will keep on increasing to have more turns and snake it can spend the whole matrix whole green that weight can keep on increasing user may not be able to play till that level but we should planning to created list 20 levels or something which can be increased in future.

Effects and View:
Should create the view with the abstraction layer so that it can be changed and modified with different variations so suppose at one Level and even inside that the effect of each tiles should we configurable and should be extendable for different variations that user can choose from the theme or maybe AS level progress the different look and feel will be given to the tiles and different effects will be used. 

To start with we can have one radiations where it will be like a dancing floor of DJ where is tile will be of different colours for maybe combination of 56 colours will be straight evenly cross the board and it should be having a glowing effect from below translucent box with the bright light at centre and little dark at the edges we should give realistic feel that it's a class for with the light kept when it of different colours. 
Coming to the vfx as the game progress so you when user walks we can have a little Android like character which chance on the path when you get chooses a particular box if the boxes incorrect it should glow in redis or some VFX to indicate that he has taken the wrong path and the real Parshad really itself and the revealed path will be turn into white glowing white colour or configurable colour and as he walks for the in that particular Run always be visible when he starts the next Run because of the less health and again.


## Economy : 

#### Agent definition:

You are expert in game tuning and design the game economy/levels/progression which best attracts the users and provide longer gameplay for user without loosing his interest.
As product manager and product content team manager, you need to plan the 
accordingly at all level. If you think some more features/improvement to be added for this cause you can suggest sometime.

According to me some improvemnet i could think:

1. Level progression should be slow. It is suddendly getting tougher for few people.
May be start with 3x3 for 2 level, then 4x4 for few, then 5x5 for few levels and simple then complicated.. likewise gradually it should increase.. And create upto 50 levels.
It should be focused to give good user experience and retention to us. 

2. we have two life. Session/Level life and walk life , 5 and 3 respectivly.
Do you think we should change it for better user exp. As level progress game becomes tougher,  should the be increased as level increased. 
May be session life can be kept same, but walk life can be increaswd with level? You think and suggest.
As level increases the luck factor (luck requirement , as more turns to choose) keeps on increasing, so should we incrase walk life. This game is more of to see how good memory is, luck factor should not be too much, it. may look boring. And for higher level, may be with given 3 walk life (luck factor) some level may not be playable/possible at all.
What do you say.

3. i want to add few features, for later levels.
we can introduce this feature, in earlier level also, as this also makes game interesting. Just keep this in account that, level should not become so easy after using these features/powers. The toughness progression should be maintained. 
For any one level , just to lure player and show feature whats coming in future level, we can give a glimps of it.

a. when path becomes tough and long, we can have some intermediate block in the path already marked white, which will help user as lighthouse to know how to proceed. So he doesnt have to trust on luck too much , he can focus on reaching the intermediate white and not target to destination from begning. 
But offcousre it will make game easier, so take that in account and make sure level toughness should not drop suddenly. Keeping this feature in accound for later level you decide the toughness of level. You also need to choose smartly how many (mostly stick to few) helper white should be exposed.


b. For some later/tougher level this might be userfull. There will be some hidden (one-time) switch/potion, when stepped in, can show the whole path , i.e all path light white, for a moment and dissapper. So player good memory can help him get good score and reach destination, by how much remembers the path in that glimps.
Or some tile switch can just reveal a white block in the path permanently (like adding a lighthouse). We will add a good vfx to show it to user, for now give the skeleton method and some easy animatkion with a good comment, which we can improve further with better vfx.

c. i want to have few tiles grayed out, which are not part of the path. So that tile is anyway not going to be covered. so we can keep few tiles greyed as hint for user to make it simpler for him. it can be of great help for higher and tougher levels. So you may have twike the design and economy accordinlgy.

Scoring system 
1. : May be we need to work on better scoring system.
The score should be global for a user login, as in it will keep accumulated as he progress with level.
(we can have a setting where he can clear gameplay data, to start fresh from level one..... u keep the fucntion for now, when ui will be created i will plugit in).
The score should be focused on the basis of , how good a player is in remembering, which means less weightage to luck factor more to memory.
Lets talk about score in a session/level:
So when by good luck he choose the correct path, he should be rewarded less in terms of score, but when he choose the path correct next time/walk he should be rewaarded more. 
But you will have to come with smart strategy. Because if user is not good with memory and he consumes all session/level life he should not be rewarded more, because it shows he have poor memory. I mean we can blindly add to score whenever he takes good turn from memory. Trying oout multiple time should penalize and decrease his score.

3. One more thing, as a new feature. After few levels (which will let user practice and luck only cannot make u win), we can start doing this (may be 2-3 level):
we can see how well he does in a level (not by luck but by memory).
Supoose for someone its very easy to play , his memory is so great. And if i push him to play simple level he may get bored. And too early if i introduced tough level to a player whose memory is not good may also loose interest.
how to deal with this, like dynamic tuining of economy?
I thought of one idea, you suggest if it is okay. We will keep level progression easy and smooth. But if a player scores very well for 2-3(configuratable) level consicutitevely , it means it is becoming too easy for him, so we can unlock (show little animation) skip power to him. So from next level (till some future level) he will get a popup at begning saying if you want to jump to next level without playing this one , but still be rewarded (some rough genric rewaard/scroe for that level will be anyway given to him). So he can choose to jump to next level without playing this borign level.
example : suppose he played so well, scored so well in level 6-7-8, then we can unlcok next 3 level with skip popup, so he will get option to skip 9-10-11 then from 12 he will be forced to play.
This is just wild idea i got. you critic it and if looks good, tune it properly and make it configurable. (for thse tuning configureation, even in last prompt which u dead with new feature... should be kept in scriptable object so that fron there it can be easily viwed and changed. .. and we can backup and try different tuining copy)




# Design:

I am developing a game in unity. Its memory based caseual game, a 2d game.

we need some HUD on top and in body part the game arena/tiles will show.

Desing cute simple casual style game.

1. The HUD need to be created well. It will have healths of two kind. (walk life of 3 (taken from config) and session life  of 5). We can have 5 energy bar, and each bar will be made up of walk-life count (i.e 3 in this example) segment. As user lose walk life that segment will change colour and may be animation of loosing the segment. when walk count of one session health is gone, some vfx to show it to user and this whole bar is changed/dead colour now.
2. In Hud it will show total scores, and current Level. steps to go to destination . Best score and session score may not be needed.

3. About game arena. come up with attractive eye pleasing desing. It can be dance floor like desing, but the color of it should be soothing to eye and looks attractive. If u think we can stick to less colors.
4. the path covered will be of white colour,  and it should be little different kind to clarify that its not general floor tile but actual path. it can be proper white with more or less glow than other.

1.over all app theme is do dull and dark. brighten up all. 2. the main game areana: the dance floor tiles are also looking dull and lack brighness and color vibrancy. it should glow like real dance floor. my first ref image was in that direction, but still it was not looking good, but you made it worse. May be in game tiles screen, background can remain dark to show dance floor contrast, but other poupup screen can hacve better bright color i guess. you decide what looks good for a game which kids, girls or people play casually.



# Develpment summary prompt:

## Memorize the Way Home — design brief
Working title: Memorize the Way Home
Play Store form: Memorize the Way Home
Short line: Watch the path. Walk it from memory.
Project / folder: memory-grid-path (Nixin Studio Unity workspace)
Product scene: Journey Hub (Assets/Scenes/JourneyHub.unity) — this is the new main game
Legacy scene: original GridPathPlay / SampleScene — leave as-is; separate save, separate home UI

Name note: “Home” is the feeling of the run (start → goal), not a house in the art. Boards may look like colored tiles, mosaics, rooftops, or map junctions. Do not require home/village story in thumbnails.

One-sentence game
A memory-path puzzle: the player is shown (or infers) a hidden route, then walks it cell-by-cell or node-by-node without seeing the full path, with lives, scoring, and a level ladder.

Core loop (all modes)
A route is generated (seeded) from start to goal under a difficulty envelope (length, turns, lives, runs).
Player moves only to legal next choices (orthogonal neighbors on a grid, or connected graph nodes).
Correct step: advance. Wrong step: reveal the true tile/node, lose a life.
Fail a walk: restart the same path from the start (limited runs). Fail all runs: session over, level stays locked.
Clear: score saved, next level unlocks; older levels stay replayable.
Shared rules kernel: memory/recall session, lives, runs, scout vs memory scoring. Presentation (grid vs graph, camera, textures) is per mode and per level, not one global look.

Product structure: three game types
Home shows three icons in a horizontal row. One is selected (default = last played). Shared strip shows that mode’s title, level, stars, career score. Play starts the current unlocked level of the selected mode. Levels opens that mode’s list.

Modes are separate careers (progress, score, last played). They are not three skins of one ladder.

Mode 1 — Tile Arena (always unlocked)
Board: 2D grid pathfinding.
Camera: fixed, full arena in view.
Look is per level, mixed in one list: some levels classic colored tiles (gaps, dance-floor), some mosaic (one image sliced across the board).
Level list: show a thumbnail of the arena (mosaic texture, or a color swatch if classic).
Placeholder content: first ~12 specs from the existing grid ladder; odd/even classic vs mosaic; mosaic can use tilebuilding.png or a procedural grid until real art lands.
Input: tap / swipe / WASD to an orthogonal neighbor that is a valid option.
Mode 2 — Graph Arena (locked until enough Tile clears)
Board: authored graph (junctions + edges) on a background image, not a 2D cell grid.
Camera: fixed, full arena visible.
Input: tap the next node (or a wrong connected node). Walker hops node to node.
Does not change the grid LevelCatalog / GridPathPlay pipeline. Parallel stack: GraphTopology / GraphPath / GraphWalkRun / GraphBoardView.
Placeholder content: a few graph levels (e.g. Sample Village, GraphLevel2, Easy Fork). Designer tool exists to place nodes on a background, connect edges, set start/goal, preview a seeded path.
Mode 3 — Scout Arena (locked until enough Graph clears)
One ladder that switches presentation per level — not a hybrid board with tiles and graph nodes at once.
Early levels: patchwork grid (random stable texture per cell from a texture set).
Later levels: easier graph maps.
Camera: follow walker, zoomed in (not full-board).
Art you will provide later: groups of patch tile images + full-page images for graph backgrounds. Until then: PatchworkTextureSet pools (BuildingTiles, BoxTiles, MixedArenaTiles) and placeholder graph backgrounds.
Placeholder mix: ~6 levels (e.g. 3 patchwork grids then 3 graphs).
Progression and locks
Inside one mode
Level 1 always open.
Next level unlocks only when the current frontier level is completed (or skipped, in the old grid skip system — hub graph/scout record completes the same way).
Player may replay any unlocked older level.
Stars on the select grid: 3 on cleared, 0 on current/locked (display, not skill-grade).
Across modes (configurable on JourneyCatalog)
Gate	Default	Meaning
Unlock Graph Arena
10 Tile Arena clears
Including completing the last Tile level if the catalog is short
Unlock Scout Arena
5 Graph Arena clears
Same cleared-count rule
Locked icons show a lock; tap shows a short reason (“Clear N Tile Arena levels to unlock”). Tile Arena is never locked.

Visual systems (four looks, used as building blocks)
These were first proven in Arena Test Lab (hotkeys 1–4). The product maps them onto modes/levels as above — not as a fourth home icon.

Look	What it is	Camera (typical)	Used in product
Classic color path
Colored tiles + gaps
Static full board
Tile Arena (some levels)
Mosaic
One image mapped across cells; lab can show coordinates
Static full board
Tile Arena (some levels)
Patchwork
Each cell picks a texture from a pool (stable per cell+seed)
Follow / zoom
Scout Arena (early levels)
Graph nodes
Circles + edges on a background
Static in mode 2; follow in mode 3
Graph Arena; Scout late levels
Patchwork data: ScriptableObject PatchworkTextureSet = theme id + list of Texture2D. ArenaVisualSettings can take a primary set + extra sets + inline textures; cells pick from the merged pool. Multiple grouping SOs (buildings, boxes, …) are assigned on settings / journey level entries — not hard-coded.

Important: Classic/mosaic/patchwork used to be one global ArenaVisualSettings for the whole old session. The hub applies per-level visual override. Do not assume LevelDefinition.ThemeId drives look (it is currently unused by old play).

Home / UX (Journey Hub only)
Three image icons in a row (sprites on JourneyCatalog; colored letter tiles if no art).
Selection highlight; last selected mode persisted.
Stats strip: LEVEL / STARS / SCORE for selected mode only.
Play / Levels / Settings.
Level select: lock / current / cleared; Tile Arena thumbnails.
In-play HUD: level, score, step, lives/runs, pause → resume / restart / quit to hub.
Complete / fail popups; next level or home.
Old HomeScreen (“Memory Path”, single career) stays for the legacy GridPathPlay scene only.

Data and saves
Legacy grid save: PlayerPrefs memory-grid-path.progress — one PlayerProgress. Do not mix with hub.
Hub save: PlayerPrefs memory-grid-path.journey — three PlayerProgress blobs + last selected mode + last played level per mode.
Catalog: JourneyCatalog SO — unlock thresholds + three JourneyModeDefinitions, each an ordered list of JourneyLevelEntry (Grid or Graph; visual type; grid spec; mosaic/patchwork refs; graph level ref; thumbnail).
Graph levels: GraphLevelDefinition (nodes, edges, background, start/goal, path shape, lives/runs) + GraphLevelCatalog for the old graph harness.
Grid difficulty: still LevelSpec / LevelCatalog (board size, turns, lives, runs, hints). Hub Tile/Scout grid rows reuse those specs.
Designer menus (Unity): Nixin Studio → Memory Grid Path → Open Journey Hub / Create Journey Catalog. Reset Progress clears both save keys.

Architecture (for further engineering)
Studio split: Game.Core = rules, no UnityEngine. Game.Unity = scenes, input, UI, cameras, textures.

Grid session: GridPathGame → GridWalkRun → GridBoardView + tile factories (DanceFloor / Mosaic / Patchwork).
Graph session: GraphWalkRun → GraphBoardView + circle nodes + edges. Editor window for authoring (draft in RAM, Save flushes to asset).
Package: packages/com.nixin.graph.core (topology, path, generator).
Boot: GridPathBoot auto-spawns old GridPathPlay unless the scene already has GridPathPlay, ArenaTestLab, GraphPathPlay, or JourneyHubPlay.
Never call Supabase/PostHog/Sentry/LLM from Core or gameplay. Client is untrusted for scores.
Out of scope (decided): hybrid board that is patchwork and graph at the same time; replacing old GridPathPlay UX; final store art; cloud saves.

Art still expected from the team
Home icons (3).
Mosaic full-board images (and optional level thumbnails).
Patchwork tile groups as PatchworkTextureSets (many small tiles).
Graph full-page backgrounds + node layouts in the graph editor.
Until then: placeholders (tilebuilding.png, tilebox.png, mixed patchwork set, sample graph maps).
Open product decisions (use this list next)
Title lock: Memorize the Way Home vs shorter store title + this as subtitle.
Mode display names on home (Tile / Graph / Scout vs player-facing names).
How many live levels per mode at first store drop vs placeholders.
Unlock numbers 10 / 5 — keep, or retune if Tile has fewer than 10 levels.
Whether “home” appears in copy/UI if art never shows a house.
Whether skip-charges from the old grid career exist on hub modes.
Default Play scene for shipping: Journey Hub vs le