using GameKamiStreaming;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        [MenuItem("GameKamiStreaming/Ensure Editable Skill Tree Canvas")]
        public static void EnsureEditableSkillTreeCanvasInScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindFirstObjectByType<KkamiPrototypeGame>();
            if (game == null)
            {
                var bootstrap = new GameObject("KkamiPrototypeGame");
                game = bootstrap.AddComponent<KkamiPrototypeGame>();
            }

            game.EnsureEditableSkillTreeCanvas();
            EnsureEditableSpawnPoints(game);
            RemoveAutoAddedCameraData();
            EnsurePersistentNextStageButton(game);
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Ensured editable skill tree canvas in scene: " + ScenePath);
        }

        static void RemoveAutoAddedCameraData()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var components = camera.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null || component.GetType().Name != "UniversalAdditionalCameraData")
                {
                    continue;
                }

                Object.DestroyImmediate(component);
            }
        }

        static void EnsurePersistentNextStageButton(KkamiPrototypeGame game)
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button.name != "Start Next Stage Button")
                {
                    continue;
                }

                for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) == game && button.onClick.GetPersistentMethodName(i) == nameof(KkamiPrototypeGame.StartNextStageFromSkillTree))
                    {
                        return;
                    }
                }

                UnityEventTools.AddPersistentListener(button.onClick, game.StartNextStageFromSkillTree);
                EditorUtility.SetDirty(button);
                return;
            }
        }

        static void EnsureEditableSpawnPoints(KkamiPrototypeGame game)
        {
            var legacySpawnPoint = GameObject.Find("spawnpoint");
            if (legacySpawnPoint != null && GameObject.Find("spawnpoint4") == null)
            {
                legacySpawnPoint.name = "spawnpoint4";
                EditorUtility.SetDirty(legacySpawnPoint);
            }

            var serializedGame = new SerializedObject(game);
            SetSpawnPointReference(serializedGame, "spawnPoint1", "spawnpoint1");
            SetSpawnPointReference(serializedGame, "spawnPoint2", "spawnpoint2");
            SetSpawnPointReference(serializedGame, "spawnPoint3", "spawnpoint3");
            SetSpawnPointReference(serializedGame, "spawnPoint4", "spawnpoint4");
            serializedGame.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetSpawnPointReference(SerializedObject serializedGame, string propertyName, string objectName)
        {
            var property = serializedGame.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            var target = GameObject.Find(objectName);
            property.objectReferenceValue = target != null ? target.transform as RectTransform : null;
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

