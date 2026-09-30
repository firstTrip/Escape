using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChannelZeroLivingRoomSceneCreator
{
    private const string ResourceRoot = "Assets/Resources/ChannelZero/LivingRoom2001";
    private const string ScenePath = "Assets/Scenes/ChannelZero_LivingRoom2001.unity";
    private const string PreviewPath = "Assets/Resources/ChannelZero/Previews/living_room_2001_layout.png";
    private static readonly Vector2 Center = new(0.5f, 0.5f);

    [MenuItem("Tools/Channel Zero/Create Living Room 2001 Scene")]
    public static void CreateInProject()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTextureImporters();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        CreateEventSystem();

        GameObject roomRoot = CreateRectObject("LivingRoom2001", canvas.transform);
        Stretch(roomRoot.GetComponent<RectTransform>());

        GameObject backgroundLayer = CreateLayer("BackgroundLayer", roomRoot.transform);
        GameObject hotspotLayer = CreateLayer("HotspotLayer", roomRoot.transform);
        CanvasGroup hotspotDebugGroup = hotspotLayer.AddComponent<CanvasGroup>();
        ChannelZeroHotspotDebugOverlay debugOverlay = hotspotLayer.AddComponent<ChannelZeroHotspotDebugOverlay>();
        debugOverlay.Configure(hotspotDebugGroup, true);

        CreateImage(backgroundLayer.transform, "RoomShell", ResourceRoot + "/bg_livingroom_furnished_2001_reference_v1.png",
            Vector2.zero, new Vector2(1920f, 1080f));

        // Broad background targets first, foreground and small targets last.
        // GraphicRaycaster therefore resolves unavoidable overlaps toward the more specific target.
        CreateHotspot(hotspotLayer.transform, "Window", "Window", "1", new Vector2(-555f, 285f), new Vector2(560f, 470f));
        CreateHotspot(hotspotLayer.transform, "HallwayDoor", "HallwayDoor", "9", new Vector2(690f, 260f), new Vector2(315f, 380f));
        CreateHotspot(hotspotLayer.transform, "Sofa_Backrest", "Sofa", "5A", new Vector2(-590f, -25f), new Vector2(710f, 140f));
        CreateHotspot(hotspotLayer.transform, "Sofa_LeftSeat", "Sofa", "5B", new Vector2(-760f, -220f), new Vector2(360f, 240f));
        CreateHotspot(hotspotLayer.transform, "Armchair", "Armchair", "13", new Vector2(705f, -265f), new Vector2(500f, 545f));
        CreateHotspot(hotspotLayer.transform, "DisplayCabinet", "DisplayCabinet", "8", new Vector2(425f, 55f), new Vector2(165f, 380f));
        CreateHotspot(hotspotLayer.transform, "GrandfatherClock", "GrandfatherClock", "7", new Vector2(250f, 125f), new Vector2(145f, 520f));
        CreateHotspot(hotspotLayer.transform, "Television", "Television", "6", new Vector2(3f, -15f), new Vector2(370f, 235f));
        CreateHotspot(hotspotLayer.transform, "CoffeeTable", "CoffeeTable", "11", new Vector2(-265f, -260f), new Vector2(510f, 260f));
        CreateHotspot(hotspotLayer.transform, "FloorLamp", "FloorLamp", "4", new Vector2(-330f, 120f), new Vector2(150f, 285f));
        CreateHotspot(hotspotLayer.transform, "LandscapePainting", "LandscapePainting", "2", new Vector2(3f, 288f), new Vector2(290f, 130f));
        CreateHotspot(hotspotLayer.transform, "FamilyPortrait", "FamilyPortrait", "3", new Vector2(395f, 320f), new Vector2(110f, 130f));
        CreateHotspot(hotspotLayer.transform, "Toolbox", "Toolbox", "12", new Vector2(370f, -435f), new Vector2(310f, 180f));
        CreateHotspot(hotspotLayer.transform, "RotaryPhone", "RotaryPhone", "10", new Vector2(900f, 90f), new Vector2(115f, 90f));

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterSceneInBuildSettings();
        AssetDatabase.SaveAssets();
        ValidateScene(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("CHANNEL_ZERO_LIVING_ROOM_SCENE_CREATED");
    }

    private static void RegisterSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
        int existingIndex = scenes.FindIndex(entry => entry.path == ScenePath);
        if (existingIndex >= 0)
        {
            EditorBuildSettingsScene existing = scenes[existingIndex];
            if (!existing.enabled)
                scenes[existingIndex] = new EditorBuildSettingsScene(existing.path, true);
        }
        else
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("Tools/Channel Zero/Open Collider Preview Scene")]
    public static void OpenColliderPreviewScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new FileNotFoundException("Collider preview scene was not found.", ScenePath);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Channel Zero/Play Collider Preview Scene")]
    public static void PlayColliderPreviewScene()
    {
        OpenColliderPreviewScene();
        if (SceneManager.GetActiveScene().path == ScenePath)
            EditorApplication.isPlaying = true;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("LayoutCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.05f, 0.045f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = -10f;
        camera.farClipPlane = 10f;
        camera.transform.position = new Vector3(0f, 0f, -5f);
        cameraObject.tag = "MainCamera";
        return camera;
    }

    private static Canvas CreateCanvas(Camera camera)
    {
        GameObject canvasObject = new("LivingRoomCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static GameObject CreateLayer(string name, Transform parent)
    {
        GameObject layer = CreateRectObject(name, parent);
        Stretch(layer.GetComponent<RectTransform>());
        return layer;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void CreateImage(Transform parent, string name, string assetPath,
        Vector2 position, Vector2 size)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new InvalidOperationException($"Sprite not found: {assetPath}");
        CreateImageInternal(parent, name, sprite, position, size, 0f);
    }

    private static void CreateHotspot(Transform parent, string objectName, string hotspotId,
        string debugLabel, Vector2 position, Vector2 size)
    {
        GameObject hotspotObject = CreateRectObject($"Hotspot_{objectName}", parent);
        Image hitArea = hotspotObject.AddComponent<Image>();
        hitArea.sprite = null;
        hitArea.color = new Color(0f, 0.9f, 1f, 0.1f);
        hitArea.raycastTarget = true;

        Outline outline = hotspotObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.95f, 1f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        RectTransform rect = hotspotObject.GetComponent<RectTransform>();
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        ChannelZeroHotspot hotspot = hotspotObject.AddComponent<ChannelZeroHotspot>();
        hotspot.Configure(hotspotId);
        CreateDebugBadge(hotspotObject.transform, debugLabel);
    }

    private static void CreateDebugBadge(Transform parent, string label)
    {
        GameObject badgeObject = CreateRectObject("DebugBadge", parent);
        Image badge = badgeObject.AddComponent<Image>();
        badge.color = new Color(0f, 0.9f, 1f, 1f);
        badge.raycastTarget = false;

        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = Vector2.zero;
        badgeRect.sizeDelta = label.Length > 1 ? new Vector2(58f, 42f) : new Vector2(42f, 42f);

        GameObject labelObject = CreateRectObject("Label", badgeObject.transform);
        Text text = labelObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(labelObject.GetComponent<RectTransform>());
    }

    private static void CreateImageInternal(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size, float rotation)
    {
        GameObject imageObject = CreateRectObject(name, parent);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureTextureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ResourceRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static void RenderPreview(Camera camera)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath) ?? ResourceRoot);
        RenderTexture renderTexture = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            camera.targetTexture = renderTexture;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = renderTexture;
            Texture2D texture = new(1920, 1080, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(PreviewPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static void ValidateScene(Scene scene)
    {
        List<string> missing = new();
        int backgroundCount = 0;
        int hitboxCount = 0;
        HashSet<string> hotspotIds = new(StringComparer.Ordinal);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                ChannelZeroHotspot hotspot = image.GetComponent<ChannelZeroHotspot>();
                if (hotspot != null)
                {
                    if (!image.raycastTarget || image.sprite != null || image.GetComponent<Outline>() == null)
                        throw new InvalidOperationException($"Hotspot is not a valid debug raycast target: {image.gameObject.name}");
                    hitboxCount++;
                    hotspotIds.Add(hotspot.HotspotId);
                    continue;
                }

                if (image.gameObject.name == "DebugBadge")
                    continue;

                if (image.sprite == null)
                {
                    missing.Add(image.gameObject.name);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(image.sprite);
                if (path == ResourceRoot + "/bg_livingroom_furnished_2001_reference_v1.png")
                    backgroundCount++;
            }
        }

        if (missing.Count > 0)
            throw new InvalidOperationException($"Images without sprites: {string.Join(", ", missing)}");
        if (backgroundCount != 1)
            throw new InvalidOperationException($"Expected one furnished background, found {backgroundCount}");
        if (hitboxCount != 14 || hotspotIds.Count != 13)
            throw new InvalidOperationException($"Expected 14 hitboxes across 13 targets, found hitboxes={hitboxCount} targets={hotspotIds.Count}");
        Debug.Log($"CHANNEL_ZERO_LIVING_ROOM_VALIDATED background={backgroundCount} hitboxes={hitboxCount} targets={hotspotIds.Count}");
    }
}
