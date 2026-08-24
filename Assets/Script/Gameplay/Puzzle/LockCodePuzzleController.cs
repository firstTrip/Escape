using System;
using UnityEngine;
using UnityEngine.UI;

public class LockCodePuzzleController : InteractableHotspot
{
    [Header("Answer")]
    [SerializeField] private int[] correctCode = { 3, 1, 3 };

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text[] digitLabels;
    [SerializeField] private Button[] digitUpButtons;
    [SerializeField] private Button[] digitDownButtons;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Reward")]
    [SerializeField] private InventoryItemSO keyItem;
    [SerializeField] private InventoryItemSO lighterItem;
    [SerializeField] private Image lockedVisual;
    [SerializeField] private Image openVisual;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip wrongClip;
    [TextArea][SerializeField] private string solvedLine = "상자가 열렸다. 여벌 열쇠와 오래된 라이터가 들어 있다.";
    [TextArea][SerializeField] private string wrongLine = "숫자가 맞지 않는 것 같다.";

    public event Action OnSolved;
    public bool IsSolved { get; private set; }

    private int[] digits;

    private void Awake()
    {
        digits = new int[correctCode.Length];
        for (int i = 0; i < digitUpButtons.Length; i++)
        {
            int idx = i;
            digitUpButtons[idx].onClick.AddListener(() => ChangeDigit(idx, 1));
            digitDownButtons[idx].onClick.AddListener(() => ChangeDigit(idx, -1));
        }
        confirmButton.onClick.AddListener(TryConfirm);
        closeButton.onClick.AddListener(ClosePanel);
        if (panelRoot != null) panelRoot.SetActive(false);
        RefreshLabels();
    }

    protected override void OnInteract()
    {
        if (IsSolved) return;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void ChangeDigit(int index, int delta)
    {
        digits[index] = ((digits[index] + delta) % 10 + 10) % 10;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        for (int i = 0; i < digitLabels.Length; i++)
            digitLabels[i].text = digits[i].ToString();
    }

    private void TryConfirm()
    {
        bool match = true;
        for (int i = 0; i < correctCode.Length; i++)
        {
            if (digits[i] != correctCode[i]) { match = false; break; }
        }

        if (!match)
        {
            if (sfxSource != null && wrongClip != null) sfxSource.PlayOneShot(wrongClip);
            SubtitleUI.Instance?.ShowLine(wrongLine);
            return;
        }

        IsSolved = true;
        if (sfxSource != null && openClip != null) sfxSource.PlayOneShot(openClip);
        InventoryManager.Instance?.Add(keyItem);
        InventoryManager.Instance?.Add(lighterItem);
        if (lockedVisual != null) lockedVisual.enabled = false;
        if (openVisual != null) openVisual.enabled = true;
        SubtitleUI.Instance?.ShowLine(solvedLine);
        ClosePanel();
        Interactable = false;
        OnSolved?.Invoke();
    }

    private void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
