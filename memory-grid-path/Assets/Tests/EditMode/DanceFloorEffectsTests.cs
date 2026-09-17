using Game.Unity.Audio;
using Game.Unity.Themes;
using Game.Unity.Ui;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class DanceFloorEffectsTests
    {
        [Test]
        public void PlayMistakePaintsOnlyTheWrongTileRed()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var host = new GameObject("FxHost");
            try
            {
                PlayerSettingsStore.SoundEffects = false;
                MemoryPathAudio.ResetForTests();
                var fx = DanceFloorEffects.Create(host.transform);
                var wrong = new FakeTileView(new GridCoord(1, 0));
                var revealed = new FakeTileView(new GridCoord(0, 1));

                fx.PlayMistake(wrong, revealed);

                Assert.That(wrong.State, Is.EqualTo(TileVisualState.Wrong));
                Assert.That(revealed.State, Is.EqualTo(TileVisualState.Idle));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PlayMistakeIntenseUsesHarderWrongState()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var host = new GameObject("FxHostIntense");
            try
            {
                PlayerSettingsStore.SoundEffects = false;
                MemoryPathAudio.ResetForTests();
                var fx = DanceFloorEffects.Create(host.transform);
                var wrong = new FakeTileView(new GridCoord(1, 0));

                fx.PlayMistake(wrong, null, intense: true);

                Assert.That(wrong.State, Is.EqualTo(TileVisualState.WrongIntense));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PlayCorrectPaintsTheTileWalked()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var host = new GameObject("FxHostCorrect");
            try
            {
                PlayerSettingsStore.SoundEffects = false;
                MemoryPathAudio.ResetForTests();
                var fx = DanceFloorEffects.Create(host.transform);
                var tile = new FakeTileView(new GridCoord(0, 1));

                fx.PlayCorrect(tile, 3);

                Assert.That(tile.State, Is.EqualTo(TileVisualState.Walked));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(host);
            }
        }

        sealed class FakeTileView : ITileView
        {
            public FakeTileView(GridCoord coord) => Coord = coord;

            public GridCoord Coord { get; }
            public TileVisualState State { get; private set; } = TileVisualState.Idle;

            public void SetState(TileVisualState state) => State = state;

            public void Flash(TileVisualState state, float seconds)
            {
            }

            public void Destroy()
            {
            }
        }
    }
}
