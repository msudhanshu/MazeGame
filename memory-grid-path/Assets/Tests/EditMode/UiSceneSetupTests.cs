using Nixin.Ui;
using Nixin.Ui.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class UiSceneSetupTests
    {
        [Test]
        public void EnsurePutsScreensAndPopupsUnderNixinCanvas()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var host = UiSceneSetup.Ensure(scene);
                Assert.That(host.name, Is.EqualTo(UiHost.ObjectName));
                Assert.That(host.ScreenRoot, Is.Not.Null);
                Assert.That(host.PopupRoot, Is.Not.Null);
                Assert.That(host.ScreenRoot.parent.name, Is.EqualTo(UiHost.CanvasName));
                Assert.That(host.PopupRoot.parent.name, Is.EqualTo(UiHost.PopupsName));
                Assert.That(host.PopupRoot.parent.parent.name, Is.EqualTo(UiHost.CanvasName));
                Assert.That(host.ScreenRoot.parent.parent, Is.SameAs(host.transform));
                Assert.That(host.GetComponent<UiNavigator>(), Is.Not.Null);
                Assert.That(UiSceneSetup.LayerFor(host, popup: true), Is.SameAs(host.PopupRoot));
                Assert.That(UiSceneSetup.LayerFor(host, popup: false), Is.SameAs(host.ScreenRoot));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
