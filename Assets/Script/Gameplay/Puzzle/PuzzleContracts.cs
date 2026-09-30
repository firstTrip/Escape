using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPuzzle : IInteractable
{
    bool IsSolved { get; }
}

public interface IPuzzleModalView
{
    bool IsOpen { get; }
    void Close();
}

public interface IPuzzleFeedbackView
{
    void ShowFeedback(string message, float extraDuration = 0f);
}

public interface IPuzzleDocumentView : IPuzzleModalView
{
    void ShowDocument(Sprite artwork, string body);
}

public interface INumericPuzzleView : IPuzzleModalView, IDisposable
{
    event Action<int, int> DigitChangeRequested;
    event Action ConfirmRequested;
    void Open();
    void Render(IReadOnlyList<int> digits);
    void SetInputEnabled(bool enabled);
}
