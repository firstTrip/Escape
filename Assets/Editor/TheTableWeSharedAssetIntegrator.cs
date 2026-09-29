using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TheTableWeSharedAssetIntegrator
{
    private const string ResourceRoot = "Assets/Resources/TheTableWeShared";
    private const string ScenePath = "Assets/Scenes/00_Prologue.unity";

    private static readonly Dictionary<string, string> SceneImageMap = new()
    {
        { "BG_Hub", "Backgrounds/bg_apartment_flat_final.png" },
        { "Vanity", "Furniture/10_bedroom_vanity.png" },
        { "RecordConsole", "Furniture/08_living_record_console.png" },
        { "Turntable", "PuzzleProps/09_old_turntable.png" },
        { "RecordShelf", "PuzzleProps/08_worn_record_jacket.png" },
        { "Vinyl", "PuzzleProps/10_vinyl_record.png" },
        { "Sofa", "Furniture/07_two_seat_sofa.png" },
        { "SofaCushion", "PuzzleProps/21_photo_fragment_center.png" },
        { "ChairLeft", "Furniture/02_dining_chair_used.png" },
        { "ChairRight", "Furniture/03_dining_chair_clean.png" },
        { "DiningTable", "Furniture/01_dining_table.png" },
        { "Coffee", "PuzzleProps/01_cold_coffee_pair.png" },
        { "LetterRackLock", "PuzzleProps/03_letter_lockbox_313.png" },
        { "Watch", "PuzzleProps/04_cracked_wristwatch_0317.png" },
        { "Phone", "PuzzleProps/02_answering_machine.png" },
        { "Sink", "Furniture/04_kitchen_sink_base.png" },
        { "Fridge", "Furniture/06_refrigerator.png" },
        { "MatchboxOnFridge", "PuzzleProps/11_matchbox_magnet.png" },
        { "PhotoFragment_Fridge", "PuzzleProps/20_photo_fragment_left.png" },
        { "Radio", "PuzzleProps/14_analog_radio.png" },
    };

    private static readonly Dictionary<string, string> ItemIconMap = new()
    {
        { "Item_Key.asset", "PuzzleProps/05_spare_door_key.png" },
        { "Item_Lighter.asset", "PuzzleProps/06_old_lighter.png" },
        { "Item_PhotoCombined.asset", "PuzzleProps/07_moving_day_photo_complete.png" },
        { "Item_PhotoFragment1.asset", "PuzzleProps/20_photo_fragment_left.png" },
        { "Item_PhotoFragment2.asset", "PuzzleProps/21_photo_fragment_center.png" },
        { "Item_PhotoFragment3.asset", "PuzzleProps/22_photo_fragment_right.png" },
        { "Item_RazorBlade.asset", "PuzzleProps/12_razor_blade.png" },
        { "Item_Record.asset", "PuzzleProps/10_vinyl_record.png" },
    };

    private static readonly Dictionary<string, float> NativeWidthMap = new()
    {
        { "Backgrounds/bg_apartment_flat_final.png", 1920f },
        { "Furniture/01_dining_table.png", 520f },
        { "Furniture/02_dining_chair_used.png", 165f },
        { "Furniture/03_dining_chair_clean.png", 155f },
        { "Furniture/04_kitchen_sink_base.png", 380f },
        { "Furniture/05_kitchen_upper_cabinet.png", 430f },
        { "Furniture/06_refrigerator.png", 260f },
        { "Furniture/07_two_seat_sofa.png", 450f },
        { "Furniture/08_living_record_console.png", 510f },
        { "Furniture/09_double_bed.png", 500f },
        { "Furniture/10_bedroom_vanity.png", 245f },
        { "Furniture/11_wardrobe.png", 245f },
        { "Furniture/12_empty_bookcase.png", 210f },
        { "Furniture/13_entry_shoe_cabinet.png", 220f },
        { "Furniture/14_balcony_utility_shelf.png", 205f },
        { "PuzzleProps/01_cold_coffee_pair.png", 125f },
        { "PuzzleProps/02_answering_machine.png", 105f },
        { "PuzzleProps/03_letter_lockbox_313.png", 105f },
        { "PuzzleProps/04_cracked_wristwatch_0317.png", 58f },
        { "PuzzleProps/08_worn_record_jacket.png", 78f },
        { "PuzzleProps/09_old_turntable.png", 145f },
        { "PuzzleProps/10_vinyl_record.png", 72f },
        { "PuzzleProps/11_matchbox_magnet.png", 58f },
        { "PuzzleProps/14_analog_radio.png", 92f },
        { "PuzzleProps/15_balcony_potted_plant.png", 110f },
        { "PuzzleProps/16_march_calendar_17.png", 105f },
        { "PuzzleProps/20_photo_fragment_left.png", 44f },
        { "PuzzleProps/21_photo_fragment_center.png", 68f },
    };

    [MenuItem("Tools/The Table We Shared/Apply Art To Prologue And Act 0")]
    public static void Apply()
    {
        ConfigureTextureImporters();
        ApplySceneArt();
        ApplyItemIcons();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("THE_TABLE_WE_SHARED_INTEGRATION_PASS");
    }

    private static void ConfigureTextureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ResourceRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite,
                value => importer.textureType = value);
            changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single,
                value => importer.spriteImportMode = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false,
                value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.alphaIsTransparency, true,
                value => importer.alphaIsTransparency = value);
            changed |= SetIfDifferent(importer.npotScale, TextureImporterNPOTScale.None,
                value => importer.npotScale = value);

            string relativePath = path.Substring(ResourceRoot.Length + 1).Replace('\\', '/');
            if (NativeWidthMap.TryGetValue(relativePath, out float targetWidth))
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    float pixelsPerUnit = texture.width / targetWidth * 100f;
                    changed |= SetIfDifferent(importer.spritePixelsPerUnit, pixelsPerUnit,
                        value => importer.spritePixelsPerUnit = value);
                }
            }

            if (changed)
                importer.SaveAndReimport();
        }
    }

    private static bool SetIfDifferent<T>(T current, T desired, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, desired))
            return false;

        setter(desired);
        return true;
    }

    private static void ApplySceneArt()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (KeyValuePair<string, string> pair in SceneImageMap)
        {
            GameObject target = FindInScene(scene, pair.Key);
            if (target == null)
                throw new InvalidOperationException($"Scene object not found: {pair.Key}");

            Image image = target.GetComponent<Image>();
            if (image == null)
                throw new InvalidOperationException($"Image component not found: {pair.Key}");

            image.sprite = LoadSprite(pair.Value);
            image.preserveAspect = pair.Key != "BG_Hub";
            EditorUtility.SetDirty(image);
        }

        // Layout is intentionally left to the scene designer. This command only
        // assigns the approved art and inventory/puzzle references.

        ApplySerializedSprite<ExamineHotspot>(scene, "Watch", "examineSprite",
            "PuzzleProps/04_cracked_wristwatch_0317.png");
        ApplySerializedSprite<ChapterTransitionStub>(scene, "MatchboxOnFridge", "matchboxSprite",
            "PuzzleProps/11_matchbox_magnet.png");
        ApplySerializedSprite<PhotoCombinePuzzleController>(scene, "PhotoAssemblyDrawer", "combinedSprite",
            "PuzzleProps/07_moving_day_photo_complete.png");
        ApplySerializedSprite<PhotoRestorePuzzle>(scene, "Vanity", "backSprite",
            "PuzzleProps/13_layered_photo_back.png");
        ApplySerializedSprite<PhotoRestorePuzzle>(scene, "Vanity", "aloneSprite",
            "PuzzleProps/28_timeline_photo_C_alone.png");
        ApplySerializedSprite<PhotoRestorePuzzle>(scene, "Vanity", "absenceSprite",
            "PuzzleProps/29_timeline_photo_D_absence.png");
        ApplySerializedSprite<RecordShelfHotspot>(scene, "RecordShelf", "jacketClosedSprite",
            "PuzzleProps/08_worn_record_jacket.png");
        ApplySerializedSprite<RecordShelfHotspot>(scene, "RecordShelf", "jacketOpenSprite",
            "PuzzleProps/10_vinyl_record.png");
        ApplySerializedSprite<RadioTuningPuzzle>(scene, "Radio", "transitionSprite",
            "PuzzleProps/17_voice_recorder_complete.png");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureRoomLayout(Scene scene)
    {
        SetNativeRect(scene, "RecordConsole", new Vector2(-360f, -245f));
        SetNativeRect(scene, "Turntable", new Vector2(130f, 105f));
        SetNativeRect(scene, "RecordShelf", new Vector2(-165f, 110f));
        SetNativeRect(scene, "Vinyl", new Vector2(-88f, 105f));
        SetNativeRect(scene, "Sofa", new Vector2(300f, -270f));
        SetNativeRect(scene, "SofaCushion", new Vector2(90f, -10f));

        SetNativeRect(scene, "ChairLeft", new Vector2(-230f, -235f));
        SetNativeRect(scene, "ChairRight", new Vector2(255f, -220f));
        SetNativeRect(scene, "DiningTable", new Vector2(15f, -255f));
        SetNativeRect(scene, "Coffee", new Vector2(-145f, 130f));
        SetNativeRect(scene, "LetterRackLock", new Vector2(-15f, 135f));
        SetNativeRect(scene, "Watch", new Vector2(85f, 128f));
        SetNativeRect(scene, "Phone", new Vector2(175f, 132f));
        SetRectPosition(scene, "EntranceDoor", new Vector2(210f, -28f));

        SetNativeRect(scene, "Sink", new Vector2(-260f, -255f));
        SetNativeRect(scene, "Fridge", new Vector2(390f, -105f));
        SetNativeRect(scene, "MatchboxOnFridge", new Vector2(-90f, 0f));
        SetNativeRect(scene, "PhotoFragment_Fridge", new Vector2(-94f, 220f));
        SetNativeRect(scene, "Radio", new Vector2(25f, 225f));

        SetNativeRect(scene, "Vanity", new Vector2(430f, -285f));

        GameObject background = FindInScene(scene, "BG_Hub");
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.SetNativeSize();
        backgroundImage.preserveAspect = true;
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Center;
        backgroundRect.anchorMax = Center;
        backgroundRect.pivot = Center;
        backgroundRect.anchoredPosition = Vector2.zero;
        AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            UnityEngine.Object.DestroyImmediate(fitter);
        EditorUtility.SetDirty(backgroundImage);
    }

    private static void ConfigureRoomNavigation(Scene scene)
    {
        GameObject hotspotLayer = FindInScene(scene, "HotspotLayer");
        GameObject living = GetOrCreateUiObject("Room_Living", hotspotLayer.transform);
        GameObject dining = GetOrCreateUiObject("Room_DiningEntry", hotspotLayer.transform);
        GameObject kitchen = GetOrCreateUiObject("Room_Kitchen", hotspotLayer.transform);
        GameObject bedroom = GetOrCreateUiObject("Room_Bedroom", hotspotLayer.transform);
        GameObject[] rooms = { living, dining, kitchen, bedroom };

        foreach (GameObject room in rooms)
            StretchInside(room.GetComponent<RectTransform>(), 0f);

        MoveToRoom(scene, living.transform, "RecordConsole", "Sofa");
        MoveToRoom(scene, dining.transform, "EntranceDoor", "ChairLeft", "ChairRight", "DiningTable");
        MoveToRoom(scene, kitchen.transform, "Sink", "Fridge");
        MoveToRoom(scene, bedroom.transform, "Vanity");

        CreateOrUpdateDecoration(living.transform, "LivingBookcase",
            "Furniture/12_empty_bookcase.png", new Vector2(-720f, -175f));
        CreateOrUpdateDecoration(dining.transform, "EntryShoeCabinet",
            "Furniture/13_entry_shoe_cabinet.png", new Vector2(650f, -250f));
        CreateOrUpdateDecoration(kitchen.transform, "KitchenUpperCabinet",
            "Furniture/05_kitchen_upper_cabinet.png", new Vector2(-265f, 175f));
        CreateOrUpdateDecoration(kitchen.transform, "BalconyUtilityShelf",
            "Furniture/14_balcony_utility_shelf.png", new Vector2(700f, -210f));
        CreateOrUpdateDecoration(kitchen.transform, "BalconyPlant",
            "PuzzleProps/15_balcony_potted_plant.png", new Vector2(695f, 40f));
        CreateOrUpdateDecoration(bedroom.transform, "DoubleBed",
            "Furniture/09_double_bed.png", new Vector2(-250f, -275f));
        CreateOrUpdateDecoration(bedroom.transform, "Wardrobe",
            "Furniture/11_wardrobe.png", new Vector2(-700f, -165f));

        Button previousButton = CreateOrUpdateNavigationButton(hotspotLayer.transform, "RoomPreviousButton",
            new Vector2(-875f, 0f), true);
        Button nextButton = CreateOrUpdateNavigationButton(hotspotLayer.transform, "RoomNextButton",
            new Vector2(875f, 0f), false);

        GameObject controllerObject = GetOrCreateUiObject("RoomNavigation", hotspotLayer.transform);
        RoomNavigationController controller = controllerObject.GetComponent<RoomNavigationController>();
        if (controller == null)
            controller = controllerObject.AddComponent<RoomNavigationController>();
        SerializedObject serializedController = new(controller);
        SerializedProperty roomArray = serializedController.FindProperty("rooms");
        roomArray.arraySize = rooms.Length;
        for (int i = 0; i < rooms.Length; i++)
            roomArray.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        serializedController.FindProperty("previousButton").objectReferenceValue = previousButton;
        serializedController.FindProperty("nextButton").objectReferenceValue = nextButton;
        serializedController.FindProperty("startingRoom").intValue = 1;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        for (int i = 0; i < rooms.Length; i++)
            rooms[i].SetActive(i == 1);
        previousButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
        controllerObject.transform.SetAsLastSibling();
        previousButton.transform.SetAsLastSibling();
        nextButton.transform.SetAsLastSibling();
    }

    private static GameObject GetOrCreateUiObject(string name, Transform parent)
    {
        Transform existing = FindRecursive(parent, name);
        if (existing != null)
            return existing.gameObject;

        GameObject result = new(name, typeof(RectTransform));
        result.layer = parent.gameObject.layer;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void MoveToRoom(Scene scene, Transform room, params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            GameObject target = FindInScene(scene, objectName);
            target.transform.SetParent(room, false);
            EditorUtility.SetDirty(target);
        }
    }

    private static void CreateOrUpdateDecoration(Transform parent, string name, string spritePath,
        Vector2 position)
    {
        GameObject decoration = GetOrCreateUiObject(name, parent);
        Image image = decoration.GetComponent<Image>();
        if (image == null)
            image = decoration.AddComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.SetNativeSize();
        RectTransform rect = decoration.GetComponent<RectTransform>();
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(rect);
    }

    private static Button CreateOrUpdateNavigationButton(Transform parent, string name,
        Vector2 position, bool flip)
    {
        GameObject buttonObject = GetOrCreateUiObject(name, parent);
        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
            image = buttonObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/arrow.png");
        image.preserveAspect = true;
        image.raycastTarget = true;
        image.color = new Color(1f, 1f, 1f, 0.82f);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        SetRect(buttonObject.GetComponent<RectTransform>(), Center, Center, position,
            new Vector2(70f, 110f), Center);
        buttonObject.transform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(button);
        return button;
    }

    private static void ConfigureUiLayout(Scene scene)
    {
        GameObject canvasObject = FindInScene(scene, "GameCanvas");
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        EditorUtility.SetDirty(scaler);

        ConfigureInventory(scene);
        ConfigureSubtitle(scene);
        ConfigureExaminePanel(scene);
        ConfigurePuzzlePanel(scene, "LockPuzzlePanel", "Dial");
        ConfigurePuzzlePanel(scene, "RadioPanel", "RDial");
    }

    private static void ConfigureInventory(Scene scene)
    {
        GameObject bar = FindInScene(scene, "InventoryBar");
        RectTransform barRect = bar.GetComponent<RectTransform>();
        SetRect(barRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 24f), new Vector2(620f, 82f), new Vector2(0f, 0f));

        for (int i = 0; i < 7; i++)
        {
            GameObject slot = FindRecursive(bar.transform, $"Slot{i}").gameObject;
            SetRect(slot.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(43f + i * 87f, 0f), new Vector2(76f, 76f), new Vector2(0.5f, 0.5f));

            GameObject icon = FindRecursive(slot.transform, $"Slot{i}_Icon").gameObject;
            StretchInside(icon.GetComponent<RectTransform>(), 7f);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            EditorUtility.SetDirty(iconImage);
        }
    }

    private static void ConfigureSubtitle(Scene scene)
    {
        GameObject panel = FindInScene(scene, "SubtitlePanel");
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 36f), new Vector2(1240f, 96f), new Vector2(0.5f, 0f));
        StretchInside(FindRecursive(panel.transform, "Label").GetComponent<RectTransform>(), 18f);
    }

    private static void ConfigureExaminePanel(Scene scene)
    {
        GameObject panel = FindInScene(scene, "ExaminePanel");
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(860f, 620f), new Vector2(0.5f, 0.5f));

        RectTransform artworkRect = FindRecursive(panel.transform, "Artwork").GetComponent<RectTransform>();
        SetRect(artworkRect, Center, Center, new Vector2(-205f, 45f), new Vector2(380f, 430f), Center);
        Image artworkImage = artworkRect.GetComponent<Image>();
        artworkImage.preserveAspect = true;
        EditorUtility.SetDirty(artworkImage);

        RectTransform bodyRect = FindRecursive(panel.transform, "BodyText").GetComponent<RectTransform>();
        SetRect(bodyRect, Center, Center, new Vector2(205f, 15f), new Vector2(330f, 390f), Center);

        GameObject close = FindRecursive(panel.transform, "CloseButton").gameObject;
        SetRect(close.GetComponent<RectTransform>(), Center, Center, new Vector2(392f, 272f),
            new Vector2(54f, 44f), Center);
        StretchInside(FindRecursive(close.transform, "CloseButton_Label").GetComponent<RectTransform>(), 0f);
    }

    private static void ConfigurePuzzlePanel(Scene scene, string panelName, string dialPrefix)
    {
        GameObject panel = FindInScene(scene, panelName);
        SetRect(panel.GetComponent<RectTransform>(), Center, Center, Vector2.zero,
            new Vector2(680f, 440f), Center);

        float[] xPositions = { -180f, 0f, 180f };
        for (int i = 0; i < 3; i++)
        {
            SetButtonWithLabel(panel.transform, $"{dialPrefix}{i}_Up", new Vector2(xPositions[i], 110f),
                new Vector2(76f, 56f));
            SetRect(FindRecursive(panel.transform, $"{dialPrefix}{i}_Label").GetComponent<RectTransform>(),
                Center, Center, new Vector2(xPositions[i], 25f), new Vector2(120f, 64f), Center);
            SetButtonWithLabel(panel.transform, $"{dialPrefix}{i}_Down", new Vector2(xPositions[i], -65f),
                new Vector2(76f, 56f));
        }

        SetButtonWithLabel(panel.transform, "ConfirmButton", new Vector2(0f, -165f), new Vector2(210f, 58f));
        SetButtonWithLabel(panel.transform, "CloseButton", new Vector2(305f, 190f), new Vector2(50f, 42f));
    }

    private static void SetButtonWithLabel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject button = FindRecursive(parent, name).gameObject;
        SetRect(button.GetComponent<RectTransform>(), Center, Center, position, size, Center);
        Transform label = FindRecursive(button.transform, $"{name}_Label");
        if (label != null)
            StretchInside(label.GetComponent<RectTransform>(), 0f);
    }

    private static readonly Vector2 Center = new(0.5f, 0.5f);

    private static void SetRect(Scene scene, string objectName, Vector2 position, Vector2 size)
    {
        GameObject target = FindInScene(scene, objectName);
        if (target == null)
            throw new InvalidOperationException($"Layout object not found: {objectName}");
        SetRect(target.GetComponent<RectTransform>(), Center, Center, position, size, Center);
    }

    private static void SetRectPosition(Scene scene, string objectName, Vector2 position)
    {
        GameObject target = FindInScene(scene, objectName);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void SetNativeRect(Scene scene, string objectName, Vector2 position)
    {
        GameObject target = FindInScene(scene, objectName);
        Image image = target.GetComponent<Image>();
        if (image == null || image.sprite == null)
            throw new InvalidOperationException($"Native-sized Image not found: {objectName}");
        image.SetNativeSize();
        image.preserveAspect = true;
        SetRectPosition(scene, objectName, position);
        EditorUtility.SetDirty(image);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void StretchInside(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void ApplyItemIcons()
    {
        foreach (KeyValuePair<string, string> pair in ItemIconMap)
        {
            string itemPath = $"Assets/Data/Items/{pair.Key}";
            InventoryItemSO item = AssetDatabase.LoadAssetAtPath<InventoryItemSO>(itemPath);
            if (item == null)
                throw new InvalidOperationException($"Inventory item not found: {itemPath}");

            SerializedObject serializedItem = new(item);
            SerializedProperty icon = serializedItem.FindProperty("icon");
            if (icon == null)
                throw new InvalidOperationException($"Icon property not found: {itemPath}");

            icon.objectReferenceValue = LoadSprite(pair.Value);
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }
    }

    private static void ApplySerializedSprite<T>(Scene scene, string objectName, string propertyName,
        string relativeSpritePath) where T : Component
    {
        GameObject target = FindInScene(scene, objectName);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null)
            throw new InvalidOperationException($"{typeof(T).Name} not found on {objectName}");

        SerializedObject serializedComponent = new(component);
        SerializedProperty property = serializedComponent.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Property not found: {typeof(T).Name}.{propertyName}");

        property.objectReferenceValue = LoadSprite(relativeSpritePath);
        serializedComponent.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static Sprite LoadSprite(string relativePath)
    {
        string path = $"{ResourceRoot}/{relativePath}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException($"Sprite not found or not imported as Sprite: {path}");
        return sprite;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindRecursive(current.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
