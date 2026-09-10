using System;
using System.IO;
using Game.Core.Memory;
using Game.Unity.Memory;
using Nixin.Maze;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Writes default memory-wall textures, prefabs, and ScriptableObjects for maze-arena.
    /// </summary>
    public static class MemoryWallDefaultAssetGenerator
    {
        const string ContentRoot = "Assets/Game/Memory";
        const string ResourcesRoot = "Assets/Resources/Memory";

        [MenuItem("Nixin Studio/Maze Arena/Generate Default Memory Assets")]
        public static void Generate()
        {
            EnsureFolders();
            var photoMat = LoadPhotoMaterial();
            var reliefMat = CreateOrUpdateReliefMaterial();

            var plainPhoto = CreatePhotoFramePrefab("PhotoPlain", photoMat, FrameStyle.Plain);
            var framePhoto = CreatePhotoFramePrefab("PhotoFrame", photoMat, FrameStyle.WoodenFrame);
            var glowPhoto = CreatePhotoFramePrefab("PhotoGlow", photoMat, FrameStyle.Glow);
            var polaroidPhoto = CreatePhotoFramePrefab("PhotoPolaroid", photoMat, FrameStyle.Polaroid);
            var metalPhoto = CreatePhotoFramePrefab("PhotoMetal", photoMat, FrameStyle.Metal);

            var textures = CreatePhotoTextures();
            var reliefTextures = CreateReliefTextures();
            var objects = CreateObjectPrefabs();

            var catalog = CreateOrUpdateCatalog(textures, reliefTextures, objects);
            SyncMazePhotoResources(textures);
            var level = CreateOrUpdateLevel();
            var kit = CreateOrUpdateDisplayKit(plainPhoto, framePhoto, glowPhoto, polaroidPhoto, metalPhoto, reliefMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Memory wall defaults generated.\n"
                + "Catalog: " + AssetDatabase.GetAssetPath(catalog) + "\n"
                + "Level: " + AssetDatabase.GetAssetPath(level) + "\n"
                + "Display kit: " + AssetDatabase.GetAssetPath(kit));
        }

        public static void GenerateFromCli()
        {
            Generate();
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Game", "Memory");
            EnsureFolder(ContentRoot, "Textures");
            EnsureFolder(ContentRoot + "/Textures", "Photos");
            EnsureFolder(ContentRoot + "/Textures", "Relief");
            EnsureFolder(ContentRoot, "Materials");
            EnsureFolder(ContentRoot, "Prefabs");
            EnsureFolder(ContentRoot + "/Prefabs", "Frames");
            EnsureFolder(ContentRoot + "/Prefabs", "Objects");
            EnsureFolder("Assets/Resources", "Memory");
        }

        static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        enum FrameStyle
        {
            Plain,
            WoodenFrame,
            Glow,
            Polaroid,
            Metal
        }

        static Material LoadPhotoMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.nixin.maze/Runtime/Unity/Resources/NixinMaze/PhotoUnlit.mat");
            if (mat != null)
                return mat;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            return new Material(shader) { color = Color.white };
        }

        static Material CreateOrUpdateReliefMaterial()
        {
            var path = ContentRoot + "/Materials/ReliefLit.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "ReliefLit" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.enableInstancing = true;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.15f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreatePhotoFramePrefab(string name, Material photoMat, FrameStyle style)
        {
            var path = ContentRoot + "/Prefabs/Frames/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var root = new GameObject(name);

            switch (style)
            {
                case FrameStyle.WoodenFrame:
                    AddMouldingFrame(root, name + "Wood", new Color(0.42f, 0.28f, 0.16f), thickness: 0.1f, depth: 0.05f);
                    break;
                case FrameStyle.Metal:
                    AddMouldingFrame(root, name + "Metal", new Color(0.72f, 0.74f, 0.78f), thickness: 0.055f, depth: 0.03f);
                    break;
                case FrameStyle.Polaroid:
                    var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    backing.name = "Backing";
                    UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
                    backing.transform.SetParent(root.transform, false);
                    backing.transform.localScale = new Vector3(1.18f, 1.42f, 0.02f);
                    backing.transform.localPosition = new Vector3(0f, -0.12f, 0.012f);
                    backing.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(
                        name + "Backing", new Color(0.96f, 0.96f, 0.94f));
                    break;
                case FrameStyle.Glow:
                    var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    halo.name = "Halo";
                    UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());
                    halo.transform.SetParent(root.transform, false);
                    halo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                    halo.transform.localPosition = new Vector3(0f, 0f, 0.008f);
                    halo.transform.localRotation = MazeGeometry.PictureQuadLocalRotation;
                    var haloMat = CreateSolidMaterial(name + "Halo", new Color(1f, 0.85f, 0.45f));
                    if (haloMat.HasProperty("_EmissionColor"))
                    {
                        haloMat.EnableKeyword("_EMISSION");
                        haloMat.SetColor("_EmissionColor", new Color(0.55f, 0.38f, 0.12f));
                    }

                    halo.GetComponent<Renderer>().sharedMaterial = haloMat;
                    break;
            }

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mesh.name = "Mesh";
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localRotation = MazeGeometry.PictureQuadLocalRotation;
            if (style == FrameStyle.Polaroid)
                mesh.transform.localPosition = new Vector3(0f, 0.08f, 0.022f);

            Material meshMat;
            if (style == FrameStyle.Glow)
            {
                meshMat = CreateSolidMaterial(name + "Glow", new Color(1f, 0.92f, 0.7f));
                if (meshMat.HasProperty("_EmissionColor"))
                {
                    meshMat.EnableKeyword("_EMISSION");
                    meshMat.SetColor("_EmissionColor", new Color(0.35f, 0.28f, 0.12f));
                }
            }
            else
            {
                meshMat = photoMat;
            }

            mesh.GetComponent<Renderer>().sharedMaterial = meshMat;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static void AddMouldingFrame(GameObject root, string matName, Color color, float thickness, float depth)
        {
            var frame = new GameObject("Frame");
            frame.transform.SetParent(root.transform, false);
            frame.transform.localScale = new Vector3(1.14f, 1.14f, 1f);
            var mat = CreateSolidMaterial(matName, color);
            AddSlat(frame, "Top", new Vector3(0f, 0.5f - thickness * 0.5f, depth * 0.5f), new Vector3(1f, thickness, depth), mat);
            AddSlat(frame, "Bottom", new Vector3(0f, -0.5f + thickness * 0.5f, depth * 0.5f), new Vector3(1f, thickness, depth), mat);
            AddSlat(frame, "Left", new Vector3(-0.5f + thickness * 0.5f, 0f, depth * 0.5f), new Vector3(thickness, 1f - thickness * 2f, depth), mat);
            AddSlat(frame, "Right", new Vector3(0.5f - thickness * 0.5f, 0f, depth * 0.5f), new Vector3(thickness, 1f - thickness * 2f, depth), mat);
        }

        static void AddSlat(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slat.name = name;
            UnityEngine.Object.DestroyImmediate(slat.GetComponent<Collider>());
            slat.transform.SetParent(parent.transform, false);
            slat.transform.localPosition = localPos;
            slat.transform.localScale = localScale;
            slat.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material CreateSolidMaterial(string name, Color color)
        {
            var path = ContentRoot + "/Materials/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        struct TextureSet
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        static System.Collections.Generic.Dictionary<string, Texture2D> CreatePhotoTextures()
        {
            var map = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.Ordinal);
            map["desert"] = LoadOrCreatePhoto("desert", new Color(0.86f, 0.72f, 0.38f), PatternKind.Gradient);
            map["waterfall"] = LoadOrCreatePhoto("waterfall", new Color(0.35f, 0.68f, 0.82f), PatternKind.Waves);

            var paintings = new[] { "monalisa", "starrynight", "scream", "persistence", "waterlilies", "girlpearl" };
            var paintingColors = new[]
            {
                new Color(0.35f, 0.25f, 0.15f),
                new Color(0.15f, 0.25f, 0.55f),
                new Color(0.65f, 0.35f, 0.25f),
                new Color(0.45f, 0.45f, 0.35f),
                new Color(0.25f, 0.55f, 0.45f),
                new Color(0.55f, 0.45f, 0.55f)
            };
            for (var i = 0; i < paintings.Length; i++)
                map[paintings[i]] = LoadOrCreatePhoto(paintings[i], paintingColors[i], PatternKind.Gradient);

            var arts = new[] { "studio", "illustration", "watercolor", "sculpture", "gallery", "museum" };
            var artColors = new[]
            {
                new Color(0.45f, 0.35f, 0.25f),
                new Color(0.25f, 0.45f, 0.35f),
                new Color(0.35f, 0.25f, 0.45f),
                new Color(0.55f, 0.55f, 0.55f),
                new Color(0.65f, 0.55f, 0.45f),
                new Color(0.45f, 0.55f, 0.65f)
            };
            for (var i = 0; i < arts.Length; i++)
                map[arts[i]] = LoadOrCreatePhoto(arts[i], artColors[i], PatternKind.Gradient);

            AddDroppedPhotos(map);
            return map;
        }

        static Texture2D LoadOrCreatePhoto(string id, Color color, PatternKind pattern)
        {
            var assetPath = ContentRoot + "/Textures/Photos/" + id + ".png";
            if (File.Exists(assetPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (existing != null)
                    return existing;
            }

            return SaveTexture(assetPath, Photo(id, color, pattern));
        }

        static void AddDroppedPhotos(System.Collections.Generic.Dictionary<string, Texture2D> map)
        {
            var dir = ContentRoot + "/Textures/Photos";
            if (!Directory.Exists(dir))
                return;

            var files = Directory.GetFiles(dir, "*.png");
            for (var i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileName(files[i]);
                var id = Path.GetFileNameWithoutExtension(fileName);
                if (map.ContainsKey(id))
                    continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + fileName);
                if (tex != null)
                    map[id] = tex;
            }
        }

        static void SyncMazePhotoResources(System.Collections.Generic.Dictionary<string, Texture2D> photos)
        {
            EnsureFolder("Assets/Resources", "MazePhotos");
            foreach (var pair in photos)
            {
                if (pair.Value == null)
                    continue;
                var src = AssetDatabase.GetAssetPath(pair.Value);
                if (string.IsNullOrEmpty(src))
                    continue;
                var dest = "Assets/Resources/MazePhotos/" + pair.Key + ".png";
                if (string.Equals(src.Replace('\\', '/'), dest, StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Copy(Path.GetFullPath(src), Path.GetFullPath(dest), overwrite: true);
                AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
                ConfigureTextureImporter(dest, normalMap: false);
            }
        }

        static System.Collections.Generic.Dictionary<string, TextureSet> CreateReliefTextures()
        {
            var defs = new[]
            {
                Relief("clock", new Color(0.78f, 0.72f, 0.5f), PatternKind.Circle),
                Relief("switchboard", new Color(0.82f, 0.82f, 0.78f), PatternKind.Grid),
                Relief("medallion", new Color(0.72f, 0.58f, 0.22f), PatternKind.Circle),
                Relief("plaque", new Color(0.48f, 0.36f, 0.26f), PatternKind.Bricks),
                Relief("watch", new Color(0.55f, 0.55f, 0.6f), PatternKind.Circle),
                Relief("plant_relief", new Color(0.22f, 0.48f, 0.2f), PatternKind.Forest)
            };

            var map = new System.Collections.Generic.Dictionary<string, TextureSet>(StringComparer.Ordinal);
            for (var i = 0; i < defs.Length; i++)
            {
                var albedo = SaveTexture(ContentRoot + "/Textures/Relief/" + defs[i].Id + "_Albedo.png", defs[i]);
                var normal = SaveNormalMap(ContentRoot + "/Textures/Relief/" + defs[i].Id + "_Normal.png", defs[i]);
                map[defs[i].Id] = new TextureSet { Albedo = albedo, Normal = normal };
            }

            return map;
        }

        static System.Collections.Generic.Dictionary<string, GameObject> CreateObjectPrefabs()
        {
            var map = new System.Collections.Generic.Dictionary<string, GameObject>(StringComparer.Ordinal);
            map["pot"] = SaveObjectPrefab("Pot", BuildPot());
            map["lamp"] = SaveObjectPrefab("Lamp", BuildLamp());
            map["vase"] = SaveObjectPrefab("Vase", BuildVase());
            map["statue"] = SaveObjectPrefab("Statue", BuildStatue());
            map["plant"] = SaveObjectPrefab("Plant", BuildPlant());
            map["shrine"] = SaveObjectPrefab("Shrine", BuildShrine());
            return map;
        }

        static GameObject SaveObjectPrefab(string name, GameObject root)
        {
            var path = ContentRoot + "/Prefabs/Objects/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            root.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildPot()
        {
            var root = new GameObject("Pot");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
            Paint(body, new Color(0.62f, 0.34f, 0.18f));
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
            rim.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            Paint(rim, new Color(0.48f, 0.26f, 0.14f));
            return root;
        }

        static GameObject BuildLamp()
        {
            var root = new GameObject("Lamp");
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGo.name = "Base";
            baseGo.transform.SetParent(root.transform, false);
            baseGo.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
            Paint(baseGo, new Color(0.35f, 0.35f, 0.38f));
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(root.transform, false);
            stem.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            stem.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Paint(stem, new Color(0.4f, 0.4f, 0.42f));
            var shade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shade.name = "Shade";
            shade.transform.SetParent(root.transform, false);
            shade.transform.localScale = Vector3.one * 0.35f;
            shade.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            Paint(shade, new Color(0.95f, 0.82f, 0.35f), emission: new Color(0.25f, 0.2f, 0.05f));
            return root;
        }

        static GameObject BuildVase()
        {
            var root = new GameObject("Vase");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            Paint(body, new Color(0.28f, 0.48f, 0.72f));
            return root;
        }

        static GameObject BuildStatue()
        {
            var root = new GameObject("Statue");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.28f, 0.42f, 0.22f);
            body.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            Paint(body, new Color(0.62f, 0.6f, 0.58f));
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * 0.22f;
            head.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            Paint(head, new Color(0.68f, 0.66f, 0.64f));
            return root;
        }

        static GameObject BuildPlant()
        {
            var root = new GameObject("Plant");
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "Pot";
            pot.transform.SetParent(root.transform, false);
            pot.transform.localScale = new Vector3(0.4f, 0.18f, 0.4f);
            pot.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            Paint(pot, new Color(0.5f, 0.28f, 0.16f));
            var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.SetParent(root.transform, false);
            leaves.transform.localScale = new Vector3(0.55f, 0.42f, 0.55f);
            leaves.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Paint(leaves, new Color(0.18f, 0.55f, 0.22f));
            return root;
        }

        static GameObject BuildShrine()
        {
            var root = new GameObject("Shrine");
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base";
            baseGo.transform.SetParent(root.transform, false);
            baseGo.transform.localScale = new Vector3(0.55f, 0.12f, 0.4f);
            baseGo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            Paint(baseGo, new Color(0.42f, 0.32f, 0.22f));
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(root.transform, false);
            roof.transform.localScale = new Vector3(0.62f, 0.08f, 0.48f);
            roof.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            Paint(roof, new Color(0.55f, 0.18f, 0.14f));
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
            Paint(pillar, new Color(0.72f, 0.68f, 0.58f));
            return root;
        }

        static void Paint(GameObject go, Color color, Color? emission = null)
        {
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var mat = CreateSolidMaterial(go.name + "Mat", color);
            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }

            renderer.sharedMaterial = mat;
        }

        static MemoryWallCatalog CreateOrUpdateCatalog(
            System.Collections.Generic.Dictionary<string, Texture2D> photos,
            System.Collections.Generic.Dictionary<string, TextureSet> reliefs,
            System.Collections.Generic.Dictionary<string, GameObject> objects)
        {
            var path = ResourcesRoot + "/DefaultMemoryCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MemoryWallCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MemoryWallCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            var entries = new System.Collections.Generic.List<MemoryWallEntryDef>
            {
                Entry("desert", "nature", MemoryDisplayKind.Photo, "polaroid", photos["desert"]),
                Entry("waterfall", "nature", MemoryDisplayKind.Photo, "frame", photos["waterfall"]),
                Entry("monalisa", "painting", MemoryDisplayKind.Photo, "frame", photos["monalisa"]),
                Entry("starrynight", "painting", MemoryDisplayKind.Photo, "glow", photos["starrynight"]),
                Entry("scream", "painting", MemoryDisplayKind.Photo, "frame", photos["scream"]),
                Entry("persistence", "painting", MemoryDisplayKind.Photo, "polaroid", photos["persistence"]),
                Entry("waterlilies", "painting", MemoryDisplayKind.Photo, "metal", photos["waterlilies"]),
                Entry("girlpearl", "painting", MemoryDisplayKind.Photo, "frame", photos["girlpearl"]),
                Entry("studio", "art", MemoryDisplayKind.Photo, "plain", photos["studio"]),
                Entry("illustration", "art", MemoryDisplayKind.Photo, "frame", photos["illustration"]),
                Entry("watercolor", "art", MemoryDisplayKind.Photo, "glow", photos["watercolor"]),
                Entry("sculpture", "art", MemoryDisplayKind.Photo, "polaroid", photos["sculpture"]),
                Entry("gallery", "art", MemoryDisplayKind.Photo, "metal", photos["gallery"]),
                Entry("museum", "art", MemoryDisplayKind.Photo, "frame", photos["museum"]),
                ReliefEntry("clock", "landmarks", reliefs["clock"]),
                ReliefEntry("switchboard", "landmarks", reliefs["switchboard"]),
                ReliefEntry("medallion", "landmarks", reliefs["medallion"]),
                ReliefEntry("plaque", "landmarks", reliefs["plaque"]),
                ReliefEntry("watch", "landmarks", reliefs["watch"]),
                ReliefEntry("plant_relief", "landmarks", reliefs["plant_relief"]),
                ObjectEntry("pot", "objects", objects["pot"]),
                ObjectEntry("lamp", "objects", objects["lamp"]),
                ObjectEntry("vase", "objects", objects["vase"]),
                ObjectEntry("statue", "objects", objects["statue"]),
                ObjectEntry("plant", "objects", objects["plant"]),
                ObjectEntry("shrine", "objects", objects["shrine"])
            };

            foreach (var pair in photos)
            {
                if (pair.Value == null)
                    continue;
                var already = false;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Id == pair.Key)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                    entries.Add(Entry(pair.Key, "art", MemoryDisplayKind.Photo, "plain", pair.Value));
            }

            catalog.Configure(
                new[]
                {
                    new MemoryGenreDef { Id = "nature", DisplayName = "Nature" },
                    new MemoryGenreDef { Id = "landmarks", DisplayName = "Landmarks" },
                    new MemoryGenreDef { Id = "objects", DisplayName = "Objects" },
                    new MemoryGenreDef { Id = "painting", DisplayName = "Paintings" },
                    new MemoryGenreDef { Id = "art", DisplayName = "Art" }
                },
                entries.ToArray());
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static MemoryLevelProfile CreateOrUpdateLevel()
        {
            var path = ResourcesRoot + "/DefaultMemoryLevel.asset";
            var level = AssetDatabase.LoadAssetAtPath<MemoryLevelProfile>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<MemoryLevelProfile>();
                AssetDatabase.CreateAsset(level, path);
            }

            level.Configure("level-1", new[] { "nature", "landmarks", "objects", "painting", "art" });
            EditorUtility.SetDirty(level);
            return level;
        }

        static MemoryWallDisplayKit CreateOrUpdateDisplayKit(
            GameObject plain,
            GameObject frame,
            GameObject glow,
            GameObject polaroid,
            GameObject metal,
            Material reliefMat)
        {
            var path = ResourcesRoot + "/DefaultMemoryDisplayKit.asset";
            var kit = AssetDatabase.LoadAssetAtPath<MemoryWallDisplayKit>(path);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<MemoryWallDisplayKit>();
                AssetDatabase.CreateAsset(kit, path);
            }

            kit.Configure(
                plain,
                reliefMat,
                new[]
                {
                    new MemoryWallDisplayKit.FrameVariant { Id = "plain", Prefab = plain },
                    new MemoryWallDisplayKit.FrameVariant { Id = "frame", Prefab = frame },
                    new MemoryWallDisplayKit.FrameVariant { Id = "glow", Prefab = glow },
                    new MemoryWallDisplayKit.FrameVariant { Id = "polaroid", Prefab = polaroid },
                    new MemoryWallDisplayKit.FrameVariant { Id = "metal", Prefab = metal }
                },
                reliefScale: 0.82f,
                objScale: 0.42f);
            EditorUtility.SetDirty(kit);
            return kit;
        }

        static MemoryWallEntryDef Entry(string id, string genre, MemoryDisplayKind kind, string frame, Texture2D image)
        {
            return new MemoryWallEntryDef
            {
                Id = id,
                GenreId = genre,
                Kind = kind,
                FrameVariantId = frame,
                SelectionWeight = 1,
                Image = image
            };
        }

        static MemoryWallEntryDef ReliefEntry(string id, string genre, TextureSet set)
        {
            return new MemoryWallEntryDef
            {
                Id = id,
                GenreId = genre,
                Kind = MemoryDisplayKind.Relief,
                FrameVariantId = "plain",
                SelectionWeight = 1,
                Image = set.Albedo,
                NormalMap = set.Normal
            };
        }

        static MemoryWallEntryDef ObjectEntry(string id, string genre, GameObject prefab)
        {
            return new MemoryWallEntryDef
            {
                Id = id,
                GenreId = genre,
                Kind = MemoryDisplayKind.Object3d,
                FrameVariantId = string.Empty,
                SelectionWeight = 1,
                ObjectPrefab = prefab
            };
        }

        enum PatternKind
        {
            Forest,
            Waves,
            Gradient,
            Peaks,
            Dots,
            Bricks,
            Lines,
            Stripe,
            Grid,
            Circle
        }

        struct TexDef
        {
            public string Id;
            public Color Base;
            public PatternKind Pattern;
        }

        static TexDef Photo(string id, Color color, PatternKind pattern)
        {
            return new TexDef { Id = id, Base = color, Pattern = pattern };
        }

        static TexDef Relief(string id, Color color, PatternKind pattern)
        {
            return new TexDef { Id = id, Base = color, Pattern = pattern };
        }

        static Texture2D SaveTexture(string assetPath, TexDef def)
        {
            var tex = BuildPatternTexture(def, 128);
            WritePng(assetPath, tex);
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(assetPath, normalMap: false);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static Texture2D SaveNormalMap(string assetPath, TexDef def)
        {
            var height = BuildHeightField(def, 128);
            var normal = HeightToNormal(height, 128);
            WritePng(assetPath, normal);
            UnityEngine.Object.DestroyImmediate(height);
            UnityEngine.Object.DestroyImmediate(normal);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(assetPath, normalMap: true);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static void ConfigureTextureImporter(string assetPath, bool normalMap)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }

        static void WritePng(string assetPath, Texture2D tex)
        {
            var bytes = tex.EncodeToPNG();
            var fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
            File.WriteAllBytes(fullPath, bytes);
        }

        static Texture2D BuildPatternTexture(TexDef def, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var t = PatternValue(def.Pattern, x, y, size);
                    var c = Color.Lerp(def.Base * 0.65f, def.Base * 1.15f, t);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D BuildHeightField(TexDef def, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var h = PatternValue(def.Pattern, x, y, size);
                    var b = (byte)(h * 255f);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D HeightToNormal(Texture2D height, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float Sample(int sx, int sy)
            {
                sx = Mathf.Clamp(sx, 0, size - 1);
                sy = Mathf.Clamp(sy, 0, size - 1);
                return height.GetPixel(sx, sy).grayscale;
            }

            const float strength = 3.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (Sample(x + 1, y) - Sample(x - 1, y)) * strength;
                    var dy = (Sample(x, y + 1) - Sample(x, y - 1)) * strength;
                    var normal = new Vector3(-dx, -dy, 1f).normalized;
                    pixels[y * size + x] = new Color32(
                        (byte)((normal.x * 0.5f + 0.5f) * 255f),
                        (byte)((normal.y * 0.5f + 0.5f) * 255f),
                        (byte)((normal.z * 0.5f + 0.5f) * 255f),
                        255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static float PatternValue(PatternKind kind, int x, int y, int size)
        {
            var u = x / (float)size;
            var v = y / (float)size;
            switch (kind)
            {
                case PatternKind.Forest:
                    return Mathf.PerlinNoise(u * 8f, v * 8f);
                case PatternKind.Waves:
                    return 0.5f + 0.5f * Mathf.Sin(v * Mathf.PI * 8f + Mathf.Sin(u * 6f) * 0.4f);
                case PatternKind.Gradient:
                    return Mathf.Clamp01(v);
                case PatternKind.Peaks:
                    return Mathf.Clamp01(1f - Mathf.Abs(v - 0.35f - Mathf.Sin(u * Mathf.PI * 3f) * 0.15f) * 5f);
                case PatternKind.Dots:
                    return ((Mathf.Floor(u * 8f) + Mathf.Floor(v * 8f)) % 2f == 0f) ? 0.85f : 0.35f;
                case PatternKind.Bricks:
                    var row = Mathf.Floor(v * 6f);
                    var offset = (row % 2f) * 0.5f;
                    var bx = Mathf.Floor((u + offset) * 4f);
                    var by = Mathf.Floor(v * 6f);
                    return ((bx + by) % 2f == 0f) ? 0.75f : 0.4f;
                case PatternKind.Lines:
                    return Mathf.Repeat(u * 10f, 1f) < 0.12f ? 0.85f : 0.45f;
                case PatternKind.Stripe:
                    return Mathf.Floor(v * 10f) % 2f == 0f ? 0.8f : 0.35f;
                case PatternKind.Grid:
                    return (Mathf.Repeat(u * 8f, 1f) < 0.08f || Mathf.Repeat(v * 8f, 1f) < 0.08f) ? 0.9f : 0.4f;
                case PatternKind.Circle:
                    var cx = u - 0.5f;
                    var cy = v - 0.5f;
                    var d = Mathf.Sqrt(cx * cx + cy * cy);
                    return d < 0.28f ? 0.9f : (d < 0.34f ? 0.55f : 0.3f);
                default:
                    return 0.5f;
            }
        }
    }
}
