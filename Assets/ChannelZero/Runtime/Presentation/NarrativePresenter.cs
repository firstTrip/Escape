using System;
using System.Collections.Generic;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class NarrativePresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text outputLabel;

        private readonly Queue<NarrativeTextEntry> queue = new();
        private ChannelZeroSessionState session;
        private Action completed;
        private int startedFrame = -1;

        public bool IsPresenting { get; private set; }
        public NarrativeTextEntry Current { get; private set; }

        public void Configure(TMP_Text label)
        {
            outputLabel = label;
        }

        public bool Begin(IEnumerable<NarrativeTextEntry> entries, ChannelZeroSessionState state,
            Action onCompleted = null)
        {
            queue.Clear();
            if (entries != null)
            {
                foreach (NarrativeTextEntry entry in entries)
                    if (entry != null)
                        queue.Enqueue(entry);
            }

            if (queue.Count == 0)
            {
                onCompleted?.Invoke();
                return false;
            }

            session = state;
            completed = onCompleted;
            IsPresenting = true;
            startedFrame = Time.frameCount;
            ShowNext();
            return true;
        }

        public void Advance()
        {
            if (!IsPresenting)
                return;
            ShowNext();
        }

        public void Skip()
        {
            if (!IsPresenting)
                return;

            while (queue.Count > 0)
                MarkSeen(queue.Dequeue());
            Finish();
        }

        private void Update()
        {
            if (!IsPresenting || Time.frameCount == startedFrame)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Skip();
            else if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                Advance();
        }

        private void ShowNext()
        {
            if (queue.Count == 0)
            {
                Finish();
                return;
            }

            Current = queue.Dequeue();
            MarkSeen(Current);
            if (outputLabel != null)
            {
                string speaker = string.IsNullOrWhiteSpace(Current.speaker) ? string.Empty : $"[{Current.speaker}] ";
                outputLabel.text = speaker + Current.text
                    + "\n<size=65%><color=#BFA66F>클릭 또는 Enter</color></size>";
            }
        }

        private void MarkSeen(NarrativeTextEntry entry)
        {
            if (entry == null || session == null)
                return;
            if (entry.once)
                session.MarkTextSeen(entry.id);
            if (string.Equals(entry.type, "document", StringComparison.OrdinalIgnoreCase))
                session.MarkRecordRead(entry.id);
        }

        private void Finish()
        {
            IsPresenting = false;
            Current = null;
            session = null;
            Action callback = completed;
            completed = null;
            callback?.Invoke();
        }
    }
}
