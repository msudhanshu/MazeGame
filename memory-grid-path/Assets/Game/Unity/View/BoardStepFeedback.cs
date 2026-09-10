using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Timing for step travel and the wrong-turn beat, shared by the play loops.
    /// </summary>
    public static class BoardStepFeedback
    {
        public const float MistakeHoldSeconds = 0.85f;
        public const float WrongTurnFlashSeconds = 0.55f;
        public const float WrongTurnFollowDelaySeconds = 0.14f;
        public const float RewindCameraSeconds = 0.55f;

        public static IEnumerator HoldThenTravel(WalkerView walker, Vector3 destination, float holdSeconds)
        {
            if (holdSeconds > 0f)
                yield return new WaitForSeconds(holdSeconds);

            if (walker == null)
                yield break;

            walker.HopTo(destination, WalkerView.MistakeTravelSeconds);
            while (walker != null && walker.IsHopping)
                yield return null;
        }

        public static IEnumerator FlashWrongTurnThenTravel(
            WalkerView walker,
            GridPathOverlay overlay,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Vector3 destination) =>
            FlashWrongTurnThenTravel(walker, overlay, wrongFrom, wrongTo, destination, null, null);

        public static IEnumerator FlashWrongTurnThenTravel(
            WalkerView walker,
            GridPathOverlay overlay,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Vector3 destination,
            IReadOnlyList<Vector3> hopPath) =>
            FlashWrongTurnThenTravel(walker, overlay, wrongFrom, wrongTo, destination, hopPath, null);

        public static IEnumerator FlashWrongTurnThenTravel(
            WalkerView walker,
            GridPathOverlay overlay,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Vector3 destination,
            IReadOnlyList<Vector3> hopPath,
            System.Action afterFlash)
        {
            overlay?.ShowWrongTurn(wrongFrom, wrongTo, WrongTurnFlashSeconds);
            if (WrongTurnFlashSeconds > 0f)
                yield return new WaitForSeconds(WrongTurnFlashSeconds);

            afterFlash?.Invoke();

            if (WrongTurnFollowDelaySeconds > 0f)
                yield return new WaitForSeconds(WrongTurnFollowDelaySeconds);

            if (walker == null)
                yield break;

            if (hopPath != null && hopPath.Count >= 2)
                walker.HopAlong(hopPath, WalkerView.MistakeTravelSeconds);
            else
                walker.HopTo(destination, WalkerView.MistakeTravelSeconds);
            while (walker != null && walker.IsHopping)
                yield return null;
        }

        public static IEnumerator LerpCamera(
            Camera camera,
            Vector3 fromPosition,
            float fromSize,
            Vector3 toPosition,
            float toSize,
            float seconds)
        {
            if (camera == null || seconds <= 0.01f)
            {
                if (camera != null)
                    ApplyOrtho(camera, toPosition, toSize);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                ApplyOrtho(
                    camera,
                    Vector3.Lerp(fromPosition, toPosition, t),
                    Mathf.Lerp(fromSize, toSize, t));
                yield return null;
            }

            ApplyOrtho(camera, toPosition, toSize);
        }

        static void ApplyOrtho(Camera camera, Vector3 position, float orthographicSize)
        {
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(position, BoardCamera.TopDownRotation);
            camera.orthographicSize = Mathf.Max(0.5f, orthographicSize);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = BoardCamera.FarClip;
        }
    }
}
