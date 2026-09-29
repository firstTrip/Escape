using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class NumericCodePuzzleBase : InteractableHotspot, IPuzzle
{
    [Header("Answer")]
    [SerializeField] private int[] correctCode = Array.Empty<int>();

    [Header("Shared numeric puzzle UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text[] digitLabels;
    [SerializeField] private Button[] digitUpButtons;
    [SerializeField] private Button[] digitDownButtons;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    private int[] digits;
    private INumericPuzzleView view;

    public bool IsSolved { get; private set; }
    protected virtual string DigitLabelPrefix => "Dial";
    protected abstract int[] DefaultCode { get; }

    protected virtual void Awake()
    {
        if (correctCode == null || correctCode.Length == 0)
            correctCode = DefaultCode ?? Array.Empty<int>();
        digits = new int[correctCode?.Length ?? 0];
        ResolveDigitLabels();
        view = new UnityNumericPuzzleView(panelRoot, digitLabels, digitUpButtons,
            digitDownButtons, confirmButton, closeButton);
        view.DigitChangeRequested += ChangeDigit;
        view.ConfirmRequested += TryConfirm;
        view.Close();
        view.Render(digits);
    }

    protected virtual void OnDestroy()
    {
        if (view == null)
            return;
        view.DigitChangeRequested -= ChangeDigit;
        view.ConfirmRequested -= TryConfirm;
        view.Dispose();
    }

    protected override void OnInteract()
    {
        if (!IsSolved)
            view?.Open();
    }

    protected abstract void HandleIncorrectAnswer();
    protected abstract void HandleSolved();

    private void ResolveDigitLabels()
    {
        int count = correctCode?.Length ?? 0;
        if (panelRoot == null)
            return;
        if (digitLabels == null || digitLabels.Length != count)
            digitLabels = new TMP_Text[count];

        for (int i = 0; i < digitLabels.Length; i++)
        {
            if (digitLabels[i] != null)
                continue;
            Transform labelTransform = panelRoot.transform.Find($"{DigitLabelPrefix}{i}_Label");
            if (labelTransform != null)
                digitLabels[i] = labelTransform.GetComponent<TMP_Text>();
        }
    }

    private void ChangeDigit(int index, int delta)
    {
        if (IsSolved || index < 0 || index >= digits.Length)
            return;
        digits[index] = ((digits[index] + delta) % 10 + 10) % 10;
        view.Render(digits);
    }

    private void TryConfirm()
    {
        if (IsSolved)
            return;

        bool match = correctCode != null && digits.Length == correctCode.Length;
        for (int i = 0; match && i < correctCode.Length; i++)
            match = digits[i] == correctCode[i];

        if (!match)
        {
            HandleIncorrectAnswer();
            return;
        }

        IsSolved = true;
        view.SetInputEnabled(false);
        view.Close();
        HandleSolved();
        Interactable = false;
    }
}

internal sealed class UnityNumericPuzzleView : INumericPuzzleView
{
    private readonly GameObject root;
    private readonly TMP_Text[] labels;
    private readonly Button[] upButtons;
    private readonly Button[] downButtons;
    private readonly Button confirmButton;
    private readonly Button closeButton;
    private readonly List<(Button button, UnityAction listener)> listeners = new();

    public event Action<int, int> DigitChangeRequested;
    public event Action ConfirmRequested;
    public bool IsOpen => root != null && root.activeSelf;

    public UnityNumericPuzzleView(GameObject root, TMP_Text[] labels, Button[] upButtons,
        Button[] downButtons, Button confirmButton, Button closeButton)
    {
        this.root = root;
        this.labels = labels ?? Array.Empty<TMP_Text>();
        this.upButtons = upButtons ?? Array.Empty<Button>();
        this.downButtons = downButtons ?? Array.Empty<Button>();
        this.confirmButton = confirmButton;
        this.closeButton = closeButton;
        BindButtons();
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);
        SetInputEnabled(true);
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Render(IReadOnlyList<int> digits)
    {
        int count = Mathf.Min(labels.Length, digits?.Count ?? 0);
        for (int i = 0; i < count; i++)
            if (labels[i] != null)
                labels[i].text = digits[i].ToString();
    }

    public void SetInputEnabled(bool enabled)
    {
        foreach (Button button in upButtons)
            if (button != null) button.interactable = enabled;
        foreach (Button button in downButtons)
            if (button != null) button.interactable = enabled;
        if (confirmButton != null) confirmButton.interactable = enabled;
        if (closeButton != null) closeButton.interactable = enabled;
    }

    public void Dispose()
    {
        foreach ((Button button, UnityAction listener) in listeners)
            if (button != null)
                button.onClick.RemoveListener(listener);
        listeners.Clear();
    }

    private void BindButtons()
    {
        int count = Mathf.Min(upButtons.Length, downButtons.Length);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            Bind(upButtons[i], () => DigitChangeRequested?.Invoke(index, 1));
            Bind(downButtons[i], () => DigitChangeRequested?.Invoke(index, -1));
        }
        Bind(confirmButton, () => ConfirmRequested?.Invoke());
        Bind(closeButton, Close);
    }

    private void Bind(Button button, Action callback)
    {
        if (button == null)
            return;
        UnityAction listener = () => callback();
        button.onClick.AddListener(listener);
        listeners.Add((button, listener));
    }
}
