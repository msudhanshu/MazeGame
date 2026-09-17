using Game.Unity.View;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Photograph on the box floor, particles floating in the box, glass tiles above.
    /// </summary>
    public static class MosaicArenaBackdrop
    {
        public const string PhotoName = "Mosaic Photo";
        public const string DustName = "Mosaic Dust";

        public static void Build(BoardLayout layout, Transform parent, ArenaVisualSettings settings)
        {
            var box = MosaicParticleBox.ForBoard(layout);
            BuildPhoto(layout, parent, settings, box);
            if (WantsVfx(settings))
                BuildVfx(parent, box, settings);
        }

        static bool WantsVfx(ArenaVisualSettings settings)
        {
            // Coloured tiles never spawn this. Mosaic: custom prefab, or the built-in field when ticked.
            return settings != null
                   && (settings.MosaicVfxPrefab != null || settings.MosaicBackgroundParticles);
        }

        static void BuildPhoto(
            BoardLayout layout,
            Transform parent,
            ArenaVisualSettings settings,
            MosaicParticleBox box)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PhotoName;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(box.Center.x, box.Min.y, box.Center.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(layout.SurfaceWidth, layout.SurfaceDepth, 1f);
            ArenaEnvironment.DestroyNow(go.GetComponent<Collider>());

            var tileRenderer = go.GetComponent<Renderer>();
            tileRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tileRenderer.receiveShadows = false;
            tileRenderer.sharedMaterial = PhotoMaterial(settings);
        }

        static Material PhotoMaterial(ArenaVisualSettings settings)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "MosaicPhoto" };
            var texture = settings != null ? settings.MosaicTexture : null;
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }

            if (settings != null && settings.FlipMosaicVertical)
            {
                material.mainTextureScale = new Vector2(1f, -1f);
                material.mainTextureOffset = new Vector2(0f, 1f);
            }

            return material;
        }

        static void BuildVfx(Transform parent, MosaicParticleBox box, ArenaVisualSettings settings)
        {
            var root = new GameObject(DustName);
            root.transform.SetParent(parent, false);
            root.transform.position = box.Center;

            var cage = root.AddComponent<MosaicParticleCage>();
            if (settings != null && settings.MosaicVfxPrefab != null)
                PlacePrefab(root.transform, box, settings.MosaicVfxPrefab);
            else
                AddDefaultLayers(root.transform, box);

            cage.Initialise(box.Extents);
        }

        static void PlacePrefab(Transform parent, MosaicParticleBox box, GameObject prefab)
        {
            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.AddComponent<MosaicBoxedVfx>().Bind(box);
        }

        static void AddDefaultLayers(Transform parent, MosaicParticleBox box)
        {
            AddLayer(parent, box, "Nebula", "MosaicVfx/circle_05",
                new Color(0.55f, 0.72f, 1f, 0.22f), size: 0.09f, speed: 0.018f, rate: 18f, max: 90, life: 6.5f);
            AddLayer(parent, box, "MilkyWay", "MosaicVfx/twirl_02",
                new Color(0.72f, 0.55f, 1f, 0.2f), size: 0.08f, speed: 0.022f, rate: 12f, max: 55, life: 7f);
            AddLayer(parent, box, "Stars", "MosaicVfx/star_08",
                new Color(1f, 0.96f, 0.78f, 0.85f), size: 0.055f, speed: 0.03f, rate: 28f, max: 140, life: 5f);
            AddLayer(parent, box, "Sparkles", "MosaicVfx/spark_05",
                new Color(1f, 0.88f, 0.55f, 0.9f), size: 0.045f, speed: 0.05f, rate: 24f, max: 110, life: 3.2f);
            AddLayer(parent, box, "Magic", "MosaicVfx/magic_05",
                new Color(0.62f, 0.95f, 1f, 0.55f), size: 0.07f, speed: 0.025f, rate: 10f, max: 48, life: 5.5f);
            AddLayer(parent, box, "Flares", "MosaicVfx/flare_01",
                new Color(1f, 0.78f, 1f, 0.35f), size: 0.075f, speed: 0.016f, rate: 8f, max: 40, life: 6f);
            AddLayer(parent, box, "Starfield", "MosaicVfx/star_01",
                new Color(0.85f, 0.92f, 1f, 0.7f), size: 0.04f, speed: 0.02f, rate: 20f, max: 100, life: 6.5f);
            AddLayer(parent, box, "Glow", "MosaicVfx/light_01",
                new Color(0.7f, 0.82f, 1f, 0.18f), size: 0.085f, speed: 0.012f, rate: 7f, max: 36, life: 7.5f);
        }

        static void AddLayer(
            Transform parent,
            MosaicParticleBox box,
            string name,
            string resource,
            Color tint,
            float size,
            float speed,
            float rate,
            int max,
            float life)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = tint;
            main.maxParticles = max;
            main.gravityModifier = 0f;
            main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.scale = box.Size;
            shape.randomDirectionAmount = 1f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-speed, speed);
            velocity.y = new ParticleSystem.MinMaxCurve(-speed * 0.4f, speed * 0.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(-speed, speed);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0.85f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-18f, 18f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = ParticleMaterial(resource, box);
        }

        static Material ParticleMaterial(string resource, MosaicParticleBox box)
        {
            var shader = Shader.Find("Nixin Studio/MosaicVfxClip")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "MosaicVfx" };
            material.color = Color.white;
            var texture = Resources.Load<Texture2D>(resource);
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_ClipCenter"))
                material.SetVector("_ClipCenter", box.Center);
            if (material.HasProperty("_ClipExtents"))
                material.SetVector("_ClipExtents", box.Extents);

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
            return material;
        }
    }
}
