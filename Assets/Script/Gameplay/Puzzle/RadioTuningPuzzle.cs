using System;
using UnityEngine;
using UnityEngine.UI;

public class RadioTuningPuzzle : InteractableHotspot
{
    [Header("Answer")]
    [SerializeField] private int[] correctCode = { 0, 9, 8 };

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text[] digitLabels;
    [SerializeField] private Button[] digitUpButtons;
    [SerializeField] private Button[] digitDownButtons;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Outcome")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip staticClip;
    [SerializeField] private AudioClip motifClip;
    [SerializeField] private Sprite transitionSprite;
    [TextArea][SerializeField] private string wrongLine = "지지직거리는 잡음뿐이다.";
    [TextArea][SerializeField] private string transitionText =
        "잡음 사이로, 낯익은 네 개의 음 중 한 소절이 섞여 든다.\n\n2번 공간 · 사무실로 이어집니다.\n(다음 업데이트에서 계속됩니다)";

    public event Action OnSolved;
    public bool IsSolved { get; private set; }

    private int[] digits;

    private void Awake()
    {
        digits = new int[correctCode.Length];
        int buttonCount = Mathf.Min(digitUpButtons?.Length ?? 0, digitDownButtons?.Length ?? 0);
        for (int i = 0; i < buttonCount; i++)
        {
            int idx = i;
            if (digitUpButtons[idx] != null)
                digitUpButtons[idx].onClick.AddListener(() => ChangeDigit(idx, 1));
            if (digitDownButtons[idx] != null)
                digitDownButtons[idx].onClick.AddListener(() => ChangeDigit(idx, -1));
        }
        if (confirmButton != null) confirmButton.onClick.AddListener(TryConfirm);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
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
        if (digitLabels == null) return;
        for (int i = 0; i < digitLabels.Length; i++)
        {
            if (digitLabels[i] != null)
                digitLabels[i].text = digits[i].ToString();
        }
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
            if (sfxSource != null && staticClip != null) sfxSource.PlayOneShot(staticClip);
            SubtitleUI.Instance?.ShowLine(wrongLine);
            return;
        }

        IsSolved = true;
        if (sfxSource != null && motifClip != null) sfxSource.PlayOneShot(motifClip);
        ClosePanel();
        ExaminePanelUI.Instance?.Show(transitionSprite, transitionText);
        Interactable = false;
        OnSolved?.Invoke();
    }

    private void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
