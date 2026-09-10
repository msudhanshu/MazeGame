using System;
using Game.Unity.Ui;
using Nixin.Ui;
using Nixin.Ui.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Unity.Editor
{
    [InitializeOnLoad]
    public static class MemoryPathUiPrefabBuilder
    {
        public const string Folder = "Assets/Game/Unity/Ui/Resources/Ui";

        const string JourneyHubPath = "Assets/Scenes/JourneyHub.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        static readonly ViewBake[] Views =
        {
            new ViewBake("MemoryPathPopup", "_popup", true, true, () => MemoryPathPopup.CreateTemplate(), popup: true),
            new ViewBake("JourneyHomeScreen", "_home", true, false, () => JourneyHomeScreen.CreateTemplate()),
            new ViewBake("JourneyLevelSelectScreen", "_levelSelect", true, false, () => JourneyLevelSelectScreen.CreateTemplate()),
            new ViewBake("HomeScreen", "_home", false, true, () => HomeScreen.CreateTemplate()),
            new ViewBake("LevelSelectScreen", "_levelSelect", false, true, () => LevelSelectScreen.CreateTemplate()),
            new ViewBake("LevelDetailScreen", "_levelDetail", true, true, () => LevelDetailScreen.CreateTemplate()),
            new ViewBake("SettingsScreen", "_settings", true, true, () => SettingsScreen.CreateTemplate()),
            new ViewBake("GridPathHud", "_hud", true, true, () => GridPathHud.Create(null), hud: true)
        };

        static MemoryPathUiPrefabBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Bake UI Prefabs")]
        [MenuItem("Nixin Studio/Memory Grid Path/Bake Popup Prefabs")]
        public static void BakeMenu()
        {
            BakePrefabs();
        }

        public static void BakeFromCli()
        {
            BakePrefabs();
        }

        static void BakePrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode, then run Nixin Studio / Memory Grid Path / Bake UI Prefabs.");
                return;
            }
            try
            {
                BakeMissing(force: true);
                PlaceInPlayScenes(openUnloaded: true);
                AssetDatabase.SaveAssets();
                Debug.Log("Memory Path UI prefabs saved under " + Folder + " and placed in play scenes.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        static void EnsureAssets()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            try
            {
                var missing = AnyMissing();
                var artMissing = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/SliceRounded.png") == null;
                if (missing || artMissing)
                    BakeMissing(force: artMissing);
                PlaceInPlayScenes(openUnloaded: missing || artMissing);
                if (missing || artMissing)
                    AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        static bool AnyMissing()
        {
            for (var i = 0; i < Views.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Views[i].PrefabPath) == null)
                    return true;
            }

            return false;
        }

        static void BakeMissing(bool force)
        {
            System.IO.Directory.CreateDirectory(Folder);
            MemoryPathUiArtBaker.Ensure(force);
            for (var i = 0; i < Views.Length; i++)
                Bake(Views[i], force);
        }

        static GameObject Bake(ViewBake view, bool force)
        {
            if (!force)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(view.PrefabPath);
                if (existing != null)
                    return existing;
            }

            var canvasGo = new GameObject("_BakeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);

            var source = view.Create();
            source.transform.SetParent(canvasGo.transform, false);
            source.gameObject.SetActive(true);
            if (view.Hud)
                source.GetComponent<GridPathHud>().SetVisible(false);

            Canvas.ForceUpdateCanvases();
            var sourceRect = source.GetComponent<RectTransform>();
            if (sourceRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(sourceRect);

            source.transform.SetParent(null, false);
            source.gameObject.SetActive(view.Hud);
            UnityEngine.Object.DestroyImmediate(canvasGo);
            MemoryPathUiArtBaker.Persist(source.gameObject);

            var prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, view.PrefabPath);
            UnityEngine.Object.DestroyImmediate(source.gameObject);
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(view.PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Failed to save " + view.PrefabPath);
            AssetDatabase.ImportAsset(view.PrefabPath);
            return prefab;
        }

        static void PlaceInPlayScenes(bool openUnloaded)
        {
            var loaded = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.path != JourneyHubPath && scene.path != SampleScenePath)
                    continue;
                if (PlaceInScene(scene))
                    EditorSceneManager.SaveScene(scene);
                loaded.Add(scene.path);
            }

            if (!openUnloaded)
                return;

            PlaceUnloaded(JourneyHubPath, loaded);
            PlaceUnloaded(SampleScenePath, loaded);
        }

        static void PlaceUnloaded(string path, System.Collections.Generic.HashSet<string> loaded)
        {
            if (loaded.Contains(path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                return;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (PlaceInScene(scene))
                EditorSceneManager.SaveScene(scene);
            if (EditorSceneManager.GetActiveScene().path != path)
                EditorSceneManager.CloseScene(scene, false);
        }

        static bool PlaceInScene(Scene scene)
        {
            var journey = scene.path == JourneyHubPath;
            var sample = scene.path == SampleScenePath;
            MonoBehaviour host = FindHost(scene);
            if (host == null)
                return false;

            var ui = UiSceneSetup.Ensure(scene);
            MemoryPathUi.ApplyPortraitCanvas(ui.GetComponent<UiNavigator>());
            var changed = false;

            for (var i = 0; i < Views.Length; i++)
            {
                var view = Views[i];
                if (journey && !view.Journey)
                    continue;
                if (sample && !view.Sample)
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(view.PrefabPath);
                if (prefab == null)
                    prefab = Bake(view, force: false);
                if (PlaceView(scene, host, ui, view, prefab))
                    changed = true;
            }

            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);
            return changed;
        }

        static MonoBehaviour FindHost(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var journey = roots[i].GetComponent<JourneyHubPlay>();
                if (journey != null)
                    return journey;
                var grid = roots[i].GetComponent<GridPathPlay>();
                if (grid != null)
                    return grid;
            }

            return null;
        }

        static bool PlaceView(Scene scene, MonoBehaviour host, UiHost ui, ViewBake view, GameObject prefab)
        {
            var hudPrefab = prefab.GetComponent<GridPathHud>();
            var type = hudPrefab != null ? typeof(GridPathHud) : prefab.GetComponent<UiView>().GetType();
            var existing = FindExisting(scene, type);
            var layer = view.Hud
                ? (host != null ? host.transform : null)
                : UiSceneSetup.LayerFor(ui, view.Popup);
            var changed = false;

            if (existing == null)
            {
                var instance = layer != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, layer)
                    : (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = view.Name;
                instance.SetActive(view.Hud);
                var hud = instance.GetComponent<GridPathHud>();
                if (hud != null)
                    hud.SetVisible(false);
                existing = hud != null ? (Component)hud : instance.GetComponent<UiView>();
                changed = true;
            }
            else if (layer != null && existing.transform.parent != layer)
            {
                existing.transform.SetParent(layer, false);
                existing.gameObject.SetActive(view.Hud);
                changed = true;
            }

            if (host == null || existing == null)
                return changed;

            var serialized = new SerializedObject(host);
            var property = serialized.FindProperty(view.HostProperty);
            if (property == null || property.objectReferenceValue == existing)
                return changed;

            property.objectReferenceValue = existing;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static Component FindExisting(Scene scene, Type type)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren(type, true);
                if (found != null)
                    return found;
            }

            return null;
        }

        sealed class ViewBake
        {
            public readonly string Name;
            public readonly string HostProperty;
            public readonly bool Journey;
            public readonly bool Sample;
            public readonly Func<Component> Create;
            public readonly bool Hud;
            public readonly bool Popup;
            public string PrefabPath => Folder + "/" + Name + ".prefab";

            public ViewBake(
                string name,
                string hostProperty,
                bool journey,
                bool sample,
                Func<Component> create,
                bool hud = false,
                bool popup = false)
            {
                Name = name;
                HostProperty = hostProperty;
                Journey = journey;
                Sample = sample;
                Create = create;
                Hud = hud;
                Popup = popup;
            }
        }
    }
}
