using Nixin.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class UiHostCanvasTests
    {
        GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void BuildReusesExistingCanvasLayers()
        {
            _root = new GameObject(UiHost.ObjectName);
            var canvas = new GameObject(UiHost.CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(_root.transform, false);
            var screens = new GameObject(UiHost.ScreensName, typeof(RectTransform));
            screens.transform.SetParent(canvas.transform, false);
            var popups = new GameObject(UiHost.PopupsName, typeof(RectTransform));
            popups.transform.SetParent(canvas.transform, false);

            var host = _root.AddComponent<UiHost>();
            host.Build();

            Assert.That(host.ScreenRoot.gameObject, Is.SameAs(screens));
            Assert.That(host.PopupRoot.parent.gameObject, Is.SameAs(popups));
            Assert.That(host.PopupRoot.name, Is.EqualTo(UiHost.PopupContentName));
            Assert.That(host.ScreenRoot.GetComponent<SafeAreaFitter>(), Is.Null);
            Assert.That(host.PopupRoot.GetComponent<SafeAreaFitter>(), Is.Not.Null);
            Assert.That(_root.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void GetOrCreateInsetsScreenChromeAndKeepsBackgroundBleed()
        {
            _root = new GameObject(UiHost.ObjectName);
            var host = _root.AddComponent<UiHost>();
            host.Build();

            var screen = Game.Unity.Ui.JourneyHomeScreen.CreateTemplate();
            try
            {
                var view = host.GetOrCreate(new UiEntry
                {
                    Id = "JourneyHomeScreen",
                    Kind = UiKind.Screen,
                    Prefab = screen
                });

                Assert.That(view, Is.SameAs(screen));
                Assert.That(screen.transform.Find(SafeAreaLayout.BleedName), Is.Not.Null);
                Assert.That(screen.transform.Find(SafeAreaLayout.BleedName).parent, Is.SameAs(screen.transform));
                var safe = screen.transform.Find(SafeAreaLayout.RootName);
                Assert.That(safe, Is.Not.Null);
                Assert.That(safe.GetComponent<SafeAreaFitter>(), Is.Not.Null);
                Assert.That(screen.FindChrome("Column"), Is.Not.Null);
                Assert.That(screen.FindChrome("Column").parent, Is.SameAs(safe));
                Assert.That(host.ScreenRoot.GetComponent<SafeAreaFitter>(), Is.Null);

                host.GetOrCreate(new UiEntry
                {
                    Id = "JourneyHomeScreen",
                    Kind = UiKind.Screen,
                    Prefab = screen
                });
                var safes = 0;
                for (var i = 0; i < screen.transform.childCount; i++)
                {
                    if (screen.transform.GetChild(i).name == SafeAreaLayout.RootName)
                        safes++;
                }

                Assert.That(safes, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void GetOrCreateDoesNotWrapPopupChrome()
        {
            _root = new GameObject(UiHost.ObjectName);
            var host = _root.AddComponent<UiHost>();
            host.Build();

            var popup = ConfirmPopup.CreateTemplate();
            popup.transform.SetParent(host.PopupRoot, false);
            host.GetOrCreate(new UiEntry
            {
                Id = "ConfirmPopup",
                Kind = UiKind.Popup,
                Prefab = popup
            });

            Assert.That(popup.transform.Find(SafeAreaLayout.RootName), Is.Null);
            Assert.That(host.PopupRoot.GetComponent<SafeAreaFitter>(), Is.Not.Null);
        }

        [Test]
        public void GetOrCreateLeavesSceneViewOnCanvasLayer()
        {
            _root = new GameObject(UiHost.ObjectName);
            var host = _root.AddComponent<UiHost>();
            host.Build();

            var popup = ConfirmPopup.CreateTemplate();
            popup.transform.SetParent(host.PopupRoot, false);

            var view = host.GetOrCreate(new UiEntry
            {
                Id = "ConfirmPopup",
                Kind = UiKind.Popup,
                Prefab = popup
            });

            Assert.That(view, Is.SameAs(popup));
            Assert.That(view.transform.parent, Is.SameAs(host.PopupRoot));
        }

        [Test]
        public void GetOrCreateMovesStraySceneViewOntoCanvas()
        {
            _root = new GameObject(UiHost.ObjectName);
            var host = _root.AddComponent<UiHost>();
            host.Build();

            var strayParent = new GameObject("PlayHost");
            var popup = ConfirmPopup.CreateTemplate();
            popup.transform.SetParent(strayParent.transform, false);

            host.GetOrCreate(new UiEntry
            {
                Id = "ConfirmPopup",
                Kind = UiKind.Popup,
                Prefab = popup
            });

            Assert.That(popup.transform.parent, Is.SameAs(host.PopupRoot));
            Object.DestroyImmediate(strayParent);
        }

        [Test]
        public void EnsureUsesSceneNixinUiInsteadOfCreatingAnother()
        {
            _root = new GameObject(UiHost.ObjectName);
            _root.AddComponent<UiHost>();
            var nav = _root.AddComponent<UiNavigator>();

            var ensured = UiNavigator.Ensure();
            Assert.That(ensured, Is.SameAs(nav));
            Assert.That(ensured.gameObject, Is.SameAs(_root));
        }

        [Test]
        public void TryHandleBackClosesOpenPopup()
        {
            _root = new GameObject(UiHost.ObjectName);
            _root.AddComponent<UiHost>();
            var nav = _root.AddComponent<UiNavigator>();
            UiNavigator.Ensure();
            var popup = ConfirmPopup.CreateTemplate();
            nav.Register(popup, UiKind.Popup);
            nav.Open<ConfirmPopup, ConfirmPayload>(new ConfirmPayload { Title = "Hi" });

            Assert.That(popup.IsOpen, Is.True);
            Assert.That(nav.TryHandleBack(), Is.True);
            Assert.That(popup.IsOpen, Is.False);
        }
    }
}
