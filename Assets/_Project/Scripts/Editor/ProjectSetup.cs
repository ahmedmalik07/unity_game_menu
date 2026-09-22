using System.IO;
using System.Linq;
using Aetherfall.Core;
using Aetherfall.UI;
using Aetherfall.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Aetherfall.Editor
{
    /// <summary>
    /// Generates the project's scenes and settings from code, so the whole setup is reproducible
    /// (and runnable from the command line with -executeMethod).
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string UiFolder = Root + "/UI";
        const string SceneFolder = Root + "/Scenes";
        const string MaterialFolder = Root + "/Materials";
        public const string MainMenuScenePath = SceneFolder + "/" + Scenes.MainMenu + ".unity";
        const string GameScenePath = SceneFolder + "/" + Scenes.Game + ".unity";
        const string BuildPath = "Builds/Windows/Aetherfall.exe";

        [MenuItem("Tools/Aetherfall/Rebuild Scenes")]
        public static void Generate()
        {
            CreatePanelSettings();
            AssetDatabase.SaveAssets();
            CreateMainMenuScene();
            CreateGameScene();
            ConfigurePlayer();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MainMenuScenePath);
            Debug.Log("[Aetherfall] Scenes generated.");
        }

        [MenuItem("Tools/Aetherfall/Build Windows Player")]
        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[Aetherfall] Build {report.summary.result}: {report.summary.outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Command-line entry point: generate everything, then build the Windows player.</summary>
        public static void GenerateAndBuild()
        {
            Generate();
            BuildWindows();
        }

        const string PanelSettingsPath = UiFolder + "/PanelSettings.asset";

        static PanelSettings LoadPanelSettings() =>
            AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath) ??
            throw new FileNotFoundException("PanelSettings asset missing", PanelSettingsPath);

        static void CreatePanelSettings()
        {
            string path = PanelSettingsPath;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (!panel)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiFolder + "/Theme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
        }

        static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene (Single) unloads unreferenced assets, so load the panel settings only after it.
            var panel = LoadPanelSettings();

            var camera = CreateCamera(new Color(0.02f, 0.02f, 0.06f));
            camera.transform.position = new Vector3(0f, 0f, -10f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.skybox = null;

            var ui = new GameObject("Main Menu UI");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiFolder + "/MainMenu.uxml");
            ui.AddComponent<MenuController>();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene (Single) unloads unreferenced assets, so load the panel settings only after it.
            var panel = LoadPanelSettings();
            var sky = new Color(0.025f, 0.03f, 0.09f);
            Random.InitState(7); // Same layout every time the scene is regenerated.

            var camera = CreateCamera(sky);
            camera.transform.position = new Vector3(0f, 2.2f, -8.5f);
            camera.transform.LookAt(new Vector3(0f, 1.3f, 0f));
            camera.fieldOfView = 50f;
            camera.gameObject.AddComponent<FloatMotion>().spin = Vector3.zero;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.1f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = sky;
            RenderSettings.fogDensity = 0.045f;

            var sun = new GameObject("Moonlight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.55f, 0.65f, 1f);
            sun.intensity = 0.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var ground = Material("Ground", new Color(0.02f, 0.025f, 0.05f), Color.black, 0.75f);
            var stone = Material("Stone", new Color(0.12f, 0.13f, 0.22f), Color.black, 0.3f);
            var crystal = Material("Crystal", new Color(0.3f, 0.9f, 1f), new Color(0.3f, 1.4f, 1.8f), 0.9f);
            var ember = Material("Ember", new Color(1f, 0.5f, 0.7f), new Color(1.6f, 0.5f, 0.9f), 0.8f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.localScale = new Vector3(8f, 1f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;

            var world = new GameObject("Crystal Shrine").transform;

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Plinth";
            plinth.transform.SetParent(world);
            plinth.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            plinth.transform.localScale = new Vector3(3.2f, 0.15f, 3.2f);
            plinth.GetComponent<Renderer>().sharedMaterial = stone;

            var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Aether Core";
            core.transform.SetParent(world);
            core.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            core.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            core.transform.localScale = Vector3.one * 0.9f;
            core.GetComponent<Renderer>().sharedMaterial = crystal;
            var coreMotion = core.AddComponent<FloatMotion>();
            coreMotion.spin = new Vector3(0f, 40f, 0f);
            coreMotion.bobHeight = 0.18f;

            var glow = new GameObject("Core Light").AddComponent<Light>();
            glow.transform.SetParent(core.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 0.95f, 1f);
            glow.range = 9f;
            glow.intensity = 3f;

            // Ring of orbiting shards.
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Shard {i + 1}";
                shard.transform.SetParent(world);
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.4f, 1.4f + Mathf.Sin(angle * 3f) * 0.35f, Mathf.Sin(angle) * 2.4f);
                shard.transform.localRotation = Random.rotation;
                shard.transform.localScale = Vector3.one * Random.Range(0.14f, 0.26f);
                shard.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? ember : crystal;
                Object.DestroyImmediate(shard.GetComponent<Collider>());
                var motion = shard.AddComponent<FloatMotion>();
                motion.spin = Random.onUnitSphere * 90f;
                motion.orbitCenter = world;
                motion.orbitSpeed = 18f;
            }

            // Monoliths framing the shot.
            // Kept to the far half of the circle so none of them block the camera.
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.Lerp(-0.15f, 1.15f, i / 6f) * Mathf.PI;
                float height = Random.Range(2f, 4.5f);
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Monolith {i + 1}";
                pillar.transform.position = new Vector3(Mathf.Cos(angle) * 7f, height * 0.5f, Mathf.Sin(angle) * 7f + 3f);
                pillar.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 90f), Random.Range(-6f, 6f));
                pillar.transform.localScale = new Vector3(Random.Range(0.6f, 1.1f), height, Random.Range(0.6f, 1.1f));
                pillar.GetComponent<Renderer>().sharedMaterial = stone;
            }

            var hud = new GameObject("Game UI");
            var document = hud.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiFolder + "/Game.uxml");
            hud.AddComponent<PauseController>();

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return camera;
        }

        static Material Material(string name, Color albedo, Color emission, float smoothness)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(Root, "Materials");
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = albedo;
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Aetherfall";
            PlayerSettings.companyName = "ahmedmalik07";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SplashScreen.show = false;
        }
    }

    /// <summary>Opens the main menu scene the first time the project is opened.</summary>
    [InitializeOnLoad]
    static class OpenMainMenuOnFirstLoad
    {
        static OpenMainMenuOnFirstLoad()
        {
            if (Application.isBatchMode || SessionState.GetBool("Aetherfall.Opened", false)) return;
            SessionState.SetBool("Aetherfall.Opened", true);
            EditorApplication.delayCall += () =>
            {
                var active = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path) && File.Exists(ProjectSetup.MainMenuScenePath))
                    EditorSceneManager.OpenScene(ProjectSetup.MainMenuScenePath);
            };
        }
    }

    /// <summary>
    /// Turns on MCP for Unity's "auto-start server on load" once, so Claude Code can connect to the
    /// editor (http://127.0.0.1:8080/mcp) as soon as Unity is open.
    /// </summary>
    [InitializeOnLoad]
    static class McpAutoStart
    {
        const string AppliedKey = "Aetherfall.McpAutoStartApplied";

        static McpAutoStart()
        {
            if (EditorPrefs.GetBool(AppliedKey, false)) return;
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool(AppliedKey, true);
            Debug.Log("[Aetherfall] Enabled MCP for Unity auto-start. Claude Code connects via http://127.0.0.1:8080/mcp");
        }
    }
}
