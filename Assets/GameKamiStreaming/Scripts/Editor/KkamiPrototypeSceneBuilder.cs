using GameKamiStreaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameKamiStreamingEditor
{
    public static class KkamiPrototypeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/KkamiPrototype.unity";

        [MenuItem("GameKamiStreaming/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            ConfigureSpriteImports();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("KkamiPrototypeGame");
            var game = bootstrap.AddComponent<KkamiPrototypeGame>();
            game.BuildEditableSceneLayout();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built editable Kkami prototype scene: " + ScenePath);
        }


        [MenuItem("GameKamiStreaming/Apply Editable UI Fixes")]
        public static void ApplyEditableUiFixes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var resourceCounters = GameObject.Find("Resource Counters");
            if (resourceCounters != null)
            {
                var grid = resourceCounters.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (grid != null)
                {
                    Object.DestroyImmediate(grid);
                }

                for (var i = 0; i < resourceCounters.transform.childCount; i++)
                {
                    var child = resourceCounters.transform.GetChild(i) as RectTransform;
                    if (child == null || !child.name.StartsWith("Resource "))
                    {
                        continue;
                    }

                    var column = i % 3;
                    var row = i / 3;
                    child.anchorMin = new Vector2(0.5f, 1f);
                    child.anchorMax = new Vector2(0.5f, 1f);
                    child.pivot = new Vector2(0.5f, 0.5f);
                    child.sizeDelta = new Vector2(330f, 86f);
                    child.anchoredPosition = new Vector2(-350f + column * 350f, -55f - row * 98f);
                }
            }

            var game = Object.FindFirstObjectByType<KkamiPrototypeGame>();
            if (game != null)
            {
                EditorUtility.SetDirty(game);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Applied editable UI fixes without moving E panel: " + ScenePath);
        }
        static void ConfigureSpriteImports()
        {
            var assets = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites" });
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            for (var i = 0; i < scenes.Length; i++)
            {
                updated[i] = scenes[i];
            }
            updated[updated.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}

