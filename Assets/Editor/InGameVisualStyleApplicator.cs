using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InGameVisualStyleApplicator
{
    private const string PrologueScenePath = "Assets/Scenes/00_Prologue.unity";
    private const string ArrowPath = "Assets/Resources/UI/arrow.png";
    private const string ItemBoxPath = "Assets/Resources/UI/Item_box.png";

    [MenuItem("Tools/The Table We Shared/Apply InGame Visual Style")]
    public static void ApplyToOpenPrologueScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != PrologueScenePath)
        {
            Debug.LogError($"Open {PrologueScenePath} before applying the InGame visual style.");
            return;
        }

        Sprite arrow = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
        Sprite itemBox = AssetDatabase.LoadAssetAtPath<Sprite>(ItemBoxPath);
        if (arrow == null || itemBox == null)
        {
            Debug.LogError("The InGame arrow or item-box sprite could not be loaded.");
            return;
        }

        ApplyNavigationButton("PreviousRoomButton", arrow, 90f);
        ApplyNavigationButton("NextRoomButton", arrow, 270f);

        GameObject inventoryBar = FindInActiveScene("InventoryBar");
        if (inventoryBar != null)
        {
            ApplyInventoryLayout(inventoryBar);

            for (int i = 0; i < inventoryBar.transform.childCount; i++)
            {
                Transform child = inventoryBar.transform.GetChild(i);
                if (!child.name.StartsWith("Slot"))
                    continue;

                Image slotImage = child.GetComponent<Image>();
                if (slotImage == null)
                    continue;

                Undo.RecordObject(slotImage, "Apply InGame item slot style");
                slotImage.sprite = itemBox;
                slotImage.type = Image.Type.Simple;
                slotImage.preserveAspect = true;
                slotImage.color = Color.white;
            }
        }

        CreateBlackScreenFrame();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("INGAME_VISUAL_STYLE_APPLIED: layout and 1920x1080 CanvasScaler were preserved.");
    }

    private static void ApplyNavigationButton(string objectName, Sprite arrow, float rotationZ)
    {
        GameObject buttonObject = FindInActiveScene(objectName);
        if (buttonObject == null)
            return;

        Image image = buttonObject.GetComponent<Image>();
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (image == null || rect == null)
            return;

        Undo.RecordObjects(new Object[] { image, rect }, "Apply InGame navigation style");
        image.sprite = arrow;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        rect.sizeDelta = new Vector2(75f, 66f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        if (objectName == "NextRoomButton")
            rect.anchoredPosition = new Vector2(-165f, 0f);
    }

    private static void ApplyInventoryLayout(GameObject inventoryBar)
    {
        Undo.RecordObject(inventoryBar, "Enable InGame inventory");
        inventoryBar.SetActive(true);

        RectTransform barRect = inventoryBar.GetComponent<RectTransform>();
        if (barRect == null)
            return;

        Undo.RecordObject(barRect, "Apply InGame inventory layout");
        barRect.anchorMin = new Vector2(1f, 0.5f);
        barRect.anchorMax = new Vector2(1f, 0.5f);
        barRect.pivot = new Vector2(1f, 0.5f);
        barRect.anchoredPosition = new Vector2(-28f, 0f);
        barRect.sizeDelta = new Vector2(104f, 678f);

        int slotIndex = 0;
        for (int i = 0; i < inventoryBar.transform.childCount; i++)
        {
            RectTransform slotRect = inventoryBar.transform.GetChild(i) as RectTransform;
            if (slotRect == null || !slotRect.name.StartsWith("Slot"))
                continue;

            Undo.RecordObject(slotRect, "Apply InGame inventory slot layout");
            slotRect.anchorMin = new Vector2(0.5f, 1f);
            slotRect.anchorMax = new Vector2(0.5f, 1f);
            slotRect.pivot = new Vector2(0.5f, 1f);
            slotRect.anchoredPosition = new Vector2(0f, -(slotIndex * 98f));
            slotRect.sizeDelta = new Vector2(90f, 90f);
            slotRect.localScale = Vector3.one;
            slotIndex++;
        }
    }

    private static void CreateBlackScreenFrame()
    {
        GameObject canvas = FindInActiveScene("GameCanvas");
        if (canvas == null)
            return;

        Transform frame = canvas.transform.Find("InGameBlackFrame");
        if (frame == null)
        {
            GameObject frameObject = new("InGameBlackFrame", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(frameObject, "Create InGame black frame");
            frameObject.layer = canvas.layer;
            frameObject.transform.SetParent(canvas.transform, false);
            frame = frameObject.transform;
        }

        Stretch(frame as RectTransform);
        CreateFrameEdge(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -9f), new Vector2(0f, 18f));
        CreateFrameEdge(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 9f), new Vector2(0f, 18f));
        CreateFrameEdge(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(9f, 0f), new Vector2(18f, 0f));
        CreateFrameEdge(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-9f, 0f), new Vector2(18f, 0f));
        frame.SetAsLastSibling();
    }

    private static void CreateFrameEdge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        Transform existing = parent.Find(name);
        GameObject edgeObject;
        if (existing == null)
        {
            edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(edgeObject, $"Create {name} frame edge");
            edgeObject.layer = parent.gameObject.layer;
            edgeObject.transform.SetParent(parent, false);
        }
        else
        {
            edgeObject = existing.gameObject;
        }

        RectTransform rect = edgeObject.GetComponent<RectTransform>();
        Image image = edgeObject.GetComponent<Image>();
        Undo.RecordObjects(new Object[] { rect, image }, "Apply InGame black frame style");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        image.sprite = null;
        image.color = new Color(0.018f, 0.016f, 0.014f, 1f);
        image.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static GameObject FindInActiveScene(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }
}
