using System;
using System.Collections.Generic;
using System.IO;
using ChannelZero.Runtime.Core;
using ChannelZero.Runtime.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RuntimeHotspot = ChannelZero.Runtime.Presentation.ChannelZeroHotspot;

public static class ChannelZeroVerticalSliceSceneCreator
{
    private const string ScenePath = "Assets/Scenes/ChannelZero_VerticalSlice.unity";
    private const string DataDirectory = "Assets/ChannelZero/Data";
    private const string CatalogPath = DataDirectory + "/ChannelZeroVerticalSliceCatalog.asset";
    private const string CloseupCatalogPath = DataDirectory + "/ChannelZeroCloseupCatalog.asset";
    private const string EntryBackgroundPath = "Assets/Resources/ChannelZero/PlayRooms/Entry/2001/bg_entryhall_2001_arrival_v01.png";
    private const string LivingRoot = "Assets/Resources/ChannelZero/PlayRooms/LivingRoom";
    private const string WorkshopRoot = "Assets/Resources/ChannelZero/PlayRooms/Workshop";
    private const string CloseupRoot = "Assets/Resources/ChannelZero/Closeups";
    private const string HudOverlayPath = "Assets/Resources/ChannelZero/UI/HUD/ui_ingame_crt_hud_overlay_v04.png";
    private const string HudApplyRequestPath = "Temp/ChannelZeroApplyCrtHud.request";
    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

    private readonly struct HotspotSpec
    {
        public readonly string Id;
        public readonly string RoomId;
        public readonly Rect NormalizedTopLeftRect;
        public readonly ChannelEra[] Eras;

        public HotspotSpec(string id, string roomId, Rect rect, params ChannelEra[] eras)
        {
            Id = id;
            RoomId = roomId;
            NormalizedTopLeftRect = rect;
            Eras = eras;
        }
    }

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedHudApply()
    {
        if (File.Exists(HudApplyRequestPath))
            EditorApplication.delayCall += TryApplyRequestedHud;
    }

