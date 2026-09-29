using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoomNavigationController : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private GameObject[] rooms;
    [SerializeField] private int startingRoom;
    [SerializeField] private bool wrapAround;
    [SerializeField] private bool restoreSavedRoom = true;
    [SerializeField] private string saveKey = "TheTableWeShared.RoomNavigation.v1";

    [Header("Optional UI")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onRoomChanged;

    public int CurrentRoomIndex { get; private set; }
    public int RoomCount => rooms?.Length ?? 0;
    public event Action<int> RoomChanged;

    private bool[] visitedRooms;

    [Serializable]
    private class RoomNavigationSaveData
    {
        public int currentRoomIndex;
        public bool[] visitedRooms;
    }

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousRoom);
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextRoom);

        visitedRooms = new bool[RoomCount];

        int initialRoom = startingRoom;
        if (restoreSavedRoom && TryLoadState(out RoomNavigationSaveData savedState))
        {
            initialRoom = savedState.currentRoomIndex;
            CopyVisitedState(savedState.visitedRooms);
        }

        ShowRoomInternal(initialRoom, false);
    }

    public void ShowPreviousRoom()
    {
        ShowRoom(CurrentRoomIndex - 1);
    }

    public void ShowNextRoom()
    {
        ShowRoom(CurrentRoomIndex + 1);
    }

    public void ShowRoom(int index)
    {
        ShowRoomInternal(index, true);
    }

    public bool IsRoomVisited(int index)
    {
        return visitedRooms != null && index >= 0 && index < visitedRooms.Length && visitedRooms[index];
    }

    public void SaveState()
    {
        if (string.IsNullOrWhiteSpace(saveKey))
            return;

        RoomNavigationSaveData data = new()
        {
            currentRoomIndex = CurrentRoomIndex,
            visitedRooms = visitedRooms,
        };

        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void ClearSavedState()
    {
        if (!string.IsNullOrWhiteSpace(saveKey))
            PlayerPrefs.DeleteKey(saveKey);

        visitedRooms = new bool[RoomCount];
        ShowRoomInternal(startingRoom, true);
    }

    private void ShowRoomInternal(int index, bool saveAfterChange)
    {
        if (rooms == null || rooms.Length == 0)
            return;

        CurrentRoomIndex = wrapAround
            ? (index % rooms.Length + rooms.Length) % rooms.Length
            : Mathf.Clamp(index, 0, rooms.Length - 1);

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null)
                rooms[i].SetActive(i == CurrentRoomIndex);
        }

        if (previousButton != null)
            previousButton.gameObject.SetActive(wrapAround || CurrentRoomIndex > 0);
        if (nextButton != null)
            nextButton.gameObject.SetActive(wrapAround || CurrentRoomIndex < rooms.Length - 1);

        if (visitedRooms == null || visitedRooms.Length != rooms.Length)
            visitedRooms = new bool[rooms.Length];
        visitedRooms[CurrentRoomIndex] = true;

        if (saveAfterChange)
            SaveState();

        RoomChanged?.Invoke(CurrentRoomIndex);
        onRoomChanged?.Invoke(CurrentRoomIndex);
    }

    private bool TryLoadState(out RoomNavigationSaveData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(saveKey) || !PlayerPrefs.HasKey(saveKey))
            return false;

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            data = JsonUtility.FromJson<RoomNavigationSaveData>(json);
            return data != null;
        }
        catch (ArgumentException)
        {
            PlayerPrefs.DeleteKey(saveKey);
            return false;
        }
    }

    private void CopyVisitedState(bool[] savedVisitedRooms)
    {
        if (savedVisitedRooms == null || visitedRooms == null)
            return;

        int count = Mathf.Min(savedVisitedRooms.Length, visitedRooms.Length);
        Array.Copy(savedVisitedRooms, visitedRooms, count);
    }
}
