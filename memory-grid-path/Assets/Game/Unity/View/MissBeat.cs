using System;
using System.Collections;
using System.Collections.Generic;
using Game.Unity.Audio;
using Game.Unity.Themes;
using Game.Unity.Ui;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>The miss scream: hop onto the mine, blast, retreat, then magnet-pull to the true tile.</summary>
    public static class MissBeat
    {
        public const int FreezeFrames = 3;

        public static IEnumerator Play(
            MonoBehaviour host,
            WalkerView walker,
            GridPathOverlay overlay,
            Camera camera,
            GridPathHud hud,
            ITileView wrong,
            ITileView revealed,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Vector3 destination,
            int heartRun,
            float tileSize,
            bool intense,
            Action onSpark,
            Action afterFlash,
            Action after,
            bool magnetPull = true,
            IReadOnlyList<Vector3> approachPath = null,
            IReadOnlyList<Vector3> magnetPath = null,
            Action paintWrong = null,
            Action flashRevealed = null,
            bool walkWrongPath = true)
        {
            _ = host;
            for (var i = 0; i < FreezeFrames; i++)
                yield return null;

            var wrongStand = Flatten(wrongTo, destination);
            var originStand = Flatten(wrongFrom, destination);
            var approach = FlattenPath(approachPath, destination.y);
            var magnet = FlattenPath(magnetPath, destination.y);
            if (walkWrongPath)
            {
                if (approach != null)
                    yield return HopPathThenWait(walker, approach, WalkerView.MissApproachSeconds);
                else
                    yield return HopThenWait(walker, wrongStand, WalkerView.MissApproachSeconds);
            }

            paintWrong?.Invoke();
            if (wrong != null)
            {
                wrong.SetState(intense ? TileVisualState.WrongIntense : TileVisualState.Wrong);
                wrong.Flash(
                    intense ? TileVisualState.WrongIntense : TileVisualState.Wrong,
                    intense ? DanceFloorEffects.WrongIntenseFlashSeconds : DanceFloorEffects.WrongFlashSeconds);
            }

            MemoryPathAudio.PlayMistake();
            overlay?.ShowWrongTurn(wrongFrom, wrongTo, BoardStepFeedback.WrongTurnFlashSeconds);
            yield return MineBlast.Play(wrongTo, tileSize);

            var sparkDone = false;
            if (hud != null && camera != null)
            {
                yield return HudHeartSpark.Fly(
                    hud.OverlayRoot,
                    camera,
                    wrongTo,
                    hud.HeartRect(heartRun),
                    () =>
                    {
                        onSpark?.Invoke();
                        sparkDone = true;
                    });
            }
            else
            {
                onSpark?.Invoke();
                sparkDone = true;
            }

            while (!sparkDone)
                yield return null;

            hud?.PulseHealth();

            if (revealed != null)
                revealed.Flash(TileVisualState.Revealed, 0.45f);
            flashRevealed?.Invoke();
            if (revealed != null || flashRevealed != null)
            {
                MemoryPathAudio.PlayRevealChime();
            }

            afterFlash?.Invoke();

            if (magnetPull)
            {
                if (walkWrongPath)
                {
                    if (approach != null && approach.Length >= 2)
                        yield return HopPathThenWait(walker, ReverseCopy(approach), WalkerView.MissApproachSeconds);
                    else
                        yield return HopThenWait(walker, originStand, WalkerView.MissApproachSeconds);
                }

                if (BoardStepFeedback.WrongTurnFollowDelaySeconds > 0f)
                    yield return new WaitForSeconds(BoardStepFeedback.WrongTurnFollowDelaySeconds);

                var correctionSeconds = walkWrongPath ? WalkerView.MistakeTravelSeconds : (float?)null;
                if (magnet != null && magnet.Length >= 2)
                    yield return HopPathThenWait(walker, magnet, correctionSeconds);
                else
                    yield return HopThenWait(walker, destination, correctionSeconds);
            }

            after?.Invoke();
        }

        static IEnumerator HopThenWait(WalkerView walker, Vector3 to, float? seconds = null)
        {
            if (walker == null)
                yield break;
            if (seconds.HasValue)
                walker.HopTo(to, seconds.Value);
            else
                walker.HopTo(to);
            while (walker != null && walker.IsHopping)
                yield return null;
        }

        static IEnumerator HopPathThenWait(WalkerView walker, Vector3[] path, float? seconds = null)
        {
            if (walker == null || path == null || path.Length == 0)
                yield break;
            if (path.Length < 2)
            {
                if (seconds.HasValue)
                    walker.HopTo(path[0], seconds.Value);
                else
                    walker.HopTo(path[0]);
            }
            else if (seconds.HasValue)
            {
                walker.HopAlong(path, seconds.Value);
            }
            else
            {
                walker.HopAlong(path);
            }

            while (walker != null && walker.IsHopping)
                yield return null;
        }

        static Vector3 Flatten(Vector3 lifted, Vector3 ground)
        {
            return new Vector3(lifted.x, ground.y, lifted.z);
        }

        static Vector3[] FlattenPath(IReadOnlyList<Vector3> path, float y)
        {
            if (path == null || path.Count == 0)
                return null;

            var points = new Vector3[path.Count];
            for (var i = 0; i < path.Count; i++)
                points[i] = new Vector3(path[i].x, y, path[i].z);
            return points;
        }

        static Vector3[] ReverseCopy(Vector3[] path)
        {
            var reversed = new Vector3[path.Length];
            for (var i = 0; i < path.Length; i++)
                reversed[i] = path[path.Length - 1 - i];
            return reversed;
        }
    }
}