    private static void TryApplyRequestedHud()
    {
        if (!File.Exists(HudApplyRequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (!SceneManager.GetSceneAt(i).isDirty)
                continue;

            Debug.LogWarning("CHANNEL_ZERO_CRT_HUD_APPLY_DEFERRED: save the currently edited scene, then use Tools/Channel Zero/Create Vertical Slice Scene.");
            return;
        }

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CreateScene();
            File.Delete(HudApplyRequestPath);
            Debug.Log("CHANNEL_ZERO_CRT_HUD_APPLIED");
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    [MenuItem("Tools/Channel Zero/Create Vertical Slice Scene")]
    public static void CreateInProject()
    {
        CreateScene();
        Debug.Log("CHANNEL_ZERO_VERTICAL_SLICE_CREATED");
    }

    public static void CreateAndValidateBatch()
    {
        CreateScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);
        Debug.Log("CHANNEL_ZERO_VERTICAL_SLICE_BATCH_VALIDATED");
    }

    [MenuItem("Tools/Channel Zero/Validate Vertical Slice Scene")]
    public static void ValidateCurrentAsset()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);
    }

    [MenuItem("Tools/Channel Zero/Capture CRT HUD Preview")]
    public static void CaptureHudPreview()
    {
        CaptureHudPreviewTo("Assets/Resources/ChannelZero/Previews/channelzero_crt_hud_runtime_preview_v01.png");
    }

    public static void CaptureHudPreviewBatch()
    {
        CaptureHudPreview();
        Debug.Log("CHANNEL_ZERO_CRT_HUD_PREVIEW_CAPTURED");
    }

    [MenuItem("Tools/Channel Zero/Play Vertical Slice Scene")]
    public static void PlayScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void CreateScene()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureHudOverlayImporter();
        ConfigureCloseupImporters();
        ChannelZeroRoomCatalog catalog = CreateOrUpdateCatalog();
        ChannelZeroCloseupCatalog closeupCatalog = CreateOrUpdateCloseupCatalog();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        CreateEventSystem();

        GameObject runtimeRoot = new("ChannelZeroRuntime");
        ChannelZeroVerticalSliceController controller = runtimeRoot.AddComponent<ChannelZeroVerticalSliceController>();

        RectTransform roomRoot = CreateRect("RoomView", canvas.transform);
        Stretch(roomRoot);

        Image background = CreateImage("RoomBackground", roomRoot, Color.white);
        Stretch(background.rectTransform);
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EntryBackgroundPath);
        background.raycastTarget = false;
        background.preserveAspect = false;

        RectTransform hotspotLayer = CreateRect("HotspotLayer", roomRoot);
        Stretch(hotspotLayer);
        CreateHotspots(hotspotLayer);

        ChannelZeroRoomPresenter presenter = roomRoot.gameObject.AddComponent<ChannelZeroRoomPresenter>();
        presenter.EditorConfigure(background, hotspotLayer, catalog);

        RectTransform chrome = CreateRect("CrtHud", canvas.transform);
        Stretch(chrome);
        chrome.SetAsLastSibling();
        CreateCrtHud(chrome, controller, out Text status, out Button backButton,
            out Text channelReadout, out Text operationReadout);

        ChannelZeroCloseupCanvasController closeupCanvas = CreateCloseupCanvas(canvas.transform, closeupCatalog);
        closeupCanvas.transform.SetAsLastSibling();

        controller.EditorConfigure(presenter, closeupCanvas, status, backButton,
            channelReadout, operationReadout, restore: false);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterSceneInBuildSettings();
        AssetDatabase.SaveAssets();
        ValidateScene(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void CaptureHudPreviewTo(string outputPath)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new InvalidOperationException("Preview camera is missing.");

        Text channel = FindSceneComponent<Text>(scene, "ChannelReadout");
        Text operation = FindSceneComponent<Text>(scene, "OperationReadout");
        Text status = FindSceneComponent<Text>(scene, "HudStatus");
        if (channel != null)
            channel.text = "CH 2001";
        if (operation != null)
            operation.text = "READY";
        if (status != null)
            status.text = "2001년 현관 — 오른쪽 다이얼과 인벤토리를 확인하세요.";

        const int width = 1920;
        const int height = 1080;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            renderTexture.Create();
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            capture.Apply(false, false);

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(capture);
        }
    }

    private static T FindSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == objectName)
                    return component;
            }
        }

        return null;
    }

    private static void ConfigureHudOverlayImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(HudOverlayPath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("CRT HUD overlay is missing.", HudOverlayPath);

        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency;
        if (!changed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ConfigureCloseupImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CloseupRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.maxTextureSize != 2048
                || importer.textureCompression != TextureImporterCompression.CompressedHQ;
            if (!changed)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }

    private static void CreateCrtHud(RectTransform parent, ChannelZeroVerticalSliceController controller,
        out Text status, out Button backButton, out Text channelReadout, out Text operationReadout)
    {
        Sprite overlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(HudOverlayPath);
        if (overlaySprite == null)
            throw new FileNotFoundException("CRT HUD overlay is not imported as a Sprite.", HudOverlayPath);

        Image overlay = CreateImage("CrtBezelOverlay", parent, Color.white);
        Stretch(overlay.rectTransform);
        overlay.sprite = overlaySprite;
        overlay.preserveAspect = false;
        overlay.raycastTarget = false;

        status = CreateText("HudStatus", parent, 20, TextAnchor.MiddleCenter);
        PlaceBottomLeft(status.rectTransform, new Vector2(820f, 190f), new Vector2(1180f, 42f));
        status.color = new Color(0.86f, 0.80f, 0.66f, 1f);
        status.raycastTarget = false;

        CreateTransparentHudButton("RecButton", parent, new Vector2(195f, 100f),
            new Vector2(180f, 84f), controller.SelectRec);
        CreateTransparentHudButton("PlayButton", parent, new Vector2(420f, 100f),
            new Vector2(180f, 84f), controller.SelectPlay);
        CreateTransparentHudButton("LoadButton", parent, new Vector2(625f, 100f),
            new Vector2(180f, 84f), controller.SelectLoad);
        CreateTransparentHudButton("RewButton", parent, new Vector2(825f, 100f),
            new Vector2(180f, 84f), controller.SelectRew);
        CreateTransparentHudButton("HoldButton", parent, new Vector2(1030f, 100f),
            new Vector2(180f, 84f), controller.SelectHold);
        backButton = CreateTransparentHudButton("BackButton", parent, new Vector2(1235f, 100f),
            new Vector2(180f, 84f), controller.GoBack);

        Image eraDial = CreateImage("ChannelDial", parent, Color.clear);
        PlaceBottomLeft(eraDial.rectTransform, new Vector2(1515f, 96f), new Vector2(310f, 170f));
        eraDial.raycastTarget = true;
        ChannelZeroEraDialDrag dialDrag = eraDial.gameObject.AddComponent<ChannelZeroEraDialDrag>();
        dialDrag.EditorConfigure(controller, 72f);

        channelReadout = null;
        operationReadout = null;

        ChannelZeroHudController hud = parent.gameObject.AddComponent<ChannelZeroHudController>();
        UnityEngine.Events.UnityAction[] actions =
        {
            hud.SelectSlot0,
            hud.SelectSlot1,
            hud.SelectSlot2,
            hud.SelectSlot3,
            hud.SelectSlot4,
            hud.SelectSlot5,
            hud.SelectSlot6,
        };
        float[] slotCentersY = { 977f, 858f, 740f, 622f, 503f, 385f, 266f };
        Image[] selectionFrames = new Image[7];
        for (int i = 0; i < selectionFrames.Length; i++)
        {
            Button slot = CreateTransparentHudButton($"InventorySlot_{i}", parent,
                new Vector2(1810f, slotCentersY[i]), new Vector2(112f, 112f), actions[i]);
            Image frame = CreateImage("Selection", slot.transform, new Color(1f, 0.48f, 0.08f, 0.10f));
            Stretch(frame.rectTransform);
            frame.raycastTarget = false;
            Outline outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.55f, 0.12f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
            selectionFrames[i] = frame;
        }
        hud.EditorConfigure(selectionFrames, 0);
    }

    private static Button CreateTransparentHudButton(string name, Transform parent, Vector2 center,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        Image image = CreateImage(name, parent, new Color(0f, 0f, 0f, 0f));
        PlaceBottomLeft(image.rectTransform, center, size);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1f, 0.55f, 0.12f, 0.10f);
        colors.pressedColor = new Color(1f, 0.42f, 0.05f, 0.22f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    private static Text CreateHudReadout(string name, Transform parent, Vector2 center, Vector2 size,
        int fontSize)
    {
        Text text = CreateText(name, parent, fontSize, TextAnchor.MiddleCenter);
        PlaceBottomLeft(text.rectTransform, center, size);
        text.color = new Color(0.12f, 0.085f, 0.05f, 1f);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        return text;
    }

    private static void PlaceBottomLeft(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
    }

    private static ChannelZeroRoomCatalog CreateOrUpdateCatalog()
    {
        Directory.CreateDirectory(DataDirectory);
        ChannelZeroRoomCatalog catalog = AssetDatabase.LoadAssetAtPath<ChannelZeroRoomCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ChannelZeroRoomCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        List<ChannelZeroRoomCatalog.BackgroundEntry> entries = new()
        {
            Entry(ChannelZeroIds.EntryRoom, ChannelEra.Year2001, EntryBackgroundPath),
            Entry(ChannelZeroIds.LivingRoom, ChannelEra.Year1961, LivingRoot + "/1961/bg_livingroom_1961_play_v01.png"),
            Entry(ChannelZeroIds.LivingRoom, ChannelEra.Year1981, LivingRoot + "/1981/bg_livingroom_1981_play_v01.png"),
            Entry(ChannelZeroIds.LivingRoom, ChannelEra.Year2001, LivingRoot + "/2001/bg_livingroom_2001_play_v01.png"),
            Entry(ChannelZeroIds.LivingRoom, ChannelEra.Year2021, LivingRoot + "/2021/bg_livingroom_2021_play_v01.png"),
            Entry(ChannelZeroIds.WorkshopRoom, ChannelEra.Year1961, WorkshopRoot + "/1961/bg_workshop_1961_play_v01.png"),
            Entry(ChannelZeroIds.WorkshopRoom, ChannelEra.Year1981, WorkshopRoot + "/1981/bg_workshop_1981_play_v01.png"),
            Entry(ChannelZeroIds.WorkshopRoom, ChannelEra.Year2001, WorkshopRoot + "/2001/bg_workshop_2001_play_v01.png"),
            Entry(ChannelZeroIds.WorkshopRoom, ChannelEra.Year2021, WorkshopRoot + "/2021/bg_workshop_2021_play_v01.png"),
        };
        catalog.EditorSetBackgrounds(entries);
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static ChannelZeroCloseupCatalog CreateOrUpdateCloseupCatalog()
    {
        Directory.CreateDirectory(DataDirectory);
        ChannelZeroCloseupCatalog catalog =
            AssetDatabase.LoadAssetAtPath<ChannelZeroCloseupCatalog>(CloseupCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ChannelZeroCloseupCatalog>();
            AssetDatabase.CreateAsset(catalog, CloseupCatalogPath);
        }

        List<ChannelZeroCloseupCatalog.ArtworkEntry> entries = new()
        {
            Closeup(ChannelZeroIds.PrologueServiceRequestCloseup, "default", CloseupRoot + "/Prologue/PRO-Z01/pro_z01_service_request_default_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtFrontCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z01/liv_z01_crt_front_off_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtRearCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z02/liv_z02_crt_rear_closed_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtRearCloseup, "open", CloseupRoot + "/LivingRoom/LIV-Z02/liv_z02_crt_rear_open_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtRearCloseup, "tube_removed", CloseupRoot + "/LivingRoom/LIV-Z02/liv_z02_crt_rear_tube_removed_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtRearCloseup, "replaced", CloseupRoot + "/LivingRoom/LIV-Z02/liv_z02_crt_rear_replaced_v01.png"),
            Closeup(ChannelZeroIds.LivingCrtRearCloseup, "bracket_removed", CloseupRoot + "/LivingRoom/LIV-Z02/liv_z02_crt_rear_bracket_removed_v01.png"),
            Closeup(ChannelZeroIds.LivingTubeStorageCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z03/liv_z03_tube_case_closed_v01.png"),
            Closeup(ChannelZeroIds.LivingTubeStorageCloseup, "open", CloseupRoot + "/LivingRoom/LIV-Z03/liv_z03_tube_case_open_v01.png"),
            Closeup(ChannelZeroIds.LivingTubeStorageCloseup, "acquired", CloseupRoot + "/LivingRoom/LIV-Z03/liv_z03_tube_case_acquired_v01.png"),
            Closeup(ChannelZeroIds.LivingNumberRugCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z04/liv_z04_number_rug_default_v01.png"),
            Closeup(ChannelZeroIds.LivingMedicalCabinetCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z05/liv_z05_medical_cabinet_default_v01.png"),
            Closeup(ChannelZeroIds.LivingHandTreatmentCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z06/liv_z06_injured_hand_default_v01.png"),
            Closeup(ChannelZeroIds.LivingLockboxCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z07/liv_z07_lockbox_default_v01.png"),
            Closeup(ChannelZeroIds.LivingRecPanelCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z08/liv_z08_rec_panel_default_v01.png"),
            Closeup(ChannelZeroIds.LivingRecSlotsCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z09/liv_z09_tape_slots_default_v01.png"),
            Closeup(ChannelZeroIds.LivingFamilyPhotosCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z10/liv_z10_family_photos_default_v01.png"),
            Closeup(ChannelZeroIds.LivingWiringDiagramCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z11/liv_z11_wiring_diagram_default_v01.png"),
            Closeup(ChannelZeroIds.LivingClockCloseup, "default", CloseupRoot + "/LivingRoom/LIV-Z12/liv_z12_grandfather_clock_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopTubeTesterCloseup, "default", CloseupRoot + "/Workshop/WKS-Z01/wks_z01_tube_tester_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopPartsDrawerCloseup, "default", CloseupRoot + "/Workshop/WKS-Z02/wks_z02_parts_drawer_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopFloorPlanCloseup, "default", CloseupRoot + "/Workshop/WKS-Z03/wks_z03_floor_lock_plan_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopWiringDiagramCloseup, "default", CloseupRoot + "/Workshop/WKS-Z04/wks_z04_power_wiring_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopRepairLogCloseup, "default", CloseupRoot + "/Workshop/WKS-Z05/wks_z05_repair_journal_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopKeyCutterCloseup, "default", CloseupRoot + "/Workshop/WKS-Z06/wks_z06_key_cutter_default_v01.png"),
            Closeup(ChannelZeroIds.WorkshopFoldingCrankInspect, "default", CloseupRoot + "/Workshop/WKS-I01/wks_i01_folding_crank_default_v01.png"),
        };
        catalog.EditorSetArtworks(entries);
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static ChannelZeroCloseupCanvasController CreateCloseupCanvas(
        Transform canvasParent, ChannelZeroCloseupCatalog catalog)
    {
        RectTransform root = CreateRect("CloseupCanvas", canvasParent);
        Stretch(root);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image dimmed = CreateImage("DimmedBackdrop", root, new Color(0f, 0f, 0f, 0.78f));
        Stretch(dimmed.rectTransform);

        Image panel = CreateImage("CloseupPanel", root, new Color(0.08f, 0.07f, 0.06f, 1f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1500f, 850f);
        panelRect.anchoredPosition = Vector2.zero;

        Image artwork = CreateImage("Artwork", panelRect, new Color(0.16f, 0.14f, 0.12f, 1f));
        Stretch(artwork.rectTransform);
        artwork.raycastTarget = false;

        RectTransform interactionLayer = CreateRect("InteractionLayer", panelRect);
        Stretch(interactionLayer);
        RectTransform feedbackLayer = CreateRect("FeedbackLayer", panelRect);
        Stretch(feedbackLayer);
        RectTransform exactTextLayer = CreateRect("ExactTextLayer", panelRect);
        Stretch(exactTextLayer);

        TextMeshProUGUI exactText = CreateTmpText("ExactText", exactTextLayer, 38, TextAlignmentOptions.Center);
        RectTransform exactTextRect = exactText.rectTransform;
        exactTextRect.anchorMin = new Vector2(0.18f, 0.18f);
        exactTextRect.anchorMax = new Vector2(0.82f, 0.82f);
        exactTextRect.offsetMin = Vector2.zero;
        exactTextRect.offsetMax = Vector2.zero;
        exactText.color = new Color(0.91f, 0.86f, 0.74f, 1f);
        exactText.raycastTarget = false;

        RectTransform inventoryStrip = CreateRect("InventoryStrip", root);
        inventoryStrip.anchorMin = new Vector2(0.15f, 0f);
        inventoryStrip.anchorMax = new Vector2(0.85f, 0f);
        inventoryStrip.pivot = new Vector2(0.5f, 0f);
        inventoryStrip.anchoredPosition = new Vector2(0f, 10f);
        inventoryStrip.sizeDelta = new Vector2(0f, 90f);

        Button close = CreateCloseButton(root);
        ChannelZeroCloseupCanvasController controller =
            root.gameObject.AddComponent<ChannelZeroCloseupCanvasController>();
        controller.EditorConfigure(group, artwork, interactionLayer, feedbackLayer, exactTextLayer,
            inventoryStrip, close, exactText, catalog);
        return controller;
    }

    private static Button CreateCloseButton(RectTransform parent)
    {
        Image image = CreateImage("CloseButton", parent, new Color(0.18f, 0.15f, 0.12f, 0.96f));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(130f, 54f);
        Button button = image.gameObject.AddComponent<Button>();
        TextMeshProUGUI label = CreateTmpText("Label", rect, 26, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = "닫기";
        label.raycastTarget = false;
        return button;
    }

    private static ChannelZeroRoomCatalog.BackgroundEntry Entry(string roomId, ChannelEra era, string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new FileNotFoundException("Required room background is missing or not imported as Sprite.", path);
        return new ChannelZeroRoomCatalog.BackgroundEntry
        {
            roomId = roomId,
            era = era,
            stateId = ChannelZeroIds.DefaultVisualState,
            sprite = sprite,
        };
    }

    private static ChannelZeroCloseupCatalog.ArtworkEntry Closeup(string closeupId, string stateId, string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new FileNotFoundException("Required closeup is missing or not imported as Sprite.", path);
        return new ChannelZeroCloseupCatalog.ArtworkEntry
        {
            closeupId = closeupId,
            stateId = stateId,
            artwork = sprite,
        };
    }

    private static void CreateHotspots(RectTransform parent)
    {
        HotspotSpec[] specs =
        {
            new("Entry_ExteriorDoor", ChannelZeroIds.EntryRoom, new Rect(0.00f, 0.08f, 0.22f, 0.70f), ChannelEra.Year2001),
            new("Entry_WallLamp", ChannelZeroIds.EntryRoom, new Rect(0.27f, 0.16f, 0.09f, 0.18f), ChannelEra.Year2001),
            new("Entry_CoatRack", ChannelZeroIds.EntryRoom, new Rect(0.28f, 0.23f, 0.19f, 0.29f), ChannelEra.Year2001),
            new("Entry_WallPhone", ChannelZeroIds.EntryRoom, new Rect(0.45f, 0.25f, 0.08f, 0.17f), ChannelEra.Year2001),
            new(ChannelZeroIds.EntryMail, ChannelZeroIds.EntryRoom, new Rect(0.43f, 0.36f, 0.12f, 0.14f), ChannelEra.Year2001),
            new("Entry_ShoeCabinet", ChannelZeroIds.EntryRoom, new Rect(0.25f, 0.50f, 0.24f, 0.24f), ChannelEra.Year2001),
            new("Entry_UmbrellaStand", ChannelZeroIds.EntryRoom, new Rect(0.21f, 0.47f, 0.08f, 0.30f), ChannelEra.Year2001),
            new(ChannelZeroIds.EntryLivingDoor, ChannelZeroIds.EntryRoom, new Rect(0.51f, 0.12f, 0.33f, 0.66f), ChannelEra.Year2001),

            new("Living_CRT", ChannelZeroIds.LivingRoom, new Rect(0.40f, 0.38f, 0.20f, 0.27f)),
            new("Living_Photos", ChannelZeroIds.LivingRoom, new Rect(0.41f, 0.25f, 0.20f, 0.14f)),
            new("Living_NumberRug", ChannelZeroIds.LivingRoom, new Rect(0.10f, 0.63f, 0.62f, 0.32f)),
            new("Living_JinwooHand", ChannelZeroIds.LivingRoom, new Rect(0.72f, 0.40f, 0.25f, 0.49f)),
            new("Living_Clock", ChannelZeroIds.LivingRoom, new Rect(0.59f, 0.15f, 0.08f, 0.45f)),
            new("Living_DisplayMedical", ChannelZeroIds.LivingRoom, new Rect(0.66f, 0.24f, 0.10f, 0.36f)),
            new("Living_Lockbox", ChannelZeroIds.LivingRoom, new Rect(0.22f, 0.58f, 0.33f, 0.30f)),
            new("Living_TubeCase", ChannelZeroIds.LivingRoom, new Rect(0.60f, 0.77f, 0.18f, 0.21f)),
            new("Living_REC", ChannelZeroIds.LivingRoom, new Rect(0.43f, 0.58f, 0.14f, 0.08f), ChannelEra.Year2001),
            new("Living_Mina", ChannelZeroIds.LivingRoom, new Rect(0.45f, 0.40f, 0.10f, 0.16f),
                ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2021),
            new(ChannelZeroIds.LivingWorkshopDoor, ChannelZeroIds.LivingRoom, new Rect(0.77f, 0.08f, 0.17f, 0.49f)),

            new("Workshop_Workbench", ChannelZeroIds.WorkshopRoom, new Rect(0.18f, 0.32f, 0.43f, 0.35f)),
            new("Workshop_PartsDrawer", ChannelZeroIds.WorkshopRoom, new Rect(0.13f, 0.10f, 0.10f, 0.36f)),
            new("Workshop_ToolBoard", ChannelZeroIds.WorkshopRoom, new Rect(0.28f, 0.06f, 0.29f, 0.34f)),
            new("Workshop_TubeTester", ChannelZeroIds.WorkshopRoom, new Rect(0.50f, 0.23f, 0.10f, 0.25f)),
            new("Workshop_Soldering", ChannelZeroIds.WorkshopRoom, new Rect(0.20f, 0.35f, 0.17f, 0.18f)),
            new("Workshop_KeyCutter", ChannelZeroIds.WorkshopRoom, new Rect(0.59f, 0.31f, 0.13f, 0.25f)),
            new("Workshop_RepairLog", ChannelZeroIds.WorkshopRoom, new Rect(0.70f, 0.10f, 0.13f, 0.36f)),
            new(ChannelZeroIds.WorkshopFloorPlan, ChannelZeroIds.WorkshopRoom, new Rect(0.30f, 0.39f, 0.20f, 0.16f)),
            new(ChannelZeroIds.WorkshopFoldingCrank, ChannelZeroIds.WorkshopRoom, new Rect(0.44f, 0.42f, 0.09f, 0.10f)),
            new("Workshop_LivingDoor", ChannelZeroIds.WorkshopRoom, new Rect(0.88f, 0.05f, 0.11f, 0.70f)),
        };

        foreach (HotspotSpec spec in specs)
        {
            Image image = CreateImage("Hotspot_" + spec.Id, parent, new Color(0f, 0f, 0f, 0f));
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            Rect normalized = spec.NormalizedTopLeftRect;
            rect.anchorMin = new Vector2(normalized.x, 1f - normalized.y - normalized.height);
            rect.anchorMax = new Vector2(normalized.x + normalized.width, 1f - normalized.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RuntimeHotspot hotspot = image.gameObject.AddComponent<RuntimeHotspot>();
            hotspot.EditorConfigure(spec.Id, spec.RoomId, spec.Eras);
        }
    }

    private static void CreateEraButtons(RectTransform parent, ChannelZeroVerticalSliceController controller)
    {
        (string label, UnityEngine.Events.UnityAction action)[] buttons =
        {
            ("1961", controller.Tune1961),
            ("1981", controller.Tune1981),
            ("2001", controller.Tune2001),
            ("2021", controller.Tune2021),
        };
        for (int i = 0; i < buttons.Length; i++)
            CreateButton(parent, "Era_" + buttons[i].label, buttons[i].label, 20f + i * 112f, -18f, 100f, 44f, buttons[i].action);
    }

    private static void CreateOperationButtons(RectTransform parent, ChannelZeroVerticalSliceController controller)
    {
        (string label, UnityEngine.Events.UnityAction action)[] buttons =
        {
            ("REC", controller.SelectRec),
            ("PLAY", controller.SelectPlay),
            ("LOAD", controller.SelectLoad),
            ("REW", controller.SelectRew),
            ("HOLD", controller.SelectHold),
        };
        for (int i = 0; i < buttons.Length; i++)
            CreateButton(parent, "Operation_" + buttons[i].label, buttons[i].label, 1360f + i * 108f, -18f, 96f, 44f, buttons[i].action);
    }

    private static Button CreateBackButton(RectTransform parent, ChannelZeroVerticalSliceController controller)
    {
        Image image = CreateImage("BackButton", parent, new Color(0.08f, 0.1f, 0.11f, 0.92f));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(24f, 20f);
        rect.sizeDelta = new Vector2(150f, 58f);

        Button button = image.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, controller.GoBack);
        TextMeshProUGUI label = CreateTmpText("Label", rect, 26, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = "← 뒤로";
        label.raycastTarget = false;
        return button;
    }

    private static Text CreateStatusLabel(RectTransform parent)
    {
        Image panel = CreateImage("StatusPanel", parent, new Color(0f, 0f, 0f, 0.76f));
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = new Vector2(0.15f, 0f);
        rect.anchorMax = new Vector2(0.85f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = new Vector2(0f, 64f);
        panel.raycastTarget = false;

        Text text = CreateText("Status", panel.transform, 24, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, float x, float y,
        float width, float height, UnityEngine.Events.UnityAction action)
    {
        Image image = CreateImage(name, parent, new Color(0.08f, 0.1f, 0.11f, 0.9f));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        Button button = image.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        Text text = CreateText("Label", image.transform, 20, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.text = label;
        text.raycastTarget = false;
        return button;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";
        return camera;
    }

    private static Canvas CreateCanvas(Camera camera)
    {
        GameObject canvasObject = new("ChannelZeroCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static TextMeshProUGUI CreateTmpText(string name, Transform parent, int fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void RegisterSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
        int index = scenes.FindIndex(item => item.path == ScenePath);
        if (index < 0)
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        else
            scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ValidateScene(Scene scene)
    {
        int backgrounds = 0;
        int hotspots = 0;
        int colliders = 0;
        int closeupCanvases = 0;
        int hudOverlays = 0;
        int inventorySlots = 0;
        int hudDials = 0;
        HashSet<string> logicalIds = new(StringComparer.Ordinal);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "RoomBackground")
                {
                    backgrounds++;
                    if (image.raycastTarget)
                        throw new InvalidOperationException("RoomBackground must not receive raycasts.");
                }

                if (image.gameObject.name == "CrtBezelOverlay")
                {
                    hudOverlays++;
                    if (image.raycastTarget || image.sprite == null)
                        throw new InvalidOperationException("CRT bezel must use a non-raycasting Sprite image.");
                }

                if (image.gameObject.name.StartsWith("InventorySlot_", StringComparison.Ordinal))
                    inventorySlots++;
                if (image.gameObject.name == "ChannelDial")
                    hudDials++;

                RuntimeHotspot hotspot = image.GetComponent<RuntimeHotspot>();
                if (hotspot == null)
                    continue;
                hotspots++;
                logicalIds.Add(hotspot.LogicalId);
                if (!image.raycastTarget || image.sprite != null)
                    throw new InvalidOperationException($"Invalid transparent UI hotspot: {image.name}");
            }

            colliders += root.GetComponentsInChildren<Collider>(true).Length;
            colliders += root.GetComponentsInChildren<Collider2D>(true).Length;
            closeupCanvases += root.GetComponentsInChildren<ChannelZeroCloseupCanvasController>(true).Length;
        }

        if (backgrounds != 1)
            throw new InvalidOperationException($"Expected one RoomBackground, found {backgrounds}.");
        if (hotspots != 29 || logicalIds.Count != 29)
            throw new InvalidOperationException($"Expected 29 unique hotspots, found {hotspots}/{logicalIds.Count}.");
        if (colliders != 0)
            throw new InvalidOperationException($"Physics colliders are forbidden on this UI slice. Found {colliders}.");
        if (closeupCanvases != 1)
            throw new InvalidOperationException($"Expected one CloseupCanvas controller, found {closeupCanvases}.");
        if (hudOverlays != 1 || inventorySlots != 7 || hudDials != 1)
            throw new InvalidOperationException(
                $"Invalid CRT HUD structure overlays={hudOverlays} slots={inventorySlots} dials={hudDials}.");
        if (scene.GetRootGameObjects()[0].scene.GetRootGameObjects().Length == 0)
            throw new InvalidOperationException("Scene has no root objects.");

        Debug.Log($"CHANNEL_ZERO_VERTICAL_SLICE_VALIDATED backgrounds={backgrounds} hotspots={hotspots} colliders={colliders} closeups={closeupCanvases} hud={hudOverlays} slots={inventorySlots} dials={hudDials}");
    }
}
