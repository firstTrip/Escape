using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RoomNavigationScaffoldCreator
{
    private static readonly string[] RoomNames =
    {
        "Room_Living",
        "Room_DiningEntry",
        "Room_Kitchen",
        "Room_Bedroom",
    };

    private const string PrologueScenePath = "Assets/Scenes/00_Prologue.unity";
    private const string EmptyRoomBackgroundPath = "Assets/Resources/room/1.room_wall.png";
    private const string AutoCreateSessionKey = "TheTableWeShared.RoomScaffold.AutoCreate.v5";

    [InitializeOnLoadMethod]
    private static void CreateForOpenPrologueSceneOnce()
    {
        if (SessionState.GetBool(AutoCreateSessionKey, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrologueScenePath)
                return;

            SessionState.SetBool(AutoCreateSessionKey, true);
            CreateRoomsOnly();

            if (scene.isDirty)
                EditorSceneManager.SaveScene(scene);
        };
    }

    [MenuItem("Tools/The Table We Shared/Create Empty Room Navigation Scaffold")]
    public static void CreateScaffold()
    {
        GameObject hotspotLayer = GameObject.Find("HotspotLayer");
        if (hotspotLayer == null)
        {
            Debug.LogError("HotspotLayer was not found in the open scene.");
            return;
        }

        GameObject roomRoot = FindDirectChild(hotspotLayer.transform, "RoomRoot");
        if (roomRoot == null)
        {
            roomRoot = CreateUiObject("RoomRoot", hotspotLayer.transform);
            StretchToParent(roomRoot.GetComponent<RectTransform>());
        }

        GameObject[] rooms = new GameObject[RoomNames.Length];
        for (int i = 0; i < RoomNames.Length; i++)
        {
            rooms[i] = FindDirectChild(roomRoot.transform, RoomNames[i]);
            if (rooms[i] == null)
            {
                rooms[i] = CreateUiObject(RoomNames[i], roomRoot.transform);
                StretchToParent(rooms[i].GetComponent<RectTransform>());
            }

            EnsureRoomLayers(rooms[i]);
        }

        PreserveLegacyContent(hotspotLayer.transform, roomRoot.transform);

        RoomNavigationController controller = hotspotLayer.GetComponent<RoomNavigationController>();
        if (controller == null)
            controller = Undo.AddComponent<RoomNavigationController>(hotspotLayer);

        SerializedObject serializedController = new(controller);
        SerializedProperty roomArray = serializedController.FindProperty("rooms");
        roomArray.arraySize = rooms.Length;
        for (int i = 0; i < rooms.Length; i++)
            roomArray.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        serializedController.FindProperty("startingRoom").intValue = 1;
        serializedController.FindProperty("wrapAround").boolValue = false;
        serializedController.FindProperty("restoreSavedRoom").boolValue = true;

        GameObject navigationUi = FindDirectChild(hotspotLayer.transform, "RoomNavigationUI");
        if (navigationUi == null)
        {
            navigationUi = CreateUiObject("RoomNavigationUI", hotspotLayer.transform);
            StretchToParent(navigationUi.GetComponent<RectTransform>());
        }

        Button previousButton = CreateNavigationButton(navigationUi.transform, "PreviousRoomButton", true);
        Button nextButton = CreateNavigationButton(navigationUi.transform, "NextRoomButton", false);
        serializedController.FindProperty("previousButton").objectReferenceValue = previousButton;
        serializedController.FindProperty("nextButton").objectReferenceValue = nextButton;
        serializedController.ApplyModifiedProperties();

        for (int i = 0; i < rooms.Length; i++)
            rooms[i].SetActive(i == 1);

        Selection.activeGameObject = roomRoot;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("ROOM_NAVIGATION_SCAFFOLD_READY");
    }

    [MenuItem("Tools/The Table We Shared/Create Rooms Only (1920x1080)")]
    public static void CreateRoomsOnly()
    {
        CreateScaffold();
        InGameVisualStyleApplicator.ApplyToOpenPrologueScene();
        Debug.Log("ROOMS_ONLY_READY: four empty rooms and navigation UI were created; furniture was not placed.");
    }

    public static void CreateRoomsInProject()
    {
        Scene scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
        CreateRoomsOnly();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(result, $"Create {name}");
        result.layer = parent.gameObject.layer;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child.gameObject;
        }

        return null;
    }

    private static void EnsureRoomLayers(GameObject room)
    {
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyRoomBackgroundPath);

        GameObject background = FindDirectChild(room.transform, "Background");
        if (background == null)
            background = CreateUiObject("Background", room.transform);
        StretchToParent(background.GetComponent<RectTransform>());
        background.transform.SetAsFirstSibling();

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = Undo.AddComponent<Image>(background);
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;
        backgroundImage.color = Color.white;

        EnsureLayer(room.transform, "FurnitureLayer");
        EnsureLayer(room.transform, "PuzzleLayer");
        EnsureLayer(room.transform, "RoomHotspotLayer");
    }

    private static void EnsureLayer(Transform room, string layerName)
    {
        GameObject layer = FindDirectChild(room, layerName);
        if (layer == null)
            layer = CreateUiObject(layerName, room);
        StretchToParent(layer.GetComponent<RectTransform>());
    }

    private static void PreserveLegacyContent(Transform hotspotLayer, Transform roomRoot)
    {
        GameObject legacyRoot = FindDirectChild(hotspotLayer, "LegacySingleRoomContent");
        if (legacyRoot == null)
        {
            legacyRoot = CreateUiObject("LegacySingleRoomContent", hotspotLayer);
            StretchToParent(legacyRoot.GetComponent<RectTransform>());
        }

        for (int i = hotspotLayer.childCount - 1; i >= 0; i--)
        {
            Transform child = hotspotLayer.GetChild(i);
            if (child == roomRoot || child == legacyRoot.transform || child.name == "RoomNavigationUI")
                continue;

            Undo.SetTransformParent(child, legacyRoot.transform, "Preserve legacy room content");
        }

        legacyRoot.SetActive(false);
        roomRoot.SetAsLastSibling();
    }

    private static Button CreateNavigationButton(Transform parent, string name, bool isPrevious)
    {
        GameObject buttonObject = FindDirectChild(parent, name);
        if (buttonObject == null)
            buttonObject = CreateUiObject(name, parent);

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(buttonObject);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/arrow.png");
        image.preserveAspect = true;
        image.raycastTarget = true;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = Undo.AddComponent<Button>(buttonObject);
        button.targetGraphic = image;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(isPrevious ? 0f : 1f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(isPrevious ? 70f : -70f, 0f);
        // Keep the 1920x1080 canvas and reuse the exact InGame arrow proportions.
        rect.sizeDelta = new Vector2(75f, 66f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, isPrevious ? 90f : 270f);
        return button;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
