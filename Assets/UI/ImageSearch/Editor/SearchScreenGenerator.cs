using System;
using System.Reflection;
using ImageSearch.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ImageSearch.UI.Editor
{
    public static class SearchScreenGenerator
    {
        private const string Root = "Assets/UI/ImageSearch";
        private const string ScreenUxmlPath = Root + "/Runtime/SearchScreen.uxml";
        private const string CardUxmlPath = Root + "/Runtime/ResultCard.uxml";
        private const string PanelPath = Root + "/Settings/ImageSearchPanelSettings.asset";
        private const string ScenePath = Root + "/Scenes/ImageSearchPreview.unity";

        [MenuItem("Tools/Image Search/Regenerate Preview Screen")]
        public static void GeneratePreviewScreen()
        {
            if (EditorSceneManager.GetActiveScene().isDirty &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AssetDatabase.Refresh();
            var screen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScreenUxmlPath);
            var card = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardUxmlPath);
            if (screen == null || card == null)
            {
                EditorUtility.DisplayDialog("검색 화면 생성 실패", "UXML 파일을 불러오지 못했습니다. Console을 확인해 주세요.", "확인");
                return;
            }

            var panelSettings = GetOrCreatePanelSettings();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.961f, 0.961f, 0.949f, 1f);
            camera.orthographic = true;

            var screenObject = new GameObject("Image Search Screen");
            var document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = screen;
            screenObject.AddComponent<SearchScreenPreviewController>();

            var previewController = screenObject.GetComponent<SearchScreenPreviewController>();
            var cardTemplateField = typeof(SearchScreenPreviewController).GetField(
                "_resultCardTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            cardTemplateField.SetValue(previewController, card);
            EditorUtility.SetDirty(previewController);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            SetGameViewTo1080x1920();
            Selection.activeGameObject = screenObject;
            Debug.Log("Image search preview generated at " + ScenePath);
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelPath);
            }

            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void SetGameViewTo1080x1920()
        {
            var editorAssembly = typeof(EditorWindow).Assembly;
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            var singletonType = editorAssembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null, null);
            var groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(instance, new[] { Enum.Parse(groupType, "Standalone") });
            var groupClass = group.GetType();
            var count = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupClass.GetMethod("GetGameViewSize");
            var targetIndex = -1;

            for (var index = 0; index < count; index++)
            {
                var size = getSize.Invoke(group, new object[] { index });
                var type = size.GetType();
                var width = (int)type.GetProperty("width").GetValue(size, null);
                var height = (int)type.GetProperty("height").GetValue(size, null);
                if (width == 1080 && height == 1920)
                {
                    targetIndex = index;
                    break;
                }
            }

            if (targetIndex < 0)
            {
                var sizeType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                var sizeClass = editorAssembly.GetType("UnityEditor.GameViewSize");
                var size = Activator.CreateInstance(sizeClass, BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance, null,
                    new[] { Enum.Parse(sizeType, "FixedResolution"), (object)1080, 1920, "1080x1920 Portrait - Image Search" },
                    null);
                groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                targetIndex = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance).SetValue(gameView, targetIndex, null);
            gameView.titleContent = new GUIContent("Game");
            gameView.Show();
            gameView.Focus();
        }
    }
}
